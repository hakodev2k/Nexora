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
    Guid ModuleId,
    string Code,
    bool Enabled,
    string State,
    bool SystemEnabled);

public sealed record AdminUserAccess(
    AdminUserRecord User,
    IReadOnlyList<AdminActionGrantRecord> ActionGrants,
    IReadOnlyList<AdminModuleGrantRecord> ModuleGrants);

public sealed record AdminChangeDiff(string Field, string? Before, string? After);

public sealed record AdminAccessBlocker(string Code, string Message);

public sealed record AdminAccessPreview(
    string PreviewToken,
    DateTimeOffset ExpiresAt,
    string ETag,
    IReadOnlyList<AdminChangeDiff> Changes,
    IReadOnlyList<AdminAccessBlocker> Blockers);

public sealed record AdminPermissionChange(string ActionKey, string Effect);

public sealed record AdminModuleGrantChange(Guid ModuleId, bool Enabled);

/// <summary>
/// A normalized, single-kind access change. The service only accepts this
/// shape after the API boundary has rejected mixed/unknown request fields.
/// </summary>
public sealed record AdminAccessChange(
    string Kind,
    string? Role,
    IReadOnlyList<AdminPermissionChange> PermissionChanges,
    IReadOnlyList<AdminModuleGrantChange> ModuleChanges);

public sealed record AdminRoleCommand(string Role, string PreviewToken);

public sealed record AdminActionGrantCommand(IReadOnlyList<AdminPermissionChange> Changes, string PreviewToken);

public sealed record AdminModuleGrantCommand(IReadOnlyList<AdminModuleGrantChange> Changes, string PreviewToken);

public sealed record AdminDisableCommand(string Confirmation, string? IfMatch);

public interface IAdminAccessService
{
    IdentityOperationResult<AdminUserPage> ListUsers(IdentityPrincipal actor, string? query = null, int? limit = null);
    IdentityOperationResult<AdminUserAccess> GetUserAccess(IdentityPrincipal actor, Guid userId);
    IdentityOperationResult<AdminAccessPreview> PreviewAccess(IdentityPrincipal actor, Guid userId, AdminAccessChange change);
    IdentityOperationResult<AdminUserAccess> SetRole(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminRoleCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<AdminUserAccess> SetActionGrant(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminActionGrantCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<AdminUserAccess> SetModuleGrant(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminModuleGrantCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> DisableUser(IdentityPrincipal actor, Guid userId, AdminDisableCommand command,
        string? idempotencyKey = null, string? traceId = null);
}
