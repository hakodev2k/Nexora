namespace Nexora.Api.Features.Discovery;

public sealed record FavoriteReferenceRequest(string ResourceType, Guid ResourceId);

public sealed record FavoriteRankRequest(decimal? Rank);

public sealed record FavoriteResponse(
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
    DateTimeOffset FavoriteUpdatedAt,
    string ETag);

public sealed record FavoritePageResponse(IReadOnlyList<FavoriteResponse> Items, string? NextCursor);
