using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Documents;

public static class DocumentTopologyEndpoints
{
    public static WebApplication MapDocumentTopologyEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/documents").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities",(HttpContext c,IDocumentTreeService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapGet("/trash",(HttpContext c,string? cursor,IDocumentTreeService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.TrashRoots(a,cursor)));
        api.MapGet("/folders",(HttpContext c,Guid? parentFolderId,string? cursor,IDocumentFolderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.ListFolders(a,parentFolderId,cursor)));
        api.MapGet("/folders/{id:guid}",(HttpContext c,Guid id,IDocumentFolderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.GetFolder(a,id)));
        api.MapPost("/folders",(HttpContext c,DocumentFolderCreate body,IDocumentFolderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.CreateFolder(a,body,Key(c),c.TraceIdentifier)));
        api.MapPut("/folders/{id:guid}",(HttpContext c,Guid id,DocumentFolderRename body,IDocumentFolderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.RenameFolder(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        foreach(var kind in new[]{"Page","Folder"})
        foreach(var operation in new[]{"archive","unarchive","trash","restore","purge"})
        {
            if(kind=="Folder"&&operation is "archive" or "unarchive")continue;
            var selectedKind=kind;var selectedOperation=operation;var path=(kind=="Page"?"/pages/":"/folders/")+"{id:guid}/";
            api.MapPost(path+"preview-"+selectedOperation,(HttpContext c,Guid id,IDocumentTreeService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Preview(a,selectedKind,id,selectedOperation)));
            api.MapPost(path+selectedOperation,(HttpContext c,Guid id,DocumentTreeCommand body,IDocumentTreeService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Execute(a,selectedKind,id,selectedOperation,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));if(!auth.Succeeded||auth.Value is null)return new ApiResult<T>(false,default,auth.Code,auth.StatusCode,auth.Title).ToHttp(c);
        var result=operation(auth.Value);if(result.Value is DocumentFolder folder)c.Response.Headers.ETag=folder.ETag;
        if(result.Succeeded&&result.Acknowledgement is {} acknowledgement)return new ApiResult<CommittedResourceAcknowledgement>(true,acknowledgement,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
