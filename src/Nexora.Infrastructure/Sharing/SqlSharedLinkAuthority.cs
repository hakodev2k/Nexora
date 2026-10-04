using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Domain.Access;

namespace Nexora.Infrastructure.Sharing;

/// <summary>A validated link supplies the owner/resource; it never grants viewer SELF access.</summary>
internal sealed record SharedLinkSourceContext(Guid OwnerId, Guid ResourceId);

internal static class SqlSharedLinkAuthority
{
    internal static bool SourceActionsAllowed(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, string moduleCode)
    {
        var prefix = moduleCode == "FX11" ? "projects.project" : "documents.page";
        // Each action is a separate requirement, including a source share grant.
        foreach (var (module, action) in new[] { ("FX04", "sharing.link.read"), (moduleCode, prefix + ".read"), (moduleCode, prefix + ".share") })
            if (!Allowed(connection, transaction, ownerId, module, action)) return false;
        return true;
    }

    private static bool Allowed(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, string moduleCode, string action)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH dependency_chain AS (
                SELECT d.DependsOnModuleId AS DependencyModuleId,d.DependencyKind
                FROM [platform].[ModuleDependency] d JOIN [platform].[Module] m ON m.Id=d.ModuleId WHERE m.Code=@Module
                UNION ALL
                SELECT d.DependsOnModuleId,d.DependencyKind FROM dependency_chain c
                JOIN [platform].[ModuleDependency] d ON d.ModuleId=c.DependencyModuleId WHERE c.DependencyKind='Hard'
            )
            SELECT CASE WHEN m.State='Ready' AND m.SystemEnabled=1 AND m.SharingEnabled=1
                AND u.State='Active' AND u.IsDeleted=0 AND u.EmailConfirmed=1 AND p.State='Active'
                AND g.Enabled=1 AND permission.EffectiveStatus='Resolved'
                AND NOT EXISTS(SELECT 1 FROM dependency_chain c
                    JOIN [platform].[Module] dep ON dep.Id=c.DependencyModuleId
                    LEFT JOIN [platform].[UserModuleGrant] dg ON dg.ModuleId=dep.Id AND dg.UserId=u.Id
                    WHERE c.DependencyKind='Hard' AND (dep.State<>'Ready' OR dep.SystemEnabled<>1 OR COALESCE(dg.Enabled,0)<>1))
                AND (currentRole.Code IN ('User','SuperAdmin') OR (currentRole.Code='Admin' AND @Grantable=1
                    AND EXISTS(SELECT 1 FROM [platform].[AdminPermission] ap WHERE ap.UserId=u.Id AND ap.PermissionId=permission.Id AND ap.Effect='Allow')
                    AND NOT EXISTS(SELECT 1 FROM [platform].[AdminPermission] ap WHERE ap.UserId=u.Id AND ap.PermissionId=permission.Id AND ap.Effect='Deny')))
                THEN 1 ELSE 0 END
            FROM [platform].[PersonalSpace] p
            JOIN [identity].[User] u ON u.Id=p.UserId
            JOIN [platform].[Module] m ON m.Code=@Module
            JOIN [platform].[UserModuleGrant] g ON g.UserId=u.Id AND g.ModuleId=m.Id
            JOIN [platform].[Permission] permission ON permission.ActionKey=@Action
            CROSS APPLY(SELECT TOP(1) r.Code FROM [identity].[UserRole] ur
                JOIN [identity].[Role] r ON r.Id=ur.RoleId WHERE ur.UserId=u.Id
                ORDER BY CASE r.Code WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC) currentRole
            WHERE p.Id=@Owner OPTION(MAXRECURSION 32);
            """;
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = ownerId;
        command.Parameters.Add("@Module", SqlDbType.VarChar, 64).Value = moduleCode;
        command.Parameters.Add("@Action", SqlDbType.NVarChar, 160).Value = action;
        command.Parameters.Add("@Grantable", SqlDbType.Bit).Value = ActionGrantPolicy.IsAdminGrantable(action);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }
}
