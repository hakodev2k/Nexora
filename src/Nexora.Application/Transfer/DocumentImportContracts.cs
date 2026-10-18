using System.Text.Json.Serialization;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Application.Transfer;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentImportSelection(Guid FileId,string FileETag,string EditorMode);
public sealed record DocumentImportPreview(Guid FileId,string FileETag,int ParserVersion,string EditorMode,string Body,string[] Warnings,string PreviewToken,DateTimeOffset ExpiresAt);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentImportCommand(Guid FileId,string FileETag,string PreviewToken,[property:JsonRequired]bool Confirmed,string[] AcceptedWarnings,[property:JsonRequired]bool DraftEdited,DocumentCreateCommand Page);
public sealed record DocumentImportReport(Guid Id,Guid PageId,Guid FileId,int ParserVersion,string EditorMode,string State,DateTimeOffset CreatedAt,string ETag);
public interface IDocumentImportService
{
    IdentityOperationResult<DocumentImportPreview> Preview(IdentityPrincipal actor,DocumentImportSelection selection);
    IdentityOperationResult<object?> Commit(IdentityPrincipal actor,DocumentImportCommand command,string key,string? trace);
    IdentityOperationResult<DocumentImportReport> GetByPage(IdentityPrincipal actor,Guid pageId);
}
