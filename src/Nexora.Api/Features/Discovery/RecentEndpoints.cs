using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Discovery;

public static class RecentEndpoints
{
    public static WebApplication MapRecentEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/recents").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext context, IRecentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), service.Capabilities)).WithName("getRecentCapabilities");
        api.MapGet("", (HttpContext context, string? resourceType, string? cursor, IRecentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), actor => service.List(actor, resourceType, cursor))).WithName("listRecents");
        api.MapGet("/clear-preview", (HttpContext context, IRecentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), service.PreviewClear)).WithName("previewClearRecents");
        api.MapPost("/clear", (HttpContext context, RecentClearRequest request, IRecentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), actor => service.Clear(actor, request.Token, context.Request.Headers["Idempotency-Key"].ToString(), context.TraceIdentifier))).WithName("clearRecents");
        return app;
    }
    private static IResult Map<T>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<T>(false, default, auth.Code, auth.StatusCode, auth.Title).ToHttp(context);
        var result = operation(auth.Value);
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    }
}
public sealed record RecentClearRequest(string Token);
