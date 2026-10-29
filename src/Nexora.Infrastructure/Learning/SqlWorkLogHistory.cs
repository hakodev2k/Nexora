using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Infrastructure.Learning;

public sealed partial class SqlWorkLogService
{
    public IdentityOperationResult<WorkLogHistoryPage> History(IdentityPrincipal actor, Guid id, string? cursor)
    {
        var scope = new WorkLogHistoryCursor.Scope(actor.UserId,actor.OwnerId,id);
        var before = string.IsNullOrEmpty(cursor) ? null : historyCursors.Decode(scope,cursor);
        if (!string.IsNullOrEmpty(cursor) && before is null) return Invalid<WorkLogHistoryPage>();
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLogHistoryPage>();
        if (Read(c,tx,actor.OwnerId,id,false) is null) return Missing<WorkLogHistoryPage>();
        using var sql = Command(c,tx,"""
            SELECT TOP(26) Sequence,Event,CapturedAt,SourceUpdatedAt,Title,RecordedOn,Minutes,Notes,Category,Employer,Status
            FROM [learning].[WorkLogVersion] WHERE OwnerId=@Owner AND WorkLogId=@Id
                AND (@Before IS NULL OR Sequence<@Before) ORDER BY Sequence DESC;
            """,actor.OwnerId);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Add(sql,"@Before",SqlDbType.BigInt,before);
        var items = new List<WorkLogVersion>();
        using (var r = sql.ExecuteReader()) while (r.Read())
            items.Add(new(r.GetInt64(0),r.GetString(1),Utc(r.GetDateTime(2)),Utc(r.GetDateTime(3)),r.GetString(4),
                DateOnly.FromDateTime(r.GetDateTime(5)),r.GetInt32(6),r.IsDBNull(7)?null:r.GetString(7),
                r.IsDBNull(8)?null:r.GetString(8),r.IsDBNull(9)?null:r.GetString(9),r.GetString(10)));
        string? next = null;
        if (items.Count > 25) { items.RemoveAt(25); next = historyCursors.Encode(scope,items[^1].Sequence); }
        if (!Allowed(c,tx,actor,"learning.worklog.read")) return Denied<WorkLogHistoryPage>();
        if (Read(c,tx,actor.OwnerId,id,false) is null) return Missing<WorkLogHistoryPage>();
        tx.Commit(); return IdentityOperationResult<WorkLogHistoryPage>.Success(new(items,next));
    }

    // Called only while the existing owner update lock and source transaction are held.
    private static void CaptureHistory(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string eventName, bool baseline = false)
    {
        using var sql = Command(c,tx,"""
            INSERT [learning].[WorkLogVersion](OwnerId,WorkLogId,Sequence,Event,CapturedByUserId,SourceUpdatedAt,
                SourceRowVersion,Title,RecordedOn,Minutes,Notes,Category,Employer,Status)
            SELECT w.OwnerId,w.Id,COALESCE((SELECT MAX(v.Sequence) FROM [learning].[WorkLogVersion] v
                WHERE v.OwnerId=@Owner AND v.WorkLogId=@Id),0)+CONVERT(bigint,1),@Event,@User,w.UpdatedAt,
                w.RowVersion,w.Title,w.RecordedOn,w.Minutes,w.Notes,w.Category,w.Employer,w.Status
            FROM [learning].[WorkLog] w WHERE w.OwnerId=@Owner AND w.Id=@Id
                AND (@Baseline=0 OR NOT EXISTS(SELECT 1 FROM [learning].[WorkLogVersion] v
                    WHERE v.OwnerId=@Owner AND v.WorkLogId=@Id));
            """,actor.OwnerId);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id); Add(sql,"@Event",SqlDbType.VarChar,eventName,32);
        Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId); Add(sql,"@Baseline",SqlDbType.Bit,baseline);
        var rows = sql.ExecuteNonQuery();
        if ((!baseline && rows != 1) || (baseline && rows is not (0 or 1)))
            throw new InvalidOperationException("Work Log history capture failed.");
    }

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value,DateTimeKind.Utc));
}
