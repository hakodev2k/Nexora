using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Organization;

/// <summary>
/// SQL-backed owner tag catalog for the first FX24 slice. A tag is a
/// namespace-scoped label; it does not grant access, move resources or imply
/// that a source provider supports assignment. ResourceTag is only consulted
/// for usage counts and delete protection until a later assignment slice owns
/// those writes.
/// </summary>
public sealed class SqlTagService : ITagService
{
    private static readonly HashSet<string> LocalNamespaces = new(StringComparer.Ordinal)
    {
        "projects",
        "documents",
        "bookmarks",
        "snippets"
    };

    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlTagService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<TagPage> List(IdentityPrincipal actor, string? tagNamespace = null,
        string? query = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "organization.tag.read"))
            return ModuleUnavailable<TagPage>();

        var normalizedNamespace = NormalizeNamespace(tagNamespace, allowEmpty: true);
        if (normalizedNamespace is not null && !LocalNamespaces.Contains(normalizedNamespace))
            return Failure<TagPage>("ValidationFailed", 422, "Tag namespace is not supported by the local slice.");

        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        if (normalizedQuery is { Length: > 50 })
            normalizedQuery = normalizedQuery[..50];

        var take = Math.Clamp(limit ?? 50, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit)
                t.[Id], t.[Namespace], t.[Name], t.[Color],
                CONVERT(int, (SELECT COUNT_BIG(1)
                              FROM [organization].[ResourceTag] rt WITH (NOLOCK)
                              WHERE rt.[OwnerId] = t.[OwnerId] AND rt.[TagId] = t.[Id])),
                t.[CreatedAt], t.[UpdatedAt], t.[RowVersion]
            FROM [organization].[Tag] t WITH (NOLOCK)
            WHERE t.[OwnerId] = @OwnerId
              AND (@Namespace IS NULL OR t.[Namespace] = @Namespace)
              AND (@Query IS NULL OR t.[Name] LIKE @QueryLike)
            ORDER BY t.[Name], t.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Namespace", SqlDbType.VarChar, (object?)normalizedNamespace ?? DBNull.Value, 64);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 50);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 52);

        using var reader = command.ExecuteReader();
        var items = new List<TagRecord>();
        while (reader.Read())
            items.Add(Read(reader));
        return IdentityOperationResult<TagPage>.Success(new TagPage(items, null));
    }

    public IdentityOperationResult<TagRecord> Create(IdentityPrincipal actor, TagCreateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "organization.tag.create"))
            return ModuleUnavailable<TagRecord>();

        var validation = ValidateCreate(command, out var normalizedNamespace, out var name, out var normalizedName, out var color);
        if (validation is not null)
            return validation;

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<TagRecord>(connection, transaction, actor, "organization.tag.create", idempotencyKey,
            $"namespace:{normalizedNamespace}|name:{normalizedName}|color:{color ?? string.Empty}", out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }

        try
        {
            var id = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [organization].[Tag]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Namespace], [Name], [NormalizedName], [Color])
                VALUES
                    (@Id, @OwnerId, @UserId, @UserId, @Namespace, @Name, @NormalizedName, @Color);
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

            WriteAudit(connection, transaction, actor, id, "organization.tag.create", traceId);
            CompleteReceipt(connection, transaction, receipt, "TagCreated");
            transaction.Commit();
            return IdentityOperationResult<TagRecord>.Success(created, 201, "TagCreated");
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
        if (!ModuleAvailable(actor, "organization.tag.rename"))
            return ModuleUnavailable<TagRecord>();

        var validation = ValidateNameAndColor(command.Name, command.Color, out var name, out var normalizedName, out var color);
        if (validation is not null)
            return validation;
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<TagRecord>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<TagRecord>(connection, transaction, actor, "organization.tag.rename", idempotencyKey,
            $"tag:{tagId:N}|etag:{ifMatch}|name:{normalizedName}|color:{color ?? string.Empty}", out var receipt);
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
                UPDATE [organization].[Tag]
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

            WriteAudit(connection, transaction, actor, tagId, "organization.tag.rename", traceId);
            CompleteReceipt(connection, transaction, receipt, "TagRenamed");
            transaction.Commit();
            return IdentityOperationResult<TagRecord>.Success(updated);
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
        if (!ModuleAvailable(actor, "organization.tag.remove"))
            return ModuleUnavailable<object?>();
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<object?>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "organization.tag.remove", idempotencyKey,
            $"tag:{tagId:N}|etag:{ifMatch}", out var receipt);
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
                "DELETE FROM [organization].[Tag] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Id", SqlDbType.UniqueIdentifier, tagId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            WriteAudit(connection, transaction, actor, tagId, "organization.tag.remove", traceId);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
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
        _capabilities.IsAllowed(actor, "FX24", actionKeys);

    private static IdentityOperationResult<TagRecord>? ValidateCreate(TagCreateCommand command,
        out string normalizedNamespace, out string name, out string normalizedName, out string? color)
    {
        normalizedNamespace = NormalizeNamespace(command.Namespace, allowEmpty: false) ?? string.Empty;
        name = string.Empty;
        normalizedName = string.Empty;
        color = null;
        if (!LocalNamespaces.Contains(normalizedNamespace))
            return Failure<TagRecord>("ValidationFailed", 422, "Tag namespace must be projects, documents, bookmarks or snippets.");

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
                                 FROM [organization].[ResourceTag] rt WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")})
                                 WHERE rt.[OwnerId] = t.[OwnerId] AND rt.[TagId] = t.[Id])),
                   t.[CreatedAt], t.[UpdatedAt], t.[RowVersion]
            FROM [organization].[Tag] t WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")})
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

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed)
            return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, N'organization.Tag', @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.OwnerId),
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
