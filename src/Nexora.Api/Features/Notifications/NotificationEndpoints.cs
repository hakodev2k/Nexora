using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Notifications;

namespace Nexora.Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static WebApplication MapNotificationEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/notifications", (HttpContext context, bool? unreadOnly, int? limit, INotificationService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.List(principal, unreadOnly ?? false, limit), ToPage))
            .WithName("listNotifications");

        api.MapPatch("/notifications/{notificationId:guid}", (HttpContext context, Guid notificationId, NotificationReadRequest request, INotificationService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.MarkRead(principal, notificationId,
                new NotificationMarkReadCommand(request.Read, context.Request.Headers.IfMatch.ToString()), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("markNotificationRead");

        api.MapPost("/notifications/mark-all-read", (HttpContext context, INotificationService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.MarkAllRead(principal, IdempotencyKey(context), context.TraceIdentifier), value => new NotificationMarkAllReadResponse(value.Watermark, value.UpdatedCount)))
            .WithName("markAllNotificationsRead");

        api.MapPost("/notifications/delete", (HttpContext context, NotificationDeleteRequest request, INotificationService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.Delete(principal, new NotificationDeleteCommand(request.NotificationIds), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("deleteNotifications");

        // This is a local-safe operator path for exercising the durable
        // notification/outbox boundary. It never calls Email or Browser Push.
        api.MapPost("/admin/notifications", (HttpContext context, NotificationPublishRequest request, INotificationService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.Publish(principal,
                new NotificationPublishCommand(request.Kind, request.Title, request.Body, request.SourceRef, request.LogicalKey, request.RecipientUserId), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("publishNotification");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
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
        if (result.Succeeded && result.Value is NotificationRecord notification) context.Response.Headers.ETag = notification.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) => new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static NotificationPageResponse ToPage(NotificationPage value) => new(value.Items.Select(ToResponse).ToArray(), value.NextCursor, value.UnreadCount);
    private static NotificationResponse ToResponse(NotificationRecord value) => new(value.Id, value.Kind, value.Title, value.Body, value.SourceRef, value.CreatedAt, value.ReadAt, value.ETag,
        value.Deliveries.Select(delivery => new NotificationDeliveryResponse(delivery.Channel, delivery.State, delivery.Attempts, delivery.LastErrorCode, delivery.UpdatedAt)).ToArray());
}

public sealed record NotificationMarkAllReadResponse(DateTimeOffset Watermark, int UpdatedCount);
