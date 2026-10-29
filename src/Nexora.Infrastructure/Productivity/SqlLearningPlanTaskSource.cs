using Nexora.Infrastructure.Learning;
namespace Nexora.Infrastructure.Productivity;

internal sealed class SqlLearningPlanTaskSource : ISqlLearningPlanSource
{
    public string Kind => "Task";
    public string Module => "FX12";
    public string ReadAction => "tasks.task.read";
    public string RegistryVersion => "tasks-plan-source-v1";
    public string EligibleSelect => """
        SELECT s.Id,s.Title,s.Status,s.UpdatedAt FROM [productivity].[Task] s
        JOIN [productivity].[Project] p ON p.OwnerId=s.OwnerId AND p.Id=s.ProjectId
        WHERE s.OwnerId=@Owner AND s.DeletedAt IS NULL AND p.DeletedAt IS NULL
          AND s.Status IN('NotStarted','InProgress','Completed','Skipped') AND p.Status<>'Deleted'
          AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] x WHERE x.OwnerId=s.OwnerId
            AND x.RestoredAt IS NULL AND x.PurgedAt IS NULL
            AND ((x.ResourceType='Task' AND x.ResourceId=s.Id) OR (x.ResourceType='Project' AND x.ResourceId=p.Id)))
        """;
}
