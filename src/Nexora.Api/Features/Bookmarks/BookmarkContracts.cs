namespace Nexora.Api.Features.Bookmarks;

public sealed record BookmarkRequest(string Url, string Title, string? Description);

public sealed record BookmarkResponse(
    Guid Id,
    string Url,
    string CanonicalUrl,
    string Title,
    string? Description,
    string Health,
    DateTimeOffset? LastCheckedAt,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record BookmarkPageResponse(IReadOnlyList<BookmarkResponse> Items, string? NextCursor);

public sealed record BookmarkTransitionRequest(string Status);
