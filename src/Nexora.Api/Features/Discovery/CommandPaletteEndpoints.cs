using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Discovery;

public static class CommandPaletteEndpoints
{
    public static WebApplication MapCommandPaletteEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/commands").RequireCsrfForUnsafeMethods();
        api.MapGet("", (HttpContext context, string? q, ICommandPaletteService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), actor => service.List(actor, q))).WithName("listCommands");
        api.MapPost("/execute", (HttpContext context, CommandSelection request, ICommandPaletteService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), actor => service.Execute(actor, request))).WithName("executeCommandHandoff");
        return app;
    }
    private static IResult Map<T>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<T>(false, default, auth.Code, auth.StatusCode, auth.Title).ToHttp(context);
        var result = operation(auth.Value); return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    }
}
