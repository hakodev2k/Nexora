using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Api.Features.Learning;

public static class WorkLogEndpoints
{
    public static WebApplication MapWorkLogEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/learning/work-logs").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities",(HttpContext c,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapGet("/",(HttpContext c,string? status,string? query,DateOnly? from,DateOnly? to,string? cursor,string? category,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.List(a,status,query,from,to,cursor,category)));
        api.MapGet("/report",(HttpContext c,DateOnly? from,DateOnly? to,string? status,string? query,string? category,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Report(a,from,to,status,query,category)));
        api.MapGet("/{id:guid}",(HttpContext c,Guid id,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Get(a,id)));
        api.MapGet("/{id:guid}/history",(HttpContext c,Guid id,string? cursor,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.History(a,id,cursor)));
        api.MapPost("/",(HttpContext c,WorkLogCommand body,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Save(a,null,null,body,Key(c),c.TraceIdentifier)));
        api.MapPut("/{id:guid}",(HttpContext c,Guid id,WorkLogCommand body,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Save(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        foreach(var operation in new[]{"archive","unarchive"})
        {
            var op=operation;
            api.MapPost("/{id:guid}/"+op,(HttpContext c,Guid id,WorkLogConfirmation body,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Transition(a,id,c.Request.Headers.IfMatch.ToString(),op,body,Key(c),c.TraceIdentifier)));
        }
        foreach(var operation in new[]{"trash","restore","purge"})
        {
            var op=operation;
            api.MapPost("/{id:guid}/"+op+"-preview",(HttpContext c,Guid id,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.PreviewLifecycle(a,id,c.Request.Headers.IfMatch.ToString(),op)));
            api.MapPost("/{id:guid}/"+op,(HttpContext c,Guid id,WorkLogLifecycleCommand body,IWorkLogService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Lifecycle(a,id,c.Request.Headers.IfMatch.ToString(),op,body,Key(c),c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result=auth.Succeeded && auth.Value is not null?operation(auth.Value):IdentityOperationResult<T>.Failure(auth.Code,auth.StatusCode,auth.Title);
        if(result.Value is WorkLog row) c.Response.Headers.ETag=row.ETag;
        if(result.Succeeded && result.Acknowledgement is {} ack)
            return new ApiResult<CommittedResourceAcknowledgement>(true,ack,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
