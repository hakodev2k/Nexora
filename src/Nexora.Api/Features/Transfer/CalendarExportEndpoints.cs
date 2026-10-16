using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;

namespace Nexora.Api.Features.Transfer;

public static class CalendarExportEndpoints
{
    public static WebApplication MapCalendarExportEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/transfer/calendar/exports").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapPost("/preview", (HttpContext c, CalendarExportFilter body, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Preview(a, body)));
        api.MapPost("/", (HttpContext c, CalendarExportRequest body, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Request(a, body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapGet("/", (HttpContext c, Guid? cursor, string? state, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, state)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapGet("/{id:guid}/content", (HttpContext c, Guid id, ICalendarExportService s, IIdentityService i, SessionCookieService cookies) =>
        {
            var actor = i.GetPrincipal(cookies.ReadRawHandle(c.Request));
            var result = actor.Succeeded && actor.Value is not null ? s.Content(actor.Value, id) : IdentityOperationResult<byte[]>.Failure(actor.Code, actor.StatusCode, actor.Title);
            c.Response.Headers.CacheControl = "no-store"; c.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return result.Succeeded && result.Value is not null ? Results.File(result.Value, "text/calendar; charset=utf-8", "calendar.ics", enableRangeProcessing: false) :
                new ApiResult<byte[]>(false, null, result.Code, result.StatusCode, result.Title).ToHttp(c);
        });
        return app;
    }
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var actor = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = actor.Succeeded && actor.Value is not null ? operation(actor.Value) : IdentityOperationResult<T>.Failure(actor.Code, actor.StatusCode, actor.Title);
        var etag = result.Value switch { CalendarExportJob job => job.ETag, CalendarExportAcknowledgement ack => ack.ETag, _ => null };
        if (etag is not null) c.Response.Headers.ETag = etag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
