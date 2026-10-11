using Nexora.Application.Identity;

namespace Nexora.Application.Automation;

public sealed record AutomationScheduleCommand(string Frequency, string TimeZoneId, int Hour, int Minute,
    int Weekdays, DateTimeOffset StartAt, DateTimeOffset? EndAt, string MissedPolicy = "Skip", bool Enabled = false);
public sealed record AutomationScheduleOccurrence(DateTimeOffset DueAt, long ScheduleVersion, string State, string Code, Guid? RunId);
public sealed record AutomationScheduleSnapshot(Guid DefinitionId, long DefinitionVersion, long ScheduleVersion,
    AutomationScheduleCommand? Configuration, IReadOnlyList<DateTimeOffset> NextRuns, string ETag,
    bool BindingStale, bool ExecutionAvailable = false, IReadOnlyList<AutomationScheduleOccurrence>? RecentOccurrences = null);
public sealed record AutomationSchedulePlan(bool Accepted, string Code, IReadOnlyList<DateTimeOffset> NextRuns);

// Pure calendar calculation. No clock, queue, network or business effects hidden in preview.
public static class AutomationScheduleContract
{
    public static AutomationSchedulePlan Preview(AutomationScheduleCommand input, DateTimeOffset after)
    {
        if (input.Frequency is not ("Daily" or "Weekly") || input.Hour is < 0 or > 23 || input.Minute is < 0 or > 59 ||
            input.Weekdays is < 1 or > 127 || (input.Frequency == "Daily" && input.Weekdays != 127) || input.MissedPolicy != "Skip" ||
            input.StartAt.Year is < 2 or > 9997 || input.EndAt is { } end && (end < input.StartAt || end.Year > 9998) ||
            string.IsNullOrEmpty(input.TimeZoneId) || input.TimeZoneId.Length > 100 || input.TimeZoneId != input.TimeZoneId.Trim())
            return new(false, "ScheduleInvalid", []);
        try
        {
            if (input.TimeZoneId != "UTC" && !TimeZoneInfo.TryConvertIanaIdToWindowsId(input.TimeZoneId, out _))
                return new(false, "TimeZoneUnsupported", []);
            var zone = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZoneId);
            var boundary = after > input.StartAt ? after : input.StartAt;
            var date = TimeZoneInfo.ConvertTime(boundary, zone).Date;
            var runs = new List<DateTimeOffset>();
            for (var days = 0; days < 366 && runs.Count < 5; days++, date = date.AddDays(1))
            {
                if ((input.Weekdays & (1 << (int)date.DayOfWeek)) == 0) continue;
                var local = DateTime.SpecifyKind(date.AddHours(input.Hour).AddMinutes(input.Minute), DateTimeKind.Unspecified);
                if (zone.IsInvalidTime(local)) continue; // Missing spring-forward occurrence is skipped.
                // Choose the earlier UTC instant for repeated local time; never emit the second fold.
                var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
                var instant = new DateTimeOffset(local, offset).ToUniversalTime();
                if (instant <= after || instant < input.StartAt) continue;
                if (input.EndAt is { } until && instant >= until) break;
                runs.Add(instant);
            }
            return new(true, runs.Count == 0 ? "NoFutureOccurrence" : "ScheduleAccepted", runs);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        { return new(false, "TimeZoneUnsupported", []); }
    }
}
