using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Notifications;

/// <summary>Notification-owned transactional writer. No real delivery or provider success is claimed.</summary>
internal static class LocalModuleNotificationWriter
{
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
