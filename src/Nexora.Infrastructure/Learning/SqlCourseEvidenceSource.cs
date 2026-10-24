using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Learning;

public sealed partial class SqlCourseService
{
    internal static (string Title, string Status)? ReadEvidenceSource(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, Guid id, SqlSelfCapability capabilities)
    {
        if (!capabilities.IsAllowed(connection, transaction, actor, "FX40", "learning.course.read")) return null;
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = "SELECT Title,Status FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id AND Status<>'Trash';";
        sql.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        sql.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        (string Title, string Status)? result = null;
        using (var reader = sql.ExecuteReader()) if (reader.Read()) result = (reader.GetString(0), reader.GetString(1));
        return capabilities.IsAllowed(connection, transaction, actor, "FX40", "learning.course.read") ? result : null;
    }
}
