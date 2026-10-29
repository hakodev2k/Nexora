using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Transfer;

public sealed class SqlImportFileRetention : IFileRetentionParticipant
{
    public bool IsRetained(SqlConnection connection,SqlTransaction transaction,Guid ownerId,Guid fileId)
    {
        using var command=new SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM [operations].[ImportBatch] WHERE OwnerId=@Owner AND FileObjectId=@File) THEN 1 ELSE 0 END;",connection,transaction);
        command.Parameters.Add("@Owner",SqlDbType.UniqueIdentifier).Value=ownerId;command.Parameters.Add("@File",SqlDbType.UniqueIdentifier).Value=fileId;return Convert.ToInt32(command.ExecuteScalar())==1;
    }
}
