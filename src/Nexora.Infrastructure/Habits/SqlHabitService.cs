using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
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
    private readonly TimeProvider _clock;

    public SqlHabitService(SqlConnectionFactory connections, string? idempotencySecret = null, TimeProvider? clock = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
        _clock = clock ?? TimeProvider.System;
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
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CanReadDetail(connection, transaction, actor)) return ModuleUnavailable<HabitPage>();
        var now = _clock.GetUtcNow();
        var rows = ReadHabitRows(connection, transaction, actor.OwnerId, normalizedState, normalizedQuery, Math.Clamp(limit ?? 50, 1, 100), false);
        var records = rows.Select(row => ToRecord(connection, transaction, row, now)).ToArray();
        if (!CanReadDetail(connection, transaction, actor)) return ModuleUnavailable<HabitPage>();
        transaction.Commit();
        return IdentityOperationResult<HabitPage>.Success(new HabitPage(records, null));
    }

    public IdentityOperationResult<HabitDetail> Get(IdentityPrincipal actor, Guid habitId, DateOnly? from = null, DateOnly? to = null)
    {
        if (!HasCapability(actor, "habits.habit.read")) return ModuleUnavailable<HabitDetail>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!CanReadDetail(connection, transaction, actor)) return ModuleUnavailable<HabitDetail>();
        var row = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
        if (row is null) return Missing<HabitDetail>();
        var now = _clock.GetUtcNow();
        var today = LocalToday(ResolveZone(row.TimeZoneId), now);
        var lower = from ?? today.AddDays(-DefaultDetailHistoryDays);
        var upper = to ?? today;
        if (upper < lower || upper.DayNumber - lower.DayNumber > 366)
            return Failure<HabitDetail>("ValidationFailed", 422, "Habit history range must be ordered and at most 366 days.");
        var detail = ReadDetail(connection, transaction, row, lower, upper, now);
        if (!CanReadDetail(connection, transaction, actor)) return ModuleUnavailable<HabitDetail>();
        transaction.Commit();
        return IdentityOperationResult<HabitDetail>.Success(detail);
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
        var actions = command.ReminderLocalTime is null ? new[] { "habits.habit.create" }
            : new[] { "habits.habit.create", "habits.habit.set_reminder" };
        var now = _clock.GetUtcNow();
        var receiptFailure = CheckReceipt(connection, transaction, actor, "habits.habit.create", idempotencyKey,
            Intent(command with { Title = title, Unit = unit, TimeZoneId = zone!.Id }),
            CanonicalCreate(command, title, unit), null, actions, out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        try
        {
            var today = LocalToday(zone!, now);
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
            InsertSchedule(connection, transaction, actor.OwnerId, actor.UserId, habitId, command.EffectiveFrom, command.WeekdayMask,
                command.Kind == HabitKinds.Count ? command.TargetCount : null, paused: false);
            var row = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (row is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, "habits.habit.create", "productivity.Habit", traceId);
            return CommitResource(connection, transaction, actor, receipt, row, "habits.habit.create", actions, "HabitCreated", 201, now);
        }
        catch (SqlException)
        {
            return Rollback(transaction, PersistenceFailure<HabitDetail>());
        }
    }

    public IdentityOperationResult<HabitDetail> Update(IdentityPrincipal actor, Guid habitId, string? ifMatch,
        HabitUpdateCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!HasCapability(actor, "habits.habit.update") && !HasCapability(actor, "habits.habit.set_reminder"))
            return ModuleUnavailable<HabitDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        var validation = ValidateUpdate(command, out var title, out var unit, out var zone);
        if (validation is not null) return Failure<HabitDetail>("ValidationFailed", 422, validation);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var now = _clock.GetUtcNow();
        var canonical = Intent(new { habitId, ifMatch, title, unit, zone = zone!.Id, command.ReminderLocalTime });
        var legacy = $"habit:{habitId:N}|etag:{ifMatch}|title:{title}|unit:{unit}|zone:{zone!.Id}|reminder:{command.ReminderLocalTime}";
        foreach (var candidate in new[] { "habits.habit.update", "habits.habit.set_reminder" })
        {
            var previous = InspectReceipt(connection, transaction, actor, candidate, idempotencyKey, canonical, legacy);
            if (previous is not null) return Rollback(transaction, ReceiptOutcome(connection, transaction, actor, candidate, habitId, previous.Value));
        }
        if (!CurrentAuthority(connection, transaction, actor, "habits.habit.update")
            && !CurrentAuthority(connection, transaction, actor, "habits.habit.set_reminder"))
            return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Only active Habits can be edited."));
        var coreFieldsChanged = !string.Equals(current.Title, title, StringComparison.Ordinal) ||
            !string.Equals(current.Unit, unit, StringComparison.Ordinal) ||
            !string.Equals(current.TimeZoneId, zone!.Id, StringComparison.Ordinal);
        var reminderChanged = current.ReminderLocalTime != command.ReminderLocalTime;
        var action = reminderChanged && !coreFieldsChanged ? "habits.habit.set_reminder" : "habits.habit.update";
        var actions = action == "habits.habit.set_reminder" ? new[] { action }
            : reminderChanged ? new[] { action, "habits.habit.read", "habits.habit.set_reminder" }
            : new[] { action, "habits.habit.read" };
        var receiptFailure = CheckReceipt(connection, transaction, actor, action, idempotencyKey,
            canonical, legacy, habitId, actions, out var receipt);
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
            return CommitResource(connection, transaction, actor, receipt, updated, action, actions,
                action == "habits.habit.set_reminder" ? "HabitReminderUpdated" : "HabitUpdated", 200, now);
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
        var now = _clock.GetUtcNow();
        var receiptFailure = CheckReceipt(connection, transaction, actor, "habits.habit.schedule", idempotencyKey,
            Intent(new { habitId, ifMatch, command.EffectiveFrom, command.WeekdayMask, command.TargetCount }),
            $"habit:{habitId:N}|etag:{ifMatch}|effective:{command.EffectiveFrom:yyyy-MM-dd}|mask:{command.WeekdayMask}|target:{command.TargetCount}",
            habitId, ["habits.habit.schedule"], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Only active Habits can change schedule."));
        var today = LocalToday(ResolveZone(current.TimeZoneId), now);
        if (command.EffectiveFrom <= today)
            return Rollback(transaction, Failure<HabitDetail>("ScheduleHistoryLocked", 422, "Schedule changes must begin after the current local date."));
        if ((current.Kind == HabitKinds.Boolean && command.TargetCount is not null) ||
            (current.Kind == HabitKinds.Count && command.TargetCount is not > 0))
            return Rollback(transaction, Failure<HabitDetail>("ValidationFailed", 422, "The schedule target does not match the Habit mode."));

        try
        {
            if (ScheduleStartsOnOrAfter(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom))
                return Rollback(transaction, Failure<HabitDetail>("ScheduleConflict", 409, "A schedule already exists on or after the selected effective date."));
            var prior = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, command.EffectiveFrom, true);
            if (prior is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            var closed = Execute(connection, transaction, """
                UPDATE [productivity].[HabitSchedule]
                SET [EffectiveUntil] = @EffectiveUntil, [UpdatedByUserId] = @UserId,
                    [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [EffectiveUntil] IS NULL AND [RowVersion] = @RowVersion;
                """,
                ("@EffectiveUntil", SqlDbType.Date, ToDbDate(command.EffectiveFrom), null),
                ("@Id", SqlDbType.UniqueIdentifier, prior.Id, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@RowVersion", SqlDbType.Binary, DecodeETag(prior.ETag), 8));
            if (closed != 1) return Rollback(transaction, Revision<HabitDetail>());
            InsertSchedule(connection, transaction, actor.OwnerId, actor.UserId, habitId, command.EffectiveFrom, command.WeekdayMask,
                current.Kind == HabitKinds.Count ? command.TargetCount : null, paused: false);
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, current.State, current.PreArchiveState))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, "habits.habit.schedule", "productivity.HabitSchedule", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, "habits.habit.schedule",
                ["habits.habit.schedule"], "HabitScheduleUpdated", 200, now);
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
        if (!HasCapability(actor, "habits.checkin.record") && !HasCapability(actor, "habits.checkin.correct"))
            return ModuleUnavailable<HabitDetail>();
        if (!TryETag(ifMatch, out var expectedVersion)) return Precondition<HabitDetail>();
        var note = HabitPolicy.TrimOrNull(command.Note);
        if (command.Note is { Length: > HabitPolicy.MaximumCheckInNoteLength })
            return Failure<HabitDetail>("ValidationFailed", 422, "Check-in note must be at most 1000 characters.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var now = _clock.GetUtcNow();
        if (!CurrentAuthority(connection, transaction, actor, "habits.checkin.record")
            && !CurrentAuthority(connection, transaction, actor, "habits.checkin.correct"))
            return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var receiptFailure = CheckReceipt(connection, transaction, actor, HabitReceiptOperations.CheckIn, idempotencyKey,
            Intent(new { habitId, ifMatch, command.LocalDate, command.Count, note }),
            $"habit:{habitId:N}|etag:{ifMatch}|date:{command.LocalDate:yyyy-MM-dd}|count:{command.Count}|note:{note}",
            habitId, [], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);

        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (current.State != HabitStates.Active) return Rollback(transaction, LifecycleLocked<HabitDetail>("Paused or archived Habits do not accept check-ins."));
        var today = LocalToday(ResolveZone(current.TimeZoneId), now);
        if (command.LocalDate > today) return Rollback(transaction, Failure<HabitDetail>("FutureCheckInDenied", 422, "Future local dates cannot be checked in."));
        var schedule = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, command.LocalDate, true);
        if (schedule is null || schedule.Paused || !HabitPolicy.IsScheduled(command.LocalDate, schedule.WeekdayMask))
            return Rollback(transaction, Failure<HabitDetail>("CheckInNotScheduled", 409, "This local date is not an active scheduled Habit day."));
        if (!IsValidCheckInCount(current.Kind, command.Count))
            return Rollback(transaction, Failure<HabitDetail>("ValidationFailed", 422, "Check-in value does not match the Habit mode."));
        var existing = ReadCheckIn(connection, transaction, actor.OwnerId, habitId, command.LocalDate, true);
        var action = existing is null ? "habits.checkin.record" : "habits.checkin.correct";
        if (!CurrentAuthority(connection, transaction, actor, action)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());

        try
        {
            if (existing is null)
            {
                var inserted = Execute(connection, transaction, """
                    INSERT INTO [productivity].[HabitCheckIn]
                        ([Id], [OwnerId], [HabitId], [ScheduleId], [CreatedByUserId], [UpdatedByUserId], [LocalDate], [Count], [Note])
                    VALUES (@Id, @OwnerId, @HabitId, @ScheduleId, @UserId, @UserId, @LocalDate, @Count, @Note);
                    """,
                    ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@HabitId", SqlDbType.UniqueIdentifier, habitId, null),
                    ("@ScheduleId", SqlDbType.UniqueIdentifier, schedule.Id, null),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@LocalDate", SqlDbType.Date, ToDbDate(command.LocalDate), null),
                    ("@Count", SqlDbType.Int, command.Count, null),
                    ("@Note", SqlDbType.NVarChar, (object?)note ?? DBNull.Value, 1000));
                if (inserted != 1) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            }
            else
            {
                var changed = Execute(connection, transaction, """
                    UPDATE [productivity].[HabitCheckIn]
                    SET [Count] = @Count, [Note] = @Note, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                    WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;
                    """,
                    ("@Count", SqlDbType.Int, command.Count, null),
                    ("@Note", SqlDbType.NVarChar, (object?)note ?? DBNull.Value, 1000),
                    ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                    ("@Id", SqlDbType.UniqueIdentifier, existing.Id, null),
                    ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                    ("@RowVersion", SqlDbType.Binary, DecodeETag(existing.ETag), 8));
                if (changed != 1) return Rollback(transaction, Revision<HabitDetail>());
            }
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, current.State, current.PreArchiveState))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, action, "productivity.HabitCheckIn", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, HabitReceiptOperations.CheckIn,
                [action], existing is null ? "HabitCheckInRecorded" : "HabitCheckInCorrected", 200, now);
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
        var requestedAction = requested switch
        {
            HabitStates.Paused => "habits.habit.pause", HabitStates.Active => "habits.habit.resume",
            HabitStates.Archived => "habits.habit.archive", "Unarchive" => "habits.habit.unarchive", _ => null
        };
        if (requestedAction is null) return Failure<HabitDetail>("ValidationFailed", 422, "Habit transition is invalid.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var now = _clock.GetUtcNow();
        var receiptFailure = CheckReceipt(connection, transaction, actor, requestedAction, idempotencyKey,
            Intent(new { habitId, ifMatch, state = requested }),
            $"habit:{habitId:N}|etag:{ifMatch}|state:{requested}", habitId, [requestedAction], out var receipt);
        if (receiptFailure is not null) return Rollback(transaction, receiptFailure);
        var current = ReadHabit(connection, transaction, actor.OwnerId, habitId, true);
        if (current is null) return Rollback(transaction, Missing<HabitDetail>());
        if (!MatchesETag(expectedVersion, current.ETag)) return Rollback(transaction, Revision<HabitDetail>());
        if (!TryTransition(current, requested, out var nextState, out var action))
            return Rollback(transaction, LifecycleLocked<HabitDetail>("The requested Habit lifecycle transition is not allowed."));
        if (action != requestedAction || !CurrentAuthority(connection, transaction, actor, action))
            return Rollback(transaction, ModuleUnavailable<HabitDetail>());

        try
        {
            var today = LocalToday(ResolveZone(current.TimeZoneId), now);
            var schedule = ReadScheduleAt(connection, transaction, actor.OwnerId, habitId, today, true);
            if (schedule is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            var shouldPauseSchedule = nextState != HabitStates.Active;
            if (!ReplaceCurrentSchedulePause(connection, transaction, actor.OwnerId, actor.UserId, habitId, today, schedule, shouldPauseSchedule))
                return Rollback(transaction, Revision<HabitDetail>());
            var preArchiveState = nextState == HabitStates.Archived ? current.State : current.PreArchiveState;
            if (!TouchHabit(connection, transaction, actor, habitId, expectedVersion, nextState,
                    nextState == HabitStates.Archived ? preArchiveState : null))
                return Rollback(transaction, Revision<HabitDetail>());
            var updated = ReadHabit(connection, transaction, actor.OwnerId, habitId, false);
            if (updated is null) return Rollback(transaction, PersistenceFailure<HabitDetail>());
            WriteAudit(connection, transaction, actor, habitId, action, "productivity.Habit", traceId);
            return CommitResource(connection, transaction, actor, receipt, updated, action, [action], "HabitTransitioned", 200, now);
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

    private static HabitDetail ReadDetail(SqlConnection connection, SqlTransaction? transaction, HabitRow row, DateOnly? from, DateOnly? to, DateTimeOffset now)
    {
        var zone = ResolveZone(row.TimeZoneId);
        var today = LocalToday(zone, now);
        var lower = from ?? today.AddDays(-DefaultDetailHistoryDays);
        var upper = to ?? today;
        if (upper < lower) (lower, upper) = (upper, lower);
        return new HabitDetail(ToRecord(connection, transaction, row, now), ReadSchedules(connection, transaction, row.OwnerId, row.Id, false),
            ReadCheckIns(connection, transaction, row.OwnerId, row.Id, lower, upper, false));
    }

    private static HabitRecord ToRecord(SqlConnection connection, SqlTransaction? transaction, HabitRow row, DateTimeOffset now)
    {
        var today = LocalToday(ResolveZone(row.TimeZoneId), now);
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
            WHERE [OwnerId] = @OwnerId AND [Id] = @HabitId AND [TrashBatchId] IS NULL;
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
              AND [TrashBatchId] IS NULL
              AND (@State IS NULL OR [State] = @State)
              AND (@Query IS NULL OR LOWER([Title]) LIKE LOWER(@QueryLike) ESCAPE N'\')
            ORDER BY CASE [State] WHEN 'Active' THEN 0 WHEN 'Paused' THEN 1 ELSE 2 END, [Title], [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, limit);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@State", SqlDbType.VarChar, (object?)state ?? DBNull.Value, 16);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)query ?? DBNull.Value, 100);
        var queryPattern = query is null ? null : $"%{EscapeLikePattern(query)}%";
        Add(command, "@QueryLike", SqlDbType.NVarChar, (object?)queryPattern ?? DBNull.Value, HabitPolicy.MaximumSearchPatternLength);
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

    private static void InsertSchedule(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid userId, Guid habitId,
        DateOnly effectiveFrom, byte weekdayMask, int? targetCount, bool paused)
    {
        Execute(connection, transaction, """
            INSERT INTO [productivity].[HabitSchedule]
                ([Id], [OwnerId], [HabitId], [CreatedByUserId], [UpdatedByUserId], [EffectiveFrom], [WeekdayMask], [TargetCount], [Paused])
            VALUES (@Id, @OwnerId, @HabitId, @UserId, @UserId, @EffectiveFrom, @WeekdayMask, @TargetCount, @Paused);
            """,
            ("@Id", SqlDbType.UniqueIdentifier, Guid.NewGuid(), null),
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null),
            ("@HabitId", SqlDbType.UniqueIdentifier, habitId, null),
            ("@UserId", SqlDbType.UniqueIdentifier, userId, null),
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

    private static bool ReplaceCurrentSchedulePause(SqlConnection connection, SqlTransaction transaction, Guid ownerId,
        Guid userId, Guid habitId, DateOnly effectiveDate, HabitScheduleRecord current, bool paused)
    {
        if (current.EffectiveFrom == effectiveDate)
        {
            var changed = Execute(connection, transaction, """
                UPDATE [productivity].[HabitSchedule]
                SET [Paused] = @Paused, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @Id AND [OwnerId] = @OwnerId;
                """,
                ("@Paused", SqlDbType.Bit, paused, null),
                ("@UserId", SqlDbType.UniqueIdentifier, userId, null),
                ("@Id", SqlDbType.UniqueIdentifier, current.Id, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null));
            return changed == 1;
        }

        var closed = Execute(connection, transaction, """
            UPDATE [productivity].[HabitSchedule]
            SET [EffectiveUntil] = @EffectiveUntil, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [EffectiveUntil] IS NULL;
            """,
            ("@EffectiveUntil", SqlDbType.Date, ToDbDate(effectiveDate), null),
            ("@UserId", SqlDbType.UniqueIdentifier, userId, null),
            ("@Id", SqlDbType.UniqueIdentifier, current.Id, null),
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId, null));
        if (closed != 1) return false;
        InsertSchedule(connection, transaction, ownerId, userId, habitId, effectiveDate, current.WeekdayMask, current.TargetCount, paused);
        return true;
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
        if ((command.Kind == HabitKinds.Boolean && command.TargetCount is not null) ||
            (command.Kind == HabitKinds.Count && command.TargetCount is not > 0))
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

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);

    private static string Intent(object command) => JsonSerializer.Serialize(new { version = 2, command });

    private bool CurrentAuthority(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        params string[] actions) => _capabilities.IsAllowed(connection, transaction, actor, "FX17", actions);

    // The installed projection includes history-derived streaks. A write grant
    // never supplies either of these independent read capabilities.
    private bool CanReadDetail(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor) =>
        CurrentAuthority(connection, transaction, actor, "habits.habit.read", "habits.streak.read");

    private ReceiptClaim? InspectReceipt(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operation, string? key, string canonical, string legacy)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var claim = _receipts.TryClaim(connection, transaction, actor.UserId, operation, key, canonical,
            _clock.GetUtcNow().UtcDateTime, claimIfMissing: false, legacyCanonicalRequest: legacy);
        return claim.IsMissing ? null : claim;
    }

    private IdentityOperationResult<HabitDetail>? CheckReceipt(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        string legacyCanonicalRequest, Guid? habitId, string[] actions, out ReceiptClaim claim)
    {
        claim = default;
        if (actions.Length > 0 && !CurrentAuthority(connection, transaction, actor, actions))
            return ModuleUnavailable<HabitDetail>();
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest,
            _clock.GetUtcNow().UtcDateTime, legacyCanonicalRequest: legacyCanonicalRequest);
        if (claim.IsClaimed) return null;
        return ReceiptOutcome(connection, transaction, actor, operationKey, habitId, claim);
    }

    private IdentityOperationResult<HabitDetail> ReceiptOutcome(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operation, Guid? habitId, ReceiptClaim claim)
    {
        var entryActions = operation == HabitReceiptOperations.CheckIn
            ? new[] { "habits.checkin.record", "habits.checkin.correct" }
            : new[] { operation };
        if (!entryActions.Any(action => CurrentAuthority(connection, transaction, actor, action)))
            return ModuleUnavailable<HabitDetail>();
        if (claim.IsInvalid) return Failure<HabitDetail>("ValidationFailed", 422, "Idempotency key must be a UUID.");
        if (claim.IsConflict) return Failure<HabitDetail>("IdempotencyConflict", 409, "The key belongs to another request.");
        if (!claim.IsReplay) return Failure<HabitDetail>("RequestInProgress", 409, "The request is in progress.");

        HabitReceipt? outcome = null;
        if (!string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            try { outcome = JsonSerializer.Deserialize<HabitReceipt>(claim.ResultJson); }
            catch (JsonException) { }
        }
        else if (habitId is { } legacyId)
        {
            // Only fixed typed payloads (with a final free-text note for check-in)
            // have an unambiguous legacy binding. Metadata/create do not.
            var legacyAction = operation == HabitReceiptOperations.CheckIn ? claim.ResultCode switch
            {
                "HabitCheckInRecorded" => "habits.checkin.record",
                "HabitCheckInCorrected" => "habits.checkin.correct", _ => null
            } : operation is "habits.habit.schedule" or "habits.habit.pause" or "habits.habit.resume"
                or "habits.habit.archive" or "habits.habit.unarchive" ? operation : null;
            if (legacyAction is not null)
                outcome = new HabitReceipt(2, actor.UserId, actor.OwnerId, legacyId, operation, [legacyAction], 200, claim.ResultCode ?? string.Empty);
        }
        if (outcome is null || outcome.Version != 2 || outcome.UserId != actor.UserId || outcome.OwnerId != actor.OwnerId
            || outcome.HabitId == Guid.Empty || (habitId.HasValue && outcome.HabitId != habitId.Value)
            || outcome.Operation != operation || outcome.Code != claim.ResultCode
            || (claim.ResultStatusCode.HasValue && outcome.Status != claim.ResultStatusCode.Value)
            || !ValidOutcome(outcome))
            return Failure<HabitDetail>("CommittedOutcomeUnavailable", 409,
                "The request completed. Reconcile its outcome before starting another request.");
        if (!CurrentAuthority(connection, transaction, actor, outcome.Actions)) return ModuleUnavailable<HabitDetail>();
        return IdentityOperationResult<HabitDetail>.Committed(outcome.HabitId, outcome.Status, outcome.Code);
    }

    private static bool ValidOutcome(HabitReceipt value)
    {
        if (value.Actions is null) return false;
        string[][] actionSets = value.Operation switch
        {
            "habits.habit.create" => [[value.Operation], [value.Operation, "habits.habit.set_reminder"]],
            "habits.habit.update" => [[value.Operation, "habits.habit.read"], [value.Operation, "habits.habit.read", "habits.habit.set_reminder"]],
            HabitReceiptOperations.CheckIn => [["habits.checkin.record"], ["habits.checkin.correct"]],
            "habits.habit.set_reminder" or "habits.habit.schedule" or "habits.habit.pause" or "habits.habit.resume"
                or "habits.habit.archive" or "habits.habit.unarchive" => [[value.Operation]],
            _ => []
        };
        if (!actionSets.Any(set => value.Actions.SequenceEqual(set))) return false;
        return (value.Operation, value.Code, value.Status) switch
        {
            ("habits.habit.create", "HabitCreated", 201) => true,
            ("habits.habit.update", "HabitUpdated", 200) => true,
            ("habits.habit.set_reminder", "HabitReminderUpdated", 200) => true,
            ("habits.habit.schedule", "HabitScheduleUpdated", 200) => true,
            (HabitReceiptOperations.CheckIn, "HabitCheckInRecorded", 200) => value.Actions[0] == "habits.checkin.record",
            (HabitReceiptOperations.CheckIn, "HabitCheckInCorrected", 200) => value.Actions[0] == "habits.checkin.correct",
            ("habits.habit.pause" or "habits.habit.resume" or "habits.habit.archive" or "habits.habit.unarchive", "HabitTransitioned", 200) => true,
            _ => false
        };
    }

    private IdentityOperationResult<HabitDetail> CommitResource(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, ReceiptClaim claim, HabitRow row, string operation, string[] actions,
        string code, int status, DateTimeOffset now)
    {
        _receipts.Complete(connection, transaction, claim, code, status,
            JsonSerializer.Serialize(new HabitReceipt(2, actor.UserId, actor.OwnerId, row.Id, operation, actions, status, code)));
        if (!CurrentAuthority(connection, transaction, actor, actions)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        HabitDetail? detail = null;
        if (CanReadDetail(connection, transaction, actor))
        {
            var current = ReadHabit(connection, transaction, actor.OwnerId, row.Id, false);
            if (current is not null) detail = ReadDetail(connection, transaction, current, null, null, now);
        }
        if (!CurrentAuthority(connection, transaction, actor, actions)) return Rollback(transaction, ModuleUnavailable<HabitDetail>());
        var readable = detail is not null && CanReadDetail(connection, transaction, actor);
        transaction.Commit();
        return readable ? IdentityOperationResult<HabitDetail>.Success(detail!, status, code)
            : IdentityOperationResult<HabitDetail>.Committed(row.Id, status, code);
    }

    private sealed record HabitReceipt(int Version, Guid UserId, Guid OwnerId, Guid HabitId, string Operation,
        string[] Actions, int Status, string Code);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string targetType, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, @TargetType, @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
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
    private static DateOnly LocalToday(TimeZoneInfo zone, DateTimeOffset now) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
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
            if (string.IsNullOrWhiteSpace(value)) return false;
            var token = value.Trim();
            if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
            bytes = Convert.FromBase64String(token[1..^1]);
            return bytes.Length == 8 && Convert.ToBase64String(bytes) == token[1..^1];
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
