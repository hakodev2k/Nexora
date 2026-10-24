using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LearningPlanMetadata(string Title, DateOnly? StartsOn, DateOnly? EndsOn, string? Notes);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LearningPlanSourceRef(string Kind, Guid Id);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LearningPlanLink(LearningPlanSourceRef Source, DateOnly? TargetOn);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LearningPlanOrder(IReadOnlyList<Guid> ItemIds);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LearningPlanConfirmation(bool Confirm);
public sealed record LearningPlan(Guid Id, string Title, DateOnly? StartsOn, DateOnly? EndsOn, string? Notes,
    string Status, DateTimeOffset UpdatedAt, string ETag);
public sealed record LearningPlanPage(IReadOnlyList<LearningPlan> Items, string? NextCursor);
public sealed record LearningPlanSource(string Status, string? Title, string? SourceState, decimal? Percent);
public sealed record LearningPlanItem(Guid Id, LearningPlanSourceRef Source, int Position, DateOnly? TargetOn,
    LearningPlanSource Projection);
public sealed record LearningPlanDetail(LearningPlan Plan, IReadOnlyList<LearningPlanItem> Items, int Known, int Unavailable,
    string ProgressLabel);
public sealed record LearningPlanSourceChoice(LearningPlanSourceRef Source, string Title);
public sealed record LearningPlanSourcePage(IReadOnlyList<LearningPlanSourceChoice> Items, string? NextCursor);

/// <summary>Registration is gated separately from business/action grants. Default is inert.</summary>
public sealed record LearningPlanReadiness(bool ReviewedLocalContract = false);

public interface ILearningPlanService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<LearningPlanPage> List(IdentityPrincipal actor, string? status, string? cursor);
    IdentityOperationResult<LearningPlanDetail> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<LearningPlanSourcePage> Sources(IdentityPrincipal actor, string kind, string? cursor);
    IdentityOperationResult<object?> Create(IdentityPrincipal actor, LearningPlanMetadata body, string key, string? trace);
    IdentityOperationResult<object?> Update(IdentityPrincipal actor, Guid id, string? etag, LearningPlanMetadata body, string key, string? trace);
    IdentityOperationResult<object?> Link(IdentityPrincipal actor, Guid id, string? etag, LearningPlanLink body, string key, string? trace);
    IdentityOperationResult<object?> Unlink(IdentityPrincipal actor, Guid id, Guid itemId, string? etag, string key, string? trace);
    IdentityOperationResult<object?> Reorder(IdentityPrincipal actor, Guid id, string? etag, LearningPlanOrder body, string key, string? trace);
    IdentityOperationResult<object?> Complete(IdentityPrincipal actor, Guid id, string? etag, LearningPlanConfirmation body, string key, string? trace);
}

public static class LearningPlanRules
{
    public static bool ValidSource(LearningPlanSourceRef? source) => source is not null && source.Id != Guid.Empty && IsKind(source.Kind);
    public static bool IsKind(string? kind) => kind is "Goal" or "Task" or "Course";
    public static bool ValidMetadata(LearningPlanMetadata? body) => body is not null &&
        !string.IsNullOrWhiteSpace(body.Title) && body.Title.Trim().Length <= 200 && body.Notes is not { Length: > 20000 } &&
        (!body.StartsOn.HasValue || !body.EndsOn.HasValue || body.StartsOn <= body.EndsOn);
    public static bool ExactOrder(IReadOnlyList<Guid>? proposed, IReadOnlyList<Guid> current) => proposed is not null &&
        proposed.Count == current.Count && proposed.All(id => id != Guid.Empty) &&
        proposed.Distinct().Count() == proposed.Count && proposed.ToHashSet().SetEquals(current);
    public static bool TryRevision(string? tag, out byte[] bytes)
    {
        bytes = [];
        if (tag is null || tag.Length != 14 || tag[0] != '"' || tag[^1] != '"') return false;
        try { bytes = Convert.FromBase64String(tag[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == tag[1..^1]; }
        catch (FormatException) { return false; }
    }
    public static string ProgressLabel(int known, int unavailable) => known + unavailable == 0 ? "No items" :
        unavailable > 0 ? $"Partially known: Known {known} / Unavailable {unavailable}" : $"Known {known} / Unavailable 0";
}
