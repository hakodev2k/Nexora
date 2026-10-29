using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Dashboard;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Dashboard;

public static class DashboardLayoutEndpoints
{
    public static WebApplication MapDashboardLayoutEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/dashboard/layout").RequireCsrfForUnsafeMethods();
        api.MapGet("", (HttpContext context, IDashboardLayoutService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, service.Get));
        api.MapPost("", (HttpContext context, DashboardLayoutCommand request, IDashboardLayoutService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Save(actor, true, null, request, Key(context), context.TraceIdentifier)));
        api.MapPut("", (HttpContext context, DashboardLayoutCommand request, IDashboardLayoutService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Save(actor, false, context.Request.Headers.IfMatch.ToString(), request, Key(context), context.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext context, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<T>(false, default, auth.Code, auth.StatusCode, auth.Title).ToHttp(context);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Acknowledgement is { } acknowledgement)
            return new ApiResult<CommittedResourceAcknowledgement>(true, acknowledgement, result.Code, result.StatusCode, result.Title).ToHttp(context);
        if (result.Value is DashboardLayout layout) context.Response.Headers.ETag = layout.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    }
    private static string? Key(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
