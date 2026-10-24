using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.News;

public sealed partial class SqlNewsReaderService : INewsReaderService
{
    public static readonly string[] Actions = ["news.source.read", "news.source.follow", "news.source.update", "news.source.pause", "news.source.resume", "news.source.unfollow",
        "news.article.read", "news.article.mark_read", "news.article.mark_unread", "news.article.mark_all_read", "news.article.save", "news.history.read", "news.history.clear"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly NewsArticleCursor cursors;
    private const string AbsentETag = "\"AAAAAAAAAAA=\"";
    public SqlNewsReaderService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); cursors = new(secret); }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => capabilities.IsAllowed(c, tx, actor, "FX29", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, "The News operation is unavailable or its input has changed.");
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403);
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, Guid owner, string text)
    { var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = text; Add(sql, "@Owner", SqlDbType.UniqueIdentifier, owner); return sql; }
    private static void Add(SqlCommand sql, string name, SqlDbType type, object? value, int size = 0)
    { var parameter = sql.Parameters.Add(name, type); if (size != 0) parameter.Size = size; parameter.Value = value ?? DBNull.Value; }
    private static string Tag(byte[] value) => "\"" + Convert.ToBase64String(value) + "\"";
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static DateTimeOffset? OptionalTime(SqlDataReader reader, int field) => reader.IsDBNull(field) ? null : Utc(reader.GetDateTime(field));
    private static string? Text(SqlDataReader reader, int field) => reader.IsDBNull(field) ? null : reader.GetString(field);
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        var result = Actions.ToDictionary(action => action, action => Allowed(c, tx, actor, action)
            && (action != "news.source.update" || Allowed(c, tx, actor, "news.source.read"))
            && (action != "news.article.save" || (Allowed(c, tx, actor, "news.article.read") && capabilities.IsAllowed(c, tx, actor, "FX23", "reading.item.save"))));
        result["reading.item.remove"] = capabilities.IsAllowed(c, tx, actor, "FX23", "reading.item.remove");
        tx.Commit(); return result.Values.Any(value => value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<NewsSourcePage> Sources(IdentityPrincipal actor, Guid? cursor, string? query, Guid? categoryId, string? state)
    {
        if (query is { Length: > 200 } || state is not (null or "" or "Followed" or "Paused" or "Unfollowed")) return Fail<NewsSourcePage>("ValidationFailed", 422);
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.source.read")) return Denied<NewsSourcePage>();
        using var sql = Command(c, tx, actor.OwnerId, """
            WITH selected AS (
              SELECT s.Id,s.FeedId,f.Url,COALESCE(s.TitleOverride,f.Title) Title,s.CategoryId,s.State,
                CASE WHEN f.State='Disabled' THEN 'Disabled' WHEN f.State='Failed' THEN 'Failed' WHEN f.LastSuccessAt IS NULL THEN 'NeverFetched' ELSE f.State END FetchState,
                f.LastAttemptAt,f.LastSuccessAt,f.LastErrorCode,s.RowVersion,s.TitleOverride
              FROM [news].[FeedSubscription] s JOIN [news].[Feed] f ON f.Id=s.FeedId
              WHERE s.OwnerId=@Owner AND ((@State IS NULL AND s.State<>'Unfollowed') OR s.State=@State)
                AND (@Category IS NULL OR s.CategoryId=@Category)
                AND (@Query IS NULL OR CHARINDEX(@Query,COALESCE(s.TitleOverride,f.Title))>0 OR CHARINDEX(@Query,f.Url)>0)
            ) SELECT TOP(26) * FROM selected x WHERE @Cursor IS NULL OR EXISTS(
                SELECT 1 FROM selected b WHERE b.Id=@Cursor AND (x.Title>b.Title OR (x.Title=b.Title AND x.Id>b.Id)))
            ORDER BY Title,Id;
            """);
        Add(sql, "@Cursor", SqlDbType.UniqueIdentifier, cursor); Add(sql, "@Query", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), 200);
        Add(sql, "@Category", SqlDbType.UniqueIdentifier, categoryId); Add(sql, "@State", SqlDbType.VarChar, string.IsNullOrEmpty(state) ? null : state, 32);
        var items = new List<NewsSource>();
        using (var r = sql.ExecuteReader()) while (r.Read()) items.Add(new(r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3), r.IsDBNull(4) ? null : r.GetGuid(4), r.GetString(5), r.GetString(6),
            OptionalTime(r, 7), OptionalTime(r, 8), r.IsDBNull(9) ? null : SafeError(r.GetString(9)), Tag(r.GetFieldValue<byte[]>(10)), Text(r, 11)));
        if (!Allowed(c, tx, actor, "news.source.read")) return Denied<NewsSourcePage>();
        tx.Commit(); return IdentityOperationResult<NewsSourcePage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    private static string SafeError(string code) => code is "FetchUnavailable" or "FetchFailed" or "ParseFailed" or "Timeout" or "SourceDisabled" ? code : "SourceError";
    private static string? NormalizeUrl(string? text)
    {
        try
        {
        if (text is null || text.Length > 2048 || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo) || uri.HostNameType == UriHostNameType.Unknown || !string.IsNullOrEmpty(uri.Fragment)) return null;
        var normalized = new UriBuilder(uri) { Host = uri.IdnHost.ToLowerInvariant() }.Uri.AbsoluteUri;
        return normalized.Length <= 2048 ? normalized : null;
        }
        catch (Exception error) when (error is UriFormatException or ArgumentException) { return null; }
    }
    private bool CategoryExists(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid? id)
    {
        if (id is null) return true;
        if (!Allowed(c, tx, actor, "news.category.read")) return false;
        using var sql = Command(c, tx, actor.OwnerId, "SELECT 1 FROM [news].[Category] WHERE OwnerId=@Owner AND Id=@Category;");
        Add(sql, "@Category", SqlDbType.UniqueIdentifier, id); return sql.ExecuteScalar() is not null;
    }
    private sealed record Receipt(int Version, Guid ActorId, Guid OwnerId, Guid ResourceId, string Action, int Status);
    private sealed record Mutation(Guid Id, int Status = 200);
    private IdentityOperationResult<object?> Mutate(IdentityPrincipal actor, string action, object request, string key, string? trace,
        string[] dependencies, Func<SqlConnection, SqlTransaction, IdentityOperationResult<Mutation>> work,
        Func<SqlConnection, SqlTransaction, Mutation, bool>? finalSource = null)
    {
        if (!Guid.TryParseExact(key, "D", out var uuid) || uuid == Guid.Empty) return Fail<object?>("IdempotencyKeyRequired", 422);
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        bool Authorized() => Allowed(c, tx, actor, action) && dependencies.All(dependency => Allowed(c, tx, actor, dependency));
        if (!Authorized()) return Denied<object?>();
        using (var owner = Command(c, tx, actor.OwnerId, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;"))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<object?>(); }
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(new { version = 1, request }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<object?>("IdempotencyConflict", 409);
        if (claim.IsReplay)
        {
            Receipt? saved = null; try { if (claim.ResultJson is not null) saved = JsonSerializer.Deserialize<Receipt>(claim.ResultJson); } catch (JsonException) { }
            if (saved is null || saved.Version != 1 || saved.ActorId != actor.UserId || saved.OwnerId != actor.OwnerId || saved.ResourceId == Guid.Empty || saved.Action != action
                || saved.Status is not (200 or 201) || claim.ResultStatusCode != saved.Status || claim.ResultCode != "NewsCommitted") return Fail<object?>("ReceiptUnavailable", 503);
            if (!Authorized()) return Denied<object?>();
            return IdentityOperationResult<object?>.Committed(saved.ResourceId, saved.Status, "NewsCommitted");
        }
        if (!claim.IsClaimed) return Fail<object?>("RequestInProgress", 409);
        var outcome = work(c, tx); if (!outcome.Succeeded || outcome.Value is null) return Fail<object?>(outcome.Code, outcome.StatusCode);
        using (var audit = Command(c, tx, actor.OwnerId, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'news.OwnerState',@Id,'Succeeded',@Trace);"))
        { Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, outcome.Value.Id); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery(); }
        receipts.Complete(c, tx, claim, "NewsCommitted", outcome.Value.Status, JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, outcome.Value.Id, action, outcome.Value.Status)));
        if (!Authorized()) return Denied<object?>();
        if (finalSource is not null && !finalSource(c, tx, outcome.Value)) return Fail<object?>("ResourceUnavailable", 404);
        tx.Commit(); return IdentityOperationResult<object?>.Committed(outcome.Value.Id, outcome.Value.Status, "NewsCommitted");
    }
}
