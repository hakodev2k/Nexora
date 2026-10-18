using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;

namespace Nexora.Api.Features.Transfer;

public static class DocumentImportEndpoints
{
    public static WebApplication MapDocumentImportEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/document-imports").RequireCsrfForUnsafeMethods();
        api.MapPost("/preview",(HttpContext c,DocumentImportSelection body,IDocumentImportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.Preview(a,body)));
        api.MapPost("",(HttpContext c,DocumentImportCommand body,IDocumentImportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.Commit(a,body,c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapGet("/by-page/{id:guid}",(HttpContext c,Guid id,IDocumentImportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.GetByPage(a,id)));
        return app;
    }
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> action)
    {
        c.Response.Headers.CacheControl="no-store";c.Response.Headers["X-Content-Type-Options"]="nosniff";var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        if(!auth.Succeeded||auth.Value is null)return new ApiResult<T>(false,default,auth.Code,auth.StatusCode,auth.Title).ToHttp(c);
        var result=action(auth.Value);if(result.Value is DocumentImportReport report)c.Response.Headers.ETag=report.ETag;
        if(result.Succeeded&&result.Acknowledgement is {} ack)return new ApiResult<CommittedResourceAcknowledgement>(true,ack,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
