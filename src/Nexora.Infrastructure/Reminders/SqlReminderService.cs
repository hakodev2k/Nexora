using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Reminders;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Reminders;

/// <summary>
/// SQL-backed local reminder authority. A scheduled intent is durable before a
/// worker sees it. The worker only creates an owner-local inbox projection; it
/// never sends email, pushes a browser message, or calls an external provider.
/// </summary>
public sealed class SqlReminderService : IReminderService, IReminderDispatchService
{
    private const int DispatchBatchSize = 25;
    private static readonly TimeSpan LateDeliveryWindow = TimeSpan.FromMinutes(15);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlReminderService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<ReminderSourceView> Get(IdentityPrincipal actor, string sourceType, Guid sourceId)
    {
        if (!TrySourceCapability(sourceType, out var sourceModule, out var sourceReadAction, out _)) return SourceUnavailable<ReminderSourceView>();
        if (!ModuleAvailable(actor, "FX14", "reminders.configuration.read") ||
            !ModuleAvailable(actor, sourceModule, sourceReadAction))
        {
            return ModuleUnavailable<ReminderSourceView>();
        }

        using var connection = _connections.Create();
        connection.Open();
        var source = ReadSource(connection, null, actor.OwnerId, sourceType, sourceId, false);
        if (source is null) return SourceUnavailable<ReminderSourceView>();
        return IdentityOperationResult<ReminderSourceView>.Success(ToView(connection, null, source));
    }

    public IdentityOperationResult<ReminderSourceView> Set(IdentityPrincipal actor, string sourceType, Guid sourceId,
        string? ifMatch, ReminderSetCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!TrySourceCapability(sourceType, out var sourceModule, out _, out var sourceWriteAction)) return SourceUnavailable<ReminderSourceView>();
        if (!ModuleAvailable(actor, "FX14", "reminders.configuration.set") ||
            !ModuleAvailable(actor, sourceModule, sourceWriteAction))
        {
            return ModuleUnavailable<ReminderSourceView>();
        }
        if (!TryDecodeETag(command.SourceETag, out var expectedSourceVersion)) return SourcePrecondition<ReminderSourceView>(command.SourceETag);
        if (!TryNormalizeConfiguration(command.ConfigType, out var configType)) return Validation<ReminderSourceView>("The reminder configuration type is not supported.");
        if (!TryValidateExactPrecondition(ifMatch, out var expectedReminderVersion)) return ReminderPrecondition<ReminderSourceView>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<ReminderSourceView>(connection, transaction, actor, "reminders.configuration.set", idempotencyKey,
            $"source:{sourceType}|id:{sourceId:N}|sourceEtag:{command.SourceETag}|reminderEtag:{ifMatch}|config:{configType}|exact:{command.ExactAt?.UtcDateTime:O}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        var source = ReadSource(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        if (source is null) return Rollback(transaction, SourceUnavailable<ReminderSourceView>());
        if (!expectedSourceVersion.AsSpan().SequenceEqual(source.RowVersion)) return Rollback(transaction, Revision<ReminderSourceView>("The source revision changed."));

        var current = ReadReminder(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        if (current is not null && expectedReminderVersion is null) return Rollback(transaction, ReminderPrecondition<ReminderSourceView>(null));
        if (current is null && expectedReminderVersion is not null) return Rollback(transaction, Revision<ReminderSourceView>("The reminder revision changed."));
        if (current is not null && !expectedReminderVersion!.AsSpan().SequenceEqual(current.RowVersion))
        {
            return Rollback(transaction, Revision<ReminderSourceView>("The reminder revision changed."));
        }

        if (!IsValidTimeZone(source.TimeZoneId)) return Rollback(transaction, Validation<ReminderSourceView>("The source timezone is not valid."));
        var schedule = ReminderPolicy.Resolve(configType, command.ExactAt, source.StartAt, DateTimeOffset.UtcNow);
        if (!schedule.IsValid)
        {
            return Rollback(transaction, Failure<ReminderSourceView>(schedule.Code ?? "ValidationFailed", 422, schedule.Title ?? "The reminder schedule is invalid."));
        }
        if (configType != ReminderConfigurationTypes.None && !source.CanConfigure)
        {
            return Rollback(transaction, LifecycleLocked<ReminderSourceView>());
        }

        if (source.CanConfigure && !TouchSource(connection, transaction, source, schedule.DueAt))
        {
            return Rollback(transaction, Revision<ReminderSourceView>("The source revision changed."));
        }
        if (source.CanConfigure)
        {
            source = ReadSource(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
            if (source is null) return Rollback(transaction, SourceUnavailable<ReminderSourceView>());
        }

        current = ReadReminder(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        var reminder = UpsertReminder(connection, transaction, current, source, configType, command.ExactAt, schedule.DueAt);
        InsertIntent(connection, transaction, source, reminder, configType == ReminderConfigurationTypes.None ? "InvalidateRequested" : "ScheduleRequested");
        WriteAudit(connection, transaction, actor.UserId, actor.UserId, reminder.Id, "reminders.configuration.set", traceId);
        CompleteReceipt(connection, transaction, receipt, "ReminderConfigured");
        transaction.Commit();
        return IdentityOperationResult<ReminderSourceView>.Success(ToView(connection, null, source), current is null ? 201 : 200, "ReminderConfigured");
    }

    public IdentityOperationResult<object?> Remove(IdentityPrincipal actor, string sourceType, Guid sourceId,
        string? ifMatch, ReminderRemoveCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!TrySourceCapability(sourceType, out var sourceModule, out _, out var sourceWriteAction)) return SourceUnavailable<object?>();
        if (!ModuleAvailable(actor, "FX14", "reminders.configuration.remove") ||
            !ModuleAvailable(actor, sourceModule, sourceWriteAction))
        {
            return ModuleUnavailable<object?>();
        }
        if (!TryDecodeETag(command.SourceETag, out var expectedSourceVersion)) return SourcePrecondition<object?>(command.SourceETag);
        if (!TryDecodeETag(ifMatch, out var expectedReminderVersion)) return ReminderPrecondition<object?>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "reminders.configuration.remove", idempotencyKey,
            $"source:{sourceType}|id:{sourceId:N}|sourceEtag:{command.SourceETag}|reminderEtag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        var source = ReadSource(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        if (source is null) return Rollback(transaction, SourceUnavailable<object?>());
        if (!expectedSourceVersion.AsSpan().SequenceEqual(source.RowVersion)) return Rollback(transaction, Revision<object?>("The source revision changed."));
        var current = ReadReminder(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        if (current is null) return Rollback(transaction, SourceUnavailable<object?>());
        if (!expectedReminderVersion.AsSpan().SequenceEqual(current.RowVersion)) return Rollback(transaction, Revision<object?>("The reminder revision changed."));

        if (source.CanConfigure && !TouchSource(connection, transaction, source, null))
        {
            return Rollback(transaction, Revision<object?>("The source revision changed."));
        }
        if (source.CanConfigure)
        {
            source = ReadSource(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
            if (source is null) return Rollback(transaction, SourceUnavailable<object?>());
        }
        current = ReadReminder(connection, transaction, actor.OwnerId, sourceType, sourceId, true);
        if (current is null) return Rollback(transaction, PersistenceFailure<object?>());
        var removed = UpsertReminder(connection, transaction, current, source, ReminderConfigurationTypes.None, null, null);
        InsertIntent(connection, transaction, source, removed, "InvalidateRequested");
        WriteAudit(connection, transaction, actor.UserId, actor.UserId, removed.Id, "reminders.configuration.remove", traceId);
        CompleteReceipt(connection, transaction, receipt, "ReminderRemoved");
        transaction.Commit();
        return IdentityOperationResult<object?>.Success(null, 204, "ReminderRemoved");
    }

    public Task<ReminderDispatchResult> DispatchDueAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = _connections.Create();
        connection.Open();
        var ids = ReadDueReminderIds(connection);
        var result = new ReminderDispatchResult(0, 0, 0, 0);
        foreach (var reminderId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result = Add(result, DispatchOne(reminderId));
        }

        return Task.FromResult(result);
    }

    private ReminderDispatchResult DispatchOne(Guid reminderId)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var reminder = ReadReminderById(connection, transaction, reminderId, true);
        if (reminder is null || reminder.State != ReminderStates.Pending || reminder.DueAt is null || reminder.DueAt > DateTimeOffset.UtcNow)
        {
            transaction.Rollback();
            return new ReminderDispatchResult(0, 0, 0, 0);
        }

        var source = ReadSource(connection, transaction, reminder.OwnerId, reminder.SourceType, reminder.SourceId, true);
        if (source is null || !source.CanConfigure || source.Revision != reminder.SourceRevision)
        {
            UpdateReminderState(connection, transaction, reminder, ReminderStates.Canceled, null);
            WriteAudit(connection, transaction, source?.OwnerUserId ?? Guid.Empty, source?.OwnerUserId ?? Guid.Empty, reminder.Id, "reminders.schedule.invalidate", null);
            transaction.Commit();
            return new ReminderDispatchResult(0, 0, 1, 0);
        }
        if (!IsDispatchAllowed(connection, transaction, source))
        {
            transaction.Rollback();
            return new ReminderDispatchResult(0, 0, 0, 1);
        }

        var now = DateTimeOffset.UtcNow;
        if (now - reminder.DueAt.Value > LateDeliveryWindow)
        {
            UpdateReminderState(connection, transaction, reminder, ReminderStates.Missed, null);
            WriteAudit(connection, transaction, source.OwnerUserId, source.OwnerUserId, reminder.Id, "reminders.schedule.dispatch", null);
            transaction.Commit();
            return new ReminderDispatchResult(0, 1, 0, 0);
        }

        var notificationId = EnsureNotificationProjection(connection, transaction, reminder, source);
        UpdateReminderState(connection, transaction, reminder, ReminderStates.Dispatched, notificationId);
        WriteAudit(connection, transaction, source.OwnerUserId, source.OwnerUserId, reminder.Id, "reminders.schedule.dispatch", null);
        transaction.Commit();
        return new ReminderDispatchResult(1, 0, 0, 0);
    }

    private ReminderSourceView ToView(SqlConnection connection, SqlTransaction? transaction, ReminderSourceSnapshot source)
    {
        var reminder = ReadReminder(connection, transaction, source.OwnerId, source.SourceType, source.SourceId, false);
        return new ReminderSourceView(source.SourceType, source.SourceId, source.Title, source.StartAt, source.TimeZoneId,
            EncodeETag(source.RowVersion), reminder is null ? null : ToRecord(connection, transaction, reminder));
    }

    private ReminderRecord ToRecord(SqlConnection connection, SqlTransaction? transaction, ReminderRow row) => new(
        row.Id, row.SourceType, row.SourceId, row.ConfigType, row.ExactAt, row.TimeZoneId, row.DueAt, row.SourceRevision,
        row.State, row.CreatedAt, row.UpdatedAt, EncodeETag(row.RowVersion), ReadDeliveries(connection, transaction, row.LastNotificationId));

    private ReminderSourceSnapshot? ReadSource(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        string sourceType, Guid sourceId, bool forUpdate)
    {
        if (sourceType == ReminderSourceTypes.Task)
        {
            return ReadSource(connection, transaction, forUpdate, """
                SELECT taskRow.[Id], taskRow.[Title], taskRow.[StartAt], userRow.[TimeZoneId], taskRow.[Status], projectRow.[Status],
                       CONVERT(bigint, taskRow.[RowVersion]), taskRow.[RowVersion], spaceRow.[UserId]
                FROM [productivity].[Task] taskRow
                INNER JOIN [productivity].[Project] projectRow ON projectRow.[Id] = taskRow.[ProjectId] AND projectRow.[OwnerId] = taskRow.[OwnerId]
                INNER JOIN [platform].[PersonalSpace] spaceRow ON spaceRow.[Id] = taskRow.[OwnerId]
                INNER JOIN [identity].[User] userRow ON userRow.[Id] = spaceRow.[UserId]
                WHERE taskRow.[Id] = @SourceId AND taskRow.[OwnerId] = @OwnerId;
                """, ownerId, sourceId, sourceType);
        }

        if (sourceType == ReminderSourceTypes.CalendarEvent)
        {
            return ReadSource(connection, transaction, forUpdate, """
                SELECT eventRow.[Id], eventRow.[Title], eventRow.[StartAt], eventRow.[TimeZoneId], eventRow.[Status], eventRow.[Status],
                       CONVERT(bigint, eventRow.[RowVersion]), eventRow.[RowVersion], spaceRow.[UserId]
                FROM [calendar].[Event] eventRow
                INNER JOIN [platform].[PersonalSpace] spaceRow ON spaceRow.[Id] = eventRow.[OwnerId]
                WHERE eventRow.[Id] = @SourceId AND eventRow.[OwnerId] = @OwnerId
                  AND eventRow.[SourceKind] = 'Manual' AND eventRow.[TaskId] IS NULL;
                """, ownerId, sourceId, sourceType);
        }

        return null;
    }

    private static ReminderSourceSnapshot? ReadSource(SqlConnection connection, SqlTransaction? transaction, bool forUpdate,
        string sql, Guid ownerId, Guid sourceId, string sourceType)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = forUpdate
            ? sql.Replace("FROM [productivity].[Task] taskRow", "FROM [productivity].[Task] taskRow WITH (UPDLOCK, ROWLOCK)")
                .Replace("FROM [calendar].[Event] eventRow", "FROM [calendar].[Event] eventRow WITH (UPDLOCK, ROWLOCK)")
            : sql;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@SourceId", SqlDbType.UniqueIdentifier, sourceId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var sourceStatus = reader.GetString(4);
        var parentStatus = reader.GetString(5);
        var canConfigure = sourceType == ReminderSourceTypes.Task
            ? (sourceStatus is "NotStarted" or "InProgress") && (parentStatus is "NotStarted" or "InProgress")
            : sourceStatus == "Scheduled";
        return new ReminderSourceSnapshot(sourceType, reader.GetGuid(0), ownerId, reader.GetString(1), ToOffset(reader.GetDateTime(2)),
            reader.GetString(3), reader.GetInt64(6), reader.GetFieldValue<byte[]>(7), reader.GetGuid(8), canConfigure);
    }

    private ReminderRow? ReadReminder(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        string sourceType, Guid sourceId, bool forUpdate) => ReadReminder(connection, transaction, $"""
            SELECT [Id], [OwnerId], [SourceType], [SourceId], [ConfigType], [ExactAt], [TimeZoneId], [DueAt], [SourceRevision],
                   [State], [LastNotificationId], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [calendar].[Reminder]{(forUpdate ? " WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [SourceType] = @SourceType AND [SourceId] = @SourceId;
            """, ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId), ("@SourceType", SqlDbType.VarChar, (object)sourceType), ("@SourceId", SqlDbType.UniqueIdentifier, (object)sourceId));

    private ReminderRow? ReadReminderById(SqlConnection connection, SqlTransaction transaction, Guid reminderId, bool forUpdate) => ReadReminder(connection, transaction, $"""
            SELECT [Id], [OwnerId], [SourceType], [SourceId], [ConfigType], [ExactAt], [TimeZoneId], [DueAt], [SourceRevision],
                   [State], [LastNotificationId], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [calendar].[Reminder]{(forUpdate ? " WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [Id] = @Id;
            """, ("@Id", SqlDbType.UniqueIdentifier, (object)reminderId));

    private static ReminderRow? ReadReminder(SqlConnection connection, SqlTransaction? transaction, string sql,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new ReminderRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4),
            reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), reader.GetString(6), reader.IsDBNull(7) ? null : ToOffset(reader.GetDateTime(7)),
            reader.GetInt64(8), reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetGuid(10), ToOffset(reader.GetDateTime(11)),
            ToOffset(reader.GetDateTime(12)), reader.GetFieldValue<byte[]>(13));
    }

    private static IReadOnlyList<ReminderDeliveryState> ReadDeliveries(SqlConnection connection, SqlTransaction? transaction, Guid? notificationId)
    {
        if (notificationId is null) return Array.Empty<ReminderDeliveryState>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Channel], [State], [Attempts], [LastErrorCode] FROM [notifications].[Delivery] WHERE [NotificationId] = @NotificationId ORDER BY [Channel];";
        Add(command, "@NotificationId", SqlDbType.UniqueIdentifier, notificationId.Value);
        using var reader = command.ExecuteReader();
        var deliveries = new List<ReminderDeliveryState>();
        while (reader.Read()) deliveries.Add(new ReminderDeliveryState(reader.GetString(0), reader.GetString(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        return deliveries;
    }

    private static bool TouchSource(SqlConnection connection, SqlTransaction transaction, ReminderSourceSnapshot source,
        DateTimeOffset? reminderAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = source.SourceType == ReminderSourceTypes.Task ? """
            UPDATE [productivity].[Task]
            SET [ReminderAt] = @ReminderAt, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @SourceId AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
            """ : """
            UPDATE [calendar].[Event]
            SET [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @SourceId AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion
              AND [SourceKind] = 'Manual' AND [TaskId] IS NULL;
            """;
        Add(command, "@ReminderAt", SqlDbType.DateTime2, source.SourceType == ReminderSourceTypes.Task
            ? (object?)reminderAt?.UtcDateTime ?? DBNull.Value : DBNull.Value);
        Add(command, "@SourceId", SqlDbType.UniqueIdentifier, source.SourceId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, source.OwnerId);
        Add(command, "@RowVersion", SqlDbType.Binary, source.RowVersion);
        return command.ExecuteNonQuery() == 1;
    }

    private ReminderRow UpsertReminder(SqlConnection connection, SqlTransaction transaction, ReminderRow? current,
        ReminderSourceSnapshot source, string configType, DateTimeOffset? exactAt, DateTimeOffset? dueAt)
    {
        var state = configType == ReminderConfigurationTypes.None ? ReminderStates.None : ReminderStates.Pending;
        if (current is null)
        {
            var id = Guid.NewGuid();
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO [calendar].[Reminder]
                    ([Id], [OwnerId], [SourceType], [SourceId], [ConfigType], [ExactAt], [TimeZoneId], [DueAt], [SourceRevision], [State])
                VALUES
                    (@Id, @OwnerId, @SourceType, @SourceId, @ConfigType, @ExactAt, @TimeZoneId, @DueAt, @SourceRevision, @State);
                """;
            Add(insert, "@Id", SqlDbType.UniqueIdentifier, id);
            AddReminderValues(insert, source, configType, exactAt, dueAt, state);
            insert.ExecuteNonQuery();
            return ReadReminder(connection, transaction, source.OwnerId, source.SourceType, source.SourceId, true)
                ?? throw new InvalidOperationException("Reminder insert was not visible in its transaction.");
        }

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [calendar].[Reminder]
            SET [ConfigType] = @ConfigType, [ExactAt] = @ExactAt, [TimeZoneId] = @TimeZoneId, [DueAt] = @DueAt,
                [SourceRevision] = @SourceRevision, [State] = @State,
                [LastNotificationId] = CASE WHEN @State = 'Pending' THEN NULL ELSE [LastNotificationId] END,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
            """;
        AddReminderValues(update, source, configType, exactAt, dueAt, state);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, current.Id);
        Add(update, "@RowVersion", SqlDbType.Binary, current.RowVersion);
        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Reminder revision changed inside a serializable transaction.");
        return ReadReminder(connection, transaction, source.OwnerId, source.SourceType, source.SourceId, true)
            ?? throw new InvalidOperationException("Reminder update was not visible in its transaction.");
    }

    private static void AddReminderValues(SqlCommand command, ReminderSourceSnapshot source, string configType,
        DateTimeOffset? exactAt, DateTimeOffset? dueAt, string state)
    {
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, source.OwnerId);
        Add(command, "@SourceType", SqlDbType.VarChar, source.SourceType);
        Add(command, "@SourceId", SqlDbType.UniqueIdentifier, source.SourceId);
        Add(command, "@ConfigType", SqlDbType.VarChar, configType);
        Add(command, "@ExactAt", SqlDbType.DateTime2, (object?)exactAt?.UtcDateTime ?? DBNull.Value);
        Add(command, "@TimeZoneId", SqlDbType.NVarChar, source.TimeZoneId);
        Add(command, "@DueAt", SqlDbType.DateTime2, (object?)dueAt?.UtcDateTime ?? DBNull.Value);
        Add(command, "@SourceRevision", SqlDbType.BigInt, source.Revision);
        Add(command, "@State", SqlDbType.VarChar, state);
    }

    private static void InsertIntent(SqlConnection connection, SqlTransaction transaction, ReminderSourceSnapshot source,
        ReminderRow reminder, string intent)
    {
        var logicalKey = $"reminder.{intent}:{reminder.Id:N}:{source.Revision}";
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO [operations].[Outbox] ([OwnerUserId], [LogicalKey], [Kind], [PayloadJson], [State], [NotBeforeAt])
            VALUES (@OwnerUserId, @LogicalKey, @Kind, @PayloadJson, 'Pending', SYSUTCDATETIME());
            """;
        Add(command, "@OwnerUserId", SqlDbType.UniqueIdentifier, source.OwnerUserId);
        Add(command, "@LogicalKey", SqlDbType.NVarChar, logicalKey);
        Add(command, "@Kind", SqlDbType.NVarChar, $"Reminders.{intent}");
        Add(command, "@PayloadJson", SqlDbType.NVarChar, JsonSerializer.Serialize(new
        {
            reminderId = reminder.Id,
            sourceType = source.SourceType,
            sourceId = source.SourceId,
            sourceRevision = source.Revision,
            dueAt = reminder.DueAt
        }));
        command.ExecuteNonQuery();
    }

    private static IReadOnlyList<Guid> ReadDueReminderIds(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id]
            FROM [calendar].[Reminder] WITH (READPAST)
            WHERE [State] = 'Pending' AND [DueAt] <= SYSUTCDATETIME()
            ORDER BY [DueAt], [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, DispatchBatchSize);
        using var reader = command.ExecuteReader();
        var ids = new List<Guid>();
        while (reader.Read()) ids.Add(reader.GetGuid(0));
        return ids;
    }

    private static bool IsDispatchAllowed(SqlConnection connection, SqlTransaction transaction, ReminderSourceSnapshot source)
    {
        var sourceModule = source.SourceType == ReminderSourceTypes.Task ? "FX12" : "FX13";
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH roots AS
            (
                SELECT [Id]
                FROM [platform].[Module]
                WHERE [Code] IN ('FX06', 'FX14', @SourceModule)
            ), dependency_chain AS
            (
                SELECT [Id] AS [ModuleId]
                FROM roots
                UNION ALL
                SELECT dependencyRow.[DependsOnModuleId]
                FROM dependency_chain chainRow
                INNER JOIN [platform].[ModuleDependency] dependencyRow ON dependencyRow.[ModuleId] = chainRow.[ModuleId]
                WHERE dependencyRow.[DependencyKind] = 'Hard'
            ), required_modules AS
            (
                SELECT DISTINCT [ModuleId]
                FROM dependency_chain
            )
            SELECT CASE
                WHEN (SELECT COUNT(*) FROM roots) <> 3
                     OR NOT EXISTS
                     (
                         SELECT 1 FROM [platform].[Permission]
                         WHERE [ActionKey] = 'reminders.schedule.dispatch' AND [EffectiveStatus] = 'Resolved'
                     )
                     OR EXISTS
                     (
                         SELECT 1
                         FROM required_modules requiredModule
                         INNER JOIN [platform].[Module] moduleRow ON moduleRow.[Id] = requiredModule.[ModuleId]
                         LEFT JOIN [platform].[UserModuleGrant] grantRow
                           ON grantRow.[ModuleId] = moduleRow.[Id] AND grantRow.[UserId] = @UserId
                         WHERE moduleRow.[State] <> 'Ready' OR moduleRow.[SystemEnabled] <> 1
                            OR moduleRow.[RegistrationEnabled] <> 1 OR COALESCE(grantRow.[Enabled], 0) <> 1
                     ) THEN 0
                ELSE 1
            END
            OPTION (MAXRECURSION 32);
            """;
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, source.OwnerUserId);
        Add(command, "@SourceModule", SqlDbType.VarChar, sourceModule);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static Guid EnsureNotificationProjection(SqlConnection connection, SqlTransaction transaction,
        ReminderRow reminder, ReminderSourceSnapshot source)
    {
        var logicalKey = $"reminder:{source.OwnerId:N}:{source.SourceType}:{source.SourceId:N}:{source.Revision}:{reminder.DueAt!.Value.UtcDateTime.Ticks}";
        var existing = ReadNotificationId(connection, transaction, logicalKey);
        if (existing is { } existingId) return existingId;

        var notificationId = Guid.NewGuid();
        using var notification = connection.CreateCommand();
        notification.Transaction = transaction;
        notification.CommandText = """
            INSERT INTO [notifications].[Notification] ([Id], [OwnerUserId], [LogicalKey], [Kind], [Title], [Body], [SourceRef])
            VALUES (@Id, @OwnerUserId, @LogicalKey, 'Reminder', @Title, @Body, @SourceRef);
            """;
        Add(notification, "@Id", SqlDbType.UniqueIdentifier, notificationId);
        Add(notification, "@OwnerUserId", SqlDbType.UniqueIdentifier, source.OwnerUserId);
        Add(notification, "@LogicalKey", SqlDbType.NVarChar, logicalKey);
        Add(notification, "@Title", SqlDbType.NVarChar, Truncate($"Reminder: {source.Title}", 200));
        Add(notification, "@Body", SqlDbType.NVarChar, $"Scheduled for {reminder.DueAt.Value.UtcDateTime:O} ({source.TimeZoneId}).");
        Add(notification, "@SourceRef", SqlDbType.NVarChar, $"{source.SourceType}:{source.SourceId:N}");
        notification.ExecuteNonQuery();

        using var delivery = connection.CreateCommand();
        delivery.Transaction = transaction;
        delivery.CommandText = """
            INSERT INTO [notifications].[Delivery] ([NotificationId], [Channel], [State], [Attempts], [LastErrorCode])
            VALUES (@NotificationId, 'InApp', 'Delivered', 1, NULL),
                   (@NotificationId, 'Email', 'NotApplicable', 0, 'ProviderUnavailable'),
                   (@NotificationId, 'BrowserPush', 'PermissionUnavailable', 0, 'ProviderUnavailable');
            """;
        Add(delivery, "@NotificationId", SqlDbType.UniqueIdentifier, notificationId);
        delivery.ExecuteNonQuery();

        using var outbox = connection.CreateCommand();
        outbox.Transaction = transaction;
        outbox.CommandText = """
            INSERT INTO [operations].[Outbox] ([OwnerUserId], [LogicalKey], [Kind], [PayloadJson], [State], [NotBeforeAt])
            VALUES (@OwnerUserId, @LogicalKey, 'Reminders.LocalDeliveryProjected', @PayloadJson, 'Completed', SYSUTCDATETIME());
            """;
        Add(outbox, "@OwnerUserId", SqlDbType.UniqueIdentifier, source.OwnerUserId);
        Add(outbox, "@LogicalKey", SqlDbType.NVarChar, $"reminder.dispatch:{notificationId:N}");
        Add(outbox, "@PayloadJson", SqlDbType.NVarChar, JsonSerializer.Serialize(new { reminderId = reminder.Id, notificationId }));
        outbox.ExecuteNonQuery();
        return notificationId;
    }

    private static Guid? ReadNotificationId(SqlConnection connection, SqlTransaction transaction, string logicalKey)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Id] FROM [notifications].[Notification] WITH (UPDLOCK, HOLDLOCK) WHERE [LogicalKey] = @LogicalKey;";
        Add(command, "@LogicalKey", SqlDbType.NVarChar, logicalKey);
        var value = command.ExecuteScalar();
        return value is Guid id ? id : null;
    }

    private static void UpdateReminderState(SqlConnection connection, SqlTransaction transaction, ReminderRow reminder,
        string state, Guid? notificationId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE [calendar].[Reminder]
            SET [State] = @State, [LastNotificationId] = @NotificationId, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [RowVersion] = @RowVersion;
            """;
        Add(command, "@State", SqlDbType.VarChar, state);
        Add(command, "@NotificationId", SqlDbType.UniqueIdentifier, (object?)notificationId ?? DBNull.Value);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, reminder.Id);
        Add(command, "@RowVersion", SqlDbType.Binary, reminder.RowVersion);
        if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Reminder revision changed while dispatching.");
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, Guid actorUserId, Guid ownerUserId,
        Guid reminderId, string action, string? traceId)
    {
        if (actorUserId == Guid.Empty || ownerUserId == Guid.Empty) return;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @Action, N'calendar.Reminder', @ReminderId, 'Succeeded', @TraceId);
            """;
        Add(command, "@ActorUserId", SqlDbType.UniqueIdentifier, actorUserId);
        Add(command, "@OwnerUserId", SqlDbType.UniqueIdentifier, ownerUserId);
        Add(command, "@Action", SqlDbType.NVarChar, action);
        Add(command, "@ReminderId", SqlDbType.UniqueIdentifier, reminderId);
        Add(command, "@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        return Failure<T>(claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress", 409,
            "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static bool TrySourceCapability(string? sourceType, out string moduleCode, out string readAction, out string writeAction)
    {
        if (sourceType == ReminderSourceTypes.Task)
        {
            moduleCode = "FX12";
            readAction = "tasks.task.read";
            writeAction = "tasks.task.update";
            return true;
        }
        if (sourceType == ReminderSourceTypes.CalendarEvent)
        {
            moduleCode = "FX13";
            readAction = "calendar.event.read";
            writeAction = "calendar.event.update";
            return true;
        }
        moduleCode = readAction = writeAction = string.Empty;
        return false;
    }

    private static bool TryNormalizeConfiguration(string? value, out string configType)
    {
        configType = value?.Trim() ?? string.Empty;
        return configType is ReminderConfigurationTypes.None or ReminderConfigurationTypes.BeforeStart15m or ReminderConfigurationTypes.Exact;
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private static bool TryValidateExactPrecondition(string? ifMatch, out byte[]? rowVersion)
    {
        rowVersion = null;
        if (string.IsNullOrWhiteSpace(ifMatch)) return true;
        if (!TryDecodeETag(ifMatch, out var bytes)) return false;
        rowVersion = bytes;
        return true;
    }

    private static bool TryDecodeETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            bytes = Convert.FromBase64String(value.Trim().Trim('"'));
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }

    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
    private static ReminderDispatchResult Add(ReminderDispatchResult left, ReminderDispatchResult right) => new(left.Dispatched + right.Dispatched, left.Missed + right.Missed, left.Canceled + right.Canceled, left.Deferred + right.Deferred);

    private static IdentityOperationResult<T> Rollback<T>(SqlTransaction transaction, IdentityOperationResult<T> result)
    {
        transaction.Rollback();
        return result;
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value)
    {
        command.Parameters.Add(name, type).Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Reminders are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> SourceUnavailable<T>() => Failure<T>("ResourceUnavailable", 404, "The reminder source is unavailable.");
    private static IdentityOperationResult<T> Validation<T>(string title) => Failure<T>("ValidationFailed", 422, title);
    private static IdentityOperationResult<T> LifecycleLocked<T>() => Failure<T>("LifecycleLocked", 409, "A terminal or projected source cannot be scheduled.");
    private static IdentityOperationResult<T> Revision<T>(string title) => Failure<T>("RevisionConflict", 412, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "Reminder persistence is unavailable.");
    private static IdentityOperationResult<T> SourcePrecondition<T>(string? value) => Failure<T>(string.IsNullOrWhiteSpace(value) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(value) ? 428 : 412, string.IsNullOrWhiteSpace(value) ? "A source ETag is required." : "The source ETag is invalid.");
    private static IdentityOperationResult<T> ReminderPrecondition<T>(string? value) => Failure<T>(string.IsNullOrWhiteSpace(value) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(value) ? 428 : 412, string.IsNullOrWhiteSpace(value) ? "If-Match is required for an existing reminder." : "The reminder ETag is invalid.");

    private sealed record ReminderSourceSnapshot(string SourceType, Guid SourceId, Guid OwnerId, string Title,
        DateTimeOffset StartAt, string TimeZoneId, long Revision, byte[] RowVersion, Guid OwnerUserId, bool CanConfigure);

    private sealed record ReminderRow(Guid Id, Guid OwnerId, string SourceType, Guid SourceId, string ConfigType,
        DateTimeOffset? ExactAt, string TimeZoneId, DateTimeOffset? DueAt, long SourceRevision, string State,
        Guid? LastNotificationId, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, byte[] RowVersion);
}
