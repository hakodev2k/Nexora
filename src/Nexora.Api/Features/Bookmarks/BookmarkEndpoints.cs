using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Bookmarks;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Bookmarks;

public static class BookmarkEndpoints
{
    public static WebApplication MapBookmarkEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/bookmarks", (HttpContext context, bool? includeArchived, string? query, int? limit,
            IBookmarkService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, includeArchived ?? false, query, limit),
                value => new BookmarkPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listBookmarks");

        api.MapPost("/bookmarks", (HttpContext context, BookmarkRequest request,
            IBookmarkService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal, new BookmarkCommand(request.Url, request.Title, request.Description),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createBookmark");

        api.MapPut("/bookmarks/{bookmarkId:guid}", (HttpContext context, Guid bookmarkId, BookmarkRequest request,
            IBookmarkService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, bookmarkId, context.Request.Headers.IfMatch.ToString(),
                    new BookmarkCommand(request.Url, request.Title, request.Description), IdempotencyKey(context), context.TraceIdentifier),
                ToResponse))
            .WithName("updateBookmark");

        api.MapPost("/bookmarks/{bookmarkId:guid}/transition", (HttpContext context, Guid bookmarkId,
            BookmarkTransitionRequest request, IBookmarkService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, bookmarkId, context.Request.Headers.IfMatch.ToString(), request.Status,
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionBookmark");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is BookmarkRecord bookmark)
            context.Response.Headers.ETag = bookmark.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static BookmarkResponse ToResponse(BookmarkRecord value) => new(value.Id, value.Url, value.CanonicalUrl,
        value.Title, value.Description, value.Health, value.LastCheckedAt, value.Status,
        value.CreatedAt, value.UpdatedAt, value.ETag);
}
