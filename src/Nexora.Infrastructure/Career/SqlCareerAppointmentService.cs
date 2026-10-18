using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Career;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;

namespace Nexora.Infrastructure.Career;

public sealed class SqlCareerAppointmentService : ICareerAppointmentService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlProductivityService _calendar;
    public SqlCareerAppointmentService(SqlConnectionFactory connections, string secret)
    { _connections = connections; _capabilities = new(connections); _receipts = new(secret); _calendar = new(connections, secret); }

    private bool CareerAllowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string action, bool jobRead) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX39", jobRead ? [action, "career.job.read"] : [action]);
    private bool CalendarAllowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string action) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX13", action);

    public IdentityOperationResult<CareerAppointmentPage> List(IdentityPrincipal actor, Guid jobId, Guid? cursor = null)
    {
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!CareerAllowed(connection, transaction, actor, "career.appointment.read", true) || !CalendarAllowed(connection, transaction, actor, "calendar.event.read")) return Denied<CareerAppointmentPage>();
            if (JobETag(connection, transaction, actor.OwnerId, jobId, false, allowTrash: true) is null) return Fail<CareerAppointmentPage>("ResourceUnavailable", 404);
            using var sql = connection.CreateCommand(); sql.Transaction = transaction;
            sql.CommandText = "SELECT TOP(26) [Id],[CalendarEventId],[RowVersion] FROM [career].[CalendarLink] WHERE [OwnerId]=@Owner AND [JobApplicationId]=@Job AND (@Cursor IS NULL OR [Id]>@Cursor) ORDER BY [Id];";
            Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@Job", SqlDbType.UniqueIdentifier, jobId); Add(sql, "@Cursor", SqlDbType.UniqueIdentifier, (object?)cursor ?? DBNull.Value);
            var rows = new List<(Guid Id, Guid EventId, string ETag)>();
            using (var reader = sql.ExecuteReader()) while (reader.Read()) rows.Add((reader.GetGuid(0), reader.GetGuid(1), Quote(reader.GetFieldValue<byte[]>(2))));
            var more = rows.Count > 25; if (more) rows.RemoveAt(25);
            var items = rows.Select(row => { var source = _calendar.ReadPersonalEventWithin(connection, transaction, actor, row.EventId);
                return new CareerAppointment(row.Id, jobId, row.EventId, row.ETag, source, source is null ? "Unavailable" : "Available"); }).ToArray();
            if (!CareerAllowed(connection, transaction, actor, "career.appointment.read", true) || !CalendarAllowed(connection, transaction, actor, "calendar.event.read")) return Denied<CareerAppointmentPage>();
            transaction.Commit(); return IdentityOperationResult<CareerAppointmentPage>.Success(new(items, more ? rows[^1].Id : null));
        }
        catch (SqlException) { return Fail<CareerAppointmentPage>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<object?> Link(IdentityPrincipal actor, Guid jobId, string? jobETag, CareerAppointmentCommand command,
        string? key = null, string? traceId = null)
    {
        if ((command.EventId.HasValue == (command.Event is not null)) || command.EventId == Guid.Empty) return Fail<object?>("ValidationFailed", 422);
        if (!ETag(jobETag, out var expected)) return Fail<object?>("PreconditionRequired", 428);
        var create = command.Event is not null; var action = create ? "career.appointment.create_event" : "career.appointment.link_event";
        var calendarAction = create ? "calendar.event.create" : "calendar.event.read";
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!CareerAllowed(connection, transaction, actor, action, true) || !CalendarAllowed(connection, transaction, actor, calendarAction)) return Denied<object?>();
            var replay = Claim(connection, transaction, actor, action, jobId, null, key,
                JsonSerializer.Serialize(new { version = 1, jobId, etag = Convert.ToBase64String(expected), command.EventId, command.Event }), out var receipt);
            if (replay is not null) return replay;
            var current = JobETag(connection, transaction, actor.OwnerId, jobId, true);
            if (current is null) return Fail<object?>("ResourceUnavailable", 404);
            if (!CryptographicOperations.FixedTimeEquals(expected, current)) return Fail<object?>("RevisionConflict", 412);
            Guid eventId;
            if (create)
            {
                var created = _calendar.CreateEventWithin(connection, transaction, actor, command.Event!, traceId);
                if (!created.Succeeded || created.Value is null) return IdentityOperationResult<object?>.Failure(created.Code, created.StatusCode, created.Title);
                eventId = created.Value.Id;
            }
            else
            {
                eventId = command.EventId!.Value;
                var source = _calendar.ReadPersonalEventWithin(connection, transaction, actor, eventId);
                if (source is null) return Fail<object?>("ResourceUnavailable", 404);
                if (source.Status != "Scheduled") return Fail<object?>("LifecycleLocked", 409);
            }
            var id = Guid.NewGuid();
            using (var sql = connection.CreateCommand())
            {
                sql.Transaction = transaction;
                sql.CommandText = "INSERT INTO [career].[CalendarLink]([Id],[OwnerId],[JobApplicationId],[CalendarEventId],[CreatedByUserId],[UpdatedByUserId]) VALUES(@Id,@Owner,@Job,@Event,@User,@User);";
                Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@Job", SqlDbType.UniqueIdentifier, jobId);
                Add(sql, "@Event", SqlDbType.UniqueIdentifier, eventId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); sql.ExecuteNonQuery();
            }
            TouchJob(connection, transaction, actor, jobId);
            Audit(connection, transaction, actor, id, action, traceId);
            _receipts.Complete(connection, transaction, receipt, "CareerAppointmentLinked", 201,
                JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, jobId, id, action)));
            if (!CareerAllowed(connection, transaction, actor, action, true) || !CalendarAllowed(connection, transaction, actor, calendarAction)
                || JobETag(connection, transaction, actor.OwnerId, jobId, false) is null) return Denied<object?>();
            if (!create)
            {
                var source = _calendar.ReadPersonalEventWithin(connection, transaction, actor, eventId);
                if (source is null || source.Status != "Scheduled") return Fail<object?>("LifecycleLocked", 409);
            }
            if (!_calendar.IsScheduledPersonalEventWithin(connection, transaction, actor, eventId, calendarAction))
                return Fail<object?>("LifecycleLocked", 409);
            transaction.Commit(); return IdentityOperationResult<object?>.Committed(id, 201, "CareerAppointmentLinked");
        }
        catch (SqlException error) when (error.Number is 2601 or 2627) { return Fail<object?>("AppointmentAlreadyLinked", 409); }
        catch (Exception error) when (error is SqlException or InvalidOperationException) { return Fail<object?>("PersistenceUnavailable", 503); }
    }

    public IdentityOperationResult<object?> Unlink(IdentityPrincipal actor, Guid jobId, Guid linkId, string? linkETag,
        string? key = null, string? traceId = null)
    {
        if (!ETag(linkETag, out var expected)) return Fail<object?>("PreconditionRequired", 428);
        const string action = "career.appointment.unlink_event";
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!CareerAllowed(connection, transaction, actor, action, false)) return Denied<object?>();
            var replay = Claim(connection, transaction, actor, action, jobId, linkId, key,
                JsonSerializer.Serialize(new { version = 1, jobId, linkId, etag = Convert.ToBase64String(expected) }), out var receipt);
            if (replay is not null) return replay;
            using var sql = connection.CreateCommand(); sql.Transaction = transaction;
            sql.CommandText = "SELECT [RowVersion] FROM [career].[CalendarLink] WITH(UPDLOCK,ROWLOCK) WHERE [OwnerId]=@Owner AND [JobApplicationId]=@Job AND [Id]=@Id;";
            Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@Job", SqlDbType.UniqueIdentifier, jobId); Add(sql, "@Id", SqlDbType.UniqueIdentifier, linkId);
            if (sql.ExecuteScalar() is not byte[] current) return Fail<object?>("ResourceUnavailable", 404);
            if (!CryptographicOperations.FixedTimeEquals(expected, current)) return Fail<object?>("RevisionConflict", 412);
            sql.CommandText = "DELETE FROM [career].[CalendarLink] WHERE [OwnerId]=@Owner AND [JobApplicationId]=@Job AND [Id]=@Id;"; sql.ExecuteNonQuery();
            TouchJob(connection, transaction, actor, jobId); Audit(connection, transaction, actor, linkId, action, traceId);
            _receipts.Complete(connection, transaction, receipt, "CareerAppointmentUnlinked", 204,
                JsonSerializer.Serialize(new Receipt(1, actor.UserId, actor.OwnerId, jobId, linkId, action)));
            if (!CareerAllowed(connection, transaction, actor, action, false)) return Denied<object?>();
            transaction.Commit(); return IdentityOperationResult<object?>.Success(null, 204, "CareerAppointmentUnlinked");
        }
        catch (SqlException) { return Fail<object?>("PersistenceUnavailable", 503); }
    }

    private IdentityOperationResult<object?>? Claim(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string action, Guid jobId, Guid? linkId, string? key, string canonical, out ReceiptClaim claim)
    {
        claim = default; if (string.IsNullOrWhiteSpace(key)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, action, key, canonical, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid) return Fail<object?>("ValidationFailed", 422);
        if (claim.IsConflict) return Fail<object?>("IdempotencyConflict", 409);
        if (!claim.IsReplay) return Fail<object?>("RequestInProgress", 409);
        Receipt? original = null; try { original = JsonSerializer.Deserialize<Receipt>(claim.ResultJson ?? string.Empty); } catch (JsonException) { }
        var unlink = action == "career.appointment.unlink_event";
        if (original is null || original.Version != 1 || original.UserId != actor.UserId || original.OwnerId != actor.OwnerId
            || original.JobId != jobId || original.LinkId == Guid.Empty || original.Action != action || (linkId.HasValue && original.LinkId != linkId.Value)
            || claim.ResultStatusCode != (unlink ? 204 : 201) || claim.ResultCode != (unlink ? "CareerAppointmentUnlinked" : "CareerAppointmentLinked")) return Fail<object?>("ReceiptUnavailable", 503);
        return unlink ? IdentityOperationResult<object?>.Success(null, 204, "CareerAppointmentUnlinked")
            : IdentityOperationResult<object?>.Committed(original.LinkId, 201, "CareerAppointmentLinked");
    }
    private static byte[]? JobETag(SqlConnection connection, SqlTransaction transaction, Guid owner, Guid jobId, bool update, bool allowTrash = false)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = $"SELECT [RowVersion] FROM [career].[JobApplication] {(update ? "WITH(UPDLOCK,HOLDLOCK)" : string.Empty)} WHERE [OwnerId]=@Owner AND [Id]=@Id{(allowTrash ? string.Empty : " AND [IsTrash]=0")};";
        Add(sql, "@Owner", SqlDbType.UniqueIdentifier, owner); Add(sql, "@Id", SqlDbType.UniqueIdentifier, jobId); return sql.ExecuteScalar() as byte[];
    }
    private static void TouchJob(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid jobId)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = "UPDATE [career].[JobApplication] SET [UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User WHERE [OwnerId]=@Owner AND [Id]=@Id; UPDATE [platform].[Resource] SET [Revision]=[Revision]+1,[UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User WHERE [OwnerId]=@Owner AND [Id]=@Id;";
        Add(sql, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Id", SqlDbType.UniqueIdentifier, jobId); sql.ExecuteNonQuery();
    }
    private static void Audit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid id, string action, string? traceId)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = "INSERT INTO [security].[AuditEvent]([ActorUserId],[OwnerUserId],[ActionKey],[TargetType],[TargetId],[Result],[TraceId]) VALUES(@User,@User,@Action,N'CalendarLink',@Id,'Succeeded',@Trace);";
        Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(sql, "@Action", SqlDbType.NVarChar, action); Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Trace", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value); sql.ExecuteNonQuery();
    }
    private static bool ETag(string? raw, out byte[] bytes)
    {
        bytes = []; if (string.IsNullOrWhiteSpace(raw)) return false; var token = raw.Trim(); if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
        try { bytes = Convert.FromBase64String(token[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == token[1..^1]; } catch (FormatException) { return false; }
    }
    private static string Quote(byte[] bytes) => $"\"{Convert.ToBase64String(bytes)}\"";
    private static void Add(SqlCommand sql, string name, SqlDbType type, object value) => sql.Parameters.Add(name, type).Value = value;
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, "Career appointment unavailable or invalid.");
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 409);
    private sealed record Receipt(int Version, Guid UserId, Guid OwnerId, Guid JobId, Guid LinkId, string Action);
}
