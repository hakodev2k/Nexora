using System.Text.Json.Serialization;

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
    string ETag,
    Guid? TagId,
    string? TagName);

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
    string ETag,
    Guid? TagId,
    string? TagName);

public sealed record DocumentPageResponse(IReadOnlyList<DocumentSummaryResponse> Items, string? NextCursor);

public sealed record DocumentCreateRequest(string Title, string DocumentType, string EditorMode, string? Body, Guid? TagId = null);

public sealed record DocumentSaveRequest(string Title, string? Body, string? ChangeNote = null, DocumentTagChangeRequest? Tag = null);

public sealed record DocumentTagChangeRequest([property: JsonRequired] Guid? Id);

public sealed record DocumentTransitionRequest(string Status);

public sealed record DocumentVersionResponse(Guid Id, Guid PageId, long VersionNumber, string Title,
    string DocumentType, string EditorMode, string SavedStatus, string? ChangeNote, DateTimeOffset CreatedAt,
    Guid? CreatedByUserId, bool TagSnapshotCaptured, Guid? TagId, string? TagName);

public sealed record DocumentVersionsResponse(IReadOnlyList<DocumentVersionResponse> Items, string? NextCursor);

public sealed record DocumentVersionPreviewResponse(DocumentVersionResponse Snapshot, string Body);
