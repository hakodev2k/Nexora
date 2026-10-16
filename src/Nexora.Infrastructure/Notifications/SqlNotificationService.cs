using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Notifications;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Owner-scoped notification inbox backed by SQL. The service owns only safe
/// notification projections; delivery state is visible per channel but no
/// provider is called here. Email and push delivery remain local-safe pending
/// work for a separately approved worker/provider boundary.
/// </summary>
public sealed class SqlNotificationService : INotificationService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlNotificationService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<NotificationPage> List(IdentityPrincipal actor, bool unreadOnly = false, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX06", "notifications.inbox.read")) return ModuleUnavailable<NotificationPage>();
        var take = Math.Clamp(limit ?? 50, 1, 200);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read")) return ModuleUnavailable<NotificationPage>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit)
                n.[Id], n.[Kind], n.[Title], n.[Body], n.[SourceRef], n.[CreatedAt],
                n.[ReadAt], n.[RowVersion]
            FROM [notifications].[Notification] n
            WHERE n.[OwnerUserId] = (SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId)
              AND n.[DeletedAt] IS NULL
              AND (@UnreadOnly = 0 OR n.[ReadAt] IS NULL)
            ORDER BY n.[CreatedAt] DESC, n.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@UnreadOnly", SqlDbType.Bit, unreadOnly);
        var items = new List<NotificationRecord>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read()) items.Add(ReadNotification(reader));
        }

        // Keep the response projection explicit and avoid an open reader while
        // querying delivery rows. A bounded inbox (max 200) keeps this simple.
        var projected = items.Select(item => Project(connection, transaction, actor, item)).ToArray();
        using var count = connection.CreateCommand();
        count.Transaction = transaction;
        count.CommandText = """
            SELECT COUNT_BIG(1)
            FROM [notifications].[Notification] n
            WHERE n.[OwnerUserId] = (SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId)
              AND n.[DeletedAt] IS NULL AND n.[ReadAt] IS NULL;
            """;
        Add(count, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        var unreadCount = Convert.ToInt32(count.ExecuteScalar() ?? 0L);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read")) return ModuleUnavailable<NotificationPage>();
        transaction.Commit();
        return IdentityOperationResult<NotificationPage>.Success(new NotificationPage(projected, null, unreadCount));
    }

    public IdentityOperationResult<NotificationRecord> MarkRead(IdentityPrincipal actor, Guid notificationId,
        NotificationMarkReadCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        var action = command.Read ? "notifications.inbox.mark_read" : "notifications.inbox.mark_unread";
        if (!ModuleAvailable(actor, "FX06", action)) return ModuleUnavailable<NotificationRecord>();
        if (!TryDecodeETag(command.IfMatch, out var expectedVersion)) return Precondition<NotificationRecord>(command.IfMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<NotificationRecord>(connection, transaction, actor,
            "notifications.inbox.read", action, idempotencyKey,
            JsonSerializer.Serialize(new { notificationId, command.Read, command.IfMatch }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var current = ReadNotification(connection, transaction, actor, notificationId, forUpdate: true);
            if (current is null) { transaction.Rollback(); return Missing<NotificationRecord>(); }
            if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<NotificationRecord>();
            }
            Execute(connection, transaction,
                "UPDATE [notifications].[Notification] SET [ReadAt] = CASE WHEN @Read = 1 THEN COALESCE([ReadAt], SYSUTCDATETIME()) ELSE NULL END WHERE [Id] = @Id AND [OwnerUserId] = @UserId AND [RowVersion] = @RowVersion AND [DeletedAt] IS NULL;",
                ("@Read", SqlDbType.Bit, (object)command.Read),
                ("@Id", SqlDbType.UniqueIdentifier, (object)notificationId),
                ("@UserId", SqlDbType.UniqueIdentifier, (object)actor.UserId),
                ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            var updated = ReadNotification(connection, transaction, actor, notificationId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<NotificationRecord>("PersistenceFailure", 500, "Notification could not be loaded after update."); }
            WriteAudit(connection, transaction, actor, notificationId, action, traceId);
            var canRead = _capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read");
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", action))
            { transaction.Rollback(); return ModuleUnavailable<NotificationRecord>(); }
            var projected = canRead ? Project(connection, transaction, actor, updated) : null;
            var result = canRead ? IdentityOperationResult<NotificationRecord>.Success(projected!, 200, "NotificationReadStateUpdated")
                : IdentityOperationResult<NotificationRecord>.Committed(updated.Id, 200, "NotificationReadStateUpdated");
            CompleteReceipt(connection, transaction, receipt, result.Code, 200,
                JsonSerializer.Serialize(new InboxReceipt(updated.Id, updated.ETag, action, canRead ? _receipts.DigestSensitive(JsonSerializer.Serialize(projected)) : null, null, null)));
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", action))
            { transaction.Rollback(); return ModuleUnavailable<NotificationRecord>(); }
            if (canRead && !_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read"))
            { transaction.Rollback(); return ModuleUnavailable<NotificationRecord>(); }
            transaction.Commit();
            return result;
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<NotificationRecord>(exception);
        }
    }

    public IdentityOperationResult<NotificationMarkAllReadResult> MarkAllRead(IdentityPrincipal actor,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX06", "notifications.inbox.mark_all_read")) return ModuleUnavailable<NotificationMarkAllReadResult>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<NotificationMarkAllReadResult>(connection, transaction, actor,
            "notifications.inbox.mark_all_read", "notifications.inbox.mark_all_read", idempotencyKey,
            "mark-all-read:v1", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var watermark = DateTime.UtcNow;
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE [notifications].[Notification] SET [ReadAt] = COALESCE([ReadAt], @Watermark) WHERE [OwnerUserId] = @UserId AND [DeletedAt] IS NULL AND [ReadAt] IS NULL AND [CreatedAt] <= @Watermark;";
            Add(update, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(update, "@Watermark", SqlDbType.DateTime2, watermark);
            var updatedCount = update.ExecuteNonQuery();
            var result = new NotificationMarkAllReadResult(new DateTimeOffset(watermark, TimeSpan.Zero), updatedCount);
            WriteAudit(connection, transaction, actor, null, "notifications.inbox.mark_all_read", traceId);
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.mark_all_read"))
            { transaction.Rollback(); return ModuleUnavailable<NotificationMarkAllReadResult>(); }
            var canRead = _capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read");
            CompleteReceipt(connection, transaction, receipt, "NotificationsMarkedRead", 200,
                JsonSerializer.Serialize(new InboxReceipt(actor.OwnerId, null, "notifications.inbox.mark_all_read", canRead ? "Outcome" : null, canRead ? result.Watermark : null, canRead ? result.UpdatedCount : null)));
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.mark_all_read") ||
                (canRead && !_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read")))
            { transaction.Rollback(); return ModuleUnavailable<NotificationMarkAllReadResult>(); }
            transaction.Commit();
            return canRead ? IdentityOperationResult<NotificationMarkAllReadResult>.Success(result, 200, "NotificationsMarkedRead")
                : IdentityOperationResult<NotificationMarkAllReadResult>.Committed(actor.OwnerId, 200, "NotificationsMarkedRead");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<NotificationMarkAllReadResult>(exception);
        }
    }

    public IdentityOperationResult<object?> Delete(IdentityPrincipal actor, NotificationDeleteCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX06", "notifications.inbox.delete")) return ModuleUnavailable<object?>();
        var ids = command.NotificationIds?.Distinct().Order().ToArray() ?? [];
        if (ids.Length is < 1 or > 100 || ids.Contains(Guid.Empty)) return Failure<object?>("ValidationFailed", 422, "Select between 1 and 100 notification IDs.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "notifications.inbox.delete", "notifications.inbox.delete", idempotencyKey, JsonSerializer.Serialize(new { ids }), out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var parameters = new List<(string Name, SqlDbType Type, object Value)> {
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId)
            };
            var names = new List<string>();
            for (var index = 0; index < ids.Length; index++)
            {
                var name = "@Id" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                names.Add(name);
                parameters.Add((name, SqlDbType.UniqueIdentifier, ids[index]));
            }
            using (var selected = connection.CreateCommand())
            {
                selected.Transaction = transaction;
                selected.CommandText = $"SELECT COUNT_BIG(1) FROM [notifications].[Notification] WITH (UPDLOCK, HOLDLOCK) WHERE [OwnerUserId]=@UserId AND [Id] IN ({string.Join(',', names)});";
                foreach (var parameter in parameters) Add(selected, parameter.Name, parameter.Type, parameter.Value);
                if (Convert.ToInt64(selected.ExecuteScalar()) != ids.Length)
                { transaction.Rollback(); return Missing<object?>(); }
            }
            Execute(connection, transaction,
                $"UPDATE [notifications].[Notification] SET [DeletedAt] = COALESCE([DeletedAt], SYSUTCDATETIME()) WHERE [OwnerUserId] = @UserId AND [Id] IN ({string.Join(',', names)}) AND [DeletedAt] IS NULL;",
                parameters.ToArray());
            WriteAudit(connection, transaction, actor, null, "notifications.inbox.delete", traceId);
            CompleteReceipt(connection, transaction, receipt, "NotificationsDeleted", 204, null);
            if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.delete"))
            { transaction.Rollback(); return ModuleUnavailable<object?>(); }
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("NotificationsDeleted");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static NotificationRecord? ReadNotification(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, Guid notificationId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT n.[Id], n.[Kind], n.[Title], n.[Body], n.[SourceRef], n.[CreatedAt], n.[ReadAt], n.[RowVersion] FROM [notifications].[Notification] n WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")}) WHERE n.[Id] = @Id AND n.[OwnerUserId] = @OwnerUserId AND n.[DeletedAt] IS NULL AND EXISTS (SELECT 1 FROM [platform].[PersonalSpace] p WHERE p.[Id] = @OwnerId AND p.[UserId] = n.[OwnerUserId]);";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, notificationId);
        Add(command, "@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadNotification(reader) : null;
    }

    private static NotificationRecord ReadNotification(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
            ToOffset(reader.GetDateTime(5)), reader.IsDBNull(6) ? null : ToOffset(reader.GetDateTime(6)), EncodeETag(reader.GetFieldValue<byte[]>(7)), Array.Empty<NotificationDeliveryRecord>());

    private static IReadOnlyList<NotificationDeliveryRecord> ReadDeliveries(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid notificationId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT d.[Channel], d.[State], d.[Attempts], d.[LastErrorCode], d.[UpdatedAt] FROM [notifications].[Delivery] d INNER JOIN [notifications].[Notification] n ON n.[Id]=d.[NotificationId] INNER JOIN [platform].[PersonalSpace] p ON p.[UserId]=n.[OwnerUserId] WHERE d.[NotificationId]=@NotificationId AND n.[OwnerUserId]=@UserId AND p.[Id]=@OwnerId AND n.[DeletedAt] IS NULL ORDER BY d.[Channel];";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@NotificationId", SqlDbType.UniqueIdentifier, notificationId);
        using var reader = command.ExecuteReader();
        var deliveries = new List<NotificationDeliveryRecord>();
        while (reader.Read()) deliveries.Add(new NotificationDeliveryRecord(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3), ToOffset(reader.GetDateTime(4))));
        return deliveries;
    }

    private static NotificationRecord Project(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, NotificationRecord value)
    {
        var projected = value with { Deliveries = ReadDeliveries(connection, transaction, actor, value.Id) };
        // Source-linked legacy snapshots can contain a private task/event title.
        // No registered current source projection contract is installed here.
        // Until its owner supplies one, fail closed without cross-module private
        // table reads or using cached content as present source authority.
        return projected.SourceRef is null ? projected : projected with
        {
            Title = "Source unavailable",
            Body = "This source notification is unavailable. Return to its module to check current access.",
            SourceRef = null,
            SourceUnavailable = true
        };
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string actionKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX06", actionKey)) return ModuleUnavailable<T>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay && claim.ResultStatusCode == 204)
            return _capabilities.IsAllowed(connection, transaction, actor, "FX06", actionKey)
                ? IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent") : ModuleUnavailable<T>();
        if (claim.IsReplay && claim.ResultStatusCode == 200 && claim.ResultJson is not null)
        {
            try
            {
                var stored = JsonSerializer.Deserialize<InboxReceipt>(claim.ResultJson);
                if (stored is not null && stored.Id != Guid.Empty && stored.Action == actionKey)
                {
                    var code = claim.ResultCode ?? "Ok";
                    if (stored.ProjectionDigest is not null && _capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read"))
                    {
                        if (typeof(T) == typeof(NotificationRecord))
                        {
                            var current = ReadNotification(connection, transaction, actor, stored.Id, false);
                            if (current is not null) current = Project(connection, transaction, actor, current);
                            if (current is not null && current.ETag == stored.ETag && _receipts.DigestSensitive(JsonSerializer.Serialize(current)) == stored.ProjectionDigest &&
                                _capabilities.IsAllowed(connection, transaction, actor, "FX06", actionKey) &&
                                _capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read"))
                                return IdentityOperationResult<T>.Success((T)(object)current, 200, code);
                        }
                        else if (typeof(T) == typeof(NotificationMarkAllReadResult) && stored.Id == actor.OwnerId && stored.Watermark is { } watermark && stored.UpdatedCount is { } count &&
                            _capabilities.IsAllowed(connection, transaction, actor, "FX06", actionKey) &&
                            _capabilities.IsAllowed(connection, transaction, actor, "FX06", "notifications.inbox.read"))
                            return IdentityOperationResult<T>.Success((T)(object)new NotificationMarkAllReadResult(watermark, count), 200, code);
                    }
                    return _capabilities.IsAllowed(connection, transaction, actor, "FX06", actionKey)
                        ? IdentityOperationResult<T>.Committed(stored.Id, 200, code) : ModuleUnavailable<T>();
                }
            }
            catch (JsonException) { }
        }
        // Legacy unbound full DTO receipts are never replayed as read authority.
        var failure = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(failure, 409, "The request was already completed or is in progress.");
    }
    private sealed record InboxReceipt(Guid Id, string? ETag, string Action, string? ProjectionDigest, DateTimeOffset? Watermark, int? UpdatedCount);

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode,
        int? resultStatusCode = null, string? resultJson = null) => _receipts.Complete(connection, transaction, claim, resultCode, resultStatusCode, resultJson);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid? targetId, string action, string? traceId) =>
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'notifications.Notification', @Target, 'Succeeded', @TraceId);",
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Action", SqlDbType.NVarChar, (object)action), ("@Target", SqlDbType.UniqueIdentifier, (object?)targetId ?? DBNull.Value), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type);
        if (size > 0) parameter.Size = size;
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Notifications are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Notification unavailable.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Notification revision changed.");
    private static IdentityOperationResult<T> Precondition<T>(string? ifMatch) => Failure<T>(string.IsNullOrWhiteSpace(ifMatch) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(ifMatch) ? 428 : 412, string.IsNullOrWhiteSpace(ifMatch) ? "If-Match is required." : "If-Match is invalid.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Notification persistence is unavailable.");

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('\"'));
    private static bool TryDecodeETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8 && EncodeETag(bytes) == value;
        }
        catch (FormatException) { return false; }
    }
}
