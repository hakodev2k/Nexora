namespace Nexora.Api.Features.Settings;

public sealed record PreferenceResponse(
    Guid Id,
    string PreferenceKey,
    int SchemaVersion,
    string ValueJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record PreferencePageResponse(IReadOnlyList<PreferenceResponse> Items, string? NextCursor);

public sealed record PreferenceUpdateRequest(int SchemaVersion, string ValueJson);
