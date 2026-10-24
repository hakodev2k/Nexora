using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Application.Documents;
using Nexora.Infrastructure.Organization;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Documents;

/// <summary>
/// Dedicated Documents vocabulary and authoritative current page usage.
/// Direct requests require Documents authority; generic catalog delegation
/// additionally requires the exact FX24 wrapper in the same transaction.
/// </summary>
public sealed class SqlDocumentTagService : IDocumentTagService
{
    private static readonly HashSet<string> LocalNamespaces = new(StringComparer.Ordinal)
    {
        "documents"
    };

    private readonly bool _organizationWrapper;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly TagListCursor _cursor;

    public SqlDocumentTagService(SqlConnectionFactory connections, string? idempotencySecret = null)
        : this(connections, idempotencySecret, false) { }

    internal SqlDocumentTagService(SqlConnectionFactory connections, string? idempotencySecret, bool organizationWrapper)
    {
        _organizationWrapper = organizationWrapper;
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _cursor = new TagListCursor(secret + "/documents-catalog/v1");
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<TagPage> List(IdentityPrincipal actor, string? tagNamespace = null,
        string? query = null, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "documents.tag.read"))
            return ModuleUnavailable<TagPage>();

        var normalizedNamespace = NormalizeNamespace(tagNamespace, allowEmpty: true) ?? "documents";
        if (normalizedNamespace is not null && !LocalNamespaces.Contains(normalizedNamespace))
            return Failure<TagPage>("ValidationFailed", 422, "Tag namespace is not supported by the local slice.");

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (normalizedQuery is { Length: > 50 })
            return Failure<TagPage>("ValidationFailed", 422, "Tag search must be at most 50 characters.");

        var take = Math.Clamp(limit ?? 25, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, "documents.tag.read")) return ModuleUnavailable<TagPage>();
        var documentsAllowed = NamespaceAllowed(connection, transaction, actor, "documents", "documents.tag.read");
        if (normalizedNamespace == "documents" && !documentsAllowed) return ModuleUnavailable<TagPage>();
        string? cursorName = null;
        Guid? cursorId = null;
        if (cursor is not null && !_cursor.TryDecode(cursor, actor.OwnerId, normalizedNamespace, normalizedQuery, take, documentsAllowed,
                out cursorName, out cursorId))
            return Failure<TagPage>("ValidationFailed", 422, "Tag list cursor is invalid for these filters.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit)
                t.[Id], t.[Namespace], t.[Name], t.[Color],
                CONVERT(int, (SELECT COUNT_BIG(1)
                              FROM [documents].[Page] rt
                              WHERE rt.[OwnerId] = t.[OwnerId] AND rt.[TagId] = t.[Id])),
                t.[CreatedAt], t.[UpdatedAt], t.[RowVersion]
            FROM [documents].[Tag] t
            WHERE t.[OwnerId] = @OwnerId
              AND t.[Namespace] IN ('projects', 'documents', 'bookmarks', 'snippets')
              AND (t.[Namespace] <> 'documents' OR @DocumentsAllowed = 1)
              AND (@Namespace IS NULL OR t.[Namespace] = @Namespace)
              AND (@Query IS NULL OR t.[Name] LIKE @QueryLike ESCAPE N'\')
              AND (@CursorId IS NULL OR t.[Name] > @CursorName
                   OR (t.[Name] = @CursorName AND t.[Id] > @CursorId))
            ORDER BY t.[Name], t.[Id];
            """;
        Add(command, "@DocumentsAllowed", SqlDbType.Bit, documentsAllowed);
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Namespace", SqlDbType.VarChar, (object?)normalizedNamespace ?? DBNull.Value, 64);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 50);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{EscapeLike(normalizedQuery)}%", 102);
        Add(command, "@CursorName", SqlDbType.NVarChar, (object?)cursorName ?? DBNull.Value, 50);
        Add(command, "@CursorId", SqlDbType.UniqueIdentifier, (object?)cursorId ?? DBNull.Value);

        var items = new List<TagRecord>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) items.Add(Read(reader));
        var hasMore = items.Count > take;
        if (hasMore) items.RemoveAt(take);
        var nextCursor = hasMore ? _cursor.Encode(actor.OwnerId, normalizedNamespace, normalizedQuery, take, documentsAllowed,
            items[^1].Name, items[^1].Id) : null;
        if (!CurrentAuthority(connection, transaction, actor, "documents.tag.read") ||
            documentsAllowed != NamespaceAllowed(connection, transaction, actor, "documents", "documents.tag.read"))
            return ModuleUnavailable<TagPage>();
        transaction.Commit();
        return IdentityOperationResult<TagPage>.Success(new TagPage(items, nextCursor));
    }

    public IdentityOperationResult<TagRecord> Get(IdentityPrincipal actor, Guid tagId)
    {
        if (!ModuleAvailable(actor, "documents.tag.read")) return ModuleUnavailable<TagRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CurrentAuthority(connection, transaction, actor, "documents.tag.read")) return ModuleUnavailable<TagRecord>();
        var tag = Read(connection, transaction, actor.OwnerId, tagId, forUpdate: false);
        if (tag is null) return Missing<TagRecord>();
        if (!NamespaceAllowed(connection, transaction, actor, tag.Namespace, "documents.tag.read") ||
            !CurrentAuthority(connection, transaction, actor, "documents.tag.read")) return ModuleUnavailable<TagRecord>();
        transaction.Commit();
        return IdentityOperationResult<TagRecord>.Success(tag);
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);

    public IdentityOperationResult<TagRecord> Create(IdentityPrincipal actor, TagCreateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "documents.tag.create"))
            return ModuleUnavailable<TagRecord>();

        var validation = ValidateCreate(command, out var normalizedNamespace, out var name, out var normalizedName, out var color);
        if (validation is not null)
            return validation;

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<TagRecord>(connection, transaction, actor, "documents.tag.create", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, actor.OwnerId, Namespace = normalizedNamespace, Name = name, Color = color }),
            null, normalizedNamespace, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }

        try
        {
            var id = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [documents].[Tag]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Name], [NormalizedName], [Color])
                VALUES
                    (@Id, @OwnerId, @UserId, @UserId, @Name, @NormalizedName, @Color);
                """,
                ("@Id", SqlDbType.UniqueIdentifier, id),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Namespace", SqlDbType.VarChar, normalizedNamespace),
                ("@Name", SqlDbType.NVarChar, name),
                ("@NormalizedName", SqlDbType.NVarChar, normalizedName),
                ("@Color", SqlDbType.VarChar, (object?)color ?? DBNull.Value));

            var created = Read(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null)
            {
                transaction.Rollback();
                return Failure<TagRecord>("PersistenceFailure", 500, "Tag could not be loaded after creation.");
            }

            WriteAudit(connection, transaction, actor, id, "documents.tag.create", traceId);
            return CommitTag(connection, transaction, actor, receipt, created, "documents.tag.create", 201, "TagCreated");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<TagRecord>("TagDuplicate", 409, "A tag with this name already exists in the namespace.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<TagRecord>(exception);
        }
    }

    public IdentityOperationResult<TagRecord> Rename(IdentityPrincipal actor, Guid tagId, string? ifMatch,
        TagRenameCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "documents.tag.rename"))
            return ModuleUnavailable<TagRecord>();

        var validation = ValidateNameAndColor(command.Name, command.Color, out var name, out var normalizedName, out var color);
        if (validation is not null)
            return validation;
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<TagRecord>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<TagRecord>(connection, transaction, actor, "documents.tag.rename", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, actor.OwnerId, TagId = tagId, ETag = EncodeETag(expectedVersion), Name = name, Color = color }),
            tagId, null, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }

        try
        {
            var current = Read(connection, transaction, actor.OwnerId, tagId, forUpdate: true);
            if (current is null)
            {
                transaction.Rollback();
                return Missing<TagRecord>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<TagRecord>();
            }

            Execute(connection, transaction, """
                UPDATE [documents].[Tag]
                SET [Name] = @Name, [NormalizedName] = @NormalizedName, [Color] = @Color,
                    [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """,
                ("@Name", SqlDbType.NVarChar, name),
                ("@NormalizedName", SqlDbType.NVarChar, normalizedName),
                ("@Color", SqlDbType.VarChar, (object?)color ?? DBNull.Value),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, tagId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));

            var updated = Read(connection, transaction, actor.OwnerId, tagId, forUpdate: false);
            if (updated is null)
            {
                transaction.Rollback();
                return Failure<TagRecord>("PersistenceFailure", 500, "Tag could not be loaded after rename.");
            }

            WriteAudit(connection, transaction, actor, tagId, "documents.tag.rename", traceId);
            return CommitTag(connection, transaction, actor, receipt, updated, "documents.tag.rename", 200, "Ok");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<TagRecord>("TagDuplicate", 409, "A tag with this name already exists in the namespace.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<TagRecord>(exception);
        }
    }

    public IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid tagId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "documents.tag.remove"))
            return ModuleUnavailable<object?>();
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<object?>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "documents.tag.remove", idempotencyKey,
            JsonSerializer.Serialize(new { Version = 2, actor.OwnerId, TagId = tagId, ETag = EncodeETag(expectedVersion) }),
            tagId, null, out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }

        try
        {
            var current = Read(connection, transaction, actor.OwnerId, tagId, forUpdate: true);
            if (current is null)
            {
                transaction.Rollback();
                return Missing<object?>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<object?>();
            }
            if (current.UsageCount > 0)
            {
                transaction.Rollback();
                return Failure<object?>("TagInUse", 409, "Tag is still assigned to resources and cannot be deleted.");
            }

            Execute(connection, transaction,
                "DELETE FROM [documents].[Tag] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Id", SqlDbType.UniqueIdentifier, tagId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, tagId, "documents.tag.remove", traceId);
            if (!CurrentAuthority(connection, transaction, actor, "documents.tag.remove") ||
                !NamespaceAllowed(connection, transaction, actor, current.Namespace, "documents.tag.remove"))
                return ModuleUnavailable<object?>();
            CompleteReceipt(connection, transaction, receipt, "TagRemoved", 204,
                JsonSerializer.Serialize(new TagReceipt(2, current.Id, current.Namespace, "documents.tag.remove", current.ETag)));
            if (!CurrentAuthority(connection, transaction, actor, "documents.tag.remove") ||
                !NamespaceAllowed(connection, transaction, actor, current.Namespace, "documents.tag.remove"))
                return ModuleUnavailable<object?>();
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("TagRemoved");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, "FX20", actionKeys) &&
        (!_organizationWrapper || _capabilities.IsAllowed(actor, "FX24", actionKeys.Select(WrapperAction).ToArray()));

    private static string WrapperAction(string action) => action switch
    {
        "documents.tag.read" => "organization.tag.read",
        "documents.tag.create" => "organization.tag.create",
        "documents.tag.rename" => "organization.tag.rename",
        "documents.tag.remove" => "organization.tag.remove",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
    private string ReceiptOperation(string action) => _organizationWrapper ? WrapperAction(action) : action;

    private static IdentityOperationResult<TagRecord>? ValidateCreate(TagCreateCommand command,
        out string normalizedNamespace, out string name, out string normalizedName, out string? color)
    {
        normalizedNamespace = NormalizeNamespace(command.Namespace, allowEmpty: false) ?? string.Empty;
        name = string.Empty;
        normalizedName = string.Empty;
        color = null;
        if (!LocalNamespaces.Contains(normalizedNamespace))
            return Failure<TagRecord>("ValidationFailed", 422, "This catalog accepts only the documents namespace.");

        return ValidateNameAndColor(command.Name, command.Color, out name, out normalizedName, out color);
    }

    private static IdentityOperationResult<TagRecord>? ValidateNameAndColor(string? rawName, string? rawColor,
        out string name, out string normalizedName, out string? color)
    {
        name = rawName?.Trim() ?? string.Empty;
        normalizedName = name.ToUpperInvariant();
        color = string.IsNullOrWhiteSpace(rawColor) ? null : rawColor.Trim();
        if (name.Length is < 1 or > 50)
            return Failure<TagRecord>("ValidationFailed", 422, "Tag name is required and must be 1 to 50 characters.");
        if (color is not null && !IsHexColor(color))
            return Failure<TagRecord>("ValidationFailed", 422, "Tag color must be a six-digit hexadecimal value such as #2F67D8.");
        return null;
    }

    private static string? NormalizeNamespace(string? value, bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(value))
            return allowEmpty ? null : string.Empty;
        return value.Trim().ToLowerInvariant();
    }

    private static bool IsHexColor(string value) =>
        value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);

    private static TagRecord? Read(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid tagId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT t.[Id], t.[Namespace], t.[Name], t.[Color],
                   CONVERT(int, (SELECT COUNT_BIG(1)
                                 FROM [documents].[Page] rt WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")})
                                 WHERE rt.[OwnerId] = t.[OwnerId] AND rt.[TagId] = t.[Id])),
                   t.[CreatedAt], t.[UpdatedAt], t.[RowVersion]
            FROM [documents].[Tag] t WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")})
            WHERE t.[OwnerId] = @OwnerId AND t.[Id] = @Id;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, tagId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    private static TagRecord Read(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetInt32(4),
        ToOffset(reader.GetDateTime(5)), ToOffset(reader.GetDateTime(6)),
        EncodeETag(reader.GetFieldValue<byte[]>(7)));

    private bool CurrentAuthority(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string action) =>
        SqlCurrentActor.IsLive(connection, transaction, actor) &&
        _capabilities.IsAllowed(connection, transaction, actor, "FX20", action) &&
        (!_organizationWrapper || _capabilities.IsAllowed(connection, transaction, actor, "FX24", WrapperAction(action)));

    private bool NamespaceAllowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string tagNamespace, string operation)
    {
        if (!LocalNamespaces.Contains(tagNamespace)) return false;
        if (tagNamespace != "documents") return true;
        var action = operation switch
        {
            "documents.tag.read" => "documents.tag.read",
            "documents.tag.create" => "documents.tag.create",
            "documents.tag.rename" => "documents.tag.rename",
            "documents.tag.remove" => "documents.tag.remove",
            _ => null
        };
        return action is not null && _capabilities.IsAllowed(connection, transaction, actor, "FX20", action);
    }

    private static string? ReadNamespace(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Namespace] FROM [documents].[Tag] WITH(UPDLOCK,HOLDLOCK) WHERE [OwnerId]=@Owner AND [Id]=@Id;";
        Add(command, "@Owner", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        return command.ExecuteScalar() as string;
    }

    private IdentityOperationResult<TagRecord> CommitTag(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, ReceiptClaim claim, TagRecord tag, string action, int status, string code)
    {
        var canRead = CurrentAuthority(connection, transaction, actor, "documents.tag.read") &&
            NamespaceAllowed(connection, transaction, actor, tag.Namespace, "documents.tag.read");
        if (!CurrentAuthority(connection, transaction, actor, action) ||
            !NamespaceAllowed(connection, transaction, actor, tag.Namespace, action)) return ModuleUnavailable<TagRecord>();
        CompleteReceipt(connection, transaction, claim, code, status,
            JsonSerializer.Serialize(new TagReceipt(2, tag.Id, tag.Namespace, action, tag.ETag)));
        if (!CurrentAuthority(connection, transaction, actor, action) ||
            !NamespaceAllowed(connection, transaction, actor, tag.Namespace, action)) return ModuleUnavailable<TagRecord>();
        transaction.Commit();
        return canRead ? IdentityOperationResult<TagRecord>.Success(tag, status, code)
            : IdentityOperationResult<TagRecord>.Committed(tag.Id, status, "TagCommitted");
    }

    private sealed record TagReceipt(int Version, Guid Id, string Namespace, string Action, string ETag);

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        Guid? targetId, string? requestedNamespace, out ReceiptClaim claim)
    {
        claim = default;
        if (!CurrentAuthority(connection, transaction, actor, operationKey)) return ModuleUnavailable<T>();
        SqlDocumentScopeLock.Acquire(connection, transaction, actor.OwnerId);
        var currentNamespace = targetId is { } id ? ReadNamespace(connection, transaction, actor.OwnerId, id) : requestedNamespace;
        if (currentNamespace is not null && !NamespaceAllowed(connection, transaction, actor, currentNamespace, operationKey))
            return ModuleUnavailable<T>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, ReceiptOperation(operationKey), idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay)
        {
            TagReceipt? stored = null;
            try { if (claim.ResultJson is not null) stored = JsonSerializer.Deserialize<TagReceipt>(claim.ResultJson); }
            catch (JsonException) { }
            var expectedStatus = operationKey == "documents.tag.create" ? 201 : operationKey == "documents.tag.remove" ? 204 : 200;
            var expectedCode = operationKey == "documents.tag.create" ? "TagCreated" : operationKey == "documents.tag.remove" ? "TagRemoved" : "Ok";
            if (stored is null || stored.Version != 2 || stored.Id == Guid.Empty || (stored.Action != operationKey && (!_organizationWrapper || stored.Action != WrapperAction(operationKey))) ||
                stored.Namespace is null || !LocalNamespaces.Contains(stored.Namespace) || !TryETag(stored.ETag, out _) ||
                (targetId is { } expectedId && stored.Id != expectedId) ||
                (requestedNamespace is not null && stored.Namespace != requestedNamespace) ||
                (currentNamespace is not null && stored.Namespace != currentNamespace) ||
                claim.ResultStatusCode != expectedStatus || claim.ResultCode != expectedCode)
                return Failure<T>("IdempotencyReplay", 409, "The completed request cannot be safely replayed.");
            if (!NamespaceAllowed(connection, transaction, actor, stored.Namespace, operationKey) ||
                !CurrentAuthority(connection, transaction, actor, operationKey)) return ModuleUnavailable<T>();
            if (expectedStatus == 204) return IdentityOperationResult<T>.NoContent("TagRemoved");
            var canRead = CurrentAuthority(connection, transaction, actor, "documents.tag.read") &&
                NamespaceAllowed(connection, transaction, actor, stored.Namespace, "documents.tag.read");
            var current = canRead ? Read(connection, transaction, actor.OwnerId, stored.Id, forUpdate: true) : null;
            canRead = current is not null && current.Namespace == stored.Namespace && current.ETag == stored.ETag &&
                CurrentAuthority(connection, transaction, actor, "documents.tag.read") &&
                NamespaceAllowed(connection, transaction, actor, stored.Namespace, "documents.tag.read");
            if (!CurrentAuthority(connection, transaction, actor, operationKey) ||
                !NamespaceAllowed(connection, transaction, actor, stored.Namespace, operationKey)) return ModuleUnavailable<T>();
            return canRead ? IdentityOperationResult<T>.Success((T)(object)current!, expectedStatus, expectedCode)
                : IdentityOperationResult<T>.Committed(stored.Id, expectedStatus, "TagCommitted");
        }
        return Failure<T>(claim.IsConflict ? "IdempotencyConflict" : "RequestInProgress", 409,
            "The request key has a different payload or older receipt format, or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode,
        int? statusCode = null, string? resultJson = null) =>
        _receipts.Complete(connection, transaction, claim, resultCode, statusCode, resultJson);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, N'documents.Tag', @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId),
        ("@ActionKey", SqlDbType.NVarChar, actionKey),
        ("@TargetId", SqlDbType.UniqueIdentifier, targetId),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
            Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is null ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size.Value);
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);

    private static IdentityOperationResult<T> ModuleUnavailable<T>() =>
        Failure<T>("ModuleUnavailable", 409, "Tags are disabled or unavailable for this user.");

    private static IdentityOperationResult<T> Missing<T>() =>
        Failure<T>("ResourceUnavailable", 404, "Tag unavailable.");

    private static IdentityOperationResult<T> Precondition<T>() =>
        Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");

    private static IdentityOperationResult<T> Revision<T>() =>
        Failure<T>("RevisionConflict", 412, "Tag revision changed.");

    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) =>
        Failure<T>("PersistenceUnavailable", 503, "Tag persistence is unavailable.");

    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";

    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));

    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() == "*")
                return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
