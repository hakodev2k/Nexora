using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Discovery;

// Source modules own the SQL below their participant boundary; this helper only
// applies the common current authority/owner parameters and safe response shape.
internal static class SqlRecentSourceQuery
{
    internal static RecentSourceProjection? Read(SqlConnection connection, SqlTransaction transaction,
        SqlSelfCapability capabilities, IdentityPrincipal actor, string module, string action,
        string type, Guid id, string sql, Func<bool>? dependency = null)
    {
        bool Allowed() => capabilities.IsAllowed(connection, transaction, actor, module, action) && (dependency?.Invoke() ?? true);
        if (!Allowed()) return null;
        using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandTimeout = 3; command.CommandText = sql;
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        RecentSourceProjection? result;
        using (var reader = command.ExecuteReader()) result = reader.Read() ? new(type, id, reader.GetString(0), reader.GetString(1)) : null;
        return Allowed() ? result : null;
    }
}
