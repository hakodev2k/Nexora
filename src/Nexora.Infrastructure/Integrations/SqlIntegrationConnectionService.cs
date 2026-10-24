using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Integrations;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
namespace Nexora.Infrastructure.Integrations;

public sealed class SqlIntegrationConnectionService : IIntegrationConnectionService
{
    private static readonly string[] Actions = ["integrations.connection.read", "integrations.connection.create", "integrations.connection.update", "integrations.connection.disable"];
    private const string Columns = "c.Id,c.ProviderKey,c.ConnectorVersion,c.Title,c.Endpoint,c.State,c.UpdatedAt,c.LastTestAt,c.RowVersion";
    private const string ResourcePredicate = "EXISTS(SELECT 1 FROM [platform].[Resource] r WITH(HOLDLOCK) JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=c.OwnerId AND r.Id=c.Id AND r.Availability='Active' AND r.PurgedAt IS NULL AND rt.Code='IntegrationConnection' AND rt.ContractVersion='integration-connection-v1' AND m.Code='FX35')";
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly IntegrationConnectionCursor cursors;
    public SqlIntegrationConnectionService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); cursors = new(secret); }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => Actions.Contains(action, StringComparer.Ordinal) &&
        SqlCurrentActor.IsLive(c, tx, actor) && capabilities.IsAllowed(c, tx, actor, "FX35", action, Actions[0]);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource or continuation is unavailable.");
    private static IdentityOperationResult<T> Safe<T>(Func<IdentityOperationResult<T>> work)
    {
        try { return work(); }
        catch (SqlException e) when (e.Number == 1205) { return Fail<T>("ConcurrencyConflict", 409, "Retry the same command."); }
        catch (SqlException) { return Fail<T>("StorageUnavailable", 503, "Connection storage is unavailable."); }
        catch (InvalidOperationException) { return Fail<T>("StorageUnavailable", 503, "Connection storage is unavailable."); }
    }
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IReadOnlyDictionary<string, bool>>();
        var result = Actions.ToDictionary(x => x, x => Allowed(c, tx, actor, x));
        foreach (var x in new[] { "integrations.connection.scopes", "integrations.connection.credential", "integrations.connection.test", "integrations.webhook.create", "integrations.worker.send", "integrations.worker.receive" }) result[x] = false;
        tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
    });
    public IdentityOperationResult<IReadOnlyList<ConnectorManifest>> Connectors(IdentityPrincipal actor) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IReadOnlyList<ConnectorManifest>>();
        tx.Commit(); return IdentityOperationResult<IReadOnlyList<ConnectorManifest>>.Success(ConnectorRegistry.Describe());
    });
    public IdentityOperationResult<IntegrationConnectionPage> List(IdentityPrincipal actor, string? cursor, string? query, string? provider, string? state) => Safe(() =>
    {
        query = query?.Trim() ?? ""; provider ??= ""; state ??= "";
        if (query.Length > 200 || query.Any(char.IsControl) || (provider != "" && provider != "n8n") || (state != "" && state != "Disabled"))
            return Fail<IntegrationConnectionPage>("ValidationFailed", 422, "Check name/provider/state filters.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IntegrationConnectionPage>();
        var identity = JsonSerializer.Serialize(new { query, provider, state }); string boundaryTitle = ""; Guid boundary = default;
        if (cursor is not null && !cursors.Decode(cursor, actor.OwnerId, identity, out boundaryTitle, out boundary)) return Missing<IntegrationConnectionPage>();
        var predicate = $"c.OwnerId=@Owner AND {ResourcePredicate} AND(@Query='' OR CHARINDEX(@Query,c.Title)>0) AND(@Provider='' OR c.ProviderKey=@Provider) AND(@State='' OR c.State=@State)";
        void Bind(SqlCommand cmd)
        { Add(cmd, "@Query", SqlDbType.NVarChar, query, 200); Add(cmd, "@Provider", SqlDbType.NVarChar, provider, 150); Add(cmd, "@State", SqlDbType.VarChar, state, 64); Add(cmd, "@Boundary", SqlDbType.UniqueIdentifier, boundary); Add(cmd, "@Title", SqlDbType.NVarChar, boundaryTitle, 200); }
        if (cursor is not null)
        {
            using var anchor = Command(c, tx, $"SELECT 1 FROM [automation].[Connection] c WHERE {predicate} AND c.Id=@Boundary AND c.Title=@Title;", actor.OwnerId); Bind(anchor);
            if (anchor.ExecuteScalar() is null) return Missing<IntegrationConnectionPage>();
        }
        using var cmd = Command(c, tx, $"SELECT TOP(26) {Columns} FROM [automation].[Connection] c WITH(HOLDLOCK) WHERE {predicate} AND(@First=1 OR c.Title>@Title OR(c.Title=@Title AND c.Id>@Boundary)) ORDER BY c.Title,c.Id;", actor.OwnerId);
        Bind(cmd); Add(cmd, "@First", SqlDbType.Bit, cursor is null); var items = Rows(cmd);
        var next = items.Count > 25 ? cursors.Encode(actor.OwnerId, identity, items[24].Title, items[24].Id) : null;
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IntegrationConnectionPage>();
        tx.Commit(); return IdentityOperationResult<IntegrationConnectionPage>.Success(new(items.Take(25).ToArray(), next));
    });
    public IdentityOperationResult<IntegrationConnectionRecord> Get(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IntegrationConnectionRecord>();
        var item = Read(c, tx, actor.OwnerId, id, false); if (item is null) return Missing<IntegrationConnectionRecord>();
        if (!Allowed(c, tx, actor, Actions[0])) return Denied<IntegrationConnectionRecord>();
        tx.Commit(); return IdentityOperationResult<IntegrationConnectionRecord>.Success(item);
    });
    public IdentityOperationResult<IntegrationConnectionAcknowledgement> Create(IdentityPrincipal actor, IntegrationConnectionCommand body, string key, string? trace) => Mutate(actor, Actions[1], null, null, body, key, trace);
    public IdentityOperationResult<IntegrationConnectionAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, IntegrationConnectionCommand body, string key, string? trace) => Mutate(actor, Actions[2], id, etag, body, key, trace);
    public IdentityOperationResult<IntegrationConnectionAcknowledgement> Disable(IdentityPrincipal actor, Guid id, string? etag, IntegrationDisableCommand body, string key, string? trace) => Mutate(actor, Actions[3], id, etag, body, key, trace);
    private IdentityOperationResult<IntegrationConnectionAcknowledgement> Mutate(IdentityPrincipal actor, string action, Guid? id, string? etag, object body, string key, string? trace) => Safe(() =>
    {
        if (!Guid.TryParse(key, out var uuid) || uuid == Guid.Empty) return Fail<IntegrationConnectionAcknowledgement>("IdempotencyKeyRequired", 422, "Supply a nonempty UUID.");
        if (id is not null && !ValidETag(etag)) return Fail<IntegrationConnectionAcknowledgement>("PreconditionRequired", string.IsNullOrEmpty(etag) ? 428 : 422, "Supply a valid If-Match revision.");
        IntegrationConnectionCommand? normalized = null;
        if (body is IntegrationConnectionCommand fields)
        { if (!IntegrationConnectionInput.Normalize(fields, out normalized)) return Fail<IntegrationConnectionAcknowledgement>("ValidationFailed", 422, "Select registered connector v1, name1..200, and HTTPS443 service origin without path/query/credentials."); }
        else if (body is not IntegrationDisableCommand { Confirmed: true }) return Fail<IntegrationConnectionAcknowledgement>("ConfirmationRequired", 422, "Confirm disabling this connection.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<IntegrationConnectionAcknowledgement>(); }
        if (!Allowed(c, tx, actor, action)) return Denied<IntegrationConnectionAcknowledgement>();
        var item = id is { } source ? Read(c, tx, actor.OwnerId, source, true) : null;
        if (id is not null && item is null) return Missing<IntegrationConnectionAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, body }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<IntegrationConnectionAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<IntegrationConnectionAcknowledgement>(saved) ?? throw new InvalidOperationException();
            if ((id is not null && ack.Id != id) || Read(c, tx, actor.OwnerId, ack.Id, false) is null) return Missing<IntegrationConnectionAcknowledgement>();
            if (!Allowed(c, tx, actor, action)) return Denied<IntegrationConnectionAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<IntegrationConnectionAcknowledgement>.Success(ack, id is null ? 201 : 200);
        }
        if (!claim.IsClaimed) return Fail<IntegrationConnectionAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (item is not null && item.ETag != etag) return Fail<IntegrationConnectionAcknowledgement>("RevisionConflict", 412, "Compare the current revision.");
        var target = id ?? Guid.NewGuid();
        if (id is null)
        {
            using var resource = Command(c, tx, "INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId) SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX35' AND rt.Code='IntegrationConnection' AND rt.ContractVersion='integration-connection-v1';", actor.OwnerId);
            Add(resource, "@Id", SqlDbType.UniqueIdentifier, target); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (resource.ExecuteNonQuery() != 1) throw new InvalidOperationException();
        }
        else if (normalized is not null && (normalized.ProviderKey != item!.ProviderKey || normalized.ConnectorVersion != item.ConnectorVersion))
            return Fail<IntegrationConnectionAcknowledgement>("ConnectorImmutable", 422, "Create a new connection for another provider or contract version.");
        using (var write = Command(c, tx, id is null
            ? "INSERT [automation].[Connection](Id,OwnerId,ProviderKey,ConnectorVersion,Title,Endpoint,ScopesJson,State,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Provider,@Version,@Title,@Endpoint,N'{\"schemaVersion\":1,\"inbound\":[],\"outbound\":[]}','Disabled',@User,@User);"
            : normalized is not null
            ? "UPDATE [automation].[Connection] SET Title=@Title,Endpoint=@Endpoint,State='Disabled',AuthorityRevision=AuthorityRevision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;"
            : "UPDATE [automation].[Connection] SET State='Disabled',AuthorityRevision=AuthorityRevision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId))
        {
            Add(write, "@Id", SqlDbType.UniqueIdentifier, target); Add(write, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            if (normalized is not null) { Add(write, "@Provider", SqlDbType.NVarChar, normalized.ProviderKey, 150); Add(write, "@Version", SqlDbType.Int, normalized.ConnectorVersion); Add(write, "@Title", SqlDbType.NVarChar, normalized.Title, 200); Add(write, "@Endpoint", SqlDbType.NVarChar, normalized.Endpoint, 2048); }
            if (write.ExecuteNonQuery() != 1) throw new InvalidOperationException();
        }
        if (id is not null)
        { using var advance = Command(c, tx, "UPDATE [platform].[Resource] SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Active' AND PurgedAt IS NULL;", actor.OwnerId); Add(advance, "@Id", SqlDbType.UniqueIdentifier, target); Add(advance, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (advance.ExecuteNonQuery() != 1) throw new InvalidOperationException(); }
        using (var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'automation.Connection',@Id,'Succeeded',@Trace);", actor.OwnerId))
        { Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, target); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery(); }
        var result = new IntegrationConnectionAcknowledgement(target);
        receipts.Complete(c, tx, claim, "ConnectionConfigurationSaved", id is null ? 201 : 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, action) || Read(c, tx, actor.OwnerId, target, false) is null) return Denied<IntegrationConnectionAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<IntegrationConnectionAcknowledgement>.Success(result, id is null ? 201 : 200);
    });
    private static bool ValidETag(string? value)
    { if (value is null || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false; try { var bytes = Convert.FromBase64String(value[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value[1..^1]; } catch (FormatException) { return false; } }
    private static IntegrationConnectionRecord? Read(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool update)
    { using var cmd = Command(c, tx, $"SELECT {Columns} FROM [automation].[Connection] c WITH({(update ? "UPDLOCK,HOLDLOCK" : "HOLDLOCK")}) WHERE c.OwnerId=@Owner AND c.Id=@Id AND {ResourcePredicate};", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).SingleOrDefault(); }
    private static List<IntegrationConnectionRecord> Rows(SqlCommand cmd)
    { using var r = cmd.ExecuteReader(); var result = new List<IntegrationConnectionRecord>(); while (r.Read()) result.Add(new(r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetString(3), r.GetString(4), r.GetString(5), Utc(r.GetDateTime(6)), r.IsDBNull(7) ? null : Utc(r.GetDateTime(7)), '"' + Convert.ToBase64String(r.GetFieldValue<byte[]>(8)) + '"')); return result; }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var parameter = cmd.Parameters.Add(name, type); if (size != 0) parameter.Size = size; parameter.Value = value ?? DBNull.Value; }
}
