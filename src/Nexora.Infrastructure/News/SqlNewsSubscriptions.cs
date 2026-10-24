using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;

namespace Nexora.Infrastructure.News;

public sealed partial class SqlNewsReaderService
{
    public IdentityOperationResult<object?> Follow(IdentityPrincipal actor, NewsFollow body, string key, string? trace)
    {
        var url = NormalizeUrl(body.Url);
        if (url is null || body.TitleOverride is { Length: > 200 } || body.CategoryId == Guid.Empty) return Fail<object?>("ValidationFailed", 422);
        var title = string.IsNullOrWhiteSpace(body.TitleOverride) ? null : body.TitleOverride.Trim();
        return Mutate(actor, "news.source.follow", new { url, body.CategoryId, title }, key, trace,
            body.CategoryId.HasValue ? ["news.category.read"] : [], (c, tx) =>
        {
            Guid? feedId = null; string? storedUrl = null;
            using (var feed = Command(c, tx, actor.OwnerId, "SELECT Id,Url FROM [news].[Feed] WHERE UrlDigest=@Digest AND State<>'Disabled' AND ApprovedPublicAt IS NOT NULL;"))
            {
                Add(feed, "@Digest", SqlDbType.Binary, SHA256.HashData(Encoding.UTF8.GetBytes(url)), 32);
                using var reader = feed.ExecuteReader(); if (reader.Read()) { feedId = reader.GetGuid(0); storedUrl = reader.GetString(1); }
            }
            // An unknown URL never starts DNS, HTTP or an unapproved cold feed registration.
            if (feedId is null) return Fail<Mutation>("StoredSourceUnavailable", 409);
            if (!string.Equals(storedUrl, url, StringComparison.Ordinal)) return Fail<Mutation>("SourceIdentityConflict", 409);
            if (!CategoryExists(c, tx, actor, body.CategoryId)) return Fail<Mutation>("ResourceUnavailable", 404);
            Guid? existing = null; string? state = null;
            using (var lookup = Command(c, tx, actor.OwnerId, "SELECT Id,State FROM [news].[FeedSubscription] WHERE OwnerId=@Owner AND FeedId=@Feed;"))
            { Add(lookup, "@Feed", SqlDbType.UniqueIdentifier, feedId); using var reader = lookup.ExecuteReader(); if (reader.Read()) { existing = reader.GetGuid(0); state = reader.GetString(1); } }
            if (existing.HasValue && state != "Unfollowed") return IdentityOperationResult<Mutation>.Success(new(existing.Value));
            var id = existing ?? Guid.NewGuid();
            using var sql = Command(c, tx, actor.OwnerId, existing.HasValue
                ? "UPDATE [news].[FeedSubscription] SET State='Followed',CategoryId=@Category,TitleOverride=@Title,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;"
                : "INSERT [news].[FeedSubscription](Id,OwnerId,FeedId,CategoryId,TitleOverride,State,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Feed,@Category,@Title,'Followed',@User,@User);");
            Add(sql, "@Feed", SqlDbType.UniqueIdentifier, feedId); Add(sql, "@Category", SqlDbType.UniqueIdentifier, body.CategoryId); Add(sql, "@Title", SqlDbType.NVarChar, title, 200);
            Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(id, existing.HasValue ? 200 : 201));
        }, (c, tx, outcome) => CategoryExists(c, tx, actor, body.CategoryId) && FollowedFeedExists(c, tx, actor.OwnerId, outcome.Id));
    }
    public IdentityOperationResult<object?> UpdateSource(IdentityPrincipal actor, Guid id, string? etag, NewsSourceMetadata body, string key, string? trace)
    {
        if (body.TitleOverride is { Length: > 200 } || body.CategoryId == Guid.Empty) return Fail<object?>("ValidationFailed", 422);
        var title = string.IsNullOrWhiteSpace(body.TitleOverride) ? null : body.TitleOverride.Trim();
        return Mutate(actor, "news.source.update", new { id, etag, body.CategoryId, title }, key, trace,
            body.CategoryId.HasValue ? ["news.source.read", "news.category.read"] : ["news.source.read"], (c, tx) =>
        {
            var current = Subscription(c, tx, actor.OwnerId, id); if (current is null) return Fail<Mutation>("ResourceUnavailable", 404);
            if (string.IsNullOrEmpty(etag)) return Fail<Mutation>("PreconditionRequired", 428);
            if (etag != current.Value.ETag) return Fail<Mutation>("RevisionConflict", 412);
            if (current.Value.State == "Unfollowed") return Fail<Mutation>("LifecycleLocked", 409);
            if (!CategoryExists(c, tx, actor, body.CategoryId)) return Fail<Mutation>("ResourceUnavailable", 404);
            using var sql = Command(c, tx, actor.OwnerId, "UPDATE [news].[FeedSubscription] SET CategoryId=@Category,TitleOverride=@Title,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;");
            Add(sql, "@Category", SqlDbType.UniqueIdentifier, body.CategoryId); Add(sql, "@Title", SqlDbType.NVarChar, title, 200); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(id));
        });
    }
    public IdentityOperationResult<object?> SourceState(IdentityPrincipal actor, Guid id, string? etag, string operation, string key, string? trace)
    {
        if (operation is not ("pause" or "resume" or "unfollow")) return Fail<object?>("ValidationFailed", 422);
        return Mutate(actor, "news.source." + operation, new { id, etag, operation }, key, trace, [], (c, tx) =>
        {
            var current = Subscription(c, tx, actor.OwnerId, id); if (current is null) return Fail<Mutation>("ResourceUnavailable", 404);
            if (string.IsNullOrEmpty(etag)) return Fail<Mutation>("PreconditionRequired", 428);
            if (etag != current.Value.ETag) return Fail<Mutation>("RevisionConflict", 412);
            if (operation == "pause" && current.Value.State != "Followed" || operation == "resume" && current.Value.State != "Paused" || operation == "unfollow" && current.Value.State == "Unfollowed") return Fail<Mutation>("LifecycleLocked", 409);
            using var sql = Command(c, tx, actor.OwnerId, "UPDATE [news].[FeedSubscription] SET State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;");
            Add(sql, "@State", SqlDbType.VarChar, operation == "pause" ? "Paused" : operation == "resume" ? "Followed" : "Unfollowed", 32);
            Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); sql.ExecuteNonQuery();
            return IdentityOperationResult<Mutation>.Success(new(id));
        });
    }
    private static (string State, string ETag)? Subscription(SqlConnection c, SqlTransaction tx, Guid owner, Guid id)
    {
        using var sql = Command(c, tx, owner, "SELECT State,RowVersion FROM [news].[FeedSubscription] WHERE OwnerId=@Owner AND Id=@Id;");
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); using var reader = sql.ExecuteReader(); return reader.Read() ? (reader.GetString(0), Tag(reader.GetFieldValue<byte[]>(1))) : null;
    }
    private static bool FollowedFeedExists(SqlConnection c, SqlTransaction tx, Guid owner, Guid id)
    {
        using var sql = Command(c, tx, owner, "SELECT 1 FROM [news].[FeedSubscription] s JOIN [news].[Feed] f ON f.Id=s.FeedId WHERE s.OwnerId=@Owner AND s.Id=@Id AND s.State IN('Followed','Paused') AND f.State<>'Disabled' AND f.ApprovedPublicAt IS NOT NULL;");
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); return sql.ExecuteScalar() is not null;
    }
}
