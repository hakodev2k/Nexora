using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Learning;

public sealed class SqlCertificationService : ICertificationService
{
    public static readonly string[] Actions = ["learning.certification.read", "learning.certification.create", "learning.certification.update",
        "learning.certification.renew", "learning.certification.archive", "learning.certification.unarchive"];
    private const string Columns = "w.Id,w.Title,w.Issuer,w.IssuedOn,w.ExpiresOn,w.VerificationUrl,w.Status,w.VersionNumber,w.CreatedAt,w.UpdatedAt,w.RowVersion";
    private const string Registry = """
        EXISTS(SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
        JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=w.OwnerId AND r.Id=w.Id AND m.Code='FX40'
        AND rt.Code='Certification' AND rt.ContractVersion='certifications-manual-v1' AND r.Availability=w.Status)
        """;
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly CertificationTokenCodec tokens;
    private readonly TimeProvider clock;
    private sealed record Boundary(DateOnly SortDate, Guid Id, long Version = 0, bool NonExpiring = false);
    private sealed record Receipt(int Version, Guid Actor, Guid Owner, Guid Id, string Action, int Status);
    private sealed record PreviewProof(Guid Actor, Guid Owner, Guid Id, string ETag, string Operation, string Version, DateTimeOffset ExpiresAt);
    public SqlCertificationService(SqlConnectionFactory connections, string secret, TimeProvider? clock = null)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); tokens = new(secret); this.clock = clock ?? TimeProvider.System; }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, string action) =>
        capabilities.IsAllowed(c, tx, a, "FX40", action) && (action.EndsWith(".create", StringComparison.Ordinal) || action.EndsWith(".read", StringComparison.Ordinal)
            || capabilities.IsAllowed(c, tx, a, "FX40", "learning.certification.read"));

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        var result = Actions.ToDictionary(action => action, action => Allowed(c, tx, actor, action)); tx.Commit();
        return result.Values.Any(x => x) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<CertificationPage> List(IdentityPrincipal actor, DateOnly ownerToday, string? status, string? query, string? cursor)
    {
        status ??= "All"; query = Trim(query);
        if (status is not ("All" or "Active" or "Expired" or "Archived") || query is { Length: > 200 }) return Invalid<CertificationPage>();
        var scope = JsonSerializer.Serialize(new { purpose = "list", actor.UserId, actor.OwnerId, status, query, ownerToday });
        var boundary = Decode<Boundary>(scope, cursor);
        if (cursor is not null && (boundary is null || boundary.Id == Guid.Empty)) return Invalid<CertificationPage>();
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<CertificationPage>();
        using var sql = Command(c, tx, $"""
            SELECT TOP(26) {Columns} FROM [learning].[Certification] w WHERE w.OwnerId=@Owner AND {Registry}
            AND (@Status='All' OR (w.Status='Archived' AND @Status='Archived') OR (w.Status='Active' AND
              ((@Status='Expired' AND w.ExpiresOn<@Today) OR (@Status='Active' AND (w.ExpiresOn IS NULL OR w.ExpiresOn>=@Today)))))
            AND (@Query IS NULL OR CHARINDEX(@Query,w.Title)>0 OR CHARINDEX(@Query,w.Issuer)>0)
            AND (@Before IS NULL OR CASE WHEN w.ExpiresOn IS NULL THEN 1 ELSE 0 END>@BeforeNull
              OR (CASE WHEN w.ExpiresOn IS NULL THEN 1 ELSE 0 END=@BeforeNull AND
                (COALESCE(w.ExpiresOn,CONVERT(date,'99991231'))>@Before OR
                (COALESCE(w.ExpiresOn,CONVERT(date,'99991231'))=@Before AND w.Id>@BeforeId))))
            ORDER BY CASE WHEN w.ExpiresOn IS NULL THEN 1 ELSE 0 END,w.ExpiresOn,w.Id;
            """, actor.OwnerId);
        Add(sql, "@Status", SqlDbType.VarChar, status, 64); Add(sql, "@Query", SqlDbType.NVarChar, query, 200);
        Add(sql, "@Today", SqlDbType.Date, ownerToday.ToDateTime(TimeOnly.MinValue));
        Add(sql, "@Before", SqlDbType.Date, boundary?.SortDate.ToDateTime(TimeOnly.MinValue)); Add(sql, "@BeforeId", SqlDbType.UniqueIdentifier, boundary?.Id);
        Add(sql, "@BeforeNull", SqlDbType.Int, boundary?.NonExpiring == true ? 1 : 0);
        var rows = new List<Certification>(); using (var reader = sql.ExecuteReader()) while (reader.Read()) rows.Add(Row(reader, ownerToday));
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<CertificationPage>(); tx.Commit();
        var next = rows.Count > 25 ? tokens.Protect(scope, JsonSerializer.Serialize(new Boundary(rows[24].ExpiresOn ?? DateOnly.MaxValue, rows[24].Id, NonExpiring: rows[24].ExpiresOn is null))) : null;
        return IdentityOperationResult<CertificationPage>.Success(new(rows.Take(25).ToArray(), next));
    }
    public IdentityOperationResult<Certification> Get(IdentityPrincipal actor, DateOnly ownerToday, Guid id)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<Certification>();
        var item = Read(c, tx, actor.OwnerId, id, ownerToday);
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<Certification>(); tx.Commit();
        return item is null ? Missing<Certification>() : IdentityOperationResult<Certification>.Success(item);
    }
    public IdentityOperationResult<CertificationHistoryPage> History(IdentityPrincipal actor, Guid id, string? cursor)
    {
        var scope = JsonSerializer.Serialize(new { purpose = "history", actor.UserId, actor.OwnerId, id }); var boundary = Decode<Boundary>(scope, cursor);
        if (cursor is not null && (boundary is null || boundary.Version < 1 || boundary.Id == Guid.Empty)) return Invalid<CertificationHistoryPage>();
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<CertificationHistoryPage>();
        if (Read(c, tx, actor.OwnerId, id, DateOnly.MinValue) is null) return Missing<CertificationHistoryPage>();
        using var sql = Command(c, tx, """
            SELECT TOP(26) Id,VersionNumber,Operation,SafeSnapshotJson,Reason,CreatedAt FROM [learning].[CertificationVersion]
            WHERE OwnerId=@Owner AND CertificationId=@Id AND (@Before IS NULL OR VersionNumber<@Before) ORDER BY VersionNumber DESC;
            """, actor.OwnerId);
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Before", SqlDbType.BigInt, boundary?.Version);
        var rows = new List<CertificationVersion>(); using (var r = sql.ExecuteReader()) while (r.Read())
            rows.Add(new(r.GetGuid(0), r.GetInt64(1).ToString(CultureInfo.InvariantCulture), r.GetString(2), JsonSerializer.Deserialize<CertificationSnapshot>(r.GetString(3))!, Text(r, 4), Utc(r.GetDateTime(5))));
        if (!Allowed(c, tx, actor, "learning.certification.read")) return Denied<CertificationHistoryPage>(); tx.Commit();
        return IdentityOperationResult<CertificationHistoryPage>.Success(new(rows.Take(25).ToArray(), rows.Count > 25 ?
            tokens.Protect(scope, JsonSerializer.Serialize(new Boundary(DateOnly.MinValue, rows[24].Id, long.Parse(rows[24].VersionNumber, CultureInfo.InvariantCulture)))) : null));
    }
    public IdentityOperationResult<object?> Create(IdentityPrincipal actor, CertificationCreate body, string key, string? trace)
    {
        if (!CertificationRules.ValidMetadata(body.Title, body.Issuer, body.VerificationUrl) || !CertificationRules.ValidDates(body.IssuedOn, body.ExpiresOn)) return Invalid<object?>();
        return Write(actor, null, null, "create", body with { Title = body.Title.Trim(), Issuer = body.Issuer.Trim(), VerificationUrl = Trim(body.VerificationUrl) }, key, trace);
    }
    public IdentityOperationResult<object?> Update(IdentityPrincipal actor, Guid id, string? etag, CertificationMetadata body, string key, string? trace)
    {
        if (!CertificationRules.ValidMetadata(body.Title, body.Issuer, body.VerificationUrl)) return Invalid<object?>();
        return Write(actor, id, etag, "update", body with { Title = body.Title.Trim(), Issuer = body.Issuer.Trim(), VerificationUrl = Trim(body.VerificationUrl) }, key, trace);
    }
    public IdentityOperationResult<object?> Renew(IdentityPrincipal actor, Guid id, string? etag, CertificationRenewal body, string key, string? trace)
    {
        if (!CertificationRules.ValidDates(body.IssuedOn, body.ExpiresOn) || body.Reason is { Length: > 2000 }) return Invalid<object?>();
        return Write(actor, id, etag, "renew", body with { Reason = Trim(body.Reason) }, key, trace);
    }
    public IdentityOperationResult<CertificationPreview> Preview(IdentityPrincipal actor, Guid id, string operation)
    {
        if (operation is not ("archive" or "unarchive")) return Invalid<CertificationPreview>();
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "learning.certification." + operation)) return Denied<CertificationPreview>();
        var row = Read(c, tx, actor.OwnerId, id, DateOnly.MinValue); if (row is null) return Missing<CertificationPreview>();
        if (row.Status != (operation == "archive" ? "Active" : "Archived")) return Locked<CertificationPreview>();
        var expires = clock.GetUtcNow().AddMinutes(5); var proof = new PreviewProof(actor.UserId, actor.OwnerId, id, row.ETag, operation, row.VersionNumber, expires);
        if (!Allowed(c, tx, actor, "learning.certification." + operation)) return Denied<CertificationPreview>(); tx.Commit();
        return IdentityOperationResult<CertificationPreview>.Success(new(id, row.ETag, operation, row.VersionNumber, expires,
            tokens.Protect("preview", JsonSerializer.Serialize(proof))));
    }
    public IdentityOperationResult<object?> Transition(IdentityPrincipal actor, Guid id, string? etag, string operation, CertificationTransition body, string key, string? trace)
    {
        if (operation is not ("archive" or "unarchive") || !body.Confirm) return Invalid<object?>();
        return Write(actor, id, etag, operation, body, key, trace);
    }
    private IdentityOperationResult<object?> Write(IdentityPrincipal actor, Guid? id, string? etag, string operation, object body, string key, string? trace)
    {
        if (!Guid.TryParseExact(key, "D", out var parsed) || parsed == Guid.Empty) return Invalid<object?>();
        if (id.HasValue && string.IsNullOrEmpty(etag)) return Fail<object?>("PreconditionRequired", 428, "If-Match is required.");
        var action = "learning.certification." + operation; var status = id.HasValue ? 200 : 201;
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
            { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<object?>(); }
            if (!Allowed(c, tx, actor, action)) return Denied<object?>();
            var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(new { version = 1, actor.OwnerId, id, etag, operation, body }), clock.GetUtcNow().UtcDateTime);
            if (claim.IsConflict) return Fail<object?>("IdempotencyConflict", 409, "This key belongs to a different request.");
            if (claim.IsReplay)
            {
                var saved = claim.ResultJson is null ? null : JsonSerializer.Deserialize<Receipt>(claim.ResultJson);
                if (saved is null || saved.Version != 1 || saved.Actor != actor.UserId || saved.Owner != actor.OwnerId || saved.Id == Guid.Empty ||
                    (id.HasValue && saved.Id != id.Value) || saved.Action != action || saved.Status != status || claim.ResultStatusCode != status || claim.ResultCode != "CertificationCommitted")
                    return Fail<object?>("CommittedOutcomeUnavailable", 409, "Reconcile the original outcome before starting a new command.");
                if (!Allowed(c, tx, actor, action)) return Denied<object?>();
                return IdentityOperationResult<object?>.Committed(saved.Id, status, "CertificationCommitted");
            }
            if (!claim.IsClaimed) return Fail<object?>("RequestInProgress", 409, "The original request is in progress.");
            if (body is CertificationTransition transition)
            {
                var proof = Decode<PreviewProof>("preview", transition.PreviewToken);
                if (proof is null || proof.Actor != actor.UserId || proof.Owner != actor.OwnerId || proof.Id != id || proof.ETag != etag || proof.Operation != operation
                    || proof.ExpiresAt != transition.ExpiresAt || proof.ExpiresAt <= clock.GetUtcNow() || proof.ExpiresAt > clock.GetUtcNow().AddMinutes(5))
                    return Fail<object?>("PreviewStale", 409, "Refresh the lifecycle preview.");
            }
            var itemId = id ?? Guid.NewGuid(); var current = id.HasValue ? Read(c, tx, actor.OwnerId, itemId, DateOnly.MinValue) : null;
            if (id.HasValue && current is null) return Missing<object?>();
            if (current is not null && current.ETag != etag) return Fail<object?>("RevisionConflict", 412, "Reload the current certification.");
            if (current is not null && (operation is "update" or "renew") && current.Status != "Active") return Locked<object?>();
            if (operation is "archive" or "unarchive" && current!.Status != (operation == "archive" ? "Active" : "Archived")) return Locked<object?>();
            var title = body switch { CertificationCreate b => b.Title, CertificationMetadata b => b.Title, _ => current!.Title };
            var issuer = body switch { CertificationCreate b => b.Issuer, CertificationMetadata b => b.Issuer, _ => current!.Issuer };
            var url = body switch { CertificationCreate b => b.VerificationUrl, CertificationMetadata b => b.VerificationUrl, _ => current!.VerificationUrl };
            var issued = body switch { CertificationCreate b => b.IssuedOn, CertificationRenewal b => b.IssuedOn, _ => current?.IssuedOn };
            var expiry = body switch { CertificationCreate b => b.ExpiresOn, CertificationRenewal b => b.ExpiresOn, _ => current?.ExpiresOn };
            var state = operation == "archive" ? "Archived" : "Active";
            if (current?.VersionNumber == long.MaxValue.ToString(CultureInfo.InvariantCulture)) return Fail<object?>("VersionLimit", 409, "The version limit is reached.");
            if (id is null)
            {
                using var resource = Command(c, tx, """
                    INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                    JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX40' AND rt.Code='Certification' AND rt.ContractVersion='certifications-manual-v1';
                    """, actor.OwnerId);
                Add(resource, "@Id", SqlDbType.UniqueIdentifier, itemId); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
                if (resource.ExecuteNonQuery() != 1) return Fail<object?>("DependencyUnavailable", 409, "The certification resource contract is unavailable.");
            }
            using (var sql = Command(c, tx, id.HasValue ? """
                UPDATE [learning].[Certification] SET Title=@Title,Issuer=@Issuer,IssuedOn=@Issued,ExpiresOn=@Expiry,VerificationUrl=@Url,
                    Status=@State,PreArchiveState=CASE WHEN @State='Archived' THEN 'Active' ELSE NULL END,VersionNumber=VersionNumber+1,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND RowVersion=@Version;
                """ : """
                INSERT [learning].[Certification](Id,OwnerId,Title,Issuer,IssuedOn,ExpiresOn,VerificationUrl,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Issuer,@Issued,@Expiry,@Url,@User,@User);
                """, actor.OwnerId))
            {
                Add(sql, "@Id", SqlDbType.UniqueIdentifier, itemId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
                Add(sql, "@Title", SqlDbType.NVarChar, title, 200); Add(sql, "@Issuer", SqlDbType.NVarChar, issuer, 200); Add(sql, "@Url", SqlDbType.NVarChar, url, 2048);
                Add(sql, "@Issued", SqlDbType.Date, issued?.ToDateTime(TimeOnly.MinValue)); Add(sql, "@Expiry", SqlDbType.Date, expiry?.ToDateTime(TimeOnly.MinValue));
                if (id.HasValue) { Add(sql, "@State", SqlDbType.VarChar, state, 64); Add(sql, "@Version", SqlDbType.Binary, ParseTag(etag!), 8); }
                if (sql.ExecuteNonQuery() != 1) return Fail<object?>("RevisionConflict", 412, "Reload the current certification.");
            }
            if (id.HasValue)
            {
                using var resource = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@State,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
                Add(resource, "@State", SqlDbType.VarChar, state, 64); Add(resource, "@Id", SqlDbType.UniqueIdentifier, itemId); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (resource.ExecuteNonQuery() != 1) return Missing<object?>();
            }
            var after = Read(c, tx, actor.OwnerId, itemId, DateOnly.MinValue)!;
            using (var history = Command(c, tx, """
                INSERT [learning].[CertificationVersion](Id,OwnerId,CertificationId,VersionNumber,Operation,SafeSnapshotJson,Reason,CreatedByUserId)
                VALUES(NEWID(),@Owner,@Id,@Number,@Operation,@Snapshot,@Reason,@User);
                """, actor.OwnerId))
            {
                Add(history, "@Id", SqlDbType.UniqueIdentifier, itemId); Add(history, "@Number", SqlDbType.BigInt, long.Parse(after.VersionNumber, CultureInfo.InvariantCulture));
                Add(history, "@Operation", SqlDbType.VarChar, operation switch { "create" => "Created", "update" => "Updated", "renew" => "Renewed", "archive" => "Archived", _ => "Unarchived" }, 32);
                Add(history, "@Snapshot", SqlDbType.NVarChar, JsonSerializer.Serialize(new CertificationSnapshot(1, title, issuer, issued, expiry, url, state)), -1);
                Add(history, "@Reason", SqlDbType.NVarChar, (body as CertificationRenewal)?.Reason, 2000); Add(history, "@User", SqlDbType.UniqueIdentifier, actor.UserId); history.ExecuteNonQuery();
            }
            using (var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.Certification',@Id,'Succeeded',@Trace);", actor.OwnerId))
            { Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, itemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery(); }
            receipts.Complete(c, tx, claim, "CertificationCommitted", status, JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, itemId, action, status)));
            if (!Allowed(c, tx, actor, action)) return Denied<object?>(); tx.Commit();
            return IdentityOperationResult<object?>.Committed(itemId, status, "CertificationCommitted");
        }
        catch (SqlException) { return Fail<object?>("PersistenceUnavailable", 503, "Certification persistence is unavailable."); }
        catch (JsonException) { return Fail<object?>("CommittedOutcomeUnavailable", 409, "Reconcile the original outcome before retrying."); }
    }
    private T? Decode<T>(string scope, string? token) where T : class
    { try { var value = token is null ? null : tokens.Unprotect(scope, token); return value is null ? null : JsonSerializer.Deserialize<T>(value); } catch (JsonException) { return null; } }
    private static Certification? Read(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, DateOnly today)
    { using var sql = Command(c, tx, $"SELECT {Columns} FROM [learning].[Certification] w WHERE w.OwnerId=@Owner AND w.Id=@Id AND {Registry};", owner); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); using var r = sql.ExecuteReader(); return r.Read() ? Row(r, today) : null; }
    private static Certification Row(SqlDataReader r, DateOnly today) => new(r.GetGuid(0), r.GetString(1), r.GetString(2), Day(r, 3), Day(r, 4), Text(r, 5), r.GetString(6),
        CertificationRules.EffectiveStatus(r.GetString(6), Day(r, 4), today), r.GetInt64(7).ToString(CultureInfo.InvariantCulture), Utc(r.GetDateTime(8)), Utc(r.GetDateTime(9)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(10)) + "\"");
    private static byte[]? ParseTag(string value)
    { try { if (value.Length != 14 || value[0] != '"' || value[^1] != '"') return null; var bytes = Convert.FromBase64String(value[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value[1..^1] ? bytes : null; } catch (FormatException) { return null; } }
    private static DateOnly? Day(SqlDataReader r, int n) => r.IsDBNull(n) ? null : DateOnly.FromDateTime(r.GetDateTime(n));
    private static string? Text(SqlDataReader r, int n) => r.IsDBNull(n) ? null : r.GetString(n);
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, string text, Guid owner)
    { var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = text; Add(sql, "@Owner", SqlDbType.UniqueIdentifier, owner); return sql; }
    private static void Add(SqlCommand sql, string name, SqlDbType type, object? value, int? size = null)
    { var p = size.HasValue ? sql.Parameters.Add(name, type, size.Value) : sql.Parameters.Add(name, type); p.Value = value ?? DBNull.Value; }
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The certification is unavailable.");
    private static IdentityOperationResult<T> Locked<T>() => Fail<T>("LifecycleLocked", 409, "Unarchive the certification before editing or renewing.");
    private static IdentityOperationResult<T> Invalid<T>() => Fail<T>("ValidationFailed", 422, "Check certification fields, dates and filters.");
}
