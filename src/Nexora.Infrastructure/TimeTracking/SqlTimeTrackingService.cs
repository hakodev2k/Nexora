using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.TimeTracking;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.TimeTracking;

/// <summary>Unlinked personal time slice. Source references are rejected by the strict API DTO.</summary>
public sealed class SqlTimeTrackingService : ITimeTrackingService
{
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    public static readonly string[] Actions = ["time.timer.read", "time.timer.start", "time.timer.stop", "time.timer.resume",
        "time.entry.read", "time.entry.create", "time.entry.update", "time.entry.trash", "time.entry.restore", "time.entry.history", "time.entry.purge", "time.report.read"];
    public SqlTimeTrackingService(SqlConnectionFactory connections, string secret)
    {
        this.connections = connections;
        capabilities = new(connections);
        receipts = new(secret);
    }
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX18", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(action => action, action => Allowed(actor, action));
        return result.Values.Any(x => x)
            ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result)
            : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<TimeEntryPage> List(IdentityPrincipal actor, Guid? cursor, bool trash)
    {
        if (!Allowed(actor, "time.entry.read")) return Denied<TimeEntryPage>();
        using var c = connections.Create(); c.Open();
        // Running timers have a separate action, so entry.read never bypasses timer.read.
        using var cmd = Command(c, null, "SELECT TOP(51) Id,StartAt,EndAt,Description,Category,Status,UpdatedAt,RowVersion FROM [time].[Entry] WHERE OwnerId=@Owner AND Status=@Status AND (@Cursor IS NULL OR StartAt<(SELECT StartAt FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Cursor) OR (StartAt=(SELECT StartAt FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Cursor) AND Id<@Cursor)) ORDER BY StartAt DESC,Id DESC", actor.OwnerId);
        Add(cmd, "@Status", trash ? "Trash" : "Stopped"); Add(cmd, "@Cursor", cursor);
        var items = Rows(cmd);
        return IdentityOperationResult<TimeEntryPage>.Success(new(items.Take(50).ToArray(), items.Count > 50 ? items[49].Id : null));
    }
    public IdentityOperationResult<TimeEntry> Get(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor, "time.entry.read")) return Denied<TimeEntry>();
        using var c = connections.Create(); c.Open(); var row = Read(c, null, actor.OwnerId, id);
        if (row is null) return Missing<TimeEntry>();
        if (row.Status == "Running" && !Allowed(actor, "time.timer.read")) return Denied<TimeEntry>();
        return IdentityOperationResult<TimeEntry>.Success(row);
    }
    public IdentityOperationResult<TimeEntry?> Timer(IdentityPrincipal actor)
    {
        if (!Allowed(actor, "time.timer.read")) return Denied<TimeEntry?>();
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, "SELECT Id,StartAt,EndAt,Description,Category,Status,UpdatedAt,RowVersion FROM [time].[Entry] WHERE OwnerId=@Owner AND Status='Running'", actor.OwnerId);
        return IdentityOperationResult<TimeEntry?>.Success(Rows(cmd).FirstOrDefault());
    }
    public IdentityOperationResult<TimeEntry> Start(IdentityPrincipal actor, TimerCommand body, string key, string? trace)
    {
        var action = body.ResumeEntryId is null ? "time.timer.start" : "time.timer.resume";
        return Mutate(actor, action, body, key, trace, (c, tx) =>
        {
            if (!ValidText(body.Description, body.Category)) return Fail<TimeEntry>("ValidationFailed", 422, "Description or category is too long.");
            if (body.ResumeEntryId is { } source)
            {
                var original = Read(c, tx, actor.OwnerId, source);
                if (original is null) return Missing<TimeEntry>();
                if (original.Status != "Stopped") return Fail<TimeEntry>("LifecycleLocked", 409, "Only stopped entries can be resumed.");
            }
            using var check = Command(c, tx, "SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Status='Running'", actor.OwnerId);
            if (Convert.ToInt32(check.ExecuteScalar()) != 0) return Fail<TimeEntry>("TimerAlreadyRunning", 409, "Stop the current timer first.");
            var id = Guid.NewGuid();
            using var insert = Command(c, tx, "INSERT [time].[Entry](Id,OwnerId,StartAt,Description,Category,Status) VALUES(@Id,@Owner,SYSUTCDATETIME(),@Description,@Category,'Running')", actor.OwnerId);
            Add(insert, "@Id", id); Text(insert, body.Description, body.Category); insert.ExecuteNonQuery();
            return IdentityOperationResult<TimeEntry>.Success(Read(c, tx, actor.OwnerId, id)!, 201);
        });
    }
    public IdentityOperationResult<TimeEntry> Stop(IdentityPrincipal actor, Guid id, string? etag, string key, string? trace) =>
        Mutate(actor, "time.timer.stop", new { id, etag }, key, trace, (c, tx) =>
        {
            var row = Read(c, tx, actor.OwnerId, id);
            if (row is null) return Missing<TimeEntry>();
            // Stop has domain idempotency in addition to its durable request receipt.
            if (row.Status == "Stopped") return IdentityOperationResult<TimeEntry>.Success(row);
            if (row.Status != "Running") return Fail<TimeEntry>("LifecycleLocked", 409, "This entry is not running.");
            var stale = Precondition<TimeEntry>(row, etag); if (stale is not null) return stale;
            SaveBefore(c, tx, actor.OwnerId, row, "time.timer.stop");
            using var cmd = Command(c, tx, "UPDATE [time].[Entry] SET EndAt=CASE WHEN SYSUTCDATETIME()>StartAt THEN SYSUTCDATETIME() ELSE DATEADD(microsecond,1,StartAt) END,Status='Stopped',UpdatedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND Id=@Id", actor.OwnerId);
            Add(cmd, "@Id", id); cmd.ExecuteNonQuery();
            return IdentityOperationResult<TimeEntry>.Success(Read(c, tx, actor.OwnerId, id)!);
        });
    public IdentityOperationResult<TimeEntry> Create(IdentityPrincipal actor, TimeEntryCommand body, string key, string? trace) =>
        Save(actor, null, null, body, key, trace);
    public IdentityOperationResult<TimeEntry> Update(IdentityPrincipal actor, Guid id, string? etag, TimeEntryCommand body, string key, string? trace) =>
        Save(actor, id, etag, body, key, trace);
    private IdentityOperationResult<TimeEntry> Save(IdentityPrincipal actor, Guid? id, string? etag, TimeEntryCommand body, string key, string? trace)
    {
        var action = id is null ? "time.entry.create" : "time.entry.update";
        return Mutate(actor, action, new { id, etag, body }, key, trace, (c, tx) =>
        {
            if (body.EndAt <= body.StartAt || !ValidText(body.Description, body.Category))
                return Fail<TimeEntry>("ValidationFailed", 422, "End must follow start; description and category must fit their limits.");
            if (id is { } existingId)
            {
                var row = Read(c, tx, actor.OwnerId, existingId);
                if (row is null) return Missing<TimeEntry>();
                if (row.Status != "Stopped") return Fail<TimeEntry>("LifecycleLocked", 409, "Only stopped entries can be edited.");
                var stale = Precondition<TimeEntry>(row, etag); if (stale is not null) return stale;
                SaveBefore(c, tx, actor.OwnerId, row, action);
            }
            using var overlap = Command(c, tx, "SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Status<>'Trash' AND (@Id IS NULL OR Id<>@Id) AND StartAt<@End AND (EndAt IS NULL OR EndAt>@Start)", actor.OwnerId);
            Add(overlap, "@Id", id); Add(overlap, "@Start", body.StartAt.UtcDateTime); Add(overlap, "@End", body.EndAt.UtcDateTime);
            if (Convert.ToInt32(overlap.ExecuteScalar()) > 0 && !body.ConfirmOverlap)
                return Fail<TimeEntry>("OverlapConfirmationRequired", 409, "This entry overlaps existing time. Confirm the gross-duration overlap explicitly.");
            var target = id ?? Guid.NewGuid();
            using var cmd = Command(c, tx, id is null
                ? "INSERT [time].[Entry](Id,OwnerId,StartAt,EndAt,Description,Category,Status) VALUES(@Id,@Owner,@Start,@End,@Description,@Category,'Stopped')"
                : "UPDATE [time].[Entry] SET StartAt=@Start,EndAt=@End,Description=@Description,Category=@Category,UpdatedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND Id=@Id", actor.OwnerId);
            Add(cmd, "@Id", target); Add(cmd, "@Start", body.StartAt.UtcDateTime); Add(cmd, "@End", body.EndAt.UtcDateTime); Text(cmd, body.Description, body.Category); cmd.ExecuteNonQuery();
            return IdentityOperationResult<TimeEntry>.Success(Read(c, tx, actor.OwnerId, target)!, id is null ? 201 : 200);
        });
    }
    public IdentityOperationResult<TimeEntry> Transition(IdentityPrincipal actor, Guid id, string? etag, bool restore, string key, string? trace)
    {
        var action = restore ? "time.entry.restore" : "time.entry.trash";
        return Mutate(actor, action, new { id, etag }, key, trace, (c, tx) =>
        {
            var row = Read(c, tx, actor.OwnerId, id); if (row is null) return Missing<TimeEntry>();
            var stale = Precondition<TimeEntry>(row, etag); if (stale is not null) return stale;
            if (row.Status != (restore ? "Trash" : "Stopped")) return Fail<TimeEntry>("LifecycleLocked", 409, "The entry state does not permit this transition.");
            SaveBefore(c, tx, actor.OwnerId, row, action);
            using var cmd = Command(c, tx, "UPDATE [time].[Entry] SET Status=@Status,UpdatedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND Id=@Id", actor.OwnerId);
            Add(cmd, "@Id", id); Add(cmd, "@Status", restore ? "Stopped" : "Trash"); cmd.ExecuteNonQuery();
            return IdentityOperationResult<TimeEntry>.Success(Read(c, tx, actor.OwnerId, id)!);
        });
    }
    public IdentityOperationResult<TimeHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor)
    {
        if (!Allowed(actor, "time.entry.history")) return Denied<TimeHistoryPage>();
        using var c = connections.Create(); c.Open(); if (Read(c, null, actor.OwnerId, id) is null) return Missing<TimeHistoryPage>();
        using var cmd = Command(c, null, "SELECT TOP(51) Id,Action,At,BeforeJson FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id AND (@Cursor IS NULL OR At<(SELECT At FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id AND Id=@Cursor) OR (At=(SELECT At FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id AND Id=@Cursor) AND Id<@Cursor)) ORDER BY At DESC,Id DESC", actor.OwnerId);
        Add(cmd, "@Id", id); Add(cmd, "@Cursor", cursor);
        using var r = cmd.ExecuteReader(); var rows = new List<TimeCorrection>();
        while (r.Read()) rows.Add(new(r.GetGuid(0), r.GetString(1), Utc(r.GetDateTime(2)), JsonSerializer.Deserialize<TimeEntry>(r.GetString(3))!));
        return IdentityOperationResult<TimeHistoryPage>.Success(new(rows.Take(50).ToArray(), rows.Count > 50 ? rows[49].Id : null));
    }
    public IdentityOperationResult<TimePurgePreview> PreviewPurge(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor, "time.entry.purge")) return Denied<TimePurgePreview>();
        using var c = connections.Create(); c.Open();
        var row = Read(c, null, actor.OwnerId, id);
        if (row is null) return Missing<TimePurgePreview>();
        if (row.Status != "Trash") return Fail<TimePurgePreview>("LifecycleLocked", 409, "Only entries in Trash can be purged.");
        using var cmd = Command(c, null, "SELECT (SELECT COUNT(*) FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id),(SELECT COUNT(*) FROM [time].[FocusConversion] WHERE OwnerId=@Owner AND EntryId=@Id)", actor.OwnerId);
        Add(cmd, "@Id", id); using var r = cmd.ExecuteReader(); r.Read();
        return IdentityOperationResult<TimePurgePreview>.Success(new(id, row.ETag, r.GetInt32(0), r.GetInt32(1) != 0));
    }
    public IdentityOperationResult<TimePurged> Purge(IdentityPrincipal actor, Guid id, string? etag,
        TimePurgeCommand body, string key, string? trace) =>
        Mutate(actor, "time.entry.purge", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var row = Read(c, tx, actor.OwnerId, id); if (row is null) return Missing<TimePurged>();
            var stale = Precondition<TimePurged>(row, etag); if (stale is not null) return stale;
            if (row.Status != "Trash") return Fail<TimePurged>("LifecycleLocked", 409, "Only entries in Trash can be purged.");
            if (!body.ConfirmPermanentDeletion) return Fail<TimePurged>("ConfirmationRequired", 422, "Confirm permanent deletion of this entry and its correction history.");
            using (var pinned = Command(c, tx, "SELECT COUNT(*) FROM [time].[FocusConversion] WHERE OwnerId=@Owner AND EntryId=@Id", actor.OwnerId))
            {
                Add(pinned, "@Id", id);
                if (Convert.ToInt32(pinned.ExecuteScalar()) != 0) return Fail<TimePurged>("ReferencePinned", 409, "A focus conversion still references this entry.");
            }
            using var cmd = Command(c, tx, "DELETE [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id; DELETE [time].[Entry] WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
            Add(cmd, "@Id", id); cmd.ExecuteNonQuery();
            return IdentityOperationResult<TimePurged>.Success(new(id));
        });
    public IdentityOperationResult<TimeReport> Report(IdentityPrincipal actor)
    {
        if (!Allowed(actor, "time.report.read")) return Denied<TimeReport>();
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, "SELECT COALESCE(SUM(DATEDIFF_BIG(millisecond,StartAt,EndAt)),0),COUNT(*),CASE WHEN EXISTS(SELECT 1 FROM [time].[Entry] a JOIN [time].[Entry] b ON a.OwnerId=b.OwnerId AND a.Id<b.Id AND a.StartAt<b.EndAt AND b.StartAt<a.EndAt WHERE a.OwnerId=@Owner AND a.Status='Stopped' AND b.Status='Stopped') THEN 1 ELSE 0 END FROM [time].[Entry] WHERE OwnerId=@Owner AND Status='Stopped'", actor.OwnerId);
        using var r = cmd.ExecuteReader(); r.Read(); return IdentityOperationResult<TimeReport>.Success(new(r.GetInt64(0), r.GetInt32(1), r.GetInt32(2) == 1));
    }
    private IdentityOperationResult<T> Mutate<T>(IdentityPrincipal actor, string action, object request, string key, string? trace,
        Func<SqlConnection, SqlTransaction, IdentityOperationResult<T>> work)
    {
        if (!Allowed(actor, action)) return Denied<T>();
        if (!Guid.TryParse(key, out _)) return Fail<T>("IdempotencyKeyRequired", 422, "A UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User", actor.OwnerId))
        { Add(owner, "@User", actor.UserId); if (owner.ExecuteScalar() is null) return Denied<T>(); }
        if (!capabilities.IsAllowed(c, tx, actor, "FX18", action)) return Denied<T>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<T>("IdempotencyConflict", 409, "The request key was used for another payload.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
            return IdentityOperationResult<T>.Success(JsonSerializer.Deserialize<T>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<T>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx);
        if (!result.Succeeded || result.Value is null) return result;
        if (!capabilities.IsAllowed(c, tx, actor, "FX18", action)) return Denied<T>();
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'time.Entry',@Id,'Succeeded',@Trace)", actor.OwnerId);
        Add(audit, "@User", actor.UserId); Add(audit, "@Action", action); Add(audit, "@Id", result.Value switch { TimeEntry entry => entry.Id, TimePurged purged => purged.EntryId, _ => throw new InvalidOperationException("Unknown Time mutation result.") }); Add(audit, "@Trace", trace); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static IdentityOperationResult<T>? Precondition<T>(TimeEntry row, string? etag) => string.IsNullOrWhiteSpace(etag)
        ? Fail<T>("PreconditionRequired", 428, "If-Match is required.")
        : etag == row.ETag ? null : Fail<T>("RevisionConflict", 412, "Reload the current entry before retrying.");
    private static bool ValidText(string? description, string? category) => description is not { Length: > 2000 } && category is not { Length: > 200 };
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static void Text(SqlCommand cmd, string? description, string? category) { Add(cmd, "@Description", description?.Trim()); Add(cmd, "@Category", category?.Trim()); }
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, object? value)
    {
        var type = value switch { Guid => SqlDbType.UniqueIdentifier, DateTime => SqlDbType.DateTime2, int => SqlDbType.Int, _ => SqlDbType.NVarChar };
        // Nullable UUID parameters still need their SQL type when their value is absent.
        if (name is "@Id" or "@Cursor" or "@Owner" or "@User") type = SqlDbType.UniqueIdentifier;
        if (name == "@Id" && value is string) type = SqlDbType.NVarChar;
        var p = cmd.Parameters.Add(name, type); if (type == SqlDbType.NVarChar) p.Size = -1; p.Value = value ?? DBNull.Value;
    }
    private static TimeEntry? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT Id,StartAt,EndAt,Description,Category,Status,UpdatedAt,RowVersion FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Id", owner);
        Add(cmd, "@Id", id); return Rows(cmd).FirstOrDefault();
    }
    private static List<TimeEntry> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var rows = new List<TimeEntry>();
        while (r.Read())
        {
            var start = Utc(r.GetDateTime(1)); DateTimeOffset? end = r.IsDBNull(2) ? null : Utc(r.GetDateTime(2));
            rows.Add(new(r.GetGuid(0), start, end, r.IsDBNull(3) ? null : r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4),
                r.GetString(5), Math.Max(0, (long)((end ?? DateTimeOffset.UtcNow) - start).TotalMilliseconds), Utc(r.GetDateTime(6)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(7)) + "\""));
        }
        return rows;
    }
    private static void SaveBefore(SqlConnection c, SqlTransaction tx, Guid owner, TimeEntry row, string action)
    {
        using var cmd = Command(c, tx, "INSERT [time].[Correction](Id,OwnerId,EntryId,Action,BeforeJson) VALUES(@Id,@Owner,@Entry,@Action,@Json)", owner);
        Add(cmd, "@Id", Guid.NewGuid()); Add(cmd, "@Entry", row.Id); Add(cmd, "@Action", action); Add(cmd, "@Json", JsonSerializer.Serialize(row)); cmd.ExecuteNonQuery();
    }
}
