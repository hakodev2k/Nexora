using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Authorization;

/// <summary>
/// Resolves the current SELF capability from SQL on every request. User and
/// SuperAdmin principals have the approved own-resource baseline; an Admin
/// principal must have an explicit Allow for the exact action. A matching
/// Deny always wins, and every hard module dependency must be ready, system
/// enabled and enabled for the same user. No decision is cached in Redis or
/// in the process.
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
        return Evaluate(connection, null, actor, moduleCode, actionKeys) == SqlCapabilityStatus.Allowed;
    }

    public bool IsAllowed(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, string moduleCode, params string[] actionKeys)
        => Evaluate(connection, transaction, actor, moduleCode, actionKeys) == SqlCapabilityStatus.Allowed;

    public SqlCapabilityStatus Evaluate(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, string moduleCode, params string[] actionKeys)
    {
        if (connection is null || actor is null || string.IsNullOrWhiteSpace(moduleCode) || actionKeys.Length == 0)
            return SqlCapabilityStatus.ModuleUnavailable;

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
            WITH dependency_chain AS
            (
                SELECT d.[ModuleId] AS [RootModuleId], d.[DependsOnModuleId] AS [DependencyModuleId], d.[DependencyKind]
                FROM [platform].[ModuleDependency] d
                INNER JOIN [platform].[Module] root ON root.[Id] = d.[ModuleId]
                WHERE root.[Code] = @ModuleCode
                UNION ALL
                SELECT c.[RootModuleId], d.[DependsOnModuleId], d.[DependencyKind]
                FROM dependency_chain c
                INNER JOIN [platform].[ModuleDependency] d ON d.[ModuleId] = c.[DependencyModuleId]
                WHERE c.[DependencyKind] = 'Hard'
            )
            SELECT CASE
                       WHEN m.[State] <> 'Ready'
                            OR m.[SystemEnabled] <> 1
                            OR COALESCE(g.[Enabled], 0) <> 1
                            OR EXISTS
                            (
                                SELECT 1
                                FROM dependency_chain c
                                INNER JOIN [platform].[Module] dependencyModule
                                  ON dependencyModule.[Id] = c.[DependencyModuleId]
                                LEFT JOIN [platform].[UserModuleGrant] dependencyGrant
                                  ON dependencyGrant.[ModuleId] = dependencyModule.[Id]
                                 AND dependencyGrant.[UserId] = @UserId
                                WHERE c.[DependencyKind] = 'Hard'
                                  AND (dependencyModule.[State] <> 'Ready'
                                       OR dependencyModule.[SystemEnabled] <> 1
                                       OR COALESCE(dependencyGrant.[Enabled], 0) <> 1)
                            ) THEN 0
                       WHEN @Role IN ('User', 'SuperAdmin') THEN 1
                       WHEN @Role = 'Admin'
                            AND EXISTS
                            (
                                SELECT 1
                                FROM [platform].[AdminPermission] ap
                                INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
                                WHERE ap.[UserId] = @UserId
                                  AND ap.[Effect] = 'Allow'
                                  AND p.[EffectiveStatus] = 'Resolved'
                                  AND p.[ActionKey] IN ({string.Join(',', keyNames)})
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM [platform].[AdminPermission] ap
                                INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
                                WHERE ap.[UserId] = @UserId
                                  AND ap.[Effect] = 'Deny'
                                  AND p.[ActionKey] IN ({string.Join(',', keyNames)})
                            ) THEN 1
                       ELSE 2
                   END
            FROM [platform].[Module] m
            LEFT JOIN [platform].[UserModuleGrant] g
              ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId
            WHERE m.[Code] = @ModuleCode
            OPTION (MAXRECURSION 32);
            """;
        command.Parameters.Add("@Role", SqlDbType.VarChar, 32).Value = actor.Role;
        command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@ModuleCode", SqlDbType.VarChar, 64).Value = moduleCode;
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) switch
        {
            1 => SqlCapabilityStatus.Allowed,
            2 => SqlCapabilityStatus.PermissionDenied,
            _ => SqlCapabilityStatus.ModuleUnavailable
        };
    }
}

internal enum SqlCapabilityStatus
{
    ModuleUnavailable = 0,
    Allowed = 1,
    PermissionDenied = 2
}
