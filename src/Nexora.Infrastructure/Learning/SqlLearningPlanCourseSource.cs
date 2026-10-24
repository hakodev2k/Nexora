namespace Nexora.Infrastructure.Learning;
internal sealed class SqlLearningPlanCourseSource : ISqlLearningPlanSource
{
    public string Kind => "Course";
    public string Module => "FX40";
    public string ReadAction => "learning.course.read";
    public string RegistryVersion => "courses-v1";
    public string EligibleSelect => """
        SELECT s.Id,s.Title,s.Status,s.UpdatedAt FROM [learning].[Course] s
        WHERE s.OwnerId=@Owner AND s.Status IN('Planned','InProgress','Completed','Abandoned')
          AND EXISTS(SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
            JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE r.OwnerId=s.OwnerId AND r.Id=s.Id
            AND m.Code='FX40' AND rt.Code='Course' AND rt.ContractVersion='courses-v1' AND r.PurgedAt IS NULL)
        """;
}
