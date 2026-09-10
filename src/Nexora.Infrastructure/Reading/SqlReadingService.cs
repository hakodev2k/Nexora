using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Reading;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Reading;

/// <summary>
/// Local Read Later queue over owner-owned Bookmarks. The queue stores only a
/// safe title/URL snapshot and never copies source bodies or follows URLs.
/// News providers and cross-module read-state coordination remain deferred.
/// </summary>
public sealed class SqlReadingService : IReadingService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlReadingService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<ReadingPage> List(IdentityPrincipal actor, string? state = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX23", "reading.queue.read")) return ModuleUnavailable<ReadingPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedState = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        if (normalizedState is not null && normalizedState is not ("Unread" or "Reading" or "Read" or "Archived"))
            return Failure<ReadingPage>("ValidationFailed", 422, "Reading state must be Unread, Reading, Read or Archived.");
        var sourceReadable = ModuleAvailable(actor, "FX21", "bookmarks.bookmark.read");
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit)
                r.[Id], r.[SourceType], r.[SourceId], r.[State], r.[Progress], r.[SavedAt], r.[ReadAt],
                r.[SafeTitleSnapshot], r.[SafeUrlSnapshot],
                CONVERT(bit, CASE WHEN b.[Id] IS NOT NULL AND b.[Status] IN ('Active','Archived') THEN 1 ELSE 0 END),
                r.[UpdatedAt], r.[RowVersion]
            FROM [knowledge].[ReadingItem] r WITH (NOLOCK)
            LEFT JOIN [knowledge].[Bookmark] b WITH (NOLOCK)
              ON b.[OwnerId] = r.[OwnerId] AND b.[Id] = r.[SourceId] AND r.[SourceType] = 'Bookmark'
            WHERE r.[OwnerId] = @OwnerId AND (@State IS NULL OR r.[State] = @State)
            ORDER BY r.[SavedAt] DESC, r.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@State", SqlDbType.VarChar, (object?)normalizedState ?? DBNull.Value, 64);
        using var reader = command.ExecuteReader();
        var items = new List<ReadingItemRecord>();
        while (reader.Read()) items.Add(Read(reader, sourceReadable));
        return IdentityOperationResult<ReadingPage>.Success(new ReadingPage(items, null));
    }

    public IdentityOperationResult<ReadingItemRecord> Save(IdentityPrincipal actor, SaveReadingCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX23", "reading.item.save")) return ModuleUnavailable<ReadingItemRecord>();
        if (!string.Equals(command.SourceType?.Trim(), "Bookmark", StringComparison.Ordinal))
            return Failure<ReadingItemRecord>("ValidationFailed", 422, "This local Read Later slice accepts Bookmark sources only.");
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.read")) return SourceUnavailable<ReadingItemRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<ReadingItemRecord>(connection, transaction, actor, "reading.item.save", idempotencyKey,
            $"source:Bookmark|{command.SourceId:N}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var source = ReadBookmarkSource(connection, transaction, actor.OwnerId, command.SourceId);
            if (source is null) { transaction.Rollback(); return SourceUnavailable<ReadingItemRecord>(); }
            var id = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [knowledge].[ReadingItem] ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [SourceType], [SourceId], [State], [Progress], [SavedAt], [ReadAt], [SafeTitleSnapshot], [SafeUrlSnapshot]) VALUES (@Id, @OwnerId, @UserId, @UserId, 'Bookmark', @SourceId, 'Unread', 0, SYSUTCDATETIME(), NULL, @Title, @Url);",
                ("@Id", SqlDbType.UniqueIdentifier, id), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId), ("@SourceId", SqlDbType.UniqueIdentifier, command.SourceId),
                ("@Title", SqlDbType.NVarChar, source.Value.Title), ("@Url", SqlDbType.NVarChar, source.Value.Url));
            var created = Read(connection, transaction, actor.OwnerId, id, sourceReadable: true, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<ReadingItemRecord>("PersistenceFailure", 500, "Reading item could not be loaded after creation."); }
            WriteAudit(connection, transaction, actor, id, "reading.item.save", traceId);
            CompleteReceipt(connection, transaction, receipt, "ReadingItemSaved");
            transaction.Commit();
            return IdentityOperationResult<ReadingItemRecord>.Success(created, 201, "ReadingItemSaved");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            using var retryConnection = _connections.Create();
            retryConnection.Open();
            var existing = ReadBySource(retryConnection, actor.OwnerId, command.SourceId);
            return existing is null
                ? Failure<ReadingItemRecord>("ReadingItemDuplicate", 409, "The source is already in the reading queue.")
                : IdentityOperationResult<ReadingItemRecord>.Success(existing, 200, "ReadingItemAlreadySaved");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<ReadingItemRecord>(exception);
        }
    }

    public IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid itemId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX23", "reading.item.remove")) return ModuleUnavailable<object?>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<object?>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "reading.item.remove", idempotencyKey,
            $"item:{itemId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, itemId, sourceReadable: false, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<object?>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<object?>();
            }
            Execute(connection, transaction,
                "DELETE FROM [knowledge].[ReadingItem] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Id", SqlDbType.UniqueIdentifier, itemId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, itemId, "reading.item.remove", traceId);
            CompleteReceipt(connection, transaction, receipt, "ReadingItemRemoved");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("ReadingItemRemoved");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<ReadingItemRecord> Update(IdentityPrincipal actor, Guid itemId, string? ifMatch,
        UpdateReadingCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        var state = command.State?.Trim() ?? string.Empty;
        var action = state switch
        {
            "Read" => "reading.item.read",
            "Unread" => "reading.item.unread",
            "Reading" => "reading.item.position",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(action)) return Failure<ReadingItemRecord>("ValidationFailed", 422, "Reading state must be Unread, Reading or Read.");
        if (!ModuleAvailable(actor, "FX23", action)) return ModuleUnavailable<ReadingItemRecord>();
        if (command.Progress is < 0 or > 1) return Failure<ReadingItemRecord>("ValidationFailed", 422, "Reading progress must be between 0 and 1.");
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.read")) return SourceUnavailable<ReadingItemRecord>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<ReadingItemRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<ReadingItemRecord>(connection, transaction, actor, action, idempotencyKey,
            $"item:{itemId:N}|etag:{ifMatch}|state:{state}|progress:{command.Progress?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "keep"}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, itemId, sourceReadable: true, forUpdate: true);
            if (current is null || !current.SourceAvailable) { transaction.Rollback(); return SourceUnavailable<ReadingItemRecord>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<ReadingItemRecord>();
            }
            var progress = command.Progress ?? current.Progress;
            var readAtSql = state == "Read" ? "SYSUTCDATETIME()" : "NULL";
            Execute(connection, transaction,
                $"UPDATE [knowledge].[ReadingItem] SET [State] = @State, [Progress] = @Progress, [ReadAt] = {readAtSql}, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@State", SqlDbType.VarChar, state), ("@Progress", SqlDbType.Decimal, progress),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId), ("@Id", SqlDbType.UniqueIdentifier, itemId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@RowVersion", SqlDbType.Binary, expectedVersion));
            var updated = Read(connection, transaction, actor.OwnerId, itemId, sourceReadable: true, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<ReadingItemRecord>("PersistenceFailure", 500, "Reading item could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, itemId, action, traceId);
            CompleteReceipt(connection, transaction, receipt, state == "Read" ? "ReadingItemRead" : state == "Unread" ? "ReadingItemUnread" : "ReadingPositionSaved");
            transaction.Commit();
            return IdentityOperationResult<ReadingItemRecord>.Success(updated);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<ReadingItemRecord>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) => _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static (string Title, string Url)? ReadBookmarkSource(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid sourceId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Title], [Url] FROM [knowledge].[Bookmark] WITH (UPDLOCK, ROWLOCK) WHERE [OwnerId] = @OwnerId AND [Id] = @Id AND [Status] IN ('Active','Archived');";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, sourceId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetString(0), reader.GetString(1)) : null;
    }

    private static ReadingItemRecord? ReadBySource(SqlConnection connection, Guid ownerId, Guid sourceId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT r.[Id], r.[SourceType], r.[SourceId], r.[State], r.[Progress], r.[SavedAt], r.[ReadAt], r.[SafeTitleSnapshot], r.[SafeUrlSnapshot], CONVERT(bit, CASE WHEN b.[Id] IS NOT NULL AND b.[Status] IN ('Active','Archived') THEN 1 ELSE 0 END), r.[UpdatedAt], r.[RowVersion] FROM [knowledge].[ReadingItem] r WITH (NOLOCK) LEFT JOIN [knowledge].[Bookmark] b WITH (NOLOCK) ON b.[OwnerId] = r.[OwnerId] AND b.[Id] = r.[SourceId] WHERE r.[OwnerId] = @OwnerId AND r.[SourceType] = 'Bookmark' AND r.[SourceId] = @SourceId;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@SourceId", SqlDbType.UniqueIdentifier, sourceId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader, sourceReadable: true) : null;
    }

    private static ReadingItemRecord? Read(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid itemId, bool sourceReadable, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT r.[Id], r.[SourceType], r.[SourceId], r.[State], r.[Progress], r.[SavedAt], r.[ReadAt], r.[SafeTitleSnapshot], r.[SafeUrlSnapshot], CONVERT(bit, CASE WHEN b.[Id] IS NOT NULL AND b.[Status] IN ('Active','Archived') THEN 1 ELSE 0 END), r.[UpdatedAt], r.[RowVersion] FROM [knowledge].[ReadingItem] r WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) LEFT JOIN [knowledge].[Bookmark] b WITH (NOLOCK) ON b.[OwnerId] = r.[OwnerId] AND b.[Id] = r.[SourceId] WHERE r.[OwnerId] = @OwnerId AND r.[Id] = @Id;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, itemId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader, sourceReadable) : null;
    }

    private static ReadingItemRecord Read(SqlDataReader reader, bool sourceReadable) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3), reader.GetDecimal(4),
        ToOffset(reader.GetDateTime(5)), reader.IsDBNull(6) ? null : ToOffset(reader.GetDateTime(6)), reader.GetString(7),
        reader.GetString(8), sourceReadable && reader.GetBoolean(9), ToOffset(reader.GetDateTime(10)), EncodeETag(reader.GetFieldValue<byte[]>(11)));

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
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'knowledge.ReadingItem', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId),
        ("@Action", SqlDbType.NVarChar, actionKey), ("@Target", SqlDbType.UniqueIdentifier, targetId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

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
        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 28;
            parameter.Scale = 8;
        }
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Read Later is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> SourceUnavailable<T>() => Failure<T>("SourceUnavailable", 409, "The source is unavailable in the current owner/module scope.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Reading item unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Reading item revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Read Later persistence is unavailable.");
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
