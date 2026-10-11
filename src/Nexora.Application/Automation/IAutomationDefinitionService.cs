using Nexora.Application.Identity;

namespace Nexora.Application.Automation;

public sealed record AutomationDefinitionCommand(string Title, string DefinitionJson);
public sealed record AutomationDefinitionRecord(Guid Id, string Title, long CurrentVersion,
    string State, bool Enabled, DateTimeOffset UpdatedAt, string ETag, string Lifecycle = "Active", Guid? DeletionBatchId = null);
public sealed record AutomationDefinitionVersion(Guid DefinitionId, long VersionNumber,
    string Title, string DefinitionJson, DateTimeOffset CreatedAt);
public sealed record AutomationDefinitionDetail(AutomationDefinitionRecord Definition, AutomationDefinitionVersion Version);
public sealed record AutomationDefinitionPage(IReadOnlyList<AutomationDefinitionRecord> Items, string? NextCursor);
public sealed record AutomationVersionPage(IReadOnlyList<AutomationDefinitionVersion> Items, long? NextVersion);
// An acknowledgement identifies a historical commit. It is not the current resource projection.
public sealed record AutomationDefinitionAcknowledgement(Guid Id, long VersionNumber, string ETag);
public sealed record AutomationDefinitionValidation(bool ShapeAccepted, string Code, bool ExecutionAvailable, bool VerifiedSimulation, bool ReferencesValid = false);

public sealed record AutomationPortableDefinition(int SchemaVersion, string Title, string DefinitionJson);
public sealed record AutomationPurgePreview(Guid Id, Guid DeletionBatchId, string ETag, long DefinitionVersions,
    long ScheduleVersions, IReadOnlyList<string> Blockers);
public sealed record AutomationPurgeCommand(Guid DeletionBatchId, bool ConfirmPermanentDeletion);
public sealed record AutomationPurgeAcknowledgement(Guid Id, Guid DeletionBatchId, bool Purged);

public interface IAutomationDefinitionService
{
    IdentityOperationResult<AutomationPurgePreview> PreviewPurge(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationPurgeAcknowledgement> Purge(IdentityPrincipal actor, Guid id, string? etag, AutomationPurgeCommand body, string key, string? trace);
    IdentityOperationResult<AutomationScheduleSnapshot> Schedule(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationSchedulePlan> PreviewSchedule(IdentityPrincipal actor, Guid id, AutomationScheduleCommand body);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> UpdateSchedule(IdentityPrincipal actor, Guid id, string? etag, AutomationScheduleCommand body, string key, string? trace);
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<AutomationDefinitionPage> List(IdentityPrincipal actor, string? cursor, string? query, string lifecycle = "Active");
    IdentityOperationResult<AutomationDefinitionDetail> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationVersionPage> History(IdentityPrincipal actor, Guid id, long? beforeVersion);
    IdentityOperationResult<AutomationDefinitionValidation> Validate(IdentityPrincipal actor, AutomationDefinitionCommand command);
    IdentityOperationResult<AutomationPortableDefinition> Export(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Import(IdentityPrincipal actor, AutomationPortableDefinition command, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Trash(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Restore(IdentityPrincipal actor, Guid id, Guid deletionBatchId, string? etag, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Create(IdentityPrincipal actor, AutomationDefinitionCommand command, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Save(IdentityPrincipal actor, Guid id, string? etag, AutomationDefinitionCommand command, string key, string? trace);
}
