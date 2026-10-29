using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Discovery;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    internal RecentSourceProjection? ReadRecentSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id) =>
        SqlRecentSourceQuery.Read(c, tx, _capabilities, actor, "FX20", "documents.page.read", "Document", id, """
            SELECT p.Title,p.Status FROM documents.Page p WHERE p.OwnerId=@Owner AND p.Id=@Id AND p.Status IN('Draft','Published')
              AND EXISTS(SELECT 1 FROM documents.VisiblePageV1 v WHERE v.OwnerId=p.OwnerId AND v.Id=p.Id);
            """);
}
