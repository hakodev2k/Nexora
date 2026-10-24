using System.Data;
using System.Globalization;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    public IdentityOperationResult<GoalProgressPage> ProgressHistory(IdentityPrincipal actor, Guid goalId, string? cursor = null)
    {
        string[] actions = ["goals.goal.history", "goals.goal.read", "goals.target.read"];
        if (!HasAllCapabilities(actor, actions)) return ModuleUnavailable<GoalProgressPage>();
        var context = new GoalProgressCursor.Context(actor.UserId, actor.OwnerId, goalId);
        var boundary = string.IsNullOrEmpty(cursor) ? null : _progressCursor.Decode(context, cursor);
        if (!string.IsNullOrEmpty(cursor) && boundary is null)
            return Failure<GoalProgressPage>("ValidationFailed", 422, "History cursor is invalid for this context.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, actions)) return ModuleUnavailable<GoalProgressPage>();
        using (var source = connection.CreateCommand())
        {
            source.Transaction = transaction;
            source.CommandText = """
                SELECT TOP (1) 1 FROM [productivity].[Goal] g
                WHERE g.[OwnerId] = @OwnerId AND g.[Id] = @GoalId
                  AND g.[Status] IN ('Draft','Active','Completed','Abandoned','Archived')
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                    WHERE tr.[OwnerId] = g.[OwnerId] AND tr.[ResourceType] = 'Goal' AND tr.[ResourceId] = g.[Id]
                      AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """;
            Add(source, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(source, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
            if (source.ExecuteScalar() is null) return Missing<GoalProgressPage>();
        }
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (26) p.[Id], p.[TargetId], t.[Kind], t.[Title], p.[Value], p.[Checked], p.[Note], p.[RecordedAt],
                CONVERT(bit, CASE WHEN t.[RemovedAt] IS NULL THEN 0 ELSE 1 END)
            FROM [productivity].[GoalProgress] p
            INNER JOIN [productivity].[GoalTarget] t ON t.[OwnerId] = p.[OwnerId] AND t.[Id] = p.[TargetId]
            WHERE p.[OwnerId] = @OwnerId AND t.[GoalId] = @GoalId
              AND ((t.[Kind] = 'Numeric' AND p.[Value] IS NOT NULL AND p.[Checked] IS NULL)
                OR (t.[Kind] = 'Boolean' AND p.[Checked] IS NOT NULL AND p.[Value] IS NULL))
              AND (@Before IS NULL OR p.[RecordedAt] < @Before OR (p.[RecordedAt] = @Before AND p.[Id] < @BeforeId))
            ORDER BY p.[RecordedAt] DESC, p.[Id] DESC;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@GoalId", SqlDbType.UniqueIdentifier, goalId);
        Add(command, "@Before", SqlDbType.DateTime2, (object?)boundary?.RecordedAt ?? DBNull.Value);
        command.Parameters["@Before"].Scale = 7;
        Add(command, "@BeforeId", SqlDbType.UniqueIdentifier, (object?)boundary?.Id ?? DBNull.Value);
        var items = new List<GoalProgressEntry>();
        using (var reader = command.ExecuteReader())
            while (reader.Read())
                items.Add(new GoalProgressEntry(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetDecimal(4).ToString("G29", CultureInfo.InvariantCulture),
                    reader.IsDBNull(5) ? null : reader.GetBoolean(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                    ToOffset(reader.GetDateTime(7)), reader.GetBoolean(8)));
        string? next = null;
        if (items.Count > 25)
        {
            items.RemoveAt(25);
            var last = items[^1];
            next = _progressCursor.Encode(context, new GoalProgressCursor.Boundary(last.RecordedAt.UtcDateTime, last.Id));
        }
        if (!CurrentAuthority(connection, transaction, actor, actions)) return ModuleUnavailable<GoalProgressPage>();
        transaction.Commit();
        return IdentityOperationResult<GoalProgressPage>.Success(new GoalProgressPage(items, next));
    }
}
