using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Api.Features.Learning;

public static class CertificationEndpoints
{
    public static WebApplication MapCertificationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/learning/certifications").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, string? status, string? query, string? cursor, ICertificationService s, IIdentityService i, SessionCookieService cookies, TimeProvider clock) =>
            Read(c, i, cookies, clock, (a, today) => s.List(a, today, status, query, cursor)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, ICertificationService s, IIdentityService i, SessionCookieService cookies, TimeProvider clock) =>
            Read(c, i, cookies, clock, (a, today) => s.Get(a, today, id)));
        api.MapGet("/{id:guid}/history", (HttpContext c, Guid id, string? cursor, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.History(a, id, cursor)));
        api.MapPost("/", (HttpContext c, CertificationCreate body, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, CertificationMetadata body, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/renew", (HttpContext c, Guid id, CertificationRenewal body, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Renew(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        foreach (var operation in new[] { "archive", "unarchive" })
        {
            var op = operation;
            api.MapPost("/{id:guid}/preview-" + op, (HttpContext c, Guid id, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Preview(a, id, op)));
            api.MapPost("/{id:guid}/" + op, (HttpContext c, Guid id, CertificationTransition body, ICertificationService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Transition(a, id, c.Request.Headers.IfMatch.ToString(), op, body, Key(c), c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Read<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, TimeProvider clock, Func<IdentityPrincipal, DateOnly, IdentityOperationResult<T>> operation)
    {
        var profile = identity.GetMe(cookies.ReadRawHandle(c.Request));
        if (!profile.Succeeded || profile.Value is null) return new ApiResult<T>(false, default, profile.Code, profile.StatusCode, profile.Title).ToHttp(c);
        try
        {
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(profile.Value.Profile.TimeZoneId)).DateTime);
            return Run(c, identity, cookies, actor => actor.OwnerId == profile.Value.Profile.PersonalSpaceId && actor.UserId == profile.Value.Profile.Id
                ? operation(actor, today) : IdentityOperationResult<T>.Failure("ResourceUnavailable", 404, "The certification is unavailable."));
        }
        catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
        { return new ApiResult<T>(false, default, "DependencyUnavailable", 503, "The current profile timezone is unavailable.").ToHttp(c); }
    }
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? operation(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        if (result.Value is Certification row) c.Response.Headers.ETag = row.ETag;
        if (result.Succeeded && result.Acknowledgement is { } ack) return new ApiResult<CommittedResourceAcknowledgement>(true, ack, result.Code, result.StatusCode, result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
