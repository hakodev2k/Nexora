namespace Nexora.Api.Features.Organization;

public sealed record TagCreateRequest(string Namespace, string Name, string? Color);

public sealed record TagRenameRequest(string Name, string? Color);

public sealed record TagResponse(
    Guid Id,
    string Namespace,
    string Name,
    string? Color,
    int UsageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record TagPageResponse(IReadOnlyList<TagResponse> Items, string? NextCursor);
