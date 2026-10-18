using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;

namespace Nexora.Infrastructure.News;

public sealed partial class SqlNewsReaderService
{
    private const string ArticleJoins = """
        FROM [news].[Article] a JOIN [news].[Feed] f ON f.Id=a.FeedId
        JOIN [news].[FeedSubscription] s ON s.FeedId=a.FeedId AND s.OwnerId=@Owner
        LEFT JOIN [news].[ArticleState] st ON st.OwnerId=s.OwnerId AND st.ArticleId=a.Id AND st.FeedId=a.FeedId
        """;
    private const string VisibleArticle = "s.State IN('Followed','Paused') AND f.State<>'Disabled' AND a.BodyFormat='PlainText'";
    private static string ArticleColumns(bool body) => "a.Id,s.Id,a.Title,COALESCE(s.TitleOverride,f.Title),a.Url,a.PublishedAt,a.FetchedAt,a.IsExcerpt,"
        + (body ? "a.SanitizedBody" : "CAST(NULL AS nvarchar(max))") + ",CONVERT(bit,CASE WHEN st.ReadAt IS NULL THEN 0 ELSE 1 END),COALESCE(st.RowVersion,CONVERT(binary(8),0x0000000000000000))";
    private static NewsArticle ReadArticle(SqlDataReader r) => new(r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.GetString(4), OptionalTime(r, 5), Utc(r.GetDateTime(6)), r.GetBoolean(7), Text(r, 8), r.GetBoolean(9), Tag(r.GetFieldValue<byte[]>(10)));

    public IdentityOperationResult<NewsArticlePage> Articles(IdentityPrincipal actor, Guid? subscriptionId, Guid? categoryId, string? query, bool unreadOnly, string? cursor, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        if (query is { Length: > 500 } || subscriptionId == Guid.Empty || categoryId == Guid.Empty || (from.HasValue && to.HasValue && from >= to)) return Fail<NewsArticlePage>("ValidationFailed", 422);
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var context = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { version = 1, actor.UserId, actor.OwnerId, subscriptionId, categoryId, query, unreadOnly, from = from?.UtcTicks, to = to?.UtcTicks })));
        DateTime? after = null; Guid? afterId = null; var watermark = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(cursor) && !cursors.TryDecode(cursor, context, out after, out afterId, out watermark)) return Fail<NewsArticlePage>("InvalidCursor", 422);
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.article.read")) return Denied<NewsArticlePage>();
        using var sql = Command(c, tx, actor.OwnerId, $"""
            SELECT TOP(26) {ArticleColumns(false)} {ArticleJoins}
            WHERE {VisibleArticle} AND a.FetchedAt<=@Watermark
              AND (@Subscription IS NULL OR s.Id=@Subscription) AND (@Category IS NULL OR s.CategoryId=@Category)
              AND (@Unread=0 OR st.ReadAt IS NULL)
              AND (@From IS NULL OR COALESCE(a.PublishedAt,a.FetchedAt)>=@From) AND (@To IS NULL OR COALESCE(a.PublishedAt,a.FetchedAt)<@To)
              AND (@Query IS NULL OR CHARINDEX(@Query,a.Title)>0 OR CHARINDEX(@Query,COALESCE(s.TitleOverride,f.Title))>0)
              AND (@After IS NULL OR COALESCE(a.PublishedAt,a.FetchedAt)<@After OR (COALESCE(a.PublishedAt,a.FetchedAt)=@After AND a.Id<@AfterId))
            ORDER BY COALESCE(a.PublishedAt,a.FetchedAt) DESC,a.Id DESC;
            """);
        Add(sql, "@Subscription", SqlDbType.UniqueIdentifier, subscriptionId); Add(sql, "@Category", SqlDbType.UniqueIdentifier, categoryId); Add(sql, "@Query", SqlDbType.NVarChar, query, 500);
        Add(sql, "@Unread", SqlDbType.Bit, unreadOnly); Add(sql, "@After", SqlDbType.DateTime2, after); Add(sql, "@AfterId", SqlDbType.UniqueIdentifier, afterId); Add(sql, "@Watermark", SqlDbType.DateTime2, watermark.UtcDateTime);
        Add(sql, "@From", SqlDbType.DateTime2, from?.UtcDateTime); Add(sql, "@To", SqlDbType.DateTime2, to?.UtcDateTime);
        var items = new List<NewsArticle>(); using (var r = sql.ExecuteReader()) while (r.Read()) items.Add(ReadArticle(r));
        if (!Allowed(c, tx, actor, "news.article.read")) return Denied<NewsArticlePage>();
        var more = items.Count > 25; if (more) items.RemoveAt(25);
        var next = more ? cursors.Encode(context, items[^1].PublishedAt ?? items[^1].FetchedAt, items[^1].Id, watermark) : null;
        tx.Commit(); return IdentityOperationResult<NewsArticlePage>.Success(new(items, next, watermark));
    }
    public IdentityOperationResult<NewsArticle> Article(IdentityPrincipal actor, Guid id)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.article.read")) return Denied<NewsArticle>();
        using (var owner = Command(c, tx, actor.OwnerId, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;"))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<NewsArticle>(); }
        var item = ArticleWithin(c, tx, actor.OwnerId, id, true);
        if (item is null) return Fail<NewsArticle>("ResourceUnavailable", 404);
        using (var history = Command(c, tx, actor.OwnerId, """
            IF EXISTS(SELECT 1 FROM [news].[ArticleState] WHERE OwnerId=@Owner AND ArticleId=@Id)
              UPDATE [news].[ArticleState] SET LastOpenedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND ArticleId=@Id;
            ELSE INSERT [news].[ArticleState](Id,OwnerId,FeedId,ArticleId,LastOpenedAt,CreatedByUserId,UpdatedByUserId)
              SELECT NEWID(),@Owner,FeedId,Id,SYSUTCDATETIME(),@User,@User FROM [news].[Article] WHERE Id=@Id;
            """))
        { Add(history, "@Id", SqlDbType.UniqueIdentifier, id); Add(history, "@User", SqlDbType.UniqueIdentifier, actor.UserId); history.ExecuteNonQuery(); }
        item = ArticleWithin(c, tx, actor.OwnerId, id, true);
        if (item is null) return Fail<NewsArticle>("ResourceUnavailable", 404);
        if (!Allowed(c, tx, actor, "news.article.read")) return Denied<NewsArticle>();
        tx.Commit(); return IdentityOperationResult<NewsArticle>.Success(item);
    }
    private static NewsArticle? ArticleWithin(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool body)
    {
        using var sql = Command(c, tx, owner, $"SELECT {ArticleColumns(body)} {ArticleJoins} WHERE a.Id=@Id AND {VisibleArticle};");
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); using var r = sql.ExecuteReader(); return r.Read() ? ReadArticle(r) : null;
    }
    public IdentityOperationResult<object?> MarkRead(IdentityPrincipal actor, Guid id, string? etag, bool read, string key, string? trace) =>
        Mutate(actor, read ? "news.article.mark_read" : "news.article.mark_unread", new { id, etag, read }, key, trace, [], (c, tx) =>
        {
            var current = ArticleWithin(c, tx, actor.OwnerId, id, false); if (current is null) return Fail<Mutation>("ResourceUnavailable", 404);
            if (string.IsNullOrEmpty(etag)) return Fail<Mutation>("PreconditionRequired", 428);
            if (etag != current.StateETag) return Fail<Mutation>("RevisionConflict", 412);
            using var sql = Command(c, tx, actor.OwnerId, """
                IF EXISTS(SELECT 1 FROM [news].[ArticleState] WHERE OwnerId=@Owner AND ArticleId=@Id)
                    UPDATE [news].[ArticleState] SET ReadAt=CASE WHEN @Read=1 THEN COALESCE(ReadAt,SYSUTCDATETIME()) ELSE NULL END,
                        UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND ArticleId=@Id;
                ELSE INSERT [news].[ArticleState](Id,OwnerId,FeedId,ArticleId,ReadAt,CreatedByUserId,UpdatedByUserId)
                    SELECT NEWID(),@Owner,FeedId,Id,CASE WHEN @Read=1 THEN SYSUTCDATETIME() ELSE NULL END,@User,@User FROM [news].[Article] WHERE Id=@Id;
                """);
            Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Read", SqlDbType.Bit, read); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(id));
        }, (c, tx, _) => ArticleWithin(c, tx, actor.OwnerId, id, false) is not null);
}
