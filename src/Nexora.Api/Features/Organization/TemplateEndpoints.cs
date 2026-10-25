using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Organization;

namespace Nexora.Api.Features.Organization;

public static class TemplateEndpoints
{
    public static WebApplication MapTemplateEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/organization/templates").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ITemplateService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, s.Capabilities)).WithName("templateCapabilities");
        api.MapGet("", (HttpContext c, string? query, string? state, int? page, ITemplateService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.List(a, query, state ?? "Active", page ?? 1))).WithName("listTemplates");
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, ITemplateService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id))).WithName("getTemplate");
        api.MapGet("/{id:guid}/draft", (HttpContext c, Guid id, ITemplateService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Draft(a, id, ETag(c)))).WithName("prepareTemplateDraft");
        api.MapPost("", (HttpContext c, TemplateWrite body, ITemplateService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier))).WithName("createTemplate");
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, TemplateWrite body, ITemplateService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Update(a, id, body, ETag(c), Key(c), c.TraceIdentifier))).WithName("updateTemplate");
        api.MapPost("/{id:guid}/lifecycle/{action}", (HttpContext c, Guid id, string action, ITemplateService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Transition(a, id, action, ETag(c), Key(c), c.TraceIdentifier))).WithName("transitionTemplate");
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static string ETag(HttpContext c) => c.Request.Headers.IfMatch.ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> run)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<object>(false, null, auth.Code, auth.StatusCode, auth.Title).ToHttp(c);
        var result = run(auth.Value);
        c.Response.Headers.CacheControl = "no-store";
        if (result.Acknowledgement is { } ack) return new ApiResult<CommittedResourceAcknowledgement>(true, ack, result.Code, result.StatusCode, result.Title).ToHttp(c);
        if (result.Value is TemplateDetail detail) c.Response.Headers.ETag = detail.Template.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
