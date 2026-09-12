using Nexora.Application.Identity;

namespace Nexora.Application.Snippets;

public sealed record SnippetRecord(
    Guid Id,
    string Title,
    string Language,
    string Body,
    string? Description,
    long VersionNumber,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record SnippetPage(IReadOnlyList<SnippetRecord> Items, string? NextCursor);

public sealed record SnippetCommand(string Title, string Language, string Body, string? Description);

public interface ISnippetService
{
    IdentityOperationResult<SnippetPage> List(IdentityPrincipal actor, bool includeArchived = false,
        string? query = null, int? limit = null);
    IdentityOperationResult<SnippetRecord> Create(IdentityPrincipal actor, SnippetCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<SnippetRecord> Save(IdentityPrincipal actor, Guid snippetId, string? ifMatch,
        SnippetCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<SnippetRecord> Transition(IdentityPrincipal actor, Guid snippetId, string? ifMatch,
        string status, string? idempotencyKey = null, string? traceId = null);
}
