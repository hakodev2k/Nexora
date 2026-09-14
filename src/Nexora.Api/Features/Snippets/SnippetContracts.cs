namespace Nexora.Api.Features.Snippets;

public sealed record SnippetRequest(string Title, string Language, string Body, string? Description);

public sealed record SnippetResponse(
    Guid Id,
    string Title,
    string Language,
    string Body,
    string? Description,
    long VersionNumber,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record SnippetPageResponse(IReadOnlyList<SnippetResponse> Items, string? NextCursor);

public sealed record SnippetTransitionRequest(string Status);
