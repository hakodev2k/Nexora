using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Authorization;

/// <summary>Current SQL account, owner, role and session for Sharing and its policy commands.</summary>
internal static class SqlCurrentActor
{
    internal static bool IsLive(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, bool requireRecentAuthentication = false)
    {
        if (actor.SessionId is null) return false;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 1
            FROM [identity].[User] u WITH(HOLDLOCK)
            JOIN [platform].[PersonalSpace] p WITH(HOLDLOCK) ON p.UserId=u.Id AND p.Id=@Owner
            JOIN [identity].[Session] s WITH(HOLDLOCK) ON s.UserId=u.Id AND s.Id=@Session
            WHERE u.Id=@User AND u.State='Active' AND u.IsDeleted=0 AND u.EmailConfirmed=1
              AND p.State='Active' AND s.RevokedAt IS NULL
              AND s.IdleExpiresAt>SYSUTCDATETIME() AND s.AbsoluteExpiresAt>SYSUTCDATETIME()
              AND s.SecurityStamp=u.SecurityStamp
              AND (@Recent=0 OR s.RecentAuthenticatedAt>=DATEADD(minute,-5,SYSUTCDATETIME()))
              AND COALESCE((SELECT TOP(1) r.Code FROM [identity].[UserRole] ur
                  JOIN [identity].[Role] r ON r.Id=ur.RoleId WHERE ur.UserId=u.Id
                  ORDER BY CASE r.Code WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC),'User')=@Role;
            """;
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        command.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = actor.UserId;
        command.Parameters.Add("@Session", SqlDbType.UniqueIdentifier).Value = actor.SessionId.Value;
        command.Parameters.Add("@Role", SqlDbType.VarChar, 32).Value = actor.Role;
        command.Parameters.Add("@Recent", SqlDbType.Bit).Value = requireRecentAuthentication;
        return command.ExecuteScalar() is not null;
    }
}
