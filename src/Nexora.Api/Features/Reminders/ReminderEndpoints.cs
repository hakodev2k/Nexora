using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Reminders;

namespace Nexora.Api.Features.Reminders;

public static class ReminderEndpoints
{
    public static WebApplication MapReminderEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/reminders/{sourceType}/{sourceId:guid}", (HttpContext context, string sourceType, Guid sourceId,
            IReminderService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, sourceType, sourceId), ToResponse))
            .WithName("getReminderSource");

        api.MapPut("/reminders/{sourceType}/{sourceId:guid}", (HttpContext context, string sourceType, Guid sourceId,
            ReminderSetRequest request, IReminderService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Set(principal, sourceType, sourceId, context.Request.Headers.IfMatch.ToString(),
                    new ReminderSetCommand(request.ConfigType, request.ExactAt, request.SourceETag),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("setReminder");

        api.MapDelete("/reminders/{sourceType}/{sourceId:guid}", (HttpContext context, string sourceType, Guid sourceId,
            ReminderRemoveRequest request, IReminderService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Remove(principal, sourceType, sourceId, context.Request.Headers.IfMatch.ToString(),
                    new ReminderRemoveCommand(request.SourceETag), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("removeReminder");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
        }

        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<ReminderSourceView>> operation,
        Func<ReminderSourceView, ReminderSourceResponse> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<ReminderSourceResponse>(false, default, auth.Code, auth.StatusCode, auth.Title));
        }

        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is not null && result.Value.Reminder is not null)
        {
            context.Response.Headers.ETag = result.Value.Reminder.ETag;
        }
        context.Response.Headers["X-Source-ETag"] = result.Succeeded && result.Value is not null ? result.Value.SourceETag : string.Empty;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<ReminderSourceResponse>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<ReminderSourceResponse>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static ReminderSourceResponse ToResponse(ReminderSourceView value) => new(value.SourceType, value.SourceId,
        value.SourceTitle, value.SourceStartAt, value.TimeZoneId, value.SourceETag,
        value.Reminder is null ? null : new ReminderResponse(value.Reminder.Id, value.Reminder.SourceType, value.Reminder.SourceId,
            value.Reminder.ConfigType, value.Reminder.ExactAt, value.Reminder.TimeZoneId, value.Reminder.DueAt,
            value.Reminder.SourceRevision, value.Reminder.State, value.Reminder.CreatedAt, value.Reminder.UpdatedAt,
            value.Reminder.ETag, value.Reminder.Deliveries.Select(delivery => new ReminderDeliveryResponse(delivery.Channel,
                delivery.State, delivery.Attempts, delivery.LastErrorCode)).ToArray()));
}
