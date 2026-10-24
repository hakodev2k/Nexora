using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

public sealed record WorkLog(Guid Id, string Title, DateOnly RecordedOn, int Minutes,
    string? Notes, string? Category, string? Employer, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag, string? PreTrashState = null);
public sealed record WorkLogPage(IReadOnlyList<WorkLog> Items, string? NextCursor);
public sealed record WorkLogVersion(long Sequence, string Event, DateTimeOffset CapturedAt,
    DateTimeOffset SourceUpdatedAt, string Title, DateOnly RecordedOn, int Minutes,
    string? Notes, string? Category, string? Employer, string Status);
public sealed record WorkLogHistoryPage(IReadOnlyList<WorkLogVersion> Items, string? NextCursor);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record WorkLogCommand(string Title, DateOnly? RecordedOn, int? Minutes,
    string? Notes, string? Category, string? Employer);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record WorkLogConfirmation(bool Confirm);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record WorkLogLifecycleCommand(bool Confirm, string PreviewToken);
public sealed record WorkLogLifecyclePreview(Guid Id, string Operation, string ETag, string FromStatus,
    string ToStatus, Guid? BatchId, long HistoryCount, long ReferenceCount, DateTimeOffset ExpiresAt, string PreviewToken);
public sealed record WorkLogDay(DateOnly RecordedOn, long Entries, long Minutes);
public sealed record WorkLogReport(DateOnly From, DateOnly To, string Status, long Entries,
    long GrossMinutes, IReadOnlyList<WorkLogDay> Days, string Mode = "GrossManualMinutes", string Overlap = "NotDeduplicated", string? Category = null);

public interface IWorkLogService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<WorkLogPage> List(IdentityPrincipal actor, string? status, string? query,
        DateOnly? from, DateOnly? to, string? cursor, string? category = null);
    IdentityOperationResult<WorkLog> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<WorkLogHistoryPage> History(IdentityPrincipal actor, Guid id, string? cursor);
    IdentityOperationResult<WorkLogLifecyclePreview> PreviewLifecycle(IdentityPrincipal actor, Guid id, string? ifMatch, string operation);
    IdentityOperationResult<object?> Lifecycle(IdentityPrincipal actor, Guid id, string? ifMatch, string operation,
        WorkLogLifecycleCommand command, string key, string? trace);
    IdentityOperationResult<object?> Save(IdentityPrincipal actor, Guid? id, string? ifMatch,
        WorkLogCommand command, string key, string? trace);
    IdentityOperationResult<object?> Transition(IdentityPrincipal actor, Guid id, string? ifMatch,
        string operation, WorkLogConfirmation command, string key, string? trace);
    IdentityOperationResult<WorkLogReport> Report(IdentityPrincipal actor, DateOnly? from, DateOnly? to, string? status, string? query, string? category = null);
}
