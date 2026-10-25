using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Organization;

public sealed class SqlTemplateService : ITemplateService
{
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly TimeProvider clock;
    private static readonly string[] Actions = ["read", "create", "update", "archive", "unarchive", "trash", "restore", "purge", "instantiate"];

    public SqlTemplateService(SqlConnectionFactory connections, string idempotencySecret, TimeProvider clock)
    {
        this.connections = connections;
        this.clock = clock;
        capabilities = new(connections);
        receipts = new(idempotencySecret);
    }

    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        SqlCurrentActor.IsLive(c, tx, actor) && capabilities.IsAllowed(c, tx, actor, "FX24",
            action is "read" or "create" ? ["organization.template." + action] : ["organization.template." + action, "organization.template.read"]);
    private bool TargetAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor) =>
        capabilities.IsAllowed(c, tx, actor, "FX21", "bookmarks.bookmark.create");

    private IdentityOperationResult<T> Within<T>(Func<SqlConnection, SqlTransaction, IdentityOperationResult<T>> run)
    {
        try
        {
            using var c = connections.Create(); c.Open();
            using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            return run(c, tx);
        }
        catch (SqlException) { return Fail<T>("PersistenceUnavailable", 503); }
        catch (InvalidOperationException) { return Fail<T>("PersistenceUnavailable", 503); }
        catch (JsonException) { return Fail<T>("TemplateUnavailable", 409); }
    }

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor) => Within<IReadOnlyDictionary<string, bool>>((c, tx) =>
    {
        var result = Actions.ToDictionary(a => "organization.template." + a, a => Allowed(c, tx, actor, a));
        result["organization.template.instantiate"] &= TargetAllowed(c, tx, actor);
        if (!SqlCurrentActor.IsLive(c, tx, actor)) return Fail<IReadOnlyDictionary<string, bool>>("ActionDenied", 403);
        tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
    });

    public IdentityOperationResult<TemplatePage> List(IdentityPrincipal actor, string? query, string state, int page)
    {
        query = query?.Trim();
        if (query is { Length: > 100 } || page is < 1 or > 10000 || state is not ("Active" or "Archived" or "Trash"))
            return Fail<TemplatePage>("ValidationFailed", 422);
        var literal = (query ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        return Within<TemplatePage>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, "read")) return Fail<TemplatePage>("ActionDenied", 403);
            using var sql = Cmd(c, tx, actor, """
                SELECT Id,Name,ModuleCode,ResourceType,SchemaVersion,Status,RowVersion FROM organization.Template
                WHERE OwnerId=@Owner AND Status=@Status AND Name LIKE @Query ESCAPE N'\' ORDER BY Name,Id
                OFFSET @Skip ROWS FETCH NEXT 26 ROWS ONLY;
                """, ("@Status", state), ("@Query", "%" + literal + "%"), ("@Skip", (page - 1) * 25));
            var rows = new List<TemplateSummary>();
            using (var r = sql.ExecuteReader()) while (r.Read()) rows.Add(Summary(r));
            var more = rows.Count > 25; if (more) rows.RemoveAt(25);
            if (!Allowed(c, tx, actor, "read")) return Fail<TemplatePage>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<TemplatePage>.Success(new(rows, more, page));
        });
    }

    public IdentityOperationResult<TemplateDetail> Get(IdentityPrincipal actor, Guid id) => Within<TemplateDetail>((c, tx) =>
    {
        if (!Allowed(c, tx, actor, "read")) return Fail<TemplateDetail>("ActionDenied", 403);
        var row = Read(c, tx, actor, id, false);
        if (row is null) return Fail<TemplateDetail>("ResourceUnavailable", 404);
        var canUse = row.Template.Status != "Trash" && Allowed(c, tx, actor, "instantiate") && TargetAllowed(c, tx, actor);
        if (!Allowed(c, tx, actor, "read")) return Fail<TemplateDetail>("ActionDenied", 403);
        tx.Commit(); return IdentityOperationResult<TemplateDetail>.Success(new(row.Template, row.Seed, canUse,
            canUse ? null : "Use requires a template outside Trash and current Bookmark create permission."));
    });

    public IdentityOperationResult<TemplateDraft> Draft(IdentityPrincipal actor, Guid id, string etag)
    {
        if (!ValidETag(etag)) return Fail<TemplateDraft>("PreconditionRequired", 428);
        return Within<TemplateDraft>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, "instantiate")) return Fail<TemplateDraft>("ActionDenied", 403);
            var row = Read(c, tx, actor, id, false);
            if (row is null) return Fail<TemplateDraft>("ResourceUnavailable", 404);
            if (row.Template.ETag != etag) return Fail<TemplateDraft>("RevisionConflict", 412);
            if (row.Template.Status == "Trash" || !TargetAllowed(c, tx, actor)) return Fail<TemplateDraft>("TemplateUnavailable", 409);
            if (!Allowed(c, tx, actor, "instantiate") || !TargetAllowed(c, tx, actor)) return Fail<TemplateDraft>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<TemplateDraft>.Success(new(id, etag, row.Seed, "FX21", "Bookmark"));
        });
    }

    public IdentityOperationResult<TemplateSummary> Create(IdentityPrincipal a, TemplateWrite b, string key, string? trace) => Write(a, "create", Guid.Empty, b, null, key, trace);
    public IdentityOperationResult<TemplateSummary> Update(IdentityPrincipal a, Guid id, TemplateWrite b, string etag, string key, string? trace) => Write(a, "update", id, b, etag, key, trace);
    public IdentityOperationResult<TemplateSummary> Transition(IdentityPrincipal a, Guid id, string action, string etag, string key, string? trace) =>
        action is "archive" or "unarchive" or "trash" or "restore" or "purge" ? Write(a, action, id, null, etag, key, trace) : Fail<TemplateSummary>("ValidationFailed", 422);

    private sealed record Receipt(int Version, Guid OwnerId, Guid Id, string Action);
    private IdentityOperationResult<TemplateSummary> Write(IdentityPrincipal actor, string action, Guid id, TemplateWrite? body, string? etag, string key, string? trace)
    {
        if (!Guid.TryParseExact(key, "D", out var keyId) || keyId == Guid.Empty) return Fail<TemplateSummary>("InvalidIdempotencyKey", 422);
        if (action != "create" && !ValidETag(etag)) return Fail<TemplateSummary>("PreconditionRequired", 428);
        if (action is "create" or "update")
        {
            if (!BookmarkTemplateRules.TryNormalize(body, out var normalized)) return Fail<TemplateSummary>("ValidationFailed", 422);
            body = normalized;
        }
        return Within<TemplateSummary>((c, tx) =>
        {
            if (!Allowed(c, tx, actor, action)) return Fail<TemplateSummary>("ActionDenied", 403);
            var operation = "organization.template." + action;
            var claim = receipts.TryClaim(c, tx, actor.UserId, operation, key,
                JsonSerializer.Serialize(new { Version = 1, actor.OwnerId, Id = id, ETag = etag, Body = body }), clock.GetUtcNow().UtcDateTime);
            var status = action == "create" ? 201 : 200;
            if (claim.IsReplay)
            {
                Receipt? replay = null;
                try { replay = JsonSerializer.Deserialize<Receipt>(claim.ResultJson ?? "null"); } catch (JsonException) { }
                if (replay is null || replay.Version != 1 || replay.OwnerId != actor.OwnerId || replay.Id == Guid.Empty || replay.Action != action ||
                    (action != "create" && replay.Id != id) || claim.ResultStatusCode != status || claim.ResultCode != "TemplateCommitted")
                    return Fail<TemplateSummary>("IdempotencyConflict", 409);
                if (!Allowed(c, tx, actor, action)) return Fail<TemplateSummary>("ActionDenied", 403);
                tx.Commit(); return IdentityOperationResult<TemplateSummary>.Committed(replay.Id, status, "TemplateCommitted");
            }
            if (!claim.IsClaimed) return Fail<TemplateSummary>("IdempotencyConflict", 409);
            if (action == "create")
            {
                id = Guid.NewGuid();
                Execute(c, tx, actor, """
                    INSERT organization.Template(Id,OwnerId,Name,ModuleCode,ResourceType,SchemaVersion,SeedJson,Status,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Id,@Owner,@Name,'FX21','Bookmark',1,@Seed,'Active',@User,@User);
                    """, ("@Id", id), ("@Name", body!.Name), ("@Seed", JsonSerializer.Serialize(body.Seed)));
            }
            else
            {
                var current = Read(c, tx, actor, id, true);
                if (current is null) return Fail<TemplateSummary>("ResourceUnavailable", 404);
                if (current.Template.ETag != etag) return Fail<TemplateSummary>("RevisionConflict", 412);
                var state = current.Template.Status;
                if (action == "update")
                {
                    if (state == "Trash") return Fail<TemplateSummary>("LifecycleConflict", 409);
                    Execute(c, tx, actor, "UPDATE organization.Template SET Name=@Name,SeedJson=@Seed WHERE OwnerId=@Owner AND Id=@Id;",
                        ("@Id", id), ("@Name", body!.Name), ("@Seed", JsonSerializer.Serialize(body.Seed)));
                }
                else
                {
                    var next = action switch
                    {
                        "archive" when state == "Active" => "Archived",
                        "unarchive" when state == "Archived" => "Active",
                        "trash" when state is "Active" or "Archived" => "Trash",
                        "restore" when state == "Trash" => current.PreTrashStatus,
                        "purge" when state == "Trash" => "Purged",
                        _ => null
                    };
                    if (next is null) return Fail<TemplateSummary>("LifecycleConflict", 409);
                    if (next == "Purged") Execute(c, tx, actor, "DELETE organization.Template WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
                    else Execute(c, tx, actor, """
                        UPDATE organization.Template SET Status=@Status,PreTrashStatus=@Previous,DeletionBatchId=@Batch
                        WHERE OwnerId=@Owner AND Id=@Id;
                        """, ("@Id", id), ("@Status", next), ("@Previous", next == "Trash" ? state : null),
                        ("@Batch", next == "Trash" ? Guid.NewGuid() : null));
                }
                if (action != "purge") Execute(c, tx, actor, "UPDATE organization.Template SET UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
            }
            if (!Allowed(c, tx, actor, action)) return Fail<TemplateSummary>("ActionDenied", 403);
            Execute(c, tx, actor, """
                INSERT security.AuditEvent(ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
                VALUES(@User,@User,@Action,N'organization.Template',@Id,'Succeeded',@Trace);
                """, ("@Id", id), ("@Action", operation), ("@Trace", trace));
            receipts.Complete(c, tx, claim, "TemplateCommitted", status, JsonSerializer.Serialize(new Receipt(1, actor.OwnerId, id, action)));
            if (!Allowed(c, tx, actor, action)) return Fail<TemplateSummary>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<TemplateSummary>.Committed(id, status, "TemplateCommitted");
        });
    }

    private sealed record Stored(TemplateSummary Template, BookmarkTemplateSeed Seed, string? PreTrashStatus);
    private static Stored? Read(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, Guid id, bool update)
    {
        using var sql = Cmd(c, tx, a, $"SELECT Id,Name,ModuleCode,ResourceType,SchemaVersion,Status,RowVersion,SeedJson,PreTrashStatus FROM organization.Template {(update ? "WITH(UPDLOCK,HOLDLOCK)" : "")} WHERE OwnerId=@Owner AND Id=@Id;", ("@Id", id));
        using var r = sql.ExecuteReader();
        if (!r.Read()) return null;
        var seed = JsonSerializer.Deserialize<BookmarkTemplateSeed>(r.GetString(7)) ?? throw new JsonException();
        var summary = Summary(r);
        if (summary.ModuleCode != "FX21" || summary.ResourceType != "Bookmark" || summary.SchemaVersion != 1 ||
            !BookmarkTemplateRules.TryNormalize(new(summary.Name, seed), out _)) throw new JsonException();
        return new(summary, seed, r.IsDBNull(8) ? null : r.GetString(8));
    }
    private static TemplateSummary Summary(SqlDataReader r) => new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetString(5), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(6)) + "\"");
    private static bool ValidETag(string? v)
    {
        if (v is null || v.Length != 14 || v[0] != '"' || v[^1] != '"') return false;
        try { var b = Convert.FromBase64String(v[1..^1]); return b.Length == 8 && Convert.ToBase64String(b) == v[1..^1]; }
        catch (FormatException) { return false; }
    }
    private static SqlCommand Cmd(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, string text, params (string Key, object? Value)[] values)
    {
        var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = text; sql.CommandTimeout = 10;
        sql.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = a.OwnerId;
        sql.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = a.UserId;
        foreach (var (key, value) in values)
        {
            var type = value switch { Guid => SqlDbType.UniqueIdentifier, int => SqlDbType.Int, _ => SqlDbType.NVarChar };
            var p = sql.Parameters.Add(key, type); if (type == SqlDbType.NVarChar) p.Size = -1;
            p.Value = value ?? DBNull.Value;
        }
        return sql;
    }
    private static void Execute(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, string text, params (string Key, object? Value)[] values)
    { using var sql = Cmd(c, tx, a, text, values); sql.ExecuteNonQuery(); }
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, code switch
    {
        "RevisionConflict" => "Template changed. Reload before continuing.",
        "PersistenceUnavailable" => "Templates are temporarily unavailable. Retry the same command safely.",
        "TemplateUnavailable" => "This template is unavailable for the current state, provider or permissions.",
        "LifecycleConflict" => "This action does not apply to the current template state.",
        "PreconditionRequired" => "A current template revision is required.",
        _ => "The template operation could not be completed."
    });
}
