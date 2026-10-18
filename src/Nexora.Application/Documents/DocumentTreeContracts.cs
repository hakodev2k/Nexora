using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Documents;

public sealed record DocumentTreeMember(Guid Id,string Kind,string? Title,string? DocumentType,string State,int Depth);
public sealed record DocumentTreePreview(Guid Id,string Kind,string Operation,string ETag,string PreviewToken,bool DetailsHidden,
    int? AffectedCount,IReadOnlyList<DocumentTreeMember>? Members);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentTreeCommand([property:JsonRequired]bool Confirm,[property:JsonRequired]string PreviewToken,string? Confirmation=null);
public sealed record DocumentTrashEntry(Guid Id,string Kind,string? Title,string? DocumentType,DateTimeOffset DeletedAt,string ETag);
public sealed record DocumentTrashPage(IReadOnlyList<DocumentTrashEntry> Items,string? NextCursor);
public interface IDocumentTreeService
{
    IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<DocumentTrashPage> TrashRoots(IdentityPrincipal actor,string? cursor);
    IdentityOperationResult<DocumentTreePreview> Preview(IdentityPrincipal actor,string kind,Guid id,string operation);
    IdentityOperationResult<object?> Execute(IdentityPrincipal actor,string kind,Guid id,string operation,string? etag,DocumentTreeCommand command,string key,string? trace);
}
