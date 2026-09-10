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
    string? Detail);

public sealed record DashboardWidget(
    string Id,
    string Title,
    string SourceModule,
    string State,
    string? Message,
    DateTimeOffset RefreshedAt,
    int Count,
    IReadOnlyList<DashboardItem> Items);

public sealed record DashboardSnapshot(
    string TimeZoneId,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<DashboardWidget> Widgets);

public interface IDashboardService
{
    IdentityOperationResult<DashboardSnapshot> Get(IdentityPrincipal actor);
}
