using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Learning;

public sealed partial class SqlWorkLogService : IWorkLogService
{
    public static readonly string[] Actions = ["learning.worklog.read", "learning.worklog.create", "learning.worklog.update",
        "learning.worklog.archive", "learning.worklog.unarchive", "learning.report.read", "learning.worklog.trash", "learning.worklog.restore", "learning.worklog.purge"];
    private const string Columns = "w.Id,w.Title,w.RecordedOn,w.Minutes,w.Notes,w.Category,w.Employer,w.Status,w.CreatedAt,w.UpdatedAt,w.RowVersion,w.PreTrashState";
    private const string Registry = """
        EXISTS(SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
            JOIN [platform].[Module] m ON m.Id=rt.ModuleId
            WHERE r.OwnerId=w.OwnerId AND r.Id=w.Id AND m.Code='FX40' AND rt.Code='WorkLog'
              AND rt.ContractVersion='worklog-manual-v1' AND r.Availability=w.Status)
        """;
    private const string Filter = "w.OwnerId=@Owner AND w.Status=@Status AND (@Query IS NULL OR CHARINDEX(@Query,w.Title)>0) AND (@From IS NULL OR w.RecordedOn>=@From) AND (@To IS NULL OR w.RecordedOn<=@To) AND (@Category IS NULL OR w.Category=@Category)";
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly WorkLogCursor cursors;
    private readonly WorkLogHistoryCursor historyCursors;
    private readonly WorkLogLifecycleCodec lifecyclePreviews;
    private readonly TimeProvider clock;
    public SqlWorkLogService(SqlConnectionFactory connections, string secret, TimeProvider? clock = null)
    { this.connections=connections; capabilities=new(connections); receipts=new(secret); cursors=new(secret); historyCursors=new(secret); lifecyclePreviews=new(secret); this.clock=clock??TimeProvider.System; }
    private sealed record Receipt(int Version, Guid Actor, Guid Owner, Guid Id, string Action, string Code, int Status);

    public IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal actor)
    {
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var result=Actions.ToDictionary(action=>action, action=>Allowed(c,tx,actor,action));
        tx.Commit();
        return result.Values.Any(value=>value) ? IdentityOperationResult<IReadOnlyDictionary<string,bool>>.Success(result) : Denied<IReadOnlyDictionary<string,bool>>();
    }

    public IdentityOperationResult<WorkLogPage> List(IdentityPrincipal actor, string? status, string? query, DateOnly? from, DateOnly? to, string? cursor, string? category = null)
    {
        status??="Active"; query=Trim(query); category=Trim(category);
        if (!ValidFilter(status,query,from,to) || category is {Length:>100}) return Invalid<WorkLogPage>();
        var scope=new WorkLogCursor.Scope(actor.UserId,actor.OwnerId,status,query,from,to,category);
        var boundary=string.IsNullOrEmpty(cursor)?null:cursors.Decode(scope,cursor);
        if (!string.IsNullOrEmpty(cursor) && boundary is null) return Invalid<WorkLogPage>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLogPage>();
        using var sql=Command(c,tx,$"SELECT TOP(26) {Columns} FROM [learning].[WorkLog] w WHERE {Filter} AND {Registry} AND (@Before IS NULL OR w.RecordedOn<@Before OR (w.RecordedOn=@Before AND w.Id<@BeforeId)) ORDER BY w.RecordedOn DESC,w.Id DESC;",actor.OwnerId);
        Filters(sql,status,query,from,to,category); Add(sql,"@Before",SqlDbType.Date,boundary?.RecordedOn.ToDateTime(TimeOnly.MinValue)); Add(sql,"@BeforeId",SqlDbType.UniqueIdentifier,boundary?.Id);
        var items=new List<WorkLog>(); using(var reader=sql.ExecuteReader()) while(reader.Read()) items.Add(Row(reader));
        string? next=null;
        if(items.Count>25) { items.RemoveAt(25); var last=items[^1]; next=cursors.Encode(scope,new(last.RecordedOn,last.Id)); }
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLogPage>();
        tx.Commit(); return IdentityOperationResult<WorkLogPage>.Success(new(items,next));
    }

    public IdentityOperationResult<WorkLog> Get(IdentityPrincipal actor, Guid id)
    {
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLog>();
        var row=Read(c,tx,actor.OwnerId,id,false);
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLog>();
        tx.Commit(); return row is null?Missing<WorkLog>():IdentityOperationResult<WorkLog>.Success(row);
    }

    public IdentityOperationResult<WorkLogReport> Report(IdentityPrincipal actor, DateOnly? from, DateOnly? to, string? status, string? query, string? category = null)
    {
        status??="Active"; query=Trim(query); category=Trim(category);
        if (!from.HasValue || !to.HasValue || status=="Trash" || !ValidFilter(status,query,from,to) || category is {Length:>100} || to.Value.DayNumber-from.Value.DayNumber>365) return Invalid<WorkLogReport>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c,tx,actor,"learning.report.read")) return Denied<WorkLogReport>();
        using var sql=Command(c,tx,$"SELECT w.RecordedOn,COUNT_BIG(*),SUM(CONVERT(bigint,w.Minutes)) FROM [learning].[WorkLog] w WHERE {Filter} AND {Registry} GROUP BY w.RecordedOn ORDER BY w.RecordedOn DESC;",actor.OwnerId);
        Filters(sql,status,query,from,to,category);
        var days=new List<WorkLogDay>(); using(var reader=sql.ExecuteReader()) while(reader.Read()) days.Add(new(DateOnly.FromDateTime(reader.GetDateTime(0)),reader.GetInt64(1),reader.GetInt64(2)));
        if (!Allowed(c,tx,actor,"learning.report.read")) return Denied<WorkLogReport>();
        var report=new WorkLogReport(from.Value,to.Value,status,days.Sum(day=>day.Entries),days.Sum(day=>day.Minutes),days,Category:category);
        tx.Commit(); return IdentityOperationResult<WorkLogReport>.Success(report);
    }

    public IdentityOperationResult<object?> Save(IdentityPrincipal actor, Guid? id, string? ifMatch, WorkLogCommand command, string key, string? trace)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length>200 || !command.RecordedOn.HasValue || command.Minutes is not (>=1 and <=1440)
            || command.Notes is {Length:>20000} || command.Category is {Length:>100} || command.Employer is {Length:>200}) return Invalid<object?>();
        var normalized=command with {Title=command.Title.Trim(), Notes=Trim(command.Notes),Category=Trim(command.Category),Employer=Trim(command.Employer)};
        return Write(actor,id,ifMatch,normalized,null,key,trace);
    }
    public IdentityOperationResult<object?> Transition(IdentityPrincipal actor, Guid id, string? ifMatch, string operation, WorkLogConfirmation command, string key, string? trace)
    {
        if (operation is not ("archive" or "unarchive") || !command.Confirm) return Invalid<object?>();
        return Write(actor,id,ifMatch,null,operation,key,trace);
    }

    private IdentityOperationResult<object?> Write(IdentityPrincipal actor, Guid? id, string? ifMatch, WorkLogCommand? body, string? operation, string key, string? trace)
    {
        var action="learning.worklog."+(operation??(id.HasValue?"update":"create"));
        var status=id.HasValue?200:201; var code=operation is null?(id.HasValue?"WorkLogUpdated":"WorkLogCreated"):"WorkLogTransitioned";
        if (!Guid.TryParseExact(key,"D",out var parsed) || parsed==Guid.Empty) return Invalid<object?>();
        byte[]? version=null;
        if (id.HasValue && !Revision(ifMatch,out version)) return Fail<object?>("PreconditionRequired",428,"A quoted current rowversion is required.");
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using(var owner=Command(c,tx,"SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;",actor.OwnerId))
            { Add(owner,"@User",SqlDbType.UniqueIdentifier,actor.UserId); if(owner.ExecuteScalar() is null) return Denied<object?>(); }
            if (!Allowed(c,tx,actor,action)) return Denied<object?>();
            var canonical=JsonSerializer.Serialize(new {version=1,owner=actor.OwnerId,id,ifMatch,body,operation});
            var claim=receipts.TryClaim(c,tx,actor.UserId,action,key,canonical,clock.GetUtcNow().UtcDateTime);
            if(claim.IsConflict) return Fail<object?>("IdempotencyConflict",409,"This key belongs to a different request.");
            if(claim.IsReplay)
            {
                Receipt? stored=null;
                try { if(claim.ResultJson is not null) stored=JsonSerializer.Deserialize<Receipt>(claim.ResultJson); } catch(JsonException) { }
                if(stored is null || stored.Version!=1 || stored.Actor!=actor.UserId || stored.Owner!=actor.OwnerId || stored.Id==Guid.Empty
                    || (id.HasValue && stored.Id!=id.Value) || stored.Action!=action || stored.Code!=code || stored.Status!=status || claim.ResultCode!=code || claim.ResultStatusCode!=status)
                    return Fail<object?>("CommittedOutcomeUnavailable",409,"Reconcile the original completed outcome before starting another command.");
                if(!Allowed(c,tx,actor,action)) return Denied<object?>();
                return IdentityOperationResult<object?>.Committed(stored.Id,status,code);
            }
            if(!claim.IsClaimed) return Fail<object?>("RequestInProgress",409,"The original request is in progress.");
            var itemId=id??Guid.NewGuid();
            WorkLog? current=null;
            if(id.HasValue)
            {
                current=Read(c,tx,actor.OwnerId,itemId,true); if(current is null) return Missing<object?>();
                if(!CryptographicOperations.FixedTimeEquals(version!,Convert.FromBase64String(current.ETag[1..^1])))
                    return Fail<object?>("RevisionConflict",412,"Reload the current Work Log revision.");
                CaptureHistory(c,tx,actor,itemId,"HistoryStarted",baseline:true);
            }
            if(operation is null)
            {
                if(current is not null && current.Status!="Active") return Fail<object?>("LifecycleLocked",409,"Unarchive the entry before editing.");
                if(!id.HasValue)
                {
                    using var registry=Command(c,tx,"""
                        INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                        SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                        JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX40' AND rt.Code='WorkLog' AND rt.ContractVersion='worklog-manual-v1';
                        """,actor.OwnerId);
                    Add(registry,"@Id",SqlDbType.UniqueIdentifier,itemId); Add(registry,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                    if(registry.ExecuteNonQuery()!=1) return Fail<object?>("DependencyUnavailable",409,"The Work Log resource contract is unavailable.");
                }
                using var sql=Command(c,tx,id.HasValue?"""
                    UPDATE [learning].[WorkLog] SET Title=@Title,RecordedOn=@Day,Minutes=@Minutes,Notes=@Notes,Category=@Category,Employer=@Employer,
                        UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND RowVersion=@Version AND Status='Active';
                    """:"""
                    INSERT [learning].[WorkLog](Id,OwnerId,Title,RecordedOn,Minutes,Notes,Category,Employer,Status,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Id,@Owner,@Title,@Day,@Minutes,@Notes,@Category,@Employer,'Active',@User,@User);
                    """,actor.OwnerId);
                Add(sql,"@Id",SqlDbType.UniqueIdentifier,itemId); Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(sql,"@Title",SqlDbType.NVarChar,body!.Title,200); Add(sql,"@Day",SqlDbType.Date,body.RecordedOn!.Value.ToDateTime(TimeOnly.MinValue));
                Add(sql,"@Minutes",SqlDbType.Int,body.Minutes!.Value); Add(sql,"@Notes",SqlDbType.NVarChar,body.Notes,-1);
                Add(sql,"@Category",SqlDbType.NVarChar,body.Category,100); Add(sql,"@Employer",SqlDbType.NVarChar,body.Employer,200);
                if(id.HasValue) Add(sql,"@Version",SqlDbType.Binary,version,8);
                if(sql.ExecuteNonQuery()!=1) return Fail<object?>("RevisionConflict",412,"Reload the current Work Log revision.");
            }
            else
            {
                var expected=operation=="archive"?"Active":"Archived";
                if(current!.Status!=expected) return Fail<object?>("LifecycleLocked",409,"This lifecycle transition is unavailable.");
                using var sql=Command(c,tx,operation=="archive"?"""
                    UPDATE [learning].[WorkLog] SET Status='Archived',PreArchiveState='Active',UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                    WHERE OwnerId=@Owner AND Id=@Id AND RowVersion=@Version AND Status='Active';
                    """:"""
                    UPDATE [learning].[WorkLog] SET Status='Active',PreArchiveState=NULL,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                    WHERE OwnerId=@Owner AND Id=@Id AND RowVersion=@Version AND Status='Archived' AND PreArchiveState='Active';
                    """,actor.OwnerId);
                Add(sql,"@Id",SqlDbType.UniqueIdentifier,itemId); Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId); Add(sql,"@Version",SqlDbType.Binary,version,8);
                if(sql.ExecuteNonQuery()!=1) return Fail<object?>("RevisionConflict",412,"The current revision or prior state is unavailable.");
            }
            using(var registry=Command(c,tx,"UPDATE [platform].[Resource] SET Availability=@State,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;",actor.OwnerId))
            {
                Add(registry,"@Id",SqlDbType.UniqueIdentifier,itemId); Add(registry,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(registry,"@State",SqlDbType.VarChar,operation=="archive"?"Archived":"Active",64);
                if(registry.ExecuteNonQuery()!=1) return Missing<object?>();
            }
            CaptureHistory(c,tx,actor,itemId,operation=="archive"?"Archived":operation=="unarchive"?"Unarchived":id.HasValue?"Updated":"Created");
            using(var audit=Command(c,tx,"INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.WorkLog',@Id,'Succeeded',@Trace);",actor.OwnerId))
            {
                Add(audit,"@User",SqlDbType.UniqueIdentifier,actor.UserId); Add(audit,"@Action",SqlDbType.NVarChar,action,160);
                Add(audit,"@Id",SqlDbType.UniqueIdentifier,itemId); Add(audit,"@Trace",SqlDbType.NVarChar,trace,100); audit.ExecuteNonQuery();
            }
            receipts.Complete(c,tx,claim,code,status,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,itemId,action,code,status)));
            if(!Allowed(c,tx,actor,action)) return Denied<object?>();
            if(Read(c,tx,actor.OwnerId,itemId,false) is null) return Missing<object?>();
            if(!Allowed(c,tx,actor,action)) return Denied<object?>();
            tx.Commit(); return IdentityOperationResult<object?>.Committed(itemId,status,code);
        }
        catch(SqlException) { return Fail<object?>("PersistenceUnavailable",503,"Work Log persistence is unavailable."); }
    }

    private bool Allowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,string action) => capabilities.IsAllowed(c,tx,actor,"FX40",action)
        && (action is not ("learning.worklog.update" or "learning.report.read" or "learning.worklog.trash" or "learning.worklog.restore" or "learning.worklog.purge") || capabilities.IsAllowed(c,tx,actor,"FX40","learning.worklog.read"));
    private static WorkLog? Read(SqlConnection c,SqlTransaction tx,Guid owner,Guid id,bool update)
    {
        using var sql=Command(c,tx,$"SELECT {Columns} FROM [learning].[WorkLog] w {(update?"WITH(UPDLOCK,ROWLOCK)":"")} WHERE w.OwnerId=@Owner AND w.Id=@Id AND w.Status IN('Active','Archived','Trash') AND {Registry};",owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); using var reader=sql.ExecuteReader(); return reader.Read()?Row(reader):null;
    }
    private static WorkLog Row(SqlDataReader r)=>new(r.GetGuid(0),r.GetString(1),DateOnly.FromDateTime(r.GetDateTime(2)),r.GetInt32(3),
        r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetString(6),r.GetString(7),
        new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(8),DateTimeKind.Utc)),new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(9),DateTimeKind.Utc)),
        $"\"{Convert.ToBase64String(r.GetFieldValue<byte[]>(10))}\"",r.IsDBNull(11)?null:r.GetString(11));
    private static bool Revision(string? value,out byte[]? bytes)
    {
        bytes=null;
        if(value is null || value.Length!=14 || value[0]!='"' || value[^1]!='"') return false;
        try { bytes=Convert.FromBase64String(value[1..^1]); return bytes.Length==8 && Convert.ToBase64String(bytes)==value[1..^1]; }
        catch(FormatException) { return false; }
    }
    private static void Filters(SqlCommand sql,string status,string? query,DateOnly? from,DateOnly? to,string? category)
    { Add(sql,"@Status",SqlDbType.VarChar,status,64); Add(sql,"@Query",SqlDbType.NVarChar,query,200); Add(sql,"@From",SqlDbType.Date,from?.ToDateTime(TimeOnly.MinValue)); Add(sql,"@To",SqlDbType.Date,to?.ToDateTime(TimeOnly.MinValue)); Add(sql,"@Category",SqlDbType.NVarChar,category,100); }
    private static bool ValidFilter(string status,string? query,DateOnly? from,DateOnly? to)=>status is "Active" or "Archived" or "Trash" && query is not {Length:>200} && (!from.HasValue || !to.HasValue || from<=to);
    private static string? Trim(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static SqlCommand Command(SqlConnection c,SqlTransaction tx,string text,Guid owner)
    { var sql=c.CreateCommand(); sql.Transaction=tx; sql.CommandText=text; Add(sql,"@Owner",SqlDbType.UniqueIdentifier,owner); return sql; }
    private static void Add(SqlCommand sql,string name,SqlDbType type,object? value,int? size=null)
    { var p=size.HasValue?sql.Parameters.Add(name,type,size.Value):sql.Parameters.Add(name,type); p.Value=value??DBNull.Value; }
    private static IdentityOperationResult<T> Fail<T>(string code,int status,string title)=>IdentityOperationResult<T>.Failure(code,status,title);
    private static IdentityOperationResult<T> Denied<T>()=>Fail<T>("ModuleUnavailable",403,"The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>()=>Fail<T>("ResourceUnavailable",404,"Work Log entry unavailable.");
    private static IdentityOperationResult<T> Invalid<T>()=>Fail<T>("ValidationFailed",422,"Check the title, local date, manual minutes (1–1440), field lengths and date/filter bounds.");
}
