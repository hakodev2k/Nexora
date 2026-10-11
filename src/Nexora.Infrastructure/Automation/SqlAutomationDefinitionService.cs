using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;
using Nexora.Domain.Automation;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Automation;

/// <summary>Owner-private configuration. No scheduler, dispatch or provider authority.</summary>
public sealed partial class SqlAutomationDefinitionService : IAutomationDefinitionService
{
    public static readonly string[] Actions = ["automation.definition.read", "automation.definition.create", "automation.definition.save", "automation.definition.history", "automation.definition.validate", "automation.definition.trash", "automation.definition.restore", "automation.definition.import", "automation.definition.export", "automation.definition.enable", "automation.definition.disable", "automation.run.start", "automation.run.read", "automation.run.cancel"];
    private const string Columns = "d.Id,d.Title,d.CurrentVersion,d.State,d.Enabled,d.UpdatedAt,d.RowVersion,(SELECT Availability FROM [platform].[Resource] WHERE OwnerId=d.OwnerId AND Id=d.Id) AS Lifecycle,d.DeletionBatchId";
    private const string ResourcePredicate = "EXISTS(SELECT 1 FROM [platform].[Resource] r WITH(HOLDLOCK) JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=d.OwnerId AND r.Id=d.Id AND r.Availability IN('Active','Trash') AND r.PurgedAt IS NULL AND rt.Code='AutomationDefinition' AND rt.ContractVersion='automation-definition-v1' AND m.Code='FX34')";
    private readonly bool localExecutionEnabled;
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly AutomationDefinitionCursor cursors;
    public SqlAutomationDefinitionService(SqlConnectionFactory connections, string secret, bool localExecutionEnabled = false)
    { this.localExecutionEnabled = localExecutionEnabled; this.connections = connections; capabilities = new(connections); receipts = new(secret); cursors = new(secret); }
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource or continuation is unavailable.");
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        Actions.Contains(action, StringComparer.Ordinal) && (action is not ("automation.definition.enable" or "automation.run.start") || localExecutionEnabled) && SqlCurrentActor.IsLive(c, tx, actor) &&
        capabilities.IsAllowed(c, tx, actor, "FX34", action) &&
        capabilities.IsAllowed(c, tx, actor, "FX34", "automation.definition.read");
    private static IdentityOperationResult<T> Safe<T>(Func<IdentityOperationResult<T>> work)
    {
        try { return work(); }
        catch (SqlException e) when (e.Number == 1205) { return Fail<T>("ConcurrencyConflict", 409, "Retry the same request."); }
        catch (SqlException) { return Fail<T>("StorageUnavailable", 503, "Definition storage is unavailable."); }
        catch (InvalidOperationException) { return Fail<T>("StorageUnavailable", 503, "Definition storage is unavailable."); }
    }
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IReadOnlyDictionary<string, bool>>();
        var result = Actions.ToDictionary(a => a, a => Allowed(c, tx, actor, a));
        tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
    });
    public IdentityOperationResult<AutomationDefinitionPage> List(IdentityPrincipal actor, string? cursor, string? query, string lifecycle = "Active") => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<AutomationDefinitionPage>();
        if (lifecycle is not ("Active" or "Trash")) return Fail<AutomationDefinitionPage>("ValidationFailed", 422, "Check the lifecycle filter.");
        query = query?.Trim() ?? "";
        var cursorIdentity = JsonSerializer.Serialize(new { query, lifecycle });
        if (query.Length > 200 || query.Any(char.IsControl)) return Fail<AutomationDefinitionPage>("ValidationFailed", 422, "Check the title query.");
        DateTime at = default; Guid boundary = default;
        if (cursor is not null && !cursors.TryDecode(cursor, actor.OwnerId, cursorIdentity, out at, out boundary)) return Missing<AutomationDefinitionPage>();
        var selected = $"SELECT {Columns} FROM [automation].[Definition] d WITH(HOLDLOCK) WHERE d.OwnerId=@Owner AND d.CurrentVersion IS NOT NULL AND {ResourcePredicate} AND EXISTS(SELECT 1 FROM [platform].[Resource] r WHERE r.OwnerId=d.OwnerId AND r.Id=d.Id AND r.Availability=@Lifecycle) AND (@Query='' OR CHARINDEX(@Query,d.Title)>0)";
        using var cmd = Command(c, tx, $"WITH selected AS ({selected}) SELECT TOP(26) * FROM selected WHERE @Boundary IS NULL OR UpdatedAt<@At OR(UpdatedAt=@At AND Id<@Boundary) ORDER BY UpdatedAt DESC,Id DESC;", actor.OwnerId);
        Add(cmd, "@Lifecycle", SqlDbType.VarChar, lifecycle, 16); Add(cmd, "@Query", SqlDbType.NVarChar, query, 200); Add(cmd, "@Boundary", SqlDbType.UniqueIdentifier, cursor is null ? null : boundary); Add(cmd, "@At", SqlDbType.DateTime2, cursor is null ? null : at);
        if (cursor is not null)
        {
            using var anchor = Command(c, tx, $"WITH selected AS ({selected}) SELECT 1 FROM selected WHERE Id=@Boundary AND UpdatedAt=@At;", actor.OwnerId);
            Add(anchor, "@Lifecycle", SqlDbType.VarChar, lifecycle, 16); Add(anchor, "@Query", SqlDbType.NVarChar, query, 200); Add(anchor, "@Boundary", SqlDbType.UniqueIdentifier, boundary); Add(anchor, "@At", SqlDbType.DateTime2, at);
            if (anchor.ExecuteScalar() is null) return Missing<AutomationDefinitionPage>();
        }
        var items = Rows(cmd);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<AutomationDefinitionPage>();
        var next = items.Count > 25 ? cursors.Encode(actor.OwnerId, cursorIdentity, items[24].UpdatedAt.UtcDateTime, items[24].Id) : null;
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionPage>.Success(new(items.Take(25).ToArray(), next));
    });
    public IdentityOperationResult<AutomationDefinitionDetail> Get(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<AutomationDefinitionDetail>();
        var item = Read(c, tx, actor.OwnerId, id, false); if (item is null) return Missing<AutomationDefinitionDetail>();
        var version = Versions(c, tx, actor.OwnerId, id, item.CurrentVersion + 1, 1).SingleOrDefault();
        if (version is null || version.VersionNumber != item.CurrentVersion) return Missing<AutomationDefinitionDetail>();
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<AutomationDefinitionDetail>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionDetail>.Success(new(item, version));
    });
    public IdentityOperationResult<AutomationVersionPage> History(IdentityPrincipal actor, Guid id, long? beforeVersion) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "automation.definition.history")) return Denied<AutomationVersionPage>();
        var item = Read(c, tx, actor.OwnerId, id, false); if (item is null) return Missing<AutomationVersionPage>();
        if (beforeVersion is < 1 || beforeVersion > item.CurrentVersion) return Missing<AutomationVersionPage>();
        var versions = Versions(c, tx, actor.OwnerId, id, beforeVersion ?? item.CurrentVersion + 1, 26);
        if (!Allowed(c, tx, actor, "automation.definition.history")) return Denied<AutomationVersionPage>();
        tx.Commit(); return IdentityOperationResult<AutomationVersionPage>.Success(new(versions.Take(25).ToArray(), versions.Count > 25 ? versions[24].VersionNumber : null));
    });
    public IdentityOperationResult<AutomationDefinitionValidation> Validate(IdentityPrincipal actor, AutomationDefinitionCommand command) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "automation.definition.validate")) return Denied<AutomationDefinitionValidation>();
        var shape = AutomationDefinitionContract.Parse(command.DefinitionJson);
        var titleValid = AutomationDefinition.TryTitle(command.Title, out _);
        var referenceCode = shape.Accepted && titleValid ? CheckTargets(c, tx, actor, shape.Definition!) : "DefinitionInvalid";
        if (!Allowed(c, tx, actor, "automation.definition.validate")) return Denied<AutomationDefinitionValidation>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionValidation>.Success(new(titleValid && shape.Accepted,
            !titleValid ? "TitleInvalid" : !shape.Accepted ? shape.Code : referenceCode ?? "DefinitionReferencesAccepted", localExecutionEnabled && referenceCode is null && shape.Accepted && TrustedExecution(shape.Definition!), false, referenceCode is null));
    });
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> Create(IdentityPrincipal actor, AutomationDefinitionCommand command, string key, string? trace) => Mutate(actor, null, null, command, key, trace);
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> Save(IdentityPrincipal actor, Guid id, string? etag, AutomationDefinitionCommand command, string key, string? trace) => Mutate(actor, id, etag, command, key, trace);
    private IdentityOperationResult<AutomationDefinitionAcknowledgement> Mutate(IdentityPrincipal actor, Guid? id, string? etag, AutomationDefinitionCommand body, string key, string? trace, string? actionOverride = null) => Safe(() =>
    {
        var action = actionOverride ?? (id is null ? "automation.definition.create" : "automation.definition.save");
        if (!Guid.TryParse(key, out var uuid) || uuid == Guid.Empty) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyKeyRequired", 422, "Supply a nonempty UUID.");
        if (id is not null && string.IsNullOrWhiteSpace(etag)) return Fail<AutomationDefinitionAcknowledgement>("PreconditionRequired", 428, "If-Match is required.");
        if (id is not null && !ValidETag(etag)) return Fail<AutomationDefinitionAcknowledgement>("ValidationFailed", 422, "Supply one valid ETag.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<AutomationDefinitionAcknowledgement>(); }
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        var item = id is { } existing ? Read(c, tx, actor.OwnerId, existing, true) : null;
        if (id is not null && item is null) return Missing<AutomationDefinitionAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, body }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyConflict", 409, "The key belongs to a different command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationDefinitionAcknowledgement>(saved) ?? throw new InvalidOperationException("Receipt integrity.");
            if (Read(c, tx, actor.OwnerId, ack.Id, false) is null || (id is not null && ack.Id != id)) return Missing<AutomationDefinitionAcknowledgement>();
            if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(ack, claim.ResultStatusCode ?? 200);
        }
        if (!claim.IsClaimed) return Fail<AutomationDefinitionAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (!AutomationDefinition.TryTitle(body.Title, out var title) || !AutomationDefinitionContract.Parse(body.DefinitionJson).Accepted)
            return Fail<AutomationDefinitionAcknowledgement>("ValidationFailed", 422, "Check the title and registered definition shape.");
        if (action == "automation.definition.import")
        {
            var targetCode = CheckTargets(c, tx, actor, AutomationDefinitionContract.Parse(body.DefinitionJson).Definition!);
            if (targetCode is not null) return Fail<AutomationDefinitionAcknowledgement>(targetCode, 422, "Rebind targets to permitted active Projects and their time bounds before import.");
        }
        if (item is not null && item.Lifecycle != "Active") return Fail<AutomationDefinitionAcknowledgement>("LifecycleLocked", 409, "Restore before editing.");
        if (item is not null && item.ETag != etag) return Fail<AutomationDefinitionAcknowledgement>("RevisionConflict", 412, "Compare the current definition before saving.");
        if (item is not null && !AutomationDefinition.CanSave(item.State, item.Enabled)) return Fail<AutomationDefinitionAcknowledgement>("LifecycleLocked", 409, "Disable before editing.");
        var definitionId = id ?? Guid.NewGuid(); var number = (item?.CurrentVersion ?? 0) + 1;
        if (id is null)
        {
            using var resource = Command(c, tx, "INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId) SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX34' AND rt.Code='AutomationDefinition' AND rt.ContractVersion='automation-definition-v1';", actor.OwnerId);
            Add(resource, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (resource.ExecuteNonQuery() != 1) throw new InvalidOperationException("Resource integrity.");
            using var insert = Command(c, tx, "INSERT [automation].[Definition](Id,OwnerId,Title,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Title,@User,@User);", actor.OwnerId);
            Add(insert, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(insert, "@Title", SqlDbType.NVarChar, title, 200); Add(insert, "@User", SqlDbType.UniqueIdentifier, actor.UserId); insert.ExecuteNonQuery();
        }
        using (var version = Command(c, tx, "INSERT [automation].[DefinitionVersion](Id,OwnerId,DefinitionId,VersionNumber,Title,SchemaVersion,DefinitionJson,CreatedByUserId) VALUES(NEWID(),@Owner,@Id,@Version,@Title,1,@Json,@User);", actor.OwnerId))
        {
            Add(version, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(version, "@Version", SqlDbType.BigInt, number); Add(version, "@Title", SqlDbType.NVarChar, title, 200); Add(version, "@Json", SqlDbType.NVarChar, body.DefinitionJson, -1); Add(version, "@User", SqlDbType.UniqueIdentifier, actor.UserId); version.ExecuteNonQuery();
        }
        using (var update = Command(c, tx, "UPDATE [automation].[Definition] SET Title=@Title,CurrentVersion=@Version,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id; UPDATE [platform].[Resource] SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Active' AND PurgedAt IS NULL;", actor.OwnerId))
        { Add(update, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(update, "@Version", SqlDbType.BigInt, number); Add(update, "@Title", SqlDbType.NVarChar, title, 200); Add(update, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (update.ExecuteNonQuery() != 2) throw new InvalidOperationException("Pointer integrity."); }
        var updated = Read(c, tx, actor.OwnerId, definitionId, false) ?? throw new InvalidOperationException("Definition integrity.");
        var result = new AutomationDefinitionAcknowledgement(definitionId, number, updated.ETag);
        using (var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'automation.Definition',@Id,'Succeeded',@Trace);", actor.OwnerId))
        { Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery(); }
        receipts.Complete(c, tx, claim, "DefinitionSaved", id is null ? 201 : 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(result, id is null ? 201 : 200);
    });
    private static bool ValidETag(string? value)
    {
        if (value is null || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
        try { var bytes = Convert.FromBase64String(value[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value[1..^1]; }
        catch (FormatException) { return false; }
    }
    private static AutomationDefinitionRecord? Read(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool update)
    {
        using var cmd = Command(c, tx, $"SELECT {Columns} FROM [automation].[Definition] d WITH({(update ? "UPDLOCK,HOLDLOCK" : "HOLDLOCK")}) WHERE d.OwnerId=@Owner AND d.Id=@Id AND d.CurrentVersion IS NOT NULL AND {ResourcePredicate};", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).SingleOrDefault();
    }
    private static List<AutomationDefinitionRecord> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var result = new List<AutomationDefinitionRecord>();
        while (r.Read()) result.Add(new(r.GetGuid(0), r.GetString(1), r.GetInt64(2), r.GetString(3), r.GetBoolean(4), Utc(r.GetDateTime(5)), '"' + Convert.ToBase64String(r.GetFieldValue<byte[]>(6)) + '"', r.GetString(7), r.IsDBNull(8) ? null : r.GetGuid(8))); return result;
    }
    private static List<AutomationDefinitionVersion> Versions(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, long before, int take)
    {
        using var cmd = Command(c, tx, "SELECT TOP(@Take) VersionNumber,Title,DefinitionJson,CreatedAt FROM [automation].[DefinitionVersion] WITH(HOLDLOCK) WHERE OwnerId=@Owner AND DefinitionId=@Id AND VersionNumber<@Before ORDER BY VersionNumber DESC;", owner);
        Add(cmd, "@Take", SqlDbType.Int, take); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Before", SqlDbType.BigInt, before);
        using var r = cmd.ExecuteReader(); var result = new List<AutomationDefinitionVersion>();
        while (r.Read()) result.Add(new(id, r.GetInt64(0), r.GetString(1), r.GetString(2), Utc(r.GetDateTime(3)))); return result;
    }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
}
