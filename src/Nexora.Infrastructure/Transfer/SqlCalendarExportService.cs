using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;

namespace Nexora.Infrastructure.Transfer;

public sealed class SqlCalendarExportService : ICalendarExportService
{
    private static readonly string[] Actions = ["transfer.export.request", "transfer.export.read", "transfer.export.download"];
    private readonly SqlConnectionFactory connections;
    private readonly TimeProvider clock;
    private readonly SqlImportAuthority authority;
    private readonly CalendarExportPreviewToken tokens;
    private readonly SqlRequestReceiptStore receipts;
    private readonly SqlCalendarExportParticipant calendar = new();
    private readonly SqlExportJobStore jobs = new();
    public SqlCalendarExportService(SqlConnectionFactory connections, string secret, TimeProvider clock)
    { this.connections = connections; this.clock = clock; authority = new(connections, clock); tokens = new(secret); receipts = new(secret); }
    private SqlImportUnit Open(IdentityPrincipal actor) => new(connections, actor, clock.GetUtcNow().UtcDateTime);
    private static IdentityOperationResult<T> Error<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Error<T>("ModuleUnavailable", 403, "The module, action or selected source is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Error<T>("ResourceUnavailable", 404, "The resource is unavailable.");
    private static IdentityOperationResult<T> Invalid<T>() => Error<T>("ValidationFailed", 422, "Check the explicit source, status, range and confirmation.");
    private static IdentityOperationResult<T> Conflict<T>() => Error<T>("RevisionConflict", 409, "The source or preview changed. Create a fresh preview.");
    private bool SourceAllowed(SqlImportUnit unit, string kind) => authority.Allowed(unit, "FX13", "calendar.ics.export") &&
        (kind == "Manual" ? authority.Allowed(unit, "FX13", "calendar.event.read") :
        authority.Allowed(unit, "FX12", "tasks.task.read") && authority.Allowed(unit, "FX11", "projects.project.read"));
    private bool Required(SqlImportUnit unit, string action, CalendarExportFilter filter) => authority.AccountZone(unit) is not null &&
        authority.Allowed(unit, "FX10", action) && filter.SourceKinds.All(kind => SourceAllowed(unit, kind));
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        using var unit = Open(actor); if (authority.AccountZone(unit) is null) return Denied<IReadOnlyDictionary<string, bool>>();
        var manual = SourceAllowed(unit, "Manual"); var task = SourceAllowed(unit, "Task");
        var result = Actions.ToDictionary(action => action, action => (manual || task) && authority.Allowed(unit, "FX10", action));
        result["calendar.ics.export"] = manual || task; result["source.Manual"] = manual; result["source.Task"] = task;
        return result.Where(x => Actions.Contains(x.Key)).Any(x => x.Value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<CalendarExportPreview> Preview(IdentityPrincipal actor, CalendarExportFilter body)
    {
        var filter = CalendarExportSelection.Normalize(body); if (filter is null) return Invalid<CalendarExportPreview>();
        using var unit = Open(actor); if (!Required(unit, "transfer.export.request", filter)) return Denied<CalendarExportPreview>();
        var zone = authority.AccountZone(unit)!; var snapshot = calendar.Snapshot(unit, filter, zone);
        if (snapshot.Error is { } error) return SourceError<CalendarExportPreview>(error);
        var serialized = CalendarIcsSerializer.Serialize(snapshot.Events, zone, new(unit.Now, TimeSpan.Zero));
        if (serialized.Reason is { } reason) return SourceError<CalendarExportPreview>(reason);
        if (!Required(unit, "transfer.export.request", filter)) return Denied<CalendarExportPreview>();
        var expires = clock.GetUtcNow().AddMinutes(2); var claim = new ExportPreviewClaim(actor.OwnerId, actor.UserId, actor.SessionId, CalendarExportSelection.Digest(filter), snapshot.Digest, zone, expires);
        return IdentityOperationResult<CalendarExportPreview>.Success(new(snapshot.Events.Count, zone, tokens.Sign(claim), expires));
    }
    public IdentityOperationResult<CalendarExportAcknowledgement> Request(IdentityPrincipal actor, CalendarExportRequest body, string key, string? trace)
    {
        var filter = CalendarExportSelection.Normalize(body.Filter); if (filter is null || !body.Confirmed) return Invalid<CalendarExportAcknowledgement>();
        try
        {
            using var unit = Open(actor); if (!Required(unit, "transfer.export.request", filter)) return Denied<CalendarExportAcknowledgement>();
            var claim = receipts.TryClaim(unit.Connection, unit.Transaction, actor.UserId, "transfer.export.request", key,
                JsonSerializer.Serialize(body with { Filter = filter }, SqlImportBatchStore.Json), unit.Now);
            if (claim.IsInvalid || !claim.IsClaimed && !claim.IsReplay && !claim.IsConflict) return Invalid<CalendarExportAcknowledgement>();
            if (claim.IsConflict) return Error<CalendarExportAcknowledgement>("IdempotencyConflict", 409, "The request key belongs to another request.");
            if (claim.IsReplay)
            {
                var ack = claim.ResultJson is null ? null : JsonSerializer.Deserialize<CalendarExportAcknowledgement>(claim.ResultJson, SqlImportBatchStore.Json);
                var stored = ack is null ? null : jobs.Get(unit, ack.JobId);
                if (stored is null || !Available(unit, stored)) return Missing<CalendarExportAcknowledgement>();
                if (!Required(unit, "transfer.export.request", stored.Filter)) return Denied<CalendarExportAcknowledgement>();
                return IdentityOperationResult<CalendarExportAcknowledgement>.Success(ack!, claim.ResultStatusCode ?? 200);
            }
            var preview = tokens.Read(body.PreviewToken); var zone = authority.AccountZone(unit)!;
            if (preview is null || preview.Owner != actor.OwnerId || preview.User != actor.UserId || preview.Session != actor.SessionId ||
                preview.ExpiresAt <= clock.GetUtcNow() || preview.Zone != zone || preview.FilterDigest != CalendarExportSelection.Digest(filter)) return Conflict<CalendarExportAcknowledgement>();
            var snapshot = calendar.Snapshot(unit, filter, zone); if (snapshot.Error is { } error) return SourceError<CalendarExportAcknowledgement>(error);
            if (snapshot.Digest != preview.CohortDigest) return Conflict<CalendarExportAcknowledgement>();
            var serialized = CalendarIcsSerializer.Serialize(snapshot.Events, zone, new(unit.Now, TimeSpan.Zero));
            if (serialized.Reason is { } reason) return SourceError<CalendarExportAcknowledgement>(reason);
            var id = jobs.Create(unit, filter, zone, snapshot, serialized.Content!);
            if (!Required(unit, "transfer.export.request", filter) || !calendar.Available(unit, snapshot.Sources)) return Denied<CalendarExportAcknowledgement>();
            var result = Ack(jobs.Get(unit, id)!);
            authority.Audit(unit, id, "transfer.export.request", trace, "ExportJob");
            receipts.Complete(unit.Connection, unit.Transaction, claim, "ExportAcknowledged", 201, JsonSerializer.Serialize(result, SqlImportBatchStore.Json));
            // The live session can expire during serialization; recheck immediately before commit.
            if (!Required(unit, "transfer.export.request", filter)) return Denied<CalendarExportAcknowledgement>();
            unit.Transaction.Commit(); return IdentityOperationResult<CalendarExportAcknowledgement>.Success(result, 201);
        }
        catch (SqlException error) when (error.Number == 1205) { return Error<CalendarExportAcknowledgement>("RetryableConflict", 409, "The export conflicted. Retry using the same request key."); }
    }
    public IdentityOperationResult<CalendarExportPage> List(IdentityPrincipal actor, Guid? cursor, string? state)
    {
        using var unit = Open(actor);
        if (authority.AccountZone(unit) is null || !authority.Allowed(unit, "FX10", "transfer.export.read") || !authority.Allowed(unit, "FX13", "calendar.ics.export")) return Denied<CalendarExportPage>();
        if (state is not (null or "Ready" or "Expired")) return Invalid<CalendarExportPage>();
        var stored = jobs.List(unit); if (stored is null) return SourceError<CalendarExportPage>("SourceLimitExceeded");
        var projectionInstant = clock.GetUtcNow();
        var visible = stored.Where(job => Required(unit, "transfer.export.read", job.Filter) && Available(unit, job))
            .Select(job => job.ExpiresAt <= projectionInstant ? job with { State = "Expired" } : job)
            .Where(job => state is null || job.State == state).ToArray();
        var start = cursor is null ? 0 : Array.FindIndex(visible, job => job.Id == cursor) + 1;
        if (cursor is not null && start == 0) return Missing<CalendarExportPage>();
        var page = visible.Skip(start).Take(26).ToArray();
        if (authority.AccountZone(unit) is null) return Denied<CalendarExportPage>();
        return IdentityOperationResult<CalendarExportPage>.Success(new(page.Take(25).ToArray(), page.Length > 25 ? page[24].Id : null));
    }
    public IdentityOperationResult<CalendarExportJob> Get(IdentityPrincipal actor, Guid id)
    {
        using var unit = Open(actor); var job = jobs.Get(unit, id); if (job is null) return Missing<CalendarExportJob>();
        if (!Required(unit, "transfer.export.read", job.Filter)) return Denied<CalendarExportJob>();
        if (!Available(unit, job)) return Missing<CalendarExportJob>();
        if (!Required(unit, "transfer.export.read", job.Filter)) return Denied<CalendarExportJob>();
        return IdentityOperationResult<CalendarExportJob>.Success(Effective(job));
    }
    public IdentityOperationResult<byte[]> Content(IdentityPrincipal actor, Guid id)
    {
        using var unit = Open(actor); var job = jobs.Get(unit, id); if (job is null) return Missing<byte[]>();
        if (!Required(unit, "transfer.export.download", job.Filter)) return Denied<byte[]>();
        if (!Available(unit, job)) return Missing<byte[]>();
        if (job.ExpiresAt <= clock.GetUtcNow()) return Error<byte[]>("ArtifactExpired", 410, "The export download has expired. Generate a new export.");
        var bytes = jobs.Content(unit, id); if (bytes is null) return Missing<byte[]>();
        if (!Required(unit, "transfer.export.download", job.Filter)) return Denied<byte[]>();
        if (job.ExpiresAt <= clock.GetUtcNow()) return Error<byte[]>("ArtifactExpired", 410, "The export download has expired. Generate a new export.");
        return IdentityOperationResult<byte[]>.Success(bytes);
    }
    private bool Available(SqlImportUnit unit, CalendarExportJob job)
    { var sources = jobs.Sources(unit, job.Id); return sources.Count == job.Count && calendar.Available(unit, sources); }
    private CalendarExportJob Effective(CalendarExportJob job) => job.ExpiresAt <= clock.GetUtcNow() ? job with { State = "Expired" } : job;
    private static CalendarExportAcknowledgement Ack(CalendarExportJob job) => new(job.Id, job.State, job.Count, job.CreatedAt, job.ExpiresAt, job.ETag);
    private static IdentityOperationResult<T> SourceError<T>(string reason) => Error<T>(reason, 422, "The saved Calendar source cannot be exported with this bounded ICS format. Check precision, range or limits.");
}
