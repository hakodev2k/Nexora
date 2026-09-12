using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Planner;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Planner;

/// <summary>
/// Persists the Planner as a same-owner Task lens. It never creates a Task,
/// Calendar Event, reminder, or source lifecycle transition.
/// </summary>
public sealed class SqlPlannerService : IPlannerService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlPlannerService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<PlannerPlan> List(IdentityPrincipal actor, DateOnly? from = null, DateOnly? to = null)
    {
        if (!HasCapabilities(actor, "planner.plan.read") || !TaskReadAvailable(actor))
            return ModuleUnavailable<PlannerPlan>();

        using var connection = _connections.Create();
        connection.Open();
        DateOnly rangeFrom;
        DateOnly rangeTo;
        if (from is null && to is null)
        {
            if (!TryReadOwnerLocalToday(connection, actor.OwnerId, out var ownerToday))
                return PersistenceFailure<PlannerPlan>();
            rangeFrom = ownerToday;
            rangeTo = ownerToday;
        }
        else
        {
            rangeFrom = from ?? to!.Value;
            rangeTo = to ?? from!.Value;
        }

        if (!PlannerPolicy.IsValidRange(rangeFrom, rangeTo))
            return Failure<PlannerPlan>("ValidationFailed", 422, "Planner range must be between one and 32 local days.");

        try
        {
            return IdentityOperationResult<PlannerPlan>.Success(ReadPlan(connection, null, actor.OwnerId, rangeFrom, rangeTo, false));
        }
        catch (SqlException)
        {
            return PersistenceFailure<PlannerPlan>();
        }
    }

    public IdentityOperationResult<PlannerPinRecord> Pin(IdentityPrincipal actor, PlannerPinCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapabilities(actor, "planner.plan.pin") || !TaskReadAvailable(actor))
            return ModuleUnavailable<PlannerPinRecord>();
        if (!TryValidateNotes(command.Notes, out var notes))
            return Failure<PlannerPinRecord>("ValidationFailed", 422, "Planner notes must be at most 2000 characters.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<PlannerPinRecord>(connection, transaction, actor, "planner.plan.pin", idempotencyKey,
            $"task:{command.TaskId:N}|date:{command.PlanDate:yyyy-MM-dd}|notes:{notes}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var source = ReadTask(connection, transaction, actor.OwnerId, command.TaskId, true);
            if (source is null) return Rollback(transaction, Missing<PlannerPinRecord>());
            if (!source.SourceAvailable) return Rollback(transaction, LifecycleLocked<PlannerPinRecord>());
            if (ReadPinByTaskDate(connection, transaction, actor.OwnerId, command.TaskId, command.PlanDate, true) is not null)
                return Rollback(transaction, Failure<PlannerPinRecord>("PlannerPinDuplicate", 409, "This Task is already pinned for that day."));

            var rank = ReadNextRank(connection, transaction, actor.OwnerId, command.PlanDate);
            var pinId = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [productivity].[PlannerPin]
                    ([Id], [OwnerId], [TaskId], [CreatedByUserId], [UpdatedByUserId], [PlanDate], [Rank], [Notes])
                VALUES (@Id, @OwnerId, @TaskId, @ActorUserId, @ActorUserId, @PlanDate, @Rank, @Notes);
                """,
                ("@Id", SqlDbType.UniqueIdentifier, pinId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@TaskId", SqlDbType.UniqueIdentifier, command.TaskId, null),
                ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@PlanDate", SqlDbType.Date, ToDbDate(command.PlanDate), null),
                ("@Rank", SqlDbType.Decimal, rank, null),
                ("@Notes", SqlDbType.NVarChar, (object?)notes ?? DBNull.Value, 2000));

            var created = ReadPin(connection, transaction, actor.OwnerId, pinId, false);
            if (created is null) return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
            WriteAudit(connection, transaction, actor, pinId, "planner.plan.pin", "productivity.PlannerPin", traceId);
            CompleteReceipt(connection, transaction, receipt, "PlannerPinned");
            transaction.Commit();
            return IdentityOperationResult<PlannerPinRecord>.Success(created, 201, "PlannerPinned");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Rollback(transaction, Failure<PlannerPinRecord>("PlannerPinDuplicate", 409, "This Task is already pinned for that day."));
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
        }
    }

    public IdentityOperationResult<PlannerPinRecord> Update(IdentityPrincipal actor, Guid pinId, string? ifMatch,
        PlannerPinUpdateCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!TaskReadAvailable(actor))
            return ModuleUnavailable<PlannerPinRecord>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<PlannerPinRecord>();
        if (!TryValidateNotes(command.Notes, out var notes))
            return Failure<PlannerPinRecord>("ValidationFailed", 422, "Planner notes must be at most 2000 characters.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadPin(connection, transaction, actor.OwnerId, pinId, true);
        if (current is null) return Rollback(transaction, Missing<PlannerPinRecord>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<PlannerPinRecord>());
        var action = current.PlanDate == command.PlanDate ? "planner.plan.notes" : "planner.plan.reschedule";
        if (!HasCapabilities(actor, action)) return Rollback(transaction, ModuleUnavailable<PlannerPinRecord>());
        if (!current.SourceAvailable) return Rollback(transaction, LifecycleLocked<PlannerPinRecord>());
        var receiptFailure = CheckReceipt<PlannerPinRecord>(connection, transaction, actor, action, idempotencyKey,
            $"pin:{pinId:N}|etag:{ifMatch}|date:{command.PlanDate:yyyy-MM-dd}|notes:{notes}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var rank = current.Rank;
            if (current.PlanDate != command.PlanDate)
            {
                if (ReadPinByTaskDate(connection, transaction, actor.OwnerId, current.TaskId, command.PlanDate, true) is not null)
                    return Rollback(transaction, Failure<PlannerPinRecord>("PlannerPinDuplicate", 409, "This Task is already pinned for that day."));
                rank = ReadNextRank(connection, transaction, actor.OwnerId, command.PlanDate);
            }
            var changed = Execute(connection, transaction, """
                UPDATE [productivity].[PlannerPin]
                SET [PlanDate] = @PlanDate, [Rank] = @Rank, [Notes] = @Notes,
                    [UpdatedByUserId] = @ActorUserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """,
                ("@PlanDate", SqlDbType.Date, ToDbDate(command.PlanDate), null),
                ("@Rank", SqlDbType.Decimal, rank, null),
                ("@Notes", SqlDbType.NVarChar, (object?)notes ?? DBNull.Value, 2000),
                ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Id", SqlDbType.UniqueIdentifier, pinId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            if (changed != 1) return Rollback(transaction, Revision<PlannerPinRecord>());
            var updated = ReadPin(connection, transaction, actor.OwnerId, pinId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
            WriteAudit(connection, transaction, actor, pinId, action, "productivity.PlannerPin", traceId);
            CompleteReceipt(connection, transaction, receipt, "PlannerPinUpdated");
            transaction.Commit();
            return IdentityOperationResult<PlannerPinRecord>.Success(updated, 200, "PlannerPinUpdated");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Rollback(transaction, Failure<PlannerPinRecord>("PlannerPinDuplicate", 409, "This Task is already pinned for that day."));
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
        }
    }

    public IdentityOperationResult<PlannerPlan> Reorder(IdentityPrincipal actor, string? ifMatch,
        PlannerReorderCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapabilities(actor, "planner.plan.reorder") || !TaskReadAvailable(actor))
            return ModuleUnavailable<PlannerPlan>();
        if (!TryETag(ifMatch, out var expectedPlanVersion)) return Precondition<PlannerPlan>();
        if (command.PinIds.Count is < 1 or > 100 || command.PinIds.Distinct().Count() != command.PinIds.Count)
            return Failure<PlannerPlan>("ValidationFailed", 422, "Planner reorder must contain distinct pins for one day.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadPlan(connection, transaction, actor.OwnerId, command.PlanDate, command.PlanDate, true);
        if (!MatchesETag(expectedPlanVersion, current.ETag)) return Rollback(transaction, Revision<PlannerPlan>());
        if (current.Pins.Count != command.PinIds.Count || !current.Pins.Select(pin => pin.Id).Order().SequenceEqual(command.PinIds.Order()))
            return Rollback(transaction, Failure<PlannerPlan>("PlannerReorderConflict", 409, "The submitted pins do not match the current plan day."));
        if (current.Pins.Any(pin => !pin.SourceAvailable)) return Rollback(transaction, LifecycleLocked<PlannerPlan>());
        var receiptFailure = CheckReceipt<PlannerPlan>(connection, transaction, actor, "planner.plan.reorder", idempotencyKey,
            $"date:{command.PlanDate:yyyy-MM-dd}|etag:{ifMatch}|pins:{string.Join(',', command.PinIds)}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            for (var index = 0; index < command.PinIds.Count; index++)
            {
                var changed = Execute(connection, transaction, """
                    UPDATE [productivity].[PlannerPin]
                    SET [Rank] = @Rank, [UpdatedByUserId] = @ActorUserId, [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [PlanDate] = @PlanDate;
                    """,
                    ("@Rank", SqlDbType.Decimal, (decimal)(index + 1), null),
                    ("@Id", SqlDbType.UniqueIdentifier, command.PinIds[index], null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@PlanDate", SqlDbType.Date, ToDbDate(command.PlanDate), null));
                if (changed != 1) return Rollback(transaction, Revision<PlannerPlan>());
            }
            var reordered = ReadPlan(connection, transaction, actor.OwnerId, command.PlanDate, command.PlanDate, false);
            WriteAudit(connection, transaction, actor, command.PinIds[0], "planner.plan.reorder", "productivity.PlannerPin", traceId);
            CompleteReceipt(connection, transaction, receipt, "PlannerReordered");
            transaction.Commit();
            return IdentityOperationResult<PlannerPlan>.Success(reordered, 200, "PlannerReordered");
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<PlannerPlan>());
        }
    }

    public IdentityOperationResult<object?> Unpin(IdentityPrincipal actor, Guid pinId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapabilities(actor, "planner.plan.unpin")) return ModuleUnavailable<object?>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<object?>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadPin(connection, transaction, actor.OwnerId, pinId, true);
        if (current is null) return Rollback(transaction, Missing<object?>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<object?>());
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "planner.plan.unpin", idempotencyKey,
            $"pin:{pinId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var removed = Execute(connection, transaction, """
                DELETE FROM [productivity].[PlannerPin]
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """,
                ("@Id", SqlDbType.UniqueIdentifier, pinId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            if (removed != 1) return Rollback(transaction, Revision<object?>());
            WriteAudit(connection, transaction, actor, pinId, "planner.plan.unpin", "productivity.PlannerPin", traceId);
            CompleteReceipt(connection, transaction, receipt, "PlannerUnpinned");
            transaction.Commit();
            return IdentityOperationResult<object?>.Success(null, 204, "PlannerUnpinned");
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<object?>());
        }
    }

    private bool HasCapabilities(IdentityPrincipal actor, params string[] actions) =>
        actions.All(action => _capabilities.IsAllowed(actor, "FX15", action));

    private bool TaskReadAvailable(IdentityPrincipal actor) =>
        _capabilities.IsAllowed(actor, "FX12", "tasks.task.read");

    private static PlannerPlan ReadPlan(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        DateOnly from, DateOnly to, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT pin.[Id], pin.[TaskId], taskRow.[Title], taskRow.[Status], projectRow.[Name], taskRow.[StartAt], taskRow.[EndAt],
                   pin.[PlanDate], pin.[Rank], pin.[Notes], projectRow.[Status], taskRow.[DeletedAt], projectRow.[DeletedAt],
                   pin.[UpdatedAt], pin.[RowVersion]
            FROM [productivity].[PlannerPin] pin {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [productivity].[Task] taskRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON taskRow.[Id] = pin.[TaskId] AND taskRow.[OwnerId] = pin.[OwnerId]
            INNER JOIN [productivity].[Project] projectRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = pin.[OwnerId]
            WHERE pin.[OwnerId] = @OwnerId AND pin.[PlanDate] >= @From AND pin.[PlanDate] <= @To
            ORDER BY pin.[PlanDate], pin.[Rank], pin.[Id];
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@From", SqlDbType.Date, ToDbDate(from));
        Add(command, "@To", SqlDbType.Date, ToDbDate(to));
        using var reader = command.ExecuteReader();
        var pins = new List<PlannerPinRecord>();
        while (reader.Read()) pins.Add(ReadPin(reader));
        return new PlannerPlan(from, to, pins, PlanETag(pins));
    }

    private static PlannerPinRecord? ReadPin(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        Guid pinId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT pin.[Id], pin.[TaskId], taskRow.[Title], taskRow.[Status], projectRow.[Name], taskRow.[StartAt], taskRow.[EndAt],
                   pin.[PlanDate], pin.[Rank], pin.[Notes], projectRow.[Status], taskRow.[DeletedAt], projectRow.[DeletedAt],
                   pin.[UpdatedAt], pin.[RowVersion]
            FROM [productivity].[PlannerPin] pin {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [productivity].[Task] taskRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON taskRow.[Id] = pin.[TaskId] AND taskRow.[OwnerId] = pin.[OwnerId]
            INNER JOIN [productivity].[Project] projectRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = pin.[OwnerId]
            WHERE pin.[OwnerId] = @OwnerId AND pin.[Id] = @Id;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, pinId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadPin(reader) : null;
    }

    private static PlannerPinRecord? ReadPinByTaskDate(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid taskId, DateOnly planDate, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT TOP (1) pin.[Id], pin.[TaskId], taskRow.[Title], taskRow.[Status], projectRow.[Name], taskRow.[StartAt], taskRow.[EndAt],
                   pin.[PlanDate], pin.[Rank], pin.[Notes], projectRow.[Status], taskRow.[DeletedAt], projectRow.[DeletedAt],
                   pin.[UpdatedAt], pin.[RowVersion]
            FROM [productivity].[PlannerPin] pin {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [productivity].[Task] taskRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON taskRow.[Id] = pin.[TaskId] AND taskRow.[OwnerId] = pin.[OwnerId]
            INNER JOIN [productivity].[Project] projectRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = pin.[OwnerId]
            WHERE pin.[OwnerId] = @OwnerId AND pin.[TaskId] = @TaskId AND pin.[PlanDate] = @PlanDate;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@TaskId", SqlDbType.UniqueIdentifier, taskId);
        Add(command, "@PlanDate", SqlDbType.Date, ToDbDate(planDate));
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadPin(reader) : null;
    }

    private static PlannerTask? ReadTask(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid taskId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT taskRow.[Id], taskRow.[Status], projectRow.[Status], taskRow.[DeletedAt], projectRow.[DeletedAt]
            FROM [productivity].[Task] taskRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [productivity].[Project] projectRow {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
                ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = taskRow.[OwnerId]
            WHERE taskRow.[OwnerId] = @OwnerId AND taskRow.[Id] = @TaskId;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@TaskId", SqlDbType.UniqueIdentifier, taskId);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new PlannerTask(reader.GetGuid(0), PlannerPolicy.IsActiveTask(reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3), reader.IsDBNull(4)))
            : null;
    }

    private static bool TryReadOwnerLocalToday(SqlConnection connection, Guid ownerId, out DateOnly today)
    {
        today = default;
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT userRow.[TimeZoneId]
                FROM [platform].[PersonalSpace] spaceRow
                INNER JOIN [identity].[User] userRow ON userRow.[Id] = spaceRow.[UserId]
                WHERE spaceRow.[Id] = @OwnerId
                  AND spaceRow.[State] = 'Active'
                  AND userRow.[State] = 'Active'
                  AND userRow.[IsDeleted] = 0;
                """;
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
            var timeZoneId = Convert.ToString(command.ExecuteScalar());
            if (!TryResolveZone(timeZoneId, out var zone) || zone is null) return false;
            today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
            return true;
        }
        catch (SqlException) { return false; }
    }

    private static decimal ReadNextRank(SqlConnection connection, SqlTransaction transaction, Guid ownerId, DateOnly planDate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX([Rank]), CONVERT(decimal(28,8), 0)) + CONVERT(decimal(28,8), 1) FROM [productivity].[PlannerPin] WITH (UPDLOCK, HOLDLOCK) WHERE [OwnerId] = @OwnerId AND [PlanDate] = @PlanDate;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@PlanDate", SqlDbType.Date, ToDbDate(planDate));
        return (decimal)(command.ExecuteScalar() ?? 1m);
    }

    private static PlannerPinRecord ReadPin(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        ToOffset(reader.GetDateTime(5)), ToOffset(reader.GetDateTime(6)), DateOnly.FromDateTime(reader.GetDateTime(7)),
        reader.GetDecimal(8), reader.IsDBNull(9) ? null : reader.GetString(9),
        PlannerPolicy.IsActiveTask(reader.GetString(3), reader.GetString(10), reader.IsDBNull(11), reader.IsDBNull(12)),
        ToOffset(reader.GetDateTime(13)), EncodeETag(reader.GetFieldValue<byte[]>(14)));

    private static bool TryValidateNotes(string? value, out string? notes)
    {
        notes = PlannerPolicy.NormalizeNotes(value);
        return value is null || value.Length <= PlannerPolicy.MaximumNotesLength;
    }

    private static bool TryResolveZone(string? value, out TimeZoneInfo? zone)
    {
        zone = null;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 128) return false;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(value.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        return Failure<T>(claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress", 409,
            "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string targetType, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, @TargetType, @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
        ("@ActionKey", SqlDbType.NVarChar, actionKey, 160),
        ("@TargetType", SqlDbType.NVarChar, targetType, 100),
        ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 128));

    private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value, int? Size)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value, parameter.Size);
        return command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is { } explicitSize ? command.Parameters.Add(name, type, explicitSize) : command.Parameters.Add(name, type);
        if (type == SqlDbType.Decimal) { parameter.Precision = 28; parameter.Scale = 8; }
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Rollback<T>(SqlTransaction transaction, IdentityOperationResult<T> result)
    {
        transaction.Rollback();
        return result;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Planner is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Planner pin or source Task is unavailable.");
    private static IdentityOperationResult<T> LifecycleLocked<T>() => Failure<T>("LifecycleLocked", 409, "Planner changes require an active Task in an active Project.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted revision.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Planner revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "Planner persistence is unavailable.");
    private static object ToDbDate(DateOnly value) => value.ToDateTime(TimeOnly.MinValue);
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static bool MatchesETag(byte[] expected, string actual) => CryptographicOperations.FixedTimeEquals(expected, DecodeETag(actual));
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "*") return false;
            bytes = DecodeETag(value);
            return bytes.Length is 8 or 32;
        }
        catch (FormatException) { return false; }
    }
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static string PlanETag(IReadOnlyList<PlannerPinRecord> pins)
    {
        var material = string.Join('|', pins.Select(pin => $"{pin.Id:N}:{pin.ETag}"));
        return EncodeETag(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private sealed record PlannerTask(Guid Id, bool SourceAvailable);
}
