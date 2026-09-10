namespace Nexora.Api.Features.Trash;

public sealed record TrashItemResponse(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    Guid DeletionBatchId,
    string PriorStatus,
    DateTimeOffset DeletedAt,
    DateTimeOffset? RestoredAt,
    DateTimeOffset? PurgedAt);

public sealed record TrashPageResponse(IReadOnlyList<TrashItemResponse> Items, string? NextCursor);

public sealed record TrashRestoreResponse(Guid DeletionBatchId, int RestoredCount, int RemainingCount);

public sealed record TrashPurgeRequest(Guid DeletionBatchId, string Confirmation);
