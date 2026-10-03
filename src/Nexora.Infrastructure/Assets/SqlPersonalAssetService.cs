using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Assets;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Assets;

/// <summary>Owner-controlled metadata, non-loan states and immutable private history.</summary>
public sealed class SqlPersonalAssetService
    : IPersonalAssetService
{
    public static readonly string[] Actions = ["assets.asset.read", "assets.asset.create", "assets.asset.update",
        "assets.asset.transition", "assets.asset.history", "assets.asset.archive", "assets.asset.unarchive",
        "assets.asset.trash", "assets.asset.restore", "assets.asset.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private const string Columns = "Id,Title,Kind,Brand,Model,Category,Notes,State,CreatedAt,UpdatedAt,RowVersion";
    public SqlPersonalAssetService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX37", action);
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        capabilities.IsAllowed(c, tx, actor, "FX37", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(action => action, action => RequiredAllowed(actor, action));
        return result.Values.Any(value => value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result)
            : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<PersonalAssetPage> List(IdentityPrincipal actor, Guid? cursor, string? state, string? kind, string? category, string? query)
    {
        if (!Allowed(actor, "assets.asset.read")) return Denied<PersonalAssetPage>();
        state ??= "Active";
        if ((!Operational(state) && state is not ("Archived" or "Trash")) || (kind is not null && !ValidKind(kind)) || category is { Length: > 100 } || query is { Length: > 200 })
            return Fail<PersonalAssetPage>("ValidationFailed", 422, "Check asset state, kind, category and query.");
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, $"""
            WITH selected AS (SELECT {Columns} FROM [assets].[PersonalAsset]
                WHERE OwnerId=@Owner AND State=@State AND (@Kind IS NULL OR Kind=@Kind) AND (@Category IS NULL OR Category=@Category)
                  AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0 OR CHARINDEX(@Query,Model)>0))
            SELECT TOP(26) {Columns} FROM selected
            WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.Title>boundary.Title OR (selected.Title=boundary.Title AND selected.Id>boundary.Id)))
            ORDER BY Title ASC,Id ASC;
            """, actor.OwnerId);
        Add(cmd, "@State", SqlDbType.VarChar, state, 64); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Kind", SqlDbType.VarChar, kind, 64); Add(cmd, "@Category", SqlDbType.NVarChar, Blank(category), 100);
        Add(cmd, "@Query", SqlDbType.NVarChar, Blank(query), 200);
        var items = Rows(cmd);
        return IdentityOperationResult<PersonalAssetPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<PersonalAsset> Get(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor, "assets.asset.read")) return Denied<PersonalAsset>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        return item is null ? Missing<PersonalAsset>() : IdentityOperationResult<PersonalAsset>.Success(item);
    }
    public IdentityOperationResult<PersonalAssetAcknowledgement> Create(IdentityPrincipal actor, PersonalAssetCreate body, string key, string? trace) =>
        Mutate(actor, "assets.asset.create", body, key, trace, (c, tx) =>
        {
            if (!Operational(body.State) || !Validate(body.Title, body.Kind, body.Brand, body.Model, body.Category, body.Notes))
                return Fail<PersonalAssetAcknowledgement>("ValidationFailed", 422, "Choose explicit asset kind and operational state; check metadata lengths.");
            var id = Guid.NewGuid();
            using var resource = Command(c, tx, """
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX37' AND rt.Code='PersonalAsset' AND rt.ContractVersion='personal-assets-v1';
                """, actor.OwnerId);
            Add(resource, "@Id", SqlDbType.UniqueIdentifier, id); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            if (resource.ExecuteNonQuery() != 1) return Fail<PersonalAssetAcknowledgement>("DependencyUnavailable", 409, "The asset resource contract is unavailable.");
            using var cmd = Command(c, tx, """
                INSERT [assets].[PersonalAsset](Id,OwnerId,Title,Kind,Brand,Model,Category,Notes,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Kind,@Brand,@Model,@Category,@Notes,@State,SYSUTCDATETIME(),@User,@User);
                """, actor.OwnerId);
            Fields(cmd, actor, id, body.Title, body.Kind, body.Brand, body.Model, body.Category, body.Notes);
            Add(cmd, "@State", SqlDbType.VarChar, body.State, 64); cmd.ExecuteNonQuery();
            return IdentityOperationResult<PersonalAssetAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag), 201);
        });
    public IdentityOperationResult<PersonalAssetAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag,
        PersonalAssetMetadata body, string key, string? trace) =>
        Mutate(actor, "assets.asset.update", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<PersonalAssetAcknowledgement>();
            var stale = Precondition<PersonalAssetAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!Operational(item.State)) return Fail<PersonalAssetAcknowledgement>("LifecycleLocked", 409, "Restore or unarchive the asset before editing.");
            if (!Validate(body.Title, body.Kind, body.Brand, body.Model, body.Category, body.Notes))
                return Fail<PersonalAssetAcknowledgement>("ValidationFailed", 422, "Check asset metadata and explicit kind.");
            using var cmd = Command(c, tx, """
                UPDATE [assets].[PersonalAsset] SET Title=@Title,Kind=@Kind,Brand=@Brand,Model=@Model,Category=@Category,Notes=@Notes,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Fields(cmd, actor, id, body.Title, body.Kind, body.Brand, body.Model, body.Category, body.Notes); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, "Active");
            return IdentityOperationResult<PersonalAssetAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    public IdentityOperationResult<PersonalAssetAcknowledgement> SetState(IdentityPrincipal actor, Guid id, string? etag,
        PersonalAssetState body, string key, string? trace) =>
        Mutate(actor, "assets.asset.transition", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<PersonalAssetAcknowledgement>();
            var stale = Precondition<PersonalAssetAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!Operational(item.State)) return Fail<PersonalAssetAcknowledgement>("LifecycleLocked", 409, "Restore or unarchive the asset before changing its state.");
            if (!Operational(body.State) || body.Reason is { Length: > 2000 }) return Fail<PersonalAssetAcknowledgement>("ValidationFailed", 422, "Choose a supported non-loan state and a reason up to 2000 characters.");
            if (body.State == item.State) return Fail<PersonalAssetAcknowledgement>("StateUnchanged", 409, "Choose a different asset state.");
            using var cmd = Command(c, tx, "UPDATE [assets].[PersonalAsset] SET State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@State", SqlDbType.VarChar, body.State, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, "Active");
            return IdentityOperationResult<PersonalAssetAcknowledgement>.Success(new(id, Read(c, tx, actor.OwnerId, id)!.ETag));
        }, body.Reason);
    public IdentityOperationResult<PersonalAssetPreview> Preview(IdentityPrincipal actor, Guid id, string operation)
    {
        var action = Action(operation);
        if (action is null) return Fail<PersonalAssetPreview>("ValidationFailed", 422, "Choose a supported asset operation.");
        if (!Allowed(actor, action)) return Denied<PersonalAssetPreview>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        if (item is null) return Missing<PersonalAssetPreview>();
        if (!Eligible(item.State, operation)) return Fail<PersonalAssetPreview>("LifecycleLocked", 409, "The asset state does not allow this operation.");
        return IdentityOperationResult<PersonalAssetPreview>.Success(new(id, item.ETag, operation, ReferenceCount(c, null, actor.OwnerId, id)));
    }
    public IdentityOperationResult<PersonalAssetAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, PersonalAssetConfirmation body, string key, string? trace)
    {
        var action = Action(operation);
        if (action is null) return Fail<PersonalAssetAcknowledgement>("ValidationFailed", 422, "Choose a supported asset operation.");
        return Mutate(actor, action, new { id, etag, operation, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<PersonalAssetAcknowledgement>();
            var stale = Precondition<PersonalAssetAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!body.Confirm) return Fail<PersonalAssetAcknowledgement>("ConfirmationRequired", 422, "Confirm this asset operation.");
            if (!Eligible(item.State, operation)) return Fail<PersonalAssetAcknowledgement>("LifecycleLocked", 409, "The asset state does not allow this operation.");
            var next = operation switch
            {
                "archive" when Operational(item.State) => "Archived",
                "unarchive" when item.State == "Archived" => Previous(c, tx, actor.OwnerId, id, "PreArchiveState"),
                "trash" when item.State != "Trash" => "Trash",
                "restore" when item.State == "Trash" => Previous(c, tx, actor.OwnerId, id, "PreTrashState"),
                "purge" when item.State == "Trash" => "Purged",
                _ => null
            };
            if (next is null) return Fail<PersonalAssetAcknowledgement>("LifecycleLocked", 409, "The asset state does not allow this operation.");
            if (next == "Purged" && ReferenceCount(c, tx, actor.OwnerId, id) != 0)
                return Fail<PersonalAssetAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
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
                    DECLARE @CurrentBatch uniqueidentifier=(SELECT TrashBatchId FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id);
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
                Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? item.State : Previous(c, tx, actor.OwnerId, id, "PreTrashState"), 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1)
                    return Fail<PersonalAssetAcknowledgement>("LifecycleLocked", 409, "The deletion cohort is unavailable.");
            }
            using var cmd = Command(c, tx, next == "Purged" ? "DELETE [assets].[AssetVersion] WHERE OwnerId=@Owner AND AssetId=@Id; DELETE [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id;" : """
                UPDATE [assets].[PersonalAsset] SET
                    PreArchiveState=CASE WHEN @Operation='archive' THEN State WHEN @Operation='unarchive' THEN NULL ELSE PreArchiveState END,
                    PreTrashState=CASE WHEN @Operation='trash' THEN State WHEN @Operation='restore' THEN NULL ELSE PreTrashState END,
                    TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch WHEN @Operation='restore' THEN NULL ELSE TrashBatchId END,
                    State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Operation", SqlDbType.VarChar, operation, 64);
            Add(cmd, "@State", SqlDbType.VarChar, next, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, next is "Archived" or "Trash" or "Purged" ? next : "Active");
            return IdentityOperationResult<PersonalAssetAcknowledgement>.Success(new(id, next == "Purged" ? null : Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    }
    private IdentityOperationResult<PersonalAssetAcknowledgement> Mutate(IdentityPrincipal actor, string action, object request,
        string key, string? trace, Func<SqlConnection, SqlTransaction, IdentityOperationResult<PersonalAssetAcknowledgement>> work, string? reason = null)
    {
        if (!RequiredAllowed(actor, action)) return Denied<PersonalAssetAcknowledgement>();
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty)
            return Fail<PersonalAssetAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<PersonalAssetAcknowledgement>(); }
        if (!RequiredAllowed(c, tx, actor, action))
            return Denied<PersonalAssetAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<PersonalAssetAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
            return IdentityOperationResult<PersonalAssetAcknowledgement>.Success(JsonSerializer.Deserialize<PersonalAssetAcknowledgement>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<PersonalAssetAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx); if (!result.Succeeded || result.Value is null) return result;
        if (!RequiredAllowed(c, tx, actor, action)) return Denied<PersonalAssetAcknowledgement>();
        if (action != "assets.asset.purge") AppendVersion(c, tx, actor, result.Value.ItemId, action, reason);
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'assets.PersonalAsset',@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160);
        Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static string? Action(string operation) => operation switch
    {
        "archive" => "assets.asset.archive",
        "unarchive" => "assets.asset.unarchive", "trash" => "assets.asset.trash",
        "restore" => "assets.asset.restore", "purge" => "assets.asset.purge", _ => null
    };
    private static bool Eligible(string status, string operation) => operation switch
    {
        "archive" => Operational(status),
        "unarchive" => status == "Archived", "trash" => status != "Trash",
        "restore" or "purge" => status == "Trash", _ => false
    };
    private static IdentityOperationResult<T>? Precondition<T>(PersonalAsset item, string? etag) =>
        string.IsNullOrWhiteSpace(etag) ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") :
        item.ETag == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current asset item.");
    private bool RequiredAllowed(IdentityPrincipal actor, string action) => Allowed(actor, action) &&
        (action != "assets.asset.update" || Allowed(actor, "assets.asset.read"));
    private bool RequiredAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => Allowed(c, tx, actor, action) &&
        (action != "assets.asset.update" || Allowed(c, tx, actor, "assets.asset.read"));
    private static bool Operational(string? state) => state is "Active" or "Stored" or "Repair" or "Sold" or "Disposed" or "Lost";
    private static bool ValidKind(string? kind) => kind is "Device" or "Electronics" or "VehicleMetadata" or "Other";
    private static bool Validate(string? title, string? kind, string? brand, string? model, string? category, string? notes) =>
        !string.IsNullOrWhiteSpace(title) && title.Length <= 200 && ValidKind(kind) && brand is not { Length: > 100 } &&
        model is not { Length: > 200 } && category is not { Length: > 100 } && notes is not { Length: > 20000 };
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Fields(SqlCommand cmd, IdentityPrincipal actor, Guid id, string title, string kind, string? brand, string? model, string? category, string? notes)
    {
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Title", SqlDbType.NVarChar, title.Trim(), 200);
        Add(cmd, "@Kind", SqlDbType.VarChar, kind, 64); Add(cmd, "@Brand", SqlDbType.NVarChar, Blank(brand), 100);
        Add(cmd, "@Model", SqlDbType.NVarChar, Blank(model), 200); Add(cmd, "@Category", SqlDbType.NVarChar, Blank(category), 100);
        Add(cmd, "@Notes", SqlDbType.NVarChar, Blank(notes), -1); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
    }
    private static void UpdateResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string availability)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("PersonalAsset registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar());
    }
    private static string? Previous(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, string column)
    {
        if (column is not ("PreArchiveState" or "PreTrashState")) throw new ArgumentException("Unknown lifecycle field.", nameof(column));
        using var cmd = Command(c, tx, $"SELECT {column} FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return cmd.ExecuteScalar() as string;
    }
    private static PersonalAsset? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, $"SELECT {Columns} FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).FirstOrDefault(); }
    private static List<PersonalAsset> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<PersonalAsset>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3),
            r.IsDBNull(4) ? null : r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6),
            r.GetString(7), Utc(r.GetDateTime(8)), Utc(r.GetDateTime(9)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(10)) + "\""));
        return items;
    }
    private static readonly JsonSerializerOptions SnapshotJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static void AppendVersion(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string action, string? reason)
    {
        var item = Read(c, tx, actor.OwnerId, id) ?? throw new InvalidOperationException("Asset history integrity failed.");
        var fields = new PersonalAssetSnapshotFields(item.Title, item.Kind, item.Brand, item.Model, item.Category, item.Notes, item.State,
            Previous(c, tx, actor.OwnerId, id, "PreArchiveState"), Previous(c, tx, actor.OwnerId, id, "PreTrashState"));
        var snapshot = new PersonalAssetSnapshot(1, "PersonalAsset", fields, [], []);
        using var cmd = Command(c, tx, """
            INSERT [assets].[AssetVersion](Id,OwnerId,AssetId,VersionNumber,CreatedByUserId,ActionKey,Reason,SafeSnapshotJson)
            SELECT NEWID(),@Owner,@Id,Revision,@User,@Action,@Reason,@Snapshot FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id;
            """, actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(cmd, "@Action", SqlDbType.VarChar, action, 160); Add(cmd, "@Reason", SqlDbType.NVarChar, Blank(reason), 2000);
        Add(cmd, "@Snapshot", SqlDbType.NVarChar, JsonSerializer.Serialize(snapshot, SnapshotJson), -1);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Asset history integrity failed.");
    }
    public IdentityOperationResult<PersonalAssetHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor, string? action, string? from, string? to, string? version)
    {
        if (!Allowed(actor, "assets.asset.history")) return Denied<PersonalAssetHistoryPage>();
        long? exactVersion = null;
        if (version is not null)
        {
            if (version.Length > 19 || !long.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1)
                return Fail<PersonalAssetHistoryPage>("ValidationFailed", 422, "Choose a positive exact history version.");
            exactVersion = number;
        }
        if ((action is not null && !Actions.Contains(action)) || !Instant(from, out var start) || !Instant(to, out var end) ||
            (start is not null && end is not null && start >= end))
            return Fail<PersonalAssetHistoryPage>("ValidationFailed", 422, "Choose an installed history action and a valid UTC instant range.");
        using var c = connections.Create(); c.Open(); if (Read(c, null, actor.OwnerId, id) is null) return Missing<PersonalAssetHistoryPage>();
        using var cmd = Command(c, null, """
            WITH selected AS (SELECT Id,VersionNumber,ActionKey,CreatedAt,CreatedByUserId,Reason,SafeSnapshotJson FROM [assets].[AssetVersion]
                WHERE OwnerId=@Owner AND AssetId=@Id AND (@Action IS NULL OR ActionKey=@Action) AND (@Version IS NULL OR VersionNumber=@Version)
                  AND (@From IS NULL OR CreatedAt>=@From) AND (@To IS NULL OR CreatedAt<@To))
            SELECT TOP(26) * FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.VersionNumber<boundary.VersionNumber OR (selected.VersionNumber=boundary.VersionNumber AND selected.Id<boundary.Id)))
            ORDER BY VersionNumber DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Version", SqlDbType.BigInt, exactVersion); Add(cmd, "@Action", SqlDbType.VarChar, action, 160); Add(cmd, "@From", SqlDbType.DateTime2, start); Add(cmd, "@To", SqlDbType.DateTime2, end);
        using var r = cmd.ExecuteReader(); var items = new List<PersonalAssetVersion>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetInt64(1), r.GetString(2), Utc(r.GetDateTime(3)), r.IsDBNull(4) ? null : r.GetGuid(4),
            r.IsDBNull(5) ? null : r.GetString(5), JsonSerializer.Deserialize<PersonalAssetSnapshot>(r.GetString(6), SnapshotJson)!));
        return IdentityOperationResult<PersonalAssetHistoryPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    private static bool Instant(string? text, out DateTime? instant)
    {
        instant = null; if (text is null) return true;
        if (text.Length > 64 || !System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        instant = parsed.UtcDateTime; return true;
    }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
}
