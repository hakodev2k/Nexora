namespace Nexora.Api.Features.Reading;

public sealed record SaveReadingRequest(string SourceType, Guid SourceId);

public sealed record UpdateReadingRequest(string State, decimal? Progress);

public sealed record ReadingResponse(
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

public sealed record ReadingPageResponse(IReadOnlyList<ReadingResponse> Items, string? NextCursor);
