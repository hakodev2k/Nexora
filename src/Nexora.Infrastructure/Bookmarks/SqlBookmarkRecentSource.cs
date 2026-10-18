using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Discovery;

namespace Nexora.Infrastructure.Bookmarks;

public sealed partial class SqlBookmarkService
{
    internal RecentSourceProjection? ReadRecentSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id) =>
        SqlRecentSourceQuery.Read(c, tx, _capabilities, actor, "FX21", "bookmarks.bookmark.read", "Bookmark", id, """
            SELECT b.Title,b.Status FROM knowledge.Bookmark b WHERE b.OwnerId=@Owner AND b.Id=@Id AND b.Status='Active'
              AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=b.OwnerId AND tr.ResourceType='Bookmark' AND tr.ResourceId=b.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
            """);
}
