using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Discovery;

namespace Nexora.Infrastructure.Snippets;

public sealed partial class SqlSnippetService
{
    internal RecentSourceProjection? ReadRecentSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id) =>
        SqlRecentSourceQuery.Read(c, tx, _capabilities, actor, "FX22", "snippets.snippet.read", "Snippet", id, """
            SELECT v.Title,s.Status FROM knowledge.Snippet s JOIN knowledge.SnippetVersion v ON v.OwnerId=s.OwnerId AND v.SnippetId=s.Id AND v.VersionNumber=s.CurrentVersion
            WHERE s.OwnerId=@Owner AND s.Id=@Id AND s.Status='Active'
              AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=s.OwnerId AND tr.ResourceType='Snippet' AND tr.ResourceId=s.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
            """);
}
