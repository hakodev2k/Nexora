using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    private sealed record Work(Guid Owner, Guid Id, Guid Definition, long Version, IdentityPrincipal Actor, Guid Token);
    private sealed record PendingStep(int Id, int Position, int Attempts, Guid Key);
    // Each call processes one ordered local step. SQL is authoritative; no in-memory job queue.
    public void ProcessNext(IProductivityService tasks, CancellationToken stoppingToken = default)
    {
        if (stoppingToken.IsCancellationRequested) return;
        var work = Claim(); if (work is null) return;
        PendingStep? pending;
        using (var c = connections.Create())
        {
            c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            LockOwner(c, tx, work.Actor);
            if (!Fence(c, tx, work)) return;
            if (!Ready(c, tx, work)) { tx.Commit(); return; }
            using var cmd = Command(c, tx, "SELECT TOP(1) StepId,Position,Attempts,CommandKey FROM [automation].[StepRun] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND RunId=@Run AND State IN('Queued','Running') ORDER BY Position;", work.Owner);
            Add(cmd,"@Run",SqlDbType.UniqueIdentifier,work.Id);
            using (var r=cmd.ExecuteReader()) pending=r.Read()?new(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetGuid(3)):null;
            if(pending is null) { Finish(c,tx,work.Owner,work.Id,"Succeeded","RunSucceeded",false); Audit(c,tx,work.Actor,"automation.step.dispatch",work.Id,null); tx.Commit(); return; }
            if(pending.Attempts>=4) { FailWork(c,tx,work,"AttemptsExhausted",true); tx.Commit(); return; }
            using var attempt=Command(c,tx,"UPDATE [automation].[StepRun] SET Attempts=Attempts+1,State='Running',EffectUncertain=1,Code='EffectPending' WHERE OwnerId=@Owner AND RunId=@Run AND StepId=@Step;",work.Owner);
            Add(attempt,"@Run",SqlDbType.UniqueIdentifier,work.Id); Add(attempt,"@Step",SqlDbType.Int,pending.Id); if(attempt.ExecuteNonQuery()!=1)throw new InvalidOperationException();
            tx.Commit(); // Durable intent and attempt before invoking Task service.
        }
        using (var c=connections.Create())
        {
            if (stoppingToken.IsCancellationRequested) return; // Durable intent remains recoverable; no new effect on shutdown.
            c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
            LockOwner(c,tx,work.Actor); if(!Fence(c,tx,work))return;
            if(!Ready(c,tx,work)) { tx.Commit(); return; }
            var version=Versions(c,tx,work.Owner,work.Definition,work.Version+1,1).SingleOrDefault();
            var parsed=AutomationDefinitionContract.Parse(version?.DefinitionJson);
            if(version?.VersionNumber!=work.Version || !parsed.Accepted || !TrustedExecution(parsed.Definition!) || pending.Position>parsed.Definition!.Steps.Length || parsed.Definition.Steps[pending.Position-1].Id!=pending.Id)
            { FailWork(c,tx,work,"VersionUnavailable",true); tx.Commit(); return; }
            var step=parsed.Definition.Steps[pending.Position-1];
            if(CheckTargets(c,tx,work.Actor,new(1,parsed.Definition.Trigger,[step])) is not null)
            { FailWork(c,tx,work,"TargetUnavailable",true); tx.Commit(); return; }
            // Exact registered v1 adapter. Existing Task domain owns validation, projections,
            // lifecycle, audit and transaction/receipt; never manufacture Task rows here.
            IdentityOperationResult<TaskRecord> result;
            if (stoppingToken.IsCancellationRequested) return; // Also check after potentially blocking authority/target reads.
            try
            {
                var input=step.Inputs;
                result=tasks.CreateTask(work.Actor,new(input.ProjectId,input.Title,input.Description,"NotStarted",null,input.StartAt,input.EndAt,input.Priority),pending.Key.ToString("D"),"automation:"+work.Id.ToString("N"));
            }
            catch(SqlException) { result=IdentityOperationResult<TaskRecord>.Failure("StorageUnavailable",503,"Task storage unavailable."); }
            catch(InvalidOperationException) { result=IdentityOperationResult<TaskRecord>.Failure("StorageUnavailable",503,"Task storage unavailable."); }
            if(result.Succeeded && result.Value is { } task)
            {
                using var complete=Command(c,tx,"UPDATE [automation].[StepRun] SET State='Succeeded',TaskId=@Task,Code='TaskCreated',EffectUncertain=0 WHERE OwnerId=@Owner AND RunId=@Run AND StepId=@Step AND State='Running';",work.Owner);
                Add(complete,"@Run",SqlDbType.UniqueIdentifier,work.Id); Add(complete,"@Step",SqlDbType.Int,pending.Id); Add(complete,"@Task",SqlDbType.UniqueIdentifier,task.Id); if(complete.ExecuteNonQuery()!=1)throw new InvalidOperationException();
                using(var remaining=Command(c,tx,"SELECT 1 FROM [automation].[StepRun] WHERE OwnerId=@Owner AND RunId=@Run AND State<>'Succeeded';",work.Owner))
                { Add(remaining,"@Run",SqlDbType.UniqueIdentifier,work.Id); if(remaining.ExecuteScalar() is null) Finish(c,tx,work.Owner,work.Id,"Succeeded","RunSucceeded",false); else Release(c,tx,work,0); }
                Audit(c,tx,work.Actor,"automation.step.dispatch",work.Id,null); tx.Commit(); return;
            }
            var transient=result.StatusCode>=500 || result.Code=="ConcurrencyConflict";
            if(transient && pending.Attempts<3)
            {
                using var retry=Command(c,tx,"UPDATE [automation].[StepRun] SET Code='TransientFailure',EffectUncertain=1 WHERE OwnerId=@Owner AND RunId=@Run AND StepId=@Step;",work.Owner);
                Add(retry,"@Run",SqlDbType.UniqueIdentifier,work.Id); Add(retry,"@Step",SqlDbType.Int,pending.Id); retry.ExecuteNonQuery();
                Release(c,tx,work,new[]{5,20,60}[pending.Attempts]); tx.Commit(); return;
            }
            var unknown=transient || result.StatusCode==404 || result.Code=="IdempotencyReplay";
            using(var failed=Command(c,tx,"UPDATE [automation].[StepRun] SET State='Failed',Code=@Code,EffectUncertain=@Unknown WHERE OwnerId=@Owner AND RunId=@Run AND StepId=@Step;",work.Owner))
            { Add(failed,"@Run",SqlDbType.UniqueIdentifier,work.Id); Add(failed,"@Step",SqlDbType.Int,pending.Id); Add(failed,"@Code",SqlDbType.VarChar,transient?"TaskStorageFailure":unknown?"TaskResultUnavailable":"TaskCommandRejected",64); Add(failed,"@Unknown",SqlDbType.Bit,unknown); failed.ExecuteNonQuery(); }
            FailWork(c,tx,work,transient?"TaskStorageFailure":unknown?"TaskResultUnavailable":"TaskCommandRejected",unknown); tx.Commit();
        }
    }
    private Work? Claim()
    {

        using var c=connections.Create(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        using(var gate=c.CreateCommand())
        { gate.Transaction=tx; gate.CommandText="DECLARE @Result int; EXEC @Result=sys.sp_getapplock @Resource='nexora-automation-local-worker',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0; SELECT @Result;"; if(Convert.ToInt32(gate.ExecuteScalar())<0)return null; }
        Guid owner,id,definition,user,session; long version; string role; DateTime auth;
        using(var cmd=c.CreateCommand())
        {
            cmd.Transaction=tx;
            cmd.CommandText="""
            SELECT TOP(1) OwnerId,Id,DefinitionId,VersionNumber,ActorUserId,SessionId,ActorRole,AuthenticatedAt
            FROM [automation].[Run] candidate WITH(UPDLOCK,HOLDLOCK)
            WHERE (candidate.State='Running' AND candidate.NextAttemptAt<=SYSUTCDATETIME() AND(candidate.LeaseUntil IS NULL OR candidate.LeaseUntil<SYSUTCDATETIME()))
              OR (candidate.State='Queued'
                  AND (SELECT COUNT_BIG(*) FROM [automation].[Run] WHERE State='Running') < @SystemLimit
                  AND (SELECT COUNT_BIG(*) FROM [automation].[Run] WHERE State='Running' AND OwnerId=candidate.OwnerId) < @OwnerLimit
                  AND NOT EXISTS(SELECT 1 FROM [automation].[Run] WHERE State='Running' AND OwnerId=candidate.OwnerId AND DefinitionId=candidate.DefinitionId))
            ORDER BY CASE WHEN candidate.State='Running' THEN 0 ELSE 1 END,candidate.CreatedAt,candidate.Id;
            """;
            Add(cmd,"@SystemLimit",SqlDbType.Int,ExecutionLimits.SystemConcurrency);
            Add(cmd,"@OwnerLimit",SqlDbType.Int,ExecutionLimits.OwnerConcurrency);
            using var r=cmd.ExecuteReader(); if(!r.Read())return null;
            owner=r.GetGuid(0);id=r.GetGuid(1);definition=r.GetGuid(2);version=r.GetInt64(3);user=r.GetGuid(4);session=r.GetGuid(5);role=r.GetString(6);auth=r.GetDateTime(7);
        }
        var token=Guid.NewGuid();
        using(var update=Command(c,tx,"UPDATE [automation].[Run] SET State='Running',StartedAt=COALESCE(StartedAt,SYSUTCDATETIME()),DeadlineAt=COALESCE(DeadlineAt,DATEADD(second,120,SYSUTCDATETIME())),LeaseToken=@Token,LeaseUntil=DATEADD(second,30,SYSUTCDATETIME()) WHERE OwnerId=@Owner AND Id=@Run AND State IN('Queued','Running');",owner))
        { Add(update,"@Run",SqlDbType.UniqueIdentifier,id);Add(update,"@Token",SqlDbType.UniqueIdentifier,token); if(update.ExecuteNonQuery()!=1)throw new InvalidOperationException(); }
        tx.Commit(); return new(owner,id,definition,version,new(user,owner,role,Utc(auth),session),token);
    }
    private static bool Fence(SqlConnection c,SqlTransaction tx,Work work)
    { using var cmd=Command(c,tx,"SELECT 1 FROM [automation].[Run] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND Id=@Run AND State='Running' AND LeaseToken=@Token;",work.Owner);Add(cmd,"@Run",SqlDbType.UniqueIdentifier,work.Id);Add(cmd,"@Token",SqlDbType.UniqueIdentifier,work.Token);return cmd.ExecuteScalar() is not null; }
    private bool Ready(SqlConnection c,SqlTransaction tx,Work work)
    {
        bool cancel,expired,old,unknown;
        using(var cmd=Command(c,tx,"SELECT CancelRequested,CASE WHEN DeadlineAt<=SYSUTCDATETIME() THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,CASE WHEN StartedAt>=DATEADD(minute,10,CreatedAt) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,CASE WHEN EXISTS(SELECT 1 FROM [automation].[StepRun] WHERE OwnerId=@Owner AND RunId=@Run AND EffectUncertain=1) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END FROM [automation].[Run] WHERE OwnerId=@Owner AND Id=@Run;",work.Owner))
        { Add(cmd,"@Run",SqlDbType.UniqueIdentifier,work.Id); using var r=cmd.ExecuteReader();if(!r.Read())return false;cancel=r.GetBoolean(0);expired=r.GetBoolean(1);old=r.GetBoolean(2);unknown=r.GetBoolean(3); }
        if(cancel || expired || old)
        { Finish(c,tx,work.Owner,work.Id,cancel?"Cancelled":"TimedOut",cancel?"CancellationRequested":"RunDeadlineExceeded",unknown);Audit(c,tx,work.Actor,"automation.step.dispatch",work.Id,null);return false; }
        var definition=Read(c,tx,work.Owner,work.Definition,false);
        if(!localExecutionEnabled || !RunAllowed(c,tx,work.Actor,"automation.run.start") || !ScheduledRunAllowed(c,tx,work) || definition is null || !definition.Enabled || definition.Lifecycle!="Active")
        { Finish(c,tx,work.Owner,work.Id,"Skipped","CurrentAuthorityUnavailable",unknown);Audit(c,tx,work.Actor,"automation.step.dispatch",work.Id,null);return false; }
        return true;
    }
    private static void Release(SqlConnection c,SqlTransaction tx,Work work,int delay)
    { using var cmd=Command(c,tx,"UPDATE [automation].[Run] SET LeaseToken=NULL,LeaseUntil=NULL,NextAttemptAt=DATEADD(second,@Delay,SYSUTCDATETIME()) WHERE OwnerId=@Owner AND Id=@Run AND State='Running' AND LeaseToken=@Token;",work.Owner);Add(cmd,"@Run",SqlDbType.UniqueIdentifier,work.Id);Add(cmd,"@Token",SqlDbType.UniqueIdentifier,work.Token);Add(cmd,"@Delay",SqlDbType.Int,delay);if(cmd.ExecuteNonQuery()!=1)throw new InvalidOperationException(); }
    private static void FailWork(SqlConnection c,SqlTransaction tx,Work work,string code,bool unknown)
    { using var cmd=Command(c,tx,"SELECT 1 FROM [automation].[StepRun] WHERE OwnerId=@Owner AND RunId=@Run AND State='Succeeded';",work.Owner);Add(cmd,"@Run",SqlDbType.UniqueIdentifier,work.Id);var partial=cmd.ExecuteScalar() is not null;Finish(c,tx,work.Owner,work.Id,partial?"PartiallySucceeded":"Failed",code,unknown);Audit(c,tx,work.Actor,"automation.step.dispatch",work.Id,null); }
}
