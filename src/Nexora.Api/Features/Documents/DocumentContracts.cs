namespace Nexora.Api.Features.Documents;

public sealed record DocumentSummaryResponse(
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

public sealed record DocumentResponse(
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

public sealed record DocumentPageResponse(IReadOnlyList<DocumentSummaryResponse> Items, string? NextCursor);

public sealed record DocumentCreateRequest(string Title, string DocumentType, string EditorMode, string? Body);

public sealed record DocumentSaveRequest(string Title, string? Body, string? ChangeNote = null);

public sealed record DocumentTransitionRequest(string Status);
