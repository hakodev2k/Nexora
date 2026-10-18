using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.News;

internal sealed record NewsReadingSource(Guid FeedId, string Title, string Url, DateTimeOffset? ReadAt, string ETag);
public sealed partial class SqlNewsReaderService
{
    internal NewsReadingSource? ReadReadingSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(c, tx, actor, "news.article.read")) return null;
        using var sql = Command(c, tx, actor.OwnerId, $"SELECT a.FeedId,a.Title,a.Url,st.ReadAt,COALESCE(st.RowVersion,CONVERT(binary(8),0x0000000000000000)) {ArticleJoins} WHERE a.Id=@Id AND {VisibleArticle};");
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); NewsReadingSource? result;
        using (var r = sql.ExecuteReader()) result = r.Read() ? new(r.GetGuid(0), r.GetString(1), r.GetString(2), OptionalTime(r, 3), Tag(r.GetFieldValue<byte[]>(4))) : null;
        return Allowed(c, tx, actor, "news.article.read") ? result : null;
    }
    internal bool SetReadingStateWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, bool read, string? trace)
    {
        var action = read ? "news.article.mark_read" : "news.article.mark_unread";
        if (!Allowed(c, tx, actor, action) || ReadReadingSourceWithin(c, tx, actor, id) is null) return false;
        using var sql = Command(c, tx, actor.OwnerId, """
            IF EXISTS(SELECT 1 FROM [news].[ArticleState] WHERE OwnerId=@Owner AND ArticleId=@Id)
              UPDATE [news].[ArticleState] SET ReadAt=CASE WHEN @Read=1 THEN COALESCE(ReadAt,SYSUTCDATETIME()) ELSE NULL END,
                UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND ArticleId=@Id;
            ELSE INSERT [news].[ArticleState](Id,OwnerId,FeedId,ArticleId,ReadAt,CreatedByUserId,UpdatedByUserId)
              SELECT NEWID(),@Owner,FeedId,Id,CASE WHEN @Read=1 THEN SYSUTCDATETIME() ELSE NULL END,@User,@User FROM [news].[Article] WHERE Id=@Id;
            INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
              VALUES(@User,@User,@Action,'news.ArticleState',@Id,'Succeeded',@Trace);
            """);
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Read", SqlDbType.Bit, read); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Action", SqlDbType.NVarChar, action, 160); Add(sql, "@Trace", SqlDbType.NVarChar, trace, 100); sql.ExecuteNonQuery();
        var current = ReadReadingSourceWithin(c, tx, actor, id);
        if (current is null || !Allowed(c, tx, actor, action)) return false;
        return true;
    }
}
