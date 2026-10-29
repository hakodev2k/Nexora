using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Automation;
using Nexora.Application.Notifications;

namespace Nexora.Infrastructure.Notifications;

/// <summary>Notification-owned transactional writer. No real delivery or provider success is claimed.</summary>
internal static class LocalModuleNotificationWriter
{
    internal static void AutomationFailed(SqlConnection connection, SqlTransaction transaction, Guid owner, Guid run)
    {
        using var cmd=connection.CreateCommand();cmd.Transaction=transaction;
        cmd.CommandText="""
            DECLARE @User uniqueidentifier,@State varchar(32);
            SELECT @User=p.UserId,@State=r.State FROM [automation].[Run] r
             JOIN [platform].[PersonalSpace] p ON p.Id=r.OwnerId
             WHERE r.OwnerId=@Owner AND r.Id=@Run AND r.State IN('Failed','PartiallySucceeded','TimedOut');
            IF @User IS NOT NULL AND NOT EXISTS(SELECT 1 FROM [notifications].[Notification] WITH(UPDLOCK,HOLDLOCK) WHERE LogicalKey=@Key)
            BEGIN
             DECLARE @Id uniqueidentifier=NEWID();
             INSERT [notifications].[Notification](Id,OwnerUserId,LogicalKey,Kind,Title,Body,SourceRef)
             VALUES(@Id,@User,@Key,'Module','Automation run needs attention',N'The run finished in '+@State+N'. Open Automation run history to inspect the result.',NULL);
             INSERT [notifications].[Delivery](NotificationId,Channel,State,Attempts,LastErrorCode)
             VALUES(@Id,'InApp','Delivered',1,NULL),(@Id,'Email','NotApplicable',0,'ProviderUnavailable'),(@Id,'BrowserPush','PermissionUnavailable',0,'ProviderUnavailable');
             INSERT [operations].[Outbox](OwnerUserId,LogicalKey,Kind,PayloadJson,State,NotBeforeAt)
             VALUES(@User,@OutboxKey,'Automation.LocalFailureProjected',N'{"notificationId":"'+CONVERT(nvarchar(36),@Id)+N'","channels":["InApp","Email","BrowserPush"]}','Completed',SYSUTCDATETIME());
            END;
            """;
        cmd.Parameters.Add("@Owner",SqlDbType.UniqueIdentifier).Value=owner;
        cmd.Parameters.Add("@Run",SqlDbType.UniqueIdentifier).Value=run;
        cmd.Parameters.Add("@Key",SqlDbType.NVarChar,200).Value=$"automation.failure:{owner:N}:{run:N}";
        cmd.Parameters.Add("@OutboxKey",SqlDbType.NVarChar,200).Value=$"automation.failure.projected:{owner:N}:{run:N}";
        cmd.ExecuteNonQuery();
    }
    internal static AutomationFailureNotice? ReadAutomationFailure(SqlConnection connection,SqlTransaction transaction,Guid owner,Guid run)
    {
        using var cmd=connection.CreateCommand();cmd.Transaction=transaction;
        cmd.CommandText="""
            SELECT n.Id,d.Channel,d.State,d.Attempts,d.LastErrorCode,d.UpdatedAt
            FROM [notifications].[Notification] n JOIN [notifications].[Delivery] d ON d.NotificationId=n.Id
            JOIN [platform].[PersonalSpace] p ON p.UserId=n.OwnerUserId
            WHERE p.Id=@Owner AND n.LogicalKey=@Key AND n.DeletedAt IS NULL ORDER BY d.Channel;
            """;
        cmd.Parameters.Add("@Owner",SqlDbType.UniqueIdentifier).Value=owner;
        cmd.Parameters.Add("@Key",SqlDbType.NVarChar,200).Value=$"automation.failure:{owner:N}:{run:N}";
        using var r=cmd.ExecuteReader();Guid? id=null;var deliveries=new List<NotificationDeliveryRecord>();
        while(r.Read()) { id=r.GetGuid(0);deliveries.Add(new(r.GetString(1),r.GetString(2),r.GetInt32(3),r.IsDBNull(4)?null:r.GetString(4),new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(5),DateTimeKind.Utc)))); }
        return id is { } found ? new(found,deliveries) : null;
    }
    internal static void FocusCompleted(SqlConnection connection, SqlTransaction transaction, Guid user, Guid session, string phase)
    {
        using var cmd = connection.CreateCommand(); cmd.Transaction = transaction;
        cmd.CommandText = """
            IF NOT EXISTS(SELECT 1 FROM [notifications].[Notification] WITH(UPDLOCK,HOLDLOCK) WHERE LogicalKey=@Key)
            BEGIN
             DECLARE @Id uniqueidentifier=NEWID();
             INSERT [notifications].[Notification](Id,OwnerUserId,LogicalKey,Kind,Title,Body,SourceRef)
             VALUES(@Id,@User,@Key,'Module','Focus phase completed',@Body,@Source);
             INSERT [notifications].[Delivery](NotificationId,Channel,State,Attempts,LastErrorCode)
             VALUES(@Id,'InApp','Delivered',1,NULL),(@Id,'Email','NotApplicable',0,'ProviderUnavailable'),
                   (@Id,'BrowserPush','PermissionUnavailable',0,'ProviderUnavailable');
            END;
            """;
        cmd.Parameters.AddWithValue("@User", user); cmd.Parameters.AddWithValue("@Key", $"focus.completed:{session:N}");
        cmd.Parameters.AddWithValue("@Body", $"{phase} ended. Start the next phase explicitly.");
        cmd.Parameters.AddWithValue("@Source", $"focus:{session:N}"); cmd.ExecuteNonQuery();
    }
}
