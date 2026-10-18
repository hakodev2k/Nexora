using System.Text.Json.Serialization;

namespace Nexora.Api.Features.Goals;

public sealed record GoalRequest(string Title, string? Description, DateOnly? StartDate, DateOnly? EndDate,
    NumericTargetRequest? NumericTarget);

public sealed record NumericTargetRequest(string Title,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal InitialValue,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal CurrentValue,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal TargetValue);

public sealed record GoalProgressRequest(
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal CurrentValue, string? Note);

public sealed record GoalTransitionRequest(string Status);
public sealed record BooleanTargetRequest(string Title, bool? BooleanValue);
public sealed record BooleanProgressRequest(bool? BooleanValue, string? Note);
public sealed record GoalTargetChangeRequest(string Mode, string? Title,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? InitialValue,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? TargetValue);
public sealed record GoalTargetCommitRequest(string PreviewToken);
public sealed record NumericTargetCreateRequest(string Title,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? InitialValue,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? CurrentValue,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? TargetValue);

public sealed record GoalResponse(Guid Id, string Title, string? Description, DateOnly? StartDate, DateOnly? EndDate,
    string Status, decimal Progress, int TargetCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag,
    string? PreArchiveStatus = null);

public sealed record GoalTargetResponse(Guid Id, string Kind, string Title, decimal? InitialValue, decimal? CurrentValue,
    decimal? TargetValue, decimal Progress, DateTimeOffset UpdatedAt, string ETag,
    string? InitialValueExact = null, string? CurrentValueExact = null, string? TargetValueExact = null,
    bool? BooleanValue = null);

public sealed record GoalDetailResponse(GoalResponse Goal, IReadOnlyList<GoalTargetResponse> Targets);

public sealed record GoalPageResponse(IReadOnlyList<GoalResponse> Items, string? NextCursor);
