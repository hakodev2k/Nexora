using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    public IdentityOperationResult<GoalDetail> CreateNumericTarget(IdentityPrincipal actor, Guid goalId,
        string? ifMatch, NumericTargetCommand command, string? idempotencyKey = null, string? traceId = null) =>
        WriteLocalTarget(actor, goalId, null, ifMatch, command.Title, false, null, idempotencyKey, traceId, command);

    public IdentityOperationResult<GoalDetail> CreateBooleanTarget(IdentityPrincipal actor, Guid goalId,
        string? ifMatch, BooleanTargetCommand command, string? idempotencyKey = null, string? traceId = null) =>
        WriteLocalTarget(actor, goalId, null, ifMatch, command.Title, command.BooleanValue, null, idempotencyKey, traceId);

    public IdentityOperationResult<GoalDetail> RecordBooleanProgress(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string? ifMatch, BooleanProgressCommand command, string? idempotencyKey = null, string? traceId = null) =>
        WriteLocalTarget(actor, goalId, targetId, ifMatch, null, command.BooleanValue, command.Note, idempotencyKey, traceId);

    private IdentityOperationResult<GoalDetail> WriteLocalTarget(IdentityPrincipal actor, Guid goalId, Guid? targetId,
        string? ifMatch, string? title, bool value, string? note, string? idempotencyKey, string? traceId,
        NumericTargetCommand? numeric = null)
    {
        var creating = !targetId.HasValue;
        var operation = creating ? "goals.target.create" : "goals.target.record_progress";
        var actions = RequiredActions(operation);
        if (!HasAllCapabilities(actor, actions)) return ModuleUnavailable<GoalDetail>();
        title = title?.Trim();
        if (creating && (string.IsNullOrEmpty(title) || title.Length > 200))
            return Failure<GoalDetail>("ValidationFailed", 422, "Target title must be between 1 and 200 characters.");
        if (numeric is not null && ValidateTarget(numeric, out _) is { } validation)
            return Failure<GoalDetail>("ValidationFailed", 422, validation);
        if (note is { Length: > 2000 })
            return Failure<GoalDetail>("ValidationFailed", 422, "Progress note must be at most 2000 characters.");
        if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
            return Failure<GoalDetail>("ValidationFailed", 422, "Idempotency key must be a nonempty UUID.");
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<GoalDetail>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        // Keep the existing Boolean intent exactly compatible with its receipts.
        var intent = numeric is null
            ? JsonSerializer.Serialize(new { version = 2, kind = "Boolean", goalId, targetId, ifMatch, title, value, note = TrimOrNull(note) })
            : JsonSerializer.Serialize(new { version = 2, kind = "Numeric", goalId, ifMatch, title,
                initial = numeric.InitialValue.ToString("G29", CultureInfo.InvariantCulture),
                current = numeric.CurrentValue.ToString("G29", CultureInfo.InvariantCulture),
                target = numeric.TargetValue.ToString("G29", CultureInfo.InvariantCulture) });
        var receiptFailure = CheckReceipt(connection, transaction, actor, operation, idempotencyKey,
            intent, intent, goalId, out var receipt);
        if (receiptFailure is not null) return receiptFailure;
        try
        {
            var goal = ReadGoal(connection, transaction, actor.OwnerId, goalId, forUpdate: true);
            if (goal is null || goal.Status == "Deleted") return Missing<GoalDetail>();
            if (IsGoalTrashed(connection, transaction, actor.OwnerId, goalId)) return Missing<GoalDetail>();
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(goal.ETag)))
                return Revision<GoalDetail>();
            if (goal.Status is not ("Draft" or "Active"))
                return Failure<GoalDetail>("LifecycleLocked", 409, "Reopen the goal before changing a target.");

            var id = targetId ?? Guid.NewGuid();
            if (creating)
            {
                using var positionCommand = connection.CreateCommand();
                positionCommand.Transaction = transaction;
                positionCommand.CommandText = "SELECT COALESCE(MAX(CONVERT(bigint, [Position])), -1) + 1 FROM [productivity].[GoalTarget] WHERE [OwnerId] = @OwnerId AND [GoalId] = @GoalId;";
                Add(positionCommand, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
                Add(positionCommand, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
                var position = (long)positionCommand.ExecuteScalar()!;
                if (position > int.MaxValue)
                    return Failure<GoalDetail>("TargetLimitReached", 409, "No target position is available.");
                if (numeric is not null)
                    Execute(connection, transaction, """
                        INSERT INTO [productivity].[GoalTarget]
                            ([Id], [OwnerId], [GoalId], [CreatedByUserId], [UpdatedByUserId],
                             [Kind], [Title], [InitialValue], [CurrentValue], [TargetValue], [Position])
                        VALUES (@Id, @OwnerId, @GoalId, @UserId, @UserId,
                            'Numeric', @Title, @Initial, @Current, @Target, @Position);
                        """,
                        ("@Id", SqlDbType.UniqueIdentifier, id, null),
                        ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                        ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                        ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                        ("@Title", SqlDbType.NVarChar, title!, 200),
                        ("@Initial", SqlDbType.Decimal, numeric.InitialValue, null),
                        ("@Current", SqlDbType.Decimal, numeric.CurrentValue, null),
                        ("@Target", SqlDbType.Decimal, numeric.TargetValue, null),
                        ("@Position", SqlDbType.Int, (int)position, null));
                else Execute(connection, transaction, """
                    INSERT INTO [productivity].[GoalTarget]
                        ([Id], [OwnerId], [GoalId], [CreatedByUserId], [UpdatedByUserId],
                         [Kind], [Title], [BooleanValue], [Position])
                    VALUES (@Id, @OwnerId, @GoalId, @UserId, @UserId, 'Boolean', @Title, @Value, @Position);
                    """,
                    ("@Id", SqlDbType.UniqueIdentifier, id, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@Title", SqlDbType.NVarChar, title!, 200),
                    ("@Value", SqlDbType.Bit, value, null),
                    ("@Position", SqlDbType.Int, (int)position, null));
            }
            else
            {
                var target = ReadTargetState(connection, transaction, actor.OwnerId, goalId, id, forUpdate: true);
                if (target is null) return Missing<GoalDetail>();
                if (target.Kind != "Boolean")
                    return Failure<GoalDetail>("ValidationFailed", 422, "Only Boolean targets accept Boolean progress.");
                Execute(connection, transaction, """
                    UPDATE [productivity].[GoalTarget]
                    SET [BooleanValue] = @Value, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [OwnerId] = @OwnerId AND [GoalId] = @GoalId AND [Id] = @Id;
                    """,
                    ("@Value", SqlDbType.Bit, value, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                    ("@Id", SqlDbType.UniqueIdentifier, id, null));
                Execute(connection, transaction, """
                    INSERT INTO [productivity].[GoalProgress]
                        ([Id], [OwnerId], [TargetId], [CreatedByUserId], [Checked], [Note])
                    VALUES (@Id, @OwnerId, @TargetId, @UserId, @Value, @Note);
                    """,
                    ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@TargetId", SqlDbType.UniqueIdentifier, id, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@Value", SqlDbType.Bit, value, null),
                    ("@Note", SqlDbType.NVarChar, (object?)TrimOrNull(note) ?? DBNull.Value, 2000));
            }
            Execute(connection, transaction, """
                UPDATE [productivity].[Goal] SET [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [OwnerId] = @OwnerId AND [Id] = @GoalId AND [RowVersion] = @RowVersion;
                """,
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            var detail = ReadDetail(connection, transaction, actor.OwnerId, goalId, forUpdate: false);
            if (detail is null) return Missing<GoalDetail>();
            if(creating)CaptureTargetDefinition(connection,transaction,actor,goalId,id,"Created");
            WriteAudit(connection, transaction, actor, id, operation, "productivity.GoalTarget", traceId);
            return CommitResource(connection, transaction, actor, receipt, detail,
                creating ? numeric is null ? "BooleanTargetCreated" : "NumericTargetCreated" : "GoalProgressRecorded",
                creating ? 201 : 200, actions);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }
}
