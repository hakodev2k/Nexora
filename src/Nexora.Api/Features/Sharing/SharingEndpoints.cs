using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Sharing;

namespace Nexora.Api.Features.Sharing;

public static class SharingEndpoints
{
    public static WebApplication MapSharingEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/sharing/links", (HttpContext context, int? limit, ISharingService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, limit), value =>
                    new ShareLinkPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listShareLinks");

        api.MapPost("/sharing/links", (HttpContext context, ShareLinkRequest request, ISharingService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal,
                    new ShareLinkCreateCommand(request.ResourceType, request.ResourceId, request.Mode,
                        request.ExpiresAt, request.AllowedUserIds, request.NoExpiry), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createShareLink");

        api.MapPatch("/sharing/links/{shareLinkId:guid}", (HttpContext context, Guid shareLinkId,
            ShareLinkUpdateRequest request, ISharingService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, shareLinkId, context.Request.Headers.IfMatch.ToString(),
                    new ShareLinkUpdateCommand(request.Mode, request.ExpiresAt, request.AllowedUserIds, request.NoExpiry),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateShareLink");

        api.MapDelete("/sharing/links/{shareLinkId:guid}", (HttpContext context, Guid shareLinkId,
            ISharingService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Revoke(principal, shareLinkId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("revokeShareLink");

        api.MapGet("/sharing/resolve/{token}", Resolve).WithName("resolveShareLink");
        // A short public form is useful for copied links while the explicit
        // resolve route remains the canonical API shape.
        api.MapGet("/sharing/{token}", Resolve).WithName("resolveShareLinkShort");
        return app;

        static IResult Resolve(HttpContext context, string token, ISharingService service,
            IIdentityService identity, SessionCookieService cookies)
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            if (!RequestRateLimiter.Allow(
                    (context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client") + ":sharing-resolve",
                    60, TimeSpan.FromMinutes(1), out var retryAfter))
            {
                context.Response.Headers["Retry-After"] = retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return Results.Problem(
                    title: "Too many shared-link requests.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    type: "/problems/RateLimited",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "RateLimited",
                        ["retryAfter"] = retryAfter,
                        ["traceId"] = context.TraceIdentifier
                    });
            }
            var auth = identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
            var viewer = auth.Succeeded ? auth.Value : null;
            var result = service.Resolve(token, viewer);
            return result.Succeeded && result.Value is not null
                ? ToHttp(context, IdentityOperationResult<SharedResourceResponse>.Success(ToResponse(result.Value), result.StatusCode, result.Code))
                : ToHttp(context, new IdentityOperationResult<SharedResourceResponse>(false, default, result.Code, result.StatusCode, result.Title));
        }
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
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
        if (result.Succeeded && result.Value is ShareLinkRecord link) context.Response.Headers.ETag = link.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static ShareLinkResponse ToResponse(ShareLinkRecord value) => new(value.Id, value.ResourceType,
        value.ResourceId, value.Mode, value.ExpiresAt, value.RevokedAt, value.ProjectionVersion,
        value.IsActive, value.CreatedAt, value.UpdatedAt, value.ETag, value.AllowedUserIds, value.RawToken);

    private static SharedResourceResponse ToResponse(SharedResource value) => new(value.ResourceType,
        value.ResourceId, value.Mode, value.ExpiresAt, value.ProjectionVersion,
        value.Project is null ? null : new SharedProjectResponse(value.Project.Id, value.Project.Name, value.Project.Description,
            value.Project.Status, value.Project.StartAt, value.Project.EndAt, value.Project.Priority, value.Project.TagsJson,
            value.Project.Tasks.Select(task => new SharedTaskResponse(task.Id, task.Title, task.Description, task.Status,
                task.DueAt, task.StartAt, task.EndAt, task.Priority, task.TagsJson, task.IsOverdue)).ToArray()),
        value.Document is null ? null : new SharedDocumentResponse(value.Document.Id, value.Document.Title,
            value.Document.DocumentType, value.Document.EditorMode, value.Document.Body, value.Document.Status,
            value.Document.VersionNumber, value.Document.UpdatedAt));

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}

public sealed record ShareLinkUpdateRequest(string Mode, DateTimeOffset? ExpiresAt, IReadOnlyList<Guid>? AllowedUserIds, bool NoExpiry = false);
