using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexora.Api.Features.Access;

/// <summary>
/// The preview endpoint deliberately accepts one access-change kind at a
/// time. Keeping the shapes separate makes a signed preview unambiguous and
/// prevents a client from quietly combining role, permission, and module
/// changes in one commit.
/// </summary>
public sealed record AdminAccessPreviewRequest(
    string? Kind,
    string? Role,
    JsonElement? Changes)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record AdminRoleCommitRequest(string? Kind, string? Role, string? PreviewToken)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record AdminPermissionCommitRequest(
    string? Kind,
    JsonElement? Changes,
    string? PreviewToken)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; init; }
}

public sealed record AdminModuleGrantCommitRequest(
    string? Kind,
    JsonElement? Changes,
    string? PreviewToken)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extra { get; init; }
}

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
public sealed record AdminModuleGrantResponse(Guid ModuleId, string Code, bool Enabled, string State, bool SystemEnabled);
public sealed record AdminUserAccessResponse(
    AdminUserResponse User,
    IReadOnlyList<AdminActionGrantResponse> ActionGrants,
    IReadOnlyList<AdminModuleGrantResponse> ModuleGrants);

public sealed record AdminAccessPreviewResponse(
    string PreviewToken,
    DateTimeOffset ExpiresAt,
    string ETag,
    IReadOnlyList<AdminAccessChangeDiffResponse> Changes,
    IReadOnlyList<AdminAccessBlockerResponse> Blockers);

public sealed record AdminAccessChangeDiffResponse(string Field, string? Before, string? After);
public sealed record AdminAccessBlockerResponse(string Code, string Message);
