using Nexora.Application.Identity;

namespace Nexora.Application.Habits;

public sealed record HabitScheduleRecord(
    Guid Id,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil,
    byte WeekdayMask,
    int? TargetCount,
    bool Paused,
    string ETag);

public sealed record HabitCheckInRecord(
    Guid Id,
    DateOnly LocalDate,
    int Count,
    string? Note,
    Guid ScheduleId,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record HabitRecord(
    Guid Id,
    string Title,
    string Kind,
    int? TargetCount,
    string? Unit,
    string State,
    string TimeZoneId,
    TimeOnly? ReminderLocalTime,
    HabitScheduleRecord? CurrentSchedule,
    int CurrentStreak,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record HabitDetail(HabitRecord Habit, IReadOnlyList<HabitScheduleRecord> Schedules,
    IReadOnlyList<HabitCheckInRecord> CheckIns);

public sealed record HabitPage(IReadOnlyList<HabitRecord> Items, string? NextCursor);

public sealed record HabitCreateCommand(
    string Title,
    string Kind,
    int? TargetCount,
    string? Unit,
    DateOnly EffectiveFrom,
    byte WeekdayMask,
    string TimeZoneId,
    TimeOnly? ReminderLocalTime);

public sealed record HabitUpdateCommand(string Title, string? Unit, TimeOnly? ReminderLocalTime, string TimeZoneId);

public sealed record HabitScheduleCommand(DateOnly EffectiveFrom, byte WeekdayMask, int? TargetCount);

public sealed record HabitCheckInCommand(DateOnly LocalDate, int Count, string? Note);

public interface IHabitService
{
    IdentityOperationResult<HabitPage> List(IdentityPrincipal actor, string? state = null, string? query = null, int? limit = null);
    IdentityOperationResult<HabitDetail> Get(IdentityPrincipal actor, Guid habitId, DateOnly? from = null, DateOnly? to = null);
    IdentityOperationResult<HabitDetail> Create(IdentityPrincipal actor, HabitCreateCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<HabitDetail> Update(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitUpdateCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<HabitDetail> SetSchedule(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitScheduleCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<HabitDetail> RecordCheckIn(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitCheckInCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<HabitDetail> Transition(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        string state, string? idempotencyKey = null, string? traceId = null);
}
