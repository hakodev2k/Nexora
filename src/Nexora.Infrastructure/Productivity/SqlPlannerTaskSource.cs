using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Productivity;

/// <summary>Productivity-owned participant. The caller's Serializable transaction
/// holds source/policy reads through the Planner commit; no connection or cache
/// outside that boundary can supply authority or a private source projection.</summary>
internal sealed class SqlPlannerTaskSource(SqlSelfCapability capabilities)
{
    public bool CanRead(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor) =>
        capabilities.IsAllowed(connection, transaction, actor, "FX12", "tasks.task.read");

    public PlannerTaskSource? Read(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid taskId)
    {
        if (!CanRead(connection, transaction, actor)) return null;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT t.[Title], t.[Status], p.[Name], t.[StartAt], t.[EndAt],
                   CONVERT(bit, CASE WHEN t.[Status] IN ('NotStarted','InProgress')
                     AND p.[Status] IN ('NotStarted','InProgress') THEN 1 ELSE 0 END)
            FROM [productivity].[Task] t
            INNER JOIN [productivity].[Project] p ON p.[OwnerId]=t.[OwnerId] AND p.[Id]=t.[ProjectId]
            WHERE t.[OwnerId]=@OwnerId AND t.[Id]=@TaskId
              AND t.[DeletedAt] IS NULL AND p.[DeletedAt] IS NULL
              AND t.[Status]<>'Deleted' AND p.[Status]<>'Deleted'
              AND NOT EXISTS(SELECT 1 FROM [platform].[TrashItem] x WHERE x.[OwnerId]=t.[OwnerId]
                  AND x.[RestoredAt] IS NULL AND x.[PurgedAt] IS NULL
                  AND ((x.[ResourceType]='Task' AND x.[ResourceId]=t.[Id]) OR (x.[ResourceType]='Project' AND x.[ResourceId]=p.[Id])));
            """;
        command.Parameters.Add("@OwnerId", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        command.Parameters.Add("@TaskId", SqlDbType.UniqueIdentifier).Value = taskId;
        PlannerTaskSource? source;
        using (var reader = command.ExecuteReader())
            source = reader.Read() ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)), reader.GetBoolean(5)) : null;
        return CanRead(connection, transaction, actor) ? source : null;
    }
}

internal sealed record PlannerTaskSource(string Title, string Status, string ProjectName,
    DateTimeOffset StartAt, DateTimeOffset EndAt, bool SourceAvailable);
