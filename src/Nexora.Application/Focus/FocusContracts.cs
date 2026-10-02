using Nexora.Application.Identity;

namespace Nexora.Application.Focus;

public sealed record FocusPreferences(int FocusMinutes, int ShortBreakMinutes, int LongBreakMinutes, int CycleLength, string ETag);
public sealed record FocusPreferenceCommand(int FocusMinutes, int ShortBreakMinutes, int LongBreakMinutes, int CycleLength);
public sealed record FocusStart(string Phase);
public sealed record FocusSession(Guid Id, string Phase, string State, int PlannedSeconds,
    long ElapsedMilliseconds, long RemainingMilliseconds, DateTimeOffset StartedAt,
    DateTimeOffset? LastRunAt, DateTimeOffset? CompletedAt, string ETag);
public sealed record FocusPage(IReadOnlyList<FocusSession> Items, Guid? NextCursor);
public sealed record FocusTimeConversion(Guid SessionId, Guid EntryId);
public sealed record FocusRecordTimeCommand(bool ConfirmOverlap = false);

public interface IFocusService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<FocusPage> List(IdentityPrincipal actor, Guid? cursor, bool activeOnly);
    IdentityOperationResult<FocusPreferences> Preferences(IdentityPrincipal actor);
    IdentityOperationResult<FocusPreferences> SavePreferences(IdentityPrincipal actor, FocusPreferenceCommand body, string? etag, string key);
    IdentityOperationResult<FocusSession> Start(IdentityPrincipal actor, FocusStart body, string key);
    IdentityOperationResult<FocusTimeConversion> RecordTime(IdentityPrincipal actor, Guid id, string? etag, FocusRecordTimeCommand body, string key);
    IdentityOperationResult<FocusSession> Transition(IdentityPrincipal actor, Guid id, string action, string? etag, string key);
}
public interface IFocusPhaseFinisher { Task FinishDueAsync(CancellationToken cancellationToken); }
