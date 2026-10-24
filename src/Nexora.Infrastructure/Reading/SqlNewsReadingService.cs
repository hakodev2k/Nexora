using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Reading;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.News;

namespace Nexora.Infrastructure.Reading;

public sealed partial class SqlReadingService
{
    private sealed record NewsReceipt(int Version, Guid ActorId, Guid OwnerId, Guid ItemId, Guid SourceId, string Action, string[] Dependencies, int Status);
    private bool NewsCapability(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action)
    {
        string[] dependencies = action switch
        {
            "reading.item.save" => ["news.article.read", "news.article.save"],
            "reading.item.read" => ["news.article.read", "news.article.mark_read"],
            "reading.item.unread" => ["news.article.read", "news.article.mark_unread"],
            "reading.item.position" => ["news.article.read"],
            _ => []
        };
        return dependencies.Length > 0 && NewsAuthority(c, tx, actor, action, dependencies);
    }
    private bool NewsAuthority(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action, string[] dependencies) =>
        _capabilities.IsAllowed(c, tx, actor, "FX23", action) && dependencies.All(dependency =>
            _capabilities.IsAllowed(c, tx, actor, dependency.StartsWith("news.", StringComparison.Ordinal) ? "FX29" : "FX23", dependency));
    private static bool NewsKey(string? key) => Guid.TryParseExact(key, "D", out var parsed) && parsed != Guid.Empty;
    private static bool NewsOwnerLock(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor)
    {
        using var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;";
        Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); return sql.ExecuteScalar() is not null;
    }
    private IdentityOperationResult<ReadingItemRecord> ReplayNews(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action, Guid? expectedId, ReceiptClaim claim)
    {
        NewsReceipt? saved = null; try { if (claim.ResultJson is not null) saved = JsonSerializer.Deserialize<NewsReceipt>(claim.ResultJson); } catch (JsonException) { }
        string[] allowed = ["news.article.read", "news.article.save", "news.article.mark_read", "news.article.mark_unread", "reading.item.position"];
        if (saved is null || saved.Version != 1 || saved.ActorId != actor.UserId || saved.OwnerId != actor.OwnerId || saved.ItemId == Guid.Empty || saved.SourceId == Guid.Empty || saved.Action != action
            || (expectedId.HasValue && saved.ItemId != expectedId.Value) || saved.Status is not (200 or 201) || claim.ResultStatusCode != saved.Status || claim.ResultCode != "NewsReadingCommitted"
            || saved.Dependencies is null || saved.Dependencies.Distinct().Count() != saved.Dependencies.Length || saved.Dependencies.Any(dependency => !allowed.Contains(dependency, StringComparer.Ordinal))
            || !saved.Dependencies.Contains("news.article.read", StringComparer.Ordinal)
            || (action == "reading.item.read" && !saved.Dependencies.Contains("news.article.mark_read", StringComparer.Ordinal))
            || (action == "reading.item.unread" && !saved.Dependencies.Contains("news.article.mark_unread", StringComparer.Ordinal))
            || (action == "reading.item.save" && !saved.Dependencies.Contains("news.article.save", StringComparer.Ordinal)))
            return Failure<ReadingItemRecord>("ReceiptUnavailable", 503, "The completed News Reading request cannot be safely resolved.");
        return NewsAuthority(c, tx, actor, action, saved.Dependencies)
            ? IdentityOperationResult<ReadingItemRecord>.Committed(saved.ItemId, saved.Status, "NewsReadingCommitted") : ModuleUnavailable<ReadingItemRecord>();
    }
    private IdentityOperationResult<ReadingItemRecord> FinishNews(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid itemId, Guid sourceId,
        string action, string[] dependencies, int status, ReceiptClaim claim, string? trace)
    {
        WriteAudit(c, tx, actor, itemId, action, trace);
        _receipts.Complete(c, tx, claim, "NewsReadingCommitted", status, JsonSerializer.Serialize(new NewsReceipt(1, actor.UserId, actor.OwnerId, itemId, sourceId, action, dependencies, status)));
        if (!NewsAuthority(c, tx, actor, action, dependencies)) return ModuleUnavailable<ReadingItemRecord>();
        if (_news.ReadReadingSourceWithin(c, tx, actor, sourceId) is null) return SourceUnavailable<ReadingItemRecord>();
        tx.Commit(); return IdentityOperationResult<ReadingItemRecord>.Committed(itemId, status, "NewsReadingCommitted");
    }
    private IdentityOperationResult<ReadingItemRecord> SaveNews(IdentityPrincipal actor, Guid sourceId, string? key, string? trace)
    {
        if (!NewsKey(key) || sourceId == Guid.Empty) return Failure<ReadingItemRecord>("ValidationFailed", 422, "A source and nonempty UUID idempotency key are required.");
        using var c = _connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        const string action = "reading.item.save"; string[] dependencies = ["news.article.read", "news.article.save"];
        if (!NewsAuthority(c, tx, actor, action, dependencies)) return ModuleUnavailable<ReadingItemRecord>();
        if (!NewsOwnerLock(c, tx, actor)) return ModuleUnavailable<ReadingItemRecord>();
        var claim = _receipts.TryClaim(c, tx, actor.UserId, action + ".news-article:v1", key!, JsonSerializer.Serialize(new { version = 1, sourceId }), DateTime.UtcNow);
        if (claim.IsConflict) return Failure<ReadingItemRecord>("IdempotencyConflict", 409, "The key belongs to another request.");
        if (claim.IsReplay) return ReplayNews(c, tx, actor, action, null, claim);
        if (!claim.IsClaimed) return Failure<ReadingItemRecord>("RequestInProgress", 409, "The request is already in progress.");
        var source = _news.ReadReadingSourceWithin(c, tx, actor, sourceId); if (source is null) return SourceUnavailable<ReadingItemRecord>();
        Guid? existing;
        using (var lookup = c.CreateCommand())
        {
            lookup.Transaction = tx; lookup.CommandText = "SELECT r.Id FROM [knowledge].[ReadingItem] r JOIN [knowledge].[NewsReadingReference] n ON n.OwnerId=r.OwnerId AND n.ReadingItemId=r.Id AND n.ArticleId=r.SourceId WHERE r.OwnerId=@Owner AND r.SourceType='NewsArticle' AND r.SourceId=@Source;";
            Add(lookup, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(lookup, "@Source", SqlDbType.UniqueIdentifier, sourceId); existing = lookup.ExecuteScalar() as Guid?;
        }
        if (existing.HasValue) return FinishNews(c, tx, actor, existing.Value, sourceId, action, dependencies, 200, claim, trace);
        var id = Guid.NewGuid();
        Execute(c, tx, """
            INSERT [knowledge].[ReadingItem](Id,OwnerId,CreatedByUserId,UpdatedByUserId,SourceType,SourceId,State,Progress,ReadAt,SafeTitleSnapshot,SafeUrlSnapshot)
              VALUES(@Id,@Owner,@User,@User,'NewsArticle',@Source,@State,0,@ReadAt,N'',N'');
            INSERT [knowledge].[NewsReadingReference](OwnerId,ReadingItemId,FeedId,ArticleId) VALUES(@Owner,@Id,@Feed,@Source);
            """, ("@Id",SqlDbType.UniqueIdentifier,id),("@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId),("@User",SqlDbType.UniqueIdentifier,actor.UserId),
            ("@Source",SqlDbType.UniqueIdentifier,sourceId),("@Feed",SqlDbType.UniqueIdentifier,source.FeedId),("@State",SqlDbType.VarChar,source.ReadAt.HasValue?"Read":"Unread"),("@ReadAt",SqlDbType.DateTime2,(object?)source.ReadAt?.UtcDateTime??DBNull.Value));
        return FinishNews(c, tx, actor, id, sourceId, action, dependencies, 201, claim, trace);
    }
    private IdentityOperationResult<ReadingItemRecord>? TryUpdateNews(IdentityPrincipal actor, Guid itemId, string? etag, UpdateReadingCommand body,
        string action, string? key, string? trace)
    {
        using var c = _connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(c, tx, actor, "FX23", action)) return ModuleUnavailable<ReadingItemRecord>();
        if (!NewsOwnerLock(c, tx, actor)) return ModuleUnavailable<ReadingItemRecord>();
        var canonical = JsonSerializer.Serialize(new { version = 1, itemId, etag, body });
        var claim = NewsKey(key) ? _receipts.TryClaim(c, tx, actor.UserId, action + ".news-article:v1", key!, canonical, DateTime.UtcNow, claimIfMissing: false) : ReceiptClaim.Missing;
        if (claim.IsConflict) return Failure<ReadingItemRecord>("IdempotencyConflict", 409, "The key belongs to another request.");
        if (claim.IsReplay) return ReplayNews(c, tx, actor, action, itemId, claim);
        ReadingItemRecord? row;
        using (var sql = c.CreateCommand())
        {
            sql.Transaction = tx; sql.CommandText = "SELECT r.Id,r.SourceType,r.SourceId,r.State,r.Progress,r.SavedAt,r.ReadAt,N'',N'',CONVERT(bit,0),r.UpdatedAt,r.RowVersion FROM [knowledge].[ReadingItem] r JOIN [knowledge].[NewsReadingReference] n ON n.OwnerId=r.OwnerId AND n.ReadingItemId=r.Id AND n.ArticleId=r.SourceId WHERE r.OwnerId=@Owner AND r.Id=@Id AND r.SourceType='NewsArticle';";
            Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Id",SqlDbType.UniqueIdentifier,itemId);using var r=sql.ExecuteReader();row=r.Read()?Read(r,false):null;
        }
        if (row is null) return claim.IsMissing ? null : Failure<ReadingItemRecord>("RequestInProgress",409,"The request is already in progress.");
        if (!NewsKey(key)) return Failure<ReadingItemRecord>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        if (!claim.IsMissing) return Failure<ReadingItemRecord>("RequestInProgress",409,"The request is already in progress.");
        claim = _receipts.TryClaim(c, tx, actor.UserId, action + ".news-article:v1", key!, canonical, DateTime.UtcNow);
        if (!claim.IsClaimed) return Failure<ReadingItemRecord>("RequestInProgress",409,"The request is already in progress.");
        if (!_capabilities.IsAllowed(c,tx,actor,"FX29","news.article.read"))return SourceUnavailable<ReadingItemRecord>();
        var source = _news.ReadReadingSourceWithin(c,tx,actor,row.SourceId);if(source is null)return SourceUnavailable<ReadingItemRecord>();
        if(string.IsNullOrEmpty(etag)||string.IsNullOrEmpty(body.SourceETag))return Precondition<ReadingItemRecord>();
        if(row.ETag!=etag||source.ETag!=body.SourceETag)return Revision<ReadingItemRecord>();
        if(row.State=="Archived")return Failure<ReadingItemRecord>("LifecycleLocked",409,"Archived queue items are readonly.");
        var dependencies = new List<string>{"news.article.read"};
        if(body.Progress.HasValue&&body.Progress!=row.Progress&&action!="reading.item.position")dependencies.Add("reading.item.position");
        var sourceAction=body.State=="Read"?"news.article.mark_read":body.State=="Unread"||source.ReadAt.HasValue?"news.article.mark_unread":null;
        if(sourceAction is not null)dependencies.Add(sourceAction);
        var required=dependencies.ToArray();if(!NewsAuthority(c,tx,actor,action,required))return ModuleUnavailable<ReadingItemRecord>();
        if(sourceAction is not null&&!_news.SetReadingStateWithin(c,tx,actor,row.SourceId,body.State=="Read",trace))return SourceUnavailable<ReadingItemRecord>();
        var current=_news.ReadReadingSourceWithin(c,tx,actor,row.SourceId);if(current is null)return SourceUnavailable<ReadingItemRecord>();
        Execute(c,tx,"UPDATE [knowledge].[ReadingItem] SET State=@State,Progress=@Progress,ReadAt=@ReadAt,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;",
            ("@State",SqlDbType.VarChar,body.State),("@Progress",SqlDbType.Decimal,body.Progress??row.Progress),("@ReadAt",SqlDbType.DateTime2,(object?)current.ReadAt?.UtcDateTime??DBNull.Value),
            ("@User",SqlDbType.UniqueIdentifier,actor.UserId),("@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId),("@Id",SqlDbType.UniqueIdentifier,itemId));
        return FinishNews(c,tx,actor,itemId,row.SourceId,action,required,200,claim,trace);
    }
    private ReadingItemRecord ProjectNews(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,ReadingItemRecord item)
    {
        if(item.SourceType!="NewsArticle")return item;
        var source=_news.ReadReadingSourceWithin(c,tx,actor,item.SourceId);
        return source is null?item with{SafeTitleSnapshot="Source unavailable",SafeUrlSnapshot=string.Empty,SourceAvailable=false,SourceETag=null}
            :item with{SafeTitleSnapshot=source.Title,SafeUrlSnapshot=string.Empty,SourceAvailable=true,SourceETag=source.ETag,ReadAt=source.ReadAt,
                State=item.State=="Archived"?"Archived":source.ReadAt.HasValue?"Read":item.State=="Reading"?"Reading":"Unread"};
    }
}
