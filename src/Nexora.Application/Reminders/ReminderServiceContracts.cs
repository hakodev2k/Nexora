using Nexora.Application.Identity;

namespace Nexora.Application.Reminders;

public sealed record ReminderSetCommand(
    string ConfigType,
    DateTimeOffset? ExactAt,
    string SourceETag);

public sealed record ReminderRemoveCommand(string SourceETag);

public sealed record ReminderDeliveryState(string Channel, string State, int Attempts, string? LastErrorCode);

public sealed record ReminderRecord(
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
    IReadOnlyList<ReminderDeliveryState> Deliveries);

public sealed record ReminderSourceView(
    string SourceType,
    Guid SourceId,
    string SourceTitle,
    DateTimeOffset SourceStartAt,
    string TimeZoneId,
    string SourceETag,
    ReminderRecord? Reminder);

public sealed record ReminderDispatchResult(int Dispatched, int Missed, int Canceled, int Deferred);

public interface IReminderService
{
    IdentityOperationResult<ReminderSourceView> Get(IdentityPrincipal actor, string sourceType, Guid sourceId);
    IdentityOperationResult<ReminderSourceView> Set(IdentityPrincipal actor, string sourceType, Guid sourceId,
        string? ifMatch, ReminderSetCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Remove(IdentityPrincipal actor, string sourceType, Guid sourceId,
        string? ifMatch, ReminderRemoveCommand command, string? idempotencyKey = null, string? traceId = null);
}

public interface IReminderDispatchService
{
    Task<ReminderDispatchResult> DispatchDueAsync(CancellationToken cancellationToken = default);
}
