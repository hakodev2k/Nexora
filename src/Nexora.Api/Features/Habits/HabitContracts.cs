namespace Nexora.Api.Features.Habits;

public sealed record HabitCreateRequest(string Title, string Kind, int? TargetCount, string? Unit, DateOnly EffectiveFrom,
    byte WeekdayMask, string TimeZoneId, TimeOnly? ReminderLocalTime);

public sealed record HabitUpdateRequest(string Title, string? Unit, TimeOnly? ReminderLocalTime, string TimeZoneId);

public sealed record HabitScheduleRequest(DateOnly EffectiveFrom, byte WeekdayMask, int? TargetCount);

public sealed record HabitCheckInRequest(DateOnly LocalDate, int Count, string? Note);

public sealed record HabitTransitionRequest(string State);

public sealed record HabitScheduleResponse(Guid Id, DateOnly EffectiveFrom, DateOnly? EffectiveUntil, byte WeekdayMask,
    int? TargetCount, bool Paused, string ETag);

public sealed record HabitCheckInResponse(Guid Id, DateOnly LocalDate, int Count, string? Note, Guid ScheduleId,
    DateTimeOffset UpdatedAt, string ETag);

public sealed record HabitResponse(Guid Id, string Title, string Kind, int? TargetCount, string? Unit, string State,
    string TimeZoneId, TimeOnly? ReminderLocalTime, HabitScheduleResponse? CurrentSchedule, int CurrentStreak,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);

public sealed record HabitDetailResponse(HabitResponse Habit, IReadOnlyList<HabitScheduleResponse> Schedules,
    IReadOnlyList<HabitCheckInResponse> CheckIns);

public sealed record HabitPageResponse(IReadOnlyList<HabitResponse> Items, string? NextCursor);
