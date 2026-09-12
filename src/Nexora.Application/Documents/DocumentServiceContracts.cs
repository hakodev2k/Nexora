using Nexora.Application.Identity;

namespace Nexora.Application.Documents;

public sealed record DocumentSummary(
    Guid Id,
    string Title,
    string DocumentType,
    string EditorMode,
    string Status,
    string? PreArchiveStatus,
    long VersionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record DocumentDetail(
    Guid Id,
    string Title,
    string DocumentType,
    string EditorMode,
    string Body,
    string Status,
    string? PreArchiveStatus,
    long VersionNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record DocumentPage(IReadOnlyList<DocumentSummary> Items, string? NextCursor);

public sealed record DocumentCreateCommand(
    string Title,
    string DocumentType,
    string EditorMode,
    string? Body);

public sealed record DocumentSaveCommand(
    string Title,
    string? Body,
    string? ChangeNote = null);

public sealed record DocumentTransitionCommand(string Status);

public interface IDocumentService
{
    IdentityOperationResult<DocumentPage> List(IdentityPrincipal actor, string? status = null, int? limit = null);
    IdentityOperationResult<DocumentDetail> Get(IdentityPrincipal actor, Guid documentId);
    IdentityOperationResult<DocumentDetail> Create(IdentityPrincipal actor, DocumentCreateCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<DocumentDetail> Save(IdentityPrincipal actor, Guid documentId, string? ifMatch,
        DocumentSaveCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<DocumentDetail> Transition(IdentityPrincipal actor, Guid documentId, string? ifMatch,
        DocumentTransitionCommand command, string? idempotencyKey = null, string? traceId = null);
}
