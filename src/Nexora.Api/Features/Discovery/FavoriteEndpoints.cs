using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Discovery;

public static class FavoriteEndpoints
{
    public static WebApplication MapFavoriteEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/favorites", (HttpContext context, string? resourceType, int? limit, string? cursor,
            IFavoriteService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, resourceType, limit, cursor),
                value => new FavoritePageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listFavorites");

        api.MapPost("/favorites", (HttpContext context, FavoriteReferenceRequest request,
            IFavoriteService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Add(principal, new FavoriteReferenceCommand(request.ResourceType, request.ResourceId),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("addFavorite");

        api.MapDelete("/favorites/{favoriteId:guid}", (HttpContext context, Guid favoriteId,
            IFavoriteService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Remove(principal, favoriteId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("removeFavorite");

        api.MapPut("/favorites/{favoriteId:guid}/rank", (HttpContext context, Guid favoriteId,
            FavoriteRankRequest request, IFavoriteService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Reorder(principal, favoriteId, context.Request.Headers.IfMatch.ToString(),
                    new FavoriteRankCommand(request.Rank), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("reorderFavorite");

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
            : result.Succeeded && result.Value is not null
                ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
                : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is FavoriteRecord favorite)
            context.Response.Headers.ETag = favorite.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static FavoriteResponse ToResponse(FavoriteRecord value) => new(
        value.Id, value.ResourceType, value.ResourceId, value.State, value.Title, value.Status,
        value.UpdatedAt, value.Route, value.Rank, value.CreatedAt, value.UpdatedRecordAt, value.ETag);
}
