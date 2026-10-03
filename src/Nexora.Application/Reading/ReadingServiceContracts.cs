using Nexora.Application.Identity;

namespace Nexora.Application.Reading;

public sealed record ReadingItemRecord(
    Guid Id,
    string SourceType,
    Guid SourceId,
    string State,
    decimal Progress,
    DateTimeOffset SavedAt,
    DateTimeOffset? ReadAt,
    string SafeTitleSnapshot,
    string SafeUrlSnapshot,
    bool SourceAvailable,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record ReadingPage(IReadOnlyList<ReadingItemRecord> Items, string? NextCursor);

public sealed record SaveReadingCommand(string SourceType, Guid SourceId);

public sealed record UpdateReadingCommand(string State, decimal? Progress);

public interface IReadingService
{
    IdentityOperationResult<ReadingPage> List(IdentityPrincipal actor, string? state = null, int? limit = null);
    IdentityOperationResult<ReadingItemRecord> Save(IdentityPrincipal actor, SaveReadingCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid itemId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<ReadingItemRecord> Update(IdentityPrincipal actor, Guid itemId, string? ifMatch,
        UpdateReadingCommand command, string? idempotencyKey = null, string? traceId = null);
}
