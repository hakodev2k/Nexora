using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Discovery;

namespace Nexora.Infrastructure.Productivity;

public sealed partial class SqlProductivityService
{
    internal RecentSourceProjection? ReadRecentSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string type, Guid id)
    {
        var spec = type switch
        {
            "Project" => (Module: "FX11", Action: "projects.project.read", Sql: """
                SELECT p.Name,p.Status FROM productivity.Project p WHERE p.OwnerId=@Owner AND p.Id=@Id
                  AND p.DeletedAt IS NULL AND p.Status IN('NotStarted','InProgress','Completed','Skipped')
                  AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=p.OwnerId AND tr.ResourceType='Project' AND tr.ResourceId=p.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
                """),
            "Task" => (Module: "FX12", Action: "tasks.task.read", Sql: """
                SELECT t.Title,t.Status FROM productivity.Task t JOIN productivity.Project p ON p.OwnerId=t.OwnerId AND p.Id=t.ProjectId
                WHERE t.OwnerId=@Owner AND t.Id=@Id AND t.DeletedAt IS NULL AND t.Status IN('NotStarted','InProgress','Completed','Skipped') AND p.DeletedAt IS NULL AND p.Status IN('NotStarted','InProgress','Completed','Skipped')
                  AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=t.OwnerId AND ((tr.ResourceType='Task' AND tr.ResourceId=t.Id) OR (tr.ResourceType='Project' AND tr.ResourceId=p.Id)) AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
                """),
            "Event" => (Module: "FX13", Action: "calendar.event.read", Sql: """
                SELECT e.Title,e.Status FROM calendar.Event e
                WHERE e.OwnerId=@Owner AND e.Id=@Id AND e.DeletedAt IS NULL AND e.Status IN('Scheduled','Completed','Canceled')
                  AND ((e.SourceKind='Manual' AND e.TaskId IS NULL) OR (e.SourceKind='Task' AND e.TaskId IS NOT NULL))
                  AND (e.TaskId IS NULL OR EXISTS(SELECT 1 FROM productivity.Task t JOIN productivity.Project p ON p.OwnerId=t.OwnerId AND p.Id=t.ProjectId
                    WHERE t.OwnerId=e.OwnerId AND t.Id=e.TaskId AND t.DeletedAt IS NULL AND t.Status IN('NotStarted','InProgress','Completed','Skipped') AND p.DeletedAt IS NULL AND p.Status IN('NotStarted','InProgress','Completed','Skipped')
                      AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=t.OwnerId AND ((tr.ResourceType='Task' AND tr.ResourceId=t.Id) OR (tr.ResourceType='Project' AND tr.ResourceId=p.Id)) AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL)))
                  AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=e.OwnerId AND tr.ResourceType='Event' AND tr.ResourceId=e.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
                """),
            _ => default
        };
        if (spec.Module is null) return null;
        bool EventSourceAllowed()
        {
            if (type != "Event") return true;
            using var command = c.CreateCommand(); command.Transaction = tx;
            command.CommandText = "SELECT CONVERT(bit,CASE WHEN TaskId IS NULL THEN 0 ELSE 1 END) FROM calendar.Event WHERE OwnerId=@Owner AND Id=@Id;";
            command.Parameters.Add("@Owner", System.Data.SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
            command.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier).Value = id;
            return command.ExecuteScalar() is false || _capabilities.IsAllowed(c, tx, actor, "FX12", "tasks.task.read");
        }
        return SqlRecentSourceQuery.Read(c, tx, _capabilities, actor, spec.Module, spec.Action, type, id, spec.Sql, EventSourceAllowed);
    }
}
