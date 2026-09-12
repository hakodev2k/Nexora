using Nexora.Application.Identity;

namespace Nexora.Application.Files;

public sealed record FileRecord(
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

public sealed record FilePage(IReadOnlyList<FileRecord> Items, string? NextCursor);

public sealed record FileUploadCommand(string OriginalName, string MediaType, long ExpectedBytes);

public sealed record FileUploadSessionRecord(
    Guid Id,
    long ExpectedBytes,
    long ReceivedBytes,
    string State,
    DateTimeOffset ExpiresAt,
    string UploadHandle,
    string ETag);

public sealed record FileRenameCommand(string OriginalName);

public sealed record FileReferenceCommand(
    string ResourceType,
    Guid ResourceId,
    long? VersionNumber,
    string Purpose,
    string ReferenceKey);

public sealed record FileReferenceRecord(
    Guid Id,
    Guid FileObjectId,
    string ResourceType,
    Guid ResourceId,
    long? VersionNumber,
    string Purpose,
    string ReferenceKey,
    DateTimeOffset CreatedAt,
    string ETag);

public sealed record FileDownload(Stream Content, string MediaType, string DownloadName, long Length);

public interface IFileService
{
    IdentityOperationResult<FilePage> List(IdentityPrincipal actor, int? limit = null);
    IdentityOperationResult<FileRecord> Get(IdentityPrincipal actor, Guid fileId);
    IdentityOperationResult<FileUploadSessionRecord> InitiateUpload(IdentityPrincipal actor, FileUploadCommand command,
        string? idempotencyKey = null, string? traceId = null);
    Task<IdentityOperationResult<FileRecord>> CompleteUploadAsync(IdentityPrincipal actor, Guid uploadSessionId,
        string uploadHandle, Stream content, long? contentLength, string? idempotencyKey = null, string? traceId = null,
        CancellationToken cancellationToken = default);
    IdentityOperationResult<object?> CancelUpload(IdentityPrincipal actor, Guid uploadSessionId, string uploadHandle,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FileRecord> Rename(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        FileRenameCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FileReferenceRecord> Attach(IdentityPrincipal actor, Guid fileId,
        FileReferenceCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Detach(IdentityPrincipal actor, Guid referenceId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FileDownload> OpenContent(IdentityPrincipal actor, Guid fileId, bool inline);
    IdentityOperationResult<object?> Trash(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FileRecord> Restore(IdentityPrincipal actor, Guid fileId, Guid deletionBatchId,
        string? ifMatch, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Purge(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
}
