using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Learning;

/// <summary>Inert until the source contract and independent gates are reviewed.
/// Every authority/source projection is resolved within the same SQL transaction.</summary>
public sealed class SqlLearningPlanService : ILearningPlanService
{
    public static readonly string[] Actions = ["learning.plan.read","learning.plan.create","learning.plan.update",
        "learning.plan.link","learning.plan.unlink","learning.plan.reorder","learning.plan.complete",
        "learning.plan.archive","learning.plan.unarchive"];
    private const string Columns = "p.Id,p.Title,p.StartsOn,p.EndsOn,p.Notes,p.Status,p.UpdatedAt,p.RowVersion";
    private const string Registry = """
        EXISTS(SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
          JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=p.OwnerId AND r.Id=p.Id
          AND m.Code='FX40' AND rt.Code='Plan' AND rt.ContractVersion='learning-plans-v1' AND r.PurgedAt IS NULL
          AND r.Availability=CASE WHEN p.Status='Archived' THEN 'Archived' ELSE 'Active' END)
        """;
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly SqlLearningPlanSources sources;
    private readonly LearningPlanCursor cursors;
    private readonly LearningPlanReadiness readiness;
    private readonly TimeProvider clock;
    public SqlLearningPlanService(SqlConnectionFactory connections, string secret, LearningPlanReadiness? readiness = null, TimeProvider? clock = null)
    { this.connections=connections; capabilities=new(connections); receipts=new(secret); sources=new(capabilities); cursors=new(secret);
      this.readiness=readiness??new(); this.clock=clock??TimeProvider.System; }
    private sealed record Receipt(int Version, Guid Actor, Guid Owner, Guid Plan, string Action, int Status, LearningPlanSourceRef? Source);
    private sealed record Item(Guid Id, LearningPlanSourceRef Source, int Position, DateOnly? TargetOn);

    public IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal a)
    {
        if (!readiness.ReviewedLocalContract) return Denied<IReadOnlyDictionary<string,bool>>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var values=Actions.ToDictionary(action=>action,action=>Allowed(c,tx,a,action)); tx.Commit();
        return IdentityOperationResult<IReadOnlyDictionary<string,bool>>.Success(values);
    }
    public IdentityOperationResult<LearningPlanPage> List(IdentityPrincipal a,string? status,string? cursor)
    {
        if(!readiness.ReviewedLocalContract) return Denied<LearningPlanPage>();
        status??="Planned";
        if(status is not ("Planned" or "Active" or "Completed" or "Archived")) return Invalid<LearningPlanPage>();
        var scope=new LearningPlanCursor.Scope(a.UserId,a.OwnerId,"learning.plan.read",status);
        var boundary=cursor is null?null:cursors.Decode(scope,cursor);
        if(cursor is not null && boundary is null) return Invalid<LearningPlanPage>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,a,"learning.plan.read")) return Denied<LearningPlanPage>();
        using var sql=Command(c,tx,$"SELECT TOP(26) {Columns} FROM [learning].[Plan] p WHERE p.OwnerId=@Owner AND p.Status=@Status AND {Registry} AND (@Before IS NULL OR p.UpdatedAt<@Before OR (p.UpdatedAt=@Before AND p.Id<@BeforeId)) ORDER BY p.UpdatedAt DESC,p.Id DESC;",a.OwnerId);
        Add(sql,"@Status",SqlDbType.VarChar,status,64); Add(sql,"@Before",SqlDbType.DateTime2,boundary?.UpdatedAt); Add(sql,"@BeforeId",SqlDbType.UniqueIdentifier,boundary?.Id);
        var rows=new List<LearningPlan>(); using(var r=sql.ExecuteReader()) while(r.Read()) rows.Add(Row(r));
        string? next=null; if(rows.Count>25) { rows.RemoveAt(25); var last=rows[^1]; next=cursors.Encode(scope,new(last.UpdatedAt.UtcDateTime,last.Id)); }
        if(!Allowed(c,tx,a,"learning.plan.read")) return Denied<LearningPlanPage>();
        tx.Commit(); return IdentityOperationResult<LearningPlanPage>.Success(new(rows,next));
    }
    public IdentityOperationResult<LearningPlanSourcePage> Sources(IdentityPrincipal a,string kind,string? cursor)
    {
        if(!readiness.ReviewedLocalContract) return Denied<LearningPlanSourcePage>();
        var p=SqlLearningPlanSources.Participant(kind); if(p is null) return Invalid<LearningPlanSourcePage>();
        var scope=new LearningPlanCursor.Scope(a.UserId,a.OwnerId,"learning.plan.link",kind);
        var boundary=cursor is null?null:cursors.Decode(scope,cursor); if(cursor is not null && boundary is null) return Invalid<LearningPlanSourcePage>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,a,"learning.plan.link") || !sources.CanRead(c,tx,a,p)) return Denied<LearningPlanSourcePage>();
        var rows=sources.Choices(c,tx,a,p,boundary); string? next=null;
        if(rows.Count>25) { rows.RemoveAt(25); var last=rows[^1]; next=cursors.Encode(scope,new(last.Updated,last.Id)); }
        if(!Allowed(c,tx,a,"learning.plan.link") || !sources.CanRead(c,tx,a,p)) return Denied<LearningPlanSourcePage>();
        tx.Commit(); return IdentityOperationResult<LearningPlanSourcePage>.Success(new(rows.Select(row=>new LearningPlanSourceChoice(new(kind,row.Id),row.Title)).ToArray(),next));
    }
    public IdentityOperationResult<LearningPlanDetail> Get(IdentityPrincipal a,Guid id)
    {
        if(!readiness.ReviewedLocalContract) return Denied<LearningPlanDetail>();
        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,a,"learning.plan.read")) return Denied<LearningPlanDetail>();
        var plan=Read(c,tx,a.OwnerId,id,false); if(plan is null) return Missing<LearningPlanDetail>();
        var items=Items(c,tx,a.OwnerId,id); var projections=new Dictionary<Guid,LearningPlanSource>();
        foreach(var item in items.OrderBy(i=>i.Source.Kind,StringComparer.Ordinal).ThenBy(i=>i.Source.Id))
            projections[item.Id]=sources.Read(c,tx,a,item.Source);
        // A source revoke late in projection masks its content before response assembly.
        foreach(var item in items)
        {
            var p=SqlLearningPlanSources.Participant(item.Source.Kind);
            if(p is null || !sources.CanRead(c,tx,a,p)) projections[item.Id]=SqlLearningPlanSources.Unavailable();
        }
        var result=items.Select(i=>new LearningPlanItem(i.Id,i.Source,i.Position,i.TargetOn,projections[i.Id])).ToArray();
        var unavailable=result.Count(i=>i.Projection.Status=="Unavailable"); var known=result.Length-unavailable;
        if(!Allowed(c,tx,a,"learning.plan.read")) return Denied<LearningPlanDetail>();
        tx.Commit(); return IdentityOperationResult<LearningPlanDetail>.Success(new(plan,result,known,unavailable,LearningPlanRules.ProgressLabel(known,unavailable)));
    }
    public IdentityOperationResult<object?> Create(IdentityPrincipal a,LearningPlanMetadata body,string key,string? trace) =>
        Write(a,null,null,"create",body,key,trace,(c,tx,id)=>
        {
            if(!LearningPlanRules.ValidMetadata(body)) return Invalid<object?>();
            using var registry=Command(c,tx,"""
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX40' AND rt.Code='Plan' AND rt.ContractVersion='learning-plans-v1';
                """,a.OwnerId);
            Identity(registry,a,id); if(registry.ExecuteNonQuery()!=1) return Fail<object?>("DependencyUnavailable",409,"The Plan registry is unavailable.");
            using var sql=Command(c,tx,"""
                INSERT [learning].[Plan](Id,OwnerId,Title,StartsOn,EndsOn,Notes,Status,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Starts,@Ends,@Notes,'Planned',@User,@User);
                """,a.OwnerId);
            Identity(sql,a,id); Metadata(sql,body); sql.ExecuteNonQuery(); return null;
        });
    public IdentityOperationResult<object?> Update(IdentityPrincipal a,Guid id,string? etag,LearningPlanMetadata body,string key,string? trace) =>
        Write(a,id,etag,"update",body,key,trace,(c,tx,_)=>
        {
            if(!LearningPlanRules.ValidMetadata(body)) return Invalid<object?>();
            using var sql=Command(c,tx,"UPDATE [learning].[Plan] SET Title=@Title,StartsOn=@Starts,EndsOn=@Ends,Notes=@Notes WHERE OwnerId=@Owner AND Id=@Id;",a.OwnerId);
            Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Metadata(sql,body); sql.ExecuteNonQuery(); return null;
        });
    public IdentityOperationResult<object?> Link(IdentityPrincipal a,Guid id,string? etag,LearningPlanLink body,string key,string? trace) =>
        Write(a,id,etag,"link",body,key,trace,(c,tx,_)=>
        {
            if(!LearningPlanRules.ValidSource(body.Source)) return Invalid<object?>();
            if(sources.Read(c,tx,a,body.Source,true).Status=="Unavailable") return Missing<object?>();
            var items=Items(c,tx,a.OwnerId,id);
            if(items.Any(i=>i.Source==body.Source)) return Fail<object?>("DuplicateSource",409,"This source is already linked.");
            if(items.Count==int.MaxValue || (items.Count>0 && items.Max(i=>i.Position)==int.MaxValue)) return Invalid<object?>();
            using var sql=Command(c,tx,"""
                INSERT [learning].[PlanItem](Id,OwnerId,PlanId,ResourceKind,ResourceId,Position,TargetOn,CreatedByUserId,UpdatedByUserId)
                VALUES(@Item,@Owner,@Id,@Kind,@Source,@Position,@Target,@User,@User);
                """,a.OwnerId);
            Identity(sql,a,id); Add(sql,"@Item",SqlDbType.UniqueIdentifier,Guid.NewGuid()); Add(sql,"@Kind",SqlDbType.VarChar,body.Source.Kind,16);
            Add(sql,"@Source",SqlDbType.UniqueIdentifier,body.Source.Id); Add(sql,"@Position",SqlDbType.Int,items.Count==0?0:items.Max(i=>i.Position)+1);
            Add(sql,"@Target",SqlDbType.Date,body.TargetOn?.ToDateTime(TimeOnly.MinValue)); sql.ExecuteNonQuery(); return null;
        },body.Source);
    public IdentityOperationResult<object?> Unlink(IdentityPrincipal a,Guid id,Guid itemId,string? etag,string key,string? trace) =>
        Write(a,id,etag,"unlink",new {itemId},key,trace,(c,tx,_)=>
        {
            using var sql=Command(c,tx,"DELETE [learning].[PlanItem] WHERE OwnerId=@Owner AND PlanId=@Id AND Id=@Item;",a.OwnerId);
            Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Add(sql,"@Item",SqlDbType.UniqueIdentifier,itemId);
            return sql.ExecuteNonQuery()==1?null:Missing<object?>();
        });
    public IdentityOperationResult<object?> Reorder(IdentityPrincipal a,Guid id,string? etag,LearningPlanOrder body,string key,string? trace) =>
        Write(a,id,etag,"reorder",body,key,trace,(c,tx,_)=>
        {
            var items=Items(c,tx,a.OwnerId,id);
            if(!LearningPlanRules.ExactOrder(body.ItemIds,items.Select(i=>i.Id).ToArray())) return Invalid<object?>();
            // Two positive ranges prevent transient unique-index collisions. All
            // positions are moved beyond the old maximum before compacting to 0..N-1.
            var max=items.Count==0?-1:items.Max(i=>i.Position);
            if((long)max+items.Count>int.MaxValue) return Fail<object?>("PositionUnavailable",409,"The ordering limit is reached.");
            for(var n=0;n<body.ItemIds.Count;n++) Position(c,tx,a,id,body.ItemIds[n],max+1+n);
            for(var n=0;n<body.ItemIds.Count;n++) Position(c,tx,a,id,body.ItemIds[n],n);
            return null;
        });
    public IdentityOperationResult<object?> Complete(IdentityPrincipal a,Guid id,string? etag,LearningPlanConfirmation body,string key,string? trace) =>
        Write(a,id,etag,"complete",body,key,trace,(c,tx,_)=>
        {
            if(!body.Confirm) return Invalid<object?>();
            using var sql=Command(c,tx,"UPDATE [learning].[Plan] SET Status='Completed' WHERE OwnerId=@Owner AND Id=@Id AND Status IN('Planned','Active');",a.OwnerId);
            Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);
            return sql.ExecuteNonQuery()==1?null:Fail<object?>("LifecycleLocked",409,"The Plan cannot be completed from its current state.");
        });

    public IdentityOperationResult<LearningPlanLifecyclePreview> PreviewLifecycle(IdentityPrincipal a,Guid id,string? etag,string operation)
    {
        if(!readiness.ReviewedLocalContract) return Denied<LearningPlanLifecyclePreview>();
        if(operation is not ("archive" or "unarchive")) return Invalid<LearningPlanLifecyclePreview>();
        using var c=connections.Create();
        try
        {
            c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
            using(var timeout=Command(c,tx,"SET LOCK_TIMEOUT 5000;",a.OwnerId)) timeout.ExecuteNonQuery();
            var action="learning.plan."+operation;
            if(!Allowed(c,tx,a,action)) return Denied<LearningPlanLifecyclePreview>();
            if(!LearningPlanRules.TryRevision(etag,out _)) return Fail<LearningPlanLifecyclePreview>("PreconditionRequired",428,"A quoted current ETag is required.");
            var plan=Read(c,tx,a.OwnerId,id,false); if(plan is null) return Missing<LearningPlanLifecyclePreview>();
            if(plan.ETag!=etag) return Fail<LearningPlanLifecyclePreview>("RevisionConflict",412,"Reload the current Plan before reviewing its impact.");
            var next=LifecycleDestination(c,tx,a.OwnerId,plan,operation);
            if(next is null) return Fail<LearningPlanLifecyclePreview>("LifecycleLocked",409,"The current or exact prior Plan state does not allow this operation.");
            var count=ItemCount(c,tx,a.OwnerId,id);
            var expiry=clock.GetUtcNow().AddMinutes(5);
            var token=cursors.EncodeLifecycle(new(a.UserId,a.OwnerId,id,action),new(plan.ETag,plan.Status,next,count,expiry));
            if(!Allowed(c,tx,a,action)) return Denied<LearningPlanLifecyclePreview>();
            tx.Commit();
            return IdentityOperationResult<LearningPlanLifecyclePreview>.Success(new(id,operation,plan.ETag,plan.Status,next,count,expiry,token));
        }
        catch(SqlException) { return Fail<LearningPlanLifecyclePreview>("PersistenceUnavailable",503,"The Plan impact could not be confirmed. Reload the preview."); }
    }
    public IdentityOperationResult<object?> Archive(IdentityPrincipal a,Guid id,string? etag,LearningPlanLifecycleConfirmation body,string key,string? trace) =>
        Lifecycle(a,id,etag,"archive",body,key,trace);
    public IdentityOperationResult<object?> Unarchive(IdentityPrincipal a,Guid id,string? etag,LearningPlanLifecycleConfirmation body,string key,string? trace) =>
        Lifecycle(a,id,etag,"unarchive",body,key,trace);
    private IdentityOperationResult<object?> Lifecycle(IdentityPrincipal a,Guid id,string? etag,string operation,LearningPlanLifecycleConfirmation body,string key,string? trace) =>
        Write(a,id,etag,operation,body,key,trace,(c,tx,_)=>
        {
            if(!body.Confirm) return Invalid<object?>();
            var proof=cursors.DecodeLifecycle(new(a.UserId,a.OwnerId,id,"learning.plan."+operation),body.PreviewToken);
            if(proof is null || proof.ETag!=etag) return Invalid<object?>();
            if(proof.ExpiresAt<=clock.GetUtcNow()) return Fail<object?>("PreviewExpired",410,"Review the current Plan impact again.");
            var plan=Read(c,tx,a.OwnerId,id,true); if(plan is null) return Missing<object?>();
            var next=LifecycleDestination(c,tx,a.OwnerId,plan,operation);
            if(next is null) return Fail<object?>("LifecycleLocked",409,"The current or exact prior Plan state does not allow this operation.");
            if(proof.FromState!=plan.Status || proof.ToState!=next || proof.RetainedItems!=ItemCount(c,tx,a.OwnerId,id))
                return Fail<object?>("PreviewChanged",409,"The Plan impact changed. Review it again.");
            using var sql=Command(c,tx,"""
                UPDATE [learning].[Plan] SET Status=@Next,PreArchiveState=@Prior
                WHERE OwnerId=@Owner AND Id=@Id AND RowVersion=@Version;
                """,a.OwnerId);
            Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Add(sql,"@Next",SqlDbType.VarChar,next,64);
            Add(sql,"@Prior",SqlDbType.VarChar,operation=="archive"?plan.Status:null,64);
            LearningPlanRules.TryRevision(etag,out var version); Add(sql,"@Version",SqlDbType.Binary,version,8);
            return sql.ExecuteNonQuery()==1?null:Fail<object?>("RevisionConflict",412,"Reload the current Plan before changing this intent.");
        });
    private static string? LifecycleDestination(SqlConnection c,SqlTransaction tx,Guid owner,LearningPlan plan,string operation)
    {
        if(operation=="archive") return plan.Status is "Planned" or "Active" or "Completed" ? "Archived" : null;
        if(operation!="unarchive" || plan.Status!="Archived") return null;
        using var sql=Command(c,tx,"SELECT PreArchiveState FROM [learning].[Plan] WHERE OwnerId=@Owner AND Id=@Id;",owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,plan.Id);
        return sql.ExecuteScalar() is string prior && prior is "Planned" or "Active" or "Completed" ? prior : null;
    }
    private static int ItemCount(SqlConnection c,SqlTransaction tx,Guid owner,Guid id)
    {
        using var sql=Command(c,tx,"SELECT COUNT(*) FROM [learning].[PlanItem] WHERE OwnerId=@Owner AND PlanId=@Id;",owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); return (int)sql.ExecuteScalar()!;
    }

    private IdentityOperationResult<object?> Write(IdentityPrincipal a,Guid? requested,string? etag,string op,object body,string key,string? trace,
        Func<SqlConnection,SqlTransaction,Guid,IdentityOperationResult<object?>?> work,LearningPlanSourceRef? source=null)
    {
        if(!readiness.ReviewedLocalContract) return Denied<object?>();
        var action="learning.plan."+op; var status=op=="create"?201:200;
        if(!Guid.TryParseExact(key,"D",out var parsed) || parsed==Guid.Empty) return Invalid<object?>();
        using var c=connections.Create();
        try
        {
            c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
            using(var timeout=Command(c,tx,"SET LOCK_TIMEOUT 5000;",a.OwnerId)) timeout.ExecuteNonQuery();
            using(var owner=Command(c,tx,"SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;",a.OwnerId))
            { Add(owner,"@User",SqlDbType.UniqueIdentifier,a.UserId); if(owner.ExecuteScalar() is null) return Denied<object?>(); }
            if(!Allowed(c,tx,a,action)) return Denied<object?>();
            var canonical=JsonSerializer.Serialize(new {version=1,owner=a.OwnerId,requested,etag,op,body});
            var claim=receipts.TryClaim(c,tx,a.UserId,action,key,canonical,clock.GetUtcNow().UtcDateTime);
            if(claim.IsConflict) return Fail<object?>("IdempotencyConflict",409,"This key belongs to a different request.");
            if(claim.IsReplay)
            {
                Receipt? saved=null; try { if(claim.ResultJson is not null) saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson); } catch(JsonException) { }
                if(saved is null || saved.Version!=1 || saved.Actor!=a.UserId || saved.Owner!=a.OwnerId || saved.Plan==Guid.Empty ||
                    (requested.HasValue && saved.Plan!=requested.Value) || saved.Action!=action || saved.Status!=status ||
                    saved.Source!=source || claim.ResultCode!="PlanCommitted" || claim.ResultStatusCode!=status)
                    return Fail<object?>("CommittedOutcomeUnavailable",409,"Reconcile the original committed outcome.");
                if(!Allowed(c,tx,a,action) || !OriginalAuthority(c,tx,a,saved.Plan)) return Denied<object?>();
                if(source is not null && (SqlLearningPlanSources.Participant(source.Kind) is not {} participant || !sources.CanRead(c,tx,a,participant))) return Denied<object?>();
                return IdentityOperationResult<object?>.Committed(saved.Plan,status,"PlanCommitted");
            }
            if(!claim.IsClaimed) return Fail<object?>("RequestInProgress",409,"Retry the original request with its original key.");
            var id=requested??Guid.NewGuid();
            string? priorState=null;
            if(requested.HasValue)
            {
                if(!LearningPlanRules.TryRevision(etag,out _)) return Fail<object?>("PreconditionRequired",428,"A quoted current ETag is required.");
                var current=Read(c,tx,a.OwnerId,id,true); if(current is null) return Missing<object?>();
                if(current.ETag!=etag) return Fail<object?>("RevisionConflict",412,"Reload the current Plan before changing this intent.");
                priorState=current.Status;
                if(current.Status=="Archived" && op!="unarchive") return Fail<object?>("LifecycleLocked",409,"Archived Plans are read-only.");
            }
            var error=work(c,tx,id); if(error is not null) return error;
            using(var touch=Command(c,tx,"UPDATE [learning].[Plan] SET UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id; UPDATE r SET Availability=CASE WHEN p.Status='Archived' THEN 'Archived' ELSE 'Active' END,Revision=r.Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User FROM [platform].[Resource] r JOIN [learning].[Plan] p ON p.OwnerId=r.OwnerId AND p.Id=r.Id WHERE r.OwnerId=@Owner AND r.Id=@Id;",a.OwnerId))
            { Identity(touch,a,id); touch.ExecuteNonQuery(); }
            using(var audit=Command(c,tx,"INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId,RedactedDiffJson) VALUES(@User,@User,@Action,'learning.Plan',@Id,'Succeeded',@Trace,@Diff);",a.OwnerId))
            {
                Identity(audit,a,id); Add(audit,"@Action",SqlDbType.NVarChar,action,160); Add(audit,"@Trace",SqlDbType.NVarChar,trace,100);
                Add(audit,"@Diff",SqlDbType.NVarChar,op is "archive" or "unarchive" ? JsonSerializer.Serialize(new {fromState=priorState,toState=Read(c,tx,a.OwnerId,id,false)?.Status}) : null,-1);
                audit.ExecuteNonQuery();
            }
            receipts.Complete(c,tx,claim,"PlanCommitted",status,JsonSerializer.Serialize(new Receipt(1,a.UserId,a.OwnerId,id,action,status,source)));
            if(!Allowed(c,tx,a,action) || !OriginalAuthority(c,tx,a,id)) return Denied<object?>();
            if(source is not null && sources.Read(c,tx,a,source).Status=="Unavailable") return Missing<object?>();
            tx.Commit(); return IdentityOperationResult<object?>.Committed(id,status,"PlanCommitted");
        }
        catch(SqlException) { return Fail<object?>("PersistenceUnavailable",503,"The outcome is unconfirmed. Retry the original request and key."); }
    }
    private bool Allowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal a,string action) => readiness.ReviewedLocalContract &&
        capabilities.IsAllowed(c,tx,a,"FX40",action) && (action is "learning.plan.create" or "learning.plan.read" || capabilities.IsAllowed(c,tx,a,"FX40","learning.plan.read"));
    private static bool OriginalAuthority(SqlConnection c,SqlTransaction tx,IdentityPrincipal a,Guid id)
    {
        using var sql=Command(c,tx,$"SELECT 1 FROM [learning].[Plan] p WHERE p.OwnerId=@Owner AND p.Id=@Id AND {Registry};",a.OwnerId);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); return sql.ExecuteScalar() is not null;
    }
    private static LearningPlan? Read(SqlConnection c,SqlTransaction tx,Guid owner,Guid id,bool update)
    {
        using var sql=Command(c,tx,$"SELECT {Columns} FROM [learning].[Plan] p {(update?"WITH(UPDLOCK,ROWLOCK)":"")} WHERE p.OwnerId=@Owner AND p.Id=@Id AND {Registry};",owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); using var r=sql.ExecuteReader(); return r.Read()?Row(r):null;
    }
    private static List<Item> Items(SqlConnection c,SqlTransaction tx,Guid owner,Guid id)
    {
        using var sql=Command(c,tx,"SELECT Id,ResourceKind,ResourceId,Position,TargetOn FROM [learning].[PlanItem] WHERE OwnerId=@Owner AND PlanId=@Id ORDER BY Position,Id;",owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); using var r=sql.ExecuteReader(); var result=new List<Item>();
        while(r.Read()) result.Add(new(r.GetGuid(0),new(r.GetString(1),r.GetGuid(2)),r.GetInt32(3),r.IsDBNull(4)?null:DateOnly.FromDateTime(r.GetDateTime(4)))); return result;
    }
    private static LearningPlan Row(SqlDataReader r) => new(r.GetGuid(0),r.GetString(1),r.IsDBNull(2)?null:DateOnly.FromDateTime(r.GetDateTime(2)),
        r.IsDBNull(3)?null:DateOnly.FromDateTime(r.GetDateTime(3)),r.IsDBNull(4)?null:r.GetString(4),r.GetString(5),
        new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(6),DateTimeKind.Utc)),"\""+Convert.ToBase64String(r.GetFieldValue<byte[]>(7))+"\"");
    private static void Position(SqlConnection c,SqlTransaction tx,IdentityPrincipal a,Guid id,Guid item,int position)
    {
        using var sql=Command(c,tx,"UPDATE [learning].[PlanItem] SET Position=@Position,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND PlanId=@Id AND Id=@Item;",a.OwnerId);
        Identity(sql,a,id); Add(sql,"@Item",SqlDbType.UniqueIdentifier,item); Add(sql,"@Position",SqlDbType.Int,position); sql.ExecuteNonQuery();
    }
    private static void Metadata(SqlCommand sql,LearningPlanMetadata b)
    { Add(sql,"@Title",SqlDbType.NVarChar,b.Title.Trim(),200); Add(sql,"@Starts",SqlDbType.Date,b.StartsOn?.ToDateTime(TimeOnly.MinValue)); Add(sql,"@Ends",SqlDbType.Date,b.EndsOn?.ToDateTime(TimeOnly.MinValue)); Add(sql,"@Notes",SqlDbType.NVarChar,b.Notes?.Trim(),-1); }
    private static void Identity(SqlCommand sql,IdentityPrincipal a,Guid id)
    { Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Add(sql,"@User",SqlDbType.UniqueIdentifier,a.UserId); }
    private static SqlCommand Command(SqlConnection c,SqlTransaction tx,string sql,Guid owner)
    { var cmd=c.CreateCommand(); cmd.Transaction=tx; cmd.CommandText=sql; Add(cmd,"@Owner",SqlDbType.UniqueIdentifier,owner); return cmd; }
    private static void Add(SqlCommand sql,string name,SqlDbType type,object? value,int size=0)
    { var p=sql.Parameters.Add(name,type); if(size!=0) p.Size=size; p.Value=value??DBNull.Value; }
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable",403,"The reviewed Plan contract or current action is unavailable.");
    private static IdentityOperationResult<T> Invalid<T>() => Fail<T>("ValidationFailed",422,"Check Plan fields, source type and exact item order.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable",404,"The resource is unavailable.");
    private static IdentityOperationResult<T> Fail<T>(string code,int status,string title) => IdentityOperationResult<T>.Failure(code,status,title);
}
