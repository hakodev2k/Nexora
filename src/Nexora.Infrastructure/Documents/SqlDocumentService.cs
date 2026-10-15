using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Documents;

/// <summary>
/// Owner-scoped Documents/Notes/Knowledge page core. The service deliberately
/// keeps folder, file, sharing and import/export concerns out of this slice;
/// every body write is versioned in SQL and guarded by the current rowversion.
/// </summary>
public sealed class SqlDocumentService : IDocumentService
{
    private const int MaxBodyBytes = 1_048_576;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly DocumentListCursor _cursor;

    public SqlDocumentService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _cursor = new DocumentListCursor(secret);
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<DocumentPage> List(IdentityPrincipal actor, string? status = null, int? limit = null,
        string? cursor = null, string? query = null, string? documentType = null)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.library.read")) return ModuleUnavailable<DocumentPage>();
        var normalizedStatus = NormalizeStatus(status);
        if (status is not null && normalizedStatus is null)
            return Failure<DocumentPage>("ValidationFailed", 422, "Document status is invalid.");

        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length > 200)
            return Failure<DocumentPage>("ValidationFailed", 422, "Document title query must be at most 200 characters.");
        var normalizedType = string.IsNullOrWhiteSpace(documentType) ? null : documentType.Trim();
        if (normalizedType is not null && normalizedType is not ("Document" or "Note" or "Knowledge"))
            return Failure<DocumentPage>("ValidationFailed", 422, "Document type is invalid.");
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var cursorContext = new DocumentListCursor.Context(actor.OwnerId, normalizedStatus, normalizedQuery, normalizedType, take);
        DocumentListCursor.Position? after = null;
        if (!string.IsNullOrEmpty(cursor) && !_cursor.TryDecode(cursor, cursorContext, out after))
            return Failure<DocumentPage>("InvalidCursor", 422, "Document cursor does not match this query.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", "documents.library.read")) return ModuleUnavailable<DocumentPage>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Title], [DocumentType], [EditorMode], [Status],
                   [PreArchiveStatus], [VersionNumber], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [documents].[Page] p
            WHERE p.[OwnerId] = @OwnerId AND p.[DeletedAt] IS NULL
              AND p.[Status] IN ('Draft', 'Published', 'Archived')
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Document'
                    AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
              AND ((@Status IS NULL AND p.[Status] <> 'Archived') OR p.[Status] = @Status)
              AND (@Type IS NULL OR p.[DocumentType] = @Type)
              AND (@TitleQuery = N'' OR p.[Title] LIKE @TitleLike ESCAPE N'\')
              AND (@AfterUpdatedAt IS NULL OR p.[UpdatedAt] < @AfterUpdatedAt
                   OR (p.[UpdatedAt] = @AfterUpdatedAt AND p.[Id] < @AfterId))
            ORDER BY [UpdatedAt] DESC, [Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@Type", SqlDbType.VarChar, (object?)normalizedType ?? DBNull.Value, 32);
        Add(command, "@TitleQuery", SqlDbType.NVarChar, normalizedQuery, 200);
        Add(command, "@TitleLike", SqlDbType.NVarChar, "%" + normalizedQuery.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal).Replace("[", "\\[", StringComparison.Ordinal) + "%", 402);
        Add(command, "@AfterUpdatedAt", SqlDbType.DateTime2, (object?)after?.UpdatedAt ?? DBNull.Value);
        Add(command, "@AfterId", SqlDbType.UniqueIdentifier, (object?)after?.Id ?? DBNull.Value);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Status", SqlDbType.VarChar, (object?)normalizedStatus ?? DBNull.Value, 16);
        using var reader = command.ExecuteReader();
        var items = new List<DocumentSummary>();
        while (reader.Read()) items.Add(ReadSummary(reader));
        reader.Close();
        var hasMore = items.Count > take;
        if (hasMore) items.RemoveAt(items.Count - 1);
        var nextCursor = hasMore ? _cursor.Encode(cursorContext, items[^1].UpdatedAt, items[^1].Id) : null;
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", "documents.library.read")) return ModuleUnavailable<DocumentPage>();
        transaction.Commit();
        return IdentityOperationResult<DocumentPage>.Success(new DocumentPage(items, nextCursor));
    }

    public IdentityOperationResult<DocumentDetail> Get(IdentityPrincipal actor, Guid documentId)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.page.read")) return ModuleUnavailable<DocumentDetail>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", "documents.page.read")) return ModuleUnavailable<DocumentDetail>();
        var document = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: false);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", "documents.page.read")) return ModuleUnavailable<DocumentDetail>();
        transaction.Commit();
        return document is null ? Missing() : IdentityOperationResult<DocumentDetail>.Success(document);
    }

    public IdentityOperationResult<DocumentDetail> Create(IdentityPrincipal actor, DocumentCreateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.page.create")) return ModuleUnavailable<DocumentDetail>();
        var validation = ValidateCreate(command);
        if (validation is not null) return validation;

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "documents.page.create", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, OwnerId = actor.OwnerId, Title = command.Title.Trim(), command.DocumentType, command.EditorMode, Body = NormalizeBody(command.Body) }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var id = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [documents].[Page] ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Title], [DocumentType], [EditorMode], [Body], [Status], [VersionNumber]) VALUES (@Id, @OwnerId, @UserId, @UserId, @Title, @DocumentType, @EditorMode, @Body, 'Draft', 1);",
                ("@Id", SqlDbType.UniqueIdentifier, id),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Title", SqlDbType.NVarChar, command.Title.Trim()),
                ("@DocumentType", SqlDbType.VarChar, command.DocumentType),
                ("@EditorMode", SqlDbType.VarChar, command.EditorMode),
                ("@Body", SqlDbType.NVarChar, (object?)NormalizeBody(command.Body) ?? string.Empty));
            Execute(connection, transaction,
                "INSERT INTO [documents].[PageVersion] ([PageId], [OwnerId], [VersionNumber], [Title], [Body], [DocumentType], [EditorMode], [Status], [ChangeNote], [CreatedByUserId]) VALUES (@PageId, @OwnerId, 1, @Title, @Body, @DocumentType, @EditorMode, 'Draft', @ChangeNote, @UserId);",
                ("@PageId", SqlDbType.UniqueIdentifier, id),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@Title", SqlDbType.NVarChar, command.Title.Trim()),
                ("@Body", SqlDbType.NVarChar, (object?)NormalizeBody(command.Body) ?? string.Empty),
                ("@DocumentType", SqlDbType.VarChar, command.DocumentType),
                ("@EditorMode", SqlDbType.VarChar, command.EditorMode),
                ("@ChangeNote", SqlDbType.NVarChar, DBNull.Value),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId));
            WriteAudit(connection, transaction, actor, id, "documents.page.create", traceId);
            var created = ReadDetail(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<DocumentDetail>("PersistenceFailure", 500, "Document could not be loaded after creation."); }
            return CommitResource(connection, transaction, actor, receipt, created, "documents.page.create", "DocumentCreated", 201);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<DocumentDetail>(exception);
        }
    }

    public IdentityOperationResult<DocumentDetail> Save(IdentityPrincipal actor, Guid documentId, string? ifMatch,
        DocumentSaveCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.page.save")) return ModuleUnavailable<DocumentDetail>();
        var validation = ValidateSave(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "documents.page.save", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, OwnerId = actor.OwnerId, Id = documentId, ETag = EncodeETag(expectedVersion), Title = command.Title.Trim(), Body = NormalizeBody(command.Body), ChangeNote = TrimOrNull(command.ChangeNote, 500) }), out var receipt, documentId);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision();
            }
            if (current.Status is not ("Draft" or "Published"))
            {
                transaction.Rollback();
                return Failure<DocumentDetail>("LifecycleLocked", 409, "Archived documents are read-only.");
            }
            var nextVersion = current.VersionNumber + 1;
            Execute(connection, transaction,
                "UPDATE [documents].[Page] SET [Title] = @Title, [Body] = @Body, [VersionNumber] = @VersionNumber, [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [DeletedAt] IS NULL;",
                ("@Title", SqlDbType.NVarChar, command.Title.Trim()),
                ("@Body", SqlDbType.NVarChar, (object?)NormalizeBody(command.Body) ?? string.Empty),
                ("@VersionNumber", SqlDbType.BigInt, nextVersion),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, documentId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            Execute(connection, transaction,
                "INSERT INTO [documents].[PageVersion] ([PageId], [OwnerId], [VersionNumber], [Title], [Body], [DocumentType], [EditorMode], [Status], [ChangeNote], [CreatedByUserId]) VALUES (@PageId, @OwnerId, @VersionNumber, @Title, @Body, @DocumentType, @EditorMode, @Status, @ChangeNote, @UserId);",
                ("@PageId", SqlDbType.UniqueIdentifier, documentId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@VersionNumber", SqlDbType.BigInt, nextVersion),
                ("@Title", SqlDbType.NVarChar, command.Title.Trim()),
                ("@Body", SqlDbType.NVarChar, (object?)NormalizeBody(command.Body) ?? string.Empty),
                ("@DocumentType", SqlDbType.VarChar, current.DocumentType),
                ("@EditorMode", SqlDbType.VarChar, current.EditorMode),
                ("@Status", SqlDbType.VarChar, current.Status),
                ("@ChangeNote", SqlDbType.NVarChar, (object?)TrimOrNull(command.ChangeNote, 500) ?? DBNull.Value),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId));
            WriteAudit(connection, transaction, actor, documentId, "documents.page.save", traceId);
            var saved = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: false);
            if (saved is null) { transaction.Rollback(); return Failure<DocumentDetail>("PersistenceFailure", 500, "Document could not be loaded after save."); }
            return CommitResource(connection, transaction, actor, receipt, saved, "documents.page.save", "DocumentSaved", 200);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<DocumentDetail>(exception);
        }
    }

    public IdentityOperationResult<DocumentDetail> Transition(IdentityPrincipal actor, Guid documentId, string? ifMatch,
        DocumentTransitionCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (command.Status is not ("Draft" or "Published" or "Archived"))
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document status must be Draft, Published or Archived.");
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "documents.page.transition", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, OwnerId = actor.OwnerId, Id = documentId, ETag = EncodeETag(expectedVersion), command.Status }), out var receipt, documentId, command.Status);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision();
            }
            if (!IsAllowedTransition(current.Status, command.Status, current.PreArchiveStatus))
            {
                transaction.Rollback();
                return Failure<DocumentDetail>("LifecycleLocked", 409, "The requested document lifecycle transition is not allowed.");
            }
            var action = current.Status == "Archived" && command.Status != "Archived"
                ? "documents.page.unarchive" : DocumentTransitionAction(command.Status);
            if (!CurrentAuthority(connection, transaction, actor, action))
            { transaction.Rollback(); return ModuleUnavailable<DocumentDetail>(); }
            if (current.Status != command.Status) Execute(connection, transaction,
                "UPDATE [documents].[Page] SET [Status] = @Status, [PreArchiveStatus] = CASE WHEN @Status = 'Archived' THEN [Status] ELSE NULL END, [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [DeletedAt] IS NULL;",
                ("@Status", SqlDbType.VarChar, command.Status),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, documentId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, documentId, action, traceId);
            var transitioned = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: false);
            if (transitioned is null) { transaction.Rollback(); return Failure<DocumentDetail>("PersistenceFailure", 500, "Document could not be loaded after transition."); }
            return CommitResource(connection, transaction, actor, receipt, transitioned, action, "DocumentTransitioned", 200);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<DocumentDetail>(exception);
        }
    }

    private static IdentityOperationResult<DocumentDetail>? ValidateCreate(DocumentCreateCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document title is required and must be at most 200 characters.");
        if (command.DocumentType is not ("Document" or "Note" or "Knowledge"))
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document type must be Document, Note or Knowledge.");
        if (command.EditorMode is not ("Markdown" or "Block"))
            return Failure<DocumentDetail>("ValidationFailed", 422, "Editor mode must be Markdown or Block.");
        return ValidateBody(command.Body);
    }

    private static IdentityOperationResult<DocumentDetail>? ValidateSave(DocumentSaveCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document title is required and must be at most 200 characters.");
        if (command.ChangeNote is { Length: > 500 })
            return Failure<DocumentDetail>("ValidationFailed", 422, "Change note is too long.");
        return ValidateBody(command.Body);
    }

    private static IdentityOperationResult<DocumentDetail>? ValidateBody(string? body)
    {
        if (body is null) return null;
        if (Encoding.UTF8.GetByteCount(body) > MaxBodyBytes || body.Contains('\0'))
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document body exceeds the 1 MiB limit or contains an invalid character.");
        if (body.Contains("<script", StringComparison.OrdinalIgnoreCase) || body.Contains("<iframe", StringComparison.OrdinalIgnoreCase) || body.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
            return Failure<DocumentDetail>("UnsafeMarkup", 422, "Executable markup is not accepted in document content.");
        return null;
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static string DocumentTransitionAction(string status) => status switch
    {
        "Published" => "documents.page.publish",
        "Draft" => "documents.page.unpublish",
        "Archived" => "documents.page.archive",
        _ => "documents.page.save"
    };

    private static DocumentDetail? ReadDetail(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid documentId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [Title], [DocumentType], [EditorMode], [Body], [Status], [PreArchiveStatus], [VersionNumber], [CreatedAt], [UpdatedAt], [RowVersion] FROM [documents].[Page] p {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)} WHERE p.[Id] = @Id AND p.[OwnerId] = @OwnerId AND p.[DeletedAt] IS NULL AND p.[Status] IN ('Draft', 'Published', 'Archived') AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Document' AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, documentId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadDetail(reader) : null;
    }

    private static DocumentSummary ReadSummary(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetInt64(6), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag((byte[])reader[9]));

    private static DocumentDetail ReadDetail(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt64(7), ToOffset(reader.GetDateTime(8)), ToOffset(reader.GetDateTime(9)), EncodeETag((byte[])reader[10]));

    private static IdentityOperationResult<DocumentDetail> Missing() => Failure<DocumentDetail>("ResourceUnavailable", 404, "Document unavailable.");
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "The requested module is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<DocumentDetail> Precondition(string? ifMatch) => string.IsNullOrWhiteSpace(ifMatch)
        ? Failure<DocumentDetail>("PreconditionRequired", 428, "If-Match is required.")
        : Failure<DocumentDetail>("RevisionConflict", 412, "If-Match is invalid.");
    private static IdentityOperationResult<DocumentDetail> Revision() => Failure<DocumentDetail>("RevisionConflict", 412, "Document revision changed.");
    private static bool IsAllowedTransition(string current, string next, string? preArchiveStatus = null) => current == next || current switch
    {
        "Draft" => next == "Published" || next == "Archived",
        "Published" => next == "Draft" || next == "Archived",
        "Archived" => preArchiveStatus is "Draft" or "Published" && next == preArchiveStatus,
        _ => false
    };

    private static string? NormalizeStatus(string? value) => value?.Trim() switch
    {
        null => null,
        "Draft" => "Draft",
        "Published" => "Published",
        "Archived" => "Archived",
        _ => null
    };

    private static string NormalizeBody(string? body) => body ?? string.Empty;
    private static string? TrimOrNull(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(max, value.Trim().Length)];
    private static bool TryDecodeETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }

    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record DocumentReceipt(Guid Id, string ETag, string Action);

    private bool CurrentAuthority(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string action) => _capabilities.IsAllowed(connection, transaction, actor, "FX20", action);

    private IdentityOperationResult<DocumentDetail> CommitResource(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, ReceiptClaim claim, DocumentDetail detail, string action, string code, int status)
    {
        _receipts.Complete(connection, transaction, claim, code, status,
            JsonSerializer.Serialize(new DocumentReceipt(detail.Id, detail.ETag, action)));
        if (!CurrentAuthority(connection, transaction, actor, action))
        { transaction.Rollback(); return ModuleUnavailable<DocumentDetail>(); }
        var canRead = CurrentAuthority(connection, transaction, actor, "documents.page.read");
        // Time may expire without another transaction changing any locked row.
        if (!CurrentAuthority(connection, transaction, actor, action))
        { transaction.Rollback(); return ModuleUnavailable<DocumentDetail>(); }
        transaction.Commit();
        return canRead ? IdentityOperationResult<DocumentDetail>.Success(detail, status, code)
            : IdentityOperationResult<DocumentDetail>.Committed(detail.Id, status, "DocumentCommitted");
    }

    private IdentityOperationResult<DocumentDetail>? CheckReceipt(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim, Guid? expectedId = null, string? targetStatus = null)
    {
        claim = default;
        string[] actions = operationKey switch
        {
            "documents.page.create" => ["documents.page.create"],
            "documents.page.save" => ["documents.page.save"],
            "documents.page.transition" => ["documents.page.publish", "documents.page.unpublish", "documents.page.archive", "documents.page.unarchive"],
            _ => []
        };
        if (!actions.Any(action => CurrentAuthority(connection, transaction, actor, action))) return ModuleUnavailable<DocumentDetail>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Failure<DocumentDetail>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay)
        {
            DocumentReceipt? stored;
            try { stored = JsonSerializer.Deserialize<DocumentReceipt>(claim.ResultJson ?? string.Empty); }
            catch (JsonException) { stored = null; }
            if (stored is null || stored.Id == Guid.Empty || !TryDecodeETag(stored.ETag, out _) ||
                !actions.Contains(stored.Action, StringComparer.Ordinal) || (expectedId is not null && stored.Id != expectedId) ||
                (operationKey == "documents.page.transition" && !(stored.Action switch
                {
                    "documents.page.publish" => targetStatus == "Published",
                    "documents.page.unpublish" => targetStatus == "Draft",
                    "documents.page.archive" => targetStatus == "Archived",
                    "documents.page.unarchive" => targetStatus is "Draft" or "Published",
                    _ => false
                })) ||
                claim.ResultStatusCode != (operationKey == "documents.page.create" ? 201 : 200) ||
                claim.ResultCode != (operationKey == "documents.page.create" ? "DocumentCreated" : operationKey == "documents.page.save" ? "DocumentSaved" : "DocumentTransitioned"))
                return Failure<DocumentDetail>("ReceiptUnavailable", 503, "The committed document receipt is unavailable.");
            if (!CurrentAuthority(connection, transaction, actor, stored.Action)) return ModuleUnavailable<DocumentDetail>();
            DocumentDetail? current = null;
            if (CurrentAuthority(connection, transaction, actor, "documents.page.read"))
                current = ReadDetail(connection, transaction, actor.OwnerId, stored.Id, forUpdate: false);
            var canRead = current is not null && current.ETag == stored.ETag &&
                CurrentAuthority(connection, transaction, actor, "documents.page.read");
            if (!CurrentAuthority(connection, transaction, actor, stored.Action)) return ModuleUnavailable<DocumentDetail>();
            return canRead ? IdentityOperationResult<DocumentDetail>.Success(current!, claim.ResultStatusCode.Value, claim.ResultCode)
                : IdentityOperationResult<DocumentDetail>.Committed(stored.Id, claim.ResultStatusCode.Value, "DocumentCommitted");
        }
        return Failure<DocumentDetail>(claim.IsConflict ? "IdempotencyConflict" : "RequestInProgress", 409,
            claim.IsConflict ? "The same Idempotency-Key was already used with a different request or older receipt format." : "The request is in progress.");
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId)
    {
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@ActorUserId, @OwnerUserId, @Action, N'DocumentPage', @TargetId, 'Succeeded', @TraceId);",
            ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId),
            ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId),
            ("@Action", SqlDbType.NVarChar, action),
            ("@TargetId", SqlDbType.UniqueIdentifier, targetId),
            ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));
    }

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string text, params (string Name, SqlDbType Type, object Value)[] values)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = text;
        foreach (var value in values) Add(command, value.Name, value.Type, value.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type, size);
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) =>
        Failure<T>("PersistenceFailure", 503, "The SQL persistence operation could not be completed.");
}
