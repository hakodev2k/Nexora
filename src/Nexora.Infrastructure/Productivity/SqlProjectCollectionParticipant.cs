using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Organization;
namespace Nexora.Infrastructure.Productivity;
internal sealed class SqlProjectCollectionParticipant(SqlSelfCapability capabilities) : CollectionSourceParticipant(capabilities)
{
    internal override string Type => "Project";
    protected override string Module => "FX11";
    protected override string Action => "projects.project.read";
    protected override string Query => """
        SELECT TOP(25) p.Id,p.Name,p.Status FROM productivity.Project p
        WHERE p.OwnerId=@Owner AND (@Id IS NULL OR p.Id=@Id) AND p.Name LIKE @Query ESCAPE N'\'
          AND p.DeletedAt IS NULL AND p.Status IN('NotStarted','InProgress','Completed','Skipped')
          AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=p.OwnerId AND tr.ResourceType='Project'
            AND tr.ResourceId=p.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL)
        ORDER BY p.Name,p.Id;
        """;
}
