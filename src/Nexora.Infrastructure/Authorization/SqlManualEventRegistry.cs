using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Authorization;

internal static class SqlManualEventRegistry
{
    internal static void Create(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        using var command=new SqlCommand("""
            INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,t.Id,'Active',1,SYSUTCDATETIME(),SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] t
            JOIN [platform].[Module] m ON m.Id=t.ModuleId WHERE m.Code='FX13' AND t.Code='ManualEvent';
            """,c,tx);Parameters(command,actor,id);if(command.ExecuteNonQuery()!=1)throw new InvalidOperationException("The Manual Event identity contribution is unavailable.");
    }
    internal static void Touch(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        using var command=new SqlCommand("UPDATE [platform].[Resource] SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Active';",c,tx);Parameters(command,actor,id);command.ExecuteNonQuery();
    }
    private static void Parameters(SqlCommand command,IdentityPrincipal actor,Guid id)
    {command.Parameters.Add("@Id",SqlDbType.UniqueIdentifier).Value=id;command.Parameters.Add("@Owner",SqlDbType.UniqueIdentifier).Value=actor.OwnerId;command.Parameters.Add("@User",SqlDbType.UniqueIdentifier).Value=actor.UserId;}
}
