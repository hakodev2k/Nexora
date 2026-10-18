using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;

namespace Nexora.Infrastructure.News;

public sealed partial class SqlNewsReaderService
{
    private static string SourceSelection(NewsMarkAll body) => JsonSerializer.Serialize(body.SubscriptionIds.OrderBy(id => id.ToString("N"), StringComparer.Ordinal));
    private static bool SelectionVisible(SqlConnection c, SqlTransaction tx, Guid owner, string selected, int count)
    {
        using var sql = Command(c, tx, owner, "SELECT COUNT(*) FROM [news].[FeedSubscription] s JOIN [news].[Feed] f ON f.Id=s.FeedId WHERE s.OwnerId=@Owner AND s.State IN('Followed','Paused') AND f.State<>'Disabled' AND s.Id IN(SELECT Id FROM OPENJSON(@Selection) WITH(Id uniqueidentifier '$'));");
        Add(sql, "@Selection", SqlDbType.NVarChar, selected, -1); return Convert.ToInt32(sql.ExecuteScalar()) == count;
    }
    public IdentityOperationResult<object?> MarkAllRead(IdentityPrincipal actor, NewsMarkAll body, string key, string? trace)
    {
        if (body.SubscriptionIds is null || body.SubscriptionIds.Count is < 1 or > 100 || body.SubscriptionIds.Any(id => id == Guid.Empty) || body.SubscriptionIds.Distinct().Count() != body.SubscriptionIds.Count)
            return Fail<object?>("ValidationFailed", 422);
        var selected = SourceSelection(body);
        return Mutate(actor, "news.article.mark_all_read", new { selected, through = body.Through.UtcTicks }, key, trace, [], (c, tx) =>
        {
            if (body.Through > DateTimeOffset.UtcNow || !SelectionVisible(c, tx, actor.OwnerId, selected, body.SubscriptionIds.Count)) return Fail<Mutation>("ScopeUnavailable", 409);
            using var sql = Command(c, tx, actor.OwnerId, """
                UPDATE st SET ReadAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                FROM [news].[ArticleState] st JOIN [news].[Article] a ON a.Id=st.ArticleId AND a.FeedId=st.FeedId
                JOIN [news].[FeedSubscription] s ON s.OwnerId=st.OwnerId AND s.FeedId=st.FeedId
                WHERE st.OwnerId=@Owner AND st.ReadAt IS NULL AND a.FetchedAt<=@Through
                  AND s.Id IN(SELECT Id FROM OPENJSON(@Selection) WITH(Id uniqueidentifier '$'));
                INSERT [news].[ArticleState](Id,OwnerId,FeedId,ArticleId,ReadAt,CreatedByUserId,UpdatedByUserId)
                SELECT NEWID(),@Owner,a.FeedId,a.Id,SYSUTCDATETIME(),@User,@User
                FROM [news].[Article] a JOIN [news].[FeedSubscription] s ON s.FeedId=a.FeedId AND s.OwnerId=@Owner
                WHERE a.FetchedAt<=@Through AND s.Id IN(SELECT Id FROM OPENJSON(@Selection) WITH(Id uniqueidentifier '$'))
                  AND NOT EXISTS(SELECT 1 FROM [news].[ArticleState] st WHERE st.OwnerId=@Owner AND st.ArticleId=a.Id);
                """);
            Add(sql, "@Selection", SqlDbType.NVarChar, selected, -1); Add(sql, "@Through", SqlDbType.DateTime2, body.Through.UtcDateTime); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(body.SubscriptionIds.OrderBy(id => id.ToString("N"), StringComparer.Ordinal).First()));
        }, (c, tx, _) => SelectionVisible(c, tx, actor.OwnerId, selected, body.SubscriptionIds.Count));
    }
    public IdentityOperationResult<NewsHistoryPage> History(IdentityPrincipal actor, string? cursor)
    {
        var context = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { version = 1, actor.UserId, actor.OwnerId, kind = "history" })));
        DateTime? after = null; Guid? afterId = null; var watermark = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(cursor) && !cursors.TryDecode(cursor, context, out after, out afterId, out watermark)) return Fail<NewsHistoryPage>("InvalidCursor", 422);
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.history.read")) return Denied<NewsHistoryPage>();
        using var sql = Command(c, tx, actor.OwnerId, "SELECT TOP(26) ArticleId,LastOpenedAt FROM [news].[ArticleState] WHERE OwnerId=@Owner AND LastOpenedAt IS NOT NULL AND LastOpenedAt<=@Watermark AND (@After IS NULL OR LastOpenedAt<@After OR (LastOpenedAt=@After AND ArticleId<@AfterId)) ORDER BY LastOpenedAt DESC,ArticleId DESC;");
        Add(sql, "@Watermark", SqlDbType.DateTime2, watermark.UtcDateTime); Add(sql, "@After", SqlDbType.DateTime2, after); Add(sql, "@AfterId", SqlDbType.UniqueIdentifier, afterId);
        var metadata = new List<(Guid Id, DateTimeOffset At)>(); using (var r = sql.ExecuteReader()) while (r.Read()) metadata.Add((r.GetGuid(0), Utc(r.GetDateTime(1))));
        var readable = Allowed(c, tx, actor, "news.article.read");
        var items = metadata.Take(25).Select(row => new NewsHistoryItem(row.Id, row.At, readable ? ArticleWithin(c, tx, actor.OwnerId, row.Id, false) : null)).ToArray();
        if (!Allowed(c, tx, actor, "news.history.read")) return Denied<NewsHistoryPage>();
        if (!Allowed(c, tx, actor, "news.article.read")) items = items.Select(item => item with { Article = null }).ToArray();
        var next = metadata.Count > 25 ? cursors.Encode(context, items[^1].OpenedAt, items[^1].ArticleId, watermark) : null;
        tx.Commit(); return IdentityOperationResult<NewsHistoryPage>.Success(new(items, next, watermark));
    }
    public IdentityOperationResult<object?> ClearHistory(IdentityPrincipal actor, DateTimeOffset through, string key, string? trace) =>
        Mutate(actor, "news.history.clear", new { through = through.UtcTicks }, key, trace, [], (c, tx) =>
        {
            if (through > DateTimeOffset.UtcNow) return Fail<Mutation>("ValidationFailed", 422);
            using var sql = Command(c, tx, actor.OwnerId, "UPDATE [news].[ArticleState] SET LastOpenedAt=NULL,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND LastOpenedAt<=@Through;");
            Add(sql, "@Through", SqlDbType.DateTime2, through.UtcDateTime); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(actor.OwnerId));
        });
}
