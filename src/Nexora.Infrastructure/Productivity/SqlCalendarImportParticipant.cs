using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Productivity;

internal sealed record CalendarImportCohort(string Digest,IReadOnlyList<CalendarImportCandidate> Intervals,bool Complete);
// Calendar and Tasks share the existing Productivity source boundary. Transfer never queries their tables.
internal sealed class SqlCalendarImportParticipant(SqlImportAuthority authority)
{
    internal string? UidState(SqlImportUnit unit,string uid)
    {
        using var command=unit.Command("SELECT Uid FROM [calendar].[ImportedUid] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND UidDigest=@Digest;");
        SqlImportUnit.Add(command,"@Digest",SqlDbType.Binary,Digest(uid),32);
        return command.ExecuteScalar() is string existing?existing==uid?"DuplicateUid":"UidDigestConflict":null;
    }
    internal CalendarImportCohort? Cohort(SqlImportUnit unit,string accountZone)
    {
        var manual=authority.Allowed(unit,"FX13","calendar.event.read");var tasks=manual&&authority.Allowed(unit,"FX12","tasks.task.read");
        using var command=unit.Command("""
            SELECT TOP(10001) e.Id,e.RowVersion,e.StartAt,e.EndAt,e.IsAllDay,e.StartDate,e.EndDateExclusive,t.RowVersion
            FROM [calendar].[Event] e LEFT JOIN [productivity].[Task] t ON t.OwnerId=e.OwnerId AND t.Id=e.TaskId
            WHERE e.OwnerId=@Owner AND e.Status<>'Deleted' AND @Manual=1
              AND (e.SourceKind='Manual' OR (@Tasks=1 AND t.Id IS NOT NULL AND t.Status<>'Deleted')) ORDER BY e.Id;
            """);
        SqlImportUnit.Add(command,"@Manual",SqlDbType.Bit,manual);SqlImportUnit.Add(command,"@Tasks",SqlDbType.Bit,tasks);
        var records=new List<CalendarImportCandidate>();var digest=new StringBuilder($"{manual}:{tasks}:");
        using var reader=command.ExecuteReader();
        while(reader.Read())
        {
            if(records.Count==10000)return null;
            digest.Append(reader.GetGuid(0).ToString("N")).Append(':').Append(Convert.ToHexString(reader.GetFieldValue<byte[]>(1))).Append(':').Append(reader.IsDBNull(7)?"-":Convert.ToHexString(reader.GetFieldValue<byte[]>(7))).Append(';');
            records.Add(new(1,0,"","","",accountZone,reader.GetBoolean(4),Utc(reader.GetDateTime(2)),Utc(reader.GetDateTime(3)),reader.IsDBNull(5)?null:DateOnly.FromDateTime(reader.GetDateTime(5)),reader.IsDBNull(6)?null:DateOnly.FromDateTime(reader.GetDateTime(6))));
        }
        return new(Convert.ToHexString(Digest(digest.ToString())),records,manual&&tasks);
    }
    internal IReadOnlyList<ImportRow> Preview(SqlImportUnit unit,IReadOnlyList<ImportRow> parsed,CalendarImportCohort cohort)
    {
        var result=new List<ImportRow>();var accepted=new List<CalendarImportCandidate>();
        foreach(var row in parsed)
        {
            if(row.Candidate is not { } candidate){result.Add(row);continue;}
            var state=UidState(unit,candidate.Uid);
            if(state=="UidDigestConflict")throw new InvalidOperationException("UID digest conflict.");
            if(state is not null){result.Add(row with{Outcome="Duplicate",ReasonCode=state,Candidate=null});continue;}
            var warnings=row.Warnings.ToList();if(!cohort.Complete)warnings.Add("OverlapCheckUnavailable");
            if(cohort.Intervals.Concat(accepted).Any(existing=>Overlaps(existing,candidate)))warnings.Add("OverlapDetected");
            result.Add(row with{Warnings=warnings.Distinct().Order(StringComparer.Ordinal).ToArray()});accepted.Add(candidate);
        }
        return result;
    }
    internal (Guid? Resource,string? Reason) Apply(SqlImportUnit unit,Guid batch,CalendarImportCandidate candidate)
    {
        var duplicate=UidState(unit,candidate.Uid);if(duplicate is not null)return (null,duplicate);
        var resource=authority.RegisterEvent(unit,Guid.NewGuid());
        using var command=unit.Command("""
            INSERT [calendar].[Event](Id,OwnerId,Title,Description,StartAt,EndAt,TimeZoneId,IsAllDay,SourceKind,Status,StartDate,EndDateExclusive,CalendarUid)
            VALUES(@Id,@Owner,@Title,@Description,@Start,@End,@Zone,@AllDay,'Manual','Scheduled',@StartDate,@EndDate,@CalendarUid);
            INSERT [calendar].[ImportedUid](Id,OwnerId,Uid,UidDigest,ManualEventId,ImportBatchId,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            VALUES(NEWID(),@Owner,@Uid,@Digest,@Id,@Batch,@Now,@Now,@User,@User);
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,resource);SqlImportUnit.Add(command,"@Batch",SqlDbType.UniqueIdentifier,batch);
        SqlImportUnit.Add(command,"@Title",SqlDbType.NVarChar,candidate.Title,200);SqlImportUnit.Add(command,"@Description",SqlDbType.NVarChar,candidate.Description,20000);
        SqlImportUnit.Add(command,"@Start",SqlDbType.DateTime2,candidate.StartAt.UtcDateTime);SqlImportUnit.Add(command,"@End",SqlDbType.DateTime2,candidate.EndAt.UtcDateTime);
        SqlImportUnit.Add(command,"@Zone",SqlDbType.NVarChar,candidate.TimeZoneId,128);SqlImportUnit.Add(command,"@AllDay",SqlDbType.Bit,candidate.IsAllDay);
        SqlImportUnit.Add(command,"@StartDate",SqlDbType.Date,candidate.StartDate?.ToDateTime(TimeOnly.MinValue));SqlImportUnit.Add(command,"@EndDate",SqlDbType.Date,candidate.EndDateExclusive?.ToDateTime(TimeOnly.MinValue));
        SqlImportUnit.Add(command,"@Uid",SqlDbType.NVarChar,candidate.Uid,1024);SqlImportUnit.Add(command,"@Digest",SqlDbType.Binary,Digest(candidate.Uid),32);
        SqlImportUnit.Add(command,"@CalendarUid",SqlDbType.NVarChar,Guid.NewGuid().ToString("N")+"@nexora.local",255);command.ExecuteNonQuery();return(resource,null);
    }
    private static bool Overlaps(CalendarImportCandidate a,CalendarImportCandidate b)
    {
        if(!a.IsAllDay&&!b.IsAllDay)return a.StartAt<b.EndAt&&a.EndAt>b.StartAt;
        if(!CalendarIcsParser.TryZone(a.TimeZoneId,out var zone))return false;
        var aStart=a.IsAllDay?a.StartDate!.Value.ToDateTime(TimeOnly.MinValue):TimeZoneInfo.ConvertTime(a.StartAt,zone).DateTime;
        var aEnd=a.IsAllDay?a.EndDateExclusive!.Value.ToDateTime(TimeOnly.MinValue):TimeZoneInfo.ConvertTime(a.EndAt,zone).DateTime;
        var bStart=b.IsAllDay?b.StartDate!.Value.ToDateTime(TimeOnly.MinValue):TimeZoneInfo.ConvertTime(b.StartAt,zone).DateTime;
        var bEnd=b.IsAllDay?b.EndDateExclusive!.Value.ToDateTime(TimeOnly.MinValue):TimeZoneInfo.ConvertTime(b.EndAt,zone).DateTime;
        return aStart<bEnd&&aEnd>bStart;
    }
    private static byte[] Digest(string text)=>SHA256.HashData(Encoding.UTF8.GetBytes(text));
    private static DateTimeOffset Utc(DateTime value)=>new(DateTime.SpecifyKind(value,DateTimeKind.Utc));
}
