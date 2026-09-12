using System.Data;
using System.Security.Cryptography;
using System.Text;
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
        if (!ModuleAvailable(actor, "FX06", "notifications.inbox.read", "notifications.view")) return ModuleUnavailable<NotificationPage>();
        var take = Math.Clamp(limit ?? 50, 1, 200);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
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
        var projected = items.Select(item => item with
        {
            Deliveries = ReadDeliveries(connection, item.Id)
        }).ToArray();
        using var count = connection.CreateCommand();
        count.CommandText = """
            SELECT COUNT_BIG(1)
            FROM [notifications].[Notification] n
            WHERE n.[OwnerUserId] = (SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId)
              AND n.[DeletedAt] IS NULL AND n.[ReadAt] IS NULL;
            """;
        Add(count, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        var unreadCount = Convert.ToInt32(count.ExecuteScalar() ?? 0L);
        return IdentityOperationResult<NotificationPage>.Success(new NotificationPage(projected, null, unreadCount));
    }

    public IdentityOperationResult<NotificationRecord> MarkRead(IdentityPrincipal actor, Guid notificationId,
        NotificationMarkReadCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX06", command.Read ? "notifications.inbox.mark_read" : "notifications.inbox.mark_unread")) return ModuleUnavailable<NotificationRecord>();
        if (!TryDecodeETag(command.IfMatch, out var expectedVersion)) return Precondition<NotificationRecord>(command.IfMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<NotificationRecord>(connection, transaction, actor,
            "notifications.inbox.read", idempotencyKey,
            $"notification:{notificationId:N}|read:{command.Read}|etag:{command.IfMatch}", out var receipt);
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
            WriteAudit(connection, transaction, actor, notificationId, command.Read ? "notifications.inbox.mark_read" : "notifications.inbox.mark_unread", traceId);
            CompleteReceipt(connection, transaction, receipt, "NotificationReadStateUpdated");
            transaction.Commit();
            return IdentityOperationResult<NotificationRecord>.Success(updated);
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
        var watermark = DateTime.UtcNow;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<NotificationMarkAllReadResult>(connection, transaction, actor,
            "notifications.inbox.mark_all_read", idempotencyKey,
            $"watermark:{watermark:O}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE [notifications].[Notification] SET [ReadAt] = COALESCE([ReadAt], @Watermark) WHERE [OwnerUserId] = @UserId AND [DeletedAt] IS NULL AND [ReadAt] IS NULL AND [CreatedAt] <= @Watermark;";
            Add(update, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(update, "@Watermark", SqlDbType.DateTime2, watermark);
            var updatedCount = update.ExecuteNonQuery();
            WriteAudit(connection, transaction, actor, null, "notifications.inbox.mark_all_read", traceId);
            CompleteReceipt(connection, transaction, receipt, "NotificationsMarkedRead");
            transaction.Commit();
            return IdentityOperationResult<NotificationMarkAllReadResult>.Success(new NotificationMarkAllReadResult(new DateTimeOffset(watermark, TimeSpan.Zero), updatedCount));
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
        if (!ModuleAvailable(actor, "FX06", "notifications.inbox.delete", "notifications.delete")) return ModuleUnavailable<object?>();
        var ids = command.NotificationIds.Distinct().Take(100).ToArray();
        if (ids.Length == 0) return Failure<object?>("ValidationFailed", 422, "At least one notification is required.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var canonical = string.Join(',', ids.OrderBy(id => id).Select(id => id.ToString("N")));
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "notifications.inbox.delete", idempotencyKey, $"notifications:{canonical}", out var receipt);
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
            Execute(connection, transaction,
                $"UPDATE [notifications].[Notification] SET [DeletedAt] = COALESCE([DeletedAt], SYSUTCDATETIME()) WHERE [OwnerUserId] = @UserId AND [Id] IN ({string.Join(',', names)}) AND [DeletedAt] IS NULL;",
                parameters.ToArray());
            WriteAudit(connection, transaction, actor, null, "notifications.inbox.delete", traceId);
            CompleteReceipt(connection, transaction, receipt, "NotificationsDeleted");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<NotificationRecord> Publish(IdentityPrincipal actor, NotificationPublishCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!string.Equals(actor.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return Failure<NotificationRecord>("PermissionDenied", 403, "Only a SuperAdmin may publish a system notification.");
        if (!ModuleAvailable(actor, "FX06", "notifications.dispatch.publish")) return ModuleUnavailable<NotificationRecord>();
        var validation = ValidatePublish(command);
        if (validation is not null) return validation;
        var recipientId = command.RecipientUserId ?? actor.UserId;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<NotificationRecord>(connection, transaction, actor,
            "notifications.dispatch.publish", idempotencyKey,
            $"logical:{command.LogicalKey}|recipient:{recipientId:N}|kind:{command.Kind}|title:{command.Title}|body:{command.Body}|source:{command.SourceRef}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            if (!UserOwnsActiveSpace(connection, transaction, recipientId))
            {
                transaction.Rollback();
                return Failure<NotificationRecord>("ResourceUnavailable", 404, "Notification recipient is unavailable.");
            }
            var notificationId = Guid.NewGuid();
            Execute(connection, transaction,
                "INSERT INTO [notifications].[Notification] ([Id], [OwnerUserId], [LogicalKey], [Kind], [Title], [Body], [SourceRef]) VALUES (@Id, @OwnerUserId, @LogicalKey, @Kind, @Title, @Body, @SourceRef);",
                ("@Id", SqlDbType.UniqueIdentifier, (object)notificationId),
                ("@OwnerUserId", SqlDbType.UniqueIdentifier, (object)recipientId),
                ("@LogicalKey", SqlDbType.NVarChar, (object)command.LogicalKey),
                ("@Kind", SqlDbType.NVarChar, (object)command.Kind),
                ("@Title", SqlDbType.NVarChar, (object)command.Title),
                ("@Body", SqlDbType.NVarChar, (object)command.Body),
                ("@SourceRef", SqlDbType.NVarChar, (object?)command.SourceRef ?? DBNull.Value));
            foreach (var channel in new[] { (Name: "InApp", State: "Pending"), (Name: "Email", State: "Pending"), (Name: "BrowserPush", State: "PermissionUnavailable") })
            {
                Execute(connection, transaction,
                    "INSERT INTO [notifications].[Delivery] ([NotificationId], [Channel], [State], [LastErrorCode]) VALUES (@NotificationId, @Channel, @State, @LastErrorCode);",
                    ("@NotificationId", SqlDbType.UniqueIdentifier, (object)notificationId),
                    ("@Channel", SqlDbType.VarChar, (object)channel.Name),
                    ("@State", SqlDbType.VarChar, (object)channel.State),
                    ("@LastErrorCode", SqlDbType.NVarChar, (object?)(channel.Name == "BrowserPush" ? "PushPermissionUnavailable" : null) ?? DBNull.Value));
            }
            Execute(connection, transaction,
                "INSERT INTO [operations].[Outbox] ([OwnerUserId], [LogicalKey], [Kind], [PayloadJson], [State]) VALUES (@OwnerUserId, @LogicalKey, 'Notifications.DispatchRequested', @PayloadJson, 'Pending');",
                ("@OwnerUserId", SqlDbType.UniqueIdentifier, (object)recipientId),
                ("@LogicalKey", SqlDbType.NVarChar, (object)$"notification.dispatch:{command.LogicalKey}"),
                ("@PayloadJson", SqlDbType.NVarChar, (object)$"{{\"notificationId\":\"{notificationId:N}\",\"channels\":[\"InApp\",\"Email\",\"BrowserPush\"]}}"));
            WriteAudit(connection, transaction, actor, notificationId, "notifications.dispatch.publish", traceId);
            var created = ReadNotification(connection, transaction, actor with { UserId = recipientId, OwnerId = actor.OwnerId }, notificationId, forUpdate: false, ownerUserIdOverride: recipientId);
            if (created is null) { transaction.Rollback(); return Failure<NotificationRecord>("PersistenceFailure", 500, "Notification could not be loaded after publish."); }
            CompleteReceipt(connection, transaction, receipt, "NotificationPublished");
            transaction.Commit();
            return IdentityOperationResult<NotificationRecord>.Success(created, 201, "NotificationPublished");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return Failure<NotificationRecord>("NotificationDuplicate", 409, "The notification logical key already exists.");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<NotificationRecord>(exception);
        }
    }

    private static IdentityOperationResult<NotificationRecord>? ValidatePublish(NotificationPublishCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Kind) || command.Kind.Length > 100 ||
            string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(command.Body) || command.Body.Trim().Length > 1000 ||
            string.IsNullOrWhiteSpace(command.LogicalKey) || command.LogicalKey.Length > 200 ||
            command.SourceRef is { Length: > 200 })
            return Failure<NotificationRecord>("ValidationFailed", 422, "Notification fields are invalid.");
        if (command.Body.Contains("token", StringComparison.OrdinalIgnoreCase) || command.Body.Contains("password", StringComparison.OrdinalIgnoreCase))
            return Failure<NotificationRecord>("SensitivePayloadRejected", 422, "Notification body must not contain credential or token material.");
        return null;
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static bool UserOwnsActiveSpace(SqlConnection connection, SqlTransaction transaction, Guid userId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM [identity].[User] u INNER JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id] WHERE u.[Id] = @UserId AND u.[State] = 'Active' AND u.[IsDeleted] = 0 AND ps.[State] = 'Active';";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        return command.ExecuteScalar() is not null;
    }

    private static NotificationRecord? ReadNotification(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, Guid notificationId, bool forUpdate, Guid? ownerUserIdOverride = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT n.[Id], n.[Kind], n.[Title], n.[Body], n.[SourceRef], n.[CreatedAt], n.[ReadAt], n.[RowVersion] FROM [notifications].[Notification] n WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE n.[Id] = @Id AND n.[OwnerUserId] = @OwnerUserId AND n.[DeletedAt] IS NULL;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, notificationId);
        Add(command, "@OwnerUserId", SqlDbType.UniqueIdentifier, ownerUserIdOverride ?? actor.UserId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadNotification(reader) : null;
    }

    private static NotificationRecord ReadNotification(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
            ToOffset(reader.GetDateTime(5)), reader.IsDBNull(6) ? null : ToOffset(reader.GetDateTime(6)), EncodeETag(reader.GetFieldValue<byte[]>(7)), Array.Empty<NotificationDeliveryRecord>());

    private static IReadOnlyList<NotificationDeliveryRecord> ReadDeliveries(SqlConnection connection, Guid notificationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [Channel], [State], [Attempts], [LastErrorCode], [UpdatedAt] FROM [notifications].[Delivery] WHERE [NotificationId] = @NotificationId ORDER BY [Channel];";
        Add(command, "@NotificationId", SqlDbType.UniqueIdentifier, notificationId);
        using var reader = command.ExecuteReader();
        var deliveries = new List<NotificationDeliveryRecord>();
        while (reader.Read()) deliveries.Add(new NotificationDeliveryRecord(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3), ToOffset(reader.GetDateTime(4))));
        return deliveries;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) => _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid? targetId, string action, string? traceId) =>
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'notifications.Notification', @Target, 'Succeeded', @TraceId);",
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
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
            if (string.IsNullOrWhiteSpace(value)) return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }
}
