using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Assets;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Assets;

/// <summary>Owner-controlled metadata, non-loan states and immutable private history.</summary>
public sealed class SqlDigitalAssetService
    : IDigitalAssetService
{
    public static readonly string[] Actions = ["digital.asset.read", "digital.asset.create", "digital.asset.update",
        "digital.asset.cancel", "digital.asset.history", "digital.renewal.record", "digital.asset.archive", "digital.asset.unarchive",
        "digital.asset.trash", "digital.asset.restore", "digital.asset.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly TimeProvider time;
    private const string Columns = """
        a.Id,a.Title,a.Kind,a.Provider,a.ExpiresOn,a.Cost,a.Currency,a.RenewalCycle,a.Notes,a.State,a.CreatedAt,a.UpdatedAt,a.RowVersion,
        CASE a.Kind
        WHEN 'Domain' THEN (SELECT d.AsciiName AS [name],d.Registrar AS [registrar],d.AutoRenewRecorded AS [autoRenewRecorded],d.RegisteredOn AS [registeredOn],d.NameserverNotes AS [nameserverNotes] FROM [assets].[DomainDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        WHEN 'Hosting' THEN (SELECT d.[Plan] AS [plan],d.Region AS [region],d.ControlPanelUrl AS [controlPanelUrl],CONVERT(varchar(19),d.StorageLimitBytes) AS [storageLimitBytes],d.DomainName AS [domainName] FROM [assets].[HostingDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        WHEN 'Vps' THEN (SELECT d.HostName AS [hostName],d.IpAddress AS [ipAddress],d.CpuCount AS [cpuCount],CONVERT(varchar(19),d.MemoryMiB) AS [memoryMiB],d.OperatingSystem AS [operatingSystem],d.[Plan] AS [plan],d.Region AS [region] FROM [assets].[VpsDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        WHEN 'Certificate' THEN (SELECT d.Subject AS [subject],d.Issuer AS [issuer],d.Fingerprint AS [fingerprint],CONVERT(varchar(33),d.NotBefore,126)+'Z' AS [notBefore],CONVERT(varchar(33),d.NotAfter,126)+'Z' AS [notAfter],d.HostName AS [hostName],JSON_QUERY(d.SubjectAlternativeNamesJson) AS [subjectAlternativeNames] FROM [assets].[CertificateDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        WHEN 'License' THEN (SELECT d.Product AS [product],d.Seats AS [seats],d.PurchasedOn AS [purchasedOn],d.Vendor AS [vendor],d.Edition AS [edition] FROM [assets].[LicenseDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        WHEN 'OnlineService' THEN (SELECT d.ServiceUrl AS [serviceUrl],d.[Plan] AS [plan] FROM [assets].[ServiceDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)
        END AS DetailJson,
        (SELECT AsciiName FROM [assets].[DomainDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id) AS AsciiName,
        (SELECT UnicodeName FROM [assets].[DomainDetail] d WHERE d.OwnerId=a.OwnerId AND d.DigitalAssetId=a.Id) AS UnicodeName
        """;
    public SqlDigitalAssetService(SqlConnectionFactory connections, string secret, TimeProvider time)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); this.time = time; }
    private sealed record OwnerClock(string Iana, string Windows, DateTimeOffset Now, DateOnly Today);
    private OwnerClock Clock(SqlConnection c, SqlTransaction? tx, IdentityPrincipal actor)
    {
        // Identity-owned current profile projection, constrained to the validated personal owner and actor.
        using var cmd = Command(c, tx, "SELECT u.TimeZoneId FROM [identity].[User] u JOIN [platform].[PersonalSpace] p ON p.UserId=u.Id WHERE p.Id=@Owner AND u.Id=@User AND u.IsDeleted=0 AND u.State='Active';", actor.OwnerId);
        Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
        if (cmd.ExecuteScalar() is not string iana || !TimeZoneInfo.TryConvertIanaIdToWindowsId(iana, out var windows))
            throw new InvalidOperationException("Current owner timezone is unavailable.");
        var now=time.GetUtcNow(); var zone=TimeZoneInfo.FindSystemTimeZoneById(iana);
        return new(iana,windows,now,DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now,zone).DateTime));
    }
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX38", action);
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        capabilities.IsAllowed(c, tx, actor, "FX38", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(action => action, action => RequiredAllowed(actor, action));
        return result.Values.Any(value => value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result)
            : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<DigitalAssetPage> List(IdentityPrincipal actor, Guid? cursor, string? state,
        string? kind, string? query, string? expiry, DateOnly? from, DateOnly? to)
    {
        if (!Allowed(actor,"digital.asset.read")) return Denied<DigitalAssetPage>();
        state ??= "Active"; expiry ??= "All";
        if (state is not ("Active" or "Expired" or "Canceled" or "Archived" or "Trash") ||
            (kind is not null && !DigitalAssetInput.Kind(kind)) || query is { Length: > 200 } || expiry is not ("All" or "Known" or "Unknown") ||
            (from is not null && to is not null && from>=to)) return Fail<DigitalAssetPage>("ValidationFailed",422,"Check asset filters and expiry date range.");
        using var c=connections.Create(); c.Open(); var clock=Clock(c,null,actor);
        using var cmd=Command(c,null,$"""
            WITH basis AS (
                SELECT a.Id,a.OwnerId,a.Title,a.Kind,a.ExpiresOn,cert.NotAfter,dom.AsciiName,dom.UnicodeName,
                  CASE WHEN cert.NotAfter IS NOT NULL THEN cert.NotAfter
                    WHEN a.ExpiresOn='9999-12-31' THEN CONVERT(datetime2(7),'9999-12-31T23:59:59.9999999')
                    WHEN a.ExpiresOn IS NOT NULL THEN CONVERT(datetime2(7), SWITCHOFFSET(
                      DATEADD(day,1,CONVERT(datetime2(7),CASE WHEN a.ExpiresOn='9999-12-31' THEN CONVERT(date,'9999-12-30') ELSE a.ExpiresOn END))
                      AT TIME ZONE @Zone,'+00:00')) ELSE NULL END AS EffectiveExpiry,
                  CASE WHEN a.State IN ('Archived','Trash','Canceled') THEN a.State
                    WHEN cert.NotAfter IS NOT NULL AND @Now>=cert.NotAfter THEN 'Expired'
                    WHEN cert.NotAfter IS NULL AND a.ExpiresOn IS NOT NULL AND @Today>a.ExpiresOn THEN 'Expired' ELSE 'Active' END AS CurrentState,
                  a.ExpiresOn AS EnteredFilterDate
                FROM [assets].[DigitalAsset] a
                LEFT JOIN [assets].[CertificateDetail] cert ON cert.OwnerId=a.OwnerId AND cert.DigitalAssetId=a.Id
                LEFT JOIN [assets].[DomainDetail] dom ON dom.OwnerId=a.OwnerId AND dom.DigitalAssetId=a.Id
                WHERE a.OwnerId=@Owner), selected AS (
                SELECT *,CASE WHEN EffectiveExpiry IS NULL THEN 1 ELSE 0 END AS UnknownExpiry FROM basis
                WHERE CurrentState=@State AND (@Kind IS NULL OR Kind=@Kind)
                  AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0 OR CHARINDEX(@Query,AsciiName)>0 OR CHARINDEX(@Query,UnicodeName)>0)
                  AND (@Expiry='All' OR (@Expiry='Known' AND EffectiveExpiry IS NOT NULL) OR (@Expiry='Unknown' AND EffectiveExpiry IS NULL))
                  AND ((NotAfter IS NOT NULL AND (@From IS NULL OR NotAfter>=@InstantFrom) AND (@To IS NULL OR NotAfter<@InstantTo)) OR
                    (NotAfter IS NULL AND (@From IS NULL OR EnteredFilterDate>=@From) AND (@To IS NULL OR EnteredFilterDate<@To)))), page AS (
                SELECT TOP(26) * FROM selected s WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor AND
                  (s.UnknownExpiry>b.UnknownExpiry OR (s.UnknownExpiry=b.UnknownExpiry AND
                    (s.EffectiveExpiry>b.EffectiveExpiry OR ((s.EffectiveExpiry=b.EffectiveExpiry OR (s.EffectiveExpiry IS NULL AND b.EffectiveExpiry IS NULL)) AND
                      (s.Title>b.Title OR (s.Title=b.Title AND s.Id>b.Id)))))))
                ORDER BY UnknownExpiry,EffectiveExpiry,Title,Id)
            SELECT {Columns} FROM page p JOIN [assets].[DigitalAsset] a ON a.OwnerId=p.OwnerId AND a.Id=p.Id
            ORDER BY p.UnknownExpiry,p.EffectiveExpiry,p.Title,p.Id;
            """,actor.OwnerId);
        Add(cmd,"@Cursor",SqlDbType.UniqueIdentifier,cursor); Add(cmd,"@State",SqlDbType.VarChar,state,64); Add(cmd,"@Kind",SqlDbType.VarChar,kind,64);
        Add(cmd,"@Query",SqlDbType.NVarChar,Blank(query),200); Add(cmd,"@Expiry",SqlDbType.VarChar,expiry,16);
        Add(cmd,"@Zone",SqlDbType.NVarChar,clock.Windows,128); Add(cmd,"@Now",SqlDbType.DateTime2,clock.Now.UtcDateTime);
        Add(cmd,"@Today",SqlDbType.Date,clock.Today.ToDateTime(TimeOnly.MinValue));
        Add(cmd,"@From",SqlDbType.Date,from?.ToDateTime(TimeOnly.MinValue)); Add(cmd,"@To",SqlDbType.Date,to?.ToDateTime(TimeOnly.MinValue));
        Add(cmd,"@InstantFrom",SqlDbType.DateTime2,from is { } start ? LocalDayUtc(start,clock.Iana) : null);
        Add(cmd,"@InstantTo",SqlDbType.DateTime2,to is { } end ? LocalDayUtc(end,clock.Iana) : null);
        var items=Rows(cmd,clock); return IdentityOperationResult<DigitalAssetPage>.Success(new(items.Take(25).ToArray(),items.Count>25?items[24].Id:null));
    }
    public IdentityOperationResult<DigitalAsset> Get(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor,"digital.asset.read")) return Denied<DigitalAsset>();
        using var c=connections.Create(); c.Open(); var item=Read(c,null,actor.OwnerId,id,Clock(c,null,actor));
        return item is null ? Missing<DigitalAsset>() : IdentityOperationResult<DigitalAsset>.Success(item);
    }
    public IdentityOperationResult<DigitalAssetAcknowledgement> Create(IdentityPrincipal actor, DigitalAssetCreate body, string key, string? trace) =>
        Mutate(actor,"digital.asset.create",body,key,trace,(c,tx,clock)=>
        {
            if (!Operational(body.State) || !DigitalAssetInput.Normalize(body.Metadata,out var metadata))
                return Fail<DigitalAssetAcknowledgement>("ValidationFailed",422,"Choose explicit kind/state and valid matching typed metadata.");
            var id=Guid.NewGuid();
            using var resource=Command(c,tx,"""
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
                WHERE m.Code='FX38' AND rt.Code='DigitalAsset' AND rt.ContractVersion='digital-assets-v1';
                """,actor.OwnerId);
            Add(resource,"@Id",SqlDbType.UniqueIdentifier,id); Add(resource,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
            if(resource.ExecuteNonQuery()!=1) return Fail<DigitalAssetAcknowledgement>("DependencyUnavailable",409,"The asset resource contract is unavailable.");
            using var cmd=Command(c,tx,"""
                INSERT [assets].[DigitalAsset](Id,OwnerId,Title,Kind,Provider,ExpiresOn,Cost,Currency,RenewalCycle,Notes,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Kind,@Provider,@ExpiresOn,@Cost,@Currency,@Cycle,@Notes,@State,SYSUTCDATETIME(),@User,@User);
                """,actor.OwnerId);
            Fields(cmd,actor,id,metadata!); Add(cmd,"@State",SqlDbType.VarChar,body.State,64); cmd.ExecuteNonQuery();
            SaveDetail(c,tx,actor,id,metadata!); return Ack(c,tx,actor,id,clock,201);
        });
    public IdentityOperationResult<DigitalAssetAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, DigitalAssetUpdate body, string key, string? trace) =>
        Mutate(actor,"digital.asset.update",new{id,etag,body},key,trace,(c,tx,clock)=>
        {
            var item=Read(c,tx,actor.OwnerId,id,clock); if(item is null) return Missing<DigitalAssetAcknowledgement>();
            var stale=Precondition<DigitalAssetAcknowledgement>(item,etag); if(stale is not null) return stale;
            if(!Operational(item.StoredState)) return Fail<DigitalAssetAcknowledgement>("LifecycleLocked",409,"Restore or unarchive before editing.");
            if(!DigitalAssetInput.Normalize(body.Metadata,out var metadata)) return Fail<DigitalAssetAcknowledgement>("ValidationFailed",422,"Check the matching typed metadata.");
            if(item.Metadata.Kind!=metadata!.Kind)
            {
                if(!body.ConfirmTypeChange) return Fail<DigitalAssetAcknowledgement>("TypeChangeConfirmationRequired",422,"Confirm replacement of current type fields; prior fields remain in history.");
                DeleteDetail(c,tx,actor.OwnerId,id,item.Metadata.Kind);
            }
            using var cmd=Command(c,tx,"""
                UPDATE [assets].[DigitalAsset] SET Title=@Title,Kind=@Kind,Provider=@Provider,ExpiresOn=@ExpiresOn,Cost=@Cost,Currency=@Currency,RenewalCycle=@Cycle,
                  Notes=@Notes,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """,actor.OwnerId);
            Fields(cmd,actor,id,metadata); cmd.ExecuteNonQuery(); SaveDetail(c,tx,actor,id,metadata); UpdateResource(c,tx,actor,id,"Active");
            return Ack(c,tx,actor,id,clock);
        });
    public IdentityOperationResult<DigitalAssetAcknowledgement> RecordRenewal(IdentityPrincipal actor, Guid id, string? etag, DigitalAssetRenewal body, string key, string? trace) =>
        Mutate(actor,"digital.renewal.record",new{id,etag,body},key,trace,(c,tx,clock)=>
        {
            var item=Read(c,tx,actor.OwnerId,id,clock); if(item is null) return Missing<DigitalAssetAcknowledgement>();
            var stale=Precondition<DigitalAssetAcknowledgement>(item,etag); if(stale is not null) return stale;
            if(!Operational(item.StoredState)) return Fail<DigitalAssetAcknowledgement>("LifecycleLocked",409,"Restore or unarchive before recording renewal.");
            if(!body.Confirm) return Fail<DigitalAssetAcknowledgement>("ConfirmationRequired",422,"Confirm that this updates only the Nexora record, without a provider renewal or charge.");
            if(body.NewExpiry<body.RenewedOn || body.Notes is {Length:>2000} || !DigitalAssetInput.Amount(body.Amount,body.Currency,out var amount))
                return Fail<DigitalAssetAcknowledgement>("ValidationFailed",422,"Check renewal dates, exact amount/currency and notes.");
            using var cmd=Command(c,tx,"""
                INSERT [assets].[RenewalRecord](Id,OwnerId,DigitalAssetId,RenewedOn,PreviousExpiry,NewExpiry,Amount,Currency,Notes,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(NEWID(),@Owner,@Id,@Renewed,@Previous,@Expiry,@Amount,@Currency,@Notes,SYSUTCDATETIME(),@User,@User);
                UPDATE [assets].[DigitalAsset] SET ExpiresOn=@Expiry,Cost=CASE WHEN @Amount IS NULL THEN Cost ELSE @Amount END,
                  Currency=CASE WHEN @Amount IS NULL THEN Currency ELSE @Currency END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """,actor.OwnerId);
            Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
            Add(cmd,"@Renewed",SqlDbType.Date,body.RenewedOn.ToDateTime(TimeOnly.MinValue)); Add(cmd,"@Previous",SqlDbType.Date,item.Metadata.ExpiresOn?.ToDateTime(TimeOnly.MinValue));
            Add(cmd,"@Expiry",SqlDbType.Date,body.NewExpiry.ToDateTime(TimeOnly.MinValue)); Decimal(cmd,"@Amount",amount);
            Add(cmd,"@Currency",SqlDbType.Char,body.Currency,3); Add(cmd,"@Notes",SqlDbType.NVarChar,Blank(body.Notes),2000); cmd.ExecuteNonQuery();
            UpdateResource(c,tx,actor,id,"Active"); return Ack(c,tx,actor,id,clock);
        },body.Notes);
    public IdentityOperationResult<DigitalAssetPreview> Preview(IdentityPrincipal actor, Guid id, string operation)
    {
        var action = Action(operation);
        if (action is null) return Fail<DigitalAssetPreview>("ValidationFailed", 422, "Choose a supported asset operation.");
        if (!Allowed(actor, action)) return Denied<DigitalAssetPreview>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id, Clock(c,null,actor));
        if (item is null) return Missing<DigitalAssetPreview>();
        if (!Eligible(item.StoredState, operation)) return Fail<DigitalAssetPreview>("LifecycleLocked", 409, "The asset state does not allow this operation.");
        return IdentityOperationResult<DigitalAssetPreview>.Success(new(id, item.ETag, operation, ReferenceCount(c, null, actor.OwnerId, id)));
    }
    public IdentityOperationResult<DigitalAssetAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, DigitalAssetConfirmation body, string key, string? trace)
    {
        var action = Action(operation);
        if (action is null) return Fail<DigitalAssetAcknowledgement>("ValidationFailed", 422, "Choose a supported asset operation.");
        return Mutate(actor, action, new { id, etag, operation, body }, key, trace, (c, tx, clock) =>
        {
            var item = Read(c, tx, actor.OwnerId, id, clock); if (item is null) return Missing<DigitalAssetAcknowledgement>();
            var stale = Precondition<DigitalAssetAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!body.Confirm) return Fail<DigitalAssetAcknowledgement>("ConfirmationRequired", 422, "Confirm this asset operation.");
            if (!Eligible(item.StoredState, operation)) return Fail<DigitalAssetAcknowledgement>("LifecycleLocked", 409, "The asset state does not allow this operation.");
            var next = operation switch
            {
                "cancel" when item.StoredState == "Active" => "Canceled",
                "archive" when Operational(item.StoredState) => "Archived",
                "unarchive" when item.StoredState == "Archived" => Previous(c, tx, actor.OwnerId, id, "PreArchiveState"),
                "trash" when item.StoredState != "Trash" => "Trash",
                "restore" when item.StoredState == "Trash" => Previous(c, tx, actor.OwnerId, id, "PreTrashState"),
                "purge" when item.StoredState == "Trash" => "Purged",
                _ => null
            };
            if (next is null) return Fail<DigitalAssetAcknowledgement>("LifecycleLocked", 409, "The asset state does not allow this operation.");
            if (next == "Purged" && ReferenceCount(c, tx, actor.OwnerId, id) != 0)
                return Fail<DigitalAssetAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
            var fingerprint=operation=="trash"?Fingerprint(c,tx,actor.OwnerId,id):null;
            if(operation is "restore" or "purge")
            {
                using var saved=Command(c,tx,"SELECT TrashChildFingerprint FROM [assets].[DigitalAsset] WHERE OwnerId=@Owner AND Id=@Id;",actor.OwnerId);
                Add(saved,"@Id",SqlDbType.UniqueIdentifier,id);
                if(saved.ExecuteScalar() is not byte[] expected || !CryptographicOperations.FixedTimeEquals(expected,Fingerprint(c,tx,actor.OwnerId,id)))
                    return Fail<DigitalAssetAcknowledgement>("LifecycleLocked",409,"The frozen owned-child cohort has changed.");
            }
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
                    DECLARE @CurrentBatch uniqueidentifier=(SELECT TrashBatchId FROM [assets].[DigitalAsset] WHERE OwnerId=@Owner AND Id=@Id);
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
                Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? item.StoredState : Previous(c, tx, actor.OwnerId, id, "PreTrashState"), 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1)
                    return Fail<DigitalAssetAcknowledgement>("LifecycleLocked", 409, "The deletion cohort is unavailable.");
            }
            if(next=="Purged") DeleteDetail(c,tx,actor.OwnerId,id,item.Metadata.Kind);
            using var cmd = Command(c, tx, next == "Purged" ? "DELETE [assets].[RenewalRecord] WHERE OwnerId=@Owner AND DigitalAssetId=@Id; DELETE [assets].[DigitalAssetVersion] WHERE OwnerId=@Owner AND DigitalAssetId=@Id; DELETE [assets].[DigitalAsset] WHERE OwnerId=@Owner AND Id=@Id;" : """
                UPDATE [assets].[DigitalAsset] SET
                    PreArchiveState=CASE WHEN @Operation='archive' THEN State WHEN @Operation='unarchive' THEN NULL ELSE PreArchiveState END,
                    PreTrashState=CASE WHEN @Operation='trash' THEN State WHEN @Operation='restore' THEN NULL ELSE PreTrashState END,
                    TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch WHEN @Operation='restore' THEN NULL ELSE TrashBatchId END,
                    TrashChildFingerprint=CASE WHEN @Operation='trash' THEN @Fingerprint WHEN @Operation='restore' THEN NULL ELSE TrashChildFingerprint END,
                    State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Operation", SqlDbType.VarChar, operation, 64);
            Add(cmd, "@State", SqlDbType.VarChar, next, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch); Add(cmd,"@Fingerprint",SqlDbType.Binary,fingerprint,32); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, next is "Archived" or "Trash" or "Purged" ? next : "Active");
            return IdentityOperationResult<DigitalAssetAcknowledgement>.Success(new(id, next == "Purged" ? null : Read(c, tx, actor.OwnerId, id, clock)!.ETag));
        });
    }
    private IdentityOperationResult<DigitalAssetAcknowledgement> Mutate(IdentityPrincipal actor, string action, object request,
        string key, string? trace, Func<SqlConnection, SqlTransaction, OwnerClock, IdentityOperationResult<DigitalAssetAcknowledgement>> work, string? reason = null)
    {
        if (!RequiredAllowed(actor, action)) return Denied<DigitalAssetAcknowledgement>();
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty)
            return Fail<DigitalAssetAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<DigitalAssetAcknowledgement>(); }
        if (!RequiredAllowed(c, tx, actor, action))
            return Denied<DigitalAssetAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<DigitalAssetAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
            return IdentityOperationResult<DigitalAssetAcknowledgement>.Success(JsonSerializer.Deserialize<DigitalAssetAcknowledgement>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<DigitalAssetAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var clock=Clock(c,tx,actor); var result = work(c, tx, clock); if (!result.Succeeded || result.Value is null) return result;
        if (!RequiredAllowed(c, tx, actor, action)) return Denied<DigitalAssetAcknowledgement>();
        if (action != "digital.asset.purge") AppendVersion(c, tx, actor, result.Value.ItemId, action, reason, clock);
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'assets.DigitalAsset',@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160);
        Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static string? Action(string operation) => operation switch
    {
        "cancel" => "digital.asset.cancel",
        "archive" => "digital.asset.archive",
        "unarchive" => "digital.asset.unarchive", "trash" => "digital.asset.trash",
        "restore" => "digital.asset.restore", "purge" => "digital.asset.purge", _ => null
    };
    private static bool Eligible(string status, string operation) => operation switch
    {
        "cancel" => status == "Active",
        "archive" => Operational(status),
        "unarchive" => status == "Archived", "trash" => status != "Trash",
        "restore" or "purge" => status == "Trash", _ => false
    };
    private static IdentityOperationResult<T>? Precondition<T>(DigitalAsset item, string? etag) =>
        string.IsNullOrWhiteSpace(etag) ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") :
        item.ETag == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current asset item.");
    private bool RequiredAllowed(IdentityPrincipal actor, string action) => Allowed(actor, action) &&
        (action != "digital.asset.update" || Allowed(actor, "digital.asset.read"));
    private bool RequiredAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) => Allowed(c, tx, actor, action) &&
        (action != "digital.asset.update" || Allowed(c, tx, actor, "digital.asset.read"));
    private static bool Operational(string? state) => state is "Active" or "Canceled";
    private static string? Blank(string? value) => DigitalAssetInput.Blank(value);
    private static void Decimal(SqlCommand cmd,string name,decimal? value)
    { var p=cmd.Parameters.Add(name,SqlDbType.Decimal); p.Precision=28; p.Scale=8; p.Value=(object?)value??DBNull.Value; }
    private static void Fields(SqlCommand cmd,IdentityPrincipal actor,Guid id,DigitalAssetMetadata metadata)
    {
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
        Add(cmd,"@Title",SqlDbType.NVarChar,metadata.Title,200); Add(cmd,"@Kind",SqlDbType.VarChar,metadata.Kind,64);
        Add(cmd,"@Provider",SqlDbType.NVarChar,metadata.Provider,200); Add(cmd,"@ExpiresOn",SqlDbType.Date,metadata.ExpiresOn?.ToDateTime(TimeOnly.MinValue));
        DigitalAssetInput.Amount(metadata.Cost,metadata.Currency,out var cost); Decimal(cmd,"@Cost",cost);
        Add(cmd,"@Currency",SqlDbType.Char,metadata.Currency,3); Add(cmd,"@Cycle",SqlDbType.NVarChar,metadata.RenewalCycle,100); Add(cmd,"@Notes",SqlDbType.NVarChar,metadata.Notes,-1);
    }
    private static DateTime? ParseInstant(string? input) { DigitalAssetInput.Instant(input,out var value); return value; }
    private static DateTime LocalDayUtc(DateOnly day,string iana)
    {
        var zone=TimeZoneInfo.FindSystemTimeZoneById(iana); var local=day.ToDateTime(TimeOnly.MinValue,DateTimeKind.Unspecified);
        // A skipped midnight/day begins at its first valid minute; an ambiguous midnight begins at the earlier occurrence.
        for(var minute=0;zone.IsInvalidTime(local) && minute<2880;minute++)
            local=local.Ticks>DateTime.MaxValue.Ticks-TimeSpan.TicksPerMinute ? DateTime.MaxValue : local.AddMinutes(1);
        if(zone.IsInvalidTime(local)) throw new InvalidOperationException("The timezone day boundary is unavailable.");
        var offset=zone.IsAmbiguousTime(local)?zone.GetAmbiguousTimeOffsets(local).Max():zone.GetUtcOffset(local);
        var ticks=local.Ticks-offset.Ticks;
        return new DateTime(Math.Clamp(ticks,DateTime.MinValue.Ticks,DateTime.MaxValue.Ticks),DateTimeKind.Utc);
    }
    private static string Unicode(string ascii) { DigitalAssetInput.Domain(ascii,out _,out var unicode); return unicode!; }
    private static void SaveDetail(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id,DigitalAssetMetadata metadata)
    {
        switch(metadata.Kind)
        {
        case "Domain":
            {
                var d = metadata.Details.Domain!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[DomainDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[DomainDetail] SET AsciiName=@AsciiName,UnicodeName=@UnicodeName,Registrar=@Registrar,AutoRenewRecorded=@AutoRenewRecorded,RegisteredOn=@RegisteredOn,NameserverNotes=@NameserverNotes,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[DomainDetail](Id,OwnerId,DigitalAssetId,Kind,AsciiName,UnicodeName,Registrar,AutoRenewRecorded,RegisteredOn,NameserverNotes,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'Domain',@AsciiName,@UnicodeName,@Registrar,@AutoRenewRecorded,@RegisteredOn,@NameserverNotes,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@AsciiName", SqlDbType.NVarChar, d.Name, 253); Add(cmd, "@UnicodeName", SqlDbType.NVarChar, Unicode(d.Name), 253); Add(cmd, "@Registrar", SqlDbType.NVarChar, d.Registrar, 200); Add(cmd, "@AutoRenewRecorded", SqlDbType.Bit, d.AutoRenewRecorded); Add(cmd, "@RegisteredOn", SqlDbType.Date, d.RegisteredOn?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@NameserverNotes", SqlDbType.NVarChar, d.NameserverNotes, -1); cmd.ExecuteNonQuery(); break;
            }
case "Hosting":
            {
                var d = metadata.Details.Hosting!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[HostingDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[HostingDetail] SET [Plan]=@Plan,Region=@Region,ControlPanelUrl=@ControlPanelUrl,StorageLimitBytes=@StorageLimitBytes,DomainName=@DomainName,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[HostingDetail](Id,OwnerId,DigitalAssetId,Kind,[Plan],Region,ControlPanelUrl,StorageLimitBytes,DomainName,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'Hosting',@Plan,@Region,@ControlPanelUrl,@StorageLimitBytes,@DomainName,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@Plan", SqlDbType.NVarChar, d.Plan, 200); Add(cmd, "@Region", SqlDbType.NVarChar, d.Region, 100); Add(cmd, "@ControlPanelUrl", SqlDbType.NVarChar, d.ControlPanelUrl, 2048); Add(cmd, "@StorageLimitBytes", SqlDbType.BigInt, d.StorageLimitBytes is null ? null : long.Parse(d.StorageLimitBytes, CultureInfo.InvariantCulture)); Add(cmd, "@DomainName", SqlDbType.NVarChar, d.DomainName, 253); cmd.ExecuteNonQuery(); break;
            }
case "Vps":
            {
                var d = metadata.Details.Vps!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[VpsDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[VpsDetail] SET HostName=@HostName,IpAddress=@IpAddress,CpuCount=@CpuCount,MemoryMiB=@MemoryMiB,OperatingSystem=@OperatingSystem,[Plan]=@Plan,Region=@Region,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[VpsDetail](Id,OwnerId,DigitalAssetId,Kind,HostName,IpAddress,CpuCount,MemoryMiB,OperatingSystem,[Plan],Region,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'Vps',@HostName,@IpAddress,@CpuCount,@MemoryMiB,@OperatingSystem,@Plan,@Region,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@HostName", SqlDbType.NVarChar, d.HostName, 253); Add(cmd, "@IpAddress", SqlDbType.NVarChar, d.IpAddress, 45); Add(cmd, "@CpuCount", SqlDbType.Int, d.CpuCount); Add(cmd, "@MemoryMiB", SqlDbType.BigInt, d.MemoryMiB is null ? null : long.Parse(d.MemoryMiB, CultureInfo.InvariantCulture)); Add(cmd, "@OperatingSystem", SqlDbType.NVarChar, d.OperatingSystem, 200); Add(cmd, "@Plan", SqlDbType.NVarChar, d.Plan, 200); Add(cmd, "@Region", SqlDbType.NVarChar, d.Region, 100); cmd.ExecuteNonQuery(); break;
            }
case "Certificate":
            {
                var d = metadata.Details.Certificate!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[CertificateDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[CertificateDetail] SET Subject=@Subject,Issuer=@Issuer,Fingerprint=@Fingerprint,NotBefore=@NotBefore,NotAfter=@NotAfter,HostName=@HostName,SubjectAlternativeNamesJson=@SubjectAlternativeNamesJson,Source=@Source,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[CertificateDetail](Id,OwnerId,DigitalAssetId,Kind,Subject,Issuer,Fingerprint,NotBefore,NotAfter,HostName,SubjectAlternativeNamesJson,Source,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'Certificate',@Subject,@Issuer,@Fingerprint,@NotBefore,@NotAfter,@HostName,@SubjectAlternativeNamesJson,@Source,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@Subject", SqlDbType.NVarChar, d.Subject, 500); Add(cmd, "@Issuer", SqlDbType.NVarChar, d.Issuer, 500); Add(cmd, "@Fingerprint", SqlDbType.NVarChar, d.Fingerprint, 200); Add(cmd, "@NotBefore", SqlDbType.DateTime2, ParseInstant(d.NotBefore)); Add(cmd, "@NotAfter", SqlDbType.DateTime2, ParseInstant(d.NotAfter)); Add(cmd, "@HostName", SqlDbType.NVarChar, d.HostName, 253); Add(cmd, "@SubjectAlternativeNamesJson", SqlDbType.NVarChar, JsonSerializer.Serialize(d.SubjectAlternativeNames ?? []), -1); Add(cmd, "@Source", SqlDbType.VarChar, "Manual", 64); cmd.ExecuteNonQuery(); break;
            }
case "License":
            {
                var d = metadata.Details.License!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[LicenseDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[LicenseDetail] SET Product=@Product,Seats=@Seats,PurchasedOn=@PurchasedOn,Vendor=@Vendor,Edition=@Edition,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[LicenseDetail](Id,OwnerId,DigitalAssetId,Kind,Product,Seats,PurchasedOn,Vendor,Edition,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'License',@Product,@Seats,@PurchasedOn,@Vendor,@Edition,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@Product", SqlDbType.NVarChar, d.Product, 200); Add(cmd, "@Seats", SqlDbType.Int, d.Seats); Add(cmd, "@PurchasedOn", SqlDbType.Date, d.PurchasedOn?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@Vendor", SqlDbType.NVarChar, d.Vendor, 200); Add(cmd, "@Edition", SqlDbType.NVarChar, d.Edition, 200); cmd.ExecuteNonQuery(); break;
            }
case "OnlineService":
            {
                var d = metadata.Details.OnlineService!;
                using var cmd = Command(c, tx, """
                    IF EXISTS(SELECT 1 FROM [assets].[ServiceDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
                        UPDATE [assets].[ServiceDetail] SET ServiceUrl=@ServiceUrl,[Plan]=@Plan,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND DigitalAssetId=@Id;
                    ELSE INSERT [assets].[ServiceDetail](Id,OwnerId,DigitalAssetId,Kind,ServiceUrl,[Plan],UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        VALUES(NEWID(),@Owner,@Id,'OnlineService',@ServiceUrl,@Plan,SYSUTCDATETIME(),@User,@User);
                    """, actor.OwnerId);
                Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cmd, "@ServiceUrl", SqlDbType.NVarChar, d.ServiceUrl, 2048); Add(cmd, "@Plan", SqlDbType.NVarChar, d.Plan, 200); cmd.ExecuteNonQuery(); break;
            }
        default: throw new InvalidOperationException("Digital subtype integrity failed.");
        }
    }
    private static string DetailTable(string kind) => kind switch
    { "Domain"=>"DomainDetail", "Hosting"=>"HostingDetail", "Vps"=>"VpsDetail", "Certificate"=>"CertificateDetail", "License"=>"LicenseDetail", "OnlineService"=>"ServiceDetail", _=>throw new InvalidOperationException("Digital subtype integrity failed.") };
    private static void DeleteDetail(SqlConnection c,SqlTransaction tx,Guid owner,Guid id,string kind)
    {
        using var cmd=Command(c,tx,$"DELETE [assets].[{DetailTable(kind)}] WHERE OwnerId=@Owner AND DigitalAssetId=@Id;",owner);
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Digital subtype integrity failed.");
    }
    private static byte[] Fingerprint(SqlConnection c,SqlTransaction tx,Guid owner,Guid id)
    {
        using var stream=new MemoryStream(); using var writer=new BinaryWriter(stream);
        foreach(var table in new[]{"DomainDetail","HostingDetail","VpsDetail","CertificateDetail","LicenseDetail","ServiceDetail","RenewalRecord"})
        {
            writer.Write(table);
            using var cmd=Command(c,tx,$"SELECT Id,RowVersion FROM [assets].[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND DigitalAssetId=@Id ORDER BY Id;",owner);
            Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); using var r=cmd.ExecuteReader(); var count=0;
            while(r.Read()) { writer.Write(r.GetGuid(0).ToByteArray()); writer.Write(r.GetFieldValue<byte[]>(1)); count++; }
            writer.Write(count);
        }
        writer.Flush(); return SHA256.HashData(stream.ToArray());
    }
    private static IdentityOperationResult<DigitalAssetAcknowledgement> Ack(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id,OwnerClock clock,int status=200) =>
        IdentityOperationResult<DigitalAssetAcknowledgement>.Success(new(id,Read(c,tx,actor.OwnerId,id,clock)!.ETag),status);
    private static void UpdateResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string availability)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("DigitalAsset registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar());
    }
    private static string? Previous(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, string column)
    {
        if (column is not ("PreArchiveState" or "PreTrashState")) throw new ArgumentException("Unknown lifecycle field.", nameof(column));
        using var cmd = Command(c, tx, $"SELECT {column} FROM [assets].[DigitalAsset] WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return cmd.ExecuteScalar() as string;
    }
    private static DigitalAsset? Read(SqlConnection c,SqlTransaction? tx,Guid owner,Guid id,OwnerClock clock)
    { using var cmd=Command(c,tx,$"SELECT {Columns} FROM [assets].[DigitalAsset] a WHERE a.OwnerId=@Owner AND a.Id=@Id;",owner); Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); return Rows(cmd,clock).FirstOrDefault(); }
    private static string? Optional(SqlDataReader r,int index)=>r.IsDBNull(index)?null:r.GetString(index);
    private static DateOnly? Day(SqlDataReader r,int index)=>r.IsDBNull(index)?null:DateOnly.FromDateTime(r.GetDateTime(index));
    private static string? Money(SqlDataReader r,int index)=>r.IsDBNull(index)?null:r.GetDecimal(index).ToString("0.########",CultureInfo.InvariantCulture);
    private static List<DigitalAsset> Rows(SqlCommand cmd,OwnerClock clock)
    {
        using var r=cmd.ExecuteReader(); var items=new List<DigitalAsset>();
        while(r.Read())
        {
            var kind=r.GetString(2); if(r.IsDBNull(13)) throw new InvalidOperationException("Digital subtype integrity failed.");
            var json=r.GetString(13);
            DigitalAssetDetails details=kind switch {
                "Domain" => new(Domain: JsonSerializer.Deserialize<DigitalDomain>(json, SnapshotJson)),
"Hosting" => new(Hosting: JsonSerializer.Deserialize<DigitalHosting>(json, SnapshotJson)),
"Vps" => new(Vps: JsonSerializer.Deserialize<DigitalVps>(json, SnapshotJson)),
"Certificate" => new(Certificate: JsonSerializer.Deserialize<DigitalCertificate>(json, SnapshotJson)),
"License" => new(License: JsonSerializer.Deserialize<DigitalLicense>(json, SnapshotJson)),
"OnlineService" => new(OnlineService: JsonSerializer.Deserialize<DigitalOnlineService>(json, SnapshotJson)),
                _=>throw new InvalidOperationException("Digital subtype integrity failed.")};
            var metadata=new DigitalAssetMetadata(r.GetString(1),kind,details,Optional(r,3),Day(r,4),Money(r,5),Optional(r,6),Optional(r,7),Optional(r,8));
            var stored=r.GetString(9); var basis=details.Certificate?.NotAfter is not null?"CertificateNotAfter":metadata.ExpiresOn is not null?"EnteredDate":"Unknown";
            var expired=basis=="CertificateNotAfter" ? clock.Now>=DateTimeOffset.Parse(details.Certificate!.NotAfter!,CultureInfo.InvariantCulture) : metadata.ExpiresOn is { } date && clock.Today>date;
            var state=stored is "Trash" or "Archived" or "Canceled" ? stored : expired?"Expired":"Active";
            items.Add(new(r.GetGuid(0),metadata,stored,state,basis,clock.Iana,Optional(r,14),Optional(r,15),Utc(r.GetDateTime(10)),Utc(r.GetDateTime(11)),"\""+Convert.ToBase64String(r.GetFieldValue<byte[]>(12))+"\""));
        }
        return items;
    }
    private static readonly JsonSerializerOptions SnapshotJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static void AppendVersion(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id,string action,string? reason,OwnerClock clock)
    {
        var item=Read(c,tx,actor.OwnerId,id,clock)??throw new InvalidOperationException("Digital history integrity failed.");
        var fields=new DigitalAssetSnapshotFields(item.Metadata,item.StoredState,Previous(c,tx,actor.OwnerId,id,"PreArchiveState"),Previous(c,tx,actor.OwnerId,id,"PreTrashState"),item.AsciiName,item.UnicodeName);
        var snapshot=new DigitalAssetSnapshot(1,"DigitalAsset",fields,[],[],new Dictionary<string,DigitalAssetProvenance>{{"metadata",new("Manual")}});
        using var cmd=Command(c,tx,"""
            INSERT [assets].[DigitalAssetVersion](Id,OwnerId,DigitalAssetId,VersionNumber,CreatedByUserId,ActionKey,Reason,SafeSnapshotJson)
            SELECT NEWID(),@Owner,@Id,Revision,@User,@Action,@Reason,@Snapshot FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id;
            """,actor.OwnerId);
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId); Add(cmd,"@Action",SqlDbType.VarChar,action,160);
        Add(cmd,"@Reason",SqlDbType.NVarChar,Blank(reason),2000); Add(cmd,"@Snapshot",SqlDbType.NVarChar,JsonSerializer.Serialize(snapshot,SnapshotJson),-1);
        if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Digital history integrity failed.");
    }
    public IdentityOperationResult<DigitalAssetHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor, string? action, string? from, string? to, string? version)
    {
        if (!Allowed(actor, "digital.asset.history")) return Denied<DigitalAssetHistoryPage>();
        long? exactVersion = null;
        if (version is not null)
        {
            if (version.Length > 19 || !long.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1)
                return Fail<DigitalAssetHistoryPage>("ValidationFailed", 422, "Choose a positive exact history version.");
            exactVersion = number;
        }
        if ((action is not null && !Actions.Contains(action)) || !Instant(from, out var start) || !Instant(to, out var end) ||
            (start is not null && end is not null && start >= end))
            return Fail<DigitalAssetHistoryPage>("ValidationFailed", 422, "Choose an installed history action and a valid UTC instant range.");
        using var c = connections.Create(); c.Open(); if (Read(c, null, actor.OwnerId, id, Clock(c,null,actor)) is null) return Missing<DigitalAssetHistoryPage>();
        using var cmd = Command(c, null, """
            WITH selected AS (SELECT Id,VersionNumber,ActionKey,CreatedAt,CreatedByUserId,Reason,SafeSnapshotJson FROM [assets].[DigitalAssetVersion]
                WHERE OwnerId=@Owner AND DigitalAssetId=@Id AND (@Action IS NULL OR ActionKey=@Action) AND (@Version IS NULL OR VersionNumber=@Version)
                  AND (@From IS NULL OR CreatedAt>=@From) AND (@To IS NULL OR CreatedAt<@To))
            SELECT TOP(26) * FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.VersionNumber<boundary.VersionNumber OR (selected.VersionNumber=boundary.VersionNumber AND selected.Id<boundary.Id)))
            ORDER BY VersionNumber DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Version", SqlDbType.BigInt, exactVersion); Add(cmd, "@Action", SqlDbType.VarChar, action, 160); Add(cmd, "@From", SqlDbType.DateTime2, start); Add(cmd, "@To", SqlDbType.DateTime2, end);
        using var r = cmd.ExecuteReader(); var items = new List<DigitalAssetVersion>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetInt64(1), r.GetString(2), Utc(r.GetDateTime(3)), r.IsDBNull(4) ? null : r.GetGuid(4),
            r.IsDBNull(5) ? null : r.GetString(5), JsonSerializer.Deserialize<DigitalAssetSnapshot>(r.GetString(6), SnapshotJson)!));
        return IdentityOperationResult<DigitalAssetHistoryPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<DigitalRenewalPage> Renewals(IdentityPrincipal actor,Guid id,Guid? cursor)
    {
        if(!Allowed(actor,"digital.asset.history")) return Denied<DigitalRenewalPage>();
        using var c=connections.Create(); c.Open(); if(Read(c,null,actor.OwnerId,id,Clock(c,null,actor)) is null) return Missing<DigitalRenewalPage>();
        using var cmd=Command(c,null,"""
            WITH selected AS (SELECT Id,RenewedOn,PreviousExpiry,NewExpiry,Amount,Currency,Notes,CreatedAt FROM [assets].[RenewalRecord] WHERE OwnerId=@Owner AND DigitalAssetId=@Id)
            SELECT TOP(26) * FROM selected s WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor AND
                (s.RenewedOn<b.RenewedOn OR (s.RenewedOn=b.RenewedOn AND (s.CreatedAt<b.CreatedAt OR (s.CreatedAt=b.CreatedAt AND s.Id<b.Id)))))
            ORDER BY RenewedOn DESC,CreatedAt DESC,Id DESC;
            """,actor.OwnerId);
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@Cursor",SqlDbType.UniqueIdentifier,cursor);
        using var r=cmd.ExecuteReader(); var items=new List<DigitalRenewalRecord>();
        while(r.Read()) items.Add(new(r.GetGuid(0),Day(r,1)!.Value,Day(r,2),Day(r,3)!.Value,Money(r,4),Optional(r,5),Optional(r,6),Utc(r.GetDateTime(7))));
        return IdentityOperationResult<DigitalRenewalPage>.Success(new(items.Take(25).ToArray(),items.Count>25?items[24].Id:null));
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
