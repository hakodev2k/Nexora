using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Organization;
namespace Nexora.Infrastructure.Documents;
internal sealed class SqlDocumentCollectionParticipant(SqlSelfCapability capabilities) : CollectionSourceParticipant(capabilities)
{
    internal override string Type => "Document";
    protected override string Module => "FX20";
    protected override string Action => "documents.page.read";
    protected override string Query => """
        SELECT TOP(25) p.Id,p.Title,p.Status FROM documents.Page p
        WHERE p.OwnerId=@Owner AND (@Id IS NULL OR p.Id=@Id) AND p.Title LIKE @Query ESCAPE N'\'
          AND p.Status IN('Draft','Published','Archived')
          AND EXISTS(SELECT 1 FROM documents.VisiblePageV1 v WHERE v.OwnerId=p.OwnerId AND v.Id=p.Id)
        ORDER BY p.Title,p.Id;
        """;
}
