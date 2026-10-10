using Nexora.Application.Identity;

namespace Nexora.Application.Transfer;

public sealed record CalendarImportPreview(Guid FileId, string FileETag);
public sealed record ImportConfirmation(bool Confirmed);
public sealed record ImportAcknowledgement(Guid BatchId, string State, int AcceptedCount, int SkippedCount, int AppliedCount, string ETag, int TotalCount);
public sealed record ImportBatch(Guid Id, Guid FileId, string State, string TimeZoneId, int AcceptedCount, int SkippedCount, int AppliedCount, DateTimeOffset CreatedAt, string ETag, int TotalCount);
public sealed record ImportBatchPage(IReadOnlyList<ImportBatch> Items, Guid? NextCursor);
public sealed record CalendarImportCandidate(int SchemaVersion, int RowNumber, string Title, string Description, string Uid,
    string TimeZoneId, bool IsAllDay, DateTimeOffset StartAt, DateTimeOffset EndAt, DateOnly? StartDate, DateOnly? EndDateExclusive);
public sealed record ImportRow(int RowNumber, string Outcome, string? ReasonCode, IReadOnlyList<string> Warnings, CalendarImportCandidate? Candidate, Guid? ResultResourceId);
public sealed record ImportRowPage(IReadOnlyList<ImportRow> Items, int? NextCursor);
public interface ICalendarImportService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<ImportAcknowledgement> Preview(IdentityPrincipal actor, CalendarImportPreview body, string key, string? trace);
    IdentityOperationResult<ImportBatchPage> List(IdentityPrincipal actor, Guid? cursor, string? state);
    IdentityOperationResult<ImportBatch> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<ImportRowPage> Rows(IdentityPrincipal actor, Guid id, int? cursor, string? outcome);
    IdentityOperationResult<ImportAcknowledgement> Commit(IdentityPrincipal actor, Guid id, string? etag, ImportConfirmation body, string key, string? trace);
    IdentityOperationResult<ImportAcknowledgement> Cancel(IdentityPrincipal actor, Guid id, string? etag, ImportConfirmation body, string key, string? trace);
}
