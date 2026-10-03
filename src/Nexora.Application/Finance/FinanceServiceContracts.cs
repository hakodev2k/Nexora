using Nexora.Application.Identity;

namespace Nexora.Application.Finance;

public sealed record FinanceCategoryRecord(
    Guid Id,
    string Title,
    int UsageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record FinanceCategoryPage(IReadOnlyList<FinanceCategoryRecord> Items, string? NextCursor);

public sealed record FinanceManualRecord(
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

public sealed record FinanceSummary(string CurrencyCode, string Amount);

public sealed record FinanceManualRecordPage(
    IReadOnlyList<FinanceManualRecord> Items,
    IReadOnlyList<FinanceSummary> Summaries,
    string? NextCursor);

public sealed record FinanceCategoryCommand(string Title, string? IfMatch);

public sealed record FinanceManualRecordCommand(
    Guid CategoryId,
    string Amount,
    string CurrencyCode,
    DateOnly OccurredOn,
    string? Note);

public interface IFinanceService
{
    IdentityOperationResult<FinanceCategoryPage> ListCategories(IdentityPrincipal actor, string? query = null, int? limit = null);
    IdentityOperationResult<FinanceCategoryRecord> CreateCategory(IdentityPrincipal actor, FinanceCategoryCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FinanceCategoryRecord> UpdateCategory(IdentityPrincipal actor, Guid categoryId, string? ifMatch,
        FinanceCategoryCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<object?> RemoveCategory(IdentityPrincipal actor, Guid categoryId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FinanceManualRecordPage> ListRecords(IdentityPrincipal actor, Guid? categoryId = null,
        string? currencyCode = null, DateOnly? from = null, DateOnly? to = null, string? query = null, int? limit = null);
    IdentityOperationResult<FinanceManualRecord> CreateRecord(IdentityPrincipal actor, FinanceManualRecordCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<FinanceManualRecord> UpdateRecord(IdentityPrincipal actor, Guid recordId, string? ifMatch,
        FinanceManualRecordCommand command, string? idempotencyKey = null, string? traceId = null);
}
