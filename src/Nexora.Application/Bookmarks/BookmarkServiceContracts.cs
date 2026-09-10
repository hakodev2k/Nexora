using Nexora.Application.Identity;

namespace Nexora.Application.Bookmarks;

public sealed record BookmarkRecord(
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

public sealed record BookmarkPage(IReadOnlyList<BookmarkRecord> Items, string? NextCursor);

public sealed record BookmarkCommand(string Url, string Title, string? Description);

public interface IBookmarkService
{
    IdentityOperationResult<BookmarkPage> List(IdentityPrincipal actor, bool includeArchived = false,
        string? query = null, int? limit = null);
    IdentityOperationResult<BookmarkRecord> Create(IdentityPrincipal actor, BookmarkCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<BookmarkRecord> Update(IdentityPrincipal actor, Guid bookmarkId, string? ifMatch,
        BookmarkCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<BookmarkRecord> Transition(IdentityPrincipal actor, Guid bookmarkId, string? ifMatch,
        string status, string? idempotencyKey = null, string? traceId = null);
}
