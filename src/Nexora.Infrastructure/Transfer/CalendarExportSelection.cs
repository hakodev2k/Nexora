using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Application.Transfer;

namespace Nexora.Infrastructure.Transfer;

internal static class CalendarExportSelection
{
    internal static CalendarExportFilter? Normalize(CalendarExportFilter? filter)
    {
        if (filter is null || filter.SchemaVersion != 1 || filter.ContainmentMode != "FullyContained" ||
            !Choices(filter.SourceKinds, ["Manual", "Task"]) || filter.SourceKinds.Length == 0 ||
            !Choices(filter.ManualStatuses, ["Scheduled", "Completed", "Canceled"]) ||
            !Choices(filter.TaskStatuses, ["NotStarted", "InProgress", "Completed", "Skipped"]) ||
            filter.SourceKinds.Contains("Manual") != (filter.ManualStatuses.Length > 0) ||
            filter.SourceKinds.Contains("Task") != (filter.TaskStatuses.Length > 0) || filter.Range is { } range && range.End <= range.Start) return null;
        return filter with { SourceKinds = filter.SourceKinds.Order(StringComparer.Ordinal).ToArray(),
            ManualStatuses = filter.ManualStatuses.Order(StringComparer.Ordinal).ToArray(), TaskStatuses = filter.TaskStatuses.Order(StringComparer.Ordinal).ToArray() };
    }
    private static bool Choices(string[]? values, string[] allowed) => values is not null && values.Length <= allowed.Length &&
        values.Distinct(StringComparer.Ordinal).Count() == values.Length && values.All(value => allowed.Contains(value, StringComparer.Ordinal));
    internal static string Digest(CalendarExportFilter filter) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(filter, SqlImportBatchStore.Json))));
}
