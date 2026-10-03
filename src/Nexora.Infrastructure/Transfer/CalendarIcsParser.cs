using System.Globalization;
using System.Text;
using Nexora.Application.Transfer;

namespace Nexora.Infrastructure.Transfer;

/// <summary>Bounded, network-free RFC5545 input subset. Never executes external properties or reminders.</summary>
public static class CalendarIcsParser
{
    public const int MaxBytes = 1024 * 1024;
    public const int MaxEvents = 1000;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    public sealed record ParseResult(bool Succeeded, string? ReasonCode, IReadOnlyList<ImportRow> Rows);
    private sealed record Property(string Name, Dictionary<string, string> Parameters, string Value);
    private sealed class Entry
    {
        public Dictionary<string, Property> Values { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Warnings { get; } = new(StringComparer.Ordinal);
        public string? Error { get; set; }
    }
    public static ParseResult Parse(ReadOnlySpan<byte> input, string accountZone)
    {
        if (input.Length == 0 || input.Length > MaxBytes) return Fail("FileLimitExceeded");
        if (!TryZone(accountZone, out _)) return Fail("UnknownTimeZone");
        List<string> lines;
        try { lines = Unfold(input); }
        catch (DecoderFallbackException) { return Fail("InvalidUtf8"); }
        catch (FormatException) { return Fail("InvalidStructure"); }
        var stack = new Stack<string>(); var rows = new List<ImportRow>(); Entry? entry = null;
        var envelope = new Dictionary<string, string>(StringComparer.Ordinal); var started = false; var ended = false;
        foreach (var line in lines)
        {
            if (line.Length == 0) continue;
            if (line.StartsWith("BEGIN:", StringComparison.OrdinalIgnoreCase))
            {
                var name = line[6..].ToUpperInvariant();
                if (!Token(name) || stack.Count >= 8 || ended) return Fail("InvalidStructure");
                if (stack.Count == 0) { if (name != "VCALENDAR" || started) return Fail("InvalidStructure"); started = true; }
                else if (name == "VCALENDAR" || (name == "VEVENT" && stack.Count != 1)) return Fail("InvalidStructure");
                if (name == "VEVENT") { if (rows.Count >= MaxEvents) return Fail("EventLimitExceeded"); entry = new(); }
                else if (entry is not null) entry.Warnings.Add(name == "VALARM" ? "ReminderIgnored" : "UnsupportedComponentIgnored");
                stack.Push(name); continue;
            }
            if (line.StartsWith("END:", StringComparison.OrdinalIgnoreCase))
            {
                var name = line[4..].ToUpperInvariant();
                if (stack.Count == 0 || stack.Pop() != name) return Fail("InvalidStructure");
                if (name == "VEVENT") { rows.Add(Convert(entry!, rows.Count + 1, accountZone)); entry = null; }
                if (name == "VCALENDAR") ended = true;
                continue;
            }
            if (stack.Count == 0) return Fail("InvalidStructure");
            if (stack.Count > 2 || stack.Peek() is not ("VEVENT" or "VCALENDAR")) continue;
            if (!TryProperty(line, out var property))
            { if (entry is null) return Fail("InvalidStructure"); entry.Error ??= "InvalidProperty"; continue; }
            if (entry is null)
            {
                if (property.Name is "VERSION" or "PRODID")
                { if (!envelope.TryAdd(property.Name, property.Value)) return Fail("InvalidStructure"); }
                continue;
            }
            if (property.Name is "RRULE" or "RDATE" or "EXDATE" or "RECURRENCE-ID") { entry.Error = "RecurringUnsupported"; continue; }
            if (property.Name == "STATUS") { entry.Warnings.Add("SourceStatusIgnored"); continue; }
            if (property.Name is not ("SUMMARY" or "DESCRIPTION" or "UID" or "DTSTART" or "DTEND"))
            { entry.Warnings.Add("UnsupportedPropertyIgnored"); continue; }
            if (!entry.Values.TryAdd(property.Name, property)) entry.Error ??= "DuplicateProperty";
        }
        if (!started || !ended || stack.Count != 0 || envelope.GetValueOrDefault("VERSION") != "2.0" || string.IsNullOrWhiteSpace(envelope.GetValueOrDefault("PRODID"))) return Fail("InvalidStructure");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < rows.Count; index++)
            if (rows[index].Candidate is { } candidate && !seen.Add(candidate.Uid))
                rows[index] = rows[index] with { Outcome = "Duplicate", ReasonCode = "DuplicateUidInFile", Candidate = null };
        return new(true, null, rows);
    }
    private static ParseResult Fail(string code) => new(false, code, []);
    private static List<string> Unfold(ReadOnlySpan<byte> input)
    {
        if (input.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) input = input[3..];
        var lines = new List<string>(); var current = new List<byte>(); var physical = 0;
        var start = 0;
        for (var index = 0; index <= input.Length; index++)
        {
            if (index != input.Length && input[index] != 10) continue;
            if (++physical > 100000) throw new FormatException();
            var segment = input[start..index]; start = index + 1;
            if (segment.Length > 0 && segment[^1] == 13) segment = segment[..^1];
            if (segment.Contains((byte)13) || segment.Contains((byte)0)) throw new FormatException();
            foreach (var item in segment) if ((item < 32 && item != 9) || item == 127) throw new FormatException();
            var continuation = segment.Length > 0 && segment[0] is 32 or 9;
            if (continuation) { if (current.Count == 0) throw new FormatException(); segment = segment[1..]; }
            else if (current.Count > 0) { lines.Add(Utf8.GetString(current.ToArray())); current.Clear(); }
            if (current.Count + segment.Length > 65536) throw new FormatException();
            foreach (var item in segment) current.Add(item);
        }
        if (current.Count > 0) lines.Add(Utf8.GetString(current.ToArray()));
        return lines;
    }
    private static bool TryProperty(string line, out Property property)
    {
        property = null!; var quoted = false; var colon = -1;
        for (var i = 0; i < line.Length; i++) { if (line[i] == '"') quoted = !quoted; if (line[i] == ':' && !quoted) { colon = i; break; } }
        if (colon <= 0 || quoted) return false;
        var parts = new List<string>(); var start = 0;
        for (var i = 0; i <= colon; i++)
        {
            if (i < colon && line[i] == '"') quoted = !quoted;
            if (i == colon || (line[i] == ';' && !quoted)) { parts.Add(line[start..i]); start = i + 1; }
        }
        var name = parts[0].ToUpperInvariant(); if (!Token(name)) return false;
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in parts.Skip(1))
        {
            var equals = part.IndexOf('='); if (equals <= 0) return false;
            var key = part[..equals].ToUpperInvariant(); var value = part[(equals + 1)..];
            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2) value = value[1..^1];
            else if (value.Contains('"')) return false;
            if (!Token(key) || value.Length == 0 || !parameters.TryAdd(key, value)) return false;
        }
        property = new(name, parameters, line[(colon + 1)..]); return true;
    }
    private static bool Token(string text) => text.Length is > 0 and <= 100 && text.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
    private static ImportRow Convert(Entry entry, int number, string zone)
    {
        var warnings = entry.Warnings.Order(StringComparer.Ordinal).ToArray();
        ImportRow Invalid(string code) => new(number, "Invalid", code, warnings, null, null);
        if (entry.Error is not null) return Invalid(entry.Error);
        if (new[] { "SUMMARY", "DESCRIPTION", "DTSTART", "DTEND", "UID" }.Any(key => !entry.Values.ContainsKey(key))) return Invalid("RequiredFieldMissing");
        if (!Text(entry.Values["SUMMARY"], out var title) || !Text(entry.Values["DESCRIPTION"], out var description) || !Text(entry.Values["UID"], out var uid)) return Invalid("InvalidText");
        title = title.Trim(); description = description.Trim(); uid = uid.Trim();
        if (title.Length is < 1 or > 200 || description.Length is < 1 or > 20000 || uid.Length is < 1 or > 1024 || uid.Any(char.IsControl)) return Invalid("InvalidFieldLength");
        var start = entry.Values["DTSTART"]; var end = entry.Values["DTEND"];
        var allDay = start.Parameters.GetValueOrDefault("VALUE")?.ToUpperInvariant() == "DATE";
        if (allDay != (end.Parameters.GetValueOrDefault("VALUE")?.ToUpperInvariant() == "DATE")) return Invalid("MixedTimeRepresentation");
        DateOnly? first = null, last = null; DateTimeOffset startAt, endAt;
        if (allDay)
        {
            if (start.Parameters.Keys.Any(k => k != "VALUE") || end.Parameters.Keys.Any(k => k != "VALUE") ||
                !DateOnly.TryParseExact(start.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var s) ||
                !DateOnly.TryParseExact(end.Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var e) || e <= s) return Invalid("InvalidDateRange");
            first = s; last = e;
            startAt = new(s.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)); endAt = new(e.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }
        else
        {
            var startError = Instant(start, zone, out startAt); var endError = Instant(end, zone, out endAt);
            var error = startError ?? endError;
            if (error is not null) return Invalid(error);
            if (endAt <= startAt) return Invalid("InvalidTimeRange");
            if (!start.Parameters.ContainsKey("TZID") && !start.Value.EndsWith('Z') || !end.Parameters.ContainsKey("TZID") && !end.Value.EndsWith('Z')) entry.Warnings.Add("FloatingAccountTimeZone");
        }
        warnings = entry.Warnings.Order(StringComparer.Ordinal).ToArray();
        return new(number, "Valid", null, warnings, new(1, number, title, description, uid, zone, allDay, startAt, endAt, first, last), null);
    }
    private static bool Text(Property property, out string text)
    {
        text = ""; if (property.Parameters.Keys.Any(k => k is not ("VALUE" or "LANGUAGE")) || property.Parameters.TryGetValue("VALUE", out var type) && !type.Equals("TEXT", StringComparison.OrdinalIgnoreCase)) return false;
        var result = new StringBuilder(); var raw = property.Value;
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '\\') { if (raw[i] is ',' or ';') return false; result.Append(raw[i]); continue; }
            if (++i == raw.Length) return false;
            if (raw[i] is 'n' or 'N') result.Append('\n'); else if (raw[i] is '\\' or ',' or ';') result.Append(raw[i]); else return false;
        }
        text = result.ToString(); return true;
    }
    private static string? Instant(Property property, string accountZone, out DateTimeOffset instant)
    {
        instant = default;
        if (property.Parameters.Keys.Any(k => k is not ("TZID" or "VALUE")) || property.Parameters.TryGetValue("VALUE", out var value) && !value.Equals("DATE-TIME", StringComparison.OrdinalIgnoreCase)) return "InvalidTime";
        var utc = property.Value.EndsWith('Z');
        if (utc && property.Parameters.ContainsKey("TZID")) return "InvalidTime";
        if (!DateTime.TryParseExact(utc ? property.Value[..^1] : property.Value, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)) return "InvalidTime";
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (utc) { instant = new(DateTime.SpecifyKind(local, DateTimeKind.Utc)); return null; }
        if (!TryZone(property.Parameters.GetValueOrDefault("TZID") ?? accountZone, out var zone)) return "UnknownTimeZone";
        if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) return "UnsafeLocalTime";
        try { instant = new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime(); return null; }
        catch (ArgumentException) { return "InvalidTime"; }
    }
    public static bool TryZone(string id, out TimeZoneInfo zone)
    {
        zone = null!; if (string.IsNullOrWhiteSpace(id) || id.Length > 128) return false;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { return false; }
    }
}
