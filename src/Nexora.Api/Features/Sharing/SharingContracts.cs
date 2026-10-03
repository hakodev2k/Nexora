namespace Nexora.Api.Features.Sharing;

public sealed record ShareLinkRequest(
    string ResourceType,
    Guid ResourceId,
    string Mode,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<Guid>? AllowedUserIds,
    bool NoExpiry = false);

public sealed record ShareLinkResponse(
    Guid Id,
    string ResourceType,
    Guid ResourceId,
    string Mode,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string ProjectionVersion,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    IReadOnlyList<Guid> AllowedUserIds,
    string? Token);

public sealed record ShareLinkPageResponse(IReadOnlyList<ShareLinkResponse> Items, string? NextCursor);

public sealed record SharedTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset? DueAt,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Priority,
    string TagsJson,
    bool IsOverdue);

public sealed record SharedProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Priority,
    string TagsJson,
    IReadOnlyList<SharedTaskResponse> Tasks);

public sealed record SharedDocumentResponse(
    Guid Id,
    string Title,
    string DocumentType,
    string EditorMode,
    string Body,
    string Status,
    long VersionNumber,
    DateTimeOffset UpdatedAt);

public sealed record SharedResourceResponse(
    string ResourceType,
    Guid ResourceId,
    string Mode,
    DateTimeOffset? ExpiresAt,
    string ProjectionVersion,
    SharedProjectResponse? Project,
    SharedDocumentResponse? Document);
