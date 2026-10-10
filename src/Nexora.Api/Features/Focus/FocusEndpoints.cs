using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Focus;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Focus;

public static class FocusEndpoints
{
    public static WebApplication MapFocusEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/focus").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/sessions", (HttpContext c, Guid? cursor, bool? activeOnly, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, activeOnly ?? false)));
        api.MapGet("/preferences", (HttpContext c, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Preferences(a)));
        api.MapPut("/preferences", (HttpContext c, FocusPreferenceCommand body, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SavePreferences(a, body, c.Request.Headers.IfMatch.ToString(), Key(c))));
        api.MapPost("/sessions", (HttpContext c, FocusStart body, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Start(a, body, Key(c))));
        foreach (var verb in new[] { "pause", "resume", "cancel" })
        {
            var action = verb;
            api.MapPost("/sessions/{id:guid}/" + action, (HttpContext c, Guid id, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Transition(a, id, action, c.Request.Headers.IfMatch.ToString(), Key(c))));
        }
        api.MapPost("/sessions/{id:guid}/record-time", (HttpContext c, Guid id, FocusRecordTimeCommand body, IFocusService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.RecordTime(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c))));
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? work(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        if (result.Value is FocusSession session) c.Response.Headers.ETag = session.ETag;
        if (result.Value is FocusPreferences prefs) c.Response.Headers.ETag = prefs.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
