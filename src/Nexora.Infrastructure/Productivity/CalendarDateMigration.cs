using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Productivity;

// Source-owned migration preflight; normal operator and SQL tests share this exact upgrade path.
internal static class CalendarDateMigration
{
    private static DateTimeOffset LosslessLocal(DateTime utc,TimeZoneInfo zone)
    {
        var instant=new DateTimeOffset(DateTime.SpecifyKind(utc,DateTimeKind.Utc));
        var ticks=instant.UtcTicks+zone.GetUtcOffset(instant).Ticks;
        if(ticks<DateTime.MinValue.Ticks||ticks>DateTime.MaxValue.Ticks)throw new InvalidOperationException("Legacy all-day local date cannot be represented without coercion.");
        var local=TimeZoneInfo.ConvertTime(instant,zone);
        if(local.Ticks!=ticks)throw new InvalidOperationException("Legacy all-day local date cannot be represented without coercion.");
        return local;
    }
    internal static async Task PrepareAsync(SqlConnection connection,CancellationToken token)
    {
        using(var create=new SqlCommand("IF OBJECT_ID('tempdb..#NexoraCalendarDateMap') IS NOT NULL DROP TABLE #NexoraCalendarDateMap; CREATE TABLE #NexoraCalendarDateMap(Id uniqueidentifier PRIMARY KEY,SourceRevision binary(8) NOT NULL,StartDate date NOT NULL,EndDateExclusive date NOT NULL);",connection))await create.ExecuteNonQueryAsync(token);
        bool dates;
        using(var probe=new SqlCommand("SELECT CASE WHEN COL_LENGTH('[calendar].[Event]','StartDate') IS NULL THEN 0 ELSE 1 END",connection))dates=Convert.ToInt32(await probe.ExecuteScalarAsync(token))==1;
        var table=new DataTable();table.Columns.Add("Id",typeof(Guid));table.Columns.Add("SourceRevision",typeof(byte[]));table.Columns.Add("StartDate",typeof(DateTime));table.Columns.Add("EndDateExclusive",typeof(DateTime));
        using(var read=new SqlCommand($"SELECT Id,RowVersion,StartAt,EndAt,TimeZoneId,{(dates?"StartDate,EndDateExclusive":"CAST(NULL AS date),CAST(NULL AS date)")} FROM [calendar].[Event] WHERE IsAllDay=1",connection))
        using(var reader=await read.ExecuteReaderAsync(token))
        {
            while(await reader.ReadAsync(token))
            {
                DateTime start,end;
                if(!reader.IsDBNull(5)&&!reader.IsDBNull(6)){start=reader.GetDateTime(5);end=reader.GetDateTime(6);}
                else
                {
                    if(!CalendarIcsParser.TryZone(reader.GetString(4),out var zone))throw new InvalidOperationException("Legacy Calendar timezone must be corrected before the date migration.");
                    var s=LosslessLocal(reader.GetDateTime(2),zone);
                    var e=LosslessLocal(reader.GetDateTime(3),zone);
                    if(s.TimeOfDay!=TimeSpan.Zero||e.TimeOfDay!=TimeSpan.Zero)throw new InvalidOperationException("Legacy all-day Calendar boundaries must be corrected before migration.");
                    start=s.Date;end=e.Date;
                }
                if(end<=start)throw new InvalidOperationException("Legacy all-day Calendar date range is invalid.");
                table.Rows.Add(reader.GetGuid(0),reader.GetFieldValue<byte[]>(1),start,end);
            }
        }
        using var bulk=new SqlBulkCopy(connection){DestinationTableName="#NexoraCalendarDateMap"};await bulk.WriteToServerAsync(table,token);
    }
}
