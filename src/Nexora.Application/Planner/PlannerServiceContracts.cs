using Nexora.Application.Identity;

namespace Nexora.Application.Planner;

public sealed record PlannerPinRecord(
    Guid Id,
    Guid TaskId,
    string TaskTitle,
    string TaskStatus,
    string ProjectName,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    DateOnly PlanDate,
    decimal Rank,
    string? Notes,
    bool SourceAvailable,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record PlannerPlan(DateOnly From, DateOnly To, IReadOnlyList<PlannerPinRecord> Pins, string ETag);

public sealed record PlannerPinCommand(Guid TaskId, DateOnly PlanDate, string? Notes);

public sealed record PlannerPinUpdateCommand(DateOnly PlanDate, string? Notes);

public sealed record PlannerReorderCommand(DateOnly PlanDate, IReadOnlyList<Guid> PinIds);

public interface IPlannerService
{
    IdentityOperationResult<PlannerPlan> List(IdentityPrincipal actor, DateOnly from, DateOnly to);
    IdentityOperationResult<PlannerPinRecord> Pin(IdentityPrincipal actor, PlannerPinCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<PlannerPinRecord> Update(IdentityPrincipal actor, Guid pinId, string? ifMatch,
        PlannerPinUpdateCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<PlannerPlan> Reorder(IdentityPrincipal actor, string? ifMatch,
        PlannerReorderCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Unpin(IdentityPrincipal actor, Guid pinId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
}
