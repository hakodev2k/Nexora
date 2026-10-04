using System.Globalization;
using System.Text;
using Nexora.Application.Transfer;

namespace Nexora.Infrastructure.Transfer;

public static class CalendarIcsSerializer
{
    public const int MaxEvents = 1000;
    public const int MaxBytes = 1048576;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    public sealed record Result(byte[]? Content, string? Reason);
    public static Result Serialize(IReadOnlyList<CalendarExportEvent> events, string zone, DateTimeOffset snapshot)
    {
        if (events.Count > MaxEvents) return new(null, "ExportLimitExceeded");
        try
        {
            var builder = new StringBuilder();
            void Line(string text)
            {
                // Count octets including the continuation space; never split a Unicode scalar.
                var width = 0;
                foreach (var rune in text.EnumerateRunes())
                {
                    if (width + rune.Utf8SequenceLength > 75) { builder.Append("\r\n "); width = 1; }
                    builder.Append(rune.ToString()); width += rune.Utf8SequenceLength;
                }
                builder.Append("\r\n");
            }
            Line("BEGIN:VCALENDAR"); Line("VERSION:2.0"); Line("PRODID:-//Nexora//Calendar export v1//EN"); Line("CALSCALE:GREGORIAN");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in events)
            {
                if (string.IsNullOrWhiteSpace(item.CalendarUid) || item.CalendarUid.Length > 255 || !seen.Add(item.CalendarUid) ||
                    string.IsNullOrWhiteSpace(item.Title) || item.Title.Length > 200 || item.Description?.Length > 20000 ||
                    item.SourceKind is not ("Manual" or "Task")) return new(null, "SourceCompatibilityUnavailable");
                if (item.StartDate is null && (item.StartAt.UtcTicks % TimeSpan.TicksPerSecond != 0 || item.EndAt.UtcTicks % TimeSpan.TicksPerSecond != 0))
                    return new(null, "SourcePrecisionUnsupported");
                if (item.EndAt <= item.StartAt || (item.StartDate is null) != (item.EndDateExclusive is null) ||
                    item.StartDate is { } start && item.EndDateExclusive <= start) return new(null, "SourceCompatibilityUnavailable");
                if (item.SourceKind == "Manual" ? item.Status is not ("Scheduled" or "Completed" or "Canceled") :
                    item.Status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped") || item.StartDate is not null ||
                    item.TaskPriority is not (null or "P0" or "P1" or "P2" or "P3") || string.IsNullOrWhiteSpace(item.ProjectTitle))
                    return new(null, "SourceCompatibilityUnavailable");
                Line("BEGIN:VEVENT"); Line("UID:" + Text(item.CalendarUid)); Line("DTSTAMP:" + Instant(snapshot));
                Line("SUMMARY:" + Text(item.Title)); if (item.Description is not null) Line("DESCRIPTION:" + Text(item.Description));
                if (item.StartDate is { } date)
                { Line("DTSTART;VALUE=DATE:" + date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)); Line("DTEND;VALUE=DATE:" + item.EndDateExclusive!.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)); }
                else { Line("DTSTART:" + Instant(item.StartAt)); Line("DTEND:" + Instant(item.EndAt)); }
                Line("X-NEXORA-SOURCE:" + item.SourceKind); Line("X-NEXORA-STATUS:" + item.Status); Line("X-NEXORA-TIMEZONE:" + Text(zone));
                if (item.SourceKind == "Manual") Line("STATUS:" + (item.Status == "Canceled" ? "CANCELLED" : "CONFIRMED"));
                else { if (item.TaskPriority is not null) Line("X-NEXORA-PRIORITY:" + item.TaskPriority); Line("X-NEXORA-PROJECT:" + Text(item.ProjectTitle!)); }
                Line("END:VEVENT");
                if (Utf8.GetByteCount(builder.ToString()) > MaxBytes) return new(null, "ExportLimitExceeded");
            }
            Line("END:VCALENDAR"); var bytes = Utf8.GetBytes(builder.ToString());
            return bytes.Length > MaxBytes ? new(null, "ExportLimitExceeded") : new(bytes, null);
        }
        catch (ArgumentException) { return new(null, "SourceCompatibilityUnavailable"); }
    }
    private static string Instant(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    private static string Text(string value)
    {
        _ = Utf8.GetByteCount(value); // Reject unpaired UTF16 before Rune enumeration can replace it.
        if (value.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'))) throw new ArgumentException("Unsupported text controls.");
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Replace("\n", "\\n", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal).Replace(",", "\\,", StringComparison.Ordinal);
    }
}
