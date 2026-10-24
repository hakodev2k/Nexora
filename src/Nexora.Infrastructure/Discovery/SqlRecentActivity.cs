using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Discovery;

internal static class SqlRecentActivity
{
    internal static bool LockOwner(SqlConnection connection, SqlTransaction transaction, Guid owner)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DECLARE @Result int; EXEC @Result=sys.sp_getapplock @Resource=@Lock,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000; SELECT @Result;";
        command.Parameters.Add("@Lock", SqlDbType.NVarChar, 255).Value = "nexora:recents:" + owner.ToString("N");
        return Convert.ToInt32(command.ExecuteScalar()) >= 0;
    }
    internal static void RecordWithin(SqlConnection connection, SqlTransaction transaction,
        SqlSelfCapability capabilities, IdentityPrincipal actor, RecentSourceProjection? source)
    {
        if (source is null || !capabilities.IsAllowed(connection, transaction, actor, "FX25", "discovery.recent.read")) return;
        if (!LockOwner(connection, transaction, actor.OwnerId)) throw new TimeoutException("Recent activity is temporarily unavailable.");
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            IF EXISTS(SELECT 1 FROM discovery.RecentItem WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND ResourceType=@Type AND ResourceId=@Resource)
                UPDATE discovery.RecentItem SET LastOpenedAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND ResourceType=@Type AND ResourceId=@Resource;
            ELSE INSERT discovery.RecentItem(Id,OwnerId,ResourceType,ResourceId,LastOpenedAt,CreatedAt,CreatedByUserId,UpdatedAt,UpdatedByUserId)
                VALUES(NEWID(),@Owner,@Type,@Resource,@Now,@Now,@User,@Now,@User);
            WITH Ordered AS(SELECT Id,ROW_NUMBER() OVER(ORDER BY LastOpenedAt DESC,RowVersion DESC,Id DESC) AS Position
                FROM discovery.RecentItem WHERE OwnerId=@Owner)
            DELETE FROM discovery.RecentItem WHERE OwnerId=@Owner AND Id IN(SELECT Id FROM Ordered WHERE Position>100);
            """;
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        command.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@Type", SqlDbType.VarChar, 32).Value = source.ResourceType;
        command.Parameters.Add("@Resource", SqlDbType.UniqueIdentifier).Value = source.ResourceId;
        command.ExecuteNonQuery();
    }
}
