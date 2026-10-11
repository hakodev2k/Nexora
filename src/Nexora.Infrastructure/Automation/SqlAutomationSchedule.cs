using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;
using Nexora.Domain.Automation;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    public IdentityOperationResult<AutomationScheduleSnapshot> Schedule(IdentityPrincipal actor, Guid id) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "automation.definition.read")) return Denied<AutomationScheduleSnapshot>();
        var definition = Read(c, tx, actor.OwnerId, id, false);
        if (definition is null) return Missing<AutomationScheduleSnapshot>();
        long scheduleVersion = 0, definitionVersion = definition.CurrentVersion; AutomationScheduleCommand? configuration = null;
        using (var query = Command(c, tx, "SELECT v.VersionNumber,v.DefinitionVersion,v.ConfigurationJson FROM [automation].[Schedule] s JOIN [automation].[ScheduleVersion] v ON v.OwnerId=s.OwnerId AND v.DefinitionId=s.DefinitionId AND v.VersionNumber=s.CurrentVersion WHERE s.OwnerId=@Owner AND s.DefinitionId=@Id;", actor.OwnerId))
        {
            Add(query, "@Id", SqlDbType.UniqueIdentifier, id); using var reader = query.ExecuteReader();
            if (reader.Read()) { scheduleVersion = reader.GetInt64(0); definitionVersion = reader.GetInt64(1); configuration = JsonSerializer.Deserialize<AutomationScheduleCommand>(reader.GetString(2)) ?? throw new InvalidOperationException(); }
        }
        var plan = configuration is null ? null : AutomationScheduleContract.Preview(configuration, DateTimeOffset.UtcNow);
        if (plan is { Accepted: false }) return Fail<AutomationScheduleSnapshot>("ScheduleUnavailable", 409, "Stored schedule requires review.");
        if (!Allowed(c, tx, actor, "automation.definition.read")) return Denied<AutomationScheduleSnapshot>();
        tx.Commit(); return IdentityOperationResult<AutomationScheduleSnapshot>.Success(new(id, definitionVersion, scheduleVersion, configuration,
            plan?.NextRuns ?? [], definition.ETag, definitionVersion != definition.CurrentVersion));
    });

    public IdentityOperationResult<AutomationSchedulePlan> PreviewSchedule(IdentityPrincipal actor, Guid id, AutomationScheduleCommand body) => Safe(() =>
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "automation.schedule.update")) return Denied<AutomationSchedulePlan>();
        var definition = Read(c, tx, actor.OwnerId, id, false);
        if (definition is null || definition.Lifecycle != "Active") return Missing<AutomationSchedulePlan>();
        var plan = AutomationScheduleContract.Preview(body, DateTimeOffset.UtcNow);
        if (!plan.Accepted) return Fail<AutomationSchedulePlan>(plan.Code, 422, "Check schedule frequency, IANA zone, bounds and Skip policy.");
        if (!Allowed(c, tx, actor, "automation.schedule.update")) return Denied<AutomationSchedulePlan>();
        tx.Commit(); return IdentityOperationResult<AutomationSchedulePlan>.Success(plan);
    });

    public IdentityOperationResult<AutomationDefinitionAcknowledgement> UpdateSchedule(IdentityPrincipal actor, Guid id, string? etag,
        AutomationScheduleCommand body, string key, string? trace) => Safe(() =>
    {
        if (!CommandIdentity(key, etag, out var uuid)) return Fail<AutomationDefinitionAcknowledgement>("ValidationFailed", string.IsNullOrEmpty(etag) ? 428 : 422, "Supply a UUID and If-Match revision.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!LockOwner(c, tx, actor) || !Allowed(c, tx, actor, "automation.schedule.update")) return Denied<AutomationDefinitionAcknowledgement>();
        var definition = Read(c, tx, actor.OwnerId, id, true);
        if (definition is null) return Missing<AutomationDefinitionAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, "automation.schedule.update", uuid.ToString("D"),
            JsonSerializer.Serialize(new { owner = actor.OwnerId, id, etag, body }), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<AutomationDefinitionAcknowledgement>("IdempotencyConflict", 409, "The key belongs to another command.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var ack = JsonSerializer.Deserialize<AutomationDefinitionAcknowledgement>(saved) ?? throw new InvalidOperationException();
            if (ack.Id != id || !Allowed(c, tx, actor, "automation.schedule.update")) return Denied<AutomationDefinitionAcknowledgement>();
            tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(ack);
        }
        if (!claim.IsClaimed) return Fail<AutomationDefinitionAcknowledgement>("RequestInProgress", 409, "Retry the same command.");
        if (definition.ETag != etag) return Fail<AutomationDefinitionAcknowledgement>("RevisionConflict", 412, "Compare current definition and schedule revision.");
        if (definition.Lifecycle != "Active" || !AutomationDefinition.CanSave(definition.State, definition.Enabled))
            return Fail<AutomationDefinitionAcknowledgement>("DefinitionLocked", 409, "Disable the definition before changing its schedule.");
        var plan = AutomationScheduleContract.Preview(body, DateTimeOffset.UtcNow);
        if (!plan.Accepted) return Fail<AutomationDefinitionAcknowledgement>(plan.Code, 422, "Check the schedule configuration.");
        var version = Versions(c, tx, actor.OwnerId, id, definition.CurrentVersion + 1, 1).Single();
        var shape = AutomationDefinitionContract.Parse(version.DefinitionJson);
        if (!shape.Accepted || CheckTargets(c, tx, actor, shape.Definition!) is not null)
            return Fail<AutomationDefinitionAcknowledgement>("TargetUnavailable", 422, "Validate the pinned definition and current target authority.");
        long nextVersion;
        using (var current = Command(c, tx, "SELECT CurrentVersion FROM [automation].[Schedule] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND DefinitionId=@Id;", actor.OwnerId))
        { Add(current, "@Id", SqlDbType.UniqueIdentifier, id); nextVersion = current.ExecuteScalar() is long number ? checked(number + 1) : 1; }
        using (var insert = Command(c, tx, "INSERT [automation].[ScheduleVersion](OwnerId,DefinitionId,VersionNumber,DefinitionVersion,ConfigurationJson,CreatedByUserId) VALUES(@Owner,@Id,@Version,@DefinitionVersion,@Json,@User); IF EXISTS(SELECT 1 FROM [automation].[Schedule] WHERE OwnerId=@Owner AND DefinitionId=@Id) UPDATE [automation].[Schedule] SET CurrentVersion=@Version,NextDueAt=@Next WHERE OwnerId=@Owner AND DefinitionId=@Id; ELSE INSERT [automation].[Schedule](OwnerId,DefinitionId,CurrentVersion,NextDueAt) VALUES(@Owner,@Id,@Version,@Next); UPDATE [automation].[Definition] SET UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId))
        {
            Add(insert, "@Id", SqlDbType.UniqueIdentifier, id); Add(insert, "@Version", SqlDbType.BigInt, nextVersion);
            Add(insert, "@DefinitionVersion", SqlDbType.BigInt, definition.CurrentVersion); Add(insert, "@Json", SqlDbType.NVarChar, JsonSerializer.Serialize(body), 2048);
            Add(insert, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(insert, "@Next", SqlDbType.DateTime2, plan.NextRuns.FirstOrDefault() == default ? null : plan.NextRuns[0].UtcDateTime);
            insert.ExecuteNonQuery();
        }
        Audit(c, tx, actor, "automation.schedule.update", id, trace);
        var updated = Read(c, tx, actor.OwnerId, id, false)!;
        var result = new AutomationDefinitionAcknowledgement(id, updated.CurrentVersion, updated.ETag);
        receipts.Complete(c, tx, claim, "ScheduleSaved", 200, JsonSerializer.Serialize(result));
        if (!Allowed(c, tx, actor, "automation.schedule.update")) return Denied<AutomationDefinitionAcknowledgement>();
        tx.Commit(); return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Success(result);
    });
}
