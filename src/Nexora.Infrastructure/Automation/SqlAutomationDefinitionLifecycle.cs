using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    // Validate against the same authority/owner transaction; never expose target metadata.
    private string? CheckTargets(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, AutomationDefinitionShape shape)
    {
        if (!capabilities.IsAllowed(c, tx, actor, "FX11", "projects.project.read") ||
            !capabilities.IsAllowed(c, tx, actor, "FX12", "tasks.task.create")) return "TargetUnavailable";
        foreach (var step in shape.Steps)
        {
            using var cmd = Command(c, tx, "SELECT CASE WHEN (StartAt IS NULL OR StartAt<=@Start) AND (EndAt IS NULL OR EndAt>=@End) THEN 1 ELSE 0 END FROM [productivity].[Project] WITH(HOLDLOCK) WHERE OwnerId=@Owner AND Id=@Id AND Status NOT IN('Completed','Skipped','Deleted');", actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, step.Inputs.ProjectId);
            Add(cmd, "@Start", SqlDbType.DateTime2, step.Inputs.StartAt.UtcDateTime);
            Add(cmd, "@End", SqlDbType.DateTime2, step.Inputs.EndAt.UtcDateTime);
            if (cmd.ExecuteScalar() is not int valid || valid != 1) return "TargetUnavailable";
        }
        return null;
    }
    public IdentityOperationResult<AutomationPortableDefinition> Export(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "automation.definition.export")) return Denied<AutomationPortableDefinition>();
        var item = Read(c, tx, actor.OwnerId, id, false);
        if (item is null || item.Lifecycle != "Active") return Missing<AutomationPortableDefinition>();
        var version = Versions(c, tx, actor.OwnerId, id, item.CurrentVersion + 1, 1).SingleOrDefault();
        if (version is null || version.VersionNumber != item.CurrentVersion || !AutomationDefinitionContract.Parse(version.DefinitionJson).Accepted) return Missing<AutomationPortableDefinition>();
        Audit(c, tx, actor, "automation.definition.export", id, null);
        if (!Allowed(c, tx, actor, "automation.definition.export")) return Denied<AutomationPortableDefinition>();
        tx.Commit(); return IdentityOperationResult<AutomationPortableDefinition>.Success(new(1, version.Title, version.DefinitionJson));
    });
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> Import(IdentityPrincipal actor, AutomationPortableDefinition body, string key, string? trace)
    {
        if (body.SchemaVersion != 1) return Fail<AutomationDefinitionAcknowledgement>("DefinitionSchemaInvalid", 422, "Unsupported portable definition version.");
        return Mutate(actor, null, null, new(body.Title, body.DefinitionJson), key, trace, "automation.definition.import");
    }
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> Trash(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace) => Lifecycle(actor, id, null, etag, key, trace);
    public IdentityOperationResult<AutomationDefinitionAcknowledgement> Restore(IdentityPrincipal actor, Guid id, Guid deletionBatchId, string? etag, string key, string? trace) => Lifecycle(actor, id, deletionBatchId, etag, key, trace);
    private IdentityOperationResult<AutomationDefinitionAcknowledgement> Lifecycle(IdentityPrincipal actor, Guid id, Guid? batch, string? etag, string key, string? trace) => Safe(() =>
    {
        var action = batch is null ? "automation.definition.trash" : "automation.definition.restore";
        if (!Guid.TryParse(key, out var uuid) || uuid == Guid.Empty) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyKeyRequired", 422, "Supply a nonempty UUID.");
        if (string.IsNullOrWhiteSpace(etag)) return Fail<AutomationDefinitionAcknowledgement>("PreconditionRequired", 428, "If-Match is required.");
        if (!ValidETag(etag) || id == Guid.Empty || batch == Guid.Empty) return Fail<AutomationDefinitionAcknowledgement>("ValidationFailed", 422, "Supply a valid revision and deletion cohort.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User AND State='Active';", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<AutomationDefinitionAcknowledgement>(); }
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        var item = Read(c, tx, actor.OwnerId, id, true); if (item is null) return Missing<AutomationDefinitionAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, batch }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyConflict", 409, "The key belongs to a different command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationDefinitionAcknowledgement>(saved) ?? throw new InvalidOperationException("Receipt integrity.");
            if (ack.Id != id) throw new InvalidOperationException("Receipt identity.");
            if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(ack);
        }
        if (!claim.IsClaimed) return Fail<AutomationDefinitionAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (item.ETag != etag) return Fail<AutomationDefinitionAcknowledgement>("RevisionConflict", 412, "Compare the current revision.");
        var restore = batch is not null;
        if (item.Lifecycle != (restore ? "Trash" : "Active") || item.Enabled || (restore ? item.DeletionBatchId != batch : item.DeletionBatchId is not null))
            return Fail<AutomationDefinitionAcknowledgement>("LifecycleConflict", 409, "The current lifecycle or deletion cohort changed.");
        using (var cohort = Command(c, tx, "SELECT TOP(2) DeletionBatchId FROM [platform].[TrashItem] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id AND RestoredAt IS NULL AND PurgedAt IS NULL;", actor.OwnerId))
        {
            Add(cohort, "@Id", SqlDbType.UniqueIdentifier, id); using var reader = cohort.ExecuteReader();
            if (restore) { if (!reader.Read() || reader.GetGuid(0) != batch || reader.Read()) return Fail<AutomationDefinitionAcknowledgement>("LifecycleConflict", 409, "The deletion cohort changed."); }
            else if (reader.Read()) return Fail<AutomationDefinitionAcknowledgement>("LifecycleConflict", 409, "An unresolved deletion cohort exists.");
        }
        var cohortId = batch ?? Guid.NewGuid();
        using (var trash = Command(c, tx, restore
            ? "UPDATE [platform].[TrashItem] SET RestoredAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id AND DeletionBatchId=@Batch AND RestoredAt IS NULL AND PurgedAt IS NULL;"
            : "INSERT [platform].[TrashItem](OwnerId,ResourceType,ResourceId,DeletionBatchId,PriorStatus) VALUES(@Owner,'AutomationDefinition',@Id,@Batch,@State);", actor.OwnerId))
        { Add(trash, "@Id", SqlDbType.UniqueIdentifier, id); Add(trash, "@Batch", SqlDbType.UniqueIdentifier, cohortId); Add(trash, "@State", SqlDbType.VarChar, item.State, 32); if (trash.ExecuteNonQuery() != 1) throw new InvalidOperationException("Cohort integrity."); }
        using (var update = Command(c, tx, "UPDATE [automation].[Definition] SET State='Disabled',Enabled=0,DeletionBatchId=@Batch,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id; UPDATE [platform].[Resource] SET Availability=@Lifecycle,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability=@Prior AND PurgedAt IS NULL;", actor.OwnerId))
        {
            Add(update, "@Id", SqlDbType.UniqueIdentifier, id); Add(update, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(update, "@Batch", SqlDbType.UniqueIdentifier, restore ? null : cohortId);
            Add(update, "@Lifecycle", SqlDbType.VarChar, restore ? "Active" : "Trash", 16); Add(update, "@Prior", SqlDbType.VarChar, item.Lifecycle, 16);
            if (update.ExecuteNonQuery() != 2) throw new InvalidOperationException("Lifecycle integrity.");
        }
        var updated = Read(c, tx, actor.OwnerId, id, false) ?? throw new InvalidOperationException("Definition integrity.");
        var result = new AutomationDefinitionAcknowledgement(id, updated.CurrentVersion, updated.ETag);
        Audit(c, tx, actor, action, id, trace);
        receipts.Complete(c, tx, claim, restore ? "DefinitionRestored" : "DefinitionTrashed", 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationDefinitionAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(result);
    });
    private static void Audit(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action, Guid id, string? trace)
    {
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'automation.Definition',@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, id); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
    }
}
