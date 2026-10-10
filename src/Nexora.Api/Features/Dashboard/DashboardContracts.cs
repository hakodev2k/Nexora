namespace Nexora.Api.Features.Dashboard;

public sealed record DashboardItemResponse(
    Guid Id,
    string Kind,
    string Title,
    string? Status,
    DateTimeOffset? At,
    string? Detail);

public sealed record DashboardWidgetResponse(
    string Id,
    string Title,
    string SourceModule,
    string State,
    string? Message,
    DateTimeOffset RefreshedAt,
    int Count,
    IReadOnlyList<DashboardItemResponse> Items);

public sealed record DashboardSnapshotResponse(
    string TimeZoneId,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<DashboardWidgetResponse> Widgets);
