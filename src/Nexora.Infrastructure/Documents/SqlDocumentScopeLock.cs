using System.Data;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Documents;

/// <summary>Documents-owned writer ordering across page, folder, Tag and cohorts.</summary>
internal static class SqlDocumentScopeLock
{
    internal static void Acquire(SqlConnection connection, SqlTransaction transaction, Guid owner)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = "DECLARE @Result int; EXEC @Result=sys.sp_getapplock @Resource=@Scope,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000; SELECT @Result;";
        sql.Parameters.Add("@Scope",SqlDbType.NVarChar,255).Value=$"documents:{owner:N}";
        if(Convert.ToInt32(sql.ExecuteScalar())<0)throw new TimeoutException("The Documents owner scope is busy.");
    }
}
