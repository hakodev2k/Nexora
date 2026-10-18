using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Goals;

/// <summary>
/// SQL-backed owner-scoped Goals with explicit local Numeric/Boolean progress.
/// Task-linked aggregation, advanced lifecycle, reminders and providers remain
/// separate contracts.
/// </summary>
public sealed partial class SqlGoalService : IGoalService
{
    private const decimal MaximumNumericValue = 99999999999999999999.99999999m;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly GoalProgressCursor _progressCursor;
    private readonly GoalProgressCursor _stateCursor;
    private readonly GoalProgressCursor _metadataCursor;
    private readonly GoalProgressCursor _targetHistoryCursor;
    private readonly GoalTargetPreviewCodec _targetPreview;
    private readonly TimeProvider _clock;

    public SqlGoalService(SqlConnectionFactory connections, string? idempotencySecret = null, TimeProvider? timeProvider = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _progressCursor = new GoalProgressCursor(secret);
        _stateCursor = new GoalProgressCursor(secret,"state-history");
        _metadataCursor = new GoalProgressCursor(secret,"metadata-history");
        _targetHistoryCursor = new GoalProgressCursor(secret,"target-definition-history");
        _targetPreview = new GoalTargetPreviewCodec(secret);
        _clock = timeProvider ?? TimeProvider.System;
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<GoalPage> List(IdentityPrincipal actor, string? status = null,
        string? query = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "goals.goal.read"))
            return ModuleUnavailable<GoalPage>();

        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        if (normalizedStatus is not null && !ValidStatuses.Contains(normalizedStatus))
            return Failure<GoalPage>("ValidationFailed", 422, "Goal status is invalid.");
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (normalizedQuery is { Length: > 100 })
            normalizedQuery = normalizedQuery[..100];
        var take = Math.Clamp(limit ?? 50, 1, 100);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read"))
            return ModuleUnavailable<GoalPage>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit)
                g.[Id], g.[Title], g.[Description], g.[StartDate], g.[EndDate], g.[Status],
                CONVERT(decimal(28,8), COALESCE(AVG(CONVERT(decimal(28,8),
                    CASE
                        WHEN t.[Kind] = 'Numeric' AND t.[TargetValue] > t.[InitialValue]
                            THEN CASE WHEN t.[CurrentValue] <= t.[InitialValue] THEN 0
                                      WHEN t.[CurrentValue] >= t.[TargetValue] THEN 1
                                      ELSE (t.[CurrentValue] - t.[InitialValue]) /
                                           (t.[TargetValue] - t.[InitialValue]) END
                        WHEN t.[Kind] = 'Boolean' AND t.[BooleanValue] = 1 THEN 1
                        ELSE 0
                    END)), 0)),
                CONVERT(int, COUNT_BIG(t.[Id])), g.[CreatedAt], g.[UpdatedAt], g.[RowVersion], g.[PreArchiveStatus]
            FROM [productivity].[Goal] g
            LEFT JOIN [productivity].[GoalTarget] t
                ON t.[OwnerId] = g.[OwnerId] AND t.[GoalId] = g.[Id] AND t.[RemovedAt] IS NULL
            WHERE g.[OwnerId] = @OwnerId
              AND g.[Status] <> 'Deleted'
              AND (@Status IS NULL OR g.[Status] = @Status)
              AND (@Query IS NULL OR g.[Title] LIKE @QueryLike OR g.[Description] LIKE @QueryLike)
            GROUP BY g.[Id], g.[Title], g.[Description], g.[StartDate], g.[EndDate], g.[Status],
                     g.[CreatedAt], g.[UpdatedAt], g.[RowVersion], g.[PreArchiveStatus]
            ORDER BY g.[UpdatedAt] DESC, g.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Status", SqlDbType.VarChar, (object?)normalizedStatus ?? DBNull.Value, 64);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 100);
        Add(command, "@QueryLike", SqlDbType.NVarChar,
            normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 102);
        using var reader = command.ExecuteReader();
        var items = new List<GoalRecord>();
        while (reader.Read())
            items.Add(ReadGoal(reader));
        reader.Close();
        if (!CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read"))
            return ModuleUnavailable<GoalPage>();
        transaction.Commit();
        return IdentityOperationResult<GoalPage>.Success(new GoalPage(items, null));
    }

    public IdentityOperationResult<GoalDetail> Get(IdentityPrincipal actor, Guid goalId)
    {
        if (!HasAllCapabilities(actor, "goals.goal.read", "goals.target.read"))
            return ModuleUnavailable<GoalDetail>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read"))
            return ModuleUnavailable<GoalDetail>();
        var detail = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
        if (detail is not null)
            Nexora.Infrastructure.Discovery.SqlRecentActivity.RecordWithin(connection, transaction, _capabilities, actor,
                ReadRecentSourceWithin(connection, transaction, actor, goalId));
        if (!CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read"))
            return ModuleUnavailable<GoalDetail>();
        transaction.Commit();
        return detail is null
            ? Missing<GoalDetail>()
            : IdentityOperationResult<GoalDetail>.Success(detail);
    }

    public IdentityOperationResult<GoalDetail> Create(IdentityPrincipal actor, GoalCommand command,
        NumericTargetCommand? target = null, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "goals.goal.create"))
            return ModuleUnavailable<GoalDetail>();
        if (target is not null && !ModuleAvailable(actor, "goals.target.create"))
            return ModuleUnavailable<GoalDetail>();
        var validation = ValidateGoal(command, out var title, out var description);
        if (validation is not null)
            return Failure<GoalDetail>("ValidationFailed", 422, validation);
        if (target is not null)
        {
            validation = ValidateTarget(target, out var targetTitle);
            if (validation is not null)
                return Failure<GoalDetail>("ValidationFailed", 422, validation);
            target = target with { Title = targetTitle };
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (target is not null && !CurrentAuthority(connection, transaction, actor, "goals.target.create"))
            return ModuleUnavailable<GoalDetail>();
        var receiptFailure = CheckReceipt(connection, transaction, actor, "goals.goal.create", idempotencyKey,
            GoalIntent(command, target), CanonicalRequest(command, target), null, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        try
        {
            var goalId = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [productivity].[Goal]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Title], [Description], [StartDate], [EndDate], [Status])
                VALUES
                    (@Id, @OwnerId, @UserId, @UserId, @Title, @Description, @StartDate, @EndDate, 'Draft');
                """,
                ("@Id", SqlDbType.UniqueIdentifier, goalId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Title", SqlDbType.NVarChar, title, 200),
                ("@Description", SqlDbType.NVarChar, (object?)description ?? DBNull.Value, -1),
                ("@StartDate", SqlDbType.Date, ToDbDate(command.StartDate), null),
                ("@EndDate", SqlDbType.Date, ToDbDate(command.EndDate), null));

            if (target is not null)
            {
                Execute(connection, transaction, """
                    INSERT INTO [productivity].[GoalTarget]
                        ([Id], [OwnerId], [GoalId], [CreatedByUserId], [UpdatedByUserId], [Kind], [Title],
                         [InitialValue], [CurrentValue], [TargetValue], [Position])
                    VALUES
                        (@Id, @OwnerId, @GoalId, @UserId, @UserId, 'Numeric', @Title,
                         @InitialValue, @CurrentValue, @TargetValue, 0);
                    """,
                    ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@Title", SqlDbType.NVarChar, target.Title, 200),
                    ("@InitialValue", SqlDbType.Decimal, target.InitialValue, null),
                    ("@CurrentValue", SqlDbType.Decimal, target.CurrentValue, null),
                    ("@TargetValue", SqlDbType.Decimal, target.TargetValue, null));
            }

            var created = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
            if (created is null)
            {
                transaction.Rollback();
                return Failure<GoalDetail>("PersistenceFailure", 500, "Goal could not be loaded after creation.");
            }
            CaptureMetadataHistory(connection,transaction,actor,goalId,"Created");
            if(target is not null)foreach(var createdTarget in created.Targets)
                CaptureTargetDefinition(connection,transaction,actor,goalId,createdTarget.Id,"Created");
            WriteAudit(connection, transaction, actor, goalId, "goals.goal.create", "productivity.Goal", traceId);
            return CommitResource(connection, transaction, actor, receipt, created, "GoalCreated", 201,
                target is null ? ["goals.goal.create"] : ["goals.goal.create", "goals.target.create"]);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<GoalDetail>("GoalDuplicate", 409, "A goal target with this position already exists.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }

    public IdentityOperationResult<GoalDetail> Update(IdentityPrincipal actor, Guid goalId, string? ifMatch,
        GoalCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "goals.goal.update"))
            return ModuleUnavailable<GoalDetail>();
        var validation = ValidateGoal(command, out var title, out var description);
        if (validation is not null)
            return Failure<GoalDetail>("ValidationFailed", 422, validation);
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<GoalDetail>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "goals.goal.update", idempotencyKey,
            JsonSerializer.Serialize(new { version = 2, goalId, ifMatch, goal = GoalIntent(command, null) }),
            $"goal:{goalId:N}|etag:{ifMatch}|{CanonicalRequest(command, null)}", goalId, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        try
        {
            var current = ReadGoal(connection, transaction, actor.OwnerId, goalId, forUpdate: true);
            if (current is null || current.Status == "Deleted" || IsGoalTrashed(connection,transaction,actor.OwnerId,goalId))
            {
                transaction.Rollback();
                return Missing<GoalDetail>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<GoalDetail>();
            }
            if (current.Status is "Completed" or "Abandoned" or "Archived")
            {
                transaction.Rollback();
                return Failure<GoalDetail>("LifecycleLocked", 409, "Reopen the goal before editing a terminal goal.");
            }
            CaptureMetadataHistory(connection,transaction,actor,goalId,"HistoryStarted",baseline:true);
            Execute(connection, transaction, """
                UPDATE [productivity].[Goal]
                SET [Title] = @Title, [Description] = @Description, [StartDate] = @StartDate, [EndDate] = @EndDate,
                    [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] <> 'Deleted';
                """,
                ("@Title", SqlDbType.NVarChar, title, 200),
                ("@Description", SqlDbType.NVarChar, (object?)description ?? DBNull.Value, -1),
                ("@StartDate", SqlDbType.Date, ToDbDate(command.StartDate), null),
                ("@EndDate", SqlDbType.Date, ToDbDate(command.EndDate), null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Id", SqlDbType.UniqueIdentifier, goalId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));

            var updated = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
            if (updated is null)
            {
                transaction.Rollback();
                return Failure<GoalDetail>("PersistenceFailure", 500, "Goal could not be loaded after update.");
            }
            CaptureMetadataHistory(connection,transaction,actor,goalId,"Updated");
            WriteAudit(connection, transaction, actor, goalId, "goals.goal.update", "productivity.Goal", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, "GoalUpdated", 200,
                ["goals.goal.update", "goals.goal.read"]);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }

    public IdentityOperationResult<GoalDetail> RecordNumericProgress(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string? ifMatch, NumericProgressCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasAllCapabilities(actor, "goals.target.read", "goals.target.record_progress"))
            return ModuleUnavailable<GoalDetail>();
        if (command.Note is { Length: > 2000 })
            return Failure<GoalDetail>("ValidationFailed", 422, "Progress note must be at most 2000 characters.");
        if (!ValidNumericValue(command.CurrentValue))
            return Failure<GoalDetail>("ValidationFailed", 422, "Progress value must fit decimal(28,8) without rounding.");
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<GoalDetail>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "goals.target.record_progress", idempotencyKey,
            JsonSerializer.Serialize(new { version = 2, goalId, targetId, ifMatch,
                value = command.CurrentValue.ToString("G29", CultureInfo.InvariantCulture), note = command.Note?.Trim() }),
            $"goal:{goalId:N}|target:{targetId:N}|etag:{ifMatch}|value:{command.CurrentValue.ToString(CultureInfo.InvariantCulture)}|note:{command.Note?.Trim()}",
            goalId, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        try
        {
            var goal = ReadGoal(connection, transaction, actor.OwnerId, goalId, forUpdate: true);
            if (goal is null || goal.Status == "Deleted")
            {
                transaction.Rollback();
                return Missing<GoalDetail>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(goal.ETag)))
            {
                transaction.Rollback();
                return Revision<GoalDetail>();
            }
            if (goal.Status is "Completed" or "Abandoned" or "Archived")
            {
                transaction.Rollback();
                return Failure<GoalDetail>("LifecycleLocked", 409, "Reopen the goal before recording progress.");
            }
            var target = ReadTargetState(connection, transaction, actor.OwnerId, goalId, targetId, forUpdate: true);
            if (target is null)
            {
                transaction.Rollback();
                return Missing<GoalDetail>();
            }
            if (target.Kind != "Numeric")
            {
                transaction.Rollback();
                return Failure<GoalDetail>("ValidationFailed", 422, "Only numeric targets accept numeric progress.");
            }

            Execute(connection, transaction, """
                UPDATE [productivity].[GoalTarget]
                SET [CurrentValue] = @CurrentValue, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @TargetId AND [OwnerId] = @OwnerId AND [GoalId] = @GoalId;
                """,
                ("@CurrentValue", SqlDbType.Decimal, command.CurrentValue, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null));
            Execute(connection, transaction, """
                INSERT INTO [productivity].[GoalProgress]
                    ([Id], [OwnerId], [TargetId], [CreatedByUserId], [Value], [Note])
                VALUES (@Id, @OwnerId, @TargetId, @UserId, @Value, @Note);
                """,
                ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Value", SqlDbType.Decimal, command.CurrentValue, null),
                ("@Note", SqlDbType.NVarChar, (object?)TrimOrNull(command.Note) ?? DBNull.Value, 2000));
            Execute(connection, transaction,
                "UPDATE [productivity].[Goal] SET [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @GoalId AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));

            var updated = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
            if (updated is null)
            {
                transaction.Rollback();
                return Failure<GoalDetail>("PersistenceFailure", 500, "Goal could not be loaded after recording progress.");
            }
            WriteAudit(connection, transaction, actor, targetId, "goals.target.record_progress", "productivity.GoalTarget", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, "GoalProgressRecorded", 200,
                ["goals.target.record_progress", "goals.target.read"]);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }

    public IdentityOperationResult<GoalDetail> Transition(IdentityPrincipal actor, Guid goalId, string? ifMatch,
        string status, string? idempotencyKey = null, string? traceId = null)
    {
        var normalizedStatus = status?.Trim() ?? string.Empty;
        if (normalizedStatus is "Archived" or "Unarchive")
            return ArchiveTransition(actor, goalId, ifMatch, normalizedStatus, idempotencyKey, traceId);
        if (normalizedStatus is not ("Active" or "Completed" or "Abandoned"))
            return Failure<GoalDetail>("ValidationFailed", 422, "Goal status must be Active, Completed or Abandoned.");
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<GoalDetail>();

        string[] transitionActions = normalizedStatus switch
        {
            "Active" => ["goals.goal.start", "goals.goal.reopen"],
            "Completed" => ["goals.goal.complete"],
            _ => ["goals.goal.abandon"]
        };
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!transitionActions.Any(action => CurrentAuthority(connection, transaction, actor, action)))
            return ModuleUnavailable<GoalDetail>();
        try
        {
            // Inspect both possible Active namespaces before looking at today's
            // lifecycle or rowversion. A completed start must never become reopen.
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                foreach (var candidate in transitionActions)
                {
                    var previous = _receipts.TryClaim(connection, transaction, actor.UserId, candidate,
                        idempotencyKey, TransitionIntent(goalId, ifMatch, normalizedStatus), DateTime.UtcNow,
                        claimIfMissing: false,
                        legacyCanonicalRequest: $"goal:{goalId:N}|etag:{ifMatch}|status:{normalizedStatus}");
                    if (!previous.IsMissing)
                        return ReceiptOutcome(connection, transaction, actor, candidate, goalId, previous);
                }
            }
            var current = ReadGoal(connection, transaction, actor.OwnerId, goalId, forUpdate: true);
            if (current is null || current.Status == "Deleted")
            {
                transaction.Rollback();
                return Missing<GoalDetail>();
            }
            if (IsGoalTrashed(connection, transaction, actor.OwnerId, goalId)) return Missing<GoalDetail>();
            var action = normalizedStatus switch
            {
                "Completed" => "goals.goal.complete",
                "Abandoned" => "goals.goal.abandon",
                "Active" when current.Status == "Draft" => "goals.goal.start",
                "Active" when current.Status is "Completed" or "Abandoned" => "goals.goal.reopen",
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(action) || !CurrentAuthority(connection, transaction, actor, action))
            {
                transaction.Rollback();
                return string.IsNullOrEmpty(action)
                    ? Failure<GoalDetail>("LifecycleLocked", 409, "The requested Goal status transition is not supported.")
                    : ModuleUnavailable<GoalDetail>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<GoalDetail>();
            }
            if (!IsAllowedTransition(current.Status, normalizedStatus, action))
            {
                transaction.Rollback();
                return Failure<GoalDetail>("LifecycleLocked", 409, "The requested Goal status transition is not supported.");
            }
            var receiptFailure = CheckReceipt(connection, transaction, actor, action, idempotencyKey,
                TransitionIntent(goalId, ifMatch, normalizedStatus),
                $"goal:{goalId:N}|etag:{ifMatch}|status:{normalizedStatus}", goalId, out var receipt);
            if (receiptFailure is not null)
            {
                transaction.Rollback();
                return receiptFailure;
            }
            Execute(connection, transaction, """
                UPDATE [productivity].[Goal]
                SET [Status] = @Status, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """,
                ("@Status", SqlDbType.VarChar, normalizedStatus, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Id", SqlDbType.UniqueIdentifier, goalId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            var updated = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
            if (updated is null)
            {
                transaction.Rollback();
                return Failure<GoalDetail>("PersistenceFailure", 500, "Goal could not be loaded after transition.");
            }
            CaptureStateHistory(connection, transaction, actor, goalId, action, current.Status, normalizedStatus);
            WriteAudit(connection, transaction, actor, goalId, action, "productivity.Goal", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, "GoalTransitioned", 200, [action]);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Draft", "Active", "Completed", "Abandoned", "Archived"
    };

    private bool ModuleAvailable(IdentityPrincipal actor, string actionKey) =>
        _capabilities.IsAllowed(actor, "FX16", actionKey);

    private bool HasAllCapabilities(IdentityPrincipal actor, params string[] actionKeys) =>
        actionKeys.All(key => ModuleAvailable(actor, key));

    private static bool IsAllowedTransition(string current, string next, string action) =>
        (action == "goals.goal.start" && current == "Draft" && next == "Active")
        || (action == "goals.goal.complete" && current == "Active" && next == "Completed")
        || (action == "goals.goal.abandon" && (current is "Draft" or "Active") && next == "Abandoned")
        || (action == "goals.goal.reopen" && (current is "Completed" or "Abandoned") && next == "Active");

    private static string? ValidateGoal(GoalCommand command, out string title, out string? description)
    {
        title = command.Title?.Trim() ?? string.Empty;
        description = TrimOrNull(command.Description);
        if (title.Length is < 1 or > 200)
            return "Goal title is required and must be at most 200 characters.";
        if (command.Description is { Length: > 20000 })
            return "Goal description must be at most 20000 characters.";
        if (command.StartDate is not null && command.EndDate is not null && command.EndDate < command.StartDate)
            return "Goal end date cannot be before its start date.";
        return null;
    }

    private static string? ValidateTarget(NumericTargetCommand command, out string title)
    {
        title = command.Title?.Trim() ?? string.Empty;
        if (title.Length is < 1 or > 200)
            return "Numeric target title is required and must be at most 200 characters.";
        if (command.TargetValue <= command.InitialValue)
            return "Numeric target value must be greater than the initial value.";
        if (!ValidNumericValue(command.InitialValue) || !ValidNumericValue(command.CurrentValue) || !ValidNumericValue(command.TargetValue))
            return "Numeric target values must fit decimal(28,8) without rounding.";
        return null;
    }

    private static bool ValidNumericValue(decimal value) => value >= -MaximumNumericValue && value <= MaximumNumericValue
        && decimal.Round(value, 8) == value;

    private static GoalDetail? ReadDetail(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid goalId, bool forUpdate)
    {
        var goal = ReadGoal(connection, transaction, ownerId, goalId, forUpdate);
        if (goal is null || goal.Status == "Deleted")
            return null;
        return new GoalDetail(goal, ReadTargets(connection, transaction, ownerId, goalId, forUpdate));
    }

    private static List<GoalTargetRecord> ReadTargets(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid goalId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT t.[Id], t.[Kind], t.[Title], t.[InitialValue], t.[CurrentValue], t.[TargetValue],
                   CONVERT(decimal(28,8), CASE
                       WHEN t.[Kind] = 'Numeric' AND t.[TargetValue] > t.[InitialValue]
                           THEN CASE WHEN t.[CurrentValue] <= t.[InitialValue] THEN 0
                                     WHEN t.[CurrentValue] >= t.[TargetValue] THEN 1
                                     ELSE (t.[CurrentValue] - t.[InitialValue]) /
                                          (t.[TargetValue] - t.[InitialValue]) END
                       WHEN t.[Kind] = 'Boolean' AND t.[BooleanValue] = 1 THEN 1
                       ELSE 0 END),
                   t.[UpdatedAt], t.[RowVersion], t.[BooleanValue]
            FROM [productivity].[GoalTarget] t {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE t.[OwnerId] = @OwnerId AND t.[GoalId] = @GoalId AND t.[RemovedAt] IS NULL
            ORDER BY t.[Position], t.[Id];
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        using var reader = command.ExecuteReader();
        var targets = new List<GoalTargetRecord>();
        while (reader.Read())
            targets.Add(ReadTarget(reader));
        return targets;
    }

    private static GoalRecord? ReadGoal(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid goalId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT g.[Id], g.[Title], g.[Description], g.[StartDate], g.[EndDate], g.[Status],
                   CONVERT(decimal(28,8), COALESCE(AVG(CONVERT(decimal(28,8),
                       CASE
                           WHEN t.[Kind] = 'Numeric' AND t.[TargetValue] > t.[InitialValue]
                               THEN CASE WHEN t.[CurrentValue] <= t.[InitialValue] THEN 0
                                         WHEN t.[CurrentValue] >= t.[TargetValue] THEN 1
                                         ELSE (t.[CurrentValue] - t.[InitialValue]) /
                                              (t.[TargetValue] - t.[InitialValue]) END
                           WHEN t.[Kind] = 'Boolean' AND t.[BooleanValue] = 1 THEN 1
                           ELSE 0 END)), 0)),
                   CONVERT(int, COUNT_BIG(t.[Id])), g.[CreatedAt], g.[UpdatedAt], g.[RowVersion], g.[PreArchiveStatus]
            FROM [productivity].[Goal] g {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            LEFT JOIN [productivity].[GoalTarget] t
                ON t.[OwnerId] = g.[OwnerId] AND t.[GoalId] = g.[Id] AND t.[RemovedAt] IS NULL
            WHERE g.[OwnerId] = @OwnerId AND g.[Id] = @GoalId
            GROUP BY g.[Id], g.[Title], g.[Description], g.[StartDate], g.[EndDate], g.[Status],
                     g.[CreatedAt], g.[UpdatedAt], g.[RowVersion], g.[PreArchiveStatus];
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadGoal(reader) : null;
    }

    private static TargetState? ReadTargetState(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, Guid goalId, Guid targetId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [GoalId], [Kind], [InitialValue], [CurrentValue], [TargetValue], [RowVersion]
            FROM [productivity].[GoalTarget] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [GoalId] = @GoalId AND [Id] = @TargetId AND [RemovedAt] IS NULL;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        Add(command, "@TargetId", SqlDbType.UniqueIdentifier, targetId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new TargetState(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetDecimal(3),
            reader.IsDBNull(4) ? null : reader.GetDecimal(4),
            reader.IsDBNull(5) ? null : reader.GetDecimal(5),
            EncodeETag(reader.GetFieldValue<byte[]>(6)));
    }

    private static GoalRecord ReadGoal(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
        reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
        reader.IsDBNull(4) ? null : DateOnly.FromDateTime(reader.GetDateTime(4)), reader.GetString(5),
        reader.IsDBNull(6) ? 0m : reader.GetDecimal(6), reader.GetInt32(7),
        ToOffset(reader.GetDateTime(8)), ToOffset(reader.GetDateTime(9)),
        EncodeETag(reader.GetFieldValue<byte[]>(10)), reader.IsDBNull(11) ? null : reader.GetString(11));

    private static GoalTargetRecord ReadTarget(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetDecimal(3),
        reader.IsDBNull(4) ? null : reader.GetDecimal(4),
        reader.IsDBNull(5) ? null : reader.GetDecimal(5),
        reader.IsDBNull(6) ? 0m : reader.GetDecimal(6), ToOffset(reader.GetDateTime(7)),
        EncodeETag(reader.GetFieldValue<byte[]>(8)), reader.IsDBNull(9) ? null : reader.GetBoolean(9));

    private static string CanonicalRequest(GoalCommand command, NumericTargetCommand? target)
    {
        var value = $"title:{command.Title.Trim()}|description:{command.Description?.Trim()}|start:{command.StartDate:yyyy-MM-dd}|end:{command.EndDate:yyyy-MM-dd}";
        return target is null
            ? value
            : $"{value}|target:{target.Title.Trim()}|initial:{target.InitialValue.ToString(CultureInfo.InvariantCulture)}|current:{target.CurrentValue.ToString(CultureInfo.InvariantCulture)}|targetValue:{target.TargetValue.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string GoalIntent(GoalCommand command, NumericTargetCommand? target) =>
        JsonSerializer.Serialize(new { version = 2, title = command.Title.Trim(),
            description = TrimOrNull(command.Description), command.StartDate, command.EndDate,
            target = target is null ? null : new { title = target.Title.Trim(),
                initial = target.InitialValue.ToString("G29", CultureInfo.InvariantCulture),
                current = target.CurrentValue.ToString("G29", CultureInfo.InvariantCulture),
                value = target.TargetValue.ToString("G29", CultureInfo.InvariantCulture) } });

    private static string TransitionIntent(Guid goalId, string? ifMatch, string status) =>
        JsonSerializer.Serialize(new { version = 2, goalId, ifMatch, status });

    private static object ToDbDate(DateOnly? date) => date is null ? DBNull.Value : date.Value.ToDateTime(TimeOnly.MinValue);
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IdentityOperationResult<GoalDetail>? CheckReceipt(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        string legacyCanonicalRequest, Guid? goalId, out ReceiptClaim claim)
    {
        claim = default;
        var requiredActions = RequiredActions(operationKey);
        if (!CurrentAuthority(connection, transaction, actor, requiredActions))
            return ModuleUnavailable<GoalDetail>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey,
            canonicalRequest, DateTime.UtcNow, legacyCanonicalRequest: legacyCanonicalRequest);
        return claim.IsClaimed ? null : ReceiptOutcome(connection, transaction, actor, operationKey, goalId, claim);
    }

    private static string[] RequiredActions(string operationKey) => operationKey switch
        {
            "goals.goal.update" => new[] { operationKey, "goals.goal.read" },
            "goals.target.record_progress" => new[] { operationKey, "goals.target.read" },
            "goals.target.create" => new[] { operationKey, "goals.goal.read", "goals.target.read" },
            "goals.target.update" or "goals.target.remove" => new[] { operationKey, "goals.goal.read", "goals.target.read" },
            _ => new[] { operationKey }
        };

    private IdentityOperationResult<GoalDetail> ReceiptOutcome(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operation, Guid? goalId, ReceiptClaim claim)
    {
        if (!CurrentAuthority(connection, transaction, actor, RequiredActions(operation)))
            return ModuleUnavailable<GoalDetail>();
        if (claim.IsInvalid) return Failure<GoalDetail>("ValidationFailed", 422, "Idempotency key must be a UUID.");
        if (claim.IsConflict) return Failure<GoalDetail>("IdempotencyConflict", 409, "The key belongs to another request.");
        if (!claim.IsReplay) return Failure<GoalDetail>("RequestInProgress", 409, "The request is in progress.");

        if (!string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            GoalReceipt? stored;
            try { stored = JsonSerializer.Deserialize<GoalReceipt>(claim.ResultJson); }
            catch (JsonException) { stored = null; }
            if (stored is null || stored.Version != 2 || stored.UserId != actor.UserId
                || stored.OwnerId != actor.OwnerId || stored.GoalId == Guid.Empty
                || (goalId.HasValue && goalId.Value != stored.GoalId) || stored.Operation != operation
                || stored.Actions is null || !stored.Actions.SequenceEqual(operation == "goals.goal.create"
                    && stored.Actions.Contains("goals.target.create")
                    ? new[] { operation, "goals.target.create" } : RequiredActions(operation))
                || stored.Code != claim.ResultCode || stored.Status != claim.ResultStatusCode
                || !ValidOutcome(operation, stored.Code, stored.Status))
                return OutcomeUnavailable();
            if (!CurrentAuthority(connection, transaction, actor, stored.Actions))
                return ModuleUnavailable<GoalDetail>();
            // Receipt intentionally stores only an opaque outcome. Never rebuild
            // a historical response from a newer Goal/Target projection.
            return IdentityOperationResult<GoalDetail>.Committed(stored.GoalId, stored.Status, stored.Code);
        }

        // Old delimiter-based metadata payloads are ambiguous; generated creates
        // also have no trustworthy resource ID. Only typed progress/transitions
        // can acknowledge a matching legacy digest without guessing current state.
        if (goalId is { } id && operation != "goals.goal.update" && operation != "goals.goal.create"
            && ValidOutcome(operation, claim.ResultCode, 200))
            return IdentityOperationResult<GoalDetail>.Committed(id, 200, claim.ResultCode!);
        return OutcomeUnavailable();
    }

    private static bool ValidOutcome(string operation, string? code, int status) => operation switch
    {
        "goals.goal.create" => code == "GoalCreated" && status == 201,
        "goals.goal.update" => code == "GoalUpdated" && status == 200,
        "goals.target.record_progress" => code == "GoalProgressRecorded" && status == 200,
        "goals.target.create" => code is "BooleanTargetCreated" or "NumericTargetCreated" && status == 201,
        "goals.target.update" => code == "GoalTargetUpdated" && status == 200,
        "goals.target.remove" => code == "GoalTargetRemoved" && status == 200,
        "goals.goal.start" or "goals.goal.reopen" or "goals.goal.complete" or "goals.goal.abandon"
            or "goals.goal.archive" or "goals.goal.unarchive"
            => code == "GoalTransitioned" && status == 200,
        _ => false
    };

    private static IdentityOperationResult<GoalDetail> OutcomeUnavailable() =>
        Failure<GoalDetail>("CommittedOutcomeUnavailable", 409,
            "The request completed. Reconcile its outcome before starting another request.");

    private sealed record GoalReceipt(int Version, Guid UserId, Guid OwnerId, Guid GoalId,
        string Operation, string[] Actions, int Status, string Code);

    private bool CurrentAuthority(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX16", actions);

    private IdentityOperationResult<GoalDetail> CommitResource(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, ReceiptClaim claim, GoalDetail detail, string code, int status, string[] actions)
    {
        _receipts.Complete(connection, transaction, claim, code, status,
            JsonSerializer.Serialize(new GoalReceipt(2, actor.UserId, actor.OwnerId, detail.Goal.Id,
                claim.IsClaimed ? claim.OperationKey : actions[0], actions, status, code)));
        if (!CurrentAuthority(connection, transaction, actor, actions))
        { transaction.Rollback(); return ModuleUnavailable<GoalDetail>(); }
        var canRead = CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read");
        // Audit/receipt triggers can change business rows as well as authority.
        // Project only the state visible after those writes, under this transaction.
        var projection = canRead ? ReadDetail(connection, transaction, actor.OwnerId, detail.Goal.Id, false) : null;
        canRead = projection is not null
            && CurrentAuthority(connection, transaction, actor, "goals.goal.read", "goals.target.read");
        if (!CurrentAuthority(connection, transaction, actor, actions))
        { transaction.Rollback(); return ModuleUnavailable<GoalDetail>(); }
        transaction.Commit();
        return canRead ? IdentityOperationResult<GoalDetail>.Success(projection!, status, code)
            : IdentityOperationResult<GoalDetail>.Committed(detail.Goal.Id, status, code);
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string targetType, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, @TargetType, @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@ActionKey", SqlDbType.NVarChar, actionKey, null),
        ("@TargetType", SqlDbType.NVarChar, targetType, null),
        ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, null));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value, int? Size)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
            Add(command, parameter.Name, parameter.Type, parameter.Value, parameter.Size);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is null ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size.Value);
        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 28;
            parameter.Scale = 8;
        }
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() =>
        Failure<T>("ModuleUnavailable", 409, "Goals are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() =>
        Failure<T>("ResourceUnavailable", 404, "Goal unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() =>
        Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() =>
        Failure<T>("RevisionConflict", 412, "Goal revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) =>
        Failure<T>("PersistenceUnavailable", 503, "Goal persistence is unavailable.");
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var token = value.Trim();
            if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
            bytes = Convert.FromBase64String(token[1..^1]);
            return bytes.Length == 8 && Convert.ToBase64String(bytes) == token[1..^1];
        }
        catch (FormatException) { return false; }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private sealed record TargetState(Guid Id, Guid GoalId, string Kind, decimal? InitialValue,
        decimal? CurrentValue, decimal? TargetValue, string ETag);
}
