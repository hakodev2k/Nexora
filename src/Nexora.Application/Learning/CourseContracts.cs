using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

public sealed record Course(Guid Id, string Title, string? Provider, string? Url, string ProgressMode,
    string? ManualProgress, bool ProgressModeLocked, int MilestonesDone, int MilestonesTotal,
    string Status, DateOnly? StartedOn, DateOnly? CompletedOn, string? Notes,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record CoursePage(IReadOnlyList<Course> Items, Guid? NextCursor);
public sealed record CourseMetadata(string Title, string ProgressMode, string? Provider = null,
    string? Url = null, string? Notes = null, DateOnly? StartedOn = null);
public sealed record CourseProgress(string? ManualProgress = null, bool Start = false, DateOnly? StartedOn = null);
public sealed record CourseConfirmation(bool Confirm, DateOnly? CompletedOn = null);
public sealed record CourseAcknowledgement(Guid ItemId, string? ETag, Guid? MilestoneId = null, string? MilestoneETag = null);
public sealed record CoursePreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record CourseMilestone(Guid Id, string Title, int Position, bool Completed, DateTimeOffset? CompletedAt, string ETag);
public sealed record CourseMilestonePage(IReadOnlyList<CourseMilestone> Items, Guid? NextCursor, string CourseETag);
public sealed record MilestoneDefinition(string Title);
public sealed record MilestoneCompletion(bool Completed);
public sealed record MilestoneMove(string Direction);

public interface ICourseService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<CoursePage> List(IdentityPrincipal actor, Guid? cursor, string? status, string? query);
    IdentityOperationResult<Course> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<CourseAcknowledgement> Create(IdentityPrincipal actor, CourseMetadata body, string key, string? trace);
    IdentityOperationResult<CourseAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, CourseMetadata body, string key, string? trace);
    IdentityOperationResult<CourseAcknowledgement> Progress(IdentityPrincipal actor, Guid id, string? etag, CourseProgress body, string key, string? trace);
    IdentityOperationResult<CoursePreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<CourseAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag, string operation, CourseConfirmation body, string key, string? trace);
    IdentityOperationResult<CourseMilestonePage> Milestones(IdentityPrincipal actor, Guid id, Guid? cursor);
    IdentityOperationResult<CourseAcknowledgement> AddMilestone(IdentityPrincipal actor, Guid id, string? etag, MilestoneDefinition body, string key, string? trace);
    IdentityOperationResult<CourseAcknowledgement> ChangeMilestone(IdentityPrincipal actor, Guid id, Guid child, string? etag, string? childETag,
        string operation, object body, string key, string? trace);
}
