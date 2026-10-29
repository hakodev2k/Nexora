using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Organization;
namespace Nexora.Infrastructure.Snippets;
internal sealed class SqlSnippetCollectionParticipant(SqlSelfCapability capabilities) : CollectionSourceParticipant(capabilities)
{
    internal override string Type => "Snippet";
    protected override string Module => "FX22";
    protected override string Action => "snippets.snippet.read";
    protected override string Query => """
        SELECT TOP(25) s.Id,v.Title,s.Status FROM knowledge.Snippet s JOIN knowledge.SnippetVersion v
          ON v.OwnerId=s.OwnerId AND v.SnippetId=s.Id AND v.VersionNumber=s.CurrentVersion
        WHERE s.OwnerId=@Owner AND (@Id IS NULL OR s.Id=@Id) AND v.Title LIKE @Query ESCAPE N'\'
          AND s.Status IN('Active','Archived')
          AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=s.OwnerId AND tr.ResourceType='Snippet'
            AND tr.ResourceId=s.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL)
        ORDER BY v.Title,s.Id;
        """;
}
