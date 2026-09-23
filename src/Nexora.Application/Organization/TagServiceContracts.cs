using Nexora.Application.Identity;

namespace Nexora.Application.Organization;

public sealed record TagRecord(
    Guid Id,
    string Namespace,
    string Name,
    string? Color,
    int UsageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record TagPage(IReadOnlyList<TagRecord> Items, string? NextCursor);

public sealed record TagCreateCommand(string Namespace, string Name, string? Color);

public sealed record TagRenameCommand(string Name, string? Color);

public interface ITagService
{
    IdentityOperationResult<TagPage> List(IdentityPrincipal actor, string? tagNamespace = null,
        string? query = null, int? limit = null);

    IdentityOperationResult<TagRecord> Create(IdentityPrincipal actor, TagCreateCommand command,
        string? idempotencyKey = null, string? traceId = null);

    IdentityOperationResult<TagRecord> Rename(IdentityPrincipal actor, Guid tagId, string? ifMatch,
        TagRenameCommand command, string? idempotencyKey = null, string? traceId = null);

    IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid tagId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
}
