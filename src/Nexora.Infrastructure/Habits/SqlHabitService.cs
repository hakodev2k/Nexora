using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Habits;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Habits;

/// <summary>
/// SQL source of truth for personal Habits. Check-ins are recorded against the
/// effective schedule for their saved local date; changing a timezone or a
/// future schedule never rewrites those historical local-date records.
/// </summary>
public sealed class SqlHabitService : IHabitService
{
    private const int DefaultDetailHistoryDays = 35;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlHabitService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<HabitPage> List(IdentityPrincipal actor, string? state = null, string? query = null, int? limit = null)
    {
        if (!HasCapability(actor, "habits.habit.read")) return ModuleUnavailable<HabitPage>();
        var normalizedState = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        if (normalizedState is not null && normalizedState is not (HabitStates.Active or HabitStates.Paused or HabitStates.Archived))
            return Failure<HabitPage>("ValidationFailed", 422, "Habit state is invalid.");
        var normalizedQuery = HabitPolicy.TrimOrNull(query);
        if (normalizedQuery is { Length: > HabitPolicy.MaximumTitleLength })
            return Failure<HabitPage>("ValidationFailed", 422, "Habit search text is too long.");

        using var connection = _connections.Create();
        connection.Open();
        var rows = ReadHabitRows(connection, null, actor.OwnerId, normalizedState, normalizedQuery, Math.Clamp(limit ?? 50, 1, 100), false);
        var records = rows.Select(row => ToRecord(connection, null, row)).ToArray();
        return IdentityOperationResult<HabitPage>.Success(new HabitPage(records, null));
    }

    public IdentityOperationResult<HabitDetail> Get(IdentityPrincipal actor, Guid habitId, DateOnly? from = null, DateOnly? to = null)
    {
        if (!HasCapability(actor, "habits.habit.read")) return ModuleUnavailable<HabitDetail>();
        using var connection = _connections.Create();
        connection.Open();
        var row = ReadHabit(connection, null, actor.OwnerId, habitId, false);
        if (row is null) return Missing<HabitDetail>();
        return IdentityOperationResult<HabitDetail>.Success(ReadDetail(connection, null, row, from, to));
    }

    public IdentityOperationResult<HabitDetail> Create(IdentityPrincipal actor, HabitCreateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapability(actor, "habits.habit.create")) return ModuleUnavailable<HabitDetail>();
        if (command.ReminderLocalTime is not null && !HasCapability(actor, "habits.habit.set_reminder"))
            return ModuleUnavailable<HabitDetail>();
        var validation = ValidateCreate(command, out var title, out var unit, out var zone);
        if (validation is not null) return Failure<HabitDetail>("ValidationFailed", 422, validation);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<HabitDetail>(connection, transaction, actor, "habits.habit.create", idempotencyKey,
            CanonicalCreate(command, title, unit), out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var today = LocalToday(zone!);
            if (command.EffectiveFrom < today)
                return Rollback(transaction, Failure<HabitDetail>("ScheduleHistoryLocked", 422, "A new Habit schedule cannot begin before its current local date."));
            var habitId = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [productivity].[Habit]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [Title], [Kind], [TargetCount], [Unit], [State], [TimeZoneId], [ReminderLocalTime])
                VALUES
                    (@Id, @OwnerId, @UserId, @UserId, @Title, @Kind, @TargetCount, @Unit, 'Active', @TimeZoneId, @ReminderLocalTime);
                """,
                ("@Id", SqlDbType.UniqueIdentifier, habitId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Title", SqlDbType.NVarChar, title, 100),
                ("@Kind", SqlDbType.VarChar, command.Kind, 16),
                ("@TargetCount", SqlDbType.Int, (object?)command.TargetCount ?? DBNull.Value, null),
                ("@Unit", SqlDbType.NVarChar, (object?)unit ?? DBNull.Value, 50),
                ("@TimeZoneId", SqlDbType.NVarChar, zone!.Id, 128),
                ("@ReminderLocalTime", SqlDbType.Time, command.ReminderLocalTime is { } time ? time.ToTimeSpan() : DBNull.Value, null));
            InsertSchedule(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom, command.WeekdayMask,
                command.Kind == HabitKinds.Count ? command.TargetCount : null, paused: false);
            var row = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (row is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            var created = ReadDetail(connection, transaction, row, null, null);
            WriteAudit(connection, transaction, actor, habitId, "habits.habit.create", "productivity.Habit", traceId);
            CompleteReceipt(connection, transaction, receipt, "HabitCreated");
            transaction.Commit();
            return IdentityOperationResult<HabitDetail>.Success(created, 201, "HabitCreated");
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    public IdentityOperationResult<HabitDetail> Update(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitUpdateCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapability(actor, "habits.habit.update")) return ModuleUnavailable<HabitDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        var validation = ValidateUpdate(command, out var title, out var unit, out var zone);
        if (validation is not null) return Failure<HabitDetail>("ValidationFailed", 422, validation);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Only active Habits can be edited."));
        var action = current.ReminderLocalTime == command.ReminderLocalTime
            ? "habits.habit.update"
            : "habits.habit.set_reminder";
        if (!HasCapability(actor, action)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var receiptFailure = CheckReceipt<HabitDetail>(connection, transaction, actor, action, idempotencyKey,
            $"habit:{habitId:N}|etag:{ifMatch}|title:{title}|unit:{unit}|zone:{zone!.Id}|reminder:{command.ReminderLocalTime}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var changed = Execute(connection, transaction, """
                UPDATE [productivity].[Habit]
                SET [Title] = @Title, [Unit] = @Unit, [TimeZoneId] = @TimeZoneId, [ReminderLocalTime] = @ReminderLocalTime,
                    [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                """,
                ("@Title", SqlDbType.NVarChar, title, 100),
                ("@Unit", SqlDbType.NVarChar, (object?)unit ?? DBNull.Value, 50),
                ("@TimeZoneId", SqlDbType.NVarChar, zone!.Id, 128),
                ("@ReminderLocalTime", SqlDbType.Time, command.ReminderLocalTime is { } time ? time.ToTimeSpan() : DBNull.Value, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@Id", SqlDbType.UniqueIdentifier, habitId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            if (changed != 1) return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, action, "productivity.Habit", traceId);
            CompleteReceipt(connection, transaction, receipt, action == "habits.habit.set_reminder" ? "HabitReminderUpdated" : "HabitUpdated");
            transaction.Commit();
            return IdentityOperationResult<HabitDetail>.Success(ReadDetail(connection, null, updated, null, null), 200, "HabitUpdated");
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    public IdentityOperationResult<HabitDetail> SetSchedule(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitScheduleCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapability(actor, "habits.habit.schedule")) return ModuleUnavailable<HabitDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        if (!HabitPolicy.IsValidWeekdayMask(command.WeekdayMask))
            return Failure<HabitDetail>("ValidationFailed", 422, "Select at least one weekday.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Only active Habits can change schedule."));
        var today = LocalToday(ResolveZone(current.TimeZoneId));
        if (command.EffectiveFrom <= today)
            return Rollback(transaction, Failure<HabitDetail>("ScheduleHistoryLocked", 422, "Schedule changes must begin after the current local date."));
        if (current.Kind == HabitKinds.Boolean && command.TargetCount is not null || current.Kind == HabitKinds.Count && command.TargetCount is not > 0)
            return Rollback(transaction, Failure<HabitDetail>("ValidationFailed", 422, "The schedule target does not match the Habit mode."));
        var receiptFailure = CheckReceipt<HabitDetail>(connection, transaction, actor, "habits.habit.schedule", idempotencyKey,
            $"habit:{habitId:N}|etag:{ifMatch}|effective:{command.EffectiveFrom:yyyy-MM-dd}|mask:{command.WeekdayMask}|target:{command.TargetCount}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            if (ScheduleStartsOnOrAfter(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom))
                return Rollback(transaction, Failure<HabitDetail>("ScheduleConflict", 409, "A schedule already exists on or after the selected effective date."));
            var prior = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom, true);
            if (prior is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            var closed = Execute(connection, transaction, """
                UPDATE [productivity].[HabitSchedule]
                SET [EffectiveUntil] = @EffectiveUntil, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [EffectiveUntil] IS NULL;
                """,
                ("@EffectiveUntil", SqlDbType.Date, ToDbDate(command.EffectiveFrom), null),
                ("@Id", SqlDbType.UniqueIdentifier, prior.Id, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null));
            if (closed != 1) return Rollback(transaction, Revision<HabitDetail>());
            InsertSchedule(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom, command.WeekdayMask,
                current.Kind == HabitKinds.Count ? command.TargetCount : null, paused: false);
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, current.State, current.PreArchiveState))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, "habits.habit.schedule", "productivity.HabitSchedule", traceId);
            CompleteReceipt(connection, transaction, receipt, "HabitScheduleUpdated");
            transaction.Commit();
            return IdentityOperationResult<HabitDetail>.Success(ReadDetail(connection, null, updated, null, null), 200, "HabitScheduleUpdated");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51023)
        {
            return Rollback(transaction, Failure<HabitDetail>("ScheduleConflict", 409, "The schedule conflicts with existing history."));
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    public IdentityOperationResult<HabitDetail> RecordCheckIn(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitCheckInCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapability(actor, "habits.checkin.record")) return ModuleUnavailable<HabitDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        var note = HabitPolicy.TrimOrNull(command.Note);
        if (command.Note is { Length: > HabitPolicy.MaximumCheckInNoteLength })
            return Failure<HabitDetail>("ValidationFailed", 422, "Check-in note must be at most 1000 characters.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Paused or archived Habits do not accept check-ins."));
        var today = LocalToday(ResolveZone(current.TimeZoneId));
        if (command.LocalDate > today) return Rollback(transaction, Failure<HabitDetail>("FutureCheckInDenied", 422, "Future local dates cannot be checked in."));
        var schedule = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, command.LocalDate, true);
        if (schedule is null || schedule.Paused || !HabitPolicy.IsScheduled(command.LocalDate, schedule.WeekdayMask))
            return Rollback(transaction, Failure<HabitDetail>("CheckInNotScheduled", 409, "This local date is not an active scheduled Habit day."));
        if (!IsValidCheckInCount(current.Kind, command.Count))
            return Rollback(transaction, Failure<HabitDetail>("ValidationFailed", 422, "Check-in value does not match the Habit mode."));
        var existing = ReadCheckIn(connection, transaction, actor.OwnerId, habitId, command.LocalDate, true);
        var action = existing is null ? "habits.checkin.record" : "habits.checkin.correct";
        if (existing is not null && !HasCapability(actor, action)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var receiptFailure = CheckReceipt<HabitDetail>(connection, transaction, actor, action, idempotencyKey,
            $"habit:{habitId:N}|etag:{ifMatch}|date:{command.LocalDate:yyyy-MM-dd}|count:{command.Count}|note:{note}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            if (existing is null)
            {
                Execute(connection, transaction, """
                    INSERT INTO [productivity].[HabitCheckIn] ([Id], [OwnerId], [HabitId], [ScheduleId], [LocalDate], [Count], [Note])
                    VALUES (@Id, @OwnerId, @HabitId, @ScheduleId, @LocalDate, @Count, @Note);
                    """,
                    ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@HabitId", SqlDbType.UniqueIdentifier, habitId, null),
                    ("@ScheduleId", SqlDbType.UniqueIdentifier, schedule.Id, null),
                    ("@LocalDate", SqlDbType.Date, ToDbDate(command.LocalDate), null),
                    ("@Count", SqlDbType.Int, command.Count, null),
                    ("@Note", SqlDbType.NVarChar, (object?)note ?? DBNull.Value, 1000));
            }
            else
            {
                Execute(connection, transaction, """
                    UPDATE [productivity].[HabitCheckIn]
                    SET [Count] = @Count, [Note] = @Note, [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                    """,
                    ("@Count", SqlDbType.Int, command.Count, null),
                    ("@Note", SqlDbType.NVarChar, (object?)note ?? DBNull.Value, 1000),
                    ("@Id", SqlDbType.UniqueIdentifier, existing.Id, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@RowVersion", SqlDbType.Binary, DecodeETag(existing.ETag), 8));
            }
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, current.State, current.PreArchiveState))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, action, "productivity.HabitCheckIn", traceId);
            CompleteReceipt(connection, transaction, receipt, existing is null ? "HabitCheckInRecorded" : "HabitCheckInCorrected");
            transaction.Commit();
            return IdentityOperationResult<HabitDetail>.Success(ReadDetail(connection, null, updated, null, null), 200,
                existing is null ? "HabitCheckInRecorded" : "HabitCheckInCorrected");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Rollback(transaction, Failure<HabitDetail>("CheckInConflict", 409, "A check-in already exists for this local date."));
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    public IdentityOperationResult<HabitDetail> Transition(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        string state, string? idempotencyKey = null, string? traceId = null)
    {
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        var requested = state?.Trim() ?? string.Empty;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (!TryTransition(current, requested, out var nextState, out var action))
            return Rollback(transaction, LifecycleLocked<HabitDetail>("The requested Habit lifecycle transition is not allowed."));
        if (!HasCapability(actor, action)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var receiptFailure = CheckReceipt<HabitDetail>(connection, transaction, actor, action, idempotencyKey,
            $"habit:{habitId:N}|etag:{ifMatch}|state:{requested}", out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var today = LocalToday(ResolveZone(current.TimeZoneId));
            var schedule = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, today, true);
            if (schedule is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            var shouldPauseSchedule = nextState != HabitStates.Active;
            ReplaceCurrentSchedulePause(connection, transaction, actor.OwnerId, habitId, today, schedule, shouldPauseSchedule);
            var preArchiveState = nextState == HabitStates.Archived ? current.State : current.PreArchiveState;
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, nextState,
                    nextState == HabitStates.Archived ? preArchiveState : null))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, action, "productivity.Habit", traceId);
            CompleteReceipt(connection, transaction, receipt, "HabitTransitioned");
            transaction.Commit();
            return IdentityOperationResult<HabitDetail>.Success(ReadDetail(connection, null, updated, null, null), 200, "HabitTransitioned");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627 or 51023)
        {
            return Rollback(transaction, Failure<HabitDetail>("ScheduleConflict", 409, "Habit lifecycle transition conflicts with the effective schedule."));
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    private bool HasCapability(IdentityPrincipal actor, string action) => _capabilities.IsAllowed(actor, "FX17", action);

    private static HabitDetail ReadDetail(SqlConnection connection, SqlTransaction? transaction, HabitRow row, DateOnly? from, DateOnly? to)
    {
        var zone = ResolveZone(row.TimeZoneId);
        var today = LocalToday(zone);
        var lower = from ?? today.AddDays(-DefaultDetailHistoryDays);
        var upper = to ?? today;
        if (upper < lower) (lower, upper) = (upper, lower);
        return new HabitDetail(ToRecord(connection, transaction, row), ReadSchedules(connection, transaction, row.OwnerId, row.Id, false),
            ReadCheckIns(connection, transaction, row.OwnerId, row.Id, lower, upper, false));
    }

    private static HabitRecord ToRecord(SqlConnection connection, SqlTransaction? transaction, HabitRow row)
    {
        var today = LocalToday(ResolveZone(row.TimeZoneId));
        var schedule = ReadScheduleAt(connection, transaction, row.OwnerId, row.Id, today, false);
        return new HabitRecord(row.Id, row.Title, row.Kind, row.TargetCount, row.Unit, row.State, row.TimeZoneId,
            row.ReminderLocalTime, schedule, ComputeCurrentStreak(connection, transaction, row, today), row.CreatedAt, row.UpdatedAt, row.ETag);
    }

    private static HabitRow? ReadHabit(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid habitId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [OwnerId], [Title], [Kind], [TargetCount], [Unit], [State], [TimeZoneId], [ReminderLocalTime],
                   [PreArchiveState], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [productivity].[Habit] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [Id] = @HabitId;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadHabit(reader) : null;
    }

    private static List<HabitRow> ReadHabitRows(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        string? state, string? query, int limit, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT TOP (@Limit) [Id], [OwnerId], [Title], [Kind], [TargetCount], [Unit], [State], [TimeZoneId], [ReminderLocalTime],
                   [PreArchiveState], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [productivity].[Habit] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId
              AND (@State IS NULL OR [State] = @State)
              AND (@Query IS NULL OR [Title] LIKE @QueryLike)
            ORDER BY CASE [State] WHEN 'Active' THEN 0 WHEN 'Paused' THEN 1 ELSE 2 END, [Title], [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, limit);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@State", SqlDbType.VarChar, (object?)state ?? DBNull.Value, 16);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)query ?? DBNull.Value, 100);
        Add(command, "@QueryLike", SqlDbType.NVarChar, query is null ? DBNull.Value : $"%{query}%", 102);
        using var reader = command.ExecuteReader();
        var rows = new List<HabitRow>();
        while (reader.Read()) rows.Add(ReadHabit(reader));
        return rows;
    }

    private static HabitScheduleRecord? ReadScheduleAt(SqlConnection connection, SqlTransaction? transaction, Guid ownerId,
        Guid habitId, DateOnly localDate, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT TOP (1) [Id], [EffectiveFrom], [EffectiveUntil], [WeekdayMask], [TargetCount], [Paused], [RowVersion]
            FROM [productivity].[HabitSchedule] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [HabitId] = @HabitId
              AND [EffectiveFrom] <= @LocalDate
              AND ([EffectiveUntil] IS NULL OR [EffectiveUntil] > @LocalDate)
            ORDER BY [EffectiveFrom] DESC;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        Add(command, "@LocalDate", SqlDbType.Date, ToDbDate(localDate));
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadSchedule(reader) : null;
    }

    private static List<HabitScheduleRecord> ReadSchedules(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid habitId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [EffectiveFrom], [EffectiveUntil], [WeekdayMask], [TargetCount], [Paused], [RowVersion]
            FROM [productivity].[HabitSchedule] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [HabitId] = @HabitId
            ORDER BY [EffectiveFrom] DESC, [Id] DESC;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        using var reader = command.ExecuteReader();
        var schedules = new List<HabitScheduleRecord>();
        while (reader.Read()) schedules.Add(ReadSchedule(reader));
        return schedules;
    }

    private static HabitCheckInRecord? ReadCheckIn(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid habitId, DateOnly localDate, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [LocalDate], [Count], [Note], [ScheduleId], [UpdatedAt], [RowVersion]
            FROM [productivity].[HabitCheckIn] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [HabitId] = @HabitId AND [LocalDate] = @LocalDate;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        Add(command, "@LocalDate", SqlDbType.Date, ToDbDate(localDate));
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadCheckIn(reader) : null;
    }

    private static List<HabitCheckInRecord> ReadCheckIns(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid habitId, DateOnly from, DateOnly to, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [LocalDate], [Count], [Note], [ScheduleId], [UpdatedAt], [RowVersion]
            FROM [productivity].[HabitCheckIn] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE [OwnerId] = @OwnerId AND [HabitId] = @HabitId AND [LocalDate] >= @From AND [LocalDate] <= @To
            ORDER BY [LocalDate] DESC, [Id] DESC;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        Add(command, "@From", SqlDbType.Date, ToDbDate(from));
        Add(command, "@To", SqlDbType.Date, ToDbDate(to));
        using var reader = command.ExecuteReader();
        var checkIns = new List<HabitCheckInRecord>();
        while (reader.Read()) checkIns.Add(ReadCheckIn(reader));
        return checkIns;
    }

    private static int ComputeCurrentStreak(SqlConnection connection, SqlTransaction? transaction, HabitRow row, DateOnly today)
    {
        var schedules = ReadSchedules(connection, transaction, row.OwnerId, row.Id, false);
        if (schedules.Count == 0) return 0;
        var firstDate = schedules.Min(schedule => schedule.EffectiveFrom);
        var checkIns = ReadCheckIns(connection, transaction, row.OwnerId, row.Id, firstDate, today, false)
            .ToDictionary(checkIn => checkIn.LocalDate, checkIn => checkIn);
        var streak = 0;
        for (var date = today; date >= firstDate; date = date.AddDays(-1))
        {
            var schedule = schedules.FirstOrDefault(candidate => candidate.EffectiveFrom <= date &&
                (candidate.EffectiveUntil is null || candidate.EffectiveUntil > date));
            if (schedule is null || schedule.Paused || !HabitPolicy.IsScheduled(date, schedule.WeekdayMask)) continue;
            if (!checkIns.TryGetValue(date, out var checkIn) || !HabitPolicy.MeetsTarget(row.Kind, checkIn.Count, schedule.TargetCount)) break;
            streak++;
        }
        return streak;
    }

    private static void InsertSchedule(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid habitId,
        DateOnly effectiveFrom, byte weekdayMask, int? targetCount, bool paused)
    {
        Execute(connection, transaction, """
            INSERT INTO [productivity].[HabitSchedule]
                ([Id], [OwnerId], [HabitId], [EffectiveFrom], [WeekdayMask], [TargetCount], [Paused])
            VALUES (@Id, @OwnerId, @HabitId, @EffectiveFrom, @WeekdayMask, @TargetCount, @Paused);
            """,
            ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null),
            ("@HabitId", SqlDbType.UniqueIdentifier, habitId, null),
            ("@EffectiveFrom", SqlDbType.Date, ToDbDate(effectiveFrom), null),
            ("@WeekdayMask", SqlDbType.TinyInt, weekdayMask, null),
            ("@TargetCount", SqlDbType.Int, (object?)targetCount ?? DBNull.Value, null),
            ("@Paused", SqlDbType.Bit, paused, null));
    }

    private static bool ScheduleStartsOnOrAfter(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid habitId, DateOnly date)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [productivity].[HabitSchedule] WITH (UPDLOCK, HOLDLOCK) WHERE [OwnerId] = @OwnerId AND [HabitId] = @HabitId AND [EffectiveFrom] >= @Date) THEN 1 ELSE 0 END;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HabitId", SqlDbType.UniqueIdentifier, habitId);
        Add(command, "@Date", SqlDbType.Date, ToDbDate(date));
        return Convert.ToInt32(command.ExecuteScalar()) == 1;
    }

    private static void ReplaceCurrentSchedulePause(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid habitId, DateOnly effectiveDate, HabitScheduleRecord current, bool paused)
    {
        if (current.EffectiveFrom == effectiveDate)
        {
            Execute(connection, transaction, """
                UPDATE [productivity].[HabitSchedule]
                SET [Paused] = @Paused, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId;
                """,
                ("@Paused", SqlDbType.Bit, paused, null),
                ("@Id", SqlDbType.UniqueIdentifier, current.Id, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null));
            return;
        }

        Execute(connection, transaction, """
            UPDATE [productivity].[HabitSchedule]
            SET [EffectiveUntil] = @EffectiveUntil, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [EffectiveUntil] IS NULL;
            """,
            ("@EffectiveUntil", SqlDbType.Date, ToDbDate(effectiveDate), null),
            ("@Id", SqlDbType.UniqueIdentifier, current.Id, null),
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null));
        InsertSchedule(connection, transaction, ownerId, habitId, effectiveDate, current.WeekdayMask, current.TargetCount, paused);
    }

    private static bool TouchHabit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid habitId,
        byte[] expectedVersion, string state, string? preArchiveState)
    {
        var changed = Execute(connection, transaction, """
            UPDATE [productivity].[Habit]
            SET [State] = @State, [PreArchiveState] = @PreArchiveState, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
            """,
            ("@State", SqlDbType.VarChar, state, 16),
            ("@PreArchiveState", SqlDbType.VarChar, (object?)preArchiveState ?? DBNull.Value, 16),
            ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
            ("@Id", SqlDbType.UniqueIdentifier, habitId, null),
            ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
            ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
        return changed == 1;
    }

    private static bool TryTransition(HabitRow current, string requested, out string nextState, out string action)
    {
        nextState = current.State;
        action = string.Empty;
        if (current.State == HabitStates.Active && requested == HabitStates.Paused)
        {
            nextState = HabitStates.Paused; action = "habits.habit.pause"; return true;
        }
        if (current.State == HabitStates.Paused && requested == HabitStates.Active)
        {
            nextState = HabitStates.Active; action = "habits.habit.resume"; return true;
        }
        if ((current.State is HabitStates.Active or HabitStates.Paused) && requested == HabitStates.Archived)
        {
            nextState = HabitStates.Archived; action = "habits.habit.archive"; return true;
        }
        if (current.State == HabitStates.Archived && requested == "Unarchive" &&
            (current.PreArchiveState is HabitStates.Active or HabitStates.Paused))
        {
            nextState = current.PreArchiveState!; action = "habits.habit.unarchive"; return true;
        }
        return false;
    }

    private static string? ValidateCreate(HabitCreateCommand command, out string title, out string? unit, out TimeZoneInfo? zone)
    {
        title = command.Title?.Trim() ?? string.Empty;
        unit = HabitPolicy.TrimOrNull(command.Unit);
        zone = null;
        if (title.Length is < 1 or > HabitPolicy.MaximumTitleLength) return "Habit title is required and must be at most 100 characters.";
        if (unit is { Length: > HabitPolicy.MaximumUnitLength }) return "Habit unit must be at most 50 characters.";
        if (command.Kind is not (HabitKinds.Boolean or HabitKinds.Count)) return "Habit mode must be Boolean or Count.";
        if (command.Kind == HabitKinds.Boolean && command.TargetCount is not null || command.Kind == HabitKinds.Count && command.TargetCount is not > 0)
            return "Habit target does not match the selected mode.";
        if (!HabitPolicy.IsValidWeekdayMask(command.WeekdayMask)) return "Select at least one weekday.";
        if (!TryResolveZone(command.TimeZoneId, out zone)) return "Habit timezone is invalid.";
        return null;
    }

    private static string? ValidateUpdate(HabitUpdateCommand command, out string title, out string? unit, out TimeZoneInfo? zone)
    {
        title = command.Title?.Trim() ?? string.Empty;
        unit = HabitPolicy.TrimOrNull(command.Unit);
        zone = null;
        if (title.Length is < 1 or > HabitPolicy.MaximumTitleLength) return "Habit title is required and must be at most 100 characters.";
        if (unit is { Length: > HabitPolicy.MaximumUnitLength }) return "Habit unit must be at most 50 characters.";
        return TryResolveZone(command.TimeZoneId, out zone) ? null : "Habit timezone is invalid.";
    }

    private static bool IsValidCheckInCount(string kind, int count) =>
        count >= 0 && (kind != HabitKinds.Boolean || count is 0 or 1);

    private static string CanonicalCreate(HabitCreateCommand command, string title, string? unit) =>
        $"title:{title}|kind:{command.Kind}|target:{command.TargetCount}|unit:{unit}|from:{command.EffectiveFrom:yyyy-MM-dd}|mask:{command.WeekdayMask}|zone:{command.TimeZoneId.Trim()}|reminder:{command.ReminderLocalTime}";

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        return Failure<T>(claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress", 409,
            "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string targetType, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, @TargetType, @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
        ("@ActionKey", SqlDbType.NVarChar, actionKey, 160),
        ("@TargetType", SqlDbType.NVarChar, targetType, 100),
        ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 128));

    private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value, int? Size)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value, parameter.Size);
        return command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is { } explicitSize ? command.Parameters.Add(name, type, explicitSize) : command.Parameters.Add(name, type);
        parameter.Value = value;
    }

    private static HabitRow ReadHabit(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetInt32(4),
        reader.IsDBNull(5) ? null : reader.GetString(5), reader.GetString(6), reader.GetString(7),
        reader.IsDBNull(8) ? null : TimeOnly.FromTimeSpan(reader.GetTimeSpan(8)), reader.IsDBNull(9) ? null : reader.GetString(9),
        ToOffset(reader.GetDateTime(10)), ToOffset(reader.GetDateTime(11)), EncodeETag(reader.GetFieldValue<byte[]>(12)));

    private static HabitScheduleRecord ReadSchedule(SqlDataReader reader) => new(
        reader.GetGuid(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.IsDBNull(2) ? null : DateOnly.FromDateTime(reader.GetDateTime(2)),
        reader.GetByte(3), reader.IsDBNull(4) ? null : reader.GetInt32(4), reader.GetBoolean(5), EncodeETag(reader.GetFieldValue<byte[]>(6)));

    private static HabitCheckInRecord ReadCheckIn(SqlDataReader reader) => new(
        reader.GetGuid(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetGuid(4), ToOffset(reader.GetDateTime(5)), EncodeETag(reader.GetFieldValue<byte[]>(6)));

    private static IdentityOperationResult<T> Rollback<T>(SqlTransaction transaction, IdentityOperationResult<T> result)
    {
        transaction.Rollback();
        return result;
    }

    private static bool TryResolveZone(string? value, out TimeZoneInfo? zone)
    {
        zone = null;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 128) return false;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(value.Trim()); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
    private static TimeZoneInfo ResolveZone(string value) => TryResolveZone(value, out var zone) && zone is not null
        ? zone : throw new InvalidOperationException("Stored Habit timezone is invalid.");
    private static DateOnly LocalToday(TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);
    private static object ToDbDate(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool MatchesETag(byte[] expected, string actual) => CryptographicOperations.FixedTimeEquals(expected, DecodeETag(actual));
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
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Habits are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Habit is unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Habit revision changed.");
    private static IdentityOperationResult<T> LifecycleLocked<T>(string title) => Failure<T>("LifecycleLocked", 409, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "Habit persistence is unavailable.");

    private sealed record HabitRow(Guid Id, Guid OwnerId, string Title, string Kind, int? TargetCount, string? Unit,
        string State, string TimeZoneId, TimeOnly? ReminderLocalTime, string? PreArchiveState,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
}
