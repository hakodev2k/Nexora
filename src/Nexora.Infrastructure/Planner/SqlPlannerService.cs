using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Infrastructure.Productivity;
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
    private readonly SqlPlannerTaskSource _sources;

    public SqlPlannerService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
        _sources = new SqlPlannerTaskSource(_capabilities);
    }

    public IdentityOperationResult<PlannerPlan> List(IdentityPrincipal actor, DateOnly? from = null, DateOnly? to = null)
    {
        if (!HasCapabilities(actor, "planner.plan.read"))
            return ModuleUnavailable<PlannerPlan>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, "planner.plan.read")) return ModuleUnavailable<PlannerPlan>();
        DateOnly rangeFrom;
        DateOnly rangeTo;
        if (from is null && to is null)
        {
            if (!TryReadOwnerLocalToday(connection, transaction, actor.OwnerId, out var ownerToday))
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
            var plan = ReadPlan(connection, transaction, actor, rangeFrom, rangeTo, false);
            if (!CurrentAuthority(connection, transaction, actor, "planner.plan.read")) return ModuleUnavailable<PlannerPlan>();
            transaction.Commit();
            return IdentityOperationResult<PlannerPlan>.Success(plan);
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
            Intent(new { command.TaskId, command.PlanDate, notes }),
            $"task:{command.TaskId:N}|date:{command.PlanDate:yyyy-MM-dd}|notes:{notes}", null, ["planner.plan.pin"], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            if (!LockOwner(connection, transaction, actor)) return Rollback(transaction, Busy<PlannerPinRecord>());
            var source = _sources.Read(connection, transaction, actor, command.TaskId);
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

            var created = ReadPin(connection, transaction, actor, pinId, false);
            if (created is null) return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
            WriteAudit(connection, transaction, actor, pinId, "planner.plan.pin", "productivity.PlannerPin", traceId);
            return CommitPin(connection, transaction, actor, receipt, created, "planner.plan.pin", ["planner.plan.pin"], "PlannerPinned", 201);
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
        var canonical = Intent(new { pinId, ifMatch, command.PlanDate, notes });
        var legacy = $"pin:{pinId:N}|etag:{ifMatch}|date:{command.PlanDate:yyyy-MM-dd}|notes:{notes}";
        foreach (var candidate in new[] { "planner.plan.notes", "planner.plan.reschedule" })
        {
            var previous = InspectReceipt(connection, transaction, actor, candidate, idempotencyKey, canonical, legacy);
            if (previous.HasValue) return Rollback(transaction, ReceiptOutcome<PlannerPinRecord>(connection, transaction, actor, candidate, pinId, previous.Value));
        }
        if ((!CurrentAuthority(connection, transaction, actor, "planner.plan.notes")
            && !CurrentAuthority(connection, transaction, actor, "planner.plan.reschedule")) || !_sources.CanRead(connection, transaction, actor))
            return Rollback(transaction, ModuleUnavailable<PlannerPinRecord>());
        if (!LockOwner(connection, transaction, actor)) return Rollback(transaction, Busy<PlannerPinRecord>());
        var current = ReadPin(connection, transaction, actor, pinId, true);
        if (current is null) return Rollback(transaction, Missing<PlannerPinRecord>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<PlannerPinRecord>());
        var action = current.PlanDate == command.PlanDate ? "planner.plan.notes" : "planner.plan.reschedule";
        var actions = current.PlanDate != command.PlanDate && !string.Equals(current.Notes, notes, StringComparison.Ordinal)
            ? new[] { "planner.plan.reschedule", "planner.plan.notes" } : new[] { action };
        if (!CurrentAuthority(connection, transaction, actor, actions)) return Rollback(transaction, ModuleUnavailable<PlannerPinRecord>());
        if (!current.SourceAvailable) return Rollback(transaction, LifecycleLocked<PlannerPinRecord>());
        var receiptFailure = CheckReceipt<PlannerPinRecord>(connection, transaction, actor, action, idempotencyKey,
            canonical, legacy, pinId, actions, out var receipt);
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
            var updated = ReadPin(connection, transaction, actor, pinId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<PlannerPinRecord>());
            WriteAudit(connection, transaction, actor, pinId, action, "productivity.PlannerPin", traceId);
            return CommitPin(connection, transaction, actor, receipt, updated, action, actions, "PlannerPinUpdated", 200);
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
        if (!TryETag(ifMatch, out var expectedPlanVersion, 32)) return Precondition<PlannerPlan>();
        if (command.PinIds.Count is < 1 or > 100 || command.PinIds.Distinct().Count() != command.PinIds.Count)
            return Failure<PlannerPlan>("ValidationFailed", 422, "Planner reorder must contain distinct pins for one day.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<PlannerPlan>(connection, transaction, actor, "planner.plan.reorder", idempotencyKey,
            Intent(new { command.PlanDate, ifMatch, command.PinIds }),
            $"date:{command.PlanDate:yyyy-MM-dd}|etag:{ifMatch}|pins:{string.Join(',', command.PinIds)}",
            command.PinIds[0], ["planner.plan.reorder"], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);
        if (!LockOwner(connection, transaction, actor)) return Rollback(transaction, Busy<PlannerPlan>());
        var current = ReadPlan(connection, transaction, actor, command.PlanDate, command.PlanDate, true);
        if (!MatchesETag(expectedPlanVersion, current.ETag)) return Rollback(transaction, Revision<PlannerPlan>());
        if (current.Pins.Count != command.PinIds.Count || !current.Pins.Select(pin => pin.Id).Order().SequenceEqual(command.PinIds.Order()))
            return Rollback(transaction, Failure<PlannerPlan>("PlannerReorderConflict", 409, "The submitted pins do not match the current plan day."));
        if (current.Pins.Any(pin => !pin.SourceAvailable)) return Rollback(transaction, LifecycleLocked<PlannerPlan>());

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
            WriteAudit(connection, transaction, actor, command.PinIds[0], "planner.plan.reorder", "productivity.PlannerPin", traceId);
            CompleteReceipt(connection, transaction, actor, receipt, "planner.plan.reorder", command.PinIds[0], ["planner.plan.reorder"], "PlannerReordered", 200);
            var reordered = ReadPlan(connection, transaction, actor, command.PlanDate, command.PlanDate, false);
            if (!CurrentAuthority(connection, transaction, actor, "planner.plan.reorder")
                || !_sources.CanRead(connection, transaction, actor) || reordered.Pins.Any(pin => !pin.SourceAvailable)
                || !reordered.Pins.Select(pin => pin.Id).Order().SequenceEqual(command.PinIds.Order()))
                return Rollback(transaction, ModuleUnavailable<PlannerPlan>());
            var canRead = CurrentAuthority(connection, transaction, actor, "planner.plan.read");
            transaction.Commit();
            return canRead ? IdentityOperationResult<PlannerPlan>.Success(reordered, 200, "PlannerReordered")
                : IdentityOperationResult<PlannerPlan>.Committed(command.PinIds[0], 200, "PlannerReordered");
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
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "planner.plan.unpin", idempotencyKey,
            Intent(new { pinId, ifMatch }), $"pin:{pinId:N}|etag:{ifMatch}", pinId, ["planner.plan.unpin"], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);
        if (!LockOwner(connection, transaction, actor)) return Rollback(transaction, Busy<object?>());
        var current = ReadPin(connection, transaction, actor, pinId, true, includeSource: false);
        if (current is null) return Rollback(transaction, Missing<object?>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<object?>());

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
            CompleteReceipt(connection, transaction, actor, receipt, "planner.plan.unpin", pinId, ["planner.plan.unpin"], "PlannerUnpinned", 204);
            if (!CurrentAuthority(connection, transaction, actor, "planner.plan.unpin"))
                return Rollback(transaction, ModuleUnavailable<object?>());
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

    private PlannerPlan ReadPlan(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        DateOnly from, DateOnly to, bool forUpdate)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id],[TaskId],[PlanDate],[Rank],[Notes],[UpdatedAt],[RowVersion]
            FROM [productivity].[PlannerPin] {(forUpdate ? "WITH (UPDLOCK, HOLDLOCK)" : string.Empty)}
            WHERE [OwnerId]=@OwnerId AND [PlanDate]>=@From AND [PlanDate]<=@To
            ORDER BY [PlanDate],[Rank],[Id];
            """;
        Add(command,"@OwnerId",SqlDbType.UniqueIdentifier,actor.OwnerId);
        Add(command,"@From",SqlDbType.Date,ToDbDate(from)); Add(command,"@To",SqlDbType.Date,ToDbDate(to));
        var pins = new List<PlannerPinRecord>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) pins.Add(ReadPin(reader));
        var projected = pins.Select(pin => ProjectPin(connection, transaction, actor, pin)).ToArray();
        return new PlannerPlan(from,to,projected,PlanETag(projected));
    }

    private PlannerPinRecord? ReadPin(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid pinId, bool forUpdate, bool includeSource = true)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id],[TaskId],[PlanDate],[Rank],[Notes],[UpdatedAt],[RowVersion]
            FROM [productivity].[PlannerPin] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId]=@OwnerId AND [Id]=@Id;
            """;
        Add(command,"@OwnerId",SqlDbType.UniqueIdentifier,actor.OwnerId); Add(command,"@Id",SqlDbType.UniqueIdentifier,pinId);
        PlannerPinRecord? pin;
        using (var reader = command.ExecuteReader()) pin = reader.Read() ? ReadPin(reader) : null;
        return pin is null || !includeSource ? pin : ProjectPin(connection, transaction, actor, pin);
    }

    private static PlannerPinRecord? ReadPinByTaskDate(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid taskId, DateOnly planDate, bool forUpdate)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"""
            SELECT TOP(1) [Id],[TaskId],[PlanDate],[Rank],[Notes],[UpdatedAt],[RowVersion]
            FROM [productivity].[PlannerPin] {(forUpdate ? "WITH (UPDLOCK, HOLDLOCK)" : string.Empty)}
            WHERE [OwnerId]=@OwnerId AND [TaskId]=@TaskId AND [PlanDate]=@PlanDate;
            """;
        Add(command,"@OwnerId",SqlDbType.UniqueIdentifier,ownerId); Add(command,"@TaskId",SqlDbType.UniqueIdentifier,taskId);
        Add(command,"@PlanDate",SqlDbType.Date,ToDbDate(planDate));
        using var reader = command.ExecuteReader(); return reader.Read() ? ReadPin(reader) : null;
    }

    private PlannerPinRecord ProjectPin(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, PlannerPinRecord pin)
    {
        var source = _sources.Read(connection, transaction, actor, pin.TaskId);
        return pin with { TaskTitle=source?.Title, TaskStatus=source?.Status, ProjectName=source?.ProjectName,
            StartAt=source?.StartAt, EndAt=source?.EndAt, SourceAvailable=source?.SourceAvailable ?? false };
    }
    private static bool TryReadOwnerLocalToday(SqlConnection connection, SqlTransaction transaction, Guid ownerId, out DateOnly today)
    {
        today = default;
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
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
        reader.GetGuid(0), reader.GetGuid(1), null, null, null, null, null,
        DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetDecimal(3), reader.IsDBNull(4) ? null : reader.GetString(4),
        false, ToOffset(reader.GetDateTime(5)), EncodeETag(reader.GetFieldValue<byte[]>(6)));
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

    private static string Intent(object command) => JsonSerializer.Serialize(new { version = 2, command });

    private bool CurrentAuthority(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX15", actions);

    private static bool LockOwner(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor)
    {
        // Serialize only this owner's Planner writers, including empty days.
        // Source mutations never acquire this lock or write Planner metadata.
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@Resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; SELECT @result;";
        Add(command, "@Resource", SqlDbType.NVarChar, $"planner:{actor.OwnerId:N}", 255);
        return Convert.ToInt32(command.ExecuteScalar()) >= 0;
    }

    private ReceiptClaim? InspectReceipt(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operation, string? key, string canonical, string legacy)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var claim = _receipts.TryClaim(connection, transaction, actor.UserId, operation, key, canonical, DateTime.UtcNow,
            claimIfMissing: false, legacyCanonicalRequest: legacy);
        return claim.IsMissing ? null : claim;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        string legacyCanonicalRequest, Guid? id, string[] actions, out ReceiptClaim claim)
    {
        claim = default;
        if (!CurrentAuthority(connection, transaction, actor, actions)
            || (operationKey != "planner.plan.unpin" && !_sources.CanRead(connection, transaction, actor)))
            return ModuleUnavailable<T>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest,
            DateTime.UtcNow, legacyCanonicalRequest: legacyCanonicalRequest);
        if (claim.IsClaimed) return null;
        return ReceiptOutcome<T>(connection, transaction, actor, operationKey, id, claim);
    }

    private IdentityOperationResult<T> ReceiptOutcome<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operation, Guid? id, ReceiptClaim claim)
    {
        if (!CurrentAuthority(connection, transaction, actor, operation)
            || (operation != "planner.plan.unpin" && !_sources.CanRead(connection, transaction, actor))) return ModuleUnavailable<T>();
        if (claim.IsInvalid) return Failure<T>("ValidationFailed", 422, "Idempotency key must be a UUID.");
        if (claim.IsConflict) return Failure<T>("IdempotencyConflict", 409, "The key belongs to another request.");
        if (!claim.IsReplay) return Busy<T>();
        PlannerReceipt? outcome = null;
        if (!string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            try { outcome = JsonSerializer.Deserialize<PlannerReceipt>(claim.ResultJson); }
            catch (JsonException) { }
        }
        else if (id.HasValue && operation is "planner.plan.reorder" or "planner.plan.unpin")
            outcome = new PlannerReceipt(2, actor.UserId, actor.OwnerId, id.Value, operation, [operation],
                operation == "planner.plan.unpin" ? 204 : 200, claim.ResultCode ?? string.Empty);
        if (outcome is null || outcome.Version != 2 || outcome.UserId != actor.UserId || outcome.OwnerId != actor.OwnerId
            || outcome.Id == Guid.Empty || (id.HasValue && outcome.Id != id.Value) || outcome.Operation != operation
            || outcome.Code != claim.ResultCode || (claim.ResultStatusCode.HasValue && outcome.Status != claim.ResultStatusCode.Value)
            || !ValidOutcome(outcome))
            return Failure<T>("CommittedOutcomeUnavailable", 409, "The request completed. Reconcile its outcome before starting another request.");
        if (!CurrentAuthority(connection, transaction, actor, outcome.Actions)) return ModuleUnavailable<T>();
        return outcome.Status == 204 ? IdentityOperationResult<T>.Success(default!, 204, outcome.Code)
            : IdentityOperationResult<T>.Committed(outcome.Id, outcome.Status, outcome.Code);
    }

    private static bool ValidOutcome(PlannerReceipt value)
    {
        if (value.Actions is null || !(value.Actions.SequenceEqual(new[] { value.Operation })
            || (value.Operation == "planner.plan.reschedule" && value.Actions.SequenceEqual(new[] { value.Operation, "planner.plan.notes" })))) return false;
        return (value.Operation, value.Code, value.Status) is
            ("planner.plan.pin", "PlannerPinned", 201) or
            ("planner.plan.notes" or "planner.plan.reschedule", "PlannerPinUpdated", 200) or
            ("planner.plan.reorder", "PlannerReordered", 200) or ("planner.plan.unpin", "PlannerUnpinned", 204);
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        ReceiptClaim claim, string operation, Guid id, string[] actions, string code, int status) =>
        _receipts.Complete(connection, transaction, claim, code, status,
            JsonSerializer.Serialize(new PlannerReceipt(2, actor.UserId, actor.OwnerId, id, operation, actions, status, code)));

    private IdentityOperationResult<PlannerPinRecord> CommitPin(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, ReceiptClaim claim, PlannerPinRecord pin, string operation, string[] actions, string code, int status)
    {
        CompleteReceipt(connection, transaction, actor, claim, operation, pin.Id, actions, code, status);
        var current = ReadPin(connection, transaction, actor, pin.Id, false);
        if (!CurrentAuthority(connection, transaction, actor, actions) || !_sources.CanRead(connection, transaction, actor)
            || current is null || !current.SourceAvailable) return Rollback(transaction, ModuleUnavailable<PlannerPinRecord>());
        var canRead = CurrentAuthority(connection, transaction, actor, "planner.plan.read");
        transaction.Commit();
        return canRead ? IdentityOperationResult<PlannerPinRecord>.Success(current, status, code)
            : IdentityOperationResult<PlannerPinRecord>.Committed(pin.Id, status, code);
    }

    private sealed record PlannerReceipt(int Version, Guid UserId, Guid OwnerId, Guid Id, string Operation,
        string[] Actions, int Status, string Code);

    private static IdentityOperationResult<T> Busy<T>() => Failure<T>("RequestInProgress", 409, "The request is in progress.");

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string targetType, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, @TargetType, @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
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
    private static bool TryETag(string? value, out byte[] bytes, int size = 8)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var token = value.Trim();
            if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
            bytes = Convert.FromBase64String(token[1..^1]);
            return bytes.Length == size && Convert.ToBase64String(bytes) == token[1..^1];
        }
        catch (FormatException) { return false; }
    }
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static string PlanETag(IReadOnlyList<PlannerPinRecord> pins)
    {
        var material = string.Join('|', pins.Select(pin => $"{pin.Id:N}:{pin.ETag}"));
        return EncodeETag(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

}
