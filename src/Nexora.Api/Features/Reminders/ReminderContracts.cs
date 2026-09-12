namespace Nexora.Api.Features.Reminders;

public sealed record ReminderSetRequest(string ConfigType, DateTimeOffset? ExactAt, string SourceETag);

public sealed record ReminderRemoveRequest(string SourceETag);

public sealed record ReminderDeliveryResponse(string Channel, string State, int Attempts, string? LastErrorCode);

public sealed record ReminderResponse(
    Guid Id,
    string SourceType,
    Guid SourceId,
    string ConfigType,
    DateTimeOffset? ExactAt,
    string TimeZoneId,
    DateTimeOffset? DueAt,
    long SourceRevision,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    IReadOnlyList<ReminderDeliveryResponse> Deliveries);

public sealed record ReminderSourceResponse(
    string SourceType,
    Guid SourceId,
    string SourceTitle,
    DateTimeOffset SourceStartAt,
    string TimeZoneId,
    string SourceETag,
    ReminderResponse? Reminder);
