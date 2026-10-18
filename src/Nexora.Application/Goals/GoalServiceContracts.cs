using Nexora.Application.Identity;

namespace Nexora.Application.Goals;

public sealed record GoalRecord(
    Guid Id,
    string Title,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string Status,
    decimal Progress,
    int TargetCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string ETag,
    string? PreArchiveStatus = null);

public sealed record GoalTargetRecord(
    Guid Id,
    string Kind,
    string Title,
    decimal? InitialValue,
    decimal? CurrentValue,
    decimal? TargetValue,
    decimal Progress,
    DateTimeOffset UpdatedAt,
    string ETag,
    bool? BooleanValue = null);

public sealed record GoalDetail(GoalRecord Goal, IReadOnlyList<GoalTargetRecord> Targets);

public sealed record GoalPage(IReadOnlyList<GoalRecord> Items, string? NextCursor);
public sealed record GoalProgressEntry(Guid Id, Guid TargetId, string Kind, string CurrentTargetTitle,
    string? ValueExact, bool? Checked, string? Note, DateTimeOffset RecordedAt, bool TargetRemoved = false);
public sealed record GoalProgressPage(IReadOnlyList<GoalProgressEntry> Items, string? NextCursor);
public sealed record GoalStateEntry(Guid Id, string ActionKey, string FromStatus, string ToStatus, DateTimeOffset RecordedAt);
public sealed record GoalStatePage(IReadOnlyList<GoalStateEntry> Items, string? NextCursor);
public sealed record GoalMetadataEntry(Guid Id,string Event,string Title,string? Description,DateOnly? StartDate,DateOnly? EndDate,
    DateTimeOffset CapturedAt,DateTimeOffset SourceUpdatedAt);
public sealed record GoalMetadataPage(IReadOnlyList<GoalMetadataEntry> Items,string? NextCursor);
public sealed record GoalTargetDefinitionEntry(Guid Id,Guid TargetId,string Event,string Kind,string Title,string? InitialValueExact,
    string? TargetValueExact,bool Removed,DateTimeOffset CapturedAt,DateTimeOffset SourceUpdatedAt);
public sealed record GoalTargetDefinitionPage(IReadOnlyList<GoalTargetDefinitionEntry> Items,string? NextCursor);

public sealed record GoalCommand(
    string Title,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record NumericTargetCommand(
    string Title,
    decimal InitialValue,
    decimal CurrentValue,
    decimal TargetValue);

public sealed record NumericProgressCommand(decimal CurrentValue, string? Note);
public sealed record BooleanTargetCommand(string Title, bool BooleanValue);
public sealed record BooleanProgressCommand(bool BooleanValue, string? Note);
public sealed record GoalTargetChange(string Mode, string? Title, decimal? InitialValue, decimal? TargetValue);
public sealed record GoalTargetChangePreview(string PreviewToken, DateTimeOffset ExpiresAt,
    string GoalETag, string TargetETag, string TargetTitle, string Kind,
    int BeforeCount, int AfterCount, decimal BeforeProgress, decimal AfterProgress,
    string? ProposedTitle, string? InitialValueExact, string? TargetValueExact);

public interface IGoalService
{
    IdentityOperationResult<GoalStatePage> StateHistory(IdentityPrincipal actor, Guid goalId, string? cursor = null);
    IdentityOperationResult<GoalMetadataPage> MetadataHistory(IdentityPrincipal actor, Guid goalId, string? cursor = null);
    IdentityOperationResult<GoalTargetDefinitionPage> TargetDefinitionHistory(IdentityPrincipal actor,Guid goalId,string? cursor=null);
    IdentityOperationResult<GoalDetail> CreateNumericTarget(IdentityPrincipal actor, Guid goalId,
        string? ifMatch, NumericTargetCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalTargetChangePreview> PreviewTargetChange(IdentityPrincipal actor, Guid goalId,
        Guid targetId, string? ifMatch, GoalTargetChange command);
    IdentityOperationResult<GoalDetail> ChangeTarget(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string mode, string? ifMatch, string previewToken, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalProgressPage> ProgressHistory(IdentityPrincipal actor, Guid goalId, string? cursor = null);
    IdentityOperationResult<GoalDetail> CreateBooleanTarget(IdentityPrincipal actor, Guid goalId,
        string? ifMatch, BooleanTargetCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalDetail> RecordBooleanProgress(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string? ifMatch, BooleanProgressCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalPage> List(IdentityPrincipal actor, string? status = null, string? query = null, int? limit = null);
    IdentityOperationResult<GoalDetail> Get(IdentityPrincipal actor, Guid goalId);
    IdentityOperationResult<GoalDetail> Create(IdentityPrincipal actor, GoalCommand command, NumericTargetCommand? target = null,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalDetail> Update(IdentityPrincipal actor, Guid goalId, string? ifMatch, GoalCommand command,
        string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalDetail> RecordNumericProgress(IdentityPrincipal actor, Guid goalId, Guid targetId,
        string? ifMatch, NumericProgressCommand command, string? idempotencyKey = null, string? traceId = null);
    IdentityOperationResult<GoalDetail> Transition(IdentityPrincipal actor, Guid goalId, string? ifMatch, string status,
        string? idempotencyKey = null, string? traceId = null);
}
