namespace Nexora.Api.Features.Discovery;

public sealed record SearchResultResponse(
    Guid Id,
    string ResourceType,
    string SourceModule,
    string Title,
    string? Snippet,
    string? Status,
    DateTimeOffset UpdatedAt,
    string Route);

public sealed record SearchProviderResponse(
    string ResourceType,
    string SourceModule,
    string State,
    string? Message,
    int Count);

public sealed record SearchPageResponse(
    string Query,
    string? ResourceType,
    bool IncludeArchived,
    IReadOnlyList<SearchResultResponse> Items,
    IReadOnlyList<SearchProviderResponse> Providers,
    string? NextCursor);
