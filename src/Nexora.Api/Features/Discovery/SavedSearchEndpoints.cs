using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Discovery;

public static class SavedSearchEndpoints
{
    public static WebApplication MapSavedSearchEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/saved-searches").RequireCsrfForUnsafeMethods();
        api.MapGet("", (HttpContext context, string? cursor, ISavedSearchService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.List(actor, cursor)));
        api.MapPost("", (HttpContext context, SavedSearchCommand request, ISavedSearchService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Save(actor, null, null, request, Key(context), context.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext context, Guid id, SavedSearchCommand request, ISavedSearchService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Save(actor, id, context.Request.Headers.IfMatch.ToString(), request, Key(context), context.TraceIdentifier)));
        api.MapDelete("/{id:guid}", (HttpContext context, Guid id, ISavedSearchService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Delete(actor, id, context.Request.Headers.IfMatch.ToString(), Key(context), context.TraceIdentifier)));
        api.MapPost("/{id:guid}/run", (HttpContext context, Guid id, string? cursor, ISavedSearchService service, IIdentityService identity, SessionCookieService cookies) =>
            Run(context, identity, cookies, actor => service.Run(actor, id, cursor)));
        return app;
    }
    private static IResult Run<T>(HttpContext context, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> operation)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<T>(false, default, auth.Code, auth.StatusCode, auth.Title).ToHttp(context);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Acknowledgement is { } acknowledgement)
            return new ApiResult<CommittedResourceAcknowledgement>(true, acknowledgement, result.Code, result.StatusCode, result.Title).ToHttp(context);
        if (result.Value is SavedSearchRecord record) context.Response.Headers.ETag = record.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    }
    private static string? Key(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
