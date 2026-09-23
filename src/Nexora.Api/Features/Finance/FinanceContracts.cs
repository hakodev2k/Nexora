namespace Nexora.Api.Features.Finance;

public sealed record FinanceCategoryRequest(string Title, string? IfMatch);

public sealed record FinanceCategoryResponse(
    Guid Id,
    string Title,
    int UsageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record FinanceCategoryPageResponse(
    IReadOnlyList<FinanceCategoryResponse> Items,
    string? NextCursor);

public sealed record FinanceRecordRequest(
    Guid CategoryId,
    string Amount,
    string CurrencyCode,
    DateOnly OccurredOn,
    string? Note);

public sealed record FinanceRecordResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryTitle,
    string Amount,
    string CurrencyCode,
    DateOnly OccurredOn,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record FinanceSummaryResponse(string CurrencyCode, string Amount);

public sealed record FinanceRecordPageResponse(
    IReadOnlyList<FinanceRecordResponse> Items,
    IReadOnlyList<FinanceSummaryResponse> Summaries,
    string? NextCursor);
