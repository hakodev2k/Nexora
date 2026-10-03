namespace Nexora.Api.Features.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Kind,
    string Title,
    string Body,
    string? SourceRef,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt,
    string ETag,
    IReadOnlyList<NotificationDeliveryResponse> Deliveries);

public sealed record NotificationDeliveryResponse(
    string Channel,
    string State,
    int Attempts,
    string? LastErrorCode,
    DateTimeOffset UpdatedAt);

public sealed record NotificationPageResponse(
    IReadOnlyList<NotificationResponse> Items,
    string? NextCursor,
    int UnreadCount);

public sealed record NotificationReadRequest(bool Read);

public sealed record NotificationDeleteRequest(IReadOnlyList<Guid> NotificationIds);

public sealed record NotificationPublishRequest(
    string Kind,
    string Title,
    string Body,
    string? SourceRef,
    string LogicalKey,
    Guid? RecipientUserId = null);
