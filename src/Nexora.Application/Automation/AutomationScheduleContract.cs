using Nexora.Application.Identity;
using System.Text.Json.Serialization;

namespace Nexora.Application.Automation;

public sealed record AutomationScheduleCommand(string Frequency, string TimeZoneId, int Hour, int Minute,
    int Weekdays, DateTimeOffset StartAt, DateTimeOffset? EndAt, string MissedPolicy = "Skip", bool Enabled = false,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CronExpression = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? IntervalMinutes = null);
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
        if (input.Frequency is not ("Daily" or "Weekly" or "Cron" or "Interval") || input.Hour is < 0 or > 23 || input.Minute is < 0 or > 59 ||
            input.Weekdays is < 1 or > 127 || (input.Frequency == "Daily" && input.Weekdays != 127) || input.MissedPolicy != "Skip" ||
            input.StartAt.Year is < 2 or > 9997 || input.EndAt is { } end && (end < input.StartAt || end.Year > 9998) ||
            string.IsNullOrEmpty(input.TimeZoneId) || input.TimeZoneId.Length > 100 || input.TimeZoneId != input.TimeZoneId.Trim())
            return new(false, "ScheduleInvalid", []);
        AutomationCronExpression? cron = null;
        if (input.Frequency == "Cron")
        {
            if (input.IntervalMinutes is not null || !AutomationCronExpression.TryParse(input.CronExpression, out cron))
                return new(false, "CronInvalid", []);
        }
        else if (input.CronExpression is not null) return new(false, "ScheduleInvalid", []);
        if (input.Frequency == "Interval")
        {
            if (input.IntervalMinutes is not (>= 1 and <= 1440)) return new(false, "IntervalInvalid", []);
        }
        else if (input.IntervalMinutes is not null) return new(false, "ScheduleInvalid", []);
        try
        {
            if (input.TimeZoneId != "UTC" && !TimeZoneInfo.TryConvertIanaIdToWindowsId(input.TimeZoneId, out _))
                return new(false, "TimeZoneUnsupported", []);
            var zone = TimeZoneInfo.FindSystemTimeZoneById(input.TimeZoneId);
            if (after.UtcDateTime.Year > 9998) return new(true, "NoFutureOccurrence", []);
            // Interval is elapsed UTC time, anchored to StartAt, never reset by restart/DST.
            if (input.Frequency == "Interval") return Interval(input, after);
            var boundary = after > input.StartAt ? after : input.StartAt;
            var date = TimeZoneInfo.ConvertTime(boundary, zone).Date;
            var runs = new List<DateTimeOffset>();
            // A bounded 32-year Cron horizon covers five ordinary leap-day occurrences.
            // Date predicates run before minute enumeration, including impossible dates.
            for (var days = 0; days < (cron is null ? 366 : 366 * 32) && runs.Count < 5; days++, date = date.AddDays(1))
            {
                if (date.Year > 9998 || input.EndAt is { } endDate && date > TimeZoneInfo.ConvertTime(endDate, zone).Date) break;
                if (cron is not null ? !cron.MatchesDate(date) : (input.Weekdays & (1 << (int)date.DayOfWeek)) == 0) continue;
                foreach (var hour in cron?.Hours ?? [input.Hour])
                foreach (var minute in cron?.Minutes ?? [input.Minute])
                {
                    var local = DateTime.SpecifyKind(date.AddHours(hour).AddMinutes(minute), DateTimeKind.Unspecified);
                    if (zone.IsInvalidTime(local)) continue; // Missing spring-forward occurrence is skipped.
                    // Choose the earlier UTC instant for repeated local time; never emit the second fold.
                    var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
                    var instant = new DateTimeOffset(local, offset).ToUniversalTime();
                    if (instant <= after || instant < input.StartAt || instant.UtcDateTime.Year > 9998) continue;
                    if (input.EndAt is { } until && instant >= until) continue;
                    runs.Add(instant);
                    if (runs.Count == 5) return new(true, "ScheduleAccepted", runs);
                }
            }
            return new(true, runs.Count == 0 ? "NoFutureOccurrence" : "ScheduleAccepted", runs);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        { return new(false, "TimeZoneUnsupported", []); }
        catch (ArgumentException) { return new(false, "ScheduleInvalid", []); }
    }
    private static AutomationSchedulePlan Interval(AutomationScheduleCommand input, DateTimeOffset after)
    {
        var anchor = input.StartAt.UtcTicks;
        var period = input.IntervalMinutes!.Value * TimeSpan.TicksPerMinute;
        var occurrence = after.UtcTicks >= anchor ? (after.UtcTicks - anchor) / period + 1 : 0;
        var runs = new List<DateTimeOffset>();
        for (var index = 0; index < 5; index++, occurrence++)
        {
            if (occurrence > (DateTime.MaxValue.Ticks - anchor) / period) break;
            var instant = new DateTimeOffset(anchor + occurrence * period, TimeSpan.Zero);
            if (instant.Year > 9998 || input.EndAt is { } until && instant >= until) break;
            runs.Add(instant);
        }
        return new(true, runs.Count == 0 ? "NoFutureOccurrence" : "ScheduleAccepted", runs);
    }
}
