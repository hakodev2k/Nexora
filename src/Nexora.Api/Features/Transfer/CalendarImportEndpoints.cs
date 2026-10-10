using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;

namespace Nexora.Api.Features.Transfer;

public static class CalendarImportEndpoints
{
    public static WebApplication MapCalendarImportEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/transfer/calendar").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities",(HttpContext c,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapPost("/imports",(HttpContext c,CalendarImportPreview body,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Preview(a,body,Key(c),c.TraceIdentifier)));
        api.MapGet("/imports",(HttpContext c,Guid? cursor,string? state,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.List(a,cursor,state)));
        api.MapGet("/imports/{id:guid}",(HttpContext c,Guid id,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Get(a,id)));
        api.MapGet("/imports/{id:guid}/rows",(HttpContext c,Guid id,int? cursor,string? outcome,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Rows(a,id,cursor,outcome)));
        api.MapPost("/imports/{id:guid}/commit",(HttpContext c,Guid id,ImportConfirmation body,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Commit(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        api.MapPost("/imports/{id:guid}/cancel",(HttpContext c,Guid id,ImportConfirmation body,ICalendarImportService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Cancel(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));var result=auth.Succeeded&&auth.Value is not null?operation(auth.Value):IdentityOperationResult<T>.Failure(auth.Code,auth.StatusCode,auth.Title);
        var etag=result.Value switch{ImportBatch batch=>batch.ETag,ImportAcknowledgement ack=>ack.ETag,_=>null};if(etag is not null)c.Response.Headers.ETag=etag;
        c.Response.Headers.CacheControl="no-store";return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
