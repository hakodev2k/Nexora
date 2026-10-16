using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Transfer;
using NodaTime;

namespace Nexora.Infrastructure.Productivity;

internal sealed record ExportSourceIdentity(Guid EventId, string SourceKind, Guid? TaskId, Guid? ProjectId);
internal sealed record CalendarExportSnapshot(string Digest, IReadOnlyList<CalendarExportEvent> Events, IReadOnlyList<ExportSourceIdentity> Sources, string? Error);
// Maps only the shared Calendar/Productivity boundary. Operations supplies the transaction, never source SQL.
internal sealed class SqlCalendarExportParticipant
{
    internal CalendarExportSnapshot Snapshot(SqlImportUnit unit, CalendarExportFilter filter, string zoneId)
    {
        void SelectionParameters(Microsoft.Data.SqlClient.SqlCommand sql)
        {
            SqlImportUnit.Add(sql, "@Manual", SqlDbType.Bit, filter.SourceKinds.Contains("Manual"));
            SqlImportUnit.Add(sql, "@Task", SqlDbType.Bit, filter.SourceKinds.Contains("Task"));
            SqlImportUnit.Add(sql, "@ManualStatuses", SqlDbType.NVarChar, JsonSerializer.Serialize(filter.ManualStatuses), 512);
            SqlImportUnit.Add(sql, "@TaskStatuses", SqlDbType.NVarChar, JsonSerializer.Serialize(filter.TaskStatuses), 512);
        }
        DateTimeOffset? lower = null, upper = null;
        if (filter.Range is { } range)
        {
            // Read only existence metadata before deciding if date-only selection needs UTC bounds.
            using var timed = unit.Command("""
                SELECT CASE WHEN EXISTS(SELECT 1 FROM [calendar].[Event] e WHERE e.OwnerId=@Owner AND @Manual=1
                  AND e.SourceKind='Manual' AND e.IsAllDay=0 AND e.Status IN(SELECT value FROM OPENJSON(@ManualStatuses)))
                  OR EXISTS(SELECT 1 FROM [productivity].[Task] t JOIN [productivity].[Project] p ON p.OwnerId=t.OwnerId AND p.Id=t.ProjectId
                    WHERE t.OwnerId=@Owner AND @Task=1 AND p.Status<>'Deleted' AND t.Status IN(SELECT value FROM OPENJSON(@TaskStatuses)))
                  THEN 1 ELSE 0 END;
                """);
            SelectionParameters(timed);
            if ((int)timed.ExecuteScalar()! == 1)
            {
                if (!Boundary(range.Start, zoneId, out var first) || !Boundary(range.End, zoneId, out var last))
                    return new("", [], [], "UnsafeRange");
                lower = first; upper = last;
            }
        }
        using var command = unit.Command("""
            WITH selected AS(
              SELECT e.Id,e.CalendarUid,e.SourceKind,e.Title,CASE WHEN DATALENGTH(e.Description)>40000 THEN NULL ELSE e.Description END Description,e.Status,e.StartAt,e.EndAt,e.StartDate,e.EndDateExclusive,
                CAST(NULL AS varchar(2)) Priority,CAST(NULL AS nvarchar(200)) ProjectName,CAST(NULL AS uniqueidentifier) TaskId,CAST(NULL AS uniqueidentifier) ProjectId,
                e.RowVersion EventRevision,CAST(NULL AS binary(8)) TaskRevision,CAST(NULL AS binary(8)) ProjectRevision,e.IsAllDay,
                CAST(CASE WHEN DATALENGTH(e.Description)>40000 THEN 1 ELSE 0 END AS bit) BodyOversized
              FROM [calendar].[Event] e WHERE e.OwnerId=@Owner AND e.SourceKind='Manual' AND @Manual=1
                AND e.Status IN(SELECT value FROM OPENJSON(@ManualStatuses))
              UNION ALL
              SELECT COALESCE(e.Id,t.Id),e.CalendarUid,'Task',t.Title,CASE WHEN DATALENGTH(t.Description)>40000 THEN NULL ELSE t.Description END,t.Status,t.StartAt,t.EndAt,NULL,NULL,
                t.Priority,p.Name,t.Id,p.Id,e.RowVersion,t.RowVersion,p.RowVersion,CAST(0 AS bit),CAST(CASE WHEN DATALENGTH(t.Description)>40000 THEN 1 ELSE 0 END AS bit)
              FROM [productivity].[Task] t JOIN [productivity].[Project] p ON p.OwnerId=t.OwnerId AND p.Id=t.ProjectId
              LEFT JOIN [calendar].[Event] e ON e.OwnerId=t.OwnerId AND e.TaskId=t.Id AND e.SourceKind='Task'
              WHERE t.OwnerId=@Owner AND @Task=1 AND p.Status<>'Deleted' AND t.Status IN(SELECT value FROM OPENJSON(@TaskStatuses)))
            SELECT TOP(1001) * FROM selected
            WHERE @HasRange=0 OR (IsAllDay=1 AND StartDate>=@RangeStart AND EndDateExclusive<=@RangeEnd)
              OR (IsAllDay=0 AND StartAt>=@Lower AND EndAt<=@Upper) ORDER BY Id;
            """);
        SelectionParameters(command);
        SqlImportUnit.Add(command, "@HasRange", SqlDbType.Bit, filter.Range is not null);
        SqlImportUnit.Add(command, "@RangeStart", SqlDbType.Date, filter.Range?.Start.ToDateTime(TimeOnly.MinValue));
        SqlImportUnit.Add(command, "@RangeEnd", SqlDbType.Date, filter.Range?.End.ToDateTime(TimeOnly.MinValue));
        SqlImportUnit.Add(command, "@Lower", SqlDbType.DateTime2, lower?.UtcDateTime);
        SqlImportUnit.Add(command, "@Upper", SqlDbType.DateTime2, upper?.UtcDateTime);
        var candidates = new List<(CalendarExportEvent Event, ExportSourceIdentity Source, string Revision)>();
        var selectedTextBytes = 0;
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (candidates.Count == CalendarIcsSerializer.MaxEvents) return new("", [], [], "ExportLimitExceeded");
                if (reader.GetBoolean(18)) return new("", [], [], "SourceCompatibilityUnavailable");
                var source = reader.GetString(2);
                var start = reader.IsDBNull(8) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(8));
                var end = reader.IsDBNull(9) ? (DateOnly?)null : DateOnly.FromDateTime(reader.GetDateTime(9));
                if (reader.GetBoolean(17) != (start is not null) || source == "Task" && start is not null)
                    return new("", [], [], "SourceCompatibilityUnavailable");
                var business = new CalendarExportEvent(reader.IsDBNull(1) ? "" : reader.GetString(1), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
                    source, reader.GetString(5), Utc(reader.GetDateTime(6)), Utc(reader.GetDateTime(7)), start, end,
                    source == "Task" && !reader.IsDBNull(10) ? reader.GetString(10) : null, source == "Task" ? reader.GetString(11) : null);
                selectedTextBytes += System.Text.Encoding.UTF8.GetByteCount(business.Title) + System.Text.Encoding.UTF8.GetByteCount(business.Description ?? "") +
                    System.Text.Encoding.UTF8.GetByteCount(business.CalendarUid) + System.Text.Encoding.UTF8.GetByteCount(business.ProjectTitle ?? "");
                if (selectedTextBytes > CalendarIcsSerializer.MaxBytes) return new("", [], [], "ExportLimitExceeded");
                var identity = new ExportSourceIdentity(reader.GetGuid(0), source, source == "Task" ? reader.GetGuid(12) : null, source == "Task" ? reader.GetGuid(13) : null);
                var revision = (reader.IsDBNull(14) ? "missing" : Convert.ToHexString(reader.GetFieldValue<byte[]>(14))) + ":" +
                    (source == "Task" ? Convert.ToHexString(reader.GetFieldValue<byte[]>(15)) + ":" + Convert.ToHexString(reader.GetFieldValue<byte[]>(16)) : "-");
                candidates.Add((business, identity, revision));
            }
        }
        var selected = candidates.ToArray();
        if (selected.Any(c => string.IsNullOrWhiteSpace(c.Event.CalendarUid))) return new("", [], [], "SourceCompatibilityUnavailable");
        var canonical = JsonSerializer.Serialize(new { zoneId, filter, rows = selected.Select(c => new { c.Event, c.Source, c.Revision }) }, SqlImportBatchStore.Json);
        return new(Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical))), selected.Select(c => c.Event).ToArray(), selected.Select(c => c.Source).ToArray(), null);
    }
    internal bool Available(SqlImportUnit unit, IReadOnlyList<ExportSourceIdentity> sources)
    {
        if (sources.Count > CalendarIcsSerializer.MaxEvents) return false;
        using var command = unit.Command("""
            SELECT COUNT(*) FROM OPENJSON(@Sources) WITH(eventId uniqueidentifier,sourceKind varchar(16),taskId uniqueidentifier,projectId uniqueidentifier) s
            JOIN [calendar].[Event] e ON e.OwnerId=@Owner AND e.Id=s.eventId AND e.SourceKind=s.sourceKind
            LEFT JOIN [productivity].[Task] t ON t.OwnerId=@Owner AND t.Id=e.TaskId
            LEFT JOIN [productivity].[Project] p ON p.OwnerId=@Owner AND p.Id=t.ProjectId
            WHERE (s.sourceKind='Manual' AND e.Status IN('Scheduled','Completed','Canceled')) OR
             (s.sourceKind='Task' AND t.Id=s.taskId AND p.Id=s.projectId AND t.Status IN('NotStarted','InProgress','Completed','Skipped') AND p.Status<>'Deleted');
            """);
        SqlImportUnit.Add(command, "@Sources", SqlDbType.NVarChar, JsonSerializer.Serialize(sources, SqlImportBatchStore.Json), -1);
        return (int)command.ExecuteScalar()! == sources.Count;
    }
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static bool Boundary(DateOnly date, string zoneId, out DateTimeOffset instant)
    {
        instant = default;
        try
        {
            var zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zoneId);
            if (zone is null && TimeZoneInfo.TryConvertWindowsIdToIanaId(zoneId, out var iana)) zone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(iana);
            if (zone is null) return false;
            var mapping = zone.MapLocal(new LocalDate(date.Year, date.Month, date.Day).AtMidnight());
            if (mapping.Count != 1) return false; // No earlier/later branch or forward shift.
            instant = mapping.Single().ToInstant().ToDateTimeOffset(); return true;
        }
        catch (ArgumentException) { return false; }
        catch (OverflowException) { return false; }
        catch (InvalidOperationException) { return false; } // NodaTime Instant outside DateTimeOffset's year1..9999 range.
    }
}
