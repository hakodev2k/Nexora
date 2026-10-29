using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    public IdentityOperationResult<GoalTargetDefinitionPage> TargetDefinitionHistory(IdentityPrincipal actor,Guid goalId,string? cursor=null)
    {
        string[] actions=["goals.goal.history","goals.goal.read","goals.target.read"];
        var context=new GoalProgressCursor.Context(actor.UserId,actor.OwnerId,goalId);
        var before=string.IsNullOrEmpty(cursor)?null:_targetHistoryCursor.Decode(context,cursor);
        if(!string.IsNullOrEmpty(cursor)&&before is null)return Failure<GoalTargetDefinitionPage>("ValidationFailed",422,"History cursor is invalid for this context.");
        using var c=_connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!CurrentAuthority(c,tx,actor,actions))return ModuleUnavailable<GoalTargetDefinitionPage>();
        if(!StateHistorySource(c,tx,actor.OwnerId,goalId))return Missing<GoalTargetDefinitionPage>();
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="""
            SELECT TOP(26) Id,TargetId,Event,Kind,Title,InitialValue,TargetValue,Removed,CapturedAt,SourceUpdatedAt
            FROM [productivity].[GoalTargetDefinitionVersion] WHERE OwnerId=@Owner AND GoalId=@Goal
                AND (@Before IS NULL OR CapturedAt<@Before OR (CapturedAt=@Before AND Id<@BeforeId)) ORDER BY CapturedAt DESC,Id DESC;
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goalId);
        Add(sql,"@Before",SqlDbType.DateTime2,(object?)before?.RecordedAt??DBNull.Value);sql.Parameters["@Before"].Scale=7;
        Add(sql,"@BeforeId",SqlDbType.UniqueIdentifier,(object?)before?.Id??DBNull.Value);
        var items=new List<GoalTargetDefinitionEntry>();using(var r=sql.ExecuteReader())while(r.Read())
            items.Add(new(r.GetGuid(0),r.GetGuid(1),r.GetString(2),r.GetString(3),r.GetString(4),
                r.IsDBNull(5)?null:r.GetDecimal(5).ToString("0.########",CultureInfo.InvariantCulture),
                r.IsDBNull(6)?null:r.GetDecimal(6).ToString("0.########",CultureInfo.InvariantCulture),r.GetBoolean(7),ToOffset(r.GetDateTime(8)),ToOffset(r.GetDateTime(9))));
        string? next=null;if(items.Count>25){items.RemoveAt(25);var last=items[^1];next=_targetHistoryCursor.Encode(context,new(last.CapturedAt.UtcDateTime,last.Id));}
        if(!CurrentAuthority(c,tx,actor,actions))return ModuleUnavailable<GoalTargetDefinitionPage>();
        if(!StateHistorySource(c,tx,actor.OwnerId,goalId))return Missing<GoalTargetDefinitionPage>();
        tx.Commit();return IdentityOperationResult<GoalTargetDefinitionPage>.Success(new(items,next));
    }

    private static void CaptureTargetDefinition(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid goal,Guid target,string eventName,bool baseline=false)
    {
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="""
            INSERT [productivity].[GoalTargetDefinitionVersion](OwnerId,GoalId,TargetId,CapturedByUserId,SourceUpdatedAt,SourceRowVersion,Event,Kind,Title,InitialValue,TargetValue,Removed)
            SELECT OwnerId,GoalId,Id,@Actor,UpdatedAt,RowVersion,@Event,Kind,Title,InitialValue,TargetValue,CONVERT(bit,CASE WHEN RemovedAt IS NULL THEN 0 ELSE 1 END)
            FROM [productivity].[GoalTarget] WHERE OwnerId=@Owner AND GoalId=@Goal AND Id=@Target
                AND (@Baseline=0 OR NOT EXISTS(SELECT 1 FROM [productivity].[GoalTargetDefinitionVersion] v WHERE v.OwnerId=@Owner AND v.TargetId=@Target));
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goal);Add(sql,"@Target",SqlDbType.UniqueIdentifier,target);
        Add(sql,"@Actor",SqlDbType.UniqueIdentifier,actor.UserId);Add(sql,"@Event",SqlDbType.VarChar,eventName,32);Add(sql,"@Baseline",SqlDbType.Bit,baseline);
        var rows=sql.ExecuteNonQuery();if((!baseline&&rows!=1)||(baseline&&rows is not(0 or 1)))throw new InvalidOperationException("Goal target definition capture failed.");
    }
}
