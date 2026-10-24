using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Organization;
namespace Nexora.Infrastructure.Bookmarks;
internal sealed class SqlBookmarkCollectionParticipant(SqlSelfCapability capabilities) : CollectionSourceParticipant(capabilities)
{
    internal override string Type => "Bookmark";
    protected override string Module => "FX21";
    protected override string Action => "bookmarks.bookmark.read";
    protected override string Query => """
        SELECT TOP(25) b.Id,b.Title,b.Status FROM knowledge.Bookmark b
        WHERE b.OwnerId=@Owner AND (@Id IS NULL OR b.Id=@Id) AND b.Title LIKE @Query ESCAPE N'\'
          AND b.Status IN('Active','Archived')
          AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=b.OwnerId AND tr.ResourceType='Bookmark'
            AND tr.ResourceId=b.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL)
        ORDER BY b.Title,b.Id;
        """;
}
