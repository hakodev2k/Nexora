using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Snippets;

namespace Nexora.Api.Features.Snippets;

public static class SnippetEndpoints
{
    public static WebApplication MapSnippetEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/snippets", (HttpContext context, bool? includeArchived, string? query, int? limit,
            ISnippetService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, includeArchived ?? false, query, limit),
                value => new SnippetPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listSnippets");

        api.MapPost("/snippets", (HttpContext context, SnippetRequest request,
            ISnippetService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal, new SnippetCommand(request.Title, request.Language, request.Body, request.Description),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createSnippet");

        api.MapPut("/snippets/{snippetId:guid}", (HttpContext context, Guid snippetId, SnippetRequest request,
            ISnippetService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Save(principal, snippetId, context.Request.Headers.IfMatch.ToString(),
                    new SnippetCommand(request.Title, request.Language, request.Body, request.Description),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("saveSnippet");

        api.MapPost("/snippets/{snippetId:guid}/transition", (HttpContext context, Guid snippetId,
            SnippetTransitionRequest request, ISnippetService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, snippetId, context.Request.Headers.IfMatch.ToString(), request.Status,
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionSnippet");

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
        if (result.Succeeded && result.Value is SnippetRecord snippet)
            context.Response.Headers.ETag = snippet.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static SnippetResponse ToResponse(SnippetRecord value) => new(value.Id, value.Title, value.Language,
        value.Body, value.Description, value.VersionNumber, value.Status, value.CreatedAt, value.UpdatedAt, value.ETag);
}
