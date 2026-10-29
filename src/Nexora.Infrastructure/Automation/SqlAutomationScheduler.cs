using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Automation;

public sealed partial class SqlAutomationDefinitionService
{
    private bool BindScheduleAuthority(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, AutomationDefinitionRecord definition)
    {
        AutomationScheduleCommand configuration; long boundVersion;
        using (var schedule = Command(c, tx, "SELECT v.ConfigurationJson,v.DefinitionVersion FROM [automation].[Schedule] s WITH(UPDLOCK,HOLDLOCK) JOIN [automation].[ScheduleVersion] v ON v.OwnerId=s.OwnerId AND v.DefinitionId=s.DefinitionId AND v.VersionNumber=s.CurrentVersion WHERE s.OwnerId=@Owner AND s.DefinitionId=@Id AND s.Enabled=1;", actor.OwnerId))
        {
            Add(schedule, "@Id", SqlDbType.UniqueIdentifier, definition.Id); using var reader = schedule.ExecuteReader();
            if (!reader.Read()) return true;
            configuration = JsonSerializer.Deserialize<AutomationScheduleCommand>(reader.GetString(0)) ?? throw new InvalidOperationException(); boundVersion = reader.GetInt64(1);
        }
        var plan = AutomationScheduleContract.Preview(configuration, DateTimeOffset.UtcNow);
        if (!localSchedulingEnabled || actor.SessionId is null || !configuration.Enabled || !plan.Accepted || plan.NextRuns.Count == 0 ||
            definition.CurrentVersion != boundVersion || !Allowed(c, tx, actor, "automation.schedule.update") || !RunAllowed(c, tx, actor, "automation.run.start")) return false;
        using var update = Command(c, tx, "UPDATE [automation].[Schedule] SET ActorUserId=@User,SessionId=@Session,ActorRole=@Role,AuthenticatedAt=@Auth,NextDueAt=@Next WHERE OwnerId=@Owner AND DefinitionId=@Id AND Enabled=1;", actor.OwnerId);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, definition.Id); Add(update, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(update, "@Session", SqlDbType.UniqueIdentifier, actor.SessionId);
        Add(update, "@Role", SqlDbType.VarChar, actor.Role, 32); Add(update, "@Auth", SqlDbType.DateTime2, actor.RecentAuthenticatedAt.UtcDateTime); Add(update, "@Next", SqlDbType.DateTime2, plan.NextRuns[0].UtcDateTime);
        return update.ExecuteNonQuery() == 1;
    }
    // One durable occurrence per call. Timer is only a wake-up signal; SQL owns progress.
    public void ProcessDueSchedule()
    {
        if (!localExecutionEnabled || !localSchedulingEnabled) return;
        Guid owner, definitionId; IdentityPrincipal actor;
        using (var advisory = connections.Create())
        {
            advisory.Open(); using var candidate = advisory.CreateCommand();
            candidate.CommandText = "SELECT TOP(1) OwnerId,DefinitionId,ActorUserId,SessionId,ActorRole,AuthenticatedAt FROM [automation].[Schedule] WHERE Enabled=1 AND NextDueAt<=SYSUTCDATETIME() ORDER BY NextDueAt,OwnerId,DefinitionId;";
            using var reader = candidate.ExecuteReader(); if (!reader.Read()) return;
            owner = reader.GetGuid(0); definitionId = reader.GetGuid(1);
            actor = new(reader.GetGuid(2), owner, reader.GetString(4), Utc(reader.GetDateTime(5)), reader.GetGuid(3));
        }
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        // Match owner -> definition -> run lock ordering used by current mutations/dispatch.
        var ownerActive = LockOwner(c, tx, actor);
        var definition = Read(c, tx, owner, definitionId, true);
        long scheduleVersion, boundVersion; DateTimeOffset due, now; AutomationScheduleCommand configuration;
        using (var schedule = Command(c, tx, "SELECT s.CurrentVersion,v.DefinitionVersion,s.NextDueAt,v.ConfigurationJson,SYSUTCDATETIME(),s.ActorUserId,s.SessionId,s.ActorRole,s.AuthenticatedAt FROM [automation].[Schedule] s WITH(UPDLOCK,HOLDLOCK) JOIN [automation].[ScheduleVersion] v ON v.OwnerId=s.OwnerId AND v.DefinitionId=s.DefinitionId AND v.VersionNumber=s.CurrentVersion WHERE s.OwnerId=@Owner AND s.DefinitionId=@Id AND s.Enabled=1 AND s.NextDueAt<=SYSUTCDATETIME();", owner))
        {
            Add(schedule, "@Id", SqlDbType.UniqueIdentifier, definitionId); using var reader = schedule.ExecuteReader(); if (!reader.Read()) return;
            scheduleVersion = reader.GetInt64(0); boundVersion = reader.GetInt64(1); due = Utc(reader.GetDateTime(2)); now = Utc(reader.GetDateTime(4));
            configuration = JsonSerializer.Deserialize<AutomationScheduleCommand>(reader.GetString(3)) ?? throw new InvalidOperationException();
            // Advisory fields are not authority: reload the exact persisted snapshot under lock.
            actor = new(reader.GetGuid(5), owner, reader.GetString(7), Utc(reader.GetDateTime(8)), reader.GetGuid(6));
        }
        var plan = AutomationScheduleContract.Preview(configuration, now);
        string? skip = !configuration.Enabled || !plan.Accepted ? "ScheduleInvalid" : null;
        if (skip is null && (!ownerActive || !RunAllowed(c, tx, actor, "automation.run.start") || !Allowed(c, tx, actor, "automation.schedule.update"))) skip = "CurrentAuthorityUnavailable";
        if (skip is null && (definition is null || definition.Lifecycle != "Active" || !definition.Enabled)) skip = "DefinitionDisabled";
        if (skip is null && definition!.CurrentVersion != boundVersion) skip = "ScheduleBindingStale";
        if (skip is null && due <= now.AddMinutes(-1)) skip = "MissedOccurrenceSkipped";
        AutomationDefinitionShape? shape = null;
        if (skip is null)
        {
            var version = Versions(c, tx, owner, definitionId, boundVersion + 1, 1).SingleOrDefault();
            var parsed = AutomationDefinitionContract.Parse(version?.DefinitionJson);
            if (version?.VersionNumber != boundVersion || !parsed.Accepted || !TrustedExecution(parsed.Definition!) || CheckTargets(c, tx, actor, parsed.Definition!) is not null) skip = "TargetUnavailable";
            else shape = parsed.Definition;
        }
        bool alreadyEvaluated;
        using (var existing = Command(c, tx, "SELECT 1 FROM [automation].[ScheduleOccurrence] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND DefinitionId=@Id AND ScheduleVersion=@Version AND DueAt=@Due;", owner))
        {
            Add(existing, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(existing, "@Version", SqlDbType.BigInt, scheduleVersion); Add(existing, "@Due", SqlDbType.DateTime2, due.UtcDateTime);
            alreadyEvaluated = existing.ExecuteScalar() is not null;
        }
        Guid? runId = null;
        if (!alreadyEvaluated && skip is null)
        {
            // Existing latest-queued policy preserves Running and all completed effects.
            SkipQueued(c, tx, owner, definitionId, "SupersededQueuedRun");
            runId = Guid.NewGuid();
            using (var run = Command(c, tx, "INSERT [automation].[Run](Id,OwnerId,DefinitionId,VersionNumber,ActorUserId,SessionId,ActorRole,AuthenticatedAt) VALUES(@Run,@Owner,@Id,@Version,@User,@Session,@Role,@Auth);", owner))
            {
                Add(run, "@Run", SqlDbType.UniqueIdentifier, runId); Add(run, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(run, "@Version", SqlDbType.BigInt, boundVersion);
                Add(run, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(run, "@Session", SqlDbType.UniqueIdentifier, actor.SessionId); Add(run, "@Role", SqlDbType.VarChar, actor.Role, 32); Add(run, "@Auth", SqlDbType.DateTime2, actor.RecentAuthenticatedAt.UtcDateTime); run.ExecuteNonQuery();
            }
            for (var index = 0; index < shape!.Steps.Length; index++)
            {
                using var step = Command(c, tx, "INSERT [automation].[StepRun](OwnerId,RunId,StepId,Position,CommandKey) VALUES(@Owner,@Run,@Step,@Position,@Key);", owner);
                Add(step, "@Run", SqlDbType.UniqueIdentifier, runId); Add(step, "@Step", SqlDbType.Int, shape.Steps[index].Id); Add(step, "@Position", SqlDbType.Int, index + 1); Add(step, "@Key", SqlDbType.UniqueIdentifier, Guid.NewGuid()); step.ExecuteNonQuery();
            }
        }
        if (!alreadyEvaluated)
        {
            using var occurrence = Command(c, tx, "INSERT [automation].[ScheduleOccurrence](OwnerId,DefinitionId,ScheduleVersion,DueAt,State,Code,RunId) VALUES(@Owner,@Id,@Version,@Due,@State,@Code,@Run);", owner);
            Add(occurrence, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(occurrence, "@Version", SqlDbType.BigInt, scheduleVersion); Add(occurrence, "@Due", SqlDbType.DateTime2, due.UtcDateTime);
            Add(occurrence, "@State", SqlDbType.VarChar, skip is null ? "Queued" : "Skipped", 32); Add(occurrence, "@Code", SqlDbType.VarChar, skip ?? "OccurrenceQueued", 64); Add(occurrence, "@Run", SqlDbType.UniqueIdentifier, runId); occurrence.ExecuteNonQuery();
            Audit(c, tx, actor, "automation.schedule.update", definitionId, "worker:schedule-evaluation");
        }
        using (var advance = Command(c, tx, "UPDATE [automation].[Schedule] SET NextDueAt=@Next WHERE OwnerId=@Owner AND DefinitionId=@Id AND CurrentVersion=@Version;", owner))
        {
            Add(advance, "@Id", SqlDbType.UniqueIdentifier, definitionId); Add(advance, "@Version", SqlDbType.BigInt, scheduleVersion);
            Add(advance, "@Next", SqlDbType.DateTime2, plan.Accepted && plan.NextRuns.Count > 0 ? plan.NextRuns[0].UtcDateTime : null);
            if (advance.ExecuteNonQuery() != 1) throw new InvalidOperationException();
        }
        if (runId is not null && (!RunAllowed(c, tx, actor, "automation.run.start") || !Allowed(c, tx, actor, "automation.schedule.update"))) return;
        tx.Commit();
    }
    private bool ScheduledRunAllowed(SqlConnection c, SqlTransaction tx, Work work)
    {
        using var query = Command(c, tx, "SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM [automation].[ScheduleOccurrence] WHERE OwnerId=@Owner AND RunId=@Run) THEN 0 WHEN EXISTS(SELECT 1 FROM [automation].[ScheduleOccurrence] o JOIN [automation].[Schedule] s ON s.OwnerId=o.OwnerId AND s.DefinitionId=o.DefinitionId AND s.CurrentVersion=o.ScheduleVersion JOIN [automation].[ScheduleVersion] v ON v.OwnerId=s.OwnerId AND v.DefinitionId=s.DefinitionId AND v.VersionNumber=s.CurrentVersion JOIN [automation].[Definition] d ON d.OwnerId=s.OwnerId AND d.Id=s.DefinitionId AND d.CurrentVersion=v.DefinitionVersion WHERE o.OwnerId=@Owner AND o.RunId=@Run AND s.Enabled=1) THEN 1 ELSE 2 END;", work.Owner);
        Add(query, "@Run", SqlDbType.UniqueIdentifier, work.Id); var state = Convert.ToInt32(query.ExecuteScalar());
        return state == 0 || (state == 1 && localSchedulingEnabled && Allowed(c, tx, work.Actor, "automation.schedule.update"));
    }
}
