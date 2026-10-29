using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Infrastructure.Learning;

public sealed partial class SqlWorkLogService
{
    private static bool LifecycleOperation(string operation)=>operation is "trash" or "restore" or "purge";

    public IdentityOperationResult<WorkLogLifecyclePreview> PreviewLifecycle(IdentityPrincipal actor,Guid id,string? ifMatch,string operation)
    {
        if(!LifecycleOperation(operation))return Invalid<WorkLogLifecyclePreview>();
        if(!Revision(ifMatch,out _))return Fail<WorkLogLifecyclePreview>("PreconditionRequired",428,"A quoted current rowversion is required.");
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var action="learning.worklog."+operation;
        if(!Allowed(c,tx,actor,action))return Denied<WorkLogLifecyclePreview>();
        var row=Read(c,tx,actor.OwnerId,id,false);if(row is null)return Missing<WorkLogLifecyclePreview>();
        if(row.ETag!=ifMatch)return Fail<WorkLogLifecyclePreview>("RevisionConflict",412,"Reload the current Work Log revision.");
        var plan=LifecyclePlan(c,tx,actor.OwnerId,row,operation);
        if(plan is null)return Fail<WorkLogLifecyclePreview>("LifecycleLocked",409,"The lifecycle or exact deletion cohort is unavailable.");
        var expiry=clock.GetUtcNow().AddMinutes(5);
        var token=lifecyclePreviews.Encode(new(actor.UserId,actor.OwnerId,id,operation),new(plan,expiry));
        if(!Allowed(c,tx,actor,action))return Denied<WorkLogLifecyclePreview>();
        tx.Commit();return IdentityOperationResult<WorkLogLifecyclePreview>.Success(new(id,operation,row.ETag,plan.From,plan.To,plan.Batch,plan.History,plan.References,expiry,token));
    }

    public IdentityOperationResult<object?> Lifecycle(IdentityPrincipal actor,Guid id,string? ifMatch,string operation,
        WorkLogLifecycleCommand command,string key,string? trace)
    {
        if(!LifecycleOperation(operation)||!command.Confirm||command.PreviewToken is null||command.PreviewToken.Length>4096
            ||!Guid.TryParseExact(key,"D",out var parsed)||parsed==Guid.Empty)return Invalid<object?>();
        if(!Revision(ifMatch,out _))return Fail<object?>("PreconditionRequired",428,"A quoted current rowversion is required.");
        var action="learning.worklog."+operation;
        var code=operation switch {"trash"=>"WorkLogTrashed","restore"=>"WorkLogRestored",_=>"WorkLogPurged"};
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        try {
            using(var owner=Command(c,tx,"SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;",actor.OwnerId))
            {Add(owner,"@User",SqlDbType.UniqueIdentifier,actor.UserId);if(owner.ExecuteScalar() is null)return Denied<object?>();}
            if(!Allowed(c,tx,actor,action))return Denied<object?>();
            var claim=receipts.TryClaim(c,tx,actor.UserId,action,key,JsonSerializer.Serialize(new {version=1,owner=actor.OwnerId,id,ifMatch,operation,command}),clock.GetUtcNow().UtcDateTime);
            if(claim.IsConflict)return Fail<object?>("IdempotencyConflict",409,"This key belongs to a different request.");
            if(claim.IsReplay) {
                Receipt? saved=null;try {if(claim.ResultJson is not null)saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson);}catch(JsonException){}
                if(saved is null||saved.Version!=1||saved.Actor!=actor.UserId||saved.Owner!=actor.OwnerId||saved.Id!=id||saved.Action!=action
                    ||saved.Code!=code||saved.Status!=200||claim.ResultCode!=code||claim.ResultStatusCode!=200)
                    return Fail<object?>("CommittedOutcomeUnavailable",409,"Reconcile the original completed outcome before starting another command.");
                if(!Allowed(c,tx,actor,action))return Denied<object?>();
                return IdentityOperationResult<object?>.Committed(id,200,code);
            }
            if(!claim.IsClaimed)return Fail<object?>("RequestInProgress",409,"The original request is in progress.");
            var preview=lifecyclePreviews.Decode(new(actor.UserId,actor.OwnerId,id,operation),command.PreviewToken);
            if(preview is null)return Invalid<object?>();
            if(preview.ExpiresAt<=clock.GetUtcNow())return Fail<object?>("PreviewExpired",409,"Load a fresh preview before confirming a new request.");
            var row=Read(c,tx,actor.OwnerId,id,true);if(row is null)return Missing<object?>();
            if(row.ETag!=ifMatch)return Fail<object?>("RevisionConflict",412,"Reload the current Work Log revision.");
            var plan=LifecyclePlan(c,tx,actor.OwnerId,row,operation);
            if(plan is null||plan!=preview.Plan)return Fail<object?>("PreviewChanged",409,"The source, cohort or dependencies changed. Load a fresh preview.");
            if(operation=="purge"&&plan.References!=0)return Fail<object?>("ResourcePinned",409,"Retained references prevent permanent deletion.");
            var batch=plan.Batch??Guid.NewGuid();
            if(operation!="purge")CaptureHistory(c,tx,actor,id,"HistoryStarted",baseline:true);
            using(var cohort=Command(c,tx,operation=="trash"?"""
                INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Batch,@Owner,@Id,SYSUTCDATETIME(),'Trashed',SYSUTCDATETIME(),@User,@User);
                INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(NEWID(),@Owner,@Batch,@Id,@Previous,0,SYSUTCDATETIME(),@User,@User);
                """:"""
                UPDATE [operations].[TrashBatch] SET State=@BatchState,RestoredAt=CASE WHEN @Operation='restore' THEN SYSUTCDATETIME() ELSE NULL END,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Batch AND State='Trashed';
                IF @Operation='purge' UPDATE [operations].[TrashMember] SET PurgedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                    WHERE OwnerId=@Owner AND BatchId=@Batch AND ResourceId=@Id;
                """,actor.OwnerId)) {
                Add(cohort,"@Id",SqlDbType.UniqueIdentifier,id);Add(cohort,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(cohort,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
                Add(cohort,"@Previous",SqlDbType.VarChar,row.Status,64);Add(cohort,"@Operation",SqlDbType.VarChar,operation,32);
                Add(cohort,"@BatchState",SqlDbType.VarChar,operation=="restore"?"Restored":"Purged",64);cohort.ExecuteNonQuery();
            }
            using(var sql=Command(c,tx,operation=="purge"?"""
                DELETE [learning].[WorkLogVersion] WHERE OwnerId=@Owner AND WorkLogId=@Id;
                DELETE [learning].[WorkLog] WHERE OwnerId=@Owner AND Id=@Id AND Status='Trash';
                """:"""
                UPDATE [learning].[WorkLog] SET PreTrashState=CASE WHEN @Operation='trash' THEN Status ELSE NULL END,
                    TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch ELSE NULL END,Status=@Next,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """,actor.OwnerId)) {
                Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);
                Add(sql,"@Operation",SqlDbType.VarChar,operation,32);Add(sql,"@Next",SqlDbType.VarChar,plan.To,64);sql.ExecuteNonQuery();
            }
            using(var registry=Command(c,tx,"""
                UPDATE [platform].[Resource] SET Availability=@Next,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,
                    PurgedAt=CASE WHEN @Next='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;
                """,actor.OwnerId)) {
                Add(registry,"@Id",SqlDbType.UniqueIdentifier,id);Add(registry,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(registry,"@Next",SqlDbType.VarChar,plan.To,64);
                if(registry.ExecuteNonQuery()!=1)return Missing<object?>();
            }
            if(operation!="purge")CaptureHistory(c,tx,actor,id,operation=="trash"?"Trashed":"Restored");
            using(var audit=Command(c,tx,"INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.WorkLog',@Id,'Succeeded',@Trace);",actor.OwnerId)) {
                Add(audit,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(audit,"@Action",SqlDbType.NVarChar,action,160);Add(audit,"@Id",SqlDbType.UniqueIdentifier,id);Add(audit,"@Trace",SqlDbType.NVarChar,trace,100);audit.ExecuteNonQuery();
            }
            receipts.Complete(c,tx,claim,code,200,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,id,action,code,200)));
            if(!Allowed(c,tx,actor,action))return Denied<object?>();
            if(!LifecycleFinal(c,tx,actor.OwnerId,id,plan.To,batch,operation,operation=="trash"?row.Status:plan.Prior!,operation=="purge"?0:plan.History+(plan.History==0?2:1)))return Fail<object?>("LifecycleLocked",409,"The committed lifecycle projection is unavailable.");
            if(!Allowed(c,tx,actor,action))return Denied<object?>();
            tx.Commit();return IdentityOperationResult<object?>.Committed(id,200,code);
        }catch(SqlException){return Fail<object?>("PersistenceUnavailable",503,"Work Log persistence is unavailable.");}
    }

    private static WorkLogLifecycleCodec.Plan? LifecyclePlan(SqlConnection c,SqlTransaction tx,Guid owner,WorkLog row,string operation)
    {
        var next=operation switch {"trash" when row.Status is "Active" or "Archived"=>"Trash","restore" when row.Status=="Trash"=>row.PreTrashState,"purge" when row.Status=="Trash"=>"Purged",_=>null};
        if(next is null || (operation=="restore"&&next is not ("Active" or "Archived")))return null;
        Guid? batch=null;string? cohort=null;
        if(row.Status=="Trash") {
            using var sql=Command(c,tx,"""
                SELECT b.Id,b.RowVersion,m.RowVersion FROM [learning].[WorkLog] w
                JOIN [operations].[TrashBatch] b ON b.OwnerId=w.OwnerId AND b.Id=w.TrashBatchId
                JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id
                WHERE w.OwnerId=@Owner AND w.Id=@Id AND b.RootResourceId=w.Id AND b.State='Trashed'
                    AND m.ResourceId=w.Id AND m.PreviousLifecycle=w.PreTrashState AND m.Depth=0 AND m.ParentResourceId IS NULL AND m.PurgedAt IS NULL
                    AND (SELECT COUNT_BIG(*) FROM [operations].[TrashMember] allMembers WHERE allMembers.OwnerId=@Owner AND allMembers.BatchId=b.Id)=1;
                """,owner);
            Add(sql,"@Id",SqlDbType.UniqueIdentifier,row.Id);using var r=sql.ExecuteReader();if(!r.Read())return null;
            batch=r.GetGuid(0);cohort=Convert.ToBase64String(r.GetFieldValue<byte[]>(1))+":"+Convert.ToBase64String(r.GetFieldValue<byte[]>(2));
        }
        using var counts=Command(c,tx,"""
            SELECT (SELECT COUNT_BIG(*) FROM [learning].[WorkLogVersion] WHERE OwnerId=@Owner AND WorkLogId=@Id),
                (SELECT COUNT_BIG(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached');
            """,owner);
        Add(counts,"@Id",SqlDbType.UniqueIdentifier,row.Id);using var reader=counts.ExecuteReader();reader.Read();
        return new(row.ETag,row.Status,next,batch,row.PreTrashState,reader.GetInt64(0),reader.GetInt64(1),cohort);
    }

    private static bool LifecycleFinal(SqlConnection c,SqlTransaction tx,Guid owner,Guid id,string state,Guid batch,string operation,string prior,long history)
    {
        using var sql=Command(c,tx,"""
            SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
            JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=@Owner AND r.Id=@Id
                AND m.Code='FX40' AND rt.Code='WorkLog' AND rt.ContractVersion='worklog-manual-v1' AND r.Availability=@State
                AND EXISTS(SELECT 1 FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] tm ON tm.OwnerId=b.OwnerId AND tm.BatchId=b.Id
                    WHERE b.OwnerId=@Owner AND b.Id=@Batch AND b.RootResourceId=@Id AND b.State=@BatchState
                        AND tm.ResourceId=@Id AND tm.PreviousLifecycle=@Prior AND tm.Depth=0 AND tm.ParentResourceId IS NULL
                        AND ((@State='Purged' AND tm.PurgedAt IS NOT NULL) OR (@State<>'Purged' AND tm.PurgedAt IS NULL))
                        AND (SELECT COUNT_BIG(*) FROM [operations].[TrashMember] allMembers WHERE allMembers.OwnerId=@Owner AND allMembers.BatchId=@Batch)=1)
                AND (SELECT COUNT_BIG(*) FROM [learning].[WorkLogVersion] v WHERE v.OwnerId=@Owner AND v.WorkLogId=@Id)=@History
                AND ((@State='Purged' AND r.PurgedAt IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [learning].[WorkLog] w WHERE w.OwnerId=@Owner AND w.Id=@Id)
                    AND NOT EXISTS(SELECT 1 FROM [platform].[ResourceLink] l WHERE l.OwnerId=@Owner AND (l.SourceResourceId=@Id OR l.TargetResourceId=@Id) AND l.State<>'Detached'))
                  OR (@State<>'Purged' AND r.PurgedAt IS NULL AND EXISTS(SELECT 1 FROM [learning].[WorkLog] w WHERE w.OwnerId=@Owner AND w.Id=@Id AND w.Status=@State
                    AND ((@State='Trash' AND w.TrashBatchId=@Batch AND w.PreTrashState=@Prior) OR (@State<>'Trash' AND w.TrashBatchId IS NULL AND w.PreTrashState IS NULL)))));
            """,owner);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@State",SqlDbType.VarChar,state,64);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);
        Add(sql,"@BatchState",SqlDbType.VarChar,operation=="trash"?"Trashed":operation=="restore"?"Restored":"Purged",64);
        Add(sql,"@Prior",SqlDbType.VarChar,prior,64);Add(sql,"@History",SqlDbType.BigInt,history);return sql.ExecuteScalar() is not null;
    }
}
