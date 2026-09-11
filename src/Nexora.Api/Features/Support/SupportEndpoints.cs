using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Support;

namespace Nexora.Api.Features.Support;

public static class SupportEndpoints
{
    public static WebApplication MapSupportEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/support/grants", (HttpContext context, int? limit, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ListGrants(principal, limit), value =>
                    new SupportGrantPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listSupportGrants");

        api.MapPost("/support/grants", (HttpContext context, SupportGrantRequest request, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Grant(principal, new SupportGrantCommand(request.ModuleCode, request.DurationMode, request.ExpiresAt),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("grantSupportConsent");

        api.MapDelete("/support/grants/{grantId:guid}", (HttpContext context, Guid grantId, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.RevokeGrant(principal, grantId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("revokeSupportConsent");

        api.MapGet("/support/sessions", (HttpContext context, int? limit, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ListSessions(principal, limit), value =>
                    new SupportSessionPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listSupportSessions");

        api.MapPost("/support/sessions", (HttpContext context, SupportSessionRequest request, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.OpenSupport(principal, new SupportSessionOpenCommand(request.TargetUserId, request.ModuleCode),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("openSupportSession");

        api.MapPost("/support/emergency", (HttpContext context, EmergencyAccessRequest request, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.OpenEmergency(principal, new EmergencyOpenCommand(request.TargetUserId, request.ModuleCode, request.Reason),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("openEmergencySupportSession");

        api.MapDelete("/support/sessions/{sessionId:guid}", (HttpContext context, Guid sessionId, ISupportService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.EndSession(principal, sessionId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("endSupportSession");

        return app;
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
        if (result.Succeeded && result.Value is SupportGrantRecord grant) context.Response.Headers.ETag = grant.ETag;
        if (result.Succeeded && result.Value is SupportSessionRecord session) context.Response.Headers.ETag = session.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static SupportGrantResponse ToResponse(SupportGrantRecord value) => new(value.Id, value.ModuleCode,
        value.DurationMode, value.ExpiresAt, value.RevokedAt, value.IsActive, value.CreatedAt, value.UpdatedAt, value.ETag);

    private static SupportSessionResponse ToResponse(SupportSessionRecord value) => new(value.Id, value.TargetUserId,
        value.ModuleCode, value.Mode, value.ExpiresAt, value.EndedAt, value.SupportGrantId, value.Reason, value.CreatedAt, value.ETag);

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
