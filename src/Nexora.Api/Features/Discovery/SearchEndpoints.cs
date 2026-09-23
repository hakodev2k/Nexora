using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Discovery;

public static class SearchEndpoints
{
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/search", (HttpContext context, string? q, string? resourceType,
            DateOnly? from, DateOnly? to, bool includeArchived, int? limit, ISearchService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Search(principal, q, resourceType, from, to, includeArchived, limit),
                value => new SearchPageResponse(
                    value.Query,
                    value.ResourceType,
                    value.IncludeArchived,
                    value.Items.Select(ToResponse).ToArray(),
                    value.Providers.Select(ToResponse).ToArray(),
                    value.NextCursor)))
            .WithName("search");

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

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static SearchResultResponse ToResponse(SearchResult value) => new(
        value.Id, value.ResourceType, value.SourceModule, value.Title, value.Snippet,
        value.Status, value.UpdatedAt, value.Route);

    private static SearchProviderResponse ToResponse(SearchProviderStatus value) => new(
        value.ResourceType, value.SourceModule, value.State, value.Message, value.Count);
}
