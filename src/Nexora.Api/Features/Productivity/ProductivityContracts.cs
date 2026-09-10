namespace Nexora.Api.Features.Productivity;

public sealed record ProjectRequest(string Name, string? Description, DateTimeOffset? StartAt = null, DateTimeOffset? EndAt = null, string Priority = "P3", string? TagsJson = null, string? Notes = null);

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null,
    string Priority = "P3",
    string? TagsJson = null,
    string? Notes = null);

public sealed record ProjectPageResponse(IReadOnlyList<ProjectResponse> Items, string? NextCursor);

public sealed record TaskRequest(
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null,
    string Priority = "P3",
    string? TagsJson = null,
    string? AcceptanceCriteriaJson = null,
    int Rank = 0,
    DateTimeOffset? ReminderAt = null);

public sealed record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null,
    string Priority = "P3",
    string? TagsJson = null,
    string? AcceptanceCriteriaJson = null,
    int Rank = 0,
    DateTimeOffset? ReminderAt = null);

public sealed record TaskPageResponse(IReadOnlyList<TaskResponse> Items, string? NextCursor);

public sealed record EventRequest(
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZoneId,
    bool IsAllDay = false,
    string? SourceUid = null);

public sealed record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZoneId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    bool IsAllDay = false,
    string? SourceUid = null);

public sealed record ProductivityTransitionRequest(string Status, string? Reason);

public sealed record EventPageResponse(IReadOnlyList<EventResponse> Items, string? NextCursor);
