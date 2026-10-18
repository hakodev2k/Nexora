using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Bookmarks;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Bookmarks;

/// <summary>
/// SQL-backed manual bookmark metadata. URLs remain inert metadata: this
/// service never follows, fetches or embeds them. Provider refresh and
/// external navigation are separate gated capabilities and are intentionally
/// unavailable in the local slice.
/// </summary>
public sealed partial class SqlBookmarkService : IBookmarkService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly BookmarkListCursor _cursor;

    public SqlBookmarkService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _cursor = new BookmarkListCursor(secret);
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
            return Failure<IReadOnlyDictionary<string, bool>>("PermissionDenied", 403, "Bookmark capabilities are unavailable.");
        string[] actions = ["bookmarks.bookmark.read", "bookmarks.bookmark.create", "bookmarks.bookmark.update", "bookmarks.bookmark.archive", "bookmarks.bookmark.unarchive"];
        var result = actions.ToDictionary(action => action,
            action => _capabilities.IsAllowed(connection, transaction, actor, "FX21", RequiredActions(action)), StringComparer.Ordinal);
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
            return Failure<IReadOnlyDictionary<string, bool>>("PermissionDenied", 403, "Bookmark capabilities are unavailable.");
        transaction.Commit();
        return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result);
    }

    public IdentityOperationResult<BookmarkPage> List(IdentityPrincipal actor, bool includeArchived = false,
        string? query = null, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.read")) return ModuleUnavailable<BookmarkPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (normalizedQuery is { Length: > 200 })
            return Failure<BookmarkPage>("ValidationFailed", 422, "Bookmark search must be at most 200 characters.");
        DateTime? afterTime = null; Guid? afterId = null;
        if (!string.IsNullOrEmpty(cursor) && !_cursor.TryDecode(cursor, actor.OwnerId, includeArchived, normalizedQuery, take, out afterTime, out afterId))
            return Failure<BookmarkPage>("InvalidCursor", 422, "Bookmark cursor does not match this query.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read"))
            return ModuleUnavailable<BookmarkPage>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Url], [CanonicalUrl], [Title], [Description], [Health], [LastCheckedAt], [Status], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [knowledge].[Bookmark] b
            WHERE [OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = b.[OwnerId] AND tr.[ResourceType] = 'Bookmark' AND tr.[ResourceId] = b.[Id]
                    AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
              AND [Status] IN ('Active', 'Archived')
              AND (@IncludeArchived = 1 OR [Status] = 'Active')
              AND (@AfterTime IS NULL OR [UpdatedAt] < @AfterTime OR ([UpdatedAt] = @AfterTime AND [Id] < @AfterId))
              AND (@Query IS NULL OR [Title] LIKE @QueryLike ESCAPE N'~' OR [Url] LIKE @QueryLike ESCAPE N'~' OR [Description] LIKE @QueryLike ESCAPE N'~')
            ORDER BY [UpdatedAt] DESC, [Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@AfterTime", SqlDbType.DateTime2, (object?)afterTime ?? DBNull.Value);
        Add(command, "@AfterId", SqlDbType.UniqueIdentifier, (object?)afterId ?? DBNull.Value);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@IncludeArchived", SqlDbType.Bit, includeArchived);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{EscapeLike(normalizedQuery)}%", 402);
        var items = new List<BookmarkRecord>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) items.Add(Read(reader));
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read"))
            return ModuleUnavailable<BookmarkPage>();
        transaction.Commit();
        string? nextCursor = null;
        if (items.Count > take)
        {
            items.RemoveAt(take);
            var last = items[^1];
            nextCursor = _cursor.Encode(actor.OwnerId, includeArchived, normalizedQuery, take, last.UpdatedAt, last.Id);
        }
        return IdentityOperationResult<BookmarkPage>.Success(new BookmarkPage(items, nextCursor));
    }

    public IdentityOperationResult<BookmarkRecord> Get(IdentityPrincipal actor, Guid bookmarkId)
    {
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.read")) return ModuleUnavailable<BookmarkRecord>();
        if (bookmarkId == Guid.Empty) return Failure<BookmarkRecord>("ResourceUnavailable", 404, "Bookmark unavailable.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read"))
            return ModuleUnavailable<BookmarkRecord>();
        var bookmark = Read(connection, transaction, actor.OwnerId, bookmarkId, forUpdate: false);
        if (bookmark is not null && bookmark.Status is "Active" or "Archived")
            Nexora.Infrastructure.Discovery.SqlRecentActivity.RecordWithin(connection, transaction, _capabilities, actor,
                ReadRecentSourceWithin(connection, transaction, actor, bookmarkId));
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read"))
            return ModuleUnavailable<BookmarkRecord>();
        transaction.Commit();
        return bookmark is null || bookmark.Status is not ("Active" or "Archived")
            ? Failure<BookmarkRecord>("ResourceUnavailable", 404, "Bookmark unavailable.")
            : IdentityOperationResult<BookmarkRecord>.Success(bookmark);
    }

    public IdentityOperationResult<BookmarkRecord> Create(IdentityPrincipal actor, BookmarkCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.create")) return ModuleUnavailable<BookmarkRecord>();
        var validation = Validate(command, out var url, out var canonicalUrl);
        if (validation is not null) return validation;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<BookmarkRecord>(connection, transaction, actor, "bookmarks.bookmark.create", idempotencyKey,
            CanonicalRequest(url!, command.Title, command.Description), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var id = Guid.NewGuid();
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO [knowledge].[Bookmark]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Url], [CanonicalUrl], [UrlDigest], [Title], [Description], [MetadataJson], [Health], [Status])
                VALUES
                    (@Id, @OwnerId, @UserId, @UserId, @Url, @CanonicalUrl, @UrlDigest, @Title, @Description, N'{}', 'Unknown', 'Active');
                """;
            Add(insert, "@Id", SqlDbType.UniqueIdentifier, id);
            Add(insert, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(insert, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(insert, "@Url", SqlDbType.NVarChar, url!, 2048);
            Add(insert, "@CanonicalUrl", SqlDbType.NVarChar, canonicalUrl!, 2048);
            Add(insert, "@UrlDigest", SqlDbType.Binary, Digest(canonicalUrl!), 32);
            Add(insert, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
            Add(insert, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description) ?? DBNull.Value, -1);
            insert.ExecuteNonQuery();
            var created = Read(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<BookmarkRecord>("PersistenceFailure", 500, "Bookmark could not be loaded after creation."); }
            WriteAudit(connection, transaction, actor, id, "bookmarks.bookmark.create", traceId);
            return CommitResult(connection, transaction, actor, "bookmarks.bookmark.create", created, 201, "BookmarkCreated", receipt);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<BookmarkRecord>("BookmarkDuplicate", 409, "A bookmark with this URL already exists in the current owner scope.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<BookmarkRecord>(exception);
        }
    }

    public IdentityOperationResult<BookmarkRecord> Update(IdentityPrincipal actor, Guid bookmarkId, string? ifMatch,
        BookmarkCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX21", "bookmarks.bookmark.update")) return ModuleUnavailable<BookmarkRecord>();
        var validation = Validate(command, out var url, out var canonicalUrl);
        if (validation is not null) return validation;
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<BookmarkRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<BookmarkRecord>(connection, transaction, actor, "bookmarks.bookmark.update", idempotencyKey,
            $"bookmark:{bookmarkId:N}|etag:{ifMatch}|{CanonicalRequest(url!, command.Title, command.Description)}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, bookmarkId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<BookmarkRecord>(); }
            if (current.Status != "Active")
            { transaction.Rollback(); return Failure<BookmarkRecord>("LifecycleLocked", 409, "Archived bookmarks are read-only until unarchived."); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<BookmarkRecord>();
            }
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE [knowledge].[Bookmark]
                SET [Url] = @Url, [CanonicalUrl] = @CanonicalUrl, [UrlDigest] = @UrlDigest,
                    [Title] = @Title, [Description] = @Description, [UpdatedByUserId] = @UserId,
                    [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """;
            Add(update, "@Url", SqlDbType.NVarChar, url!, 2048);
            Add(update, "@CanonicalUrl", SqlDbType.NVarChar, canonicalUrl!, 2048);
            Add(update, "@UrlDigest", SqlDbType.Binary, Digest(canonicalUrl!), 32);
            Add(update, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
            Add(update, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description) ?? DBNull.Value, -1);
            Add(update, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(update, "@Id", SqlDbType.UniqueIdentifier, bookmarkId);
            Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
            if (update.ExecuteNonQuery() != 1) { transaction.Rollback(); return Revision<BookmarkRecord>(); }
            var updated = Read(connection, transaction, actor.OwnerId, bookmarkId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<BookmarkRecord>("PersistenceFailure", 500, "Bookmark could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, bookmarkId, "bookmarks.bookmark.update", traceId);
            return CommitResult(connection, transaction, actor, "bookmarks.bookmark.update", updated, 200, "BookmarkUpdated", receipt);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<BookmarkRecord>("BookmarkDuplicate", 409, "A bookmark with this URL already exists in the current owner scope.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<BookmarkRecord>(exception);
        }
    }

    public IdentityOperationResult<BookmarkRecord> Transition(IdentityPrincipal actor, Guid bookmarkId, string? ifMatch,
        string status, string? idempotencyKey = null, string? traceId = null)
    {
        var normalizedStatus = status?.Trim() ?? string.Empty;
        var action = normalizedStatus switch
        {
            "Archived" => "bookmarks.bookmark.archive",
            "Active" => "bookmarks.bookmark.unarchive",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(action)) return Failure<BookmarkRecord>("ValidationFailed", 422, "Bookmark status must be Active or Archived.");
        if (!ModuleAvailable(actor, "FX21", action)) return ModuleUnavailable<BookmarkRecord>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<BookmarkRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<BookmarkRecord>(connection, transaction, actor, action, idempotencyKey,
            $"bookmark:{bookmarkId:N}|etag:{ifMatch}|status:{normalizedStatus}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, bookmarkId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<BookmarkRecord>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<BookmarkRecord>();
            }
            if (current.Status is not ("Active" or "Archived") || string.Equals(current.Status, normalizedStatus, StringComparison.Ordinal))
            {
                transaction.Rollback();
                return Failure<BookmarkRecord>("LifecycleLocked", 409, "Bookmark is already in the requested state.");
            }
            Execute(connection, transaction,
                "UPDATE [knowledge].[Bookmark] SET [Status] = @Status, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Status", SqlDbType.VarChar, normalizedStatus), ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, bookmarkId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            var updated = Read(connection, transaction, actor.OwnerId, bookmarkId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<BookmarkRecord>("PersistenceFailure", 500, "Bookmark could not be loaded after transition."); }
            WriteAudit(connection, transaction, actor, bookmarkId, action, traceId);
            return CommitResult(connection, transaction, actor, action, updated, 200,
                normalizedStatus == "Archived" ? "BookmarkArchived" : "BookmarkUnarchived", receipt);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<BookmarkRecord>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys.SelectMany(RequiredActions).Distinct(StringComparer.Ordinal).ToArray());

    private static string[] RequiredActions(string action) => action == "bookmarks.bookmark.update"
        ? [action, "bookmarks.bookmark.read"] : [action];

    private static IdentityOperationResult<BookmarkRecord>? Validate(BookmarkCommand command, out string? url, out string? canonicalUrl)
    {
        url = command.Url?.Trim();
        canonicalUrl = null;
        if (string.IsNullOrWhiteSpace(url) || url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || parsed.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(parsed.Host))
            return Failure<BookmarkRecord>("ValidationFailed", 422, "Bookmark URL must be an absolute HTTP(S) URL up to 2048 characters.");
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length is < 1 or > 200)
            return Failure<BookmarkRecord>("ValidationFailed", 422, "Bookmark title is required and must be at most 200 characters.");
        if (command.Description is { Length: > 20000 })
            return Failure<BookmarkRecord>("ValidationFailed", 422, "Bookmark description must be at most 20000 characters.");
        try
        {
            var builder = new UriBuilder(parsed)
            {
                Scheme = parsed.Scheme.ToLowerInvariant(),
                Host = parsed.Host.ToLowerInvariant()
            };
            if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) || (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
                builder.Port = -1;
            canonicalUrl = builder.Uri.GetComponents(UriComponents.HttpRequestUrl | UriComponents.Fragment, UriFormat.UriEscaped);
        }
        catch (UriFormatException)
        {
            return Failure<BookmarkRecord>("ValidationFailed", 422, "Bookmark URL is not valid.");
        }
        return null;
    }

    private static string CanonicalRequest(string url, string title, string? description) =>
        $"url:{Convert.ToHexString(Digest(url))}|title:{title.Trim()}|description:{Convert.ToHexString(Digest(description?.Trim() ?? string.Empty))}";

    private static string EscapeLike(string value) => value.Replace("~", "~~", StringComparison.Ordinal)
        .Replace("%", "~%", StringComparison.Ordinal).Replace("_", "~_", StringComparison.Ordinal)
        .Replace("[", "~[", StringComparison.Ordinal);

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BookmarkRecord Read(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetString(5), reader.IsDBNull(6) ? null : ToOffset(reader.GetDateTime(6)), reader.GetString(7),
        ToOffset(reader.GetDateTime(8)), ToOffset(reader.GetDateTime(9)), EncodeETag(reader.GetFieldValue<byte[]>(10)));

    private static BookmarkRecord? Read(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid bookmarkId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [Url], [CanonicalUrl], [Title], [Description], [Health], [LastCheckedAt], [Status], [CreatedAt], [UpdatedAt], [RowVersion] FROM [knowledge].[Bookmark] b {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)} WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId] = b.[OwnerId] AND tr.[ResourceType] = 'Bookmark' AND tr.[ResourceId] = b.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, bookmarkId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", RequiredActions(operationKey)))
            return ModuleUnavailable<T>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid)
            return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay && claim.ResultStatusCode == 204)
            return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent");
        if (claim.IsReplay && !string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            try
            {
                var status = claim.ResultStatusCode ?? 200;
                var replayCode = claim.ResultCode ?? "IdempotencyReplay";
                using var json = JsonDocument.Parse(claim.ResultJson);
                if (json.RootElement.TryGetProperty("DetailAvailable", out _))
                {
                    var result = JsonSerializer.Deserialize<BookmarkReceipt>(claim.ResultJson);
                    if (result is not null && result.Id != Guid.Empty && TryETag(result.ETag, out _))
                    {
                        var canRead = result.DetailAvailable && _capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read");
                        var current = canRead ? Read(connection, transaction, actor.OwnerId, result.Id, false) : null;
                        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", RequiredActions(operationKey))) return ModuleUnavailable<T>();
                        return current is { Status: "Active" or "Archived" } && current.ETag == result.ETag
                            && _capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read")
                            ? IdentityOperationResult<T>.Success((T)(object)current, status, replayCode)
                            : IdentityOperationResult<T>.Committed(result.Id, status, replayCode);
                    }
                }
                else
                {
                    // Historical DTO receipts did not record their original response projection.
                    // A current read grant cannot prove that the original response was detailed.
                    var historical = JsonSerializer.Deserialize<BookmarkRecord>(claim.ResultJson);
                    if (historical is not null && historical.Id != Guid.Empty)
                        return IdentityOperationResult<T>.Committed(historical.Id, status, replayCode);
                }
            }
            catch (JsonException) { }
            catch (NotSupportedException) { }
        }
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private sealed record BookmarkReceipt(Guid Id, string ETag, bool DetailAvailable);

    private IdentityOperationResult<BookmarkRecord> CommitResult(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string action, BookmarkRecord value, int status, string code, ReceiptClaim receipt)
    {
        var canRead = _capabilities.IsAllowed(connection, transaction, actor, "FX21", "bookmarks.bookmark.read");
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX21", RequiredActions(action)))
        { transaction.Rollback(); return ModuleUnavailable<BookmarkRecord>(); }
        _receipts.Complete(connection, transaction, receipt, code, status,
            JsonSerializer.Serialize(new BookmarkReceipt(value.Id, value.ETag, canRead)));
        transaction.Commit();
        return canRead ? IdentityOperationResult<BookmarkRecord>.Success(value, status, code)
            : IdentityOperationResult<BookmarkRecord>.Committed(value.Id, status, code);
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'knowledge.Bookmark', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, actor.UserId),
        ("@Action", SqlDbType.NVarChar, actionKey), ("@Target", SqlDbType.UniqueIdentifier, targetId),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

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
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Bookmarks is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Bookmark unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Bookmark revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Bookmark persistence is unavailable.");
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "*") return false;
            var normalized = value.Trim();
            if (normalized.Length < 2 || normalized[0] != '"' || normalized[^1] != '"') return false;
            bytes = Convert.FromBase64String(normalized[1..^1]);
            return bytes.Length == 8 && EncodeETag(bytes) == normalized;
        }
        catch (FormatException) { return false; }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
