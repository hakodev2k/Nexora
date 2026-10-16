using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Finance;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Finance;

/// <summary>
/// SQL-backed implementation of the deliberately small Finance manual-record
/// scope. It has no ledger accounts, inferred signs, currency conversion,
/// deletion or provider integration. Amounts remain decimal strings at the
/// API boundary and are owner-scoped in every query.
/// </summary>
public sealed class SqlFinanceService : IFinanceService
{
    private static readonly Regex AmountPattern = new("^[0-9]{1,20}(\\.[0-9]{1,8})?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex CurrencyPattern = new("^[A-Z]{3}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly FinanceListCursor _cursor;

    public SqlFinanceService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _cursor = new FinanceListCursor(secret);
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<FinanceCategoryPage> ListCategories(IdentityPrincipal actor, string? query = null, int? limit = null, string? cursor = null)
    {
        const string action = "finance.manual_category.read";
        if (!ModuleAvailable(actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : NormalizeTitle(query);
        if (normalizedQuery is { Length: > 100 }) return Failure<FinanceCategoryPage>("ValidationFailed", 422, "Category search must be at most 100 characters.");
        var filters = JsonSerializer.Serialize(new { query = normalizedQuery });
        string? afterTitle = null; Guid? afterId = null;
        if (!string.IsNullOrEmpty(cursor) && !_cursor.TryDecode(cursor, actor.OwnerId, "categories", filters, take, out afterTitle, out afterId))
            return Failure<FinanceCategoryPage>("InvalidCursor", 422, "Finance cursor does not match this query.");
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryPage>();
        var includeUsage = CanReadUsage(connection, transaction, actor);
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) c.[Id], c.[Title],
                   CASE WHEN @IncludeUsage = 1 THEN CONVERT(int, (SELECT COUNT_BIG(1) FROM [finance].[ManualRecord] r WHERE r.[OwnerId] = c.[OwnerId] AND r.[CategoryId] = c.[Id])) ELSE NULL END,
                   c.[CreatedAt], c.[UpdatedAt], c.[RowVersion], c.[NormalizedTitle]
            FROM [finance].[ManualCategory] c
            WHERE c.[OwnerId] = @OwnerId
              AND (@Query IS NULL OR c.[NormalizedTitle] LIKE @QueryLike ESCAPE N'~')
              AND (@AfterTitle IS NULL OR c.[NormalizedTitle] > @AfterTitle OR (c.[NormalizedTitle] = @AfterTitle AND c.[Id] > @AfterId))
            ORDER BY c.[NormalizedTitle], c.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@IncludeUsage", SqlDbType.Bit, includeUsage);
        Add(command, "@AfterTitle", SqlDbType.NVarChar, (object?)afterTitle ?? DBNull.Value, 100);
        Add(command, "@AfterId", SqlDbType.UniqueIdentifier, (object?)afterId ?? DBNull.Value);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 100);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{EscapeLike(normalizedQuery)}%", 202);
        var items = new List<FinanceCategoryRecord>(); var sortKeys = new List<string>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) { items.Add(ReadCategory(reader)); sortKeys.Add(reader.GetString(6)); }
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryPage>();
        transaction.Commit(); string? nextCursor = null;
        if (items.Count > take)
        { items.RemoveAt(take); nextCursor = _cursor.Encode(actor.OwnerId, "categories", filters, take, sortKeys[take - 1], items[^1].Id); }
        return IdentityOperationResult<FinanceCategoryPage>.Success(new FinanceCategoryPage(items, nextCursor));
    }

    public IdentityOperationResult<FinanceCategoryRecord> GetCategory(IdentityPrincipal actor, Guid categoryId)
    {
        const string action = "finance.manual_category.read";
        if (!ModuleAvailable(actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryRecord>();
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryRecord>();
        var value = ReadCategory(connection, transaction, actor.OwnerId, categoryId, false, CanReadUsage(connection, transaction, actor));
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceCategoryRecord>();
        transaction.Commit();
        return value is null ? Missing<FinanceCategoryRecord>() : IdentityOperationResult<FinanceCategoryRecord>.Success(value);
    }

    public IdentityOperationResult<FinanceManualRecord> GetRecord(IdentityPrincipal actor, Guid recordId)
    {
        const string action = "finance.manual_record.read";
        if (!ModuleAvailable(actor, "FX27", action)) return ModuleUnavailable<FinanceManualRecord>();
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceManualRecord>();
        var value = ReadRecord(connection, transaction, actor.OwnerId, recordId, false);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) return ModuleUnavailable<FinanceManualRecord>();
        transaction.Commit();
        return value is null ? Missing<FinanceManualRecord>() : IdentityOperationResult<FinanceManualRecord>.Success(value);
    }

    public IdentityOperationResult<FinanceCategoryRecord> CreateCategory(IdentityPrincipal actor, FinanceCategoryCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_category.create")) return ModuleUnavailable<FinanceCategoryRecord>();
        var validation = ValidateCategory(command.Title);
        if (validation is not null) return validation;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FinanceCategoryRecord>(connection, transaction, actor, "finance.manual_category.create", idempotencyKey,
            JsonSerializer.Serialize(new { title = command.Title.Trim() }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var id = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [finance].[ManualCategory] ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Title], [NormalizedTitle]) VALUES (@Id, @OwnerId, @UserId, @UserId, @Title, @NormalizedTitle);",
                ("@Id", SqlDbType.UniqueIdentifier, id), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId), ("@Title", SqlDbType.NVarChar, command.Title.Trim()),
                ("@NormalizedTitle", SqlDbType.NVarChar, NormalizeTitle(command.Title)));
            var created = ReadCategory(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<FinanceCategoryRecord>("PersistenceFailure", 500, "Category could not be loaded after creation."); }
            WriteAudit(connection, transaction, actor, id, "finance.manual_category.create", traceId);
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryCreated", 201, JsonSerializer.Serialize(new ReceiptResource(created.Id, created.ETag, ProjectionDigest(connection, transaction, actor, created))));
            return CommitResult(connection, transaction, actor, "finance.manual_category.create", created, created.Id, 201, "FinanceCategoryCreated");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<FinanceCategoryRecord>("CategoryDuplicate", 409, "A category with this name already exists.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<FinanceCategoryRecord>(exception);
        }
    }

    public IdentityOperationResult<FinanceCategoryRecord> UpdateCategory(IdentityPrincipal actor, Guid categoryId, string? ifMatch,
        FinanceCategoryCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_category.update")) return ModuleUnavailable<FinanceCategoryRecord>();
        var validation = ValidateCategory(command.Title);
        if (validation is not null) return validation;
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<FinanceCategoryRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FinanceCategoryRecord>(connection, transaction, actor, "finance.manual_category.update", idempotencyKey,
            JsonSerializer.Serialize(new { categoryId, ifMatch, title = command.Title.Trim() }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadCategory(connection, transaction, actor.OwnerId, categoryId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<FinanceCategoryRecord>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<FinanceCategoryRecord>();
            }
            Execute(connection, transaction,
                "UPDATE [finance].[ManualCategory] SET [Title] = @Title, [NormalizedTitle] = @NormalizedTitle, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Title", SqlDbType.NVarChar, command.Title.Trim()), ("@NormalizedTitle", SqlDbType.NVarChar, NormalizeTitle(command.Title)),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId), ("@Id", SqlDbType.UniqueIdentifier, categoryId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@RowVersion", SqlDbType.Binary, expectedVersion));
            var updated = ReadCategory(connection, transaction, actor.OwnerId, categoryId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<FinanceCategoryRecord>("PersistenceFailure", 500, "Category could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, categoryId, "finance.manual_category.update", traceId);
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryUpdated", 200, JsonSerializer.Serialize(new ReceiptResource(updated.Id, updated.ETag, ProjectionDigest(connection, transaction, actor, updated))));
            return CommitResult(connection, transaction, actor, "finance.manual_category.update", updated, updated.Id, 200, "FinanceCategoryUpdated");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<FinanceCategoryRecord>("CategoryDuplicate", 409, "A category with this name already exists.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<FinanceCategoryRecord>(exception);
        }
    }

    public IdentityOperationResult<object?> RemoveCategory(IdentityPrincipal actor, Guid categoryId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_category.remove")) return ModuleUnavailable<object?>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<object?>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "finance.manual_category.remove", idempotencyKey,
            JsonSerializer.Serialize(new { categoryId, ifMatch }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadCategory(connection, transaction, actor.OwnerId, categoryId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<object?>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<object?>();
            }
            if (current.UsageCount > 0)
            {
                transaction.Rollback();
                return Failure<object?>("CategoryInUse", 409, "A category referenced by manual records cannot be removed.");
            }
            Execute(connection, transaction, "DELETE FROM [finance].[ManualCategory] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Id", SqlDbType.UniqueIdentifier, categoryId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, categoryId, "finance.manual_category.remove", traceId);
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryRemoved", 204);
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_category.remove"))
            { transaction.Rollback(); return ModuleUnavailable<object?>(); }
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("FinanceCategoryRemoved");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<FinanceManualRecordPage> ListRecords(IdentityPrincipal actor, Guid? categoryId = null,
        string? currencyCode = null, DateOnly? from = null, DateOnly? to = null, string? query = null, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_record.read", "finance.manual_summary.read")) return ModuleUnavailable<FinanceManualRecordPage>();
        var validation = ValidateFilters(currencyCode, from, to);
        if (validation is not null) return validation;
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedCurrency = currencyCode?.Trim().ToUpperInvariant();
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (normalizedQuery is { Length: > 200 }) return Failure<FinanceManualRecordPage>("ValidationFailed", 422, "Record search must be at most 200 characters.");
        var filters = JsonSerializer.Serialize(new { categoryId, currencyCode = normalizedCurrency, from, to, query = normalizedQuery });
        DateOnly? afterDate = null; Guid? afterId = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!_cursor.TryDecode(cursor, actor.OwnerId, "records", filters, take, out var afterKey, out afterId) ||
                !DateOnly.TryParseExact(afterKey, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return Failure<FinanceManualRecordPage>("InvalidCursor", 422, "Finance cursor does not match this query.");
            afterDate = parsed;
        }
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_record.read", "finance.manual_summary.read")) return ModuleUnavailable<FinanceManualRecordPage>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) r.[Id], r.[CategoryId], c.[Title], r.[Amount], r.[CurrencyCode], r.[OccurredOn],
                   r.[Note], r.[CreatedAt], r.[UpdatedAt], r.[RowVersion]
            FROM [finance].[ManualRecord] r
            INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId]
            WHERE r.[OwnerId] = @OwnerId
              AND (@CategoryId IS NULL OR r.[CategoryId] = @CategoryId)
              AND (@CurrencyCode IS NULL OR r.[CurrencyCode] = @CurrencyCode)
              AND (@From IS NULL OR r.[OccurredOn] >= @From)
              AND (@To IS NULL OR r.[OccurredOn] <= @To)
              AND (@Query IS NULL OR c.[Title] LIKE @QueryLike ESCAPE N'~' OR r.[Note] LIKE @QueryLike ESCAPE N'~')
              AND (@AfterDate IS NULL OR r.[OccurredOn] < @AfterDate OR (r.[OccurredOn] = @AfterDate AND r.[Id] < @AfterId))
            ORDER BY r.[OccurredOn] DESC, r.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@AfterDate", SqlDbType.Date, (object?)ToDbDate(afterDate) ?? DBNull.Value);
        Add(command, "@AfterId", SqlDbType.UniqueIdentifier, (object?)afterId ?? DBNull.Value);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@CategoryId", SqlDbType.UniqueIdentifier, (object?)categoryId ?? DBNull.Value);
        Add(command, "@CurrencyCode", SqlDbType.Char, (object?)normalizedCurrency ?? DBNull.Value, 3);
        Add(command, "@From", SqlDbType.Date, (object?)ToDbDate(from) ?? DBNull.Value);
        Add(command, "@To", SqlDbType.Date, (object?)ToDbDate(to) ?? DBNull.Value);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{EscapeLike(normalizedQuery)}%", 402);
        var items = new List<FinanceManualRecord>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) items.Add(ReadRecord(reader));
        }

        using var summary = connection.CreateCommand();
        summary.Transaction = transaction;
        summary.CommandText = """
            SELECT r.[CurrencyCode], SUM(r.[Amount])
            FROM [finance].[ManualRecord] r
            INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId]
            WHERE r.[OwnerId] = @OwnerId
              AND (@CategoryId IS NULL OR r.[CategoryId] = @CategoryId)
              AND (@CurrencyCode IS NULL OR r.[CurrencyCode] = @CurrencyCode)
              AND (@From IS NULL OR r.[OccurredOn] >= @From)
              AND (@To IS NULL OR r.[OccurredOn] <= @To)
              AND (@Query IS NULL OR c.[Title] LIKE @QueryLike ESCAPE N'~' OR r.[Note] LIKE @QueryLike ESCAPE N'~')
            GROUP BY r.[CurrencyCode]
            ORDER BY r.[CurrencyCode];
            """;
        Add(summary, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(summary, "@CategoryId", SqlDbType.UniqueIdentifier, (object?)categoryId ?? DBNull.Value);
        Add(summary, "@CurrencyCode", SqlDbType.Char, (object?)normalizedCurrency ?? DBNull.Value, 3);
        Add(summary, "@From", SqlDbType.Date, (object?)ToDbDate(from) ?? DBNull.Value);
        Add(summary, "@To", SqlDbType.Date, (object?)ToDbDate(to) ?? DBNull.Value);
        Add(summary, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(summary, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{EscapeLike(normalizedQuery)}%", 402);
        var summaries = new List<FinanceSummary>();
        using (var reader = summary.ExecuteReader())
        {
            while (reader.Read()) summaries.Add(new FinanceSummary(reader.GetString(0), FormatSummary(reader.GetSqlDecimal(1))));
        }
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_record.read", "finance.manual_summary.read")) return ModuleUnavailable<FinanceManualRecordPage>();
        transaction.Commit(); string? nextCursor = null;
        if (items.Count > take)
        {
            items.RemoveAt(take); var last = items[^1];
            nextCursor = _cursor.Encode(actor.OwnerId, "records", filters, take, last.OccurredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), last.Id);
        }
        return IdentityOperationResult<FinanceManualRecordPage>.Success(new FinanceManualRecordPage(items, summaries, nextCursor));
    }

    public IdentityOperationResult<FinanceManualRecord> CreateRecord(IdentityPrincipal actor, FinanceManualRecordCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_record.create")) return ModuleUnavailable<FinanceManualRecord>();
        var validation = ValidateRecord(command);
        if (validation is not null) return validation;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var amount = ParseAmount(command.Amount);
        var receiptFailure = CheckReceipt<FinanceManualRecord>(connection, transaction, actor, "finance.manual_record.create", idempotencyKey,
            CanonicalRecord(command, amount), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            if (!CategoryExists(connection, transaction, actor.OwnerId, command.CategoryId))
            {
                transaction.Rollback();
                return Failure<FinanceManualRecord>("CategoryUnavailable", 422, "The selected category is unavailable.");
            }
            var id = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [finance].[ManualRecord] ([Id], [OwnerId], [CategoryId], [CreatedByUserId], [UpdatedByUserId], [Amount], [CurrencyCode], [OccurredOn], [Note]) VALUES (@Id, @OwnerId, @CategoryId, @UserId, @UserId, @Amount, @CurrencyCode, @OccurredOn, @Note);",
                ("@Id", SqlDbType.UniqueIdentifier, id), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@CategoryId", SqlDbType.UniqueIdentifier, command.CategoryId), ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Amount", SqlDbType.Decimal, amount), ("@CurrencyCode", SqlDbType.Char, command.CurrencyCode.Trim().ToUpperInvariant()),
                ("@OccurredOn", SqlDbType.Date, command.OccurredOn.ToDateTime(TimeOnly.MinValue)),
                ("@Note", SqlDbType.NVarChar, (object?)TrimOrNull(command.Note, 2000) ?? DBNull.Value));
            var created = ReadRecord(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<FinanceManualRecord>("PersistenceFailure", 500, "Record could not be loaded after creation."); }
            WriteAudit(connection, transaction, actor, id, "finance.manual_record.create", traceId);
            CompleteReceipt(connection, transaction, receipt, "FinanceRecordCreated", 201, JsonSerializer.Serialize(new ReceiptResource(created.Id, created.ETag, ProjectionDigest(connection, transaction, actor, created))));
            return CommitResult(connection, transaction, actor, "finance.manual_record.create", created, created.Id, 201, "FinanceRecordCreated");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<FinanceManualRecord>(exception);
        }
    }

    public IdentityOperationResult<FinanceManualRecord> UpdateRecord(IdentityPrincipal actor, Guid recordId, string? ifMatch,
        FinanceManualRecordCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_record.update")) return ModuleUnavailable<FinanceManualRecord>();
        var validation = ValidateRecord(command);
        if (validation is not null) return validation;
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<FinanceManualRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var amount = ParseAmount(command.Amount);
        var receiptFailure = CheckReceipt<FinanceManualRecord>(connection, transaction, actor, "finance.manual_record.update", idempotencyKey,
            JsonSerializer.Serialize(new { recordId, ifMatch, record = CanonicalRecord(command, amount) }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadRecord(connection, transaction, actor.OwnerId, recordId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<FinanceManualRecord>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<FinanceManualRecord>();
            }
            if (!CategoryExists(connection, transaction, actor.OwnerId, command.CategoryId))
            {
                transaction.Rollback();
                return Failure<FinanceManualRecord>("CategoryUnavailable", 422, "The selected category is unavailable.");
            }
            Execute(connection, transaction,
                "UPDATE [finance].[ManualRecord] SET [CategoryId] = @CategoryId, [UpdatedByUserId] = @UserId, [Amount] = @Amount, [CurrencyCode] = @CurrencyCode, [OccurredOn] = @OccurredOn, [Note] = @Note, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@CategoryId", SqlDbType.UniqueIdentifier, command.CategoryId), ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Amount", SqlDbType.Decimal, amount), ("@CurrencyCode", SqlDbType.Char, command.CurrencyCode.Trim().ToUpperInvariant()),
                ("@OccurredOn", SqlDbType.Date, command.OccurredOn.ToDateTime(TimeOnly.MinValue)),
                ("@Note", SqlDbType.NVarChar, (object?)TrimOrNull(command.Note, 2000) ?? DBNull.Value),
                ("@Id", SqlDbType.UniqueIdentifier, recordId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            var updated = ReadRecord(connection, transaction, actor.OwnerId, recordId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<FinanceManualRecord>("PersistenceFailure", 500, "Record could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, recordId, "finance.manual_record.update", traceId);
            CompleteReceipt(connection, transaction, receipt, "FinanceRecordUpdated", 200, JsonSerializer.Serialize(new ReceiptResource(updated.Id, updated.ETag, ProjectionDigest(connection, transaction, actor, updated))));
            return CommitResult(connection, transaction, actor, "finance.manual_record.update", updated, updated.Id, 200, "FinanceRecordUpdated");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<FinanceManualRecord>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static IdentityOperationResult<FinanceCategoryRecord>? ValidateCategory(string title) =>
        string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 1 or > 100
            ? Failure<FinanceCategoryRecord>("ValidationFailed", 422, "Category title is required and must be at most 100 characters.")
            : null;

    private static IdentityOperationResult<FinanceManualRecordPage>? ValidateFilters(string? currencyCode, DateOnly? from, DateOnly? to)
    {
        if (currencyCode is not null && !CurrencyPattern.IsMatch(currencyCode.Trim().ToUpperInvariant()))
            return Failure<FinanceManualRecordPage>("ValidationFailed", 422, "Currency code must be an explicit three-letter ISO code.");
        if (from is not null && to is not null && from > to)
            return Failure<FinanceManualRecordPage>("ValidationFailed", 422, "The date range is invalid.");
        return null;
    }

    private static IdentityOperationResult<FinanceManualRecord>? ValidateRecord(FinanceManualRecordCommand command)
    {
        if (command.CategoryId == Guid.Empty || !TryParseAmount(command.Amount, out _))
            return Failure<FinanceManualRecord>("ValidationFailed", 422, "Category and nonnegative decimal amount are required.");
        if ((string.IsNullOrWhiteSpace(command.CurrencyCode) || !CurrencyPattern.IsMatch(command.CurrencyCode.Trim().ToUpperInvariant())))
            return Failure<FinanceManualRecord>("ValidationFailed", 422, "Currency code must be an explicit three-letter ISO code.");
        if (command.Note is { Length: > 2000 })
            return Failure<FinanceManualRecord>("ValidationFailed", 422, "Record note must be at most 2000 characters.");
        return null;
    }

    private static bool TryParseAmount(string input, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(input) || !AmountPattern.IsMatch(input.Trim())) return false;
        if (!decimal.TryParse(input.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)) return false;
        return amount >= 0 && decimal.Round(amount, 8) == amount;
    }

    private static decimal ParseAmount(string input) => decimal.Parse(input.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
    private static string FormatSummary(System.Data.SqlTypes.SqlDecimal amount)
    {
        // SQL SUM is decimal(38,8), wider than CLR decimal: retain every digit.
        var value = amount.ToString();
        return value.Contains('.') ? value.TrimEnd('0').TrimEnd('.') : value;
    }
    private static string FormatAmount(decimal amount) => amount.ToString("0.########", CultureInfo.InvariantCulture);
    private static string NormalizeTitle(string value) => value.Trim().ToUpperInvariant();
    private static object? ToDbDate(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue);
    private static string? TrimOrNull(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(max, value.Trim().Length)];

    private static FinanceCategoryRecord ReadCategory(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt32(2), ToOffset(reader.GetDateTime(3)), ToOffset(reader.GetDateTime(4)), EncodeETag(reader.GetFieldValue<byte[]>(5)));

    private static FinanceCategoryRecord? ReadCategory(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid categoryId, bool forUpdate, bool includeUsage = true)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT c.[Id], c.[Title], CASE WHEN @IncludeUsage = 1 THEN CONVERT(int, (SELECT COUNT_BIG(1) FROM [finance].[ManualRecord] r WHERE r.[OwnerId] = c.[OwnerId] AND r.[CategoryId] = c.[Id])) ELSE NULL END, c.[CreatedAt], c.[UpdatedAt], c.[RowVersion] FROM [finance].[ManualCategory] c WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")}) WHERE c.[Id] = @Id AND c.[OwnerId] = @OwnerId;";
        Add(command, "@IncludeUsage", SqlDbType.Bit, includeUsage);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, categoryId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadCategory(reader) : null;
    }

    private static FinanceManualRecord ReadRecord(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), FormatAmount(reader.GetDecimal(3)), reader.GetString(4),
        DateOnly.FromDateTime(reader.GetDateTime(5)), reader.IsDBNull(6) ? null : reader.GetString(6),
        ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag(reader.GetFieldValue<byte[]>(9)));

    private static FinanceManualRecord? ReadRecord(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid recordId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT r.[Id], r.[CategoryId], c.[Title], r.[Amount], r.[CurrencyCode], r.[OccurredOn], r.[Note], r.[CreatedAt], r.[UpdatedAt], r.[RowVersion] FROM [finance].[ManualRecord] r WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")}) INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId] WHERE r.[Id] = @Id AND r.[OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, recordId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRecord(reader) : null;
    }

    private static bool CategoryExists(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid categoryId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM [finance].[ManualCategory] WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, categoryId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return command.ExecuteScalar() is not null;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        // Trusted command key, never derived from user-controlled canonical text.
        // Serializable auth reads share the write transaction, including null keys.
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", operationKey)) return ModuleUnavailable<T>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay && claim.ResultStatusCode == 204) return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "FinanceCategoryRemoved");
        if (claim.IsReplay && claim.ResultStatusCode is 200 or 201 && !string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            try
            {
                var resource = JsonSerializer.Deserialize<ReceiptResource>(claim.ResultJson);
                if (resource is not null && resource.Id != Guid.Empty)
                {
                    var code = claim.ResultCode ?? "Ok"; var status = claim.ResultStatusCode.Value;
                    if (typeof(T) == typeof(FinanceCategoryRecord) && _capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_category.read"))
                    {
                        var current = ReadCategory(connection, transaction, actor.OwnerId, resource.Id, false, CanReadUsage(connection, transaction, actor));
                        if (current is not null && current.ETag == resource.ETag && resource.ResponseDigest is not null && _receipts.DigestSensitive(JsonSerializer.Serialize(current)) == resource.ResponseDigest)
                            return IdentityOperationResult<T>.Success((T)(object)current, status, code);
                    }
                    else if (typeof(T) == typeof(FinanceManualRecord) && _capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_record.read"))
                    {
                        var current = ReadRecord(connection, transaction, actor.OwnerId, resource.Id, false);
                        if (current is not null && current.ETag == resource.ETag && resource.ResponseDigest is not null && _receipts.DigestSensitive(JsonSerializer.Serialize(current)) == resource.ResponseDigest)
                            return IdentityOperationResult<T>.Success((T)(object)current, status, code);
                    }
                    // Prior commit remains success after read revocation, source
                    // changes, deletion or newer revision; never rerun the write.
                    return IdentityOperationResult<T>.Committed(resource.Id, status, code);
                }
            }
            catch (JsonException) { }
        }
        var failure = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(failure, 409, "The request was already completed or is in progress.");
    }

    private IdentityOperationResult<T> CommitResult<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string action, T value, Guid id, int status, string code)
    {
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", action)) { transaction.Rollback(); return ModuleUnavailable<T>(); }
        var readAction = value is FinanceCategoryRecord ? "finance.manual_category.read" : "finance.manual_record.read";
        var canRead = _capabilities.IsAllowed(connection, transaction, actor, "FX27", readAction);
        if (canRead && value is FinanceCategoryRecord category && !CanReadUsage(connection, transaction, actor)) value = (T)(object)(category with { UsageCount = null });
        // No fallible detail query runs after commit.
        transaction.Commit();
        return canRead ? IdentityOperationResult<T>.Success(value, status, code) : IdentityOperationResult<T>.Committed(id, status, code);
    }

    private bool CanReadUsage(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX27", "finance.manual_record.read", "finance.manual_summary.read");
    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode,
        int? statusCode = null, string? resultJson = null) => _receipts.Complete(connection, transaction, claim, resultCode, statusCode, resultJson);
    private string? ProjectionDigest<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, T value)
    {
        var readAction = value is FinanceCategoryRecord ? "finance.manual_category.read" : "finance.manual_record.read";
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX27", readAction)) return null;
        if (value is FinanceCategoryRecord category && !CanReadUsage(connection, transaction, actor)) value = (T)(object)(category with { UsageCount = null });
        // Keyed digest includes derived count/source title without storing
        // plaintext details. Originally opaque results remain opaque.
        return _receipts.DigestSensitive(JsonSerializer.Serialize(value));
    }
    private sealed record ReceiptResource(Guid Id, string ETag, string? ResponseDigest);
    private static string CanonicalRecord(FinanceManualRecordCommand command, decimal amount) => JsonSerializer.Serialize(new
    { command.CategoryId, amount = FormatAmount(amount), currency = command.CurrencyCode.Trim().ToUpperInvariant(), command.OccurredOn, note = TrimOrNull(command.Note, 2000) });
    private static string EscapeLike(string value) => value.Replace("~", "~~", StringComparison.Ordinal).Replace("%", "~%", StringComparison.Ordinal).Replace("_", "~_", StringComparison.Ordinal).Replace("[", "~[", StringComparison.Ordinal);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, @TargetType, @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, actor.UserId), ("@Action", SqlDbType.NVarChar, actionKey), ("@TargetType", SqlDbType.NVarChar, actionKey.StartsWith("finance.manual_category.", StringComparison.Ordinal) ? "finance.ManualCategory" : "finance.ManualRecord"), ("@Target", SqlDbType.UniqueIdentifier, targetId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is null ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size.Value);
        parameter.Value = value;
        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 28;
            parameter.Scale = 8;
        }
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Finance is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Finance resource unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Finance resource revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Finance persistence is unavailable.");
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8 && EncodeETag(bytes) == value;
        }
        catch (FormatException) { return false; }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
