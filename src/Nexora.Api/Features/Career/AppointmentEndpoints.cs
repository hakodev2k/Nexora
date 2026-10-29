using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Career;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Career;

public static class AppointmentEndpoints
{
    public static WebApplication MapCareerAppointmentEndpoints(this WebApplication app)
    {
        var api=app.MapGroup("/api/v1/career/jobs/{jobId:guid}/events").RequireCsrfForUnsafeMethods();
        api.MapGet("",(HttpContext context,Guid jobId,Guid? cursor,ICareerAppointmentService service,IIdentityService identity,SessionCookieService cookies)=>
            Run(context,identity,cookies,actor=>service.List(actor,jobId,cursor)));
        api.MapPost("",(HttpContext context,Guid jobId,CareerAppointmentCommand request,ICareerAppointmentService service,IIdentityService identity,SessionCookieService cookies)=>
            Run(context,identity,cookies,actor=>service.Link(actor,jobId,context.Request.Headers.IfMatch.ToString(),request,Key(context),context.TraceIdentifier)));
        api.MapDelete("/{linkId:guid}",(HttpContext context,Guid jobId,Guid linkId,ICareerAppointmentService service,IIdentityService identity,SessionCookieService cookies)=>
            Run(context,identity,cookies,actor=>service.Unlink(actor,jobId,linkId,context.Request.Headers.IfMatch.ToString(),Key(context),context.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext context,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
        if(!auth.Succeeded||auth.Value is null)return new ApiResult<T>(false,default,auth.Code,auth.StatusCode,auth.Title).ToHttp(context);
        var result=operation(auth.Value);
        if(result.Succeeded&&result.Acknowledgement is {} acknowledgement)return new ApiResult<CommittedResourceAcknowledgement>(true,acknowledgement,result.Code,result.StatusCode,result.Title).ToHttp(context);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(context);
    }
    private static string? Key(HttpContext context)=>context.Request.Headers["Idempotency-Key"].ToString();
}
