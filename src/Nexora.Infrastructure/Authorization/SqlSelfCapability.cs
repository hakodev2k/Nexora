using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Authorization;

/// <summary>
/// Resolves the current SELF capability from SQL on every request. User and
/// SuperAdmin principals have the approved own-resource baseline; an Admin
/// principal must have an explicit Allow for the exact action. A matching
/// Deny always wins, and module/user enablement is evaluated independently.
/// No decision is cached in Redis or in the process.
/// </summary>
internal sealed class SqlSelfCapability
{
    private readonly SqlConnectionFactory _connections;

    public SqlSelfCapability(SqlConnectionFactory connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
    }

    public bool IsAllowed(IdentityPrincipal actor, string moduleCode, params string[] actionKeys)
    {
        if (actor is null || string.IsNullOrWhiteSpace(moduleCode) || actionKeys.Length == 0)
            return false;

        using var connection = _connections.Create();
        connection.Open();
        return IsAllowed(connection, null, actor, moduleCode, actionKeys);
    }

    public bool IsAllowed(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, string moduleCode, params string[] actionKeys)
    {
        if (connection is null || actor is null || string.IsNullOrWhiteSpace(moduleCode) || actionKeys.Length == 0)
            return false;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var keyNames = new List<string>(actionKeys.Length);
        for (var index = 0; index < actionKeys.Length; index++)
        {
            var name = "@Action" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            keyNames.Add(name);
            command.Parameters.Add(name, SqlDbType.NVarChar, 160).Value = actionKeys[index];
        }

        command.CommandText = $"""
            SELECT CASE WHEN m.[State] = 'Ready'
                              AND m.[SystemEnabled] = 1
                              AND COALESCE(g.[Enabled], 0) = 1
                              AND (
                                  @Role IN ('User', 'SuperAdmin')
                                  OR (
                                      @Role = 'Admin'
                                      AND EXISTS (
                                          SELECT 1
                                          FROM [platform].[AdminPermission] ap
                                          INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
                                          WHERE ap.[UserId] = @UserId
                                            AND ap.[Effect] = 'Allow'
                                            AND p.[EffectiveStatus] = 'Resolved'
                                            AND p.[ActionKey] IN ({string.Join(',', keyNames)})
                                      )
                                      AND NOT EXISTS (
                                          SELECT 1
                                          FROM [platform].[AdminPermission] ap
                                          INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
                                          WHERE ap.[UserId] = @UserId
                                            AND ap.[Effect] = 'Deny'
                                            AND p.[ActionKey] IN ({string.Join(',', keyNames)})
                                      )
                                  )
                              ) THEN 1 ELSE 0 END
            FROM [platform].[Module] m
            LEFT JOIN [platform].[UserModuleGrant] g
              ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId
            WHERE m.[Code] = @ModuleCode;
            """;
        command.Parameters.Add("@Role", SqlDbType.VarChar, 32).Value = actor.Role;
        command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@ModuleCode", SqlDbType.VarChar, 64).Value = moduleCode;
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }
}
