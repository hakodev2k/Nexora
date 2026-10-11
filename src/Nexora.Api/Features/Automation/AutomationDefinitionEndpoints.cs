using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Automation;

public static class AutomationDefinitionEndpoints
{
    public static WebApplication MapAutomationDefinitionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/automation/definitions").RequireCsrfForUnsafeMethods(256 * 1024);
        api.MapGet("/capabilities", (HttpContext c, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, string? cursor, string? query, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, query)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapGet("/{id:guid}/versions", (HttpContext c, Guid id, long? beforeVersion, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.History(a, id, beforeVersion)));
        api.MapPost("/validate", (HttpContext c, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Validate(a, body)));
        api.MapPost("/", (HttpContext c, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Save(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl = "no-store";
        var actor = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = actor.Succeeded && actor.Value is not null ? work(actor.Value) : IdentityOperationResult<T>.Failure(actor.Code, actor.StatusCode, actor.Title);
        if (result.Value is AutomationDefinitionDetail item) c.Response.Headers.ETag = item.Definition.ETag;
        if (result.Value is AutomationDefinitionAcknowledgement ack) c.Response.Headers.ETag = ack.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
