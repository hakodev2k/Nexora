using Nexora.Application.Identity;

namespace Nexora.Application.Discovery;

public sealed record RecentSourceProjection(string ResourceType, Guid ResourceId, string Title, string Status);
public sealed record RecentItem(Guid Id, string ResourceType, Guid ResourceId, string Title, string Status, DateTimeOffset LastOpenedAt);
public sealed record RecentPage(IReadOnlyList<RecentItem> Items, string? NextCursor, string? ClearToken);
public sealed record RecentClearPreview(string Token);
public sealed record RecentCapabilities(bool CanRead, bool CanClear);
public interface IRecentService
{
    IdentityOperationResult<RecentCapabilities> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<RecentPage> List(IdentityPrincipal actor, string? resourceType = null, string? cursor = null);
    IdentityOperationResult<RecentClearPreview> PreviewClear(IdentityPrincipal actor);
    IdentityOperationResult<object?> Clear(IdentityPrincipal actor, string token, string key, string? trace);
}
