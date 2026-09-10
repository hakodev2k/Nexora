using Nexora.Application.Identity;

namespace Nexora.Application.Discovery;

public sealed record SearchResult(
    Guid Id,
    string ResourceType,
    string SourceModule,
    string Title,
    string? Snippet,
    string? Status,
    DateTimeOffset UpdatedAt,
    string Route);

public sealed record SearchProviderStatus(
    string ResourceType,
    string SourceModule,
    string State,
    string? Message,
    int Count);

public sealed record SearchPage(
    string Query,
    string? ResourceType,
    bool IncludeArchived,
    IReadOnlyList<SearchResult> Items,
    IReadOnlyList<SearchProviderStatus> Providers,
    string? NextCursor);

public interface ISearchService
{
    IdentityOperationResult<SearchPage> Search(IdentityPrincipal actor, string? query = null,
        string? resourceType = null, DateOnly? from = null, DateOnly? to = null,
        bool includeArchived = false, int? limit = null);
}
