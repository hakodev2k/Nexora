using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Automation;
using Nexora.Application.Identity;
namespace Nexora.Api.Features.Automation;

public static class AutomationRunEndpoints
{
    public static WebApplication MapAutomationRunEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/automation").RequireCsrfForUnsafeMethods(16*1024);
        api.MapPost("/definitions/{id:guid}/enable",(HttpContext c,Guid id,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.SetEnabled(a,id,true,c.Request.Headers.IfMatch.ToString(),c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapPost("/definitions/{id:guid}/disable",(HttpContext c,Guid id,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.SetEnabled(a,id,false,c.Request.Headers.IfMatch.ToString(),c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapPost("/definitions/{id:guid}/runs",(HttpContext c,Guid id,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Start(a,id,c.Request.Headers.IfMatch.ToString(),c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapGet("/definitions/{id:guid}/runs",(HttpContext c,Guid id,string? cursor,string? state,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Runs(a,id,cursor,state)));
        api.MapGet("/runs/{id:guid}",(HttpContext c,Guid id,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Run(a,id)));
        api.MapPost("/runs/{id:guid}/cancel",(HttpContext c,Guid id,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Cancel(a,id,c.Request.Headers.IfMatch.ToString(),c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        api.MapPost("/runs/{id:guid}/steps/{stepId:int}/retry",(HttpContext c,Guid id,int stepId,IAutomationRunService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.RetryStep(a,id,stepId,c.Request.Headers.IfMatch.ToString(),c.Request.Headers["Idempotency-Key"].ToString(),c.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl="no-store";var actor=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result=actor.Succeeded && actor.Value is not null?work(actor.Value):IdentityOperationResult<T>.Failure(actor.Code,actor.StatusCode,actor.Title);
        if(result.Value is AutomationRunDetail detail)c.Response.Headers.ETag=detail.Run.ETag;
        if(result.Value is AutomationDefinitionAcknowledgement ack)c.Response.Headers.ETag=ack.ETag;
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
