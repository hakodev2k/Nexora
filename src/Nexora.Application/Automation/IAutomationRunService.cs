using Nexora.Application.Identity;
using Nexora.Application.Notifications;
namespace Nexora.Application.Automation;

public sealed record AutomationRunRecord(Guid Id, Guid DefinitionId, long VersionNumber, string State,
    DateTimeOffset CreatedAt, DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? Code, bool EffectUncertain, string ETag);
public sealed record AutomationStepRunRecord(int StepId, int Position, string State, int Attempts, Guid? TaskId, string? Code, bool EffectUncertain);
public sealed record AutomationFailureNotice(Guid Id, IReadOnlyList<NotificationDeliveryRecord> Deliveries);
public sealed record AutomationRunDetail(AutomationRunRecord Run, IReadOnlyList<AutomationStepRunRecord> Steps, AutomationFailureNotice? FailureNotice = null);
public sealed record AutomationRunPage(IReadOnlyList<AutomationRunRecord> Items, string? NextCursor);
// Historical identity acknowledgement; always reconcile current run separately.
public sealed record AutomationRunAcknowledgement(Guid Id);
public interface IAutomationRunService
{
    IdentityOperationResult<AutomationRunAcknowledgement> RetryStep(IdentityPrincipal actor, Guid id, int stepId, string? etag, string key, string? trace);
    IdentityOperationResult<AutomationDefinitionAcknowledgement> SetEnabled(IdentityPrincipal actor, Guid id, bool enabled, string? etag, string key, string? trace);
    IdentityOperationResult<AutomationRunAcknowledgement> Start(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace);
    IdentityOperationResult<AutomationRunPage> Runs(IdentityPrincipal actor, Guid id, string? cursor, string? state,
        long? version = null, string? createdFrom = null, string? createdBefore = null);
    IdentityOperationResult<AutomationRunDetail> Run(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<AutomationRunAcknowledgement> Cancel(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace);
}
