using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Assets;

namespace Nexora.Api.Features.Assets;

public static class PersonalAssetEndpoints
{
    public static WebApplication MapPersonalAssetEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/assets/personal").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, Guid? cursor, string? state, string? kind, string? category, string? query, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, state, kind, category, query)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapPost("/", (HttpContext c, PersonalAssetCreate body, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, PersonalAssetMetadata body, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/transition", (HttpContext c, Guid id, PersonalAssetState body, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SetState(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapGet("/{id:guid}/history", (HttpContext c, Guid id, Guid? cursor, string? action, string? from, string? to, string? version, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.History(a, id, cursor, action, from, to, version)));
        foreach (var operation in new[] { "archive", "unarchive", "trash", "restore", "purge" })
        {
            var op = operation;
            api.MapPost("/{id:guid}/preview-" + op, (HttpContext c, Guid id, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Preview(a, id, op)));
            api.MapPost("/{id:guid}/" + op, (HttpContext c, Guid id, PersonalAssetConfirmation body, IPersonalAssetService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Transition(a, id, c.Request.Headers.IfMatch.ToString(), op, body, Key(c), c.TraceIdentifier)));
        }
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? operation(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        if (result.Value is PersonalAsset item) c.Response.Headers.ETag = item.ETag;
        if (result.Value is PersonalAssetAcknowledgement acknowledgement && acknowledgement.ETag is { } etag) c.Response.Headers.ETag = etag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
