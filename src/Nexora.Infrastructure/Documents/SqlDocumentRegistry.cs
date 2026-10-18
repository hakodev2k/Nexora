using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    internal static bool RegisterPageWithin(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="""
            INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
            JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX20' AND rt.Code='Page' AND rt.ContractVersion='documents-page-v1';
            """;
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
        return sql.ExecuteNonQuery()==1;
    }
    internal static void TouchPageRegistryWithin(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id,string availability)
    {
        if(availability is not("Active" or "Archived" or "Trash" or "Purged"))throw new ArgumentException("Unknown Documents lifecycle.",nameof(availability));
        using var sql=c.CreateCommand();sql.Transaction=tx;
        sql.CommandText="UPDATE [platform].[Resource] SET Availability=@State,Revision=Revision+1,PurgedAt=CASE WHEN @State='Purged' THEN SYSUTCDATETIME() ELSE NULL END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;";
        Add(sql,"@State",SqlDbType.VarChar,availability);Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);
        if(sql.ExecuteNonQuery()!=1)throw new InvalidOperationException("Documents registry integrity is unavailable.");
    }
}
