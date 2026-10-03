using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Career;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Career;

/// <summary>Manual owner-scoped companies, jobs, immutable events and confirmed lifecycle.</summary>
public sealed class SqlCareerService : ICareerService
{
    public static readonly string[] Actions = ["career.company.read", "career.company.create", "career.company.update", "career.company.merge",
        "career.job.read", "career.job.create", "career.job.update", "career.job.transition", "career.job.history", "career.job.trash", "career.job.restore", "career.job.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly TimeProvider clock;
    private readonly byte[] previewKey;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string CompanyColumns = "Id,Title,Url,Industry,Location,Notes,MergedIntoId,CreatedAt,UpdatedAt,RowVersion";
    private const string JobColumns = "Id,Title,CompanyId,Url,Location,WorkMode,EmploymentType,SalaryText,SalaryMin,SalaryMax,Currency,Description,Notes,Source,DiscoveredOn,AppliedOn,Stage,StageChangedAt,IsTrash,CreatedAt,UpdatedAt,RowVersion,PreTrashStage,TrashBatchId,TrashFingerprint";
    private const string EventColumns = "Id,VersionNumber,ActionKey,FromStage,ToStage,OccurredAt,CreatedByUserId,Note,CompanyLabelSnapshot,SnapshotJson";
    public SqlCareerService(SqlConnectionFactory connections, string secret, TimeProvider clock)
    { this.connections = connections; this.clock = clock; capabilities = new(connections); receipts = new(secret); previewKey = SHA256.HashData(Encoding.UTF8.GetBytes("career-merge-v1:" + secret)); }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => capabilities.IsAllowed(c, tx, actor, "FX39", action);
    private bool Required(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => Allowed(c, tx, actor, action) &&
        (action != "career.company.update" || Allowed(c, tx, actor, "career.company.read")) &&
        (action != "career.job.update" || Allowed(c, tx, actor, "career.job.read"));
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");
    private static IdentityOperationResult<T> Invalid<T>() => Fail<T>("ValidationFailed", 422, "Check the supported fields and explicit choices.");
    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
    // Serialize owner mutations and lock current session/account authority with their data.
    private bool Authority(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor)
    {
        using var cmd = Command(c, tx, """
            SELECT p.Id FROM [platform].[PersonalSpace] p WITH(UPDLOCK,HOLDLOCK)
            JOIN [identity].[User] u ON u.Id=p.UserId
            JOIN [identity].[Session] s ON s.UserId=u.Id AND s.Id=@Session
            WHERE p.Id=@Owner AND p.UserId=@User AND p.State='Active' AND u.State='Active' AND u.IsDeleted=0
            AND s.RevokedAt IS NULL AND s.IdleExpiresAt>@Now AND s.AbsoluteExpiresAt>@Now AND s.SecurityStamp=u.SecurityStamp;
            """, actor.OwnerId);
        Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(cmd, "@Session", SqlDbType.UniqueIdentifier, actor.SessionId);
        Add(cmd, "@Now", SqlDbType.DateTime2, Now()); return cmd.ExecuteScalar() is not null;
    }
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor)) return Denied<IReadOnlyDictionary<string, bool>>();
        var values = Actions.ToDictionary(a => a, a => Required(c, tx, actor, a));
        return values.Values.Any(v => v) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(values) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<CompanyPage> Companies(IdentityPrincipal actor, Guid? cursor, string? query, bool? eligibleOnly)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.company.read")) return Denied<CompanyPage>();
        if (query is { Length: > 200 }) return Invalid<CompanyPage>();
        using var cmd = Command(c, tx, $"""
            WITH selected AS (SELECT {CompanyColumns} FROM [career].[Company] WHERE OwnerId=@Owner
                AND (@Eligible=0 OR MergedIntoId IS NULL) AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0 OR CHARINDEX(@Query,Location)>0))
            SELECT TOP(26) {CompanyColumns},CASE WHEN @JobRead=1 THEN (SELECT COUNT(*) FROM [career].[JobApplication] j WHERE j.OwnerId=@Owner AND j.CompanyId=selected.Id) ELSE NULL END
            FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.Title>boundary.Title OR (selected.Title=boundary.Title AND selected.Id>boundary.Id))) ORDER BY Title,Id;
            """, actor.OwnerId);
        Add(cmd, "@Eligible", SqlDbType.Bit, eligibleOnly == true); Add(cmd, "@Query", SqlDbType.NVarChar, CareerInput.Optional(query), 200);
        Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor); Add(cmd, "@JobRead", SqlDbType.Bit, Allowed(c, tx, actor, "career.job.read"));
        var items = CompanyRows(cmd); return IdentityOperationResult<CompanyPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<CareerCompany> Company(IdentityPrincipal actor, Guid id)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.company.read")) return Denied<CareerCompany>();
        var item = ReadCompany(c, tx, actor.OwnerId, id, Allowed(c, tx, actor, "career.job.read"));
        return item is null ? Missing<CareerCompany>() : IdentityOperationResult<CareerCompany>.Success(item);
    }
    public IdentityOperationResult<CareerAcknowledgement> CreateCompany(IdentityPrincipal actor, CompanyChange body, string key, string? trace) =>
        Mutate(actor, "career.company.create", body, key, trace, (c, tx, now) =>
        {
            if (!CareerInput.Company(body.Metadata, out var m)) return Invalid<CareerAcknowledgement>();
            var id = Guid.NewGuid(); if (!InsertResource(c, tx, actor, id, "Company", now)) return Dependency();
            using var cmd = Command(c, tx, """
                INSERT [career].[Company](Id,OwnerId,Title,Url,Industry,Location,Notes,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Url,@Industry,@Location,@Notes,@Now,@Now,@User,@User);
                """, actor.OwnerId);
            CompanyFields(cmd, actor, id, m, now); cmd.ExecuteNonQuery(); return Ack(c, tx, actor, id, false, 201);
        });
    public IdentityOperationResult<CareerAcknowledgement> UpdateCompany(IdentityPrincipal actor, Guid id, string? etag, CompanyChange body, string key, string? trace) =>
        Mutate(actor, "career.company.update", new { id, etag, body }, key, trace, (c, tx, now) =>
        {
            var item = ReadCompany(c, tx, actor.OwnerId, id); if (item is null) return Missing<CareerAcknowledgement>();
            var pre = Precondition<CareerAcknowledgement>(item.ETag, etag); if (pre is not null) return pre;
            if (item.MergedIntoId is not null) return Locked();
            if (!CareerInput.Company(body.Metadata, out var m)) return Invalid<CareerAcknowledgement>();
            using var cmd = Command(c, tx, """
                UPDATE [career].[Company] SET Title=@Title,Url=@Url,Industry=@Industry,Location=@Location,Notes=@Notes,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            CompanyFields(cmd, actor, id, m, now); cmd.ExecuteNonQuery(); TouchResource(c, tx, actor, id, "Active", now); return Ack(c, tx, actor, id, false);
        });
    public IdentityOperationResult<JobPage> Jobs(IdentityPrincipal actor, Guid? cursor, string? state, string? stage, Guid? companyId,
        string? query, string? location, string? dateField, DateOnly? from, DateOnly? to)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.job.read")) return Denied<JobPage>();
        state ??= "Active";
        if (state is not ("Active" or "Trash") || (stage is not null && !CareerInput.ValidStage(stage)) || query is { Length: > 200 } || location is { Length: > 200 } ||
            dateField is not (null or "Discovered" or "Applied") || ((from is not null || to is not null) && dateField is null) || (from is not null && to is not null && from >= to)) return Invalid<JobPage>();
        var sourceRead = Allowed(c, tx, actor, "career.company.read");
        using var cmd = Command(c, tx, $"""
            WITH selected AS (SELECT {JobColumns} FROM [career].[JobApplication] j WHERE OwnerId=@Owner AND IsTrash=@Trash
              AND (@Stage IS NULL OR Stage=@Stage) AND (@Company IS NULL OR CompanyId=@Company)
              AND (@Location IS NULL OR CHARINDEX(@Location,Location)>0)
              AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0 OR (@CompanyRead=1 AND EXISTS(SELECT 1 FROM [career].[Company] co WHERE co.OwnerId=@Owner AND co.Id=j.CompanyId AND CHARINDEX(@Query,co.Title)>0)))
              AND (@From IS NULL OR CASE WHEN @DateField='Discovered' THEN DiscoveredOn ELSE AppliedOn END>=@From)
              AND (@To IS NULL OR CASE WHEN @DateField='Discovered' THEN DiscoveredOn ELSE AppliedOn END<@To))
            SELECT TOP(26) {JobColumns},CASE WHEN @CompanyRead=1 THEN (SELECT Title FROM [career].[Company] co WHERE co.OwnerId=@Owner AND co.Id=selected.CompanyId) ELSE NULL END
            FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
              AND (selected.UpdatedAt<boundary.UpdatedAt OR (selected.UpdatedAt=boundary.UpdatedAt AND selected.Id<boundary.Id))) ORDER BY UpdatedAt DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor); Add(cmd, "@Trash", SqlDbType.Bit, state == "Trash"); Add(cmd, "@Stage", SqlDbType.VarChar, stage, 64);
        Add(cmd, "@Company", SqlDbType.UniqueIdentifier, companyId); Add(cmd, "@CompanyRead", SqlDbType.Bit, sourceRead);
        Add(cmd, "@Query", SqlDbType.NVarChar, CareerInput.Optional(query), 200); Add(cmd, "@Location", SqlDbType.NVarChar, CareerInput.Optional(location), 200);
        Add(cmd, "@DateField", SqlDbType.VarChar, dateField, 64); Add(cmd, "@From", SqlDbType.Date, from?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@To", SqlDbType.Date, to?.ToDateTime(TimeOnly.MinValue));
        var items = JobRows(cmd).Select(r => r.Item).ToArray(); return IdentityOperationResult<JobPage>.Success(new(items.Take(25).ToArray(), items.Length > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<CareerJob> Job(IdentityPrincipal actor, Guid id)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.job.read")) return Denied<CareerJob>();
        var item = ReadJob(c, tx, actor.OwnerId, id, Allowed(c, tx, actor, "career.company.read"));
        return item is null ? Missing<CareerJob>() : IdentityOperationResult<CareerJob>.Success(item.Item);
    }
    public IdentityOperationResult<CareerAcknowledgement> CreateJob(IdentityPrincipal actor, JobCreate body, string key, string? trace) =>
        Mutate(actor, "career.job.create", body, key, trace, (c, tx, now) =>
        {
            if (!CareerInput.Job(body.Metadata, out var m) || !CareerInput.ValidStage(body.Stage)) return Invalid<CareerAcknowledgement>();
            var association = Association(c, tx, actor, m.CompanyId, null); if (association is not null) return association;
            var id = Guid.NewGuid(); if (!InsertResource(c, tx, actor, id, "JobApplication", now)) return Dependency();
            using var cmd = Command(c, tx, """
                INSERT [career].[JobApplication](Id,OwnerId,Title,CompanyId,Url,Location,WorkMode,EmploymentType,SalaryText,SalaryMin,SalaryMax,Currency,Description,Notes,Source,DiscoveredOn,AppliedOn,Stage,StageChangedAt,IsTrash,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Company,@Url,@Location,@WorkMode,@EmploymentType,@SalaryText,@Min,@Max,@Currency,@Description,@Notes,@Source,@Discovered,@Applied,@Stage,@Now,0,@Now,@Now,@User,@User);
                """, actor.OwnerId);
            JobFields(cmd, actor, id, m, now); Add(cmd, "@Stage", SqlDbType.VarChar, body.Stage, 64); cmd.ExecuteNonQuery();
            AppendEvent(c, tx, actor, id, "career.job.create", null, null, now); return Ack(c, tx, actor, id, true, 201);
        }, () => body.Metadata?.CompanyId is not null);
    public IdentityOperationResult<CareerAcknowledgement> UpdateJob(IdentityPrincipal actor, Guid id, string? etag, JobChange body, string key, string? trace)
    {
        var sourceReadRequired = false;
        return Mutate(actor, "career.job.update", new { id, etag, body }, key, trace, (c, tx, now) =>
        {
            var row = ReadJob(c, tx, actor.OwnerId, id); if (row is null) return Missing<CareerAcknowledgement>();
            var pre = Precondition<CareerAcknowledgement>(row.Item.ETag, etag); if (pre is not null) return pre;
            if (row.Item.IsTrash) return Locked(); if (!CareerInput.Job(body.Metadata, out var m)) return Invalid<CareerAcknowledgement>();
            sourceReadRequired = m.CompanyId is not null && m.CompanyId != row.Item.Metadata.CompanyId;
            var association = Association(c, tx, actor, m.CompanyId, row.Item.Metadata.CompanyId); if (association is not null) return association;
            using var cmd = Command(c, tx, """
                UPDATE [career].[JobApplication] SET Title=@Title,CompanyId=@Company,Url=@Url,Location=@Location,WorkMode=@WorkMode,EmploymentType=@EmploymentType,
                  SalaryText=@SalaryText,SalaryMin=@Min,SalaryMax=@Max,Currency=@Currency,Description=@Description,Notes=@Notes,Source=@Source,DiscoveredOn=@Discovered,AppliedOn=@Applied,
                  UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            JobFields(cmd, actor, id, m, now); cmd.ExecuteNonQuery(); TouchResource(c, tx, actor, id, "Active", now);
            AppendEvent(c, tx, actor, id, "career.job.update", row.Item.Stage, null, now); return Ack(c, tx, actor, id, true);
        }, () => sourceReadRequired);
    }
    public IdentityOperationResult<CareerAcknowledgement> ChangeStage(IdentityPrincipal actor, Guid id, string? etag, JobStageChange body, string key, string? trace) =>
        Mutate(actor, "career.job.transition", new { id, etag, body }, key, trace, (c, tx, now) =>
        {
            var row = ReadJob(c, tx, actor.OwnerId, id); if (row is null) return Missing<CareerAcknowledgement>();
            var pre = Precondition<CareerAcknowledgement>(row.Item.ETag, etag); if (pre is not null) return pre;
            if (row.Item.IsTrash) return Locked();
            if (!CareerInput.ValidStage(body.Stage) || body.Stage == row.Item.Stage || body.Reason is { Length: > 2000 } || !body.Confirm ||
                (CareerInput.ReturnToProgress(row.Item.Stage, body.Stage) && (!body.ConfirmReturnToProgress || string.IsNullOrWhiteSpace(body.Reason)))) return Invalid<CareerAcknowledgement>();
            using var cmd = Command(c, tx, "UPDATE [career].[JobApplication] SET Stage=@Stage,StageChangedAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
            Common(cmd, actor, id, now); Add(cmd, "@Stage", SqlDbType.VarChar, body.Stage, 64); cmd.ExecuteNonQuery(); TouchResource(c, tx, actor, id, "Active", now);
            AppendEvent(c, tx, actor, id, "career.job.transition", row.Item.Stage, CareerInput.Optional(body.Reason), now); return Ack(c, tx, actor, id, true);
        });
    private IdentityOperationResult<CareerAcknowledgement>? Association(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid? selected, Guid? previous)
    {
        if (selected is null || selected == previous) return null;
        if (!Allowed(c, tx, actor, "career.company.read")) return Denied<CareerAcknowledgement>();
        var company = ReadCompany(c, tx, actor.OwnerId, selected.Value);
        return company is null ? Missing<CareerAcknowledgement>() : company.MergedIntoId is not null ? Locked() : null;
    }
    public IdentityOperationResult<JobHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor, string? action, string? from, string? to)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.job.history")) return Denied<JobHistoryPage>();
        if (action is not null && action is not ("career.job.create" or "career.job.update" or "career.job.transition" or "career.job.trash" or "career.job.restore" or "career.company.merge")) return Invalid<JobHistoryPage>();
        if (!CareerInput.Instant(from, out var start) || !CareerInput.Instant(to, out var end) || (start is not null && end is not null && start >= end)) return Invalid<JobHistoryPage>();
        if (ReadJob(c, tx, actor.OwnerId, id) is null) return Missing<JobHistoryPage>();
        var companyRead = Allowed(c, tx, actor, "career.company.read");
        using var cmd = Command(c, tx, $"""
            WITH selected AS (SELECT {EventColumns} FROM [career].[ApplicationEvent] WHERE OwnerId=@Owner AND ApplicationId=@Id
              AND (@Action IS NULL OR ActionKey=@Action) AND (@From IS NULL OR OccurredAt>=@From) AND (@To IS NULL OR OccurredAt<@To))
            SELECT TOP(26) {EventColumns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
              AND (selected.VersionNumber<boundary.VersionNumber OR (selected.VersionNumber=boundary.VersionNumber AND selected.Id<boundary.Id))) ORDER BY VersionNumber DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Action", SqlDbType.VarChar, action, 160); Add(cmd, "@From", SqlDbType.DateTime2, start); Add(cmd, "@To", SqlDbType.DateTime2, end);
        using var r = cmd.ExecuteReader(); var items = new List<JobEvent>();
        while (r.Read())
        {
            var snapshot = JsonSerializer.Deserialize<JobSnapshot>(r.GetString(9), Json) ?? throw new InvalidOperationException("Job event integrity failed.");
            if (!companyRead) snapshot = snapshot with { Fields = snapshot.Fields with { CompanyLabelSnapshot = null } };
            items.Add(new(r.GetGuid(0), r.GetInt64(1), r.GetString(2), Text(r, 3), r.GetString(4), Utc(r.GetDateTime(5)), r.IsDBNull(6) ? null : r.GetGuid(6), Text(r, 7), snapshot));
        }
        return IdentityOperationResult<JobHistoryPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<JobPreview> PreviewJob(IdentityPrincipal actor, Guid id, string operation)
    {
        if (operation is not ("trash" or "restore" or "purge")) return Invalid<JobPreview>();
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.job." + operation)) return Denied<JobPreview>();
        var row = ReadJob(c, tx, actor.OwnerId, id); if (row is null) return Missing<JobPreview>();
        if (row.Item.IsTrash != (operation != "trash")) return Fail<JobPreview>("LifecycleLocked", 409, "The lifecycle does not allow this operation.");
        return IdentityOperationResult<JobPreview>.Success(new(id, row.Item.ETag, operation, ReferenceCount(c, tx, actor.OwnerId, id)));
    }
    public IdentityOperationResult<CareerAcknowledgement> Lifecycle(IdentityPrincipal actor, Guid id, string? etag, string operation, CareerConfirmation body, string key, string? trace)
    {
        if (operation is not ("trash" or "restore" or "purge")) return Invalid<CareerAcknowledgement>();
        return Mutate(actor, "career.job." + operation, new { id, etag, operation, body }, key, trace, (c, tx, now) =>
        {
            var row = ReadJob(c, tx, actor.OwnerId, id); if (row is null) return Missing<CareerAcknowledgement>();
            var pre = Precondition<CareerAcknowledgement>(row.Item.ETag, etag); if (pre is not null) return pre;
            if (!body.Confirm) return Invalid<CareerAcknowledgement>(); if (row.Item.IsTrash != (operation != "trash")) return Locked();
            if (operation != "trash" && (row.Fingerprint is null || !CryptographicOperations.FixedTimeEquals(row.Fingerprint, Fingerprint(c, tx, actor.OwnerId, row)))) return Locked();
            if (operation == "purge" && ReferenceCount(c, tx, actor.OwnerId, id) != 0) return Fail<CareerAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
            var batch = Guid.NewGuid();
            using (var cohort = Command(c, tx, operation == "trash" ? """
                INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Batch,@Owner,@Id,@Now,'Trashed',@Now,@User,@User);
                INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(NEWID(),@Owner,@Batch,@Id,@Previous,0,@Now,@User,@User); SELECT 1;
                """ : """
                IF NOT EXISTS(SELECT 1 FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id
                  WHERE b.OwnerId=@Owner AND b.Id=@Batch AND b.RootResourceId=@Id AND b.State='Trashed' AND m.ResourceId=@Id
                  AND m.PreviousLifecycle=@Previous AND m.Depth=0 AND m.ParentResourceId IS NULL AND m.PurgedAt IS NULL)
                  OR (SELECT COUNT(*) FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=@Batch)<>1 SELECT 0;
                ELSE BEGIN UPDATE [operations].[TrashBatch] SET State=CASE WHEN @Operation='restore' THEN 'Restored' ELSE 'Purged' END,
                    RestoredAt=CASE WHEN @Operation='restore' THEN @Now ELSE NULL END,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Batch;
                  IF @Operation='purge' UPDATE [operations].[TrashMember] SET PurgedAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND BatchId=@Batch AND ResourceId=@Id;
                  SELECT 1; END;
                """, actor.OwnerId))
            {
                Common(cohort, actor, id, now); Add(cohort, "@Batch", SqlDbType.UniqueIdentifier, operation == "trash" ? batch : row.TrashBatch);
                Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? row.Item.Stage : row.PreTrashStage, 64); Add(cohort, "@Operation", SqlDbType.VarChar, operation, 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1) return Locked();
            }
            TouchResource(c, tx, actor, id, operation == "trash" ? "Trash" : operation == "purge" ? "Purged" : "Active", now);
            if (operation == "trash")
            {
                AppendEvent(c, tx, actor, id, "career.job.trash", row.Item.Stage, null, now, row.Item.Stage);
                var frozen = Fingerprint(c, tx, actor.OwnerId, row with { PreTrashStage = row.Item.Stage });
                using var cmd = Command(c, tx, "UPDATE [career].[JobApplication] SET IsTrash=1,PreTrashStage=Stage,TrashBatchId=@Batch,TrashFingerprint=@Fingerprint,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
                Common(cmd, actor, id, now); Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch); Add(cmd, "@Fingerprint", SqlDbType.Binary, frozen, 32); cmd.ExecuteNonQuery();
            }
            else if (operation == "restore")
            {
                using var cmd = Command(c, tx, "UPDATE [career].[JobApplication] SET IsTrash=0,PreTrashStage=NULL,TrashBatchId=NULL,TrashFingerprint=NULL,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
                Common(cmd, actor, id, now); cmd.ExecuteNonQuery(); AppendEvent(c, tx, actor, id, "career.job.restore", row.Item.Stage, null, now);
            }
            else
            {
                using var cmd = Command(c, tx, "DELETE [career].[ApplicationEvent] WHERE OwnerId=@Owner AND ApplicationId=@Id; DELETE [career].[JobApplication] WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
                Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); cmd.ExecuteNonQuery(); return IdentityOperationResult<CareerAcknowledgement>.Success(new(id, null));
            }
            return Ack(c, tx, actor, id, true);
        });
    }
    private sealed record PreviewStamp(Guid UserId, Guid OwnerId, Guid? SessionId, Guid SourceId, Guid TargetId, string SourceETag, string TargetETag, string Digest, int Jobs, int References, DateTimeOffset ExpiresAt);
    private sealed record CohortEntry(Guid Id, string ETag, string? Title);
    private sealed record MergeCohort(CareerCompany Source, CareerCompany Target, IReadOnlyList<CohortEntry> Jobs, IReadOnlyList<CohortEntry> References, string Digest);
    private IdentityOperationResult<MergeCohort> Cohort(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid sourceId, Guid targetId)
    {
        if (sourceId == targetId || sourceId == Guid.Empty || targetId == Guid.Empty) return Invalid<MergeCohort>();
        var source = ReadCompany(c, tx, actor.OwnerId, sourceId); var target = ReadCompany(c, tx, actor.OwnerId, targetId);
        if (source is null || target is null) return Missing<MergeCohort>();
        if (source.MergedIntoId is not null || target.MergedIntoId is not null) return Fail<MergeCohort>("LifecycleLocked", 409, "Choose two unmerged companies.");
        var jobs = new List<CohortEntry>(); var refs = new List<CohortEntry>();
        using (var cmd = Command(c, tx, "SELECT TOP(1001) Id,RowVersion,Title,IsTrash FROM [career].[JobApplication] WHERE OwnerId=@Owner AND CompanyId=@Source ORDER BY Id;", actor.OwnerId))
        {
            Add(cmd, "@Source", SqlDbType.UniqueIdentifier, sourceId); using var r = cmd.ExecuteReader();
            while (r.Read()) { if (r.GetBoolean(3)) return Fail<MergeCohort>("LifecycleLocked", 409, "Resolve linked job Trash before merging."); jobs.Add(new(r.GetGuid(0), Tag(r, 1), r.GetString(2))); }
        }
        using (var cmd = Command(c, tx, "SELECT TOP(1001) Id,RowVersion FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND State<>'Detached' AND (SourceResourceId IN (@Source,@Target) OR TargetResourceId IN (@Source,@Target)) ORDER BY Id;", actor.OwnerId))
        {
            Add(cmd, "@Source", SqlDbType.UniqueIdentifier, sourceId); Add(cmd, "@Target", SqlDbType.UniqueIdentifier, targetId);
            using var r = cmd.ExecuteReader(); while (r.Read()) refs.Add(new(r.GetGuid(0), Tag(r, 1), null));
        }
        if (jobs.Count > 1000 || refs.Count > 1000) return Fail<MergeCohort>("ImpactTooLarge", 409, "The merge impact exceeds its bounded contract.");
        var digest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { jobs = jobs.Select(j => new { j.Id, j.ETag }), references = refs.Select(r => new { r.Id, r.ETag }) }, Json)));
        return IdentityOperationResult<MergeCohort>.Success(new(source, target, jobs, refs, digest));
    }
    public IdentityOperationResult<CompanyMergePreview> PreviewMerge(IdentityPrincipal actor, Guid sourceId, Guid targetId)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Allowed(c, tx, actor, "career.company.merge")) return Denied<CompanyMergePreview>();
        var result = Cohort(c, tx, actor, sourceId, targetId); if (!result.Succeeded || result.Value is not { } cohort) return Fail<CompanyMergePreview>(result.Code, result.StatusCode, result.Title);
        var expires = clock.GetUtcNow().AddMinutes(5); var stamp = new PreviewStamp(actor.UserId, actor.OwnerId, actor.SessionId, sourceId, targetId, cohort.Source.ETag, cohort.Target.ETag, cohort.Digest, cohort.Jobs.Count, cohort.References.Count, expires);
        var companyRead = Allowed(c, tx, actor, "career.company.read"); var jobRead = Allowed(c, tx, actor, "career.job.read");
        var preview = new CompanyMergePreview(sourceId, targetId, cohort.Source.ETag, cohort.Target.ETag, companyRead ? cohort.Source.Metadata : null, companyRead ? cohort.Target.Metadata : null,
            cohort.Jobs.Select(j => new MergeImpact(j.Id, j.ETag, jobRead ? j.Title : null)).ToArray(), cohort.References.Select(r => r.Id).ToArray(), expires, Sign(stamp));
        return JsonSerializer.SerializeToUtf8Bytes(preview, Json).Length > 1048576 ? Fail<CompanyMergePreview>("ImpactTooLarge", 409, "The merge preview exceeds its bounded contract.") : IdentityOperationResult<CompanyMergePreview>.Success(preview);
    }
    public IdentityOperationResult<CareerAcknowledgement> Merge(IdentityPrincipal actor, Guid sourceId, CompanyMergeRequest body, string key, string? trace) =>
        Mutate(actor, "career.company.merge", new { sourceId, body }, key, trace, (c, tx, now) =>
        {
            if (!body.Confirm || !body.KeepTargetMetadata) return Invalid<CareerAcknowledgement>();
            var stamp = Verify(body.PreviewToken); if (stamp is null || stamp.UserId != actor.UserId || stamp.OwnerId != actor.OwnerId || stamp.SessionId != actor.SessionId || stamp.SourceId != sourceId || stamp.TargetId != body.TargetId || stamp.ExpiresAt <= Utc(now) || stamp.SourceETag != body.SourceETag || stamp.TargetETag != body.TargetETag)
                return Fail<CareerAcknowledgement>("InvalidPreview", 409, "Request a fresh merge preview.");
            var result = Cohort(c, tx, actor, sourceId, body.TargetId); if (!result.Succeeded || result.Value is not { } cohort) return Fail<CareerAcknowledgement>(result.Code, result.StatusCode, result.Title);
            var pre = Precondition<CareerAcknowledgement>(cohort.Source.ETag, body.SourceETag) ?? Precondition<CareerAcknowledgement>(cohort.Target.ETag, body.TargetETag); if (pre is not null) return pre;
            if (cohort.Digest != stamp.Digest || cohort.Jobs.Count != stamp.Jobs || cohort.References.Count != stamp.References) return Fail<CareerAcknowledgement>("RevisionConflict", 409, "The merge impact changed. Request a fresh preview.");
            foreach (var job in cohort.Jobs)
            {
                var row = ReadJob(c, tx, actor.OwnerId, job.Id)!;
                using var cmd = Command(c, tx, "UPDATE [career].[JobApplication] SET CompanyId=@Target,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
                Common(cmd, actor, job.Id, now); Add(cmd, "@Target", SqlDbType.UniqueIdentifier, body.TargetId); cmd.ExecuteNonQuery(); TouchResource(c, tx, actor, job.Id, "Active", now);
                AppendEvent(c, tx, actor, job.Id, "career.company.merge", row.Item.Stage, null, now);
            }
            using (var cmd = Command(c, tx, "UPDATE [career].[Company] SET MergedIntoId=CASE WHEN Id=@Id THEN @Target ELSE MergedIntoId END,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id IN (@Id,@Target);", actor.OwnerId))
            { Common(cmd, actor, sourceId, now); Add(cmd, "@Target", SqlDbType.UniqueIdentifier, body.TargetId); cmd.ExecuteNonQuery(); }
            TouchResource(c, tx, actor, sourceId, "Active", now); TouchResource(c, tx, actor, body.TargetId, "Active", now); return Ack(c, tx, actor, sourceId, false);
        });
    private string Sign(PreviewStamp stamp)
    {
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(stamp, Json));
        return payload + "." + Convert.ToBase64String(HMACSHA256.HashData(previewKey, Encoding.ASCII.GetBytes(payload)));
    }
    private PreviewStamp? Verify(string? token)
    {
        if (token is null || token.Length > 2048 || token.Any(ch => ch > 127)) return null;
        var parts = token.Split('.'); if (parts.Length != 2) return null;
        try
        {
            var signature = Convert.FromBase64String(parts[1]); var expected = HMACSHA256.HashData(previewKey, Encoding.ASCII.GetBytes(parts[0]));
            return signature.Length == expected.Length && CryptographicOperations.FixedTimeEquals(signature, expected) ? JsonSerializer.Deserialize<PreviewStamp>(Convert.FromBase64String(parts[0]), Json) : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException) { return null; }
    }
    private sealed record ReceiptResult(CareerAcknowledgement Acknowledgement, bool RequiresCompanyRead);
    private IdentityOperationResult<CareerAcknowledgement> Mutate(IdentityPrincipal actor, string action, object request, string key, string? trace,
        Func<SqlConnection, SqlTransaction, DateTime, IdentityOperationResult<CareerAcknowledgement>> work, Func<bool>? requiresCompanyRead = null)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Authority(c, tx, actor) || !Required(c, tx, actor, action)) return Denied<CareerAcknowledgement>();
        if (!Guid.TryParse(key, out var parsed) || parsed == Guid.Empty) return Fail<CareerAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        var now = Now(); var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request, Json), now);
        if (claim.IsConflict) return Fail<CareerAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var receipt = JsonSerializer.Deserialize<ReceiptResult>(saved, Json) ?? throw new InvalidOperationException("Career receipt integrity failed.");
            if (receipt.RequiresCompanyRead && !Allowed(c, tx, actor, "career.company.read")) return Denied<CareerAcknowledgement>();
            return IdentityOperationResult<CareerAcknowledgement>.Success(receipt.Acknowledgement, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        }
        if (!claim.IsClaimed) return Fail<CareerAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx, now); if (!result.Succeeded || result.Value is null) return result;
        if (!Authority(c, tx, actor) || !Required(c, tx, actor, action) || (requiresCompanyRead?.Invoke() == true && !Allowed(c, tx, actor, "career.company.read"))) return Denied<CareerAcknowledgement>();
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,@Type,@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Type", SqlDbType.NVarChar, action.StartsWith("career.company.", StringComparison.Ordinal) ? "career.Company" : "career.JobApplication", 100);
        Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(new ReceiptResult(result.Value, requiresCompanyRead?.Invoke() == true), Json)); tx.Commit(); return result;
    }
    private static IdentityOperationResult<CareerAcknowledgement> Locked() => Fail<CareerAcknowledgement>("LifecycleLocked", 409, "The lifecycle or frozen cohort does not allow this operation.");
    private static IdentityOperationResult<CareerAcknowledgement> Dependency() => Fail<CareerAcknowledgement>("DependencyUnavailable", 409, "The Career resource contract is unavailable.");
    private static IdentityOperationResult<T>? Precondition<T>(string current, string? etag) => string.IsNullOrWhiteSpace(etag) ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") : current == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current revision.");
    private static IdentityOperationResult<CareerAcknowledgement> Ack(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, bool job, int status = 200) =>
        IdentityOperationResult<CareerAcknowledgement>.Success(new(id, job ? ReadJob(c, tx, actor.OwnerId, id)!.Item.ETag : ReadCompany(c, tx, actor.OwnerId, id)!.ETag), status);
    private static bool InsertResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string type, DateTime now)
    {
        using var cmd = Command(c, tx, """
            INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,rt.Id,'Active',1,@Now,@Now,@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
            WHERE m.Code='FX39' AND rt.Code=@Type AND rt.ContractVersion='career-manual-v1';
            """, actor.OwnerId);
        Common(cmd, actor, id, now); Add(cmd, "@Type", SqlDbType.NVarChar, type, 100); return cmd.ExecuteNonQuery() == 1;
    }
    private static void TouchResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string availability, DateTime now)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=@Now,UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN @Now ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
        Common(cmd, actor, id, now); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Career registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar()); }
    private static CareerCompany? ReadCompany(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool jobRead = false)
    {
        using var cmd = Command(c, tx, $"SELECT {CompanyColumns},CASE WHEN @JobRead=1 THEN (SELECT COUNT(*) FROM [career].[JobApplication] j WHERE j.OwnerId=@Owner AND j.CompanyId=co.Id) ELSE NULL END FROM [career].[Company] co WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@JobRead", SqlDbType.Bit, jobRead); return CompanyRows(cmd).FirstOrDefault();
    }
    private static List<CareerCompany> CompanyRows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var result = new List<CareerCompany>();
        while (r.Read()) result.Add(new(r.GetGuid(0), new(r.GetString(1), Text(r, 2), Text(r, 3), Text(r, 4), Text(r, 5)), r.IsDBNull(6) ? null : r.GetGuid(6), r.IsDBNull(10) ? null : r.GetInt32(10), Utc(r.GetDateTime(7)), Utc(r.GetDateTime(8)), Tag(r, 9)));
        return result;
    }
    private sealed record JobRow(CareerJob Item, string? PreTrashStage, Guid? TrashBatch, byte[]? Fingerprint);
    private static JobRow? ReadJob(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, bool companyRead = false)
    {
        using var cmd = Command(c, tx, $"SELECT {JobColumns},CASE WHEN @CompanyRead=1 THEN (SELECT Title FROM [career].[Company] co WHERE co.OwnerId=@Owner AND co.Id=j.CompanyId) ELSE NULL END FROM [career].[JobApplication] j WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@CompanyRead", SqlDbType.Bit, companyRead); return JobRows(cmd).FirstOrDefault();
    }
    private static List<JobRow> JobRows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var result = new List<JobRow>();
        while (r.Read())
        {
            var metadata = new JobMetadata(r.GetString(1), r.IsDBNull(2) ? null : r.GetGuid(2), Text(r, 3), Text(r, 4), Text(r, 5), Text(r, 6), Text(r, 7),
                r.IsDBNull(8) ? null : CareerInput.Exact(r.GetDecimal(8)), r.IsDBNull(9) ? null : CareerInput.Exact(r.GetDecimal(9)), Text(r, 10), Text(r, 11), Text(r, 12), Text(r, 13),
                r.IsDBNull(14) ? null : DateOnly.FromDateTime(r.GetDateTime(14)), r.IsDBNull(15) ? null : DateOnly.FromDateTime(r.GetDateTime(15)));
            result.Add(new(new(r.GetGuid(0), metadata, r.GetString(16), Utc(r.GetDateTime(17)), r.GetBoolean(18), Text(r, 25), Utc(r.GetDateTime(19)), Utc(r.GetDateTime(20)), Tag(r, 21)), Text(r, 22), r.IsDBNull(23) ? null : r.GetGuid(23), r.IsDBNull(24) ? null : r.GetFieldValue<byte[]>(24)));
        }
        return result;
    }
    private static void AppendEvent(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string action, string? from, string? note, DateTime now, string? futurePreTrash = null)
    {
        var row = ReadJob(c, tx, actor.OwnerId, id, true) ?? throw new InvalidOperationException("Job event integrity failed.");
        var snapshot = new JobSnapshot(1, "JobApplication", new(row.Item.Metadata, row.Item.Stage, row.Item.StageChangedAt, futurePreTrash ?? row.PreTrashStage, row.Item.CompanyLabel), [], [], new Dictionary<string, CareerProvenance> { ["metadata"] = new("Manual") });
        using var cmd = Command(c, tx, """
            INSERT [career].[ApplicationEvent](Id,OwnerId,ApplicationId,VersionNumber,ActionKey,FromStage,ToStage,OccurredAt,Note,CompanyLabelSnapshot,SnapshotJson,CreatedAt,CreatedByUserId)
            SELECT NEWID(),@Owner,@Id,Revision,@Action,@From,@To,@Now,@Note,@Label,@Snapshot,@Now,@User FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id;
            """, actor.OwnerId);
        Common(cmd, actor, id, now); Add(cmd, "@Action", SqlDbType.VarChar, action, 160); Add(cmd, "@From", SqlDbType.VarChar, from, 64); Add(cmd, "@To", SqlDbType.VarChar, row.Item.Stage, 64);
        Add(cmd, "@Note", SqlDbType.NVarChar, note, 2000); Add(cmd, "@Label", SqlDbType.NVarChar, row.Item.CompanyLabel, 200); Add(cmd, "@Snapshot", SqlDbType.NVarChar, JsonSerializer.Serialize(snapshot, Json), -1);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Job event integrity failed.");
    }
    private static byte[] Fingerprint(SqlConnection c, SqlTransaction tx, Guid owner, JobRow row)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Append(object value) { var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json); hash.AppendData(BitConverter.GetBytes(bytes.Length)); hash.AppendData(bytes); }
        Append(new { row.Item.Id, row.Item.Metadata, row.Item.Stage, row.Item.StageChangedAt, row.PreTrashStage });
        using (var cmd = Command(c, tx, $"SELECT {EventColumns},CreatedAt,RowVersion FROM [career].[ApplicationEvent] WHERE OwnerId=@Owner AND ApplicationId=@Id ORDER BY VersionNumber,Id;", owner))
        {
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, row.Item.Id); using var r = cmd.ExecuteReader();
            while (r.Read()) Append(new { id = r.GetGuid(0), version = r.GetInt64(1), action = r.GetString(2), from = Text(r, 3), to = r.GetString(4), at = Utc(r.GetDateTime(5)), actor = r.IsDBNull(6) ? (Guid?)null : r.GetGuid(6), note = Text(r, 7), label = Text(r, 8), snapshot = r.GetString(9), createdAt = Utc(r.GetDateTime(10)), revision = Tag(r, 11) });
        }
        using (var cmd = Command(c, tx, "SELECT Id,SourceResourceId,TargetResourceId,RelationType,TargetVersion,State,RowVersion FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached' ORDER BY Id;", owner))
        {
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, row.Item.Id); using var r = cmd.ExecuteReader();
            while (r.Read()) Append(new { id = r.GetGuid(0), source = r.GetGuid(1), target = r.GetGuid(2), relation = r.GetString(3), version = r.IsDBNull(4) ? (long?)null : r.GetInt64(4), state = r.GetString(5), revision = Tag(r, 6) });
        }
        return hash.GetHashAndReset();
    }
    private static void CompanyFields(SqlCommand cmd, IdentityPrincipal actor, Guid id, CompanyMetadata m, DateTime now)
    { Common(cmd, actor, id, now); Add(cmd, "@Title", SqlDbType.NVarChar, m.Title, 200); Add(cmd, "@Url", SqlDbType.NVarChar, m.Url, 2048); Add(cmd, "@Industry", SqlDbType.NVarChar, m.Industry, 200); Add(cmd, "@Location", SqlDbType.NVarChar, m.Location, 200); Add(cmd, "@Notes", SqlDbType.NVarChar, m.Notes, -1); }
    private static void JobFields(SqlCommand cmd, IdentityPrincipal actor, Guid id, JobMetadata m, DateTime now)
    {
        Common(cmd, actor, id, now); Add(cmd, "@Title", SqlDbType.NVarChar, m.Title, 200); Add(cmd, "@Company", SqlDbType.UniqueIdentifier, m.CompanyId); Add(cmd, "@Url", SqlDbType.NVarChar, m.Url, 2048);
        Add(cmd, "@Location", SqlDbType.NVarChar, m.Location, 200); Add(cmd, "@WorkMode", SqlDbType.VarChar, m.WorkMode, 64); Add(cmd, "@EmploymentType", SqlDbType.NVarChar, m.EmploymentType, 100); Add(cmd, "@SalaryText", SqlDbType.NVarChar, m.SalaryText, 1000);
        foreach (var (name, value) in new[] { ("@Min", m.SalaryMin), ("@Max", m.SalaryMax) }) { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 28; p.Scale = 8; p.Value = value is null ? DBNull.Value : decimal.Parse(value, CultureInfo.InvariantCulture); }
        Add(cmd, "@Currency", SqlDbType.Char, m.Currency, 3); Add(cmd, "@Description", SqlDbType.NVarChar, m.Description, -1); Add(cmd, "@Notes", SqlDbType.NVarChar, m.Notes, -1); Add(cmd, "@Source", SqlDbType.NVarChar, m.Source, 200);
        Add(cmd, "@Discovered", SqlDbType.Date, m.DiscoveredOn?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@Applied", SqlDbType.Date, m.AppliedOn?.ToDateTime(TimeOnly.MinValue));
    }
    private static void Common(SqlCommand cmd, IdentityPrincipal actor, Guid id, DateTime now) { Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(cmd, "@Now", SqlDbType.DateTime2, now); }
    private static string? Text(SqlDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static string Tag(SqlDataReader r, int i) => "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(i)) + "\"";
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, Guid owner) { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0) { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
}
