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
    public IdentityOperationResult<GoalTargetChangePreview> PreviewTargetChange(IdentityPrincipal actor,
        Guid goalId, Guid targetId, string? ifMatch, GoalTargetChange command)
    {
        var operation = TargetChangeAction(command.Mode);
        if (operation is null) return Failure<GoalTargetChangePreview>("ValidationFailed", 422, "Target change mode must be Update or Remove.");
        var actions = RequiredActions(operation);
        if (!HasAllCapabilities(actor, actions)) return ModuleUnavailable<GoalTargetChangePreview>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<GoalTargetChangePreview>();
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, actions)) return ModuleUnavailable<GoalTargetChangePreview>();
        var detail = ReadDetail(connection, transaction, actor.OwnerId, goalId, false);
        if (detail is null || IsGoalTrashed(connection, transaction, actor.OwnerId, goalId)) return Missing<GoalTargetChangePreview>();
        if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(detail.Goal.ETag))) return Revision<GoalTargetChangePreview>();
        if (detail.Goal.Status is not ("Draft" or "Active"))
            return Failure<GoalTargetChangePreview>("LifecycleLocked", 409, "Reopen the goal before changing a target.");
        var target = detail.Targets.FirstOrDefault(item => item.Id == targetId);
        if (target is null) return Missing<GoalTargetChangePreview>();
        var validation = ValidateTargetChange(command, target, out var title);
        if (validation is not null) return Failure<GoalTargetChangePreview>("ValidationFailed", 422, validation);
        var removing = command.Mode == "Remove";
        var afterCount = detail.Targets.Count - (removing ? 1 : 0);
        var nextProgress = removing ? 0m : target.Progress;
        if (!removing && target.Kind == "Numeric")
        {
            using var calculation = connection.CreateCommand(); calculation.Transaction = transaction;
            calculation.CommandText = """
                SELECT CONVERT(decimal(28,8), CASE WHEN @Current <= @Initial THEN 0
                    WHEN @Current >= @Target THEN 1 ELSE (@Current - @Initial) / (@Target - @Initial) END);
                """;
            Add(calculation, "@Current", SqlDbType.Decimal, target.CurrentValue!.Value);
            Add(calculation, "@Initial", SqlDbType.Decimal, command.InitialValue!.Value);
            Add(calculation, "@Target", SqlDbType.Decimal, command.TargetValue!.Value);
            nextProgress = (decimal)calculation.ExecuteScalar()!;
        }
        // Use the actual SQL AVG/decimal rules of the persisted projection,
        // including the zero-target case, rather than approximating them in CLR.
        using var aggregate = connection.CreateCommand(); aggregate.Transaction = transaction;
        aggregate.CommandText = """
            SELECT CONVERT(decimal(28,8), COALESCE(AVG(CONVERT(decimal(28,8),
                CASE WHEN t.[Id] = @TargetId THEN @Next ELSE
                    CASE WHEN t.[Kind] = 'Numeric' AND t.[TargetValue] > t.[InitialValue]
                        THEN CASE WHEN t.[CurrentValue] <= t.[InitialValue] THEN 0
                                  WHEN t.[CurrentValue] >= t.[TargetValue] THEN 1
                                  ELSE (t.[CurrentValue] - t.[InitialValue]) / (t.[TargetValue] - t.[InitialValue]) END
                        WHEN t.[Kind] = 'Boolean' AND t.[BooleanValue] = 1 THEN 1 ELSE 0 END
                END)), 0))
            FROM [productivity].[GoalTarget] t
            WHERE t.[OwnerId] = @OwnerId AND t.[GoalId] = @GoalId AND t.[RemovedAt] IS NULL
              AND (@Removing = 0 OR t.[Id] <> @TargetId);
            """;
        Add(aggregate, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(aggregate, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        Add(aggregate, "@TargetId", SqlDbType.UniqueIdentifier, targetId);
        Add(aggregate, "@Next", SqlDbType.Decimal, nextProgress);
        Add(aggregate, "@Removing", SqlDbType.Bit, removing);
        var afterProgress = (decimal)aggregate.ExecuteScalar()!;
        var expires = _clock.GetUtcNow().AddMinutes(5);
        var token = _targetPreview.Encode(new(actor.UserId, actor.OwnerId, goalId, targetId, command.Mode),
            new(detail.Goal.ETag, target.ETag, target.Kind, title, command.InitialValue, command.TargetValue, expires));
        if (!CurrentAuthority(connection, transaction, actor, actions)) return ModuleUnavailable<GoalTargetChangePreview>();
        transaction.Commit();
        return IdentityOperationResult<GoalTargetChangePreview>.Success(new(token, expires, detail.Goal.ETag,
            target.ETag, target.Title, target.Kind, detail.Targets.Count, afterCount, detail.Goal.Progress, afterProgress,
            title, command.InitialValue?.ToString("G29", CultureInfo.InvariantCulture), command.TargetValue?.ToString("G29", CultureInfo.InvariantCulture)));
    }

    public IdentityOperationResult<GoalDetail> ChangeTarget(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string mode, string? ifMatch, string previewToken, string? idempotencyKey = null, string? traceId = null)
    {
        var operation = TargetChangeAction(mode);
        if (operation is null) return Failure<GoalDetail>("ValidationFailed", 422, "Target change mode is invalid.");
        var actions = RequiredActions(operation);
        if (!HasAllCapabilities(actor, actions)) return ModuleUnavailable<GoalDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<GoalDetail>();
        if (!Guid.TryParse(idempotencyKey, out var key) || key == Guid.Empty || string.IsNullOrEmpty(previewToken) || previewToken.Length > 4096)
            return Failure<GoalDetail>("ValidationFailed", 422, "A preview token and nonempty UUID idempotency key are required.");
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var intent = JsonSerializer.Serialize(new { version = 2, goalId, targetId, mode, ifMatch, previewToken });
        var receiptFailure = CheckReceipt(connection, transaction, actor, operation, idempotencyKey, intent, intent, goalId, out var receipt);
        if (receiptFailure is not null) return receiptFailure;
        var preview = _targetPreview.Decode(new(actor.UserId, actor.OwnerId, goalId, targetId, mode), previewToken);
        if (preview is null || preview.GoalETag != ifMatch || !TryETag(preview.TargetETag, out var expectedTarget))
            return Failure<GoalDetail>("ValidationFailed", 422, "Preview does not match this target revision.");
        if (preview.ExpiresAt <= _clock.GetUtcNow())
            return Failure<GoalDetail>("PreviewExpired", 410, "Preview expired. Review the current impact again.");
        try
        {
            var goal = ReadGoal(connection, transaction, actor.OwnerId, goalId, true);
            if (goal is null || goal.Status == "Deleted" || IsGoalTrashed(connection, transaction, actor.OwnerId, goalId)) return Missing<GoalDetail>();
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(goal.ETag))) return Revision<GoalDetail>();
            if (goal.Status is not ("Draft" or "Active"))
                return Failure<GoalDetail>("LifecycleLocked", 409, "Reopen the goal before changing a target.");
            var target = ReadTargets(connection, transaction, actor.OwnerId, goalId, true).FirstOrDefault(item => item.Id == targetId);
            if (target is null) return Missing<GoalDetail>();
            if (target.Kind != preview.Kind || !CryptographicOperations.FixedTimeEquals(expectedTarget, DecodeETag(target.ETag)))
                return Revision<GoalDetail>();
            var validation = ValidateTargetChange(new(mode, preview.Title, preview.InitialValue, preview.TargetValue), target, out var title);
            if (validation is not null) return Failure<GoalDetail>("ValidationFailed", 422, validation);
            CaptureTargetDefinition(connection,transaction,actor,goalId,targetId,"HistoryStarted",baseline:true);
            if (mode == "Remove")
                Execute(connection, transaction, """
                    UPDATE [productivity].[GoalTarget]
                    SET [RemovedAt] = SYSUTCDATETIME(), [RemovedByUserId] = @UserId,
                        [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId
                    WHERE [OwnerId] = @OwnerId AND [GoalId] = @GoalId AND [Id] = @TargetId
                      AND [RemovedAt] IS NULL AND [RowVersion] = @Version;
                    """,
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                    ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
                    ("@Version", SqlDbType.Binary, expectedTarget, 8));
            else
                Execute(connection, transaction, """
                    UPDATE [productivity].[GoalTarget]
                    SET [Title] = @Title, [InitialValue] = @Initial, [TargetValue] = @Target,
                        [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId
                    WHERE [OwnerId] = @OwnerId AND [GoalId] = @GoalId AND [Id] = @TargetId
                      AND [RemovedAt] IS NULL AND [RowVersion] = @Version;
                    """,
                    ("@Title", SqlDbType.NVarChar, title!, 200),
                    ("@Initial", SqlDbType.Decimal, (object?)preview.InitialValue ?? DBNull.Value, null),
                    ("@Target", SqlDbType.Decimal, (object?)preview.TargetValue ?? DBNull.Value, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                    ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
                    ("@Version", SqlDbType.Binary, expectedTarget, 8));
            Execute(connection, transaction, """
                UPDATE [productivity].[Goal] SET [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId
                WHERE [OwnerId] = @OwnerId AND [Id] = @GoalId AND [RowVersion] = @Version;
                """,
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@GoalId", SqlDbType.UniqueIdentifier, goalId, null),
                ("@Version", SqlDbType.Binary, expectedVersion, 8));
            var detail = ReadDetail(connection, transaction, actor.OwnerId, goalId, false);
            if (detail is null) return Missing<GoalDetail>();
            CaptureTargetDefinition(connection,transaction,actor,goalId,targetId,mode=="Remove"?"Removed":"Updated");
            WriteAudit(connection, transaction, actor, targetId, operation, "productivity.GoalTarget", traceId);
            return CommitResource(connection, transaction, actor, receipt, detail,
                mode == "Remove" ? "GoalTargetRemoved" : "GoalTargetUpdated", 200, actions);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<GoalDetail>(exception);
        }
    }

    private static string? TargetChangeAction(string mode) => mode switch
    { "Update" => "goals.target.update", "Remove" => "goals.target.remove", _ => null };

    private static string? ValidateTargetChange(GoalTargetChange command, GoalTargetRecord target, out string? title)
    {
        title = command.Title?.Trim();
        if (target.Kind is not ("Numeric" or "Boolean")) return "This target kind does not support local definition changes.";
        if (command.Mode == "Remove")
            return command.Title is not null || command.InitialValue.HasValue || command.TargetValue.HasValue
                ? "Remove preview cannot change target fields." : null;
        if (string.IsNullOrEmpty(title) || title.Length > 200) return "Target title must be between 1 and 200 characters.";
        if (target.Kind == "Boolean") return command.InitialValue.HasValue || command.TargetValue.HasValue
            ? "Boolean target cannot contain numeric fields." : null;
        if (command.InitialValue is not { } initial || command.TargetValue is not { } end || target.CurrentValue is not { } current)
            return "Numeric definition requires initial and target values.";
        return ValidateTarget(new(title, initial, current, end), out _);
    }

    private static bool IsGoalTrashed(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid goalId)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT TOP (1) 1 FROM [platform].[TrashItem] WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'Goal' AND [ResourceId] = @GoalId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        return command.ExecuteScalar() is not null;
    }
}
