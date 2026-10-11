using Nexora.Application.Identity;

namespace Nexora.Application.Automation;

public sealed record AutomationDefinitionCommand(string Title, string DefinitionJson);
public sealed record AutomationDefinitionRecord(Guid Id, string Title, long CurrentVersion,
    string State, bool Enabled, DateTimeOffset UpdatedAt, string ETag);
public sealed record AutomationDefinitionVersion(Guid DefinitionId, long VersionNumber,
    string Title, string DefinitionJson, DateTimeOffset CreatedAt);
public sealed record AutomationDefinitionDetail(AutomationDefinitionRecord Definition, AutomationDefinitionVersion Version);
public sealed record AutomationDefinitionPage(IReadOnlyList<AutomationDefinitionRecord> Items, string? NextCursor);
public sealed record AutomationVersionPage(IReadOnlyList<AutomationDefinitionVersion> Items, long? NextVersion);
// An acknowledgement identifies a historical commit. It is not the current resource projection.
public sealed record AutomationDefinitionAcknowledgement(Guid Id, long VersionNumber, string ETag);
public sealed record AutomationDefinitionValidation(bool ShapeAccepted, string Code, bool ExecutionAvailable, bool VerifiedSimulation);

public interface IAutomationDefinitionService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<AutomationDefinitionPage> List(IdentityPrincipal actor, string? cursor, string? query);
    IdentityOperationResult<AutomationDefinitionDetail> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationVersionPage> History(IdentityPrincipal actor, Guid id, long? beforeVersion);
    IdentityOperationResult<AutomationDefinitionValidation> Validate(IdentityPrincipal actor, AutomationDefinitionCommand command);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Create(IdentityPrincipal actor, AutomationDefinitionCommand command, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> Save(IdentityPrincipal actor, Guid id, string? etag, AutomationDefinitionCommand command, string key, string? trace);
}
