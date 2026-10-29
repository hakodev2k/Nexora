using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;

namespace Nexora.Api.Features.Transfer;

public static class DocumentExportEndpoints
{
    public static WebApplication MapDocumentExportEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();
        api.MapPost("/documents/{id:guid}/exports/preview",(HttpContext c,Guid id,DocumentExportSelection body,IDocumentExportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.Preview(a,id,c.Request.Headers.IfMatch.ToString(),body)));
        api.MapPost("/documents/{id:guid}/exports",(HttpContext c,Guid id,DocumentExportRequest body,IDocumentExportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.Request(a,id,c.Request.Headers.IfMatch.ToString(),body,c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapGet("/document-exports/{id:guid}",(HttpContext c,Guid id,IDocumentExportService s,IIdentityService identity,SessionCookieService cookies)
            =>Run(c,identity,cookies,a=>s.Get(a,id)));
        api.MapGet("/document-exports/{id:guid}/content",(HttpContext c,Guid id,IDocumentExportService s,IIdentityService identity,SessionCookieService cookies)=>
        {
            Private(c);var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
            if(!auth.Succeeded||auth.Value is null)return new ApiResult<object?>(false,null,auth.Code,auth.StatusCode,auth.Title).ToHttp(c);
            var result=s.Content(auth.Value,id);return result.Succeeded&&result.Value is {} download
                ?Results.File(download.Content,download.MediaType,download.FileName,enableRangeProcessing:false)
                :new ApiResult<object?>(false,null,result.Code,result.StatusCode,result.Title).ToHttp(c);
        });
        return app;
    }
    private static void Private(HttpContext c){c.Response.Headers.CacheControl="no-store";c.Response.Headers["X-Content-Type-Options"]="nosniff";}
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> action)
    {
        Private(c);var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        if(!auth.Succeeded||auth.Value is null)return new ApiResult<T>(false,default,auth.Code,auth.StatusCode,auth.Title).ToHttp(c);
        var result=action(auth.Value);if(result.Value is DocumentExportJob job)c.Response.Headers.ETag=job.ETag;
        if(result.Succeeded&&result.Acknowledgement is {} ack)return new ApiResult<CommittedResourceAcknowledgement>(true,ack,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
