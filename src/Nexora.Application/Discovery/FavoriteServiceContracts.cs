using Nexora.Application.Identity;

namespace Nexora.Application.Discovery;

public sealed record FavoriteReferenceCommand(string ResourceType, Guid ResourceId);

public sealed record FavoriteRankCommand(decimal? Rank);

public sealed record FavoriteRecord(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    string State,
    string? Title,
    string? Status,
    DateTimeOffset? UpdatedAt,
    string? Route,
    decimal Rank,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedRecordAt,
    string ETag);

public sealed record FavoritePage(IReadOnlyList<FavoriteRecord> Items, string? NextCursor);

public interface IFavoriteService
{
    IdentityOperationResult<FavoritePage> List(IdentityPrincipal actor, string? resourceType = null,
        int? limit = null, string? cursor = null);
    IdentityOperationResult<FavoriteRecord> Add(IdentityPrincipal actor, FavoriteReferenceCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid favoriteId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FavoriteRecord> Reorder(IdentityPrincipal actor, Guid favoriteId, string? ifMatch,
        FavoriteRankCommand command, string? idempotencyKey = null, string? traceId = null);
}
