using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Domain.Access;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Authorization;

/// <summary>
/// Resolves the current SELF capability from SQL on every request. User and
/// SuperAdmin principals use the approved own-resource baseline.
/// Admin SELF is narrower: a resolved, Admin-grantable action needs an
/// explicit Allow row and any explicit Deny wins. Every hard module
/// dependency must be ready, system enabled and enabled for the same user.
/// RegistrationEnabled is a verification-time default, not a current-user
/// authorization gate. No decision is cached in Redis or in the process.
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
        if (actor is null || string.IsNullOrWhiteSpace(moduleCode) || actionKeys is null || actionKeys.Length == 0)
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
        var actions = actionKeys?
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .Select(action => action.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
        if (connection is null || actor is null || string.IsNullOrWhiteSpace(moduleCode) || actions.Length == 0)
            return SqlCapabilityStatus.ModuleUnavailable;

        // Admin SELF is an explicit grant context. A stale or hand-inserted
        // AdminPermission row cannot turn a PUBLIC/SUPER/CONTROL/SYSTEM action
        // into self access; the manifest projection is checked before SQL.
        if (string.Equals(actor.Role, "Admin", StringComparison.Ordinal) &&
            actions.Any(action => !ActionGrantPolicy.IsAdminGrantable(action)))
        {
            return SqlCapabilityStatus.PermissionDenied;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var actionParameters = new string[actions.Length];
        for (var index = 0; index < actions.Length; index++)
        {
            actionParameters[index] = "@Action" + index;
            command.Parameters.Add(actionParameters[index], SqlDbType.NVarChar, 160).Value = actions[index];
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
                            OR userRow.[State] <> 'Active'
                            OR userRow.[IsDeleted] <> 0
                            OR spaceRow.[State] <> 'Active'
                            OR
                            (
                                NOT EXISTS
                                (
                                    SELECT 1
                                    FROM [platform].[Permission] requestedPermission
                                    WHERE requestedPermission.[EffectiveStatus] = 'Resolved'
                                      AND requestedPermission.[ActionKey] IN ({string.Join(',', actionParameters)})
                                )
                            )
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
                                FROM [platform].[Permission] requestedPermission
                                WHERE requestedPermission.[EffectiveStatus] = 'Resolved'
                                  AND requestedPermission.[ActionKey] IN ({string.Join(',', actionParameters)})
                                  AND EXISTS
                                  (
                                      SELECT 1
                                      FROM [platform].[AdminPermission] adminPermission
                                      WHERE adminPermission.[PermissionId] = requestedPermission.[Id]
                                        AND adminPermission.[UserId] = @UserId
                                        AND adminPermission.[Effect] = 'Allow'
                                  )
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM [platform].[Permission] deniedAction
                                INNER JOIN [platform].[AdminPermission] deniedPermission
                                  ON deniedPermission.[PermissionId] = deniedAction.[Id]
                                 AND deniedPermission.[UserId] = @UserId
                                 AND deniedPermission.[Effect] = 'Deny'
                                WHERE deniedAction.[EffectiveStatus] = 'Resolved'
                                  AND deniedAction.[ActionKey] IN ({string.Join(',', actionParameters)})
                            ) THEN 1
                       ELSE 2
                   END
            FROM [platform].[Module] m
            LEFT JOIN [platform].[UserModuleGrant] g
              ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId
            INNER JOIN [identity].[User] userRow
              ON userRow.[Id] = @UserId
            INNER JOIN [platform].[PersonalSpace] spaceRow
              ON spaceRow.[Id] = @OwnerId AND spaceRow.[UserId] = @UserId
            WHERE m.[Code] = @ModuleCode
            OPTION (MAXRECURSION 32);
            """;
        command.Parameters.Add("@Role", SqlDbType.VarChar, 32).Value = actor.Role;
        command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@OwnerId", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
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
