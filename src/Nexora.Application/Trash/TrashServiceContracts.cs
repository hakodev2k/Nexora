using Nexora.Application.Identity;

namespace Nexora.Application.Trash;

public sealed record TrashItemRecord(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    Guid DeletionBatchId,
    string PriorStatus,
    DateTimeOffset DeletedAt,
    DateTimeOffset? RestoredAt,
    DateTimeOffset? PurgedAt);

public sealed record TrashPage(IReadOnlyList<TrashItemRecord> Items, string? NextCursor);

public sealed record TrashRestoreResult(Guid DeletionBatchId, int RestoredCount, int RemainingCount);

public sealed record TrashPurgeCommand(Guid DeletionBatchId, string Confirmation);

public interface ITrashService
{
    IdentityOperationResult<TrashPage> List(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<TrashRestoreResult> Restore(IdentityPrincipal actor, Guid deletionBatchId,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Purge(IdentityPrincipal actor, TrashPurgeCommand command,
        string? idempotencyKey = null, string? traceId = null);
}
