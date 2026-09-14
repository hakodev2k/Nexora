using Nexora.Application.Identity;

namespace Nexora.Application.Sharing;

public sealed record ShareLinkRecord(
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
    string? RawToken = null);

public sealed record ShareLinkPage(IReadOnlyList<ShareLinkRecord> Items, string? NextCursor);

public sealed record ShareLinkCreateCommand(
    string ResourceType,
    Guid ResourceId,
    string Mode,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<Guid>? AllowedUserIds,
    bool NoExpiry = false);

public sealed record ShareLinkUpdateCommand(
    string Mode,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<Guid>? AllowedUserIds,
    bool NoExpiry = false);

public sealed record SharedTaskProjection(
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

public sealed record SharedProjectProjection(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string Priority,
    string TagsJson,
    IReadOnlyList<SharedTaskProjection> Tasks);

public sealed record SharedDocumentProjection(
    Guid Id,
    string Title,
    string DocumentType,
    string EditorMode,
    string Body,
    string Status,
    long VersionNumber,
    DateTimeOffset UpdatedAt);

public sealed record SharedResource(
    string ResourceType,
    Guid ResourceId,
    string Mode,
    DateTimeOffset? ExpiresAt,
    string ProjectionVersion,
    SharedProjectProjection? Project,
    SharedDocumentProjection? Document);

public interface ISharingService
{
    IdentityOperationResult<ShareLinkPage> List(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<ShareLinkRecord> Create(IdentityPrincipal actor, ShareLinkCreateCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<ShareLinkRecord> Update(IdentityPrincipal actor, Guid shareLinkId, string? ifMatch,
        ShareLinkUpdateCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Revoke(IdentityPrincipal actor, Guid shareLinkId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<SharedResource> Resolve(string token, IdentityPrincipal? viewer);
}
