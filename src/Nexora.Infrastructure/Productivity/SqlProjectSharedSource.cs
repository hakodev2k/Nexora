using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Sharing;
using Nexora.Infrastructure.Sharing;

namespace Nexora.Infrastructure.Productivity;

internal static class SqlProjectSharedSource
{
    internal static (Guid OwnerId,string Status)? ReadState(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid resourceId)
    {
        using var command=connection.CreateCommand(); command.Transaction=transaction;
        command.CommandText="SELECT [OwnerId], [Status] FROM [productivity].[Project] WITH(HOLDLOCK) WHERE [Id]=@Id AND [OwnerId]=@OwnerId;";
        Add(command,"@Id",SqlDbType.UniqueIdentifier,resourceId); Add(command,"@OwnerId",SqlDbType.UniqueIdentifier,ownerId);
        using var reader=command.ExecuteReader(); return reader.Read()?(reader.GetGuid(0),reader.GetString(1)):null;
    }
    internal static SharedProjectProjection? ReadProjection(SqlConnection connection, SqlTransaction transaction, SharedLinkSourceContext source, DateTimeOffset now)
    {
        var ownerId=source.OwnerId; var projectId=source.ResourceId;
        using var command = connection.CreateCommand();
        command.Transaction=transaction;
        command.CommandText = """
            SELECT [Id], [Name], [Description], [Status], [StartAt], [EndAt], [Priority], [TagsJson]
            FROM [productivity].[Project]
            WHERE [Id] = @ProjectId AND [OwnerId] = @OwnerId AND [Status] <> 'Deleted';
            """;
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        SharedProjectProjection? project;
        using (var reader = command.ExecuteReader())
        {
            if (!reader.Read()) return null;
            project = new SharedProjectProjection(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3), ToOffset(reader.GetDateTime(4)), ToOffset(reader.GetDateTime(5)), reader.GetString(6), reader.GetString(7), Array.Empty<SharedTaskProjection>());
        }

        using var tasks = connection.CreateCommand();
        tasks.Transaction=transaction;
        tasks.CommandText = """
            SELECT [Id], [Title], [Description], [Status], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson]
            FROM [productivity].[Task]
            WHERE [OwnerId] = @OwnerId AND [ProjectId] = @ProjectId AND [Status] <> 'Deleted'
            ORDER BY [Rank], [DueAt], [Id];
            """;
        Add(tasks, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(tasks, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        var items = new List<SharedTaskProjection>();
        using var taskReader = tasks.ExecuteReader();
        while (taskReader.Read())
        {
            var status = taskReader.GetString(3);
            var startAt = ToOffset(taskReader.GetDateTime(4));
            var endAt = ToOffset(taskReader.GetDateTime(5));
            items.Add(new SharedTaskProjection(taskReader.GetGuid(0), taskReader.GetString(1), taskReader.IsDBNull(2) ? null : taskReader.GetString(2),
                status, startAt, endAt, taskReader.IsDBNull(6) ? null : taskReader.GetString(6), taskReader.GetString(7),
                endAt < now && status is "NotStarted" or "InProgress", taskReader.IsDBNull(8) ? null : taskReader.GetString(8)));
        }
        return project! with { Tasks = items };
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static void Add(SqlCommand command, string name, SqlDbType type, object value) => command.Parameters.Add(name,type).Value=value;
}
