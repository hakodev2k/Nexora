using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Transfer;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentExportSelection(long VersionNumber,string Format);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentExportRequest(long VersionNumber,string Format,string PreviewToken,[property:JsonRequired]bool Confirmed,string[] AcceptedWarnings);
public sealed record DocumentExportPreview(Guid PageId,long VersionNumber,string Format,string ETag,string[] Warnings,string PreviewToken,DateTimeOffset ExpiresAt,int ByteLength);
public sealed record DocumentExportJob(Guid Id,Guid PageId,long VersionNumber,string Format,int CodecVersion,string State,DateTimeOffset CreatedAt,DateTimeOffset ExpiresAt,string ETag);
public sealed record DocumentExportDownload(byte[] Content,string MediaType,string FileName);
public interface IDocumentExportService
{
    IdentityOperationResult<DocumentExportPreview> Preview(IdentityPrincipal actor,Guid pageId,string? etag,DocumentExportSelection selection);
    IdentityOperationResult<object?> Request(IdentityPrincipal actor,Guid pageId,string? etag,DocumentExportRequest body,string key,string? trace);
    IdentityOperationResult<DocumentExportJob> Get(IdentityPrincipal actor,Guid jobId);
    IdentityOperationResult<DocumentExportDownload> Content(IdentityPrincipal actor,Guid jobId);
}
