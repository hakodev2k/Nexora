using Nexora.Application.Identity;

namespace Nexora.Application.TimeTracking;

public sealed record TimeEntry(Guid Id, DateTimeOffset StartAt, DateTimeOffset? EndAt,
    string? Description, string? Category, string Status, long DurationMilliseconds,
    DateTimeOffset UpdatedAt, string ETag);
public sealed record TimeEntryPage(IReadOnlyList<TimeEntry> Items, Guid? NextCursor);
public sealed record TimeEntryCommand(DateTimeOffset StartAt, DateTimeOffset EndAt,
    string? Description, string? Category, bool ConfirmOverlap = false);
public sealed record TimerCommand(string? Description, string? Category, Guid? ResumeEntryId = null);
public sealed record TimeCorrection(Guid Id, string Action, DateTimeOffset At, TimeEntry Before);
public sealed record TimeHistoryPage(IReadOnlyList<TimeCorrection> Items, Guid? NextCursor);
public sealed record TimeReport(long GrossDurationMilliseconds, int EntryCount, bool HasOverlaps);
public sealed record TimeEntryFilter(DateTimeOffset? From = null, DateTimeOffset? To = null,
    string? Category = null, string? Query = null);

public sealed record TimePurgePreview(Guid EntryId, string ETag, int CorrectionCount, bool HasConversionPin);
public sealed record TimePurgeCommand(bool ConfirmPermanentDeletion);
public sealed record TimePurged(Guid EntryId);

public interface ITimeTrackingService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<TimeEntryPage> List(IdentityPrincipal actor, Guid? cursor, bool trash, TimeEntryFilter? filter = null);
    IdentityOperationResult<TimeEntry> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<TimeEntry?> Timer(IdentityPrincipal actor);
    IdentityOperationResult<TimeEntry> Start(IdentityPrincipal actor, TimerCommand body, string key, string? trace);
    IdentityOperationResult<TimeEntry> Stop(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace);
    IdentityOperationResult<TimeEntry> Create(IdentityPrincipal actor, TimeEntryCommand body, string key, string? trace);
    IdentityOperationResult<TimeEntry> Update(IdentityPrincipal actor, Guid id, string? etag, TimeEntryCommand body, string key, string? trace);
    IdentityOperationResult<TimeEntry> Transition(IdentityPrincipal actor, Guid id, string? etag, bool restore, string key, string? trace);
    IdentityOperationResult<TimeHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor);
    IdentityOperationResult<TimePurgePreview> PreviewPurge(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<TimePurged> Purge(IdentityPrincipal actor, Guid id, string? etag, TimePurgeCommand body, string key, string? trace);
    IdentityOperationResult<TimeReport> Report(IdentityPrincipal actor, TimeEntryFilter? filter = null);
}
