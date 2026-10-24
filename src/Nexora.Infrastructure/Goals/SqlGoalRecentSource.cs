using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Discovery;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    internal RecentSourceProjection? ReadRecentSourceWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id) =>
        SqlRecentSourceQuery.Read(c, tx, _capabilities, actor, "FX16", "goals.goal.read", "Goal", id, """
            SELECT g.Title,g.Status FROM productivity.Goal g WHERE g.OwnerId=@Owner AND g.Id=@Id AND g.Status IN('Draft','Active','Completed','Abandoned')
              AND NOT EXISTS(SELECT 1 FROM platform.TrashItem tr WHERE tr.OwnerId=g.OwnerId AND tr.ResourceType='Goal' AND tr.ResourceId=g.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL);
            """);
}
