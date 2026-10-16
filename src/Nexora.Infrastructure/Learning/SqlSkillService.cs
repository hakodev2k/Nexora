using System.Data;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Learning;

/// <summary>Owner-controlled skill metadata and explicit self assessment. Evidence/merge are not installed.</summary>
public sealed class SqlSkillService
    : ISkillService
{
    public static readonly string[] Actions = ["learning.skill.read", "learning.skill.create", "learning.skill.update",
        "learning.skill.proficiency", "learning.skill.archive", "learning.skill.unarchive",
        "learning.skill.trash", "learning.skill.restore", "learning.skill.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private const string Columns = "Id,Title,Level,Description,Category,LastUsed,Status,CreatedAt,UpdatedAt,RowVersion";
    public SqlSkillService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX40", action);
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        capabilities.IsAllowed(c, tx, actor, "FX40", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(action => action, action => RequiredAllowed(actor, action));
        return result.Values.Any(value => value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result)
            : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<SkillPage> List(IdentityPrincipal actor, Guid? cursor, string? status, string? query)
    {
        if (!Allowed(actor, "learning.skill.read")) return Denied<SkillPage>();
        status ??= "Active";
        if (status is not ("Active" or "Archived" or "Trash") || query is { Length: > 200 })
            return Fail<SkillPage>("ValidationFailed", 422, "Choose a valid skill state and a query up to 200 characters.");
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, $"""
            WITH selected AS (SELECT {Columns} FROM [learning].[Skill]
                WHERE OwnerId=@Owner AND Status=@Status AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0))
            SELECT TOP(26) {Columns} FROM selected
            WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.UpdatedAt<boundary.UpdatedAt OR (selected.UpdatedAt=boundary.UpdatedAt AND selected.Id<boundary.Id)))
            ORDER BY UpdatedAt DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Status", SqlDbType.VarChar, status, 64); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Query", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), 200);
        var items = Rows(cmd);
        return IdentityOperationResult<SkillPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<Skill> Get(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor, "learning.skill.read")) return Denied<Skill>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        return item is null ? Missing<Skill>() : IdentityOperationResult<Skill>.Success(item);
    }
    public IdentityOperationResult<SkillAcknowledgement> Create(IdentityPrincipal actor, SkillCreate body, string key, string? trace) =>
        Mutate(actor, "learning.skill.create", body, key, trace, (c, tx) =>
        {
            if (!ValidLevel(body.Level) || !Validate(body.Title, body.Description, body.Category, out var normalized))
                return Fail<SkillAcknowledgement>("ValidationFailed", 422, "Check skill name, explicit self-assessed level, description and category.");
            if (Duplicate(c, tx, actor.OwnerId, normalized, null)) return Fail<SkillAcknowledgement>("DuplicateSkill", 409, "A retained skill already has this normalized name.");
            var id = Guid.NewGuid();
            using var resource = Command(c, tx, """
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX40' AND rt.Code='Skill' AND rt.ContractVersion='skills-v1';
                """, actor.OwnerId);
            Add(resource, "@Id", SqlDbType.UniqueIdentifier, id); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            if (resource.ExecuteNonQuery() != 1) return Fail<SkillAcknowledgement>("DependencyUnavailable", 409, "The skill resource contract is unavailable.");
            using var cmd = Command(c, tx, """
                INSERT [learning].[Skill](Id,OwnerId,Title,NormalizedTitle,Level,Description,Category,LastUsed,Status,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Normalized,@Level,@Description,@Category,@LastUsed,'Active',SYSUTCDATETIME(),@User,@User);
                """, actor.OwnerId);
            Fields(cmd, actor, id, body.Title, normalized, body.Description, body.Category, body.LastUsed);
            Add(cmd, "@Level", SqlDbType.VarChar, body.Level, 64); cmd.ExecuteNonQuery();
            return IdentityOperationResult<SkillAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag), 201);
        });
    public IdentityOperationResult<SkillAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag,
        SkillMetadata body, string key, string? trace) =>
        Mutate(actor, "learning.skill.update", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<SkillAcknowledgement>();
            var stale = Precondition<SkillAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (item.Status != "Active") return Fail<SkillAcknowledgement>("LifecycleLocked", 409, "Restore or unarchive the skill before editing.");
            if (!Validate(body.Title, body.Description, body.Category, out var normalized))
                return Fail<SkillAcknowledgement>("ValidationFailed", 422, "Check skill name, description and category.");
            if (Duplicate(c, tx, actor.OwnerId, normalized, id)) return Fail<SkillAcknowledgement>("DuplicateSkill", 409, "A retained skill already has this normalized name.");
            using var cmd = Command(c, tx, """
                UPDATE [learning].[Skill] SET Title=@Title,NormalizedTitle=@Normalized,Description=@Description,Category=@Category,LastUsed=@LastUsed,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Fields(cmd, actor, id, body.Title, normalized, body.Description, body.Category, body.LastUsed); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, "Active");
            return IdentityOperationResult<SkillAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    public IdentityOperationResult<SkillAcknowledgement> Proficiency(IdentityPrincipal actor, Guid id, string? etag,
        SkillProficiency body, string key, string? trace) =>
        Mutate(actor, "learning.skill.proficiency", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<SkillAcknowledgement>();
            var stale = Precondition<SkillAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (item.Status != "Active") return Fail<SkillAcknowledgement>("LifecycleLocked", 409, "Restore or unarchive the skill before assessing proficiency.");
            if (!ValidLevel(body.Level)) return Fail<SkillAcknowledgement>("ValidationFailed", 422, "Choose an explicit self-assessed proficiency.");
            using var cmd = Command(c, tx, "UPDATE [learning].[Skill] SET Level=@Level,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Level", SqlDbType.VarChar, body.Level, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, "Active");
            return IdentityOperationResult<SkillAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    public IdentityOperationResult<SkillPreview> Preview(IdentityPrincipal actor, Guid id, string operation)
    {
        var action = Action(operation);
        if (action is null) return Fail<SkillPreview>("ValidationFailed", 422, "Choose a supported skill operation.");
        if (!Allowed(actor, action)) return Denied<SkillPreview>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        if (item is null) return Missing<SkillPreview>();
        if (!Eligible(item.Status, operation)) return Fail<SkillPreview>("LifecycleLocked", 409, "The skill state does not allow this operation.");
        return IdentityOperationResult<SkillPreview>.Success(new(id, item.ETag, operation, ReferenceCount(c, null, actor.OwnerId, id)));
    }
    public IdentityOperationResult<SkillAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, SkillConfirmation body, string key, string? trace)
    {
        var action = Action(operation);
        if (action is null) return Fail<SkillAcknowledgement>("ValidationFailed", 422, "Choose a supported skill operation.");
        return Mutate(actor, action, new { id, etag, operation, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<SkillAcknowledgement>();
            var stale = Precondition<SkillAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!body.Confirm) return Fail<SkillAcknowledgement>("ConfirmationRequired", 422, "Confirm this skill operation.");
            if (!Eligible(item.Status, operation)) return Fail<SkillAcknowledgement>("LifecycleLocked", 409, "The skill state does not allow this operation.");
            var next = operation switch
            {
                "archive" when item.Status == "Active" => "Archived",
                "unarchive" when item.Status == "Archived" => Previous(c, tx, actor.OwnerId, id, "PreArchiveState"),
                "trash" when item.Status != "Trash" => "Trash",
                "restore" when item.Status == "Trash" => Previous(c, tx, actor.OwnerId, id, "PreTrashState"),
                "purge" when item.Status == "Trash" => "Purged",
                _ => null
            };
            if (next is null) return Fail<SkillAcknowledgement>("LifecycleLocked", 409, "The skill state does not allow this operation.");
            if (next == "Purged" && ReferenceCount(c, tx, actor.OwnerId, id) != 0)
                return Fail<SkillAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
            var batch = Guid.NewGuid();
            if (operation is "trash" or "restore" or "purge")
            {
                using var cohort = Command(c, tx, operation == "trash" ? """
                    INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Batch,@Owner,@Id,SYSUTCDATETIME(),'Trashed',SYSUTCDATETIME(),@User,@User);
                    INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(NEWID(),@Owner,@Batch,@Id,@Previous,0,SYSUTCDATETIME(),@User,@User);
                    SELECT 1;
                    """ : """
                    DECLARE @CurrentBatch uniqueidentifier=(SELECT TrashBatchId FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id);
                    IF NOT EXISTS(SELECT 1 FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id
                        WHERE b.OwnerId=@Owner AND b.Id=@CurrentBatch AND b.RootResourceId=@Id AND b.State='Trashed'
                        AND m.ResourceId=@Id AND m.PreviousLifecycle=@Previous AND m.Depth=0 AND m.ParentResourceId IS NULL AND m.PurgedAt IS NULL)
                        OR (SELECT COUNT(*) FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=@CurrentBatch)<>1
                        SELECT 0;
                    ELSE BEGIN
                        UPDATE [operations].[TrashBatch] SET State=CASE WHEN @Operation='restore' THEN 'Restored' ELSE 'Purged' END,
                            RestoredAt=CASE WHEN @Operation='restore' THEN SYSUTCDATETIME() ELSE NULL END,
                            UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@CurrentBatch;
                        IF @Operation='purge' UPDATE [operations].[TrashMember] SET PurgedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                            WHERE OwnerId=@Owner AND BatchId=@CurrentBatch AND ResourceId=@Id;
                        SELECT 1;
                    END;
                    """, actor.OwnerId);
                Add(cohort, "@Id", SqlDbType.UniqueIdentifier, id); Add(cohort, "@Batch", SqlDbType.UniqueIdentifier, batch);
                Add(cohort, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(cohort, "@Operation", SqlDbType.VarChar, operation, 64);
                Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? item.Status : Previous(c, tx, actor.OwnerId, id, "PreTrashState"), 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1)
                    return Fail<SkillAcknowledgement>("LifecycleLocked", 409, "The deletion cohort is unavailable.");
            }
            using var cmd = Command(c, tx, next == "Purged" ? "DELETE [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id;" : """
                UPDATE [learning].[Skill] SET
                    PreArchiveState=CASE WHEN @Operation='archive' THEN Status WHEN @Operation='unarchive' THEN NULL ELSE PreArchiveState END,
                    PreTrashState=CASE WHEN @Operation='trash' THEN Status WHEN @Operation='restore' THEN NULL ELSE PreTrashState END,
                    TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch WHEN @Operation='restore' THEN NULL ELSE TrashBatchId END,
                    Status=@Status,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Operation", SqlDbType.VarChar, operation, 64);
            Add(cmd, "@Status", SqlDbType.VarChar, next, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, next is "Archived" or "Trash" or "Purged" ? next : "Active");
            return IdentityOperationResult<SkillAcknowledgement>.Success(new(id, next == "Purged" ? null : Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    }
    private IdentityOperationResult<SkillAcknowledgement> Mutate(IdentityPrincipal actor, string action, object request,
        string key, string? trace, Func<SqlConnection, SqlTransaction, IdentityOperationResult<SkillAcknowledgement>> work)
    {
        if (!RequiredAllowed(actor, action)) return Denied<SkillAcknowledgement>();
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty)
            return Fail<SkillAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<SkillAcknowledgement>(); }
        if (!RequiredAllowed(c, tx, actor, action))
            return Denied<SkillAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<SkillAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
            return IdentityOperationResult<SkillAcknowledgement>.Success(JsonSerializer.Deserialize<SkillAcknowledgement>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<SkillAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx); if (!result.Succeeded || result.Value is null) return result;
        if (!RequiredAllowed(c, tx, actor, action)) return Denied<SkillAcknowledgement>();
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.Skill',@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160);
        Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static string? Action(string operation) => operation switch
    {
        "archive" => "learning.skill.archive",
        "unarchive" => "learning.skill.unarchive", "trash" => "learning.skill.trash",
        "restore" => "learning.skill.restore", "purge" => "learning.skill.purge", _ => null
    };
    private static bool Eligible(string status, string operation) => operation switch
    {
        "archive" => status == "Active",
        "unarchive" => status == "Archived", "trash" => status != "Trash",
        "restore" or "purge" => status == "Trash", _ => false
    };
    private static IdentityOperationResult<T>? Precondition<T>(Skill item, string? etag) =>
        string.IsNullOrWhiteSpace(etag) ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") :
        item.ETag == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current skill item.");
    private bool RequiredAllowed(IdentityPrincipal actor, string action) => Allowed(actor, action) &&
        (action != "learning.skill.update" || Allowed(actor, "learning.skill.read")) &&
        (action != "learning.skill.create" || Allowed(actor, "learning.skill.proficiency"));
    private bool RequiredAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => Allowed(c, tx, actor, action) &&
        (action != "learning.skill.update" || Allowed(c, tx, actor, "learning.skill.read")) &&
        (action != "learning.skill.create" || Allowed(c, tx, actor, "learning.skill.proficiency"));
    private static bool ValidLevel(string level) => level is "Beginner" or "Intermediate" or "Advanced" or "Expert";
    private static bool Validate(string? title, string? description, string? category, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200 || description is { Length: > 20000 } || category is { Length: > 200 }) return false;
        try { normalized = title.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant(); }
        catch (ArgumentException) { return false; }
        return normalized.Length is > 0 and <= 200;
    }
    private static bool Duplicate(SqlConnection c, SqlTransaction tx, Guid owner, string normalized, Guid? except)
    {
        using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [learning].[Skill] WHERE OwnerId=@Owner AND NormalizedTitle=@Normalized AND (@Except IS NULL OR Id<>@Except);", owner);
        Add(cmd, "@Normalized", SqlDbType.NVarChar, normalized, 200); Add(cmd, "@Except", SqlDbType.UniqueIdentifier, except);
        return Convert.ToInt32(cmd.ExecuteScalar()) != 0;
    }
    private static void Fields(SqlCommand cmd, IdentityPrincipal actor, Guid id, string title, string normalized, string? description, string? category, DateOnly? lastUsed)
    {
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Title", SqlDbType.NVarChar, title.Trim(), 200);
        Add(cmd, "@Normalized", SqlDbType.NVarChar, normalized, 200); Add(cmd, "@Description", SqlDbType.NVarChar, description?.Trim(), -1);
        Add(cmd, "@Category", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(category) ? null : category.Trim(), 200);
        Add(cmd, "@LastUsed", SqlDbType.Date, lastUsed?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
    }
    private static void UpdateResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string availability)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Skill registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar());
    }
    private static string? Previous(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, string column)
    {
        if (column is not ("PreArchiveState" or "PreTrashState")) throw new ArgumentException("Unknown lifecycle field.", nameof(column));
        using var cmd = Command(c, tx, $"SELECT {column} FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return cmd.ExecuteScalar() as string;
    }
    private static Skill? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, $"SELECT {Columns} FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).FirstOrDefault(); }
    private static List<Skill> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<Skill>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3),
            r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : DateOnly.FromDateTime(r.GetDateTime(5)),
            r.GetString(6), Utc(r.GetDateTime(7)), Utc(r.GetDateTime(8)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(9)) + "\""));
        return items;
    }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
}
