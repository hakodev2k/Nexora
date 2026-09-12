using System.Data;
using System.Security.Cryptography;
using System.Text;
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

    public SqlDocumentService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<DocumentPage> List(IdentityPrincipal actor, string? status = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.library.read")) return ModuleUnavailable<DocumentPage>();
        var normalizedStatus = NormalizeStatus(status);
        if (status is not null && normalizedStatus is null)
            return Failure<DocumentPage>("ValidationFailed", 422, "Document status is invalid.");

        var take = Math.Clamp(limit ?? 25, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Title], [DocumentType], [EditorMode], [Status],
                   [PreArchiveStatus], [VersionNumber], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [documents].[Page] WITH (NOLOCK)
            WHERE [OwnerId] = @OwnerId AND [DeletedAt] IS NULL
              AND (@Status IS NULL OR [Status] = @Status)
            ORDER BY [UpdatedAt] DESC, [Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Status", SqlDbType.VarChar, (object?)normalizedStatus ?? DBNull.Value, 16);
        using var reader = command.ExecuteReader();
        var items = new List<DocumentSummary>();
        while (reader.Read()) items.Add(ReadSummary(reader));
        return IdentityOperationResult<DocumentPage>.Success(new DocumentPage(items, null));
    }

    public IdentityOperationResult<DocumentDetail> Get(IdentityPrincipal actor, Guid documentId)
    {
        if (!ModuleAvailable(actor, "FX20", "documents.page.read")) return ModuleUnavailable<DocumentDetail>();
        using var connection = _connections.Create();
        connection.Open();
        var document = ReadDetail(connection, null, actor.OwnerId, documentId, forUpdate: false);
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
            $"title:{command.Title.Trim()}|type:{command.DocumentType}|mode:{command.EditorMode}|body:{command.Body ?? string.Empty}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "DocumentCreated");
            transaction.Commit();
            return IdentityOperationResult<DocumentDetail>.Success(created, 201, "DocumentCreated");
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
            $"document:{documentId:N}|etag:{ifMatch}|title:{command.Title.Trim()}|body:{command.Body ?? string.Empty}|note:{command.ChangeNote ?? string.Empty}", out var receipt);
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
            CompleteReceipt(connection, transaction, receipt, "DocumentSaved");
            transaction.Commit();
            return IdentityOperationResult<DocumentDetail>.Success(saved);
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
        if (!ModuleAvailable(actor, "FX20", DocumentTransitionAction(command.Status))) return ModuleUnavailable<DocumentDetail>();
        if (command.Status is not ("Draft" or "Published" or "Archived"))
            return Failure<DocumentDetail>("ValidationFailed", 422, "Document status must be Draft, Published or Archived.");
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt(connection, transaction, actor, "documents.page.transition", idempotencyKey,
            $"document:{documentId:N}|etag:{ifMatch}|status:{command.Status}", out var receipt);
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
            Execute(connection, transaction,
                "UPDATE [documents].[Page] SET [Status] = @Status, [PreArchiveStatus] = CASE WHEN @Status = 'Archived' THEN [Status] ELSE NULL END, [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @UserId WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [DeletedAt] IS NULL;",
                ("@Status", SqlDbType.VarChar, command.Status),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, documentId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, documentId, "documents.page.transition", traceId);
            var transitioned = ReadDetail(connection, transaction, actor.OwnerId, documentId, forUpdate: false);
            if (transitioned is null) { transaction.Rollback(); return Failure<DocumentDetail>("PersistenceFailure", 500, "Document could not be loaded after transition."); }
            CompleteReceipt(connection, transaction, receipt, "DocumentTransitioned");
            transaction.Commit();
            return IdentityOperationResult<DocumentDetail>.Success(transitioned);
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
        command.CommandText = $"SELECT [Id], [Title], [DocumentType], [EditorMode], [Body], [Status], [PreArchiveStatus], [VersionNumber], [CreatedAt], [UpdatedAt], [RowVersion] FROM [documents].[Page] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [DeletedAt] IS NULL;";
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
        "Archived" => next == (preArchiveStatus ?? "Draft"),
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

    private IdentityOperationResult<DocumentDetail>? CheckReceipt(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        var message = claim.IsConflict ? "The same Idempotency-Key was already used with a different request." : "The request was already completed or is in progress.";
        return Failure<DocumentDetail>(code, 409, message);
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId)
    {
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES ((SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId), @OwnerId, @Action, N'DocumentPage', @TargetId, 'Succeeded', @TraceId);",
            ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
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
