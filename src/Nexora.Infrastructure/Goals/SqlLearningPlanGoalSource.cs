using Nexora.Infrastructure.Learning;
namespace Nexora.Infrastructure.Goals;

internal sealed class SqlLearningPlanGoalSource : ISqlLearningPlanSource
{
    public string Kind => "Goal";
    public string Module => "FX16";
    public string ReadAction => "goals.goal.read";
    public string RegistryVersion => "goals-plan-source-v1";
    public string EligibleSelect => """
        SELECT s.Id,s.Title,s.Status,s.UpdatedAt FROM [productivity].[Goal] s
        WHERE s.OwnerId=@Owner AND s.Status IN('Draft','Active','Completed','Abandoned')
          AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] x WHERE x.OwnerId=s.OwnerId
            AND x.ResourceType='Goal' AND x.ResourceId=s.Id AND x.RestoredAt IS NULL AND x.PurgedAt IS NULL)
        """;
}
