using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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

    public SqlFinanceService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<FinanceCategoryPage> ListCategories(IdentityPrincipal actor, string? query = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_category.read")) return ModuleUnavailable<FinanceCategoryPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : NormalizeTitle(query);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) c.[Id], c.[Title],
                   CONVERT(int, (SELECT COUNT_BIG(1) FROM [finance].[ManualRecord] r WHERE r.[OwnerId] = c.[OwnerId] AND r.[CategoryId] = c.[Id])),
                   c.[CreatedAt], c.[UpdatedAt], c.[RowVersion]
            FROM [finance].[ManualCategory] c WITH (NOLOCK)
            WHERE c.[OwnerId] = @OwnerId AND (@Query IS NULL OR c.[NormalizedTitle] LIKE @QueryLike)
            ORDER BY c.[NormalizedTitle], c.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 100);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 104);
        using var reader = command.ExecuteReader();
        var items = new List<FinanceCategoryRecord>();
        while (reader.Read()) items.Add(ReadCategory(reader));
        return IdentityOperationResult<FinanceCategoryPage>.Success(new FinanceCategoryPage(items, null));
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
            $"title:{NormalizeTitle(command.Title)}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryCreated");
            transaction.Commit();
            return IdentityOperationResult<FinanceCategoryRecord>.Success(created, 201, "FinanceCategoryCreated");
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
            $"category:{categoryId:N}|etag:{ifMatch}|title:{NormalizeTitle(command.Title)}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryUpdated");
            transaction.Commit();
            return IdentityOperationResult<FinanceCategoryRecord>.Success(updated);
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
            $"category:{categoryId:N}|etag:{ifMatch}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "FinanceCategoryRemoved");
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
        string? currencyCode = null, DateOnly? from = null, DateOnly? to = null, string? query = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX27", "finance.manual_record.read", "finance.manual_summary.read")) return ModuleUnavailable<FinanceManualRecordPage>();
        var validation = ValidateFilters(currencyCode, from, to);
        if (validation is not null) return validation;
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedCurrency = currencyCode?.Trim().ToUpperInvariant();
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) r.[Id], r.[CategoryId], c.[Title], r.[Amount], r.[CurrencyCode], r.[OccurredOn],
                   r.[Note], r.[CreatedAt], r.[UpdatedAt], r.[RowVersion]
            FROM [finance].[ManualRecord] r WITH (NOLOCK)
            INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId]
            WHERE r.[OwnerId] = @OwnerId
              AND (@CategoryId IS NULL OR r.[CategoryId] = @CategoryId)
              AND (@CurrencyCode IS NULL OR r.[CurrencyCode] = @CurrencyCode)
              AND (@From IS NULL OR r.[OccurredOn] >= @From)
              AND (@To IS NULL OR r.[OccurredOn] <= @To)
              AND (@Query IS NULL OR c.[Title] LIKE @QueryLike OR r.[Note] LIKE @QueryLike)
            ORDER BY r.[OccurredOn] DESC, r.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@CategoryId", SqlDbType.UniqueIdentifier, (object?)categoryId ?? DBNull.Value);
        Add(command, "@CurrencyCode", SqlDbType.Char, (object?)normalizedCurrency ?? DBNull.Value, 3);
        Add(command, "@From", SqlDbType.Date, (object?)ToDbDate(from) ?? DBNull.Value);
        Add(command, "@To", SqlDbType.Date, (object?)ToDbDate(to) ?? DBNull.Value);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 202);
        var items = new List<FinanceManualRecord>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) items.Add(ReadRecord(reader));
        }

        using var summary = connection.CreateCommand();
        summary.CommandText = """
            SELECT r.[CurrencyCode], SUM(r.[Amount])
            FROM [finance].[ManualRecord] r
            INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId]
            WHERE r.[OwnerId] = @OwnerId
              AND (@CategoryId IS NULL OR r.[CategoryId] = @CategoryId)
              AND (@CurrencyCode IS NULL OR r.[CurrencyCode] = @CurrencyCode)
              AND (@From IS NULL OR r.[OccurredOn] >= @From)
              AND (@To IS NULL OR r.[OccurredOn] <= @To)
              AND (@Query IS NULL OR c.[Title] LIKE @QueryLike OR r.[Note] LIKE @QueryLike)
            GROUP BY r.[CurrencyCode]
            ORDER BY r.[CurrencyCode];
            """;
        Add(summary, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(summary, "@CategoryId", SqlDbType.UniqueIdentifier, (object?)categoryId ?? DBNull.Value);
        Add(summary, "@CurrencyCode", SqlDbType.Char, (object?)normalizedCurrency ?? DBNull.Value, 3);
        Add(summary, "@From", SqlDbType.Date, (object?)ToDbDate(from) ?? DBNull.Value);
        Add(summary, "@To", SqlDbType.Date, (object?)ToDbDate(to) ?? DBNull.Value);
        Add(summary, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(summary, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 202);
        var summaries = new List<FinanceSummary>();
        using (var reader = summary.ExecuteReader())
        {
            while (reader.Read()) summaries.Add(new FinanceSummary(reader.GetString(0), FormatAmount(reader.GetDecimal(1))));
        }
        return IdentityOperationResult<FinanceManualRecordPage>.Success(new FinanceManualRecordPage(items, summaries, null));
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
            $"category:{command.CategoryId:N}|amount:{FormatAmount(amount)}|currency:{command.CurrencyCode.Trim().ToUpperInvariant()}|date:{command.OccurredOn:yyyy-MM-dd}|note:{command.Note?.Trim()}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "FinanceRecordCreated");
            transaction.Commit();
            return IdentityOperationResult<FinanceManualRecord>.Success(created, 201, "FinanceRecordCreated");
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
            $"record:{recordId:N}|etag:{ifMatch}|category:{command.CategoryId:N}|amount:{FormatAmount(amount)}|currency:{command.CurrencyCode.Trim().ToUpperInvariant()}|date:{command.OccurredOn:yyyy-MM-dd}|note:{command.Note?.Trim()}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "FinanceRecordUpdated");
            transaction.Commit();
            return IdentityOperationResult<FinanceManualRecord>.Success(updated);
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
        if (!CurrencyPattern.IsMatch(command.CurrencyCode.Trim().ToUpperInvariant()))
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
    private static string FormatAmount(decimal amount) => amount.ToString("0.########", CultureInfo.InvariantCulture);
    private static string NormalizeTitle(string value) => value.Trim().ToUpperInvariant();
    private static object? ToDbDate(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue);
    private static string? TrimOrNull(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(max, value.Trim().Length)];

    private static FinanceCategoryRecord ReadCategory(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2), ToOffset(reader.GetDateTime(3)), ToOffset(reader.GetDateTime(4)), EncodeETag(reader.GetFieldValue<byte[]>(5)));

    private static FinanceCategoryRecord? ReadCategory(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid categoryId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT c.[Id], c.[Title], CONVERT(int, (SELECT COUNT_BIG(1) FROM [finance].[ManualRecord] r WHERE r.[OwnerId] = c.[OwnerId] AND r.[CategoryId] = c.[Id])), c.[CreatedAt], c.[UpdatedAt], c.[RowVersion] FROM [finance].[ManualCategory] c WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE c.[Id] = @Id AND c.[OwnerId] = @OwnerId;";
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
        command.CommandText = $"SELECT r.[Id], r.[CategoryId], c.[Title], r.[Amount], r.[CurrencyCode], r.[OccurredOn], r.[Note], r.[CreatedAt], r.[UpdatedAt], r.[RowVersion] FROM [finance].[ManualRecord] r WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) INNER JOIN [finance].[ManualCategory] c ON c.[Id] = r.[CategoryId] AND c.[OwnerId] = r.[OwnerId] WHERE r.[Id] = @Id AND r.[OwnerId] = @OwnerId;";
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
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) => _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'finance.ManualRecord', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@Action", SqlDbType.NVarChar, actionKey), ("@Target", SqlDbType.UniqueIdentifier, targetId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

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
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "*") return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
