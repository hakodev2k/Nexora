using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Api.Features.Learning;

public static class CourseEndpoints
{
    public static WebApplication MapCourseEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/learning/courses").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, Guid? cursor, string? status, string? query, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, status, query)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapPost("/", (HttpContext c, CourseMetadata body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, CourseMetadata body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/progress", (HttpContext c, Guid id, CourseProgress body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Progress(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        foreach (var operation in new[] { "complete", "abandon", "archive", "unarchive", "trash", "restore", "purge" })
        {
            var op = operation;
            api.MapPost("/{id:guid}/preview-" + op, (HttpContext c, Guid id, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Preview(a, id, op)));
            api.MapPost("/{id:guid}/" + op, (HttpContext c, Guid id, CourseConfirmation body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Transition(a, id, c.Request.Headers.IfMatch.ToString(), op, body, Key(c), c.TraceIdentifier)));
        }
        api.MapGet("/{id:guid}/milestones", (HttpContext c, Guid id, Guid? cursor, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Milestones(a, id, cursor)));
        api.MapPost("/{id:guid}/milestones", (HttpContext c, Guid id, MilestoneDefinition body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.AddMilestone(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPut("/{id:guid}/milestones/{child:guid}", (HttpContext c, Guid id, Guid child, MilestoneDefinition body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.ChangeMilestone(a, id, child, c.Request.Headers.IfMatch.ToString(), ChildTag(c), "update", body, Key(c), c.TraceIdentifier)));
        api.MapDelete("/{id:guid}/milestones/{child:guid}", (HttpContext c, Guid id, Guid child, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.ChangeMilestone(a, id, child, c.Request.Headers.IfMatch.ToString(), ChildTag(c), "delete", new { }, Key(c), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/milestones/{child:guid}/completion", (HttpContext c, Guid id, Guid child, MilestoneCompletion body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.ChangeMilestone(a, id, child, c.Request.Headers.IfMatch.ToString(), ChildTag(c), "completion", body, Key(c), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/milestones/{child:guid}/move", (HttpContext c, Guid id, Guid child, MilestoneMove body, ICourseService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.ChangeMilestone(a, id, child, c.Request.Headers.IfMatch.ToString(), ChildTag(c), "move", body, Key(c), c.TraceIdentifier)));
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static string ChildTag(HttpContext c) => c.Request.Headers["Milestone-If-Match"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? operation(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        if (result.Value is Course item) c.Response.Headers.ETag = item.ETag;
        if (result.Value is CourseAcknowledgement ack && ack.ETag is { } etag) c.Response.Headers.ETag = etag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
