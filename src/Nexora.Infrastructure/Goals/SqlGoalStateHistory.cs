using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    public IdentityOperationResult<GoalStatePage> StateHistory(IdentityPrincipal actor, Guid goalId, string? cursor = null)
    {
        string[] actions = ["goals.goal.history","goals.goal.read"];
        var context = new GoalProgressCursor.Context(actor.UserId,actor.OwnerId,goalId);
        var boundary = string.IsNullOrEmpty(cursor) ? null : _stateCursor.Decode(context,cursor);
        if (!string.IsNullOrEmpty(cursor) && boundary is null)
            return Failure<GoalStatePage>("ValidationFailed",422,"History cursor is invalid for this context.");
        using var c = _connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(c,tx,actor,actions)) return ModuleUnavailable<GoalStatePage>();
        if (!StateHistorySource(c,tx,actor.OwnerId,goalId)) return Missing<GoalStatePage>();
        using var sql = c.CreateCommand(); sql.Transaction = tx;
        sql.CommandText = """
            SELECT TOP(26) Id,ActionKey,FromStatus,ToStatus,RecordedAt FROM [productivity].[GoalStateHistory]
            WHERE OwnerId=@Owner AND GoalId=@Goal AND (@Before IS NULL OR RecordedAt<@Before OR (RecordedAt=@Before AND Id<@BeforeId))
            ORDER BY RecordedAt DESC,Id DESC;
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId); Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goalId);
        Add(sql,"@Before",SqlDbType.DateTime2,(object?)boundary?.RecordedAt ?? DBNull.Value); sql.Parameters["@Before"].Scale=7;
        Add(sql,"@BeforeId",SqlDbType.UniqueIdentifier,(object?)boundary?.Id ?? DBNull.Value);
        var items = new List<GoalStateEntry>();
        using (var r = sql.ExecuteReader()) while (r.Read()) items.Add(new(r.GetGuid(0),r.GetString(1),r.GetString(2),r.GetString(3),ToOffset(r.GetDateTime(4))));
        string? next = null;
        if (items.Count>25) { items.RemoveAt(25); var last=items[^1]; next=_stateCursor.Encode(context,new(last.RecordedAt.UtcDateTime,last.Id)); }
        if (!CurrentAuthority(c,tx,actor,actions)) return ModuleUnavailable<GoalStatePage>();
        if (!StateHistorySource(c,tx,actor.OwnerId,goalId)) return Missing<GoalStatePage>();
        tx.Commit(); return IdentityOperationResult<GoalStatePage>.Success(new(items,next));
    }

    private static bool StateHistorySource(SqlConnection c,SqlTransaction tx,Guid owner,Guid goal)
    {
        using var sql=c.CreateCommand(); sql.Transaction=tx;
        sql.CommandText="""
            SELECT 1 FROM [productivity].[Goal] WHERE OwnerId=@Owner AND Id=@Goal
                AND Status IN ('Draft','Active','Completed','Abandoned','Archived');
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,owner); Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goal);
        return sql.ExecuteScalar() is not null && !IsGoalTrashed(c,tx,owner,goal);
    }

    private static void CaptureStateHistory(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid goal,
        string action,string from,string to)
    {
        using var sql=c.CreateCommand(); sql.Transaction=tx;
        sql.CommandText="""
            INSERT [productivity].[GoalStateHistory](OwnerId,GoalId,ActorUserId,ActionKey,FromStatus,ToStatus,SourceRowVersion)
            SELECT OwnerId,Id,@Actor,@Action,@From,@To,RowVersion FROM [productivity].[Goal]
            WHERE OwnerId=@Owner AND Id=@Goal AND Status=@To;
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId); Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goal);
        Add(sql,"@Actor",SqlDbType.UniqueIdentifier,actor.UserId); Add(sql,"@Action",SqlDbType.NVarChar,action,160);
        Add(sql,"@From",SqlDbType.VarChar,from,64); Add(sql,"@To",SqlDbType.VarChar,to,64);
        if(sql.ExecuteNonQuery()!=1) throw new InvalidOperationException("Goal lifecycle history capture failed.");
    }
}
