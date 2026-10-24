using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Nexora.Infrastructure.Snippets;

namespace Nexora.Infrastructure.Organization;

public sealed class SqlCollectionService : ICollectionService
{
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly CollectionSourceParticipant[] sources;
    private static readonly string[] Actions = ["read", "create", "update", "delete", "add", "remove", "reorder"];

    public SqlCollectionService(SqlConnectionFactory connections, string idempotencySecret)
    {
        this.connections = connections;
        capabilities = new(connections); receipts = new(idempotencySecret);
        sources = [new SqlProjectCollectionParticipant(capabilities), new SqlDocumentCollectionParticipant(capabilities),
            new SqlBookmarkCollectionParticipant(capabilities), new SqlSnippetCollectionParticipant(capabilities)];
    }

    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        SqlCurrentActor.IsLive(c, tx, actor) && capabilities.IsAllowed(c, tx, actor, "FX24",
            action is "update" or "add" or "remove" or "reorder"
                ? ["organization.collection." + action, "organization.collection.read"] : ["organization.collection." + action]);

    private IdentityOperationResult<T> Within<T>(Func<SqlConnection, SqlTransaction, IdentityOperationResult<T>> run)
    {
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            return run(c, tx);
        }
        catch (SqlException) { return Fail<T>("PersistenceUnavailable", 503); }
        catch (InvalidOperationException) { return Fail<T>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor) => Within<IReadOnlyDictionary<string, bool>>((c, tx) =>
    {
        var values = Actions.ToDictionary(a => "organization.collection." + a, a => Allowed(c, tx, actor, a));
        foreach (var a in Actions) values["organization.collection." + a] &= Allowed(c, tx, actor, a);
        tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(values);
    });

    public IdentityOperationResult<CollectionPage> List(IdentityPrincipal actor, string? query, int page)
    {
        query = query?.Trim();
        if (query is { Length: > 100 } || page is < 1 or > 10000) return Fail<CollectionPage>("ValidationFailed", 422);
        var literal = (query ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        return Within<CollectionPage>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, "read")) return Fail<CollectionPage>("ActionDenied", 403);
            using var sql = Cmd(c, tx, actor, """
                SELECT Id,Name,Description,RowVersion FROM organization.Collection
                WHERE OwnerId=@Owner AND Name LIKE @Query ESCAPE N'\' ORDER BY Name,Id
                OFFSET @Skip ROWS FETCH NEXT 26 ROWS ONLY;
                """, ("@Query", "%" + literal + "%"), ("@Skip", (page - 1) * 25));
            var items = new List<CollectionSummary>();
            using (var r = sql.ExecuteReader()) while (r.Read()) items.Add(Summary(r));
            var more = items.Count > 25; if (more) items.RemoveAt(25);
            if (!Allowed(c, tx, actor, "read")) return Fail<CollectionPage>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<CollectionPage>.Success(new(items, more, page));
        });
    }

    public IdentityOperationResult<CollectionDetail> Get(IdentityPrincipal actor, Guid id) => Within<CollectionDetail>((c, tx) =>
    {
        if (!Allowed(c, tx, actor, "read")) return Fail<CollectionDetail>("ActionDenied", 403);
        var collection = Read(c, tx, actor, id, false);
        if (collection is null) return Fail<CollectionDetail>("ResourceUnavailable", 404);
        var visible = Visible(c, tx, actor, Rows(c, tx, actor, id));
        // Revalidate all participant authorities at the final projection checkpoint.
        visible = Revalidate(c, tx, actor, visible);
        if (!Allowed(c, tx, actor, "read")) return Fail<CollectionDetail>("ActionDenied", 403);
        tx.Commit(); return IdentityOperationResult<CollectionDetail>.Success(new(collection, visible));
    });

    public IdentityOperationResult<IReadOnlyList<CollectionCandidate>> Candidates(IdentityPrincipal actor, string type, string? query)
    {
        var source = sources.SingleOrDefault(s => s.Type == type);
        if (source is null) return Fail<IReadOnlyList<CollectionCandidate>>("SourceTypeUnavailable", 422);
        query = query?.Trim(); if (query is { Length: > 100 }) return Fail<IReadOnlyList<CollectionCandidate>>("ValidationFailed", 422);
        return Within<IReadOnlyList<CollectionCandidate>>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, "read")) return Fail<IReadOnlyList<CollectionCandidate>>("ActionDenied", 403);
            if (!source.Available(c, tx, actor)) return Fail<IReadOnlyList<CollectionCandidate>>("SourceUnavailable", 409);
            var rows = source.Read(c, tx, actor, null, query);
            rows = rows.Where(row => Resolve(c, tx, actor, row.ResourceType, row.ResourceId) is not null).ToArray();
            if (!source.Available(c, tx, actor)) return Fail<IReadOnlyList<CollectionCandidate>>("SourceUnavailable", 409);
            if (!Allowed(c, tx, actor, "read")) return Fail<IReadOnlyList<CollectionCandidate>>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<IReadOnlyList<CollectionCandidate>>.Success(rows);
        });
    }

    public IdentityOperationResult<CollectionSummary> Create(IdentityPrincipal a, CollectionWrite b, string key, string? trace) =>
        Write(a, "create", Guid.Empty, b, null, key, trace);
    public IdentityOperationResult<CollectionSummary> Update(IdentityPrincipal a, Guid id, CollectionWrite b, string etag, string key, string? trace) =>
        Write(a, "update", id, b, etag, key, trace);
    public IdentityOperationResult<CollectionSummary> Delete(IdentityPrincipal a, Guid id, string etag, string key, string? trace) =>
        Write(a, "delete", id, null, etag, key, trace);
    public IdentityOperationResult<CollectionSummary> Add(IdentityPrincipal a, Guid id, CollectionAdd b, string etag, string key, string? trace) =>
        Write(a, "add", id, b, etag, key, trace);
    public IdentityOperationResult<CollectionSummary> Remove(IdentityPrincipal a, Guid id, Guid memberId, string etag, string key, string? trace) =>
        Write(a, "remove", id, memberId, etag, key, trace);
    public IdentityOperationResult<CollectionSummary> Reorder(IdentityPrincipal a, Guid id, CollectionOrder b, string etag, string key, string? trace) =>
        Write(a, "reorder", id, b, etag, key, trace);

    private sealed record Receipt(int Version, Guid OwnerId, Guid Id, string Action);
    private IdentityOperationResult<CollectionSummary> Write(IdentityPrincipal actor, string action, Guid id, object? body, string? etag, string key, string? trace)
    {
        if (!Guid.TryParseExact(key, "D", out var keyId) || keyId == Guid.Empty) return Fail<CollectionSummary>("InvalidIdempotencyKey", 422);
        if (action != "create" && !ValidETag(etag)) return Fail<CollectionSummary>("PreconditionRequired", 428);
        if (body is CollectionWrite metadata)
        {
            var name = metadata.Name?.Trim(); var description = metadata.Description?.Trim();
            if (name is null || name.Length is < 1 or > 100 || description is { Length: > 4000 }) return Fail<CollectionSummary>("ValidationFailed", 422);
            body = new CollectionWrite(name, string.IsNullOrEmpty(description) ? null : description);
        }
        if (body is CollectionAdd reference && (reference.ResourceId == Guid.Empty || !sources.Any(s => s.Type == reference.ResourceType)))
            return Fail<CollectionSummary>("SourceTypeUnavailable", 422);
        if (body is CollectionOrder order && (order.MemberIds is null || order.MemberIds.Count > 500 || order.MemberIds.Any(x => x == Guid.Empty) || order.MemberIds.Distinct().Count() != order.MemberIds.Count))
            return Fail<CollectionSummary>("ValidationFailed", 422);
        return Within<CollectionSummary>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, action)) return Fail<CollectionSummary>("ActionDenied", 403);
            var operation = "organization.collection." + action;
            var claim = receipts.TryClaim(c, tx, actor.UserId, operation, key,
                JsonSerializer.Serialize(new { Version = 1, actor.OwnerId, Id = id, ETag = etag, Body = body }), DateTime.UtcNow);
            var status = action == "create" ? 201 : 200;
            if (claim.IsReplay)
            {
                Receipt? replay = null;
                try { replay = JsonSerializer.Deserialize<Receipt>(claim.ResultJson ?? "null"); } catch (JsonException) { }
                if (replay is null || replay.Version != 1 || replay.OwnerId != actor.OwnerId || replay.Id == Guid.Empty ||
                    replay.Action != action || (action != "create" && replay.Id != id) || claim.ResultStatusCode != status || claim.ResultCode != "CollectionCommitted")
                    return Fail<CollectionSummary>("IdempotencyConflict", 409);
                if (!Allowed(c, tx, actor, action)) return Fail<CollectionSummary>("ActionDenied", 403);
                if (body is CollectionAdd originalAdd &&
                    sources.Single(s => s.Type == originalAdd.ResourceType).Available(c, tx, actor) == false)
                    return Fail<CollectionSummary>("SourceUnavailable", 409);
                tx.Commit(); return IdentityOperationResult<CollectionSummary>.Committed(replay.Id, status, "CollectionCommitted");
            }
            if (!claim.IsClaimed) return Fail<CollectionSummary>(claim.IsInvalid ? "InvalidIdempotencyKey" : "IdempotencyConflict", claim.IsInvalid ? 422 : 409);
            List<CollectionMember>? reorderedSources = null;
            if (action == "create")
            {
                id = Guid.NewGuid(); var b = (CollectionWrite)body!;
                Execute(c, tx, actor, """
                    INSERT organization.Collection(Id,OwnerId,Name,Description,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Id,@Owner,@Name,@Description,@User,@User);
                    """, ("@Id", id), ("@Name", b.Name), ("@Description", b.Description));
            }
            else
            {
                var current = Read(c, tx, actor, id, true);
                if (current is null) return Fail<CollectionSummary>("ResourceUnavailable", 404);
                if (current.ETag != etag) return Fail<CollectionSummary>("RevisionConflict", 412);
                var rows = Rows(c, tx, actor, id);
                switch (action)
                {
                    case "update":
                        var b = (CollectionWrite)body!;
                        Execute(c, tx, actor, "UPDATE organization.Collection SET Name=@Name,Description=@Description WHERE OwnerId=@Owner AND Id=@Id;",
                            ("@Id", id), ("@Name", b.Name), ("@Description", b.Description));
                        break;
                    case "delete":
                        Execute(c, tx, actor, "DELETE organization.CollectionMember WHERE OwnerId=@Owner AND CollectionId=@Id; DELETE organization.Collection WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
                        break;
                    case "add":
                        var add = (CollectionAdd)body!;
                        if (Resolve(c, tx, actor, add.ResourceType, add.ResourceId) is null) return Fail<CollectionSummary>("ResourceUnavailable", 404);
                        if (rows.Any(r => r.Type == add.ResourceType && r.ResourceId == add.ResourceId)) return Fail<CollectionSummary>("MembershipConflict", 409);
                        if (rows.Count >= 500) return Fail<CollectionSummary>("CollectionUnavailable", 409);
                        Execute(c, tx, actor, """
                            INSERT organization.CollectionMember(Id,OwnerId,CollectionId,ResourceType,ResourceId,Rank,CreatedByUserId)
                            VALUES(@Member,@Owner,@Id,@Type,@Resource,@Rank,@User);
                            """, ("@Member", Guid.NewGuid()), ("@Id", id), ("@Type", add.ResourceType), ("@Resource", add.ResourceId),
                            ("@Rank", rows.Count == 0 ? 0m : rows.Max(r => r.Rank) + 1));
                        break;
                    case "remove":
                        if (!rows.Any(r => r.Id == (Guid)body!)) return Fail<CollectionSummary>("ResourceUnavailable", 404);
                        Execute(c, tx, actor, "DELETE organization.CollectionMember WHERE OwnerId=@Owner AND CollectionId=@Id AND Id=@Member;", ("@Id", id), ("@Member", (Guid)body!));
                        break;
                    case "reorder":
                        var visible = Revalidate(c, tx, actor, Visible(c, tx, actor, rows));
                        reorderedSources = visible;
                        var requested = ((CollectionOrder)body!).MemberIds;
                        if (requested.Count != visible.Count || !requested.ToHashSet().SetEquals(visible.Select(v => v.Id)))
                            return Fail<CollectionSummary>("MembershipConflict", 409);
                        var visibleIds = visible.Select(v => v.Id).ToHashSet();
                        var slots = rows.Where(r => visibleIds.Contains(r.Id)).Select(r => r.Rank).ToArray();
                        for (var i = 0; i < requested.Count; i++)
                            Execute(c, tx, actor, "UPDATE organization.CollectionMember SET Rank=@Rank WHERE OwnerId=@Owner AND CollectionId=@Id AND Id=@Member;",
                                ("@Rank", slots[i]), ("@Id", id), ("@Member", requested[i]));
                        break;
                }
                if (action != "delete") Execute(c, tx, actor, "UPDATE organization.Collection SET UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
            }
            if (body is CollectionAdd finalAdd && Resolve(c, tx, actor, finalAdd.ResourceType, finalAdd.ResourceId) is null)
                return Fail<CollectionSummary>("ResourceUnavailable", 404);
            if (reorderedSources is not null && Revalidate(c, tx, actor, reorderedSources).Count != reorderedSources.Count)
                return Fail<CollectionSummary>("MembershipConflict", 409);
            if (!Allowed(c, tx, actor, action)) return Fail<CollectionSummary>("ActionDenied", 403);
            Execute(c, tx, actor, """
                INSERT security.AuditEvent(ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
                VALUES(@User,@User,@Action,N'organization.Collection',@Id,'Succeeded',@Trace);
                """, ("@Action", operation), ("@Id", id), ("@Trace", trace));
            receipts.Complete(c, tx, claim, "CollectionCommitted", status, JsonSerializer.Serialize(new Receipt(1, actor.OwnerId, id, action)));
            if (!Allowed(c, tx, actor, action)) return Fail<CollectionSummary>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<CollectionSummary>.Committed(id, status, "CollectionCommitted");
        });
    }

    private CollectionCandidate? Resolve(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string type, Guid id) =>
        sources.SingleOrDefault(s => s.Type == type)?.Read(c, tx, actor, id).SingleOrDefault();
    private sealed record Row(Guid Id, string Type, Guid ResourceId, decimal Rank);
    private static List<Row> Rows(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var sql = Cmd(c, tx, actor, "SELECT TOP(501) Id,ResourceType,ResourceId,Rank FROM organization.CollectionMember WHERE OwnerId=@Owner AND CollectionId=@Id ORDER BY Rank,Id;", ("@Id", id));
        var rows = new List<Row>(); using var r = sql.ExecuteReader();
        while (r.Read()) rows.Add(new(r.GetGuid(0), r.GetString(1), r.GetGuid(2), r.GetDecimal(3)));
        if (rows.Count > 500) throw new InvalidOperationException("Collection bound exceeded.");
        return rows;
    }
    private List<CollectionMember> Visible(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, List<Row> rows)
    {
        var result = new List<CollectionMember>();
        foreach (var row in rows)
            if (Resolve(c, tx, actor, row.Type, row.ResourceId) is { } s) result.Add(new(row.Id, s.ResourceType, s.ResourceId, s.Title, s.Status));
        return result;
    }
    private List<CollectionMember> Revalidate(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, List<CollectionMember> members) =>
        members.Where(m => Resolve(c, tx, actor, m.ResourceType, m.ResourceId) is not null).ToList();
    private static CollectionSummary? Read(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, bool update)
    {
        using var sql = Cmd(c, tx, actor, $"SELECT Id,Name,Description,RowVersion FROM organization.Collection {(update ? "WITH(UPDLOCK,HOLDLOCK)" : "")} WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
        using var r = sql.ExecuteReader(); return r.Read() ? Summary(r) : null;
    }
    private static CollectionSummary Summary(SqlDataReader r) => new(r.GetGuid(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(3)) + "\"");
    private static bool ValidETag(string? value)
    {
        if (value is null || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
        try { var bytes = Convert.FromBase64String(value[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value[1..^1]; }
        catch (FormatException) { return false; }
    }
    private static SqlCommand Cmd(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string text, params (string Key, object? Value)[] values)
    {
        var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = text; sql.CommandTimeout = 10;
        sql.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        sql.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        foreach (var (key, value) in values)
        {
            var type = value switch { Guid => SqlDbType.UniqueIdentifier, int => SqlDbType.Int, decimal => SqlDbType.Decimal, _ => SqlDbType.NVarChar };
            var p = sql.Parameters.Add(key, type); if (type == SqlDbType.NVarChar) p.Size = -1;
            if (type == SqlDbType.Decimal) { p.Precision = 28; p.Scale = 8; }
            p.Value = value ?? DBNull.Value;
        }
        return sql;
    }
    private static void Execute(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, string text, params (string Key, object? Value)[] values)
    { using var sql = Cmd(c, tx, a, text, values); sql.ExecuteNonQuery(); }
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status,
        code switch { "RevisionConflict" => "Collection changed. Reload before saving.", "PersistenceUnavailable" => "Collections are temporarily unavailable. Retry safely.",
            "SourceTypeUnavailable" => "This source type is not eligible for Collections.", "SourceUnavailable" => "The source module is disabled, unavailable, or not readable.", "PreconditionRequired" => "A current collection revision is required.",
            _ => "The collection operation could not be completed." });
}
