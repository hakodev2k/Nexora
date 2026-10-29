using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Integrations;
namespace Nexora.Api.Features.Integrations;

public static class IntegrationConnectionEndpoints
{
    public static WebApplication MapIntegrationConnectionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/integrations/connections").RequireCsrfForUnsafeMethods(16 * 1024);
        api.MapGet("/capabilities", (HttpContext c, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, s.Capabilities));
        api.MapGet("/connectors", (HttpContext c, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, s.Connectors));
        api.MapGet("/", (HttpContext c, string? cursor, string? query, string? provider, string? state, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, query, provider, state)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapPost("/", (HttpContext c, IntegrationConnectionCommand body, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, IntegrationConnectionCommand body, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/disable", (HttpContext c, Guid id, IntegrationDisableCommand body, IIntegrationConnectionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Disable(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl = "no-store";
        var principal = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = principal.Succeeded && principal.Value is { } actor ? work(actor) : IdentityOperationResult<T>.Failure(principal.Code, principal.StatusCode, principal.Title);
        if (result.Value is IntegrationConnectionRecord record) c.Response.Headers.ETag = record.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
