using Nexora.Application.Identity;
using System.Text.Json.Serialization;

namespace Nexora.Application.Transfer;

public sealed record CalendarExportRange([property: JsonRequired] DateOnly Start, [property: JsonRequired] DateOnly End);
public sealed record CalendarExportFilter(int SchemaVersion, string[] SourceKinds, string[] ManualStatuses,
    string[] TaskStatuses, [property: JsonRequired] CalendarExportRange? Range, string ContainmentMode);
public sealed record CalendarExportRequest(CalendarExportFilter Filter, string PreviewToken, bool Confirmed);
public sealed record CalendarExportPreview(int Count, string TimeZoneId, string PreviewToken, DateTimeOffset ExpiresAt);
public sealed record CalendarExportJob(Guid Id, string State, int Count, CalendarExportFilter Filter,
    string TimeZoneId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string ETag);
public sealed record CalendarExportPage(IReadOnlyList<CalendarExportJob> Items, Guid? NextCursor);
public sealed record CalendarExportAcknowledgement(Guid JobId, string State, int Count, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, string ETag);
// Only the explicit Event business projection reaches the serializer; no entity/owner/resource IDs.
public sealed record CalendarExportEvent(string CalendarUid, string Title, string? Description, string SourceKind,
    string Status, DateTimeOffset StartAt, DateTimeOffset EndAt, DateOnly? StartDate, DateOnly? EndDateExclusive,
    string? TaskPriority, string? ProjectTitle);
public interface ICalendarExportService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<CalendarExportPreview> Preview(IdentityPrincipal actor, CalendarExportFilter filter);
    IdentityOperationResult<CalendarExportAcknowledgement> Request(IdentityPrincipal actor, CalendarExportRequest body, string key, string? trace);
    IdentityOperationResult<CalendarExportPage> List(IdentityPrincipal actor, Guid? cursor, string? state);
    IdentityOperationResult<CalendarExportJob> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<byte[]> Content(IdentityPrincipal actor, Guid id);
}
