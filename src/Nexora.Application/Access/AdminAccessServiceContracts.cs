using Nexora.Application.Identity;

namespace Nexora.Application.Access;

/// <summary>
/// Operational account metadata exposed to SuperAdmin. Business-resource
/// fields are intentionally absent from these projections.
/// </summary>
public sealed record AdminUserRecord(
    Guid Id,
    string Email,
    string DisplayName,
    string State,
    bool EmailConfirmed,
    string Role,
    Guid? PersonalSpaceId,
    string? PersonalSpaceState,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record AdminUserPage(IReadOnlyList<AdminUserRecord> Items, string? NextCursor);

public sealed record AdminActionGrantRecord(
    string ActionKey,
    string Effect,
    string Status,
    DateTimeOffset UpdatedAt);

public sealed record AdminModuleGrantRecord(
    string Code,
    bool Enabled,
    string State,
    bool SystemEnabled);

public sealed record AdminUserAccess(
    AdminUserRecord User,
    IReadOnlyList<AdminActionGrantRecord> ActionGrants,
    IReadOnlyList<AdminModuleGrantRecord> ModuleGrants);

public sealed record AdminRoleCommand(string Role, string? IfMatch);

public sealed record AdminActionGrantCommand(string ActionKey, string Effect, string? IfMatch);

public sealed record AdminModuleGrantCommand(string ModuleCode, bool Enabled, string? IfMatch);

public sealed record AdminDisableCommand(string Confirmation, string? IfMatch);

public interface IAdminAccessService
{
    IdentityOperationResult<AdminUserPage> ListUsers(IdentityPrincipal actor, string? query = null, int? limit = null);
    IdentityOperationResult<AdminUserAccess> GetUserAccess(IdentityPrincipal actor, Guid userId);
    IdentityOperationResult<AdminUserAccess> SetRole(IdentityPrincipal actor, Guid userId, AdminRoleCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<AdminUserAccess> SetActionGrant(IdentityPrincipal actor, Guid userId, AdminActionGrantCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<AdminUserAccess> SetModuleGrant(IdentityPrincipal actor, Guid userId, AdminModuleGrantCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> DisableUser(IdentityPrincipal actor, Guid userId, AdminDisableCommand command,
        string? idempotencyKey = null, string? traceId = null);
}
