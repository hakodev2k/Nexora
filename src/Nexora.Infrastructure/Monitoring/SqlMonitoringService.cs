using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Monitoring;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Monitoring;

/// <summary>Private owner HTTP configuration. No DNS, outbound client, probe or worker dependency.</summary>
public sealed class SqlMonitoringService : IMonitoringService
{
    public static readonly string[] Actions = ["monitoring.monitor.read", "monitoring.monitor.create",
        "monitoring.monitor.update", "monitoring.monitor.pause", "monitoring.monitor.resume"];
    private const string Columns = "Id,Title,Kind,Target,IntervalSeconds,ExpectedStatus,Enabled,State,LastObservedAt,CreatedAt,UpdatedAt,RowVersion";
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    public SqlMonitoringService(SqlConnectionFactory connections, string secret)
    { this.connections=connections; capabilities=new(connections); receipts=new(secret); }
    private static IdentityOperationResult<T> Fail<T>(string code,int status,string title)=>IdentityOperationResult<T>.Failure(code,status,title);
    private static IdentityOperationResult<T> Denied<T>()=>Fail<T>("ModuleUnavailable",403,"The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>()=>Fail<T>("ResourceUnavailable",404,"The resource is unavailable.");
    private bool Allowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,string action)=>
        Actions.Contains(action,StringComparer.Ordinal) && SqlCurrentActor.IsLive(c,tx,actor) &&
        capabilities.IsAllowed(c,tx,actor,"FX36",action) &&
        (action!="monitoring.monitor.update" || capabilities.IsAllowed(c,tx,actor,"FX36","monitoring.monitor.read"));
    private static IdentityOperationResult<T> Safe<T>(Func<IdentityOperationResult<T>> work)
    {
        try{return work();}
        catch(SqlException e) when(e.Number==1205){return Fail<T>("ConcurrencyConflict",409,"Retry the reviewed request.");}
        catch(SqlException){return Fail<T>("StorageUnavailable",503,"Local storage is unavailable.");}
        catch(InvalidOperationException){return Fail<T>("StorageUnavailable",503,"The installed storage contract is unavailable.");}
    }
    public IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal actor)=>Safe(()=>
    {
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!SqlCurrentActor.IsLive(c,tx,actor))return Denied<IReadOnlyDictionary<string,bool>>();
        var result=Actions.ToDictionary(a=>a,a=>Allowed(c,tx,actor,a));
        if(!result.Values.Any(x=>x) || !SqlCurrentActor.IsLive(c,tx,actor))return Denied<IReadOnlyDictionary<string,bool>>();
        tx.Commit();return IdentityOperationResult<IReadOnlyDictionary<string,bool>>.Success(result);
    });
    public IdentityOperationResult<MonitorPage> List(IdentityPrincipal actor,Guid? cursor,string? query,string? state)=>Safe(()=>
    {
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,actor,"monitoring.monitor.read"))return Denied<MonitorPage>();
        query=string.IsNullOrWhiteSpace(query)?null:query.Trim();state=string.IsNullOrWhiteSpace(state)?null:state;
        if(query is {Length:>200} || state is not (null or "Unknown" or "Paused"))return Fail<MonitorPage>("ValidationFailed",422,"Choose a valid state and a query up to 200 characters.");
        var selected=$"SELECT {string.Join(',',Columns.Split(',').Select(x=>"m."+x))} FROM [monitoring].[Monitor] m WITH(HOLDLOCK) WHERE m.OwnerId=@Owner AND {SqlMonitorRegistry.Predicate} AND (@State IS NULL OR m.State=@State) AND (@Query IS NULL OR CHARINDEX(@Query,m.Title)>0)";
        if(cursor is not null)
        {
            using var boundary=Command(c,tx,$"WITH selected AS ({selected}) SELECT Id FROM selected WHERE Id=@Cursor;",actor.OwnerId);
            Filters(boundary,cursor,query,state);if(boundary.ExecuteScalar() is null)return Missing<MonitorPage>();
        }
        using var cmd=Command(c,tx,$"WITH selected AS ({selected}) SELECT TOP(26) {Columns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor AND (selected.Title>b.Title OR(selected.Title=b.Title AND selected.Id>b.Id))) ORDER BY Title ASC,Id ASC;",actor.OwnerId);
        Filters(cmd,cursor,query,state);var items=Rows(cmd);
        if(!Allowed(c,tx,actor,"monitoring.monitor.read"))return Denied<MonitorPage>();
        tx.Commit();return IdentityOperationResult<MonitorPage>.Success(new(items.Take(25).ToArray(),items.Count>25?items[24].Id:null));
    });
    public IdentityOperationResult<MonitorRecord> Get(IdentityPrincipal actor,Guid id)=>Safe(()=>
    {
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,actor,"monitoring.monitor.read"))return Denied<MonitorRecord>();
        var item=Read(c,tx,actor.OwnerId,id,false);if(item is null)return Missing<MonitorRecord>();
        if(!Allowed(c,tx,actor,"monitoring.monitor.read"))return Denied<MonitorRecord>();
        tx.Commit();return IdentityOperationResult<MonitorRecord>.Success(item);
    });
    public IdentityOperationResult<MonitorAcknowledgement> Create(IdentityPrincipal actor,MonitorCreate body,string key,string? trace)=>
        Mutate(actor,"monitoring.monitor.create",null,null,body,key,trace,(c,tx,_)=>
        {
            if(!MonitorInput.TryNormalize(body.Metadata,out var metadata))return new(Invalid(),false);
            var id=Guid.NewGuid();SqlMonitorRegistry.Create(c,tx,actor,id);
            using var cmd=Command(c,tx,"INSERT [monitoring].[Monitor](Id,OwnerId,Title,Kind,Target,IntervalSeconds,ExpectedStatus,Enabled,State,UpdatedAt,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Title,'Http',@Target,@Interval,@Expected,@Enabled,@State,SYSUTCDATETIME(),@User,@User);",actor.OwnerId);
            Fields(cmd,actor,id,metadata);Add(cmd,"@Enabled",SqlDbType.Bit,body.Enabled);Add(cmd,"@State",SqlDbType.VarChar,body.Enabled?"Unknown":"Paused",64);cmd.ExecuteNonQuery();
            return new(IdentityOperationResult<MonitorAcknowledgement>.Success(Ack(Read(c,tx,actor.OwnerId,id,false)!),201),true);
        });
    public IdentityOperationResult<MonitorAcknowledgement> Update(IdentityPrincipal actor,Guid id,string? etag,MonitorUpdate body,string key,string? trace)=>
        Mutate(actor,"monitoring.monitor.update",id,etag,body,key,trace,(c,tx,item)=>
        {
            if(!MonitorInput.TryNormalize(body.Metadata,out var metadata))return new(Invalid(),false);
            using var cmd=Command(c,tx,"UPDATE [monitoring].[Monitor] SET Title=@Title,Target=@Target,IntervalSeconds=@Interval,ExpectedStatus=@Expected,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;",actor.OwnerId);
            Fields(cmd,actor,id,metadata);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Monitor integrity.");
            SqlMonitorRegistry.Advance(c,tx,actor,id);return new(IdentityOperationResult<MonitorAcknowledgement>.Success(Ack(Read(c,tx,actor.OwnerId,id,false)!)),true);
        });
    public IdentityOperationResult<MonitorAcknowledgement> SetEnabled(IdentityPrincipal actor,Guid id,string? etag,bool enabled,MonitorConfirmation body,string key,string? trace)=>
        Mutate(actor,enabled?"monitoring.monitor.resume":"monitoring.monitor.pause",id,etag,new{enabled,body},key,trace,(c,tx,item)=>
        {
            if(!body.Confirmed)return new(Fail<MonitorAcknowledgement>("ConfirmationRequired",422,"Confirm this preference change."),false);
            if(item!.Enabled==enabled)return new(IdentityOperationResult<MonitorAcknowledgement>.Success(Ack(item)),false);
            using var cmd=Command(c,tx,"UPDATE [monitoring].[Monitor] SET Enabled=@Enabled,State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;",actor.OwnerId);
            Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id);Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(cmd,"@Enabled",SqlDbType.Bit,enabled);Add(cmd,"@State",SqlDbType.VarChar,enabled?"Unknown":"Paused",64);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Monitor integrity.");
            SqlMonitorRegistry.Advance(c,tx,actor,id);return new(IdentityOperationResult<MonitorAcknowledgement>.Success(Ack(Read(c,tx,actor.OwnerId,id,false)!)),true);
        });
    private sealed record Effect(IdentityOperationResult<MonitorAcknowledgement> Result,bool Changed);
    private IdentityOperationResult<MonitorAcknowledgement> Mutate(IdentityPrincipal actor,string action,Guid? id,string? etag,object body,string key,string? trace,Func<SqlConnection,SqlTransaction,MonitorRecord?,Effect> work)=>Safe(()=>
    {
        if(!Guid.TryParse(key,out var uuid)||uuid==Guid.Empty)return Fail<MonitorAcknowledgement>("IdempotencyKeyRequired",422,"A nonempty UUID is required.");
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        using(var owner=Command(c,tx,"SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';",actor.OwnerId))
        {Add(owner,"@User",SqlDbType.UniqueIdentifier,actor.UserId);if(owner.ExecuteScalar() is null)return Denied<MonitorAcknowledgement>();}
        if(!Allowed(c,tx,actor,action))return Denied<MonitorAcknowledgement>();
        var item=id is {} source?Read(c,tx,actor.OwnerId,source,true):null;
        if(id is not null && item is null)return Missing<MonitorAcknowledgement>();
        var claim=receipts.TryClaim(c,tx,actor.UserId,action,key,JsonSerializer.Serialize(new{id,etag,body}),DateTime.UtcNow);
        if(claim.IsConflict)return Fail<MonitorAcknowledgement>("IdempotencyConflict",409,"The key belongs to a different request.");
        if(claim.IsReplay && claim.ResultJson is {} saved)
        {
            var ack=JsonSerializer.Deserialize<MonitorAcknowledgement>(saved)??throw new InvalidOperationException("Receipt projection.");
            if(Read(c,tx,actor.OwnerId,ack.Id,false) is null || (id is not null && ack.Id!=id))return Missing<MonitorAcknowledgement>();
            if(!Allowed(c,tx,actor,action))return Denied<MonitorAcknowledgement>();
            tx.Commit();return IdentityOperationResult<MonitorAcknowledgement>.Success(ack,claim.ResultStatusCode??200,claim.ResultCode??"Ok");
        }
        if(!claim.IsClaimed)return Fail<MonitorAcknowledgement>("RequestInProgress",409,"The request is already in progress.");
        if(item is not null)
        {
            if(string.IsNullOrWhiteSpace(etag))return Fail<MonitorAcknowledgement>("PreconditionRequired",428,"If-Match is required.");
            if(item.ETag!=etag)return Fail<MonitorAcknowledgement>("RevisionConflict",412,"Reload the current configuration.");
        }
        var effect=work(c,tx,item);if(!effect.Result.Succeeded || effect.Result.Value is null)return effect.Result;
        if(effect.Changed)
        {
            using var audit=Command(c,tx,"INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'monitoring.Monitor',@Id,'Succeeded',@Trace);",actor.OwnerId);
            Add(audit,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(audit,"@Action",SqlDbType.NVarChar,action,160);Add(audit,"@Id",SqlDbType.UniqueIdentifier,effect.Result.Value.Id);Add(audit,"@Trace",SqlDbType.NVarChar,trace,100);audit.ExecuteNonQuery();
        }
        receipts.Complete(c,tx,claim,effect.Result.Code,effect.Result.StatusCode,JsonSerializer.Serialize(effect.Result.Value));
        if(!Allowed(c,tx,actor,action)||Read(c,tx,actor.OwnerId,effect.Result.Value.Id,false) is null)return Denied<MonitorAcknowledgement>();
        tx.Commit();return effect.Result;
    });
    private static IdentityOperationResult<MonitorAcknowledgement> Invalid()=>Fail<MonitorAcknowledgement>("ValidationFailed",422,"Check the explicit HTTP configuration fields.");
    private static MonitorAcknowledgement Ack(MonitorRecord item)=>new(item.Id,item.Enabled,item.State,item.ETag);
    private static MonitorRecord? Read(SqlConnection c,SqlTransaction tx,Guid owner,Guid id,bool update)
    {
        using var cmd=Command(c,tx,$"SELECT {string.Join(',',Columns.Split(',').Select(x=>"m."+x))} FROM [monitoring].[Monitor] m WITH({(update?"UPDLOCK,HOLDLOCK":"HOLDLOCK")}) WHERE m.OwnerId=@Owner AND m.Id=@Id AND {SqlMonitorRegistry.Predicate};",owner);
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id);return Rows(cmd).FirstOrDefault();
    }
    private static List<MonitorRecord> Rows(SqlCommand cmd)
    {
        using var r=cmd.ExecuteReader();var items=new List<MonitorRecord>();
        while(r.Read())items.Add(new(r.GetGuid(0),new(1,r.GetString(1),r.GetString(2),r.GetString(3),r.GetInt32(4),r.IsDBNull(5)?null:r.GetInt32(5)),r.GetBoolean(6),r.GetString(7),r.IsDBNull(8)?null:Utc(r.GetDateTime(8)),Utc(r.GetDateTime(9)),Utc(r.GetDateTime(10)),"\""+Convert.ToBase64String(r.GetFieldValue<byte[]>(11))+"\""));return items;
    }
    private static DateTimeOffset Utc(DateTime value)=>new(DateTime.SpecifyKind(value,DateTimeKind.Utc));
    private static void Filters(SqlCommand cmd,Guid? cursor,string? query,string? state)
    {Add(cmd,"@Cursor",SqlDbType.UniqueIdentifier,cursor);Add(cmd,"@Query",SqlDbType.NVarChar,query,200);Add(cmd,"@State",SqlDbType.VarChar,state,64);}
    private static void Fields(SqlCommand cmd,IdentityPrincipal actor,Guid id,MonitorMetadata value)
    {Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id);Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(cmd,"@Title",SqlDbType.NVarChar,value.Title,200);Add(cmd,"@Target",SqlDbType.NVarChar,value.Target,2048);Add(cmd,"@Interval",SqlDbType.Int,value.IntervalSeconds);Add(cmd,"@Expected",SqlDbType.Int,value.ExpectedStatus);}
    internal static SqlCommand Command(SqlConnection c,SqlTransaction tx,string sql,Guid owner)
    {var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;Add(cmd,"@Owner",SqlDbType.UniqueIdentifier,owner);return cmd;}
    internal static void Add(SqlCommand cmd,string name,SqlDbType type,object? value,int size=0)
    {var p=cmd.Parameters.Add(name,type);if(size!=0)p.Size=size;p.Value=value??DBNull.Value;}
}

/// <summary>Named Platform registry participant for the exact local Monitor resource contract.</summary>
internal static class SqlMonitorRegistry
{
    internal const string Predicate="EXISTS(SELECT 1 FROM [platform].[Resource] r WITH(HOLDLOCK) JOIN [platform].[ResourceType] rt WITH(HOLDLOCK) ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] moduleRow WITH(HOLDLOCK) ON moduleRow.Id=rt.ModuleId WHERE r.OwnerId=m.OwnerId AND r.Id=m.Id AND r.Availability='Active' AND r.PurgedAt IS NULL AND rt.Code='Monitor' AND rt.ContractVersion='monitoring-http-config-v1' AND moduleRow.Code='FX36')";
    internal static void Create(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        using var cmd=SqlMonitoringService.Command(c,tx,"INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId) SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX36' AND rt.Code='Monitor' AND rt.ContractVersion='monitoring-http-config-v1';",actor.OwnerId);
        SqlMonitoringService.Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id);SqlMonitoringService.Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Monitor registry contract.");
    }
    internal static void Advance(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        using var cmd=SqlMonitoringService.Command(c,tx,"UPDATE r SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=@Owner AND r.Id=@Id AND r.Availability='Active' AND r.PurgedAt IS NULL AND rt.Code='Monitor' AND rt.ContractVersion='monitoring-http-config-v1' AND m.Code='FX36';",actor.OwnerId);
        SqlMonitoringService.Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id);SqlMonitoringService.Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException("Monitor registry integrity.");
    }
}
