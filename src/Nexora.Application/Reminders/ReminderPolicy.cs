namespace Nexora.Application.Reminders;

public static class ReminderSourceTypes
{
    public const string Task = "Task";
    public const string CalendarEvent = "CalendarEvent";

    public static bool IsSupported(string? value) =>
        string.Equals(value, Task, StringComparison.Ordinal) ||
        string.Equals(value, CalendarEvent, StringComparison.Ordinal);
}

public static class ReminderConfigurationTypes
{
    public const string None = "None";
    public const string BeforeStart15m = "BeforeStart15m";
    public const string Exact = "Exact";
}

public static class ReminderStates
{
    public const string None = "None";
    public const string Pending = "Pending";
    public const string Dispatched = "Dispatched";
    public const string Canceled = "Canceled";
    public const string Expired = "Expired";
    public const string Missed = "Missed";
}

public sealed record ReminderScheduleResolution(
    bool IsValid,
    string? Code,
    string? Title,
    DateTimeOffset? DueAt);

/// <summary>
/// Deterministic reminder rules shared by the API service and UI projection.
/// Providers are deliberately outside this policy: the local slice only
/// persists a schedule and projects a local notification when it becomes due.
/// </summary>
public static class ReminderPolicy
{
    public static ReminderScheduleResolution Resolve(
        string? configType,
        DateTimeOffset? exactAt,
        DateTimeOffset sourceStartAt,
        DateTimeOffset now)
    {
        var normalized = configType?.Trim();
        if (string.Equals(normalized, ReminderConfigurationTypes.None, StringComparison.Ordinal))
        {
            return exactAt is null
                ? Valid(null)
                : Invalid("ValidationFailed", "None reminders cannot include an exact time.");
        }

        if (string.Equals(normalized, ReminderConfigurationTypes.BeforeStart15m, StringComparison.Ordinal))
        {
            if (exactAt is not null)
            {
                return Invalid("ValidationFailed", "Preset reminders cannot include an exact time.");
            }

            var dueAt = sourceStartAt.AddMinutes(-15);
            return dueAt > now
                ? Valid(dueAt)
                : Invalid("ReminderPresetElapsed", "The preset reminder time has already passed.");
        }

        if (string.Equals(normalized, ReminderConfigurationTypes.Exact, StringComparison.Ordinal))
        {
            return exactAt is { } value && value > now
                ? Valid(value)
                : Invalid("ReminderTimeInvalid", "An exact reminder time must be in the future.");
        }

        return Invalid("ValidationFailed", "The reminder configuration type is not supported.");
    }

    private static ReminderScheduleResolution Valid(DateTimeOffset? dueAt) => new(true, null, null, dueAt);

    private static ReminderScheduleResolution Invalid(string code, string title) => new(false, code, title, null);
}
