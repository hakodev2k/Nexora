using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Monitoring;

namespace Nexora.Api.Features.Monitoring;

public static class MonitoringEndpoints
{
    public static WebApplication MapMonitoringEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/monitoring/monitors").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities",(HttpContext c,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapGet("/",(HttpContext c,Guid? cursor,string? query,string? state,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.List(a,cursor,query,state)));
        api.MapGet("/{id:guid}",(HttpContext c,Guid id,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Get(a,id)));
        api.MapPost("/",(HttpContext c,MonitorCreate body,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Create(a,body,Key(c),c.TraceIdentifier)));
        api.MapPut("/{id:guid}",(HttpContext c,Guid id,MonitorUpdate body,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Update(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        foreach(var enable in new[]{false,true})
        {
            var enabled=enable;
            api.MapPost("/{id:guid}/"+(enabled?"resume":"pause"),(HttpContext c,Guid id,MonitorConfirmation body,IMonitoringService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.SetEnabled(a,id,c.Request.Headers.IfMatch.ToString(),enabled,body,Key(c),c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl="no-store";
        var actor=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result=actor.Succeeded&&actor.Value is not null?work(actor.Value):IdentityOperationResult<T>.Failure(actor.Code,actor.StatusCode,actor.Title);
        if(result.Value is MonitorRecord item)c.Response.Headers.ETag=item.ETag;
        if(result.Value is MonitorAcknowledgement acknowledgement)c.Response.Headers.ETag=acknowledgement.ETag;
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
