using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Documents;

public sealed record DocumentFolder(Guid Id,string Title,Guid? ParentFolderId,int Depth,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt,string ETag);
public sealed record DocumentFolders(IReadOnlyList<DocumentFolder> Items,string? NextCursor);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentFolderCreate(string Title,Guid? ParentFolderId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DocumentFolderRename(string Title);
public interface IDocumentFolderService
{
    IdentityOperationResult<DocumentFolders> ListFolders(IdentityPrincipal actor,Guid? parentFolderId,string? cursor);
    IdentityOperationResult<DocumentFolder> GetFolder(IdentityPrincipal actor,Guid id);
    IdentityOperationResult<object?> CreateFolder(IdentityPrincipal actor,DocumentFolderCreate command,string key,string? trace);
    IdentityOperationResult<object?> RenameFolder(IdentityPrincipal actor,Guid id,string? etag,DocumentFolderRename command,string key,string? trace);
}
