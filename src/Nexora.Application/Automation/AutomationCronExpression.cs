using System.Globalization;

namespace Nexora.Application.Automation;

// Closed numeric five-field dialect: minute hour day-of-month month day-of-week.
// No seconds/year, names, macros, L/W/#, code or provider interpretation.
internal sealed class AutomationCronExpression(int[] minutes, int[] hours, int[] days, int[] months, int[] weekdays)
{
    internal int[] Minutes { get; } = minutes;
    internal int[] Hours { get; } = hours;
    private readonly bool anyDay = days.Length == 31;
    private readonly bool anyWeekday = Enumerable.Range(0, 7).All(day => weekdays.Contains(day) || (day == 0 && weekdays.Contains(7)));
    internal bool MatchesDate(DateTime date)
    {
        if (!months.Contains(date.Month)) return false;
        var day = days.Contains(date.Day);
        var weekday = weekdays.Contains((int)date.DayOfWeek) || (date.DayOfWeek == DayOfWeek.Sunday && weekdays.Contains(7));
        // Restricted DOM and DOW use OR; an unrestricted field leaves the other field decisive.
        return anyDay ? weekday : anyWeekday ? day : day || weekday;
    }
    internal static bool TryParse(string? expression, out AutomationCronExpression? parsed)
    {
        parsed = null;
        if (string.IsNullOrEmpty(expression) || expression.Length > 256 || expression != expression.Trim() ||
            expression.Any(ch => ch is not (>= '0' and <= '9') && ch is not (' ' or '*' or ',' or '-' or '/'))) return false;
        var fields = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 5 || !Field(fields[0], 0, 59, out var minutes) || !Field(fields[1], 0, 23, out var hours) ||
            !Field(fields[2], 1, 31, out var days) || !Field(fields[3], 1, 12, out var months) || !Field(fields[4], 0, 7, out var weekdays)) return false;
        parsed = new(minutes, hours, days, months, weekdays); return true;
    }
    private static bool Field(string text, int minimum, int maximum, out int[] values)
    {
        values = []; var selected = new SortedSet<int>();
        foreach (var component in text.Split(','))
        {
            var pieces = component.Split('/'); if (pieces.Length > 2 || pieces[0].Length == 0) return false;
            var increment = 1;
            if (pieces.Length == 2 && (!Number(pieces[1], out increment) || increment < 1 || increment > maximum - minimum + 1)) return false;
            var first = minimum; var last = maximum;
            if (pieces[0] != "*")
            {
                var range = pieces[0].Split('-');
                if (range.Length > 2 || !Number(range[0], out first) || first < minimum || first > maximum) return false;
                last = first;
                if (range.Length == 2 && (!Number(range[1], out last) || last < first || last > maximum)) return false;
                if (range.Length == 1 && pieces.Length == 2) return false; // /step applies only to * or a range.
            }
            for (var value = first; value <= last; value += increment) selected.Add(value);
        }
        if (selected.Count == 0) return false;
        values = selected.ToArray(); return true;
    }
    private static bool Number(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
