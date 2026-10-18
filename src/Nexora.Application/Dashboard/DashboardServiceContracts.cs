using Nexora.Application.Identity;

namespace Nexora.Application.Dashboard;

/// <summary>
/// Read-only attention data for the owner dashboard. Each widget carries its
/// own state so a source-module failure cannot hide data from other widgets.
/// Source records remain authoritative; this contract is not a new business
/// aggregate or a write model.
/// </summary>
public sealed record DashboardItem(
    Guid Id,
    string Kind,
    string Title,
    string? Status,
    DateTimeOffset? At,
    string? Detail,
    DateOnly? StartDate = null,
    DateOnly? EndDateExclusive = null,
    DateTimeOffset? EndAt = null);

public sealed record DashboardWidget(
    string Id,
    string Title,
    string SourceModule,
    string State,
    string? Message,
    DateTimeOffset RefreshedAt,
    int Count,
    IReadOnlyList<DashboardItem> Items,
    string? DrilldownToken = null);

public sealed record DashboardSnapshot(
    string TimeZoneId,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<DashboardWidget> Widgets);

public interface IDashboardService
{
    IdentityOperationResult<DashboardCreateTarget> OpenCreate(IdentityPrincipal actor, string resourceType);
    IdentityOperationResult<DashboardDrilldownPage> Drilldown(IdentityPrincipal actor, string token);
    IdentityOperationResult<DashboardSnapshot> Get(IdentityPrincipal actor);
    IdentityOperationResult<DashboardSnapshot> Refresh(IdentityPrincipal actor);
}

public sealed record DashboardCreateTarget(string ResourceType, string ModuleCode);
public sealed record DashboardDrilldownPage(DashboardWidget Widget, string TimeZoneId,
    DateTimeOffset CapturedAt, DateOnly LocalDate, string? NextToken, string? PreviousToken);
