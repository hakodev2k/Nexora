using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    private IdentityOperationResult<GoalDetail> ArchiveTransition(IdentityPrincipal actor, Guid goalId,
        string? ifMatch, string request, string? idempotencyKey, string? traceId)
    {
        var archiving = request == "Archived";
        var action = archiving ? "goals.goal.archive" : "goals.goal.unarchive";
        if (!ModuleAvailable(actor, action)) return ModuleUnavailable<GoalDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<GoalDetail>();
        if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty)
            return Failure<GoalDetail>("ValidationFailed", 422, "A nonempty UUID idempotency key is required.");
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var intent = JsonSerializer.Serialize(new { version = 2, goalId, ifMatch, status = request });
        var receiptFailure = CheckReceipt(connection, transaction, actor, action, idempotencyKey,
            intent, intent, goalId, out var receipt);
        if (receiptFailure is not null) return receiptFailure;
        try
        {
            var goal = ReadGoal(connection, transaction, actor.OwnerId, goalId, true);
            if (goal is null || goal.Status == "Deleted" || IsGoalTrashed(connection, transaction, actor.OwnerId, goalId))
                return Missing<GoalDetail>();
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(goal.ETag))) return Revision<GoalDetail>();
            string next;
            if (archiving)
            {
                if (goal.Status is not ("Draft" or "Active" or "Completed" or "Abandoned"))
                    return Failure<GoalDetail>("LifecycleLocked", 409, "This Goal cannot be archived from its current state.");
                next = "Archived";
            }
            else
            {
                if (goal.Status != "Archived")
                    return Failure<GoalDetail>("LifecycleLocked", 409, "Only an archived Goal can be unarchived.");
                using var prior = connection.CreateCommand(); prior.Transaction = transaction;
                prior.CommandText = "SELECT [PreArchiveStatus] FROM [productivity].[Goal] WHERE [OwnerId] = @OwnerId AND [Id] = @GoalId;";
                Add(prior, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
                Add(prior, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
                next = prior.ExecuteScalar() as string ?? string.Empty;
                if (next is not ("Draft" or "Active" or "Completed" or "Abandoned"))
                    return Failure<GoalDetail>("PriorStateUnavailable", 409, "The prior Goal state is unavailable. No replacement state was assumed.");
            }
            Execute(connection, transaction, """
                UPDATE [productivity].[Goal]
                SET [Status] = @Status, [PreArchiveStatus] = @Prior,
                    [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId
                WHERE [OwnerId] = @OwnerId AND [Id] = @GoalId AND [RowVersion] = @Version;
                """,
                ("@Status", SqlDbType.VarChar, next, 64),
                ("@Prior", SqlDbType.VarChar, archiving ? goal.Status : DBNull.Value, 64),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                ("@Version", SqlDbType.Binary, expectedVersion, 8));
            var detail = ReadDetail(connection, transaction, actor.OwnerId, goalId, false);
            if (detail is null) return Missing<GoalDetail>();
            CaptureStateHistory(connection, transaction, actor, goalId, action, goal.Status, next);
            WriteAudit(connection, transaction, actor, goalId, action, "productivity.Goal", traceId);
            return CommitResource(connection, transaction, actor, receipt, detail, "GoalTransitioned", 200, [action]);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }
}
