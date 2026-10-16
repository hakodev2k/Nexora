namespace Nexora.Api.Features.Goals;

public sealed record GoalRequest(string Title, string? Description, DateOnly? StartDate, DateOnly? EndDate,
    NumericTargetRequest? NumericTarget);

public sealed record NumericTargetRequest(string Title, decimal InitialValue, decimal CurrentValue, decimal TargetValue);

public sealed record GoalProgressRequest(decimal CurrentValue, string? Note);

public sealed record GoalTransitionRequest(string Status);

public sealed record GoalResponse(Guid Id, string Title, string? Description, DateOnly? StartDate, DateOnly? EndDate,
    string Status, decimal Progress, int TargetCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);

public sealed record GoalTargetResponse(Guid Id, string Kind, string Title, decimal? InitialValue, decimal? CurrentValue,
    decimal? TargetValue, decimal Progress, DateTimeOffset UpdatedAt, string ETag);

public sealed record GoalDetailResponse(GoalResponse Goal, IReadOnlyList<GoalTargetResponse> Targets);

public sealed record GoalPageResponse(IReadOnlyList<GoalResponse> Items, string? NextCursor);
