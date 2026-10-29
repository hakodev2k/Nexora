using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Career;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Career;

public static class CareerEndpoints
{
    public static WebApplication MapCareerEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/career").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/companies", (HttpContext c, Guid? cursor, string? query, bool? eligibleOnly, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Companies(a, cursor, query, eligibleOnly)));
        api.MapGet("/companies/{id:guid}", (HttpContext c, Guid id, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Company(a, id)));
        api.MapPost("/companies", (HttpContext c, CompanyChange body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.CreateCompany(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/companies/{id:guid}", (HttpContext c, Guid id, CompanyChange body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.UpdateCompany(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPost("/companies/{id:guid}/merge-preview", (HttpContext c, Guid id, CompanyMergeTarget body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.PreviewMerge(a, id, body.TargetId)));
        api.MapPost("/companies/{id:guid}/merge", (HttpContext c, Guid id, CompanyMergeRequest body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Merge(a, id, body, Key(c), c.TraceIdentifier)));
        api.MapGet("/jobs", (HttpContext c, Guid? cursor, string? state, string? stage, Guid? companyId, string? query, string? location, string? dateField, DateOnly? from, DateOnly? to, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Jobs(a, cursor, state, stage, companyId, query, location, dateField, from, to)));
        api.MapGet("/jobs/{id:guid}", (HttpContext c, Guid id, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Job(a, id)));
        api.MapPost("/jobs", (HttpContext c, JobCreate body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.CreateJob(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/jobs/{id:guid}", (HttpContext c, Guid id, JobChange body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.UpdateJob(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPost("/jobs/{id:guid}/transition", (HttpContext c, Guid id, JobStageChange body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.ChangeStage(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapGet("/jobs/{id:guid}/history", (HttpContext c, Guid id, Guid? cursor, string? action, string? from, string? to, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.History(a, id, cursor, action, from, to)));
        foreach (var operation in new[] { "trash", "restore", "purge" })
        {
            var op = operation;
            api.MapPost("/jobs/{id:guid}/preview-" + op, (HttpContext c, Guid id, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.PreviewJob(a, id, op)));
            api.MapPost("/jobs/{id:guid}/" + op, (HttpContext c, Guid id, CareerConfirmation body, ICareerService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Lifecycle(a, id, c.Request.Headers.IfMatch.ToString(), op, body, Key(c), c.TraceIdentifier)));
        }
        return app;
    }
    public sealed record CompanyMergeTarget(Guid TargetId);
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? operation(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        var etag = result.Value switch { CareerCompany item => item.ETag, CareerJob item => item.ETag, CareerAcknowledgement acknowledgement => acknowledgement.ETag, _ => null };
        if (etag is not null) c.Response.Headers.ETag = etag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
