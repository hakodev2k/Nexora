using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.News;

namespace Nexora.Api.Features.News;

public static class NewsCategoryEndpoints
{
    public static WebApplication MapNewsCategoryEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/news/categories").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, INewsCategoryService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, Guid? cursor, string? query, INewsCategoryService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, query)));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, INewsCategoryService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapPost("/", (HttpContext c, NewsCategoryCommand body, INewsCategoryService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, NewsCategoryCommand body, INewsCategoryService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Update(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        return app;
    }
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl = "no-store";
        var actor = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = actor.Succeeded && actor.Value is not null ? work(actor.Value) : IdentityOperationResult<T>.Failure(actor.Code, actor.StatusCode, actor.Title);
        if (result.Value is NewsCategoryRecord item) c.Response.Headers.ETag = item.ETag;
        if (result.Value is NewsCategoryAcknowledgement acknowledgement) c.Response.Headers.ETag = acknowledgement.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
