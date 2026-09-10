using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Reading;

namespace Nexora.Api.Features.Reading;

public static class ReadingEndpoints
{
    public static WebApplication MapReadingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/read-later", (HttpContext context, string? state, int? limit,
            IReadingService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, state, limit),
                value => new ReadingPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listReadingQueue");

        api.MapPost("/read-later", (HttpContext context, SaveReadingRequest request,
            IReadingService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Save(principal, new SaveReadingCommand(request.SourceType, request.SourceId),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("saveReadingItem");

        api.MapDelete("/read-later/{itemId:guid}", (HttpContext context, Guid itemId,
            IReadingService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Remove(principal, itemId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("removeReadingItem");

        api.MapPatch("/read-later/{itemId:guid}", (HttpContext context, Guid itemId, UpdateReadingRequest request,
            IReadingService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, itemId, context.Request.Headers.IfMatch.ToString(),
                    new UpdateReadingCommand(request.State, request.Progress), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateReadingItem");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.StatusCode == StatusCodes.Status204NoContent
            ? ToHttp(context, IdentityOperationResult<TOut>.NoContent(result.Code))
            : result.Succeeded
                ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value!), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is ReadingItemRecord item)
            context.Response.Headers.ETag = item.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static ReadingResponse ToResponse(ReadingItemRecord value) => new(value.Id, value.SourceType, value.SourceId,
        value.State, value.Progress, value.SavedAt, value.ReadAt, value.SafeTitleSnapshot, value.SafeUrlSnapshot,
        value.SourceAvailable, value.UpdatedAt, value.ETag);
}
