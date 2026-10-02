using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Focus;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Notifications;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Focus;

public sealed class SqlFocusService : IFocusService, IFocusPhaseFinisher
{
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private static readonly string[] Actions = ["focus.session.read", "focus.session.start", "focus.session.pause", "focus.session.resume", "focus.session.cancel", "focus.preference.update", "focus.session.record_time"];
    public SqlFocusService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX19", action);
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(a => a, a => Allowed(actor, a) && (a != "focus.session.record_time" || capabilities.IsAllowed(actor, "FX18", "time.entry.create")));
        return result.Values.Any(x => x) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<FocusPage> List(IdentityPrincipal actor, Guid? cursor, bool activeOnly)
    {
        if (!Allowed(actor, "focus.session.read")) return Denied<FocusPage>();
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, """
            SELECT TOP(51) Id,Phase,State,PlannedSeconds,ElapsedMilliseconds,StartedAt,LastRunAt,CompletedAt,RowVersion,SYSUTCDATETIME()
            FROM [time].[FocusSession] WHERE OwnerId=@Owner AND (@Active=0 OR IsActive=1)
             AND (@Cursor IS NULL OR StartedAt<(SELECT StartedAt FROM [time].[FocusSession] WHERE OwnerId=@Owner AND Id=@Cursor)
              OR (StartedAt=(SELECT StartedAt FROM [time].[FocusSession] WHERE OwnerId=@Owner AND Id=@Cursor) AND Id<@Cursor))
            ORDER BY StartedAt DESC,Id DESC
            """, actor.OwnerId);
        Add(cmd, "@Active", activeOnly); Add(cmd, "@Cursor", cursor);
        var rows = Rows(cmd); return IdentityOperationResult<FocusPage>.Success(new(rows.Take(50).ToArray(), rows.Count > 50 ? rows[49].Id : null));
    }
    public IdentityOperationResult<FocusPreferences> Preferences(IdentityPrincipal actor)
    {
        if (!Allowed(actor, "focus.session.read")) return Denied<FocusPreferences>();
        using var c = connections.Create(); c.Open(); return IdentityOperationResult<FocusPreferences>.Success(ReadPreferences(c, null, actor.OwnerId));
    }
    public IdentityOperationResult<FocusPreferences> SavePreferences(IdentityPrincipal actor, FocusPreferenceCommand body, string? etag, string key) =>
        Mutate(actor, "focus.preference.update", new { body, etag }, key, (c, tx) =>
        {
            if (body.FocusMinutes is < 1 or > 180 || body.ShortBreakMinutes is < 1 or > 60 || body.LongBreakMinutes is < 1 or > 120 || body.CycleLength is < 1 or > 12)
                return Fail<FocusPreferences>("ValidationFailed", 422, "Focus preferences are outside their supported ranges.");
            var old = ReadPreferences(c, tx, actor.OwnerId); var stale = Precondition<FocusPreferences>(old.ETag, etag); if (stale is not null) return stale;
            using var cmd = Command(c, tx, """
                IF EXISTS(SELECT 1 FROM [time].[FocusPreference] WHERE OwnerId=@Owner)
                 UPDATE [time].[FocusPreference] SET FocusMinutes=@Focus,ShortBreakMinutes=@Short,LongBreakMinutes=@Long,CycleLength=@Cycle WHERE OwnerId=@Owner;
                ELSE INSERT [time].[FocusPreference](OwnerId,FocusMinutes,ShortBreakMinutes,LongBreakMinutes,CycleLength) VALUES(@Owner,@Focus,@Short,@Long,@Cycle);
                """, actor.OwnerId);
            Add(cmd, "@Focus", body.FocusMinutes); Add(cmd, "@Short", body.ShortBreakMinutes); Add(cmd, "@Long", body.LongBreakMinutes); Add(cmd, "@Cycle", body.CycleLength); cmd.ExecuteNonQuery();
            return IdentityOperationResult<FocusPreferences>.Success(ReadPreferences(c, tx, actor.OwnerId));
        });
    public IdentityOperationResult<FocusSession> Start(IdentityPrincipal actor, FocusStart body, string key) =>
        Mutate(actor, "focus.session.start", body, key, (c, tx) =>
        {
            if (body.Phase is not ("Focus" or "ShortBreak" or "LongBreak")) return Fail<FocusSession>("ValidationFailed", 422, "Choose a supported phase.");
            using var check = Command(c, tx, "SELECT COUNT(*) FROM [time].[FocusSession] WHERE OwnerId=@Owner AND IsActive=1", actor.OwnerId);
            if (Convert.ToInt32(check.ExecuteScalar()) != 0) return Fail<FocusSession>("FocusAlreadyActive", 409, "End the active session first.");
            var prefs = ReadPreferences(c, tx, actor.OwnerId);
            var seconds = 60 * (body.Phase == "Focus" ? prefs.FocusMinutes : body.Phase == "ShortBreak" ? prefs.ShortBreakMinutes : prefs.LongBreakMinutes);
            var id = Guid.NewGuid();
            using var cmd = Command(c, tx, "INSERT [time].[FocusSession](Id,OwnerId,Phase,State,IsActive,PlannedSeconds,ElapsedMilliseconds,StartedAt,LastRunAt) VALUES(@Id,@Owner,@Phase,'Running',1,@Seconds,0,SYSUTCDATETIME(),SYSUTCDATETIME())", actor.OwnerId);
            Add(cmd, "@Id", id); Add(cmd, "@Phase", body.Phase); Add(cmd, "@Seconds", seconds); cmd.ExecuteNonQuery();
            return IdentityOperationResult<FocusSession>.Success(Read(c, tx, actor.OwnerId, id)!, 201);
        });
    public IdentityOperationResult<FocusSession> Transition(IdentityPrincipal actor, Guid id, string action, string? etag, string key)
    {
        if (action is not ("pause" or "resume" or "cancel")) return Fail<FocusSession>("ValidationFailed", 422, "Unknown session transition.");
        return Mutate(actor, "focus.session." + action, new { id, etag }, key, (c, tx) =>
        {
            var old = Read(c, tx, actor.OwnerId, id); if (old is null) return Fail<FocusSession>("ResourceUnavailable", 404, "The resource is unavailable.");
            var stale = Precondition<FocusSession>(old.ETag, etag); if (stale is not null) return stale;
            if (old.State is not ("Running" or "Paused") || action == "pause" && old.State != "Running" || action == "resume" && old.State != "Paused")
                return Fail<FocusSession>("LifecycleLocked", 409, "The session state does not permit this transition.");
            if (old.State == "Running" && old.RemainingMilliseconds == 0) return Fail<FocusSession>("PhaseFinishing", 409, "The server is completing this phase. Reload its state.");
            using var cmd = Command(c, tx, "UPDATE [time].[FocusSession] SET State=@State,IsActive=@Active,ElapsedMilliseconds=@Elapsed,LastRunAt=CASE WHEN @State='Running' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id", actor.OwnerId);
            Add(cmd, "@Id", id); Add(cmd, "@State", action == "pause" ? "Paused" : action == "resume" ? "Running" : "Cancelled"); Add(cmd, "@Active", action != "cancel"); Add(cmd, "@Elapsed", old.ElapsedMilliseconds); cmd.ExecuteNonQuery();
            return IdentityOperationResult<FocusSession>.Success(Read(c, tx, actor.OwnerId, id)!);
        });
    }
    public IdentityOperationResult<FocusTimeConversion> RecordTime(IdentityPrincipal actor, Guid id,
        string? etag, FocusRecordTimeCommand body, string key)
    {
        // The wrapper never borrows target authority from its own action grant.
        if (!capabilities.IsAllowed(actor, "FX18", "time.entry.create")) return Denied<FocusTimeConversion>();
        return Mutate(actor, "focus.session.record_time", new { id, etag, body }, key, (c, tx) =>
        {
            if (!capabilities.IsAllowed(c, tx, actor, "FX18", "time.entry.create")) return Denied<FocusTimeConversion>();
            var session = Read(c, tx, actor.OwnerId, id);
            if (session is null) return Fail<FocusTimeConversion>("ResourceUnavailable", 404, "The resource is unavailable.");
            // Domain deduplication survives different request keys and expired receipts.
            using (var prior = Command(c, tx, "SELECT EntryId FROM [time].[FocusConversion] WHERE OwnerId=@Owner AND SessionId=@Id", actor.OwnerId))
            {
                Add(prior, "@Id", id);
                if (prior.ExecuteScalar() is Guid existing) return IdentityOperationResult<FocusTimeConversion>.Success(new(id, existing));
            }
            var stale = Precondition<FocusTimeConversion>(session.ETag, etag); if (stale is not null) return stale;
            if (session.Phase != "Focus" || session.State != "Completed" || session.CompletedAt is null || session.ElapsedMilliseconds <= 0)
                return Fail<FocusTimeConversion>("LifecycleLocked", 409, "Only a completed work phase can be recorded as time.");
            // Elapsed work excludes pauses. A conversion represents that duration
            // ending at completion; it does not copy the wall-clock span as work.
            var end = session.CompletedAt.Value.UtcDateTime;
            var start = end.AddMilliseconds(-session.ElapsedMilliseconds);
            using (var overlap = Command(c, tx, "SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Status<>'Trash' AND StartAt<@End AND (EndAt IS NULL OR EndAt>@Start)", actor.OwnerId))
            {
                Add(overlap, "@Start", start); Add(overlap, "@End", end);
                if (Convert.ToInt32(overlap.ExecuteScalar()) > 0 && !body.ConfirmOverlap)
                    return Fail<FocusTimeConversion>("OverlapConfirmationRequired", 409, "Confirm the overlapping gross work duration explicitly.");
            }
            var entry = Guid.NewGuid();
            using (var insert = Command(c, tx, "INSERT [time].[Entry](Id,OwnerId,StartAt,EndAt,Description,Category,Status) VALUES(@Entry,@Owner,@Start,@End,NULL,'Focus','Stopped'); INSERT [time].[FocusConversion](OwnerId,SessionId,EntryId) VALUES(@Owner,@Id,@Entry);", actor.OwnerId))
            {
                Add(insert, "@Entry", entry); Add(insert, "@Id", id); Add(insert, "@Start", start); Add(insert, "@End", end); insert.ExecuteNonQuery();
            }
            if (!capabilities.IsAllowed(c, tx, actor, "FX18", "time.entry.create")) return Denied<FocusTimeConversion>();
            Audit(c, tx, actor, "time.entry.create", entry);
            return IdentityOperationResult<FocusTimeConversion>.Success(new(id, entry), 201);
        });
    }

    public Task FinishDueAsync(CancellationToken cancellationToken)
    {
        using var c = connections.Create(); c.Open(); var due = new List<(Guid Id, Guid Owner, Guid User, string Role)>();
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = """
                SELECT TOP(100) f.Id,f.OwnerId,p.UserId,
                 COALESCE((SELECT TOP(1) r.Code FROM [identity].[UserRole] ur JOIN [identity].[Role] r ON r.Id=ur.RoleId WHERE ur.UserId=p.UserId ORDER BY CASE r.Code WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC),'User')
                FROM [time].[FocusSession] f JOIN [platform].[PersonalSpace] p ON p.Id=f.OwnerId
                WHERE f.State='Running' AND f.ElapsedMilliseconds+DATEDIFF_BIG(millisecond,f.LastRunAt,SYSUTCDATETIME())>=CAST(f.PlannedSeconds AS bigint)*1000
                 AND f.CompletionFailures<5 AND (f.NextCompletionAttemptAt IS NULL OR f.NextCompletionAttemptAt<=SYSUTCDATETIME())
                ORDER BY f.StartedAt,f.Id
                """;
            using var r = cmd.ExecuteReader(); while (r.Read()) due.Add((r.GetGuid(0), r.GetGuid(1), r.GetGuid(2), r.GetString(3)));
        }
        foreach (var item in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actor = new IdentityPrincipal(item.User, item.Owner, item.Role, DateTimeOffset.MinValue);
            try
            {
                using var tx = c.BeginTransaction(IsolationLevel.Serializable); LockOwner(c, tx, actor);
                var current = Read(c, tx, item.Owner, item.Id);
                if (current is null || current.State != "Running" || current.RemainingMilliseconds != 0 || !capabilities.IsAllowed(c, tx, actor, "FX19", "focus.session.start")) continue;
                using var finish = Command(c, tx, """
                    UPDATE [time].[FocusSession]
                    SET State='Completed',IsActive=0,
                        CompletedAt=DATEADD(millisecond,CAST(PlannedSeconds*1000-ElapsedMilliseconds AS int),LastRunAt),
                        LastRunAt=NULL,ElapsedMilliseconds=CAST(PlannedSeconds AS bigint)*1000
                    WHERE OwnerId=@Owner AND Id=@Id AND State='Running';
                    """, item.Owner);
                Add(finish, "@Id", item.Id); if (finish.ExecuteNonQuery() != 1) continue;
                LocalModuleNotificationWriter.FocusCompleted(c, tx, item.User, item.Id, current.Phase);
                Audit(c, tx, actor, "focus.session.finish_phase", item.Id, system: true);
                if (!capabilities.IsAllowed(c, tx, actor, "FX19", "focus.session.start")) continue;
                tx.Commit();
            }
            catch (SqlException)
            {
                // Persist a bounded backoff after rollback. No payload or exception message is stored.
                using var failed = Command(c, null, "UPDATE [time].[FocusSession] SET CompletionFailures=CompletionFailures+1,NextCompletionAttemptAt=DATEADD(second,30,SYSUTCDATETIME()) WHERE OwnerId=@Owner AND Id=@Id AND State='Running' AND CompletionFailures<5", item.Owner);
                Add(failed, "@Id", item.Id); failed.ExecuteNonQuery();
            }
        }
        return Task.CompletedTask;
    }
    private IdentityOperationResult<T> Mutate<T>(IdentityPrincipal actor, string action, object body, string key, Func<SqlConnection, SqlTransaction, IdentityOperationResult<T>> work)
    {
        if (!Allowed(actor, action)) return Denied<T>();
        if (!Guid.TryParse(key, out _)) return Fail<T>("IdempotencyKeyRequired", 422, "A UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable); LockOwner(c, tx, actor);
        if (!capabilities.IsAllowed(c, tx, actor, "FX19", action)) return Denied<T>();
        if (action == "focus.session.record_time" && !capabilities.IsAllowed(c, tx, actor, "FX18", "time.entry.create")) return Denied<T>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(body), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<T>("IdempotencyConflict", 409, "The request key was used for another payload.");
        if (claim.IsReplay && claim.ResultJson is { } saved) return IdentityOperationResult<T>.Success(JsonSerializer.Deserialize<T>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<T>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx); if (!result.Succeeded || result.Value is null) return result;
        if (!capabilities.IsAllowed(c, tx, actor, "FX19", action)) return Denied<T>();
        Audit(c, tx, actor, action, result.Value is FocusSession session ? session.Id : result.Value is FocusTimeConversion conversion ? conversion.SessionId : actor.OwnerId);
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static void LockOwner(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor)
    {
        using var cmd = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User", actor.OwnerId);
        Add(cmd, "@User", actor.UserId); cmd.ExecuteScalar();
    }
    private static void Audit(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action, Guid id, bool system = false)
    {
        using var cmd = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result) VALUES(@Actor,@User,@Action,@TargetType,@Id,'Succeeded')", actor.OwnerId);
        Add(cmd, "@TargetType", action == "time.entry.create" ? "time.Entry" : "time.Focus");
        Add(cmd, "@Actor", system ? null : actor.UserId); Add(cmd, "@User", actor.UserId); Add(cmd, "@Action", action); Add(cmd, "@Id", id); cmd.ExecuteNonQuery();
    }
    private static IdentityOperationResult<T>? Precondition<T>(string current, string? etag) => string.IsNullOrWhiteSpace(etag)
        ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") : current == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current revision before retrying.");
    private static FocusPreferences ReadPreferences(SqlConnection c, SqlTransaction? tx, Guid owner)
    {
        using var cmd = Command(c, tx, "SELECT FocusMinutes,ShortBreakMinutes,LongBreakMinutes,CycleLength,RowVersion FROM [time].[FocusPreference] WHERE OwnerId=@Owner", owner);
        using var r = cmd.ExecuteReader(); return r.Read() ? new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), Tag(r.GetFieldValue<byte[]>(4))) : new(25, 5, 15, 4, "\"0\"");
    }
    private static FocusSession? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT Id,Phase,State,PlannedSeconds,ElapsedMilliseconds,StartedAt,LastRunAt,CompletedAt,RowVersion,SYSUTCDATETIME() FROM [time].[FocusSession] WHERE OwnerId=@Owner AND Id=@Id", owner);
        Add(cmd, "@Id", id); return Rows(cmd).FirstOrDefault();
    }
    private static List<FocusSession> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<FocusSession>();
        while (r.Read())
        {
            DateTimeOffset? last = r.IsDBNull(6) ? null : Utc(r.GetDateTime(6)); var planned = r.GetInt32(3);
            var elapsed = Math.Clamp(r.GetInt64(4) + (last is { } running ? (long)(Utc(r.GetDateTime(9)) - running).TotalMilliseconds : 0), 0, planned * 1000L);
            items.Add(new(r.GetGuid(0), r.GetString(1), r.GetString(2), planned, elapsed, planned * 1000L - elapsed, Utc(r.GetDateTime(5)), last, r.IsDBNull(7) ? null : Utc(r.GetDateTime(7)), Tag(r.GetFieldValue<byte[]>(8))));
        }
        return items;
    }
    private static DateTimeOffset Utc(DateTime date) => new(DateTime.SpecifyKind(date, DateTimeKind.Utc));
    private static string Tag(byte[] version) => "\"" + Convert.ToBase64String(version) + "\"";
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, object? value)
    {
        var type = value switch { Guid => SqlDbType.UniqueIdentifier, bool => SqlDbType.Bit, long => SqlDbType.BigInt, int => SqlDbType.Int, DateTime => SqlDbType.DateTime2, _ => SqlDbType.NVarChar };
        if (name is "@Id" or "@Cursor" or "@User" or "@Owner" or "@Actor") type = SqlDbType.UniqueIdentifier;
        var p = cmd.Parameters.Add(name, type); if (type == SqlDbType.NVarChar) p.Size = -1; p.Value = value ?? DBNull.Value;
    }
}
