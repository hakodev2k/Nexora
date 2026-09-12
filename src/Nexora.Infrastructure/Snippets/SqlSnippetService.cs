using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Snippets;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Snippets;

/// <summary>
/// SQL-backed text-only snippets. Source is always rendered as data: this
/// service never compiles, evaluates, sends or publishes snippet contents.
/// Versions are append-only and the current row is protected by rowversion.
/// </summary>
public sealed class SqlSnippetService : ISnippetService
{
    private const int MaxSourceBytes = 1024 * 1024;
    private static readonly Regex LanguagePattern = new("^[A-Za-z0-9][A-Za-z0-9+.#_-]{0,49}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlSnippetService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<SnippetPage> List(IdentityPrincipal actor, bool includeArchived = false,
        string? query = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX22", "snippets.snippet.read")) return ModuleUnavailable<SnippetPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit)
                s.[Id], s.[Title], s.[Language], v.[SourceText], v.[Description],
                s.[CurrentVersion], s.[Status], s.[CreatedAt], s.[UpdatedAt], s.[RowVersion]
            FROM [knowledge].[Snippet] s WITH (NOLOCK)
            INNER JOIN [knowledge].[SnippetVersion] v WITH (NOLOCK)
                ON v.[OwnerId] = s.[OwnerId] AND v.[SnippetId] = s.[Id] AND v.[VersionNumber] = s.[CurrentVersion]
            WHERE s.[OwnerId] = @OwnerId
              AND (@IncludeArchived = 1 OR s.[Status] = 'Active')
              AND (@Query IS NULL OR s.[Title] LIKE @QueryLike OR s.[Language] LIKE @QueryLike
                   OR v.[SourceText] LIKE @QueryLike OR v.[Description] LIKE @QueryLike)
            ORDER BY s.[UpdatedAt] DESC, s.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@IncludeArchived", SqlDbType.Bit, includeArchived);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 200);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 202);
        using var reader = command.ExecuteReader();
        var items = new List<SnippetRecord>();
        while (reader.Read()) items.Add(Read(reader));
        return IdentityOperationResult<SnippetPage>.Success(new SnippetPage(items, null));
    }

    public IdentityOperationResult<SnippetRecord> Create(IdentityPrincipal actor, SnippetCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX22", "snippets.snippet.create")) return ModuleUnavailable<SnippetRecord>();
        var validation = Validate(command, out var title, out var language, out var body, out var description);
        if (validation is not null) return validation;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<SnippetRecord>(connection, transaction, actor, "snippets.snippet.create", idempotencyKey,
            CanonicalRequest(title!, language!, body!, description), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var id = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [knowledge].[Snippet] ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Title], [Language], [CurrentVersion], [Status]) VALUES (@Id, @OwnerId, @UserId, @UserId, @Title, @Language, 1, 'Active');",
                ("@Id", SqlDbType.UniqueIdentifier, id), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId), ("@Title", SqlDbType.NVarChar, title!),
                ("@Language", SqlDbType.NVarChar, language!));
            Execute(connection, transaction,
                "INSERT INTO [knowledge].[SnippetVersion] ([Id], [OwnerId], [SnippetId], [VersionNumber], [Title], [Language], [SourceText], [Description], [SourceVersion], [CommandKeyHash], [CreatedByUserId]) VALUES (@VersionId, @OwnerId, @SnippetId, 1, @Title, @Language, @SourceText, @Description, NULL, @CommandKeyHash, @UserId);",
                ("@VersionId", SqlDbType.UniqueIdentifier, Guid.NewGuid()), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@SnippetId", SqlDbType.UniqueIdentifier, id), ("@Title", SqlDbType.NVarChar, title!),
                ("@Language", SqlDbType.NVarChar, language!), ("@SourceText", SqlDbType.NVarChar, body!),
                ("@Description", SqlDbType.NVarChar, (object?)description ?? DBNull.Value), ("@CommandKeyHash", SqlDbType.Binary, Digest(CanonicalRequest(title!, language!, body!, description))),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId));
            var created = Read(connection, transaction, actor.OwnerId, id, forUpdate: false);
            if (created is null) { transaction.Rollback(); return Failure<SnippetRecord>("PersistenceFailure", 500, "Snippet could not be loaded after creation."); }
            WriteAudit(connection, transaction, actor, id, "snippets.snippet.create", traceId);
            CompleteReceipt(connection, transaction, receipt, "SnippetCreated");
            transaction.Commit();
            return IdentityOperationResult<SnippetRecord>.Success(created, 201, "SnippetCreated");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<SnippetRecord>(exception);
        }
    }

    public IdentityOperationResult<SnippetRecord> Save(IdentityPrincipal actor, Guid snippetId, string? ifMatch,
        SnippetCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX22", "snippets.snippet.save")) return ModuleUnavailable<SnippetRecord>();
        var validation = Validate(command, out var title, out var language, out var body, out var description);
        if (validation is not null) return validation;
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<SnippetRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<SnippetRecord>(connection, transaction, actor, "snippets.snippet.save", idempotencyKey,
            $"snippet:{snippetId:N}|etag:{ifMatch}|{CanonicalRequest(title!, language!, body!, description)}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, snippetId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<SnippetRecord>(); }
            if (current.Status != "Active") { transaction.Rollback(); return Failure<SnippetRecord>("LifecycleLocked", 409, "Archived snippets are read-only until unarchived."); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<SnippetRecord>();
            }
            var nextVersion = checked(current.VersionNumber + 1);
            Execute(connection, transaction,
                "UPDATE [knowledge].[Snippet] SET [Title] = @Title, [Language] = @Language, [CurrentVersion] = @CurrentVersion, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Title", SqlDbType.NVarChar, title!), ("@Language", SqlDbType.NVarChar, language!),
                ("@CurrentVersion", SqlDbType.BigInt, nextVersion), ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, snippetId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            Execute(connection, transaction,
                "INSERT INTO [knowledge].[SnippetVersion] ([Id], [OwnerId], [SnippetId], [VersionNumber], [Title], [Language], [SourceText], [Description], [SourceVersion], [CommandKeyHash], [CreatedByUserId]) VALUES (@VersionId, @OwnerId, @SnippetId, @VersionNumber, @Title, @Language, @SourceText, @Description, NULL, @CommandKeyHash, @UserId);",
                ("@VersionId", SqlDbType.UniqueIdentifier, Guid.NewGuid()), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@SnippetId", SqlDbType.UniqueIdentifier, snippetId), ("@VersionNumber", SqlDbType.BigInt, nextVersion),
                ("@Title", SqlDbType.NVarChar, title!), ("@Language", SqlDbType.NVarChar, language!),
                ("@SourceText", SqlDbType.NVarChar, body!), ("@Description", SqlDbType.NVarChar, (object?)description ?? DBNull.Value),
                ("@CommandKeyHash", SqlDbType.Binary, Digest(CanonicalRequest(title!, language!, body!, description))),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId));
            var saved = Read(connection, transaction, actor.OwnerId, snippetId, forUpdate: false);
            if (saved is null) { transaction.Rollback(); return Failure<SnippetRecord>("PersistenceFailure", 500, "Snippet could not be loaded after save."); }
            WriteAudit(connection, transaction, actor, snippetId, "snippets.snippet.save", traceId);
            CompleteReceipt(connection, transaction, receipt, "SnippetSaved");
            transaction.Commit();
            return IdentityOperationResult<SnippetRecord>.Success(saved);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<SnippetRecord>(exception);
        }
    }

    public IdentityOperationResult<SnippetRecord> Transition(IdentityPrincipal actor, Guid snippetId, string? ifMatch,
        string status, string? idempotencyKey = null, string? traceId = null)
    {
        var normalizedStatus = status?.Trim() ?? string.Empty;
        var action = normalizedStatus switch
        {
            "Archived" => "snippets.snippet.archive",
            "Active" => "snippets.snippet.unarchive",
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(action)) return Failure<SnippetRecord>("ValidationFailed", 422, "Snippet status must be Active or Archived.");
        if (!ModuleAvailable(actor, "FX22", action)) return ModuleUnavailable<SnippetRecord>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<SnippetRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<SnippetRecord>(connection, transaction, actor, action, idempotencyKey,
            $"snippet:{snippetId:N}|etag:{ifMatch}|status:{normalizedStatus}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = Read(connection, transaction, actor.OwnerId, snippetId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<SnippetRecord>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<SnippetRecord>();
            }
            if (current.Status == normalizedStatus)
            {
                transaction.Rollback();
                return Failure<SnippetRecord>("LifecycleLocked", 409, "Snippet is already in the requested state.");
            }
            Execute(connection, transaction,
                "UPDATE [knowledge].[Snippet] SET [Status] = @Status, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@Status", SqlDbType.VarChar, normalizedStatus), ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId),
                ("@Id", SqlDbType.UniqueIdentifier, snippetId), ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId),
                ("@RowVersion", SqlDbType.Binary, expectedVersion));
            var updated = Read(connection, transaction, actor.OwnerId, snippetId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<SnippetRecord>("PersistenceFailure", 500, "Snippet could not be loaded after transition."); }
            WriteAudit(connection, transaction, actor, snippetId, action, traceId);
            CompleteReceipt(connection, transaction, receipt, normalizedStatus == "Archived" ? "SnippetArchived" : "SnippetUnarchived");
            transaction.Commit();
            return IdentityOperationResult<SnippetRecord>.Success(updated);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<SnippetRecord>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static IdentityOperationResult<SnippetRecord>? Validate(SnippetCommand command, out string? title,
        out string? language, out string? body, out string? description)
    {
        title = command.Title?.Trim();
        language = command.Language?.Trim();
        body = command.Body;
        description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            return Failure<SnippetRecord>("ValidationFailed", 422, "Snippet title is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(language) || language.Length > 50 || !LanguagePattern.IsMatch(language))
            return Failure<SnippetRecord>("ValidationFailed", 422, "Snippet language must be a registered text label up to 50 characters.");
        if (string.IsNullOrEmpty(body) || Encoding.UTF8.GetByteCount(body) > MaxSourceBytes)
            return Failure<SnippetRecord>("ValidationFailed", 422, "Snippet source is required and must be at most 1 MiB in UTF-8.");
        if (description is { Length: > 20000 })
            return Failure<SnippetRecord>("ValidationFailed", 422, "Snippet description must be at most 20000 characters.");
        return null;
    }

    private static string CanonicalRequest(string title, string language, string body, string? description) =>
        $"title:{title}|language:{language}|body:{Convert.ToHexString(Digest(body))}|description:{Convert.ToHexString(Digest(description ?? string.Empty))}";

    private static byte[] Digest(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static SnippetRecord Read(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
        reader.GetInt64(5), reader.GetString(6), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag(reader.GetFieldValue<byte[]>(9)));

    private static SnippetRecord? Read(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid snippetId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT s.[Id], s.[Title], s.[Language], v.[SourceText], v.[Description], s.[CurrentVersion], s.[Status], s.[CreatedAt], s.[UpdatedAt], s.[RowVersion] FROM [knowledge].[Snippet] s WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) INNER JOIN [knowledge].[SnippetVersion] v WITH (NOLOCK) ON v.[OwnerId] = s.[OwnerId] AND v.[SnippetId] = s.[Id] AND v.[VersionNumber] = s.[CurrentVersion] WHERE s.[Id] = @Id AND s.[OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, snippetId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
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

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'knowledge.Snippet', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId),
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
        if (size is null && type == SqlDbType.NVarChar && value is string text && text.Length > 4000)
            parameter.Size = -1;
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Code Snippets is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Snippet unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Snippet revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Snippet persistence is unavailable.");
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
