namespace Nexora.Api.Features.Files;

public sealed record FileUploadRequest(string OriginalName, string MediaType, long ExpectedBytes);

public sealed record FileResponse(
    Guid Id,
    string OriginalName,
    string MediaType,
    long ByteLength,
    string ScanState,
    string Lifecycle,
    long CurrentRevision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record FilePageResponse(IReadOnlyList<FileResponse> Items, string? NextCursor);

public sealed record FileUploadSessionResponse(
    Guid Id,
    long ExpectedBytes,
    long ReceivedBytes,
    string State,
    DateTimeOffset ExpiresAt,
    string UploadHandle,
    string ETag);

public sealed record FileRenameRequest(string OriginalName);

public sealed record FileReferenceRequest(
    string ResourceType,
    Guid ResourceId,
    long? VersionNumber,
    string Purpose,
    string ReferenceKey);

public sealed record FileReferenceResponse(
    Guid Id,
    Guid FileObjectId,
    string ResourceType,
    Guid ResourceId,
    long? VersionNumber,
    string Purpose,
    string ReferenceKey,
    DateTimeOffset CreatedAt,
    string ETag);

public sealed record FileRestoreRequest(Guid DeletionBatchId);
