namespace Nexora.Api.Features.Dashboard;

public sealed record DashboardItemResponse(
    Guid Id,
    string Kind,
    string Title,
    string? Status,
    DateTimeOffset? At,
    string? Detail,
    DateOnly? StartDate = null,
    DateOnly? EndDateExclusive = null,
    DateTimeOffset? EndAt = null);

public sealed record DashboardWidgetResponse(
    string Id,
    string Title,
    string SourceModule,
    string State,
    string? Message,
    DateTimeOffset RefreshedAt,
    int Count,
    IReadOnlyList<DashboardItemResponse> Items,
    string? DrilldownToken = null);

public sealed record DashboardSnapshotResponse(
    string TimeZoneId,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<DashboardWidgetResponse> Widgets);
