using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Organization;

namespace Nexora.Api.Features.Organization;

public static class OrganizationEndpoints
{
    public static WebApplication MapOrganizationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/organization/tags", (HttpContext context, int? limit,
            ITagService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal,
                    context.Request.Query["namespace"].ToString(),
                    context.Request.Query["query"].ToString(), limit),
                value => new TagPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listOrganizationTags");

        api.MapPost("/organization/tags", (HttpContext context, TagCreateRequest request,
            ITagService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal,
                    new TagCreateCommand(request.Namespace, request.Name, request.Color),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createOrganizationTag");

        api.MapPut("/organization/tags/{tagId:guid}", (HttpContext context, Guid tagId,
            TagRenameRequest request, ITagService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Rename(principal, tagId, context.Request.Headers.IfMatch.ToString(),
                    new TagRenameCommand(request.Name, request.Color), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("renameOrganizationTag");

        api.MapDelete("/organization/tags/{tagId:guid}", (HttpContext context, Guid tagId,
            ITagService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Remove(principal, tagId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("removeOrganizationTag");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        if (result.Succeeded && result.StatusCode == StatusCodes.Status204NoContent)
            return ToHttp(context, IdentityOperationResult<TOut>.NoContent(result.Code));
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is TagRecord tag)
            context.Response.Headers.ETag = tag.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static TagResponse ToResponse(TagRecord value) => new(
        value.Id, value.Namespace, value.Name, value.Color, value.UsageCount,
        value.CreatedAt, value.UpdatedAt, value.ETag);
}
