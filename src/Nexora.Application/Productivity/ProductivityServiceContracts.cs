using Nexora.Application.Identity;

namespace Nexora.Application.Productivity;

public sealed record ProjectCommand(
    string Name,
    string? Description,
    DateTimeOffset? StartAt = null,
    DateTimeOffset? EndAt = null,
    string Priority = "P3",
    string? TagsJson = null,
    string? Notes = null,
    string? TransitionReason = null);

public sealed record ProjectRecord(
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

public sealed record ProjectPage(IReadOnlyList<ProjectRecord> Items, string? NextCursor);

public sealed record TaskCommand(
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
    DateTimeOffset? ReminderAt = null,
    string? TransitionReason = null);

public sealed record TaskRecord(
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

public sealed record TaskPage(IReadOnlyList<TaskRecord> Items, string? NextCursor);

public sealed record EventCommand(
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string TimeZoneId,
    bool IsAllDay = false,
    string? SourceUid = null,
    string? Status = null);

public sealed record EventRecord(
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

public sealed record EventPage(IReadOnlyList<EventRecord> Items, string? NextCursor);

public interface IProductivityService
{
    IdentityOperationResult<ProjectPage> ListProjects(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<ProjectRecord> CreateProject(IdentityPrincipal actor, ProjectCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<ProjectRecord> UpdateProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, ProjectCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<ProjectRecord> TransitionProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> DeleteProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string? idempotencyKey = null, string? traceId = null);

    IdentityOperationResult<TaskPage> ListTasks(IdentityPrincipal actor, Guid? projectId = null, int? limit = null);
    IdentityOperationResult<TaskRecord> CreateTask(IdentityPrincipal actor, TaskCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<TaskRecord> UpdateTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, TaskCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<TaskRecord> TransitionTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> DeleteTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string? idempotencyKey = null, string? traceId = null);

    IdentityOperationResult<EventPage> ListEvents(IdentityPrincipal actor, DateTimeOffset? from = null, DateTimeOffset? to = null, int? limit = null);
    IdentityOperationResult<EventRecord> CreateEvent(IdentityPrincipal actor, EventCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<EventRecord> UpdateEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, EventCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<EventRecord> TransitionEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string status, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> DeleteEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string? idempotencyKey = null, string? traceId = null);
}
