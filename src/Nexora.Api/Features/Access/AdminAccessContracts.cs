namespace Nexora.Api.Features.Access;

public sealed record AdminRoleRequest(string Role, string? IfMatch);
public sealed record AdminActionGrantRequest(string ActionKey, string Effect, string? IfMatch);
public sealed record AdminModuleGrantRequest(bool Enabled, string? IfMatch);
public sealed record AdminDisableRequest(string Confirmation, string? IfMatch);

public sealed record AdminUserResponse(
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

public sealed record AdminUserPageResponse(IReadOnlyList<AdminUserResponse> Items, string? NextCursor);

public sealed record AdminActionGrantResponse(string ActionKey, string Effect, string Status, DateTimeOffset UpdatedAt);
public sealed record AdminModuleGrantResponse(string Code, bool Enabled, string State, bool SystemEnabled);
public sealed record AdminUserAccessResponse(
    AdminUserResponse User,
    IReadOnlyList<AdminActionGrantResponse> ActionGrants,
    IReadOnlyList<AdminModuleGrantResponse> ModuleGrants);
