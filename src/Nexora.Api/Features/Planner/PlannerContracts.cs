namespace Nexora.Api.Features.Planner;

public sealed record PlannerPinRequest(Guid TaskId, DateOnly PlanDate, string? Notes);

public sealed record PlannerPinUpdateRequest(DateOnly PlanDate, string? Notes);

public sealed record PlannerReorderRequest(DateOnly PlanDate, IReadOnlyList<Guid> PinIds);

public sealed record PlannerPinResponse(Guid Id, Guid TaskId, string TaskTitle, string TaskStatus, string ProjectName,
    DateTimeOffset StartAt, DateTimeOffset EndAt, DateOnly PlanDate, decimal Rank, string? Notes, bool SourceAvailable,
    DateTimeOffset UpdatedAt, string ETag);

public sealed record PlannerPlanResponse(DateOnly From, DateOnly To, IReadOnlyList<PlannerPinResponse> Pins, string ETag);
