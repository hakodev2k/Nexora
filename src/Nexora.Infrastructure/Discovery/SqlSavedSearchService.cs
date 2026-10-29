using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Discovery;

public sealed class SqlSavedSearchService : ISavedSearchService
{
    private const string Prefix = "discovery.saved_search.";
    private const string Columns = "[Id],[Title],[SchemaVersion],[QueryJson],[UpdatedAt],[RowVersion]";
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSearchService _search;

    public SqlSavedSearchService(SqlConnectionFactory connections, string secret)
    {
        _connections = connections; _capabilities = new(connections); _receipts = new(secret); _search = new(connections, secret);
    }
    private bool Allowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX25", actions.Select(action => Prefix + action).ToArray());

    public IdentityOperationResult<SavedSearchPage> List(IdentityPrincipal actor, string? cursor = null)
    {
        Cursor? after = null;
        if (!string.IsNullOrEmpty(cursor) && !DecodeCursor(actor.OwnerId, cursor, out after)) return Fail<SavedSearchPage>("InvalidCursor", 422);
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "read")) return Denied<SavedSearchPage>();
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"SELECT TOP(26) {Columns} FROM [discovery].[SavedQuery] WHERE [OwnerId]=@Owner AND (@After IS NULL OR [UpdatedAt]<@After OR ([UpdatedAt]=@After AND [Id]<@Id)) ORDER BY [UpdatedAt] DESC,[Id] DESC;";
            Add(command, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(command, "@After", SqlDbType.DateTime2, after is null ? DBNull.Value : after.UpdatedAt);
            Add(command, "@Id", SqlDbType.UniqueIdentifier, after?.Id ?? Guid.Empty);
            var items = new List<SavedSearchRecord>(); using (var reader = command.ExecuteReader()) while (reader.Read()) items.Add(Read(reader));
            var more = items.Count > 25; if (more) items.RemoveAt(25);
            var next = more ? EncodeCursor(actor.OwnerId, new(items[^1].UpdatedAt.UtcDateTime, items[^1].Id)) : null;
            if (!Allowed(connection, transaction, actor, "read")) return Denied<SavedSearchPage>();
            transaction.Commit(); return IdentityOperationResult<SavedSearchPage>.Success(new(items, next));
        }
        catch (SqlException) { return Fail<SavedSearchPage>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<SavedSearchRecord> Save(IdentityPrincipal actor, Guid? id, string? ifMatch, SavedSearchCommand command,
        string? key = null, string? traceId = null)
    {
        if (command.SchemaVersion != 1) return Fail<SavedSearchRecord>("MigrationRequired", 409);
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 100 || !ValidDefinition(command.Query) || id == Guid.Empty)
            return Fail<SavedSearchRecord>("ValidationFailed", 422);
        byte[] expected = [];
        if (id.HasValue && !ETag(ifMatch, out expected)) return Fail<SavedSearchRecord>("PreconditionRequired", 428);
        var action = id.HasValue ? "update" : "create";
        string[] actions = id.HasValue ? [action, "read"] : [action];
        var query = command.Query with { Text = command.Query.Text.Trim() };
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, actions)) return Denied<SavedSearchRecord>();
            var replay = Claim<SavedSearchRecord>(connection, transaction, actor, action, actions, key,
                JsonSerializer.Serialize(new { version = 1, id, etag = id.HasValue ? Convert.ToBase64String(expected) : null,
                    title = command.Title.Trim(), command.SchemaVersion, query }), id, out var receipt);
            if (replay is not null) return replay;
            var resourceId = id ?? Guid.NewGuid();
            if (id.HasValue)
            {
                var current = Read(connection, transaction, actor.OwnerId, resourceId, true);
                if (current is null) return Fail<SavedSearchRecord>("ResourceUnavailable", 404);
                if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(current.ETag[1..^1]))) return Fail<SavedSearchRecord>("RevisionConflict", 412);
            }
            using (var sql = connection.CreateCommand())
            {
                sql.Transaction = transaction;
                sql.CommandText = id.HasValue
                    ? "UPDATE [discovery].[SavedQuery] SET [Title]=@Title,[SchemaVersion]=1,[QueryJson]=@Query,[UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User WHERE [OwnerId]=@Owner AND [Id]=@Id AND [RowVersion]=@ETag;"
                    : "INSERT INTO [discovery].[SavedQuery]([Id],[OwnerId],[Title],[SchemaVersion],[QueryJson],[CreatedByUserId],[UpdatedByUserId]) VALUES(@Id,@Owner,@Title,1,@Query,@User,@User);";
                Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
                Add(sql, "@Id", SqlDbType.UniqueIdentifier, resourceId); Add(sql, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 100);
                Add(sql, "@Query", SqlDbType.NVarChar, JsonSerializer.Serialize(query), -1); Add(sql, "@ETag", SqlDbType.Binary, expected, 8);
                if (sql.ExecuteNonQuery() != 1) return Fail<SavedSearchRecord>("RevisionConflict", 412);
            }
            Audit(connection, transaction, actor, resourceId, action, traceId);
            var status = id.HasValue ? 200 : 201;
            _receipts.Complete(connection, transaction, receipt, "SavedSearchCommitted", status,
                JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, resourceId, action, actions)));
            if (!Allowed(connection, transaction, actor, actions)) return Denied<SavedSearchRecord>();
            var detail = Allowed(connection, transaction, actor, "read") ? Read(connection, transaction, actor.OwnerId, resourceId, false) : null;
            var readable = detail is not null && Allowed(connection, transaction, actor, "read");
            if (!Allowed(connection, transaction, actor, actions)) return Denied<SavedSearchRecord>();
            transaction.Commit(); return readable ? IdentityOperationResult<SavedSearchRecord>.Success(detail!, status, "SavedSearchCommitted")
                : IdentityOperationResult<SavedSearchRecord>.Committed(resourceId, status, "SavedSearchCommitted");
        }
        catch (SqlException) { return Fail<SavedSearchRecord>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<object?> Delete(IdentityPrincipal actor, Guid id, string? ifMatch, string? key = null, string? traceId = null)
    {
        if (!ETag(ifMatch, out var expected)) return Fail<object?>("PreconditionRequired", 428);
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "delete")) return Denied<object?>();
            var replay = Claim<object?>(connection, transaction, actor, "delete", ["delete"], key,
                JsonSerializer.Serialize(new { version = 1, id, etag = Convert.ToBase64String(expected) }), id, out var receipt);
            if (replay is not null) return replay;
            var current = Read(connection, transaction, actor.OwnerId, id, true);
            if (current is null) return Fail<object?>("ResourceUnavailable", 404);
            if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(current.ETag[1..^1])))
                return Fail<object?>("RevisionConflict", 412);
            using var sql = connection.CreateCommand(); sql.Transaction = transaction;
            sql.CommandText = "DELETE FROM [discovery].[SavedQuery] WHERE [OwnerId]=@Owner AND [Id]=@Id AND [RowVersion]=@ETag;";
            Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@ETag", SqlDbType.Binary, expected, 8);
            if (sql.ExecuteNonQuery() != 1) return Fail<object?>("RevisionConflict", 412);
            Audit(connection, transaction, actor, id, "delete", traceId);
            _receipts.Complete(connection, transaction, receipt, "SavedSearchCommitted", 204,
                JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, id, "delete", ["delete"])));
            if (!Allowed(connection, transaction, actor, "delete")) return Denied<object?>();
            transaction.Commit(); return IdentityOperationResult<object?>.Success(null, 204, "SavedSearchCommitted");
        }
        catch (SqlException) { return Fail<object?>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<SearchPage> Run(IdentityPrincipal actor, Guid id, string? cursor = null)
    {
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "run")) return Denied<SearchPage>();
            var saved = Read(connection, transaction, actor.OwnerId, id, false);
            if (saved is null) return Fail<SearchPage>("ResourceUnavailable", 404);
            if (saved.SchemaVersion != 1 || saved.Query is null || !ValidDefinition(saved.Query)) return Fail<SearchPage>("MigrationRequired", 409);
            var query = saved.Query;
            var result = _search.SearchWithin(connection, transaction, actor, query.Text.Trim(), query.ResourceType, query.From, query.To, query.IncludeArchived, 25, cursor);
            if (!result.Succeeded) return result;
            if (!Allowed(connection, transaction, actor, "run")) return Denied<SearchPage>();
            transaction.Commit(); return result;
        }
        catch (SqlException) { return Fail<SearchPage>("SearchUnavailable", 503); }
    }

    private IdentityOperationResult<T>? Claim<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string action, string[] actions, string? key, string canonical, Guid? id, out ReceiptClaim claim)
    {
        claim = default; if (string.IsNullOrWhiteSpace(key)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, Prefix + action, key, canonical, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Fail<T>("ValidationFailed", 422);
        if (claim.IsConflict) return Fail<T>("IdempotencyConflict", 409);
        if (!claim.IsReplay) return Fail<T>("RequestInProgress", 409);
        Receipt? receipt = null; try { receipt = JsonSerializer.Deserialize<Receipt>(claim.ResultJson ?? string.Empty); } catch (JsonException) { }
        var status = action == "delete" ? 204 : action == "create" ? 201 : 200;
        if (receipt is null || receipt.Version != 1 || receipt.UserId != actor.UserId || receipt.OwnerId != actor.OwnerId || receipt.Id == Guid.Empty
            || (id.HasValue && id.Value != receipt.Id) || receipt.Action != action || receipt.Actions is null || !receipt.Actions.SequenceEqual(actions)
            || claim.ResultCode != "SavedSearchCommitted" || claim.ResultStatusCode != status) return Fail<T>("ReceiptUnavailable", 503);
        if (!Allowed(connection, transaction, actor, actions)) return Denied<T>();
        return status == 204 ? IdentityOperationResult<T>.Success(default!, 204, "SavedSearchCommitted")
            : IdentityOperationResult<T>.Committed(receipt.Id, status, "SavedSearchCommitted");
    }

    private static SavedSearchRecord? Read(SqlConnection connection, SqlTransaction transaction, Guid owner, Guid id, bool update)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = $"SELECT {Columns} FROM [discovery].[SavedQuery] {(update ? "WITH(UPDLOCK,ROWLOCK)" : string.Empty)} WHERE [OwnerId]=@Owner AND [Id]=@Id;";
        Add(sql, "@Owner", SqlDbType.UniqueIdentifier, owner); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id);
        using var reader = sql.ExecuteReader(); return reader.Read() ? Read(reader) : null;
    }
    private static SavedSearchRecord Read(SqlDataReader reader)
    {
        var version = reader.GetInt32(2); SavedSearchDefinition? query = null;
        if (version == 1) try { query = JsonSerializer.Deserialize<SavedSearchDefinition>(reader.GetString(3)); } catch (JsonException) { }
        var valid = version == 1 && ValidDefinition(query);
        return new(reader.GetGuid(0), reader.GetString(1), version, valid ? query : null,
            new(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)), $"\"{Convert.ToBase64String(reader.GetFieldValue<byte[]>(5))}\"", valid ? "Compatible" : "MigrationRequired");
    }
    private static bool ValidDefinition(SavedSearchDefinition? value) => value is not null && value.Text is not null && value.Text.Length <= 500
        && value.Sort == "relevance" && value.ResourceType is null or "Project" or "Task" or "Event" or "Document" or "Bookmark" or "Snippet" or "Goal"
        && !(value.From.HasValue && value.To.HasValue && value.To < value.From);
    private string EncodeCursor(Guid owner, Cursor cursor)
    {
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cursor)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return payload + "." + _receipts.DigestSensitive($"saved-query-cursor:v1:{owner:N}:{payload}");
    }
    private bool DecodeCursor(Guid owner, string token, out Cursor? cursor)
    {
        cursor = null; if (token.Length > 1024) return false; var parts = token.Split('.'); if (parts.Length != 2) return false;
        try
        {
            var expected = Convert.FromHexString(_receipts.DigestSensitive($"saved-query-cursor:v1:{owner:N}:{parts[0]}"));
            if (!CryptographicOperations.FixedTimeEquals(expected, Convert.FromHexString(parts[1]))) return false;
            var encoded = parts[0].Replace('-', '+').Replace('_', '/'); encoded += new string('=', (4 - encoded.Length % 4) % 4);
            cursor = JsonSerializer.Deserialize<Cursor>(Convert.FromBase64String(encoded));
            return cursor is { Id: var id, UpdatedAt.Kind: DateTimeKind.Utc } && id != Guid.Empty;
        }
        catch (Exception error) when (error is FormatException or JsonException) { return false; }
    }
    private static bool ETag(string? text, out byte[] value)
    {
        value = []; if (string.IsNullOrWhiteSpace(text)) return false; var token = text.Trim();
        if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
        try { value = Convert.FromBase64String(token[1..^1]); return value.Length == 8 && Convert.ToBase64String(value) == token[1..^1]; } catch (FormatException) { return false; }
    }
    private static void Audit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid id, string action, string? trace)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = "INSERT INTO [security].[AuditEvent]([ActorUserId],[OwnerUserId],[ActionKey],[TargetType],[TargetId],[Result],[TraceId]) VALUES(@Actor,@Actor,@Action,'SavedQuery',@Id,'Succeeded',@Trace);";
        Add(sql, "@Actor", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Action", SqlDbType.NVarChar, Prefix + action, 160);
        Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Trace", SqlDbType.NVarChar, (object?)trace ?? DBNull.Value, 200); sql.ExecuteNonQuery();
    }
    private static void Add(SqlCommand sql, string name, SqlDbType type, object value, int? size = null) =>
        (size.HasValue ? sql.Parameters.Add(name, type, size.Value) : sql.Parameters.Add(name, type)).Value = value;
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status,
        code == "MigrationRequired" ? "This saved query needs an explicit compatible definition; no filters were broadened." : "Saved search unavailable or invalid.");
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 409);
    private sealed record Receipt(int Version, Guid UserId, Guid OwnerId, Guid Id, string Action, string[] Actions);
    private sealed record Cursor(DateTime UpdatedAt, Guid Id);
}
