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
    string ETag);

public sealed record GoalTargetRecord(
    Guid Id,
    string Kind,
    string Title,
    decimal? InitialValue,
    decimal? CurrentValue,
    decimal? TargetValue,
    decimal Progress,
    DateTimeOffset UpdatedAt,
    string ETag);

public sealed record GoalDetail(GoalRecord Goal, IReadOnlyList<GoalTargetRecord> Targets);

public sealed record GoalPage(IReadOnlyList<GoalRecord> Items, string? NextCursor);

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

public interface IGoalService
{
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
