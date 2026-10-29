using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Files;

/// <summary>Files-owned reference retention participant; no binary or private file projection.</summary>
internal static class SqlDocumentFilePins
{
    internal static bool HasPinsWithin(SqlConnection connection,SqlTransaction transaction,Guid owner,IReadOnlyList<Guid> pageIds)
    {
        using var sql=connection.CreateCommand();sql.Transaction=transaction;
        sql.CommandText="SELECT CASE WHEN EXISTS(SELECT 1 FROM [files].[FileReference] WITH(HOLDLOCK) WHERE OwnerId=@Owner AND ResourceType='Document' AND ResourceId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'))) THEN 1 ELSE 0 END;";
        sql.Parameters.Add("@Owner",SqlDbType.UniqueIdentifier).Value=owner;sql.Parameters.Add("@Pages",SqlDbType.NVarChar,-1).Value=JsonSerializer.Serialize(pageIds);
        return Convert.ToInt32(sql.ExecuteScalar())==1;
    }
}
