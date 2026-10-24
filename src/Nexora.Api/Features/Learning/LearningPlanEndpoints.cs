using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
namespace Nexora.Api.Features.Learning;

public static class LearningPlanEndpoints
{
    public static WebApplication MapLearningPlanEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/learning/plans").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities",(HttpContext c,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapGet("/",(HttpContext c,string? status,string? cursor,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.List(a,status,cursor)));
        api.MapGet("/sources",(HttpContext c,string kind,string? cursor,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Sources(a,kind,cursor)));
        api.MapGet("/{id:guid}",(HttpContext c,Guid id,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Get(a,id)));
        api.MapPost("/",(HttpContext c,LearningPlanMetadata body,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Create(a,body,Key(c),c.TraceIdentifier)));
        api.MapPut("/{id:guid}",(HttpContext c,Guid id,LearningPlanMetadata body,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Update(a,id,Tag(c),body,Key(c),c.TraceIdentifier)));
        api.MapPost("/{id:guid}/items",(HttpContext c,Guid id,LearningPlanLink body,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Link(a,id,Tag(c),body,Key(c),c.TraceIdentifier)));
        api.MapDelete("/{id:guid}/items/{itemId:guid}",(HttpContext c,Guid id,Guid itemId,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Unlink(a,id,itemId,Tag(c),Key(c),c.TraceIdentifier)));
        api.MapPut("/{id:guid}/order",(HttpContext c,Guid id,LearningPlanOrder body,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Reorder(a,id,Tag(c),body,Key(c),c.TraceIdentifier)));
        api.MapPost("/{id:guid}/complete",(HttpContext c,Guid id,LearningPlanConfirmation body,ILearningPlanService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Complete(a,id,Tag(c),body,Key(c),c.TraceIdentifier)));
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static string Tag(HttpContext c)=>c.Request.Headers.IfMatch.ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result=auth.Succeeded && auth.Value is not null?operation(auth.Value):IdentityOperationResult<T>.Failure(auth.Code,auth.StatusCode,auth.Title);
        if(result.Value is LearningPlanDetail detail) c.Response.Headers.ETag=detail.Plan.ETag;
        if(result.Succeeded && result.Acknowledgement is {} ack)
            return new ApiResult<CommittedResourceAcknowledgement>(true,ack,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
