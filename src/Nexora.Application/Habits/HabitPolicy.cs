namespace Nexora.Application.Habits;

public static class HabitKinds
{
    public const string Boolean = "Boolean";
    public const string Count = "Count";
}

public static class HabitStates
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Archived = "Archived";
}

public static class HabitPolicy
{
    public const int MaximumTitleLength = 100;
    public const int MaximumUnitLength = 50;
    public const int MaximumCheckInNoteLength = 1000;

    public static bool IsValidWeekdayMask(byte value) => value is >= 1 and <= 127;

    public static bool IsScheduled(DateOnly date, byte weekdayMask) =>
        (weekdayMask & (1 << (((int)date.DayOfWeek + 6) % 7))) != 0;

    public static bool MeetsTarget(string kind, int count, int? targetCount) =>
        kind == HabitKinds.Boolean ? count == 1 : count >= targetCount.GetValueOrDefault();

    public static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
