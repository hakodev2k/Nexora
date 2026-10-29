using System.Text.Json;
using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Dashboard;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DashboardWidgetCommand(Guid? Id, string Key, string ProviderVersion, int ConfigVersion,
    int WidthUnits, bool Enabled, JsonElement Options);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DashboardLayoutCommand(int SchemaVersion, IReadOnlyList<DashboardWidgetCommand> Widgets);
public sealed record DashboardLayout(Guid Id, int SchemaVersion, long LayoutVersion, IReadOnlyList<DashboardWidgetCommand> Widgets, string ETag);
public sealed record DashboardLayoutState(DashboardLayout? Layout);
public interface IDashboardLayoutService
{
    IdentityOperationResult<DashboardLayoutState> Get(IdentityPrincipal actor);
    IdentityOperationResult<DashboardLayout> Save(IdentityPrincipal actor, bool create, string? ifMatch, DashboardLayoutCommand command,
        string? key = null, string? traceId = null);
}
