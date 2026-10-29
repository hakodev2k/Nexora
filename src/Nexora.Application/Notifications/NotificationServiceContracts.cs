using Nexora.Application.Identity;

namespace Nexora.Application.Notifications;

public sealed record NotificationDeliveryRecord(
    string Channel,
    string State,
    int Attempts,
    string? LastErrorCode,
    DateTimeOffset UpdatedAt);

public sealed record NotificationRecord(
    Guid Id,
    string Kind,
    string Title,
    string Body,
    string? SourceRef,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    string ETag,
    IReadOnlyList<NotificationDeliveryRecord> Deliveries,
    bool SourceUnavailable = false);

public sealed record NotificationPage(
    IReadOnlyList<NotificationRecord> Items,
    string? NextCursor,
    int UnreadCount);

public sealed record NotificationMarkReadCommand(bool Read, string? IfMatch);

public sealed record NotificationDeleteCommand(IReadOnlyList<Guid> NotificationIds);

public sealed record NotificationMarkAllReadResult(DateTimeOffset Watermark, int UpdatedCount);
public sealed record NotificationSource(string ResourceType, Guid ResourceId, string Title, string Route);

public interface INotificationService
{
    IdentityOperationResult<NotificationSource> OpenSource(IdentityPrincipal actor, Guid notificationId);
    IdentityOperationResult<NotificationPage> List(IdentityPrincipal actor, bool unreadOnly = false, int? limit = null,
        string? cursor = null, string? category = null, DateTimeOffset? from = null, DateTimeOffset? to = null);
    IdentityOperationResult<NotificationRecord> MarkRead(IdentityPrincipal actor, Guid notificationId, NotificationMarkReadCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<NotificationMarkAllReadResult> MarkAllRead(IdentityPrincipal actor,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Delete(IdentityPrincipal actor, NotificationDeleteCommand command,
        string? idempotencyKey = null, string? traceId = null);
}
