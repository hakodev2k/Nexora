using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    private sealed record CreateScope(Guid? FolderId,Guid? ParentPageId,int Depth);
    // Source-owned lifecycle filtering. Direct page reads remain page.read;
    // container read grants do not authorize a private page body.
    internal const string ContainersVisible="EXISTS(SELECT 1 FROM [documents].[VisiblePageV1] visible WHERE visible.OwnerId=p.OwnerId AND visible.Id=p.Id)";
    private bool CreateScopeAllowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,DocumentCreateCommand command)=>
        CurrentAuthority(c,tx,actor,"documents.page.create")
        &&(!command.ParentPageId.HasValue||CurrentAuthority(c,tx,actor,"documents.page.read"))
        &&(!command.FolderId.HasValue||CurrentAuthority(c,tx,actor,"documents.folder.read"));
    private IdentityOperationResult<CreateScope> ResolveCreateScope(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,DocumentCreateCommand command)
    {
        if(command.FolderId==Guid.Empty||command.ParentPageId==Guid.Empty||(command.FolderId.HasValue&&command.ParentPageId.HasValue))return Failure<CreateScope>("ValidationFailed",422,"Choose either a root Folder or a parent page.");
        if(command.ParentPageId is {} parentId)
        {
            var parent=ReadDetail(c,tx,actor.OwnerId,parentId,true);
            if(parent is null)return Failure<CreateScope>("ResourceUnavailable",404,"The parent page is unavailable.");
            if(parent.Depth!=1||parent.ParentPageId.HasValue)return Failure<CreateScope>("DepthLimit",422,"A child page cannot contain another page level.");
            if(parent.Status is not("Draft" or "Published"))return Failure<CreateScope>("LifecycleLocked",409,"The parent page is readonly.");
            return IdentityOperationResult<CreateScope>.Success(new(parent.FolderId,parent.Id,2));
        }
        if(command.FolderId is {} folderId&&SqlDocumentFolderService.ReadWithin(c,tx,actor.OwnerId,folderId)is null)return Failure<CreateScope>("ResourceUnavailable",404,"The Folder is unavailable.");
        return IdentityOperationResult<CreateScope>.Success(new(command.FolderId,null,1));
    }
}
