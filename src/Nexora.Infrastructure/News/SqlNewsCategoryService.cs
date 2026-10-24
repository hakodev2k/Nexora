using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.News;

/// <summary>Private category containers; no source ingestion, provider or article dependency.</summary>
public sealed class SqlNewsCategoryService : INewsCategoryService
{
    public static readonly string[] Actions = ["news.category.read", "news.category.create", "news.category.update"];
    private const string Columns = "Id,Name,CreatedAt,UpdatedAt,RowVersion";
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    public SqlNewsCategoryService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        Actions.Contains(action, StringComparer.Ordinal) && SqlCurrentActor.IsLive(c, tx, actor) &&
        capabilities.IsAllowed(c, tx, actor, "FX29", action) &&
        (action != "news.category.update" || capabilities.IsAllowed(c, tx, actor, "FX29", "news.category.read"));
    private static IdentityOperationResult<T> Safe<T>(Func<IdentityOperationResult<T>> work)
    {
        try { return work(); }
        catch (SqlException e) when (e.Number == 1205) { return Fail<T>("ConcurrencyConflict", 409, "Retry the reviewed request."); }
        catch (SqlException) { return Fail<T>("StorageUnavailable", 503, "Local storage is unavailable."); }
        catch (InvalidOperationException) { return Fail<T>("StorageUnavailable", 503, "The installed storage contract is unavailable."); }
    }
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!SqlCurrentActor.IsLive(c, tx, actor)) return Denied<IReadOnlyDictionary<string, bool>>();
        var result = Actions.ToDictionary(a => a, a => Allowed(c, tx, actor, a));
        if (!result.Values.Any(x => x) || !SqlCurrentActor.IsLive(c, tx, actor)) return Denied<IReadOnlyDictionary<string, bool>>();
        tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
    });
    public IdentityOperationResult<NewsCategoryPage> List(IdentityPrincipal actor, Guid? cursor, string? query) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.category.read")) return Denied<NewsCategoryPage>();
        if (!NewsCategoryInput.TryName(query, true, out query)) return Fail<NewsCategoryPage>("ValidationFailed", 422, "Enter a literal category name query up to 100 characters.");
        var selected = $"SELECT {string.Join(',', Columns.Split(',').Select(x => "n." + x))} FROM [news].[Category] n WITH(HOLDLOCK) WHERE n.OwnerId=@Owner AND {SqlNewsCategoryRegistry.Predicate} AND (@Query IS NULL OR CHARINDEX(@Query COLLATE Latin1_General_100_BIN2,n.Name)>0)";
        if (cursor is not null)
        {
            using var boundary = Command(c, tx, $"WITH selected AS ({selected}) SELECT Id FROM selected WHERE Id=@Cursor;", actor.OwnerId);
            Filters(boundary, cursor, query); if (boundary.ExecuteScalar() is null) return Missing<NewsCategoryPage>();
        }
        using var cmd = Command(c, tx, $"WITH selected AS ({selected}) SELECT TOP(26) {Columns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor AND (selected.Name>b.Name OR(selected.Name=b.Name AND selected.Id>b.Id))) ORDER BY Name ASC,Id ASC;", actor.OwnerId);
        Filters(cmd, cursor, query); var items = Rows(cmd);
        if (!Allowed(c, tx, actor, "news.category.read")) return Denied<NewsCategoryPage>();
        tx.Commit(); return IdentityOperationResult<NewsCategoryPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    });
    public IdentityOperationResult<NewsCategoryRecord> Get(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "news.category.read")) return Denied<NewsCategoryRecord>();
        var item = Read(c, tx, actor.OwnerId, id, false); if (item is null) return Missing<NewsCategoryRecord>();
        if (!Allowed(c, tx, actor, "news.category.read")) return Denied<NewsCategoryRecord>();
        tx.Commit(); return IdentityOperationResult<NewsCategoryRecord>.Success(item);
    });
    public IdentityOperationResult<NewsCategoryAcknowledgement> Create(IdentityPrincipal actor, NewsCategoryCommand body, string key, string? trace) =>
        Mutate(actor, "news.category.create", null, null, body, key, trace, (c, tx, _) =>
        {
            if (!NewsCategoryInput.TryNormalize(body.Metadata, out var metadata)) return new(Invalid(), false);
            var id = Guid.NewGuid(); SqlNewsCategoryRegistry.Create(c, tx, actor, id);
            using var cmd = Command(c, tx, "INSERT [news].[Category](Id,OwnerId,Name,UpdatedAt,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Name,SYSUTCDATETIME(),@User,@User);", actor.OwnerId);
            Fields(cmd, actor, id, metadata); cmd.ExecuteNonQuery();
            return new(IdentityOperationResult<NewsCategoryAcknowledgement>.Success(Ack(Read(c, tx, actor.OwnerId, id, false)!), 201), true);
        });
    public IdentityOperationResult<NewsCategoryAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, NewsCategoryCommand body, string key, string? trace) =>
        Mutate(actor, "news.category.update", id, etag, body, key, trace, (c, tx, item) =>
        {
            if (!NewsCategoryInput.TryNormalize(body.Metadata, out var metadata)) return new(Invalid(), false);
            if (item!.Metadata.Name == metadata.Name) return new(IdentityOperationResult<NewsCategoryAcknowledgement>.Success(Ack(item)), false);
            using var cmd = Command(c, tx, "UPDATE [news].[Category] SET Name=@Name,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
            Fields(cmd, actor, id, metadata); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Category integrity.");
            SqlNewsCategoryRegistry.Advance(c, tx, actor, id); return new(IdentityOperationResult<NewsCategoryAcknowledgement>.Success(Ack(Read(c, tx, actor.OwnerId, id, false)!)), true);
        });
    private sealed record Effect(IdentityOperationResult<NewsCategoryAcknowledgement> Result, bool Changed);
    private IdentityOperationResult<NewsCategoryAcknowledgement> Mutate(IdentityPrincipal actor, string action, Guid? id, string? etag, object body, string key, string? trace,
        Func<SqlConnection, SqlTransaction, NewsCategoryRecord?, Effect> work) => Safe(() =>
    {
        if (!Guid.TryParse(key, out var uuid) || uuid == Guid.Empty) return Fail<NewsCategoryAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID is required.");
        key = uuid.ToString("D");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<NewsCategoryAcknowledgement>(); }
        if (!Allowed(c, tx, actor, action)) return Denied<NewsCategoryAcknowledgement>();
        var item = id is { } source ? Read(c, tx, actor.OwnerId, source, true) : null;
        if (id is not null && item is null) return Missing<NewsCategoryAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(new { id, etag, body }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<NewsCategoryAcknowledgement>("IdempotencyConflict", 409, "The key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<NewsCategoryAcknowledgement>(saved) ?? throw new InvalidOperationException("Receipt projection.");
            if (Read(c, tx, actor.OwnerId, ack.Id, false) is null || (id is not null && ack.Id != id)) return Missing<NewsCategoryAcknowledgement>();
            if (!Allowed(c, tx, actor, action)) return Denied<NewsCategoryAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<NewsCategoryAcknowledgement>.Success(ack, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        }
        if (!claim.IsClaimed) return Fail<NewsCategoryAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        if (item is not null)
        {
            if (string.IsNullOrWhiteSpace(etag)) return Fail<NewsCategoryAcknowledgement>("PreconditionRequired", 428, "If-Match is required.");
            if (!NewsCategoryInput.ValidEtag(etag)) return Fail<NewsCategoryAcknowledgement>("ValidationFailed", 422, "Supply one valid resource ETag.");
            if (item.ETag != etag) return Fail<NewsCategoryAcknowledgement>("RevisionConflict", 412, "Reload the current category.");
        }
        var effect = work(c, tx, item); if (!effect.Result.Succeeded || effect.Result.Value is null) return effect.Result;
        if (effect.Changed)
        {
            using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'news.Category',@Id,'Succeeded',@Trace);", actor.OwnerId);
            Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, effect.Result.Value.Id); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        }
        receipts.Complete(c, tx, claim, effect.Result.Code, effect.Result.StatusCode, JsonSerializer.Serialize(effect.Result.Value));
        if (!Allowed(c, tx, actor, action) || Read(c, tx, actor.OwnerId, effect.Result.Value.Id, false) is null) return Denied<NewsCategoryAcknowledgement>();
        tx.Commit(); return effect.Result;
    });
    private static IdentityOperationResult<NewsCategoryAcknowledgement> Invalid() => Fail<NewsCategoryAcknowledgement>("ValidationFailed", 422, "Enter a valid literal category name.");
    private static NewsCategoryAcknowledgement Ack(NewsCategoryRecord item) => new(item.Id, item.ETag);
    private static NewsCategoryRecord? Read(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool update)
    {
        using var cmd = Command(c, tx, $"SELECT {string.Join(',', Columns.Split(',').Select(x => "n." + x))} FROM [news].[Category] n WITH({(update ? "UPDLOCK,HOLDLOCK" : "HOLDLOCK")}) WHERE n.OwnerId=@Owner AND n.Id=@Id AND {SqlNewsCategoryRegistry.Predicate};", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).FirstOrDefault();
    }
    private static List<NewsCategoryRecord> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<NewsCategoryRecord>();
        while (r.Read()) items.Add(new(r.GetGuid(0), new(1, r.GetString(1)), Utc(r.GetDateTime(2)), Utc(r.GetDateTime(3)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(4)) + "\"")); return items;
    }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static void Filters(SqlCommand cmd, Guid? cursor, string? query)
    { Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor); Add(cmd, "@Query", SqlDbType.NVarChar, query, 100); }
    private static void Fields(SqlCommand cmd, IdentityPrincipal actor, Guid id, NewsCategoryMetadata value)
    { Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(cmd, "@Name", SqlDbType.NVarChar, value.Name, 100); }
    internal static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    internal static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
}

internal static class SqlNewsCategoryRegistry
{
    internal const string Predicate = "EXISTS(SELECT 1 FROM [platform].[Resource] r WITH(HOLDLOCK) JOIN [platform].[ResourceType] rt WITH(HOLDLOCK) ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m WITH(HOLDLOCK) ON m.Id=rt.ModuleId WHERE r.OwnerId=n.OwnerId AND r.Id=n.Id AND r.Availability='Active' AND r.PurgedAt IS NULL AND rt.Code='Category' AND rt.ContractVersion='news-category-v1' AND m.Code='FX29')";
    internal static void Create(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var cmd = SqlNewsCategoryService.Command(c, tx, "INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId) SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX29' AND rt.Code='Category' AND rt.ContractVersion='news-category-v1';", actor.OwnerId);
        SqlNewsCategoryService.Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); SqlNewsCategoryService.Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Category registry contract.");
    }
    internal static void Advance(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var cmd = SqlNewsCategoryService.Command(c, tx, "UPDATE r SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=@Owner AND r.Id=@Id AND r.Availability='Active' AND r.PurgedAt IS NULL AND rt.Code='Category' AND rt.ContractVersion='news-category-v1' AND m.Code='FX29';", actor.OwnerId);
        SqlNewsCategoryService.Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); SqlNewsCategoryService.Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Category registry integrity.");
    }
}
