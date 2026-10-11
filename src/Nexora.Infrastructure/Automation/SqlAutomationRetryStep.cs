using System.Collections.Immutable;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    public IdentityOperationResult<AutomationRunAcknowledgement> RetryStep(IdentityPrincipal actor, Guid id, int stepId,
        string? etag, string key, string? trace) => Safe(() =>
    {
        if (stepId <= 0 || !CommandIdentity(key, etag, out var uuid))
            return Fail<AutomationRunAcknowledgement>("ValidationFailed", string.IsNullOrEmpty(etag) ? 428 : 422, "Supply a step, UUID and If-Match revision.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        bool Authority() => localExecutionEnabled && RunAllowed(c, tx, actor, "automation.run.retry_step") && RunAllowed(c, tx, actor, "automation.run.start");
        if (!LockOwner(c, tx, actor) || !Authority()) return Denied<AutomationRunAcknowledgement>();
        var run = ReadRun(c, tx, actor.OwnerId, id);
        if (run is null) return Missing<AutomationRunAcknowledgement>();
        var definition = Read(c, tx, actor.OwnerId, run.DefinitionId, false);
        if (definition is null) return Missing<AutomationRunAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, "automation.run.retry_step", uuid.ToString("D"),
            JsonSerializer.Serialize(new { owner = actor.OwnerId, id, stepId, etag }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationRunAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationRunAcknowledgement>(saved) ?? throw new InvalidOperationException();
            if (ack.Id != id || !Authority()) return Denied<AutomationRunAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(ack, 202);
        }
        if (!claim.IsClaimed) return Fail<AutomationRunAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (run.ETag != etag) return Fail<AutomationRunAcknowledgement>("RevisionConflict", 412, "Compare current run revision.");
        using (var age = Command(c, tx, "SELECT 1 FROM [automation].[Run] WHERE OwnerId=@Owner AND Id=@Run AND CreatedAt<=DATEADD(minute,-10,SYSUTCDATETIME());", actor.OwnerId))
        {
            Add(age, "@Run", SqlDbType.UniqueIdentifier, id);
            if (age.ExecuteScalar() is not null) return Fail<AutomationRunAcknowledgement>("RetryWindowExpired", 409, "The existing run queue-age budget expired; this run cannot be requeued.");
        }
        if (run.State is not ("Failed" or "PartiallySucceeded") || run.EffectUncertain || !definition.Enabled || definition.Lifecycle != "Active")
            return Fail<AutomationRunAcknowledgement>("RetryUnsafe", 409, "Only a known no-effect failure in an enabled definition can be retried.");
        int position;
        using (var step = Command(c, tx, "SELECT Position FROM [automation].[StepRun] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND RunId=@Run AND StepId=@Step AND State='Failed' AND EffectUncertain=0 AND TaskId IS NULL AND Attempts BETWEEN 1 AND 3;", actor.OwnerId))
        {
            Add(step, "@Run", SqlDbType.UniqueIdentifier, id); Add(step, "@Step", SqlDbType.Int, stepId);
            if (step.ExecuteScalar() is not int found) return Fail<AutomationRunAcknowledgement>("RetryUnsafe", 409, "The step is uncertain, successful or its attempt budget is exhausted.");
            position = found;
        }
        using (var guards = Command(c, tx, "SELECT 1 WHERE EXISTS(SELECT 1 FROM [automation].[Run] WHERE OwnerId=@Owner AND DefinitionId=@Definition AND Id<>@Run AND State IN('Queued','Running')) OR EXISTS(SELECT 1 FROM [automation].[StepRun] WHERE OwnerId=@Owner AND RunId=@Run AND ((Position<@Position AND State<>'Succeeded') OR (Position>@Position AND (State<>'Skipped' OR Attempts<>0 OR EffectUncertain=1 OR TaskId IS NOT NULL))));", actor.OwnerId))
        {
            Add(guards, "@Run", SqlDbType.UniqueIdentifier, id); Add(guards, "@Definition", SqlDbType.UniqueIdentifier, run.DefinitionId); Add(guards, "@Position", SqlDbType.Int, position);
            if (guards.ExecuteScalar() is not null) return Fail<AutomationRunAcknowledgement>("RetryUnsafe", 409, "Resolve busy runs or step outcomes first.");
        }
        var version = Versions(c, tx, actor.OwnerId, run.DefinitionId, run.VersionNumber + 1, 1).SingleOrDefault();
        var parsed = AutomationDefinitionContract.Parse(version?.DefinitionJson);
        if (version?.VersionNumber != run.VersionNumber || !parsed.Accepted || !TrustedExecution(parsed.Definition!) ||
            position > parsed.Definition!.Steps.Length || parsed.Definition.Steps[position - 1].Id != stepId ||
            CheckTargets(c, tx, actor, new(1, parsed.Definition.Trigger, parsed.Definition.Steps.Skip(position - 1).ToImmutableArray())) is not null)
            return Fail<AutomationRunAcknowledgement>("TargetUnavailable", 422, "Validate the pinned version and current target authority.");
        using (var update = Command(c, tx, "UPDATE [automation].[StepRun] SET State='Queued',Code=NULL WHERE OwnerId=@Owner AND RunId=@Run AND Position>=@Position; UPDATE [automation].[Run] SET State='Queued',Code=NULL,FinishedAt=NULL,StartedAt=NULL,DeadlineAt=NULL,NextAttemptAt=SYSUTCDATETIME(),CancelRequested=0,LeaseToken=NULL,LeaseUntil=NULL,ActorUserId=@User,SessionId=@Session,ActorRole=@Role,AuthenticatedAt=@Auth WHERE OwnerId=@Owner AND Id=@Run;", actor.OwnerId))
        {
            Add(update, "@Run", SqlDbType.UniqueIdentifier, id); Add(update, "@Position", SqlDbType.Int, position);
            Add(update, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(update, "@Session", SqlDbType.UniqueIdentifier, actor.SessionId);
            Add(update, "@Role", SqlDbType.VarChar, actor.Role, 32); Add(update, "@Auth", SqlDbType.DateTime2, actor.RecentAuthenticatedAt.UtcDateTime);
            update.ExecuteNonQuery();
        }
        // Existing successes, immutable version, effect keys and cumulative attempts remain intact.
        var result = new AutomationRunAcknowledgement(id); Audit(c, tx, actor, "automation.run.retry_step", id, trace);
        receipts.Complete(c, tx, claim, "StepRetryQueued", 202, JsonSerializer.Serialize(result));
        if (!Authority()) return Denied<AutomationRunAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(result, 202);
    });
}
