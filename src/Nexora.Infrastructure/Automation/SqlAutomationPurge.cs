using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    public IdentityOperationResult<AutomationPurgePreview> PreviewPurge(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!LockOwner(c, tx, actor) || !Allowed(c, tx, actor, "automation.definition.purge")) return Denied<AutomationPurgePreview>();
        var definition = Read(c, tx, actor.OwnerId, id, true);
        if (definition is null) return Missing<AutomationPurgePreview>();
        if (definition.Lifecycle != "Trash" || definition.Enabled || definition.DeletionBatchId is null)
            return Fail<AutomationPurgePreview>("LifecycleConflict", 409, "Only a disabled definition in its current Trash cohort can be purged.");
        var preview = PurgePlan(c, tx, actor.OwnerId, definition);
        if (!Allowed(c, tx, actor, "automation.definition.purge")) return Denied<AutomationPurgePreview>();
        tx.Commit(); return IdentityOperationResult<AutomationPurgePreview>.Success(preview);
    });

    public IdentityOperationResult<AutomationPurgeAcknowledgement> Purge(IdentityPrincipal actor, Guid id, string? etag, AutomationPurgeCommand body, string key, string? trace) => Safe(() =>
    {
        if (string.IsNullOrEmpty(etag)) return Fail<AutomationPurgeAcknowledgement>("PreconditionRequired", 428, "If-Match is required.");
        if (!ValidETag(etag) || !Guid.TryParse(key, out var uuid) || uuid == Guid.Empty || id == Guid.Empty || body.DeletionBatchId == Guid.Empty || !body.ConfirmPermanentDeletion)
            return Fail<AutomationPurgeAcknowledgement>("ValidationFailed", 422, "Confirm permanent deletion using the preview revision, cohort and a UUID key.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        const string action = "automation.definition.purge";
        if (!LockOwner(c, tx, actor) || !Allowed(c, tx, actor, action)) return Denied<AutomationPurgeAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, uuid.ToString("D"), JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, body }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationPurgeAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationPurgeAcknowledgement>(saved) ?? throw new InvalidOperationException();
            if (ack.Id != id || ack.DeletionBatchId != body.DeletionBatchId || !ack.Purged) throw new InvalidOperationException();
            using var tombstone = Command(c, tx, "SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=@Owner AND r.Id=@Id AND r.Availability='Purged' AND r.PurgedAt IS NOT NULL AND rt.Code='AutomationDefinition' AND m.Code='FX34' AND EXISTS(SELECT 1 FROM [platform].[TrashItem] t WHERE t.OwnerId=r.OwnerId AND t.ResourceId=r.Id AND t.ResourceType='AutomationDefinition' AND t.DeletionBatchId=@Batch AND t.PurgedAt IS NOT NULL);", actor.OwnerId);
            Add(tombstone, "@Id", SqlDbType.UniqueIdentifier, id); Add(tombstone, "@Batch", SqlDbType.UniqueIdentifier, body.DeletionBatchId);
            if (tombstone.ExecuteScalar() is null) return Missing<AutomationPurgeAcknowledgement>();
            if (!Allowed(c, tx, actor, action)) return Denied<AutomationPurgeAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationPurgeAcknowledgement>.Success(ack);
        }
        if (!claim.IsClaimed) return Fail<AutomationPurgeAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        var definition = Read(c, tx, actor.OwnerId, id, true);
        if (definition is null) return Missing<AutomationPurgeAcknowledgement>();
        if (definition.ETag != etag) return Fail<AutomationPurgeAcknowledgement>("RevisionConflict", 412, "Reload the purge preview.");
        if (definition.Lifecycle != "Trash" || definition.Enabled || definition.DeletionBatchId != body.DeletionBatchId)
            return Fail<AutomationPurgeAcknowledgement>("LifecycleConflict", 409, "The Trash cohort or lifecycle changed.");
        var plan = PurgePlan(c, tx, actor.OwnerId, definition);
        if (plan.Blockers.Count != 0) return Fail<AutomationPurgeAcknowledgement>("PurgePinned", 409, "Retained history or resource references prevent permanent deletion.");
        using (var remove = Command(c, tx, "DELETE [automation].[Schedule] WHERE OwnerId=@Owner AND DefinitionId=@Id; DELETE [automation].[ScheduleVersion] WHERE OwnerId=@Owner AND DefinitionId=@Id; UPDATE [automation].[Definition] SET CurrentVersion=NULL WHERE OwnerId=@Owner AND Id=@Id; DELETE [automation].[DefinitionVersion] WHERE OwnerId=@Owner AND DefinitionId=@Id; DELETE [automation].[Definition] WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId))
        { Add(remove, "@Id", SqlDbType.UniqueIdentifier, id); remove.ExecuteNonQuery(); }
        using (var mark = Command(c, tx, "UPDATE [platform].[TrashItem] SET PurgedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id AND DeletionBatchId=@Batch AND RestoredAt IS NULL AND PurgedAt IS NULL; UPDATE [platform].[Resource] SET Availability='Purged',PurgedAt=SYSUTCDATETIME(),Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Trash' AND PurgedAt IS NULL;", actor.OwnerId))
        {
            Add(mark, "@Id", SqlDbType.UniqueIdentifier, id); Add(mark, "@Batch", SqlDbType.UniqueIdentifier, body.DeletionBatchId); Add(mark, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            if (mark.ExecuteNonQuery() != 2) throw new InvalidOperationException();
        }
        var result = new AutomationPurgeAcknowledgement(id, body.DeletionBatchId, true);
        Audit(c, tx, actor, action, id, trace); receipts.Complete(c, tx, claim, "DefinitionPurged", 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, action)) return Denied<AutomationPurgeAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationPurgeAcknowledgement>.Success(result);
    });

    private static AutomationPurgePreview PurgePlan(SqlConnection c, SqlTransaction tx, Guid owner, AutomationDefinitionRecord definition)
    {
        using var query = Command(c, tx, """
            SELECT (SELECT COUNT_BIG(*) FROM [automation].[DefinitionVersion] WHERE OwnerId=@Owner AND DefinitionId=@Id),
            (SELECT COUNT_BIG(*) FROM [automation].[ScheduleVersion] WHERE OwnerId=@Owner AND DefinitionId=@Id),
            CASE WHEN EXISTS(SELECT 1 FROM [automation].[Run] WHERE OwnerId=@Owner AND DefinitionId=@Id)
                   OR EXISTS(SELECT 1 FROM [automation].[ScheduleOccurrence] WHERE OwnerId=@Owner AND DefinitionId=@Id) THEN 1 ELSE 0 END,
            CASE WHEN EXISTS(SELECT 1 FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached')
                   OR EXISTS(SELECT 1 FROM [files].[FileReference] WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id) THEN 1 ELSE 0 END,
            CASE WHEN (SELECT COUNT_BIG(*) FROM [platform].[TrashItem] WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id AND RestoredAt IS NULL AND PurgedAt IS NULL)=1
                   AND EXISTS(SELECT 1 FROM [platform].[TrashItem] WHERE OwnerId=@Owner AND ResourceType='AutomationDefinition' AND ResourceId=@Id AND DeletionBatchId=@Batch AND RestoredAt IS NULL AND PurgedAt IS NULL) THEN 1 ELSE 0 END;
            """, owner);
        Add(query, "@Id", SqlDbType.UniqueIdentifier, definition.Id); Add(query, "@Batch", SqlDbType.UniqueIdentifier, definition.DeletionBatchId);
        using var reader = query.ExecuteReader(); if (!reader.Read()) throw new InvalidOperationException();
        var blockers = new List<string>();
        if (reader.GetInt32(2) == 1) blockers.Add("RetainedOperationalHistory");
        if (reader.GetInt32(3) == 1) blockers.Add("ResourceReferences");
        if (reader.GetInt32(4) != 1) blockers.Add("DeletionCohortUnavailable");
        return new(definition.Id, definition.DeletionBatchId!.Value, definition.ETag, reader.GetInt64(0), reader.GetInt64(1), blockers);
    }
}
