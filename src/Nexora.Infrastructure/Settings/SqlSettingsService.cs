using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Settings;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Settings;

/// <summary>
/// SQL-backed, schema-validated user preferences. Only registered non-secret
/// payloads are accepted; this is deliberately not an arbitrary settings bag.
/// </summary>
public sealed class SqlSettingsService : ISettingsService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlSettingsService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<PreferencePage> ListPreferences(IdentityPrincipal actor)
    {
        if (!ModuleAvailable(actor, "FX09", "settings.preference.read")) return ModuleUnavailable<PreferencePage>();
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Id], [PreferenceKey], [SchemaVersion], [ValueJson], [CreatedAt], [UpdatedAt], [RowVersion] FROM [platform].[Preference] WHERE [OwnerId] = @OwnerId AND [ModuleId] IS NULL ORDER BY [PreferenceKey];";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        var items = new List<PreferenceRecord>();
        while (reader.Read()) items.Add(Read(reader));
        return IdentityOperationResult<PreferencePage>.Success(new PreferencePage(items, null));
    }

    public IdentityOperationResult<PreferenceRecord> UpdatePreference(IdentityPrincipal actor, PreferenceUpdateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX09", "settings.preference.update")) return ModuleUnavailable<PreferenceRecord>();
        var validation = Validate(command);
        if (validation is not null) return validation;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<PreferenceRecord>(connection, transaction, actor, "settings.preference.update", idempotencyKey,
            $"key:{command.PreferenceKey}|version:{command.SchemaVersion}|value:{command.ValueJson.Trim()}|etag:{command.IfMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadPreference(connection, transaction, actor.OwnerId, command.PreferenceKey, forUpdate: true);
            var now = DateTime.UtcNow;
            if (current is null)
            {
                if (!string.Equals(command.IfMatch?.Trim(), "*", StringComparison.Ordinal))
                {
                    transaction.Rollback();
                    return Failure<PreferenceRecord>("PreconditionRequired", 428, "If-Match: * is required to create a preference.");
                }
                Execute(connection, transaction,
                    "INSERT INTO [platform].[Preference] ([OwnerId], [CreatedByUserId], [UpdatedByUserId], [ModuleId], [PreferenceKey], [SchemaVersion], [ValueJson], [CreatedAt], [UpdatedAt]) VALUES (@OwnerId, @UserId, @UserId, NULL, @PreferenceKey, @SchemaVersion, @ValueJson, @Now, @Now);",
                    ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@UserId", SqlDbType.UniqueIdentifier, (object)actor.UserId),
                    ("@PreferenceKey", SqlDbType.NVarChar, (object)command.PreferenceKey), ("@SchemaVersion", SqlDbType.Int, (object)command.SchemaVersion),
                    ("@ValueJson", SqlDbType.NVarChar, (object)command.ValueJson.Trim()), ("@Now", SqlDbType.DateTime2, (object)now));
            }
            else
            {
                if (!TryETag(command.IfMatch, out var expectedVersion) || !expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
                {
                    transaction.Rollback();
                    return Revision<PreferenceRecord>();
                }
                Execute(connection, transaction,
                    "UPDATE [platform].[Preference] SET [UpdatedByUserId] = @UserId, [SchemaVersion] = @SchemaVersion, [ValueJson] = @ValueJson, [UpdatedAt] = @Now WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                    ("@UserId", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@SchemaVersion", SqlDbType.Int, (object)command.SchemaVersion),
                    ("@ValueJson", SqlDbType.NVarChar, (object)command.ValueJson.Trim()), ("@Now", SqlDbType.DateTime2, (object)now),
                    ("@Id", SqlDbType.UniqueIdentifier, (object)current.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                    ("@RowVersion", SqlDbType.Binary, (object)DecodeETag(current.ETag)));
            }
            var updated = ReadPreference(connection, transaction, actor.OwnerId, command.PreferenceKey, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<PreferenceRecord>("PersistenceFailure", 500, "Preference could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, updated.Id, traceId);
            CompleteReceipt(connection, transaction, receipt, "PreferenceUpdated");
            transaction.Commit();
            return IdentityOperationResult<PreferenceRecord>.Success(updated);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<PreferenceRecord>("PreferenceConflict", 409, "The preference already exists or changed concurrently.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<PreferenceRecord>(exception);
        }
    }

    private static IdentityOperationResult<PreferenceRecord>? Validate(PreferenceUpdateCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.PreferenceKey) || command.PreferenceKey.Length > 100 || command.SchemaVersion != 1 || string.IsNullOrWhiteSpace(command.ValueJson) || command.ValueJson.Length > 10000)
            return Failure<PreferenceRecord>("ValidationFailed", 422, "Preference key, schema version or payload is invalid.");
        try
        {
            using var document = JsonDocument.Parse(command.ValueJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Failure<PreferenceRecord>("ValidationFailed", 422, "Preference payload must be an object.");
            return command.PreferenceKey switch
            {
                "theme" => ValidateTheme(root),
                "locale" => ValidateLocale(root),
                "nav" => ValidateNav(root),
                "list" => ValidateList(root),
                _ => Failure<PreferenceRecord>("UnknownPreference", 422, "Preference key is not registered.")
            };
        }
        catch (JsonException) { return Failure<PreferenceRecord>("ValidationFailed", 422, "Preference payload must be valid JSON."); }
    }

    private static IdentityOperationResult<PreferenceRecord>? ValidateTheme(JsonElement root) =>
        HasExactly(root, "mode") && root.GetProperty("mode").ValueKind == JsonValueKind.String && root.GetProperty("mode").GetString() is "System" or "Light" or "Dark"
            ? null : Failure<PreferenceRecord>("ValidationFailed", 422, "Theme mode must be System, Light or Dark.");

    private static IdentityOperationResult<PreferenceRecord>? ValidateLocale(JsonElement root) =>
        HasExactly(root, "language", "timezone", "weekStart") && root.GetProperty("language").GetString() is "vi" or "en" && root.GetProperty("timezone").ValueKind == JsonValueKind.String && root.GetProperty("weekStart").GetString() is "Monday" or "Sunday"
            ? null : Failure<PreferenceRecord>("ValidationFailed", 422, "Locale preference is invalid.");

    private static IdentityOperationResult<PreferenceRecord>? ValidateNav(JsonElement root)
    {
        if (!HasExactly(root, "collapsed", "pinnedModuleCodes") || root.GetProperty("collapsed").ValueKind != JsonValueKind.True && root.GetProperty("collapsed").ValueKind != JsonValueKind.False || root.GetProperty("pinnedModuleCodes").ValueKind != JsonValueKind.Array)
            return Failure<PreferenceRecord>("ValidationFailed", 422, "Navigation preference is invalid.");
        var codes = root.GetProperty("pinnedModuleCodes");
        if (codes.GetArrayLength() > 20 || codes.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || item.GetString()!.Length > 64))
            return Failure<PreferenceRecord>("ValidationFailed", 422, "Pinned module list is invalid.");
        return null;
    }

    private static IdentityOperationResult<PreferenceRecord>? ValidateList(JsonElement root) =>
        HasExactly(root, "view", "sort", "density") && root.EnumerateObject().All(property => property.Value.ValueKind == JsonValueKind.String && property.Value.GetString()!.Length <= 64)
            ? null : Failure<PreferenceRecord>("ValidationFailed", 422, "List preference is invalid.");

    private static bool HasExactly(JsonElement root, params string[] properties) => root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(properties.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal);

    private static PreferenceRecord? ReadPreference(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, string key, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [PreferenceKey], [SchemaVersion], [ValueJson], [CreatedAt], [UpdatedAt], [RowVersion] FROM [platform].[Preference] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [OwnerId] = @OwnerId AND [ModuleId] IS NULL AND [PreferenceKey] = @Key;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Key", SqlDbType.NVarChar, key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }

    private static PreferenceRecord Read(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3), ToOffset(reader.GetDateTime(4)), ToOffset(reader.GetDateTime(5)), EncodeETag(reader.GetFieldValue<byte[]>(6)));

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) => _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, 'settings.preference.update', N'platform.Preference', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@Target", SqlDbType.UniqueIdentifier, (object)targetId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value)
    {
        var parameter = command.Parameters.Add(name, type);
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Settings are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Preference revision changed.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Preference persistence is unavailable.");
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('\"'));
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
}
