using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.News;

public sealed record NewsSource(Guid Id, Guid FeedId, string Url, string Title, Guid? CategoryId, string State,
    string FetchState, DateTimeOffset? LastAttempt, DateTimeOffset? LastSuccess, string? LastErrorCode, string ETag, string? TitleOverride = null);
public sealed record NewsSourcePage(IReadOnlyList<NewsSource> Items, Guid? NextCursor);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewsFollow(string Url, Guid? CategoryId, string? TitleOverride);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewsSourceMetadata(Guid? CategoryId, string? TitleOverride);
public sealed record NewsArticle(Guid Id, Guid SubscriptionId, string Title, string SourceTitle, string OriginalUrl,
    DateTimeOffset? PublishedAt, DateTimeOffset FetchedAt, bool IsExcerpt, string? Body, bool Read, string StateETag);
public sealed record NewsArticlePage(IReadOnlyList<NewsArticle> Items, string? NextCursor, DateTimeOffset Watermark);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record NewsMarkAll(IReadOnlyList<Guid> SubscriptionIds, DateTimeOffset Through);
public sealed record NewsHistoryItem(Guid ArticleId, DateTimeOffset OpenedAt, NewsArticle? Article);
public sealed record NewsHistoryPage(IReadOnlyList<NewsHistoryItem> Items, string? NextCursor, DateTimeOffset Watermark);
public interface INewsReaderService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<NewsSourcePage> Sources(IdentityPrincipal actor, Guid? cursor, string? query, Guid? categoryId, string? state);
    IdentityOperationResult<object?> Follow(IdentityPrincipal actor, NewsFollow body, string key, string? trace);
    IdentityOperationResult<object?> UpdateSource(IdentityPrincipal actor, Guid id, string? etag, NewsSourceMetadata body, string key, string? trace);
    IdentityOperationResult<object?> SourceState(IdentityPrincipal actor, Guid id, string? etag, string operation, string key, string? trace);
    IdentityOperationResult<NewsArticlePage> Articles(IdentityPrincipal actor, Guid? subscriptionId, Guid? categoryId, string? query, bool unreadOnly, string? cursor, DateTimeOffset? from = null, DateTimeOffset? to = null);
    IdentityOperationResult<NewsArticle> Article(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<object?> MarkRead(IdentityPrincipal actor, Guid id, string? etag, bool read, string key, string? trace);
    IdentityOperationResult<object?> MarkAllRead(IdentityPrincipal actor, NewsMarkAll body, string key, string? trace);
    IdentityOperationResult<NewsHistoryPage> History(IdentityPrincipal actor, string? cursor);
    IdentityOperationResult<object?> ClearHistory(IdentityPrincipal actor, DateTimeOffset through, string key, string? trace);
}
