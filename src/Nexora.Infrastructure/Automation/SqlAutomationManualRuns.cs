using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;
using Nexora.Infrastructure.Notifications;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService : IAutomationRunService
{
    private const string RunColumns = "Id,DefinitionId,VersionNumber,State,CreatedAt,StartedAt,FinishedAt,Code,EffectUncertain,RowVersion";
    private bool RunAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        Allowed(c, tx, actor, action) && Allowed(c, tx, actor, "automation.run.read");
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> SetEnabled(IdentityPrincipal actor, Guid id, bool enabled, string? etag, string key, string? trace) => Safe(() =>
    {
        var action = enabled ? "automation.definition.enable" : "automation.definition.disable";
        if (!CommandIdentity(key, etag, out var uuid)) return Fail<AutomationDefinitionAcknowledgement>("ValidationFailed", etag is null or "" ? 428 : 422, "Supply a UUID and If-Match revision.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!LockOwner(c, tx, actor) || !Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        var item = Read(c, tx, actor.OwnerId, id, true); if (item is null) return Missing<AutomationDefinitionAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, enabled }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        { var ack = JsonSerializer.Deserialize<AutomationDefinitionAcknowledgement>(saved) ?? throw new InvalidOperationException(); if (ack.Id != id) throw new InvalidOperationException(); tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(ack); }
        if (!claim.IsClaimed) return Fail<AutomationDefinitionAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (item.ETag != etag) return Fail<AutomationDefinitionAcknowledgement>("RevisionConflict", 412, "Compare current revision.");
        if (item.Lifecycle != "Active") return Fail<AutomationDefinitionAcknowledgement>("LifecycleConflict", 409, "Restore before configuring execution.");
        if (enabled)
        {
            var version = Versions(c, tx, actor.OwnerId, id, item.CurrentVersion + 1, 1).Single();
            var shape = AutomationDefinitionContract.Parse(version.DefinitionJson);
            if (!shape.Accepted || !TrustedExecution(shape.Definition!) || CheckTargets(c, tx, actor, shape.Definition!) is not null) return Fail<AutomationDefinitionAcknowledgement>("TargetUnavailable", 422, "Validate the current targets before enabling.");
            if (!BindScheduleAuthority(c, tx, actor, item)) return Fail<AutomationDefinitionAcknowledgement>("ScheduleUnavailable", 409, "Validate schedule binding and current execution authority before enabling.");
        }
        using (var update = Command(c, tx, "UPDATE [automation].[Definition] SET Enabled=@Enabled,State=@State,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id; UPDATE [platform].[Resource] SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Active';", actor.OwnerId))
        { Add(update, "@Id", SqlDbType.UniqueIdentifier, id); Add(update, "@Enabled", SqlDbType.Bit, enabled); Add(update, "@State", SqlDbType.VarChar, enabled ? "Enabled" : "Disabled", 64); Add(update, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (update.ExecuteNonQuery() != 2) throw new InvalidOperationException(); }
        if (!enabled)
        {
            SkipQueued(c, tx, actor.OwnerId, id, "DefinitionDisabled");
            using var active = Command(c, tx, "SELECT Id FROM [automation].[Run] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND DefinitionId=@Id AND State='Running';", actor.OwnerId);
            Add(active,"@Id",SqlDbType.UniqueIdentifier,id); var ids=new List<Guid>(); using(var r=active.ExecuteReader())while(r.Read())ids.Add(r.GetGuid(0));
            foreach(var runId in ids) Finish(c,tx,actor.OwnerId,runId,"Skipped","DefinitionDisabled",EffectUnknown(c,tx,actor.OwnerId,runId));
        }
        var current = Read(c, tx, actor.OwnerId, id, false)!; var result = new AutomationDefinitionAcknowledgement(id, current.CurrentVersion, current.ETag);
        Audit(c, tx, actor, action, id, trace); receipts.Complete(c, tx, claim, "DefinitionConfigured", 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(result);
    });
    public IdentityOperationResult<AutomationRunAcknowledgement> Start(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace) => Safe(() =>
    {
        if (!CommandIdentity(key, etag, out var uuid) || actor.SessionId is null) return Fail<AutomationRunAcknowledgement>("ValidationFailed", etag is null or "" ? 428 : 422, "Supply a UUID and If-Match revision.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!LockOwner(c, tx, actor) || !RunAllowed(c, tx, actor, "automation.run.start")) return Denied<AutomationRunAcknowledgement>();
        var item = Read(c, tx, actor.OwnerId, id, true); if (item is null) return Missing<AutomationRunAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, "automation.run.start", uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationRunAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationRunAcknowledgement>(saved) ?? throw new InvalidOperationException();
            using var exists = Command(c, tx, "SELECT 1 FROM [automation].[Run] WHERE OwnerId=@Owner AND Id=@Run AND DefinitionId=@Id;", actor.OwnerId);
            Add(exists, "@Run", SqlDbType.UniqueIdentifier, ack.Id); Add(exists, "@Id", SqlDbType.UniqueIdentifier, id); if (exists.ExecuteScalar() is null) return Missing<AutomationRunAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(ack, 202);
        }
        if (!claim.IsClaimed) return Fail<AutomationRunAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (item.ETag != etag) return Fail<AutomationRunAcknowledgement>("RevisionConflict", 412, "Compare current revision.");
        if (item.Lifecycle != "Active" || !item.Enabled || item.State != "Enabled") return Fail<AutomationRunAcknowledgement>("DefinitionDisabled", 409, "Enable the current definition first.");
        var version = Versions(c, tx, actor.OwnerId, id, item.CurrentVersion + 1, 1).Single(); var shape = AutomationDefinitionContract.Parse(version.DefinitionJson);
        if (!shape.Accepted || !TrustedExecution(shape.Definition!) || CheckTargets(c, tx, actor, shape.Definition!) is not null) return Fail<AutomationRunAcknowledgement>("TargetUnavailable", 422, "Validate current target authority.");
        SkipQueued(c, tx, actor.OwnerId, id, "SupersededQueuedRun");
        var runId = Guid.NewGuid();
        using (var insert = Command(c, tx, "INSERT [automation].[Run](Id,OwnerId,DefinitionId,VersionNumber,ActorUserId,SessionId,ActorRole,AuthenticatedAt) VALUES(@Run,@Owner,@Id,@Version,@User,@Session,@Role,@Auth);", actor.OwnerId))
        { Add(insert, "@Run", SqlDbType.UniqueIdentifier, runId); Add(insert, "@Id", SqlDbType.UniqueIdentifier, id); Add(insert, "@Version", SqlDbType.BigInt, item.CurrentVersion); Add(insert, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(insert, "@Session", SqlDbType.UniqueIdentifier, actor.SessionId); Add(insert, "@Role", SqlDbType.VarChar, actor.Role, 32); Add(insert, "@Auth", SqlDbType.DateTime2, actor.RecentAuthenticatedAt.UtcDateTime); insert.ExecuteNonQuery(); }
        for (var i = 0; i < shape.Definition!.Steps.Length; i++)
        {
            using var step = Command(c, tx, "INSERT [automation].[StepRun](OwnerId,RunId,StepId,Position,CommandKey) VALUES(@Owner,@Run,@Step,@Position,@Key);", actor.OwnerId);
            Add(step, "@Run", SqlDbType.UniqueIdentifier, runId); Add(step, "@Step", SqlDbType.Int, shape.Definition.Steps[i].Id); Add(step, "@Position", SqlDbType.Int, i + 1); Add(step, "@Key", SqlDbType.UniqueIdentifier, Guid.NewGuid()); step.ExecuteNonQuery();
        }
        Audit(c, tx, actor, "automation.run.start", runId, trace); var result = new AutomationRunAcknowledgement(runId);
        receipts.Complete(c, tx, claim, "RunQueued", 202, JsonSerializer.Serialize(result));
        if (!RunAllowed(c, tx, actor, "automation.run.start")) return Denied<AutomationRunAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(result, 202);
    });
    public IdentityOperationResult<AutomationRunPage> Runs(IdentityPrincipal actor, Guid id, string? cursor, string? state,
        long? version = null, string? createdFrom = null, string? createdBefore = null) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!RunAllowed(c, tx, actor, "automation.run.read")) return Denied<AutomationRunPage>();
        var definition = Read(c, tx, actor.OwnerId, id, false);
        if (definition is null) return Missing<AutomationRunPage>();
        if (!string.IsNullOrEmpty(state) && !RunStates.Contains(state, StringComparer.Ordinal)) return Fail<AutomationRunPage>("ValidationFailed", 422, "Check the run state filter.");
        if (version is < 1 || version > definition.CurrentVersion ||
            !TryRunFilterInstant(createdFrom, out var from) || !TryRunFilterInstant(createdBefore, out var before) ||
            (from is not null && before is not null && before <= from))
            return Fail<AutomationRunPage>("ValidationFailed", 422, "Use an existing definition version and explicit-offset created-time bounds, from inclusive and before exclusive.");
        var identity = JsonSerializer.Serialize(new { id, state = state ?? "", version, from, before }); DateTime at = default; Guid boundary = default;
        if (cursor is not null && !cursors.TryDecode(cursor, actor.OwnerId, identity, out at, out boundary)) return Missing<AutomationRunPage>();
        const string filters = "OwnerId=@Owner AND DefinitionId=@Id AND(@State='' OR State=@State) AND(@Version IS NULL OR VersionNumber=@Version) AND(@From IS NULL OR CreatedAt>=@From) AND(@Before IS NULL OR CreatedAt<@Before)";
        using var cmd = Command(c, tx, $"SELECT TOP(26) {RunColumns} FROM [automation].[Run] WHERE {filters} AND(@Boundary IS NULL OR CreatedAt<@At OR(CreatedAt=@At AND Id<@Boundary)) ORDER BY CreatedAt DESC,Id DESC;", actor.OwnerId);
        void BindFilters(SqlCommand command)
        {
            Add(command, "@Id", SqlDbType.UniqueIdentifier, id); Add(command, "@State", SqlDbType.VarChar, state ?? "", 32);
            Add(command, "@Version", SqlDbType.BigInt, version); Add(command, "@From", SqlDbType.DateTime2, from); Add(command, "@Before", SqlDbType.DateTime2, before);
            Add(command, "@Boundary", SqlDbType.UniqueIdentifier, cursor is null ? null : boundary); Add(command, "@At", SqlDbType.DateTime2, cursor is null ? null : at);
        }
        BindFilters(cmd);
        if (cursor is not null)
        { using var anchor = Command(c, tx, $"SELECT 1 FROM [automation].[Run] WHERE {filters} AND Id=@Boundary AND CreatedAt=@At;", actor.OwnerId); BindFilters(anchor); if(anchor.ExecuteScalar() is null) return Missing<AutomationRunPage>(); }
        var items = RunRows(cmd); var next = items.Count > 25 ? cursors.Encode(actor.OwnerId, identity, items[24].CreatedAt.UtcDateTime, items[24].Id) : null;
        if (!RunAllowed(c, tx, actor, "automation.run.read")) return Denied<AutomationRunPage>();
        tx.Commit(); return IdentityOperationResult<AutomationRunPage>.Success(new(items.Take(25).ToArray(), next,
            new(localExecutionEnabled, ExecutionLimits.SystemConcurrency, ExecutionLimits.OwnerConcurrency, 1, 120, 10)));
    });
    private static bool TryRunFilterInstant(string? value, out DateTime? utc)
    {
        utc = null; if (value is null or "") return true;
        if (value.Length > 40 || !System.Text.RegularExpressions.Regex.IsMatch(value,
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$", System.Text.RegularExpressions.RegexOptions.CultureInvariant) ||
            !DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var instant) ||
            instant.UtcDateTime.Year is < 2 or > 9997) return false;
        utc = instant.UtcDateTime; return true;
    }
    public IdentityOperationResult<AutomationRunDetail> Run(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!RunAllowed(c, tx, actor, "automation.run.read")) return Denied<AutomationRunDetail>();
        var run = ReadRun(c, tx, actor.OwnerId, id); if (run is null || Read(c, tx, actor.OwnerId, run.DefinitionId, false) is null) return Missing<AutomationRunDetail>();
        var taskRead = capabilities.IsAllowed(c, tx, actor, "FX12", "tasks.task.read");
        using var cmd = Command(c, tx, "SELECT s.StepId,s.Position,s.State,s.Attempts,CASE WHEN @TaskRead=1 AND EXISTS(SELECT 1 FROM [productivity].[Task] t WHERE t.OwnerId=s.OwnerId AND t.Id=s.TaskId AND t.Status<>'Deleted') THEN s.TaskId END,s.Code,s.EffectUncertain FROM [automation].[StepRun] s WHERE s.OwnerId=@Owner AND s.RunId=@Run ORDER BY s.Position;", actor.OwnerId);
        Add(cmd, "@TaskRead", SqlDbType.Bit, taskRead); Add(cmd, "@Run", SqlDbType.UniqueIdentifier, id); var steps = new List<AutomationStepRunRecord>();
        using (var r = cmd.ExecuteReader()) while (r.Read()) steps.Add(new(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3), r.IsDBNull(4) ? null : r.GetGuid(4), r.IsDBNull(5) ? null : r.GetString(5), r.GetBoolean(6)));
        var notice = capabilities.IsAllowed(c, tx, actor, "FX06", "notifications.inbox.read")
            ? LocalModuleNotificationWriter.ReadAutomationFailure(c, tx, actor.OwnerId, id) : null;
        tx.Commit(); return IdentityOperationResult<AutomationRunDetail>.Success(new(run, steps, notice));
    });
    public IdentityOperationResult<AutomationRunAcknowledgement> Cancel(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace) => Safe(() =>
    {
        if (!CommandIdentity(key, etag, out var uuid)) return Fail<AutomationRunAcknowledgement>("ValidationFailed", etag is null or "" ? 428 : 422, "Supply a UUID and If-Match revision.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!LockOwner(c, tx, actor) || !RunAllowed(c, tx, actor, "automation.run.cancel")) return Denied<AutomationRunAcknowledgement>();
        var run = ReadRun(c, tx, actor.OwnerId, id); if (run is null || Read(c, tx, actor.OwnerId, run.DefinitionId, false) is null) return Missing<AutomationRunAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, "automation.run.cancel", uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationRunAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved) { var ack = JsonSerializer.Deserialize<AutomationRunAcknowledgement>(saved) ?? throw new InvalidOperationException(); if(ack.Id!=id)throw new InvalidOperationException(); tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(ack); }
        if (!claim.IsClaimed) return Fail<AutomationRunAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (run.ETag != etag) return Fail<AutomationRunAcknowledgement>("RevisionConflict", 412, "Compare current run revision.");
        if (run.State is not ("Queued" or "Running")) return Fail<AutomationRunAcknowledgement>("RunTerminal", 409, "The run is already terminal.");
        using var update = Command(c, tx, "UPDATE [automation].[Run] SET CancelRequested=1 WHERE OwnerId=@Owner AND Id=@Run AND State IN('Queued','Running');", actor.OwnerId);
        Add(update,"@Run",SqlDbType.UniqueIdentifier,id); if(update.ExecuteNonQuery()!=1)throw new InvalidOperationException();
        Finish(c,tx,actor.OwnerId,id,"Cancelled","CancellationRequested",EffectUnknown(c,tx,actor.OwnerId,id));
        var result=new AutomationRunAcknowledgement(id); Audit(c,tx,actor,"automation.run.cancel",id,trace); receipts.Complete(c,tx,claim,"CancellationRequested",200,JsonSerializer.Serialize(result));
        tx.Commit(); return IdentityOperationResult<AutomationRunAcknowledgement>.Success(result);
    });
    private static bool TrustedExecution(AutomationDefinitionShape shape) =>
        shape.Trigger is { Key: "manual", Version: 1 } && AutomationContractRegistry.FindTrigger("manual",1) is { ExecutionAvailable: true, ExternalEffects: false } &&
        shape.Steps.All(step => step.Action is { Key: "tasks.task.create", Version: 1 } && AutomationContractRegistry.FindAction(step.Action.Key,step.Action.Version) is { ExecutionAvailable: true, ExternalEffects: false });
    private static readonly string[] RunStates = ["Queued","Running","Succeeded","PartiallySucceeded","Failed","Cancelled","TimedOut","Skipped"];
    private static bool CommandIdentity(string key, string? etag, out Guid uuid) => Guid.TryParse(key,out uuid) && uuid!=Guid.Empty && ValidETag(etag);
    private static bool LockOwner(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor)
    { using var cmd=Command(c,tx,"SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';",actor.OwnerId); Add(cmd,"@User",SqlDbType.UniqueIdentifier,actor.UserId); return cmd.ExecuteScalar() is not null; }
    private static AutomationRunRecord? ReadRun(SqlConnection c,SqlTransaction tx,Guid owner,Guid id)
    { using var cmd=Command(c,tx,$"SELECT {RunColumns} FROM [automation].[Run] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND Id=@Run;",owner); Add(cmd,"@Run",SqlDbType.UniqueIdentifier,id); return RunRows(cmd).SingleOrDefault(); }
    private static List<AutomationRunRecord> RunRows(SqlCommand cmd)
    { using var r=cmd.ExecuteReader(); var items=new List<AutomationRunRecord>(); while(r.Read())items.Add(new(r.GetGuid(0),r.GetGuid(1),r.GetInt64(2),r.GetString(3),Utc(r.GetDateTime(4)),r.IsDBNull(5)?null:Utc(r.GetDateTime(5)),r.IsDBNull(6)?null:Utc(r.GetDateTime(6)),r.IsDBNull(7)?null:r.GetString(7),r.GetBoolean(8),'"'+Convert.ToBase64String(r.GetFieldValue<byte[]>(9))+'"')); return items; }
    private static void SkipQueued(SqlConnection c,SqlTransaction tx,Guid owner,Guid definition,string code)
    {
        using var cmd=Command(c,tx,"UPDATE s SET State='Skipped',Code=@Code FROM [automation].[StepRun] s JOIN [automation].[Run] r ON r.OwnerId=s.OwnerId AND r.Id=s.RunId WHERE r.OwnerId=@Owner AND r.DefinitionId=@Id AND r.State='Queued'; UPDATE [automation].[Run] SET State='Skipped',Code=@Code,FinishedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND DefinitionId=@Id AND State='Queued';",owner);
        Add(cmd,"@Id",SqlDbType.UniqueIdentifier,definition); Add(cmd,"@Code",SqlDbType.VarChar,code,64); cmd.ExecuteNonQuery();
    }
    private static bool EffectUnknown(SqlConnection c,SqlTransaction tx,Guid owner,Guid run)
    { using var cmd=Command(c,tx,"SELECT 1 FROM [automation].[StepRun] WHERE OwnerId=@Owner AND RunId=@Run AND EffectUncertain=1;",owner);Add(cmd,"@Run",SqlDbType.UniqueIdentifier,run);return cmd.ExecuteScalar() is not null; }
    private static void Finish(SqlConnection c,SqlTransaction tx,Guid owner,Guid run,string state,string code,bool uncertain)
    {
        using var cmd=Command(c,tx,"DECLARE @Changed bit=0; UPDATE [automation].[Run] SET State=@State,Code=@Code,EffectUncertain=@Unknown,FinishedAt=SYSUTCDATETIME(),LeaseToken=NULL,LeaseUntil=NULL WHERE OwnerId=@Owner AND Id=@Run AND State IN('Queued','Running'); IF @@ROWCOUNT=1 BEGIN SET @Changed=1; UPDATE [automation].[StepRun] SET State=CASE WHEN @State IN('Cancelled','TimedOut') THEN @State ELSE 'Skipped' END,Code=@Code,EffectUncertain=CASE WHEN State='Running' THEN @Unknown ELSE 0 END WHERE OwnerId=@Owner AND RunId=@Run AND State IN('Queued','Running'); END; SELECT @Changed;",owner);
        Add(cmd,"@Run",SqlDbType.UniqueIdentifier,run); Add(cmd,"@State",SqlDbType.VarChar,state,32); Add(cmd,"@Code",SqlDbType.VarChar,code,64); Add(cmd,"@Unknown",SqlDbType.Bit,uncertain);
        var changed=cmd.ExecuteScalar() is bool value && value;
        if(changed && state is "Failed" or "PartiallySucceeded" or "TimedOut") LocalModuleNotificationWriter.AutomationFailed(c,tx,owner,run);
    }
}
