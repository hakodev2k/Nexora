using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Goals;

public sealed partial class SqlGoalService
{
    public IdentityOperationResult<GoalMetadataPage> MetadataHistory(IdentityPrincipal actor,Guid goalId,string? cursor=null)
    {
        string[] actions=["goals.goal.history","goals.goal.read"];
        var context=new GoalProgressCursor.Context(actor.UserId,actor.OwnerId,goalId);
        var before=string.IsNullOrEmpty(cursor)?null:_metadataCursor.Decode(context,cursor);
        if(!string.IsNullOrEmpty(cursor)&&before is null)return Failure<GoalMetadataPage>("ValidationFailed",422,"History cursor is invalid for this context.");
        using var c=_connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!CurrentAuthority(c,tx,actor,actions))return ModuleUnavailable<GoalMetadataPage>();
        if(!StateHistorySource(c,tx,actor.OwnerId,goalId))return Missing<GoalMetadataPage>();
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="""
            SELECT TOP(26) Id,Event,Title,Description,StartDate,EndDate,CapturedAt,SourceUpdatedAt
            FROM [productivity].[GoalMetadataVersion] WHERE OwnerId=@Owner AND GoalId=@Goal
                AND (@Before IS NULL OR CapturedAt<@Before OR (CapturedAt=@Before AND Id<@BeforeId))
            ORDER BY CapturedAt DESC,Id DESC;
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goalId);
        Add(sql,"@Before",SqlDbType.DateTime2,(object?)before?.RecordedAt??DBNull.Value);sql.Parameters["@Before"].Scale=7;
        Add(sql,"@BeforeId",SqlDbType.UniqueIdentifier,(object?)before?.Id??DBNull.Value);
        var items=new List<GoalMetadataEntry>();using(var r=sql.ExecuteReader())while(r.Read())
            items.Add(new(r.GetGuid(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),
                r.IsDBNull(4)?null:DateOnly.FromDateTime(r.GetDateTime(4)),r.IsDBNull(5)?null:DateOnly.FromDateTime(r.GetDateTime(5)),
                ToOffset(r.GetDateTime(6)),ToOffset(r.GetDateTime(7))));
        string? next=null;if(items.Count>25){items.RemoveAt(25);var last=items[^1];next=_metadataCursor.Encode(context,new(last.CapturedAt.UtcDateTime,last.Id));}
        if(!CurrentAuthority(c,tx,actor,actions))return ModuleUnavailable<GoalMetadataPage>();
        if(!StateHistorySource(c,tx,actor.OwnerId,goalId))return Missing<GoalMetadataPage>();
        tx.Commit();return IdentityOperationResult<GoalMetadataPage>.Success(new(items,next));
    }

    private static void CaptureMetadataHistory(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid goal,string eventName,bool baseline=false)
    {
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="""
            INSERT [productivity].[GoalMetadataVersion](OwnerId,GoalId,CapturedByUserId,SourceUpdatedAt,SourceRowVersion,Event,Title,Description,StartDate,EndDate)
            SELECT OwnerId,Id,@Actor,UpdatedAt,RowVersion,@Event,Title,Description,StartDate,EndDate FROM [productivity].[Goal]
            WHERE OwnerId=@Owner AND Id=@Goal AND (@Baseline=0 OR NOT EXISTS(SELECT 1 FROM [productivity].[GoalMetadataVersion] v WHERE v.OwnerId=@Owner AND v.GoalId=@Goal));
            """;
        Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Goal",SqlDbType.UniqueIdentifier,goal);
        Add(sql,"@Actor",SqlDbType.UniqueIdentifier,actor.UserId);Add(sql,"@Event",SqlDbType.VarChar,eventName,32);Add(sql,"@Baseline",SqlDbType.Bit,baseline);
        var rows=sql.ExecuteNonQuery();if((!baseline&&rows!=1)||(baseline&&rows is not(0 or 1)))throw new InvalidOperationException("Goal metadata history capture failed.");
    }
}
