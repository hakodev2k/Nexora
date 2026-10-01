using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.TimeTracking;

namespace Nexora.Api.Features.TimeTracking;

public static class TimeTrackingEndpoints
{
    public static WebApplication MapTimeTrackingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/time").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/entries", (HttpContext c, Guid? cursor, bool? trash, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.List(a, cursor, trash ?? false)));
        api.MapGet("/entries/{id:guid}", (HttpContext c, Guid id, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Get(a, id)));
        api.MapGet("/entries/{id:guid}/history", (HttpContext c, Guid id, Guid? cursor, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.History(a, id, cursor)));
        api.MapGet("/timer", (HttpContext c, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Timer(a)));
        api.MapGet("/report", (HttpContext c, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Report(a)));
        api.MapPost("/timer", (HttpContext c, TimerCommand body, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Start(a, body, Key(c), c.TraceIdentifier)));
        api.MapPost("/timer/{id:guid}/stop", (HttpContext c, Guid id, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Stop(a, id, c.Request.Headers.IfMatch.ToString(), Key(c), c.TraceIdentifier)));
        api.MapPost("/entries", (HttpContext c, TimeEntryCommand body, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/entries/{id:guid}", (HttpContext c, Guid id, TimeEntryCommand body, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        foreach (var restore in new[] { false, true })
        {
            var isRestore = restore;
            api.MapPost("/entries/{id:guid}/" + (restore ? "restore" : "trash"), (HttpContext c, Guid id, ITimeTrackingService s, IIdentityService i, SessionCookieService cookies) =>
                Run(c, i, cookies, a => s.Transition(a, id, c.Request.Headers.IfMatch.ToString(), isRestore, Key(c), c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies,
        Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? operation(auth.Value)
            : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        if (result.Value is TimeEntry entry) c.Response.Headers.ETag = entry.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
