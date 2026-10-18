using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Dashboard;
using Nexora.Application.Identity;
using NodaTime;
using NodaTime.TimeZones;

namespace Nexora.Infrastructure.Dashboard;

public sealed partial class SqlDashboardService
{
    public IdentityOperationResult<DashboardDrilldownPage> Drilldown(IdentityPrincipal actor, string token)
    {
        var scope = _drilldownCursors.Decode(token);
        if (scope is null || scope.UserId != actor.UserId || scope.OwnerId != actor.OwnerId)
            return Failure<DashboardDrilldownPage>("InvalidCursor", 422, "Invalid dashboard window.");
        bool Expired()
        {
            var now = _clock.GetUtcNow();
            return scope.CapturedAt > now || now - scope.CapturedAt >= TimeSpan.FromMinutes(5);
        }
        if (Expired()) return Failure<DashboardDrilldownPage>("DashboardWindowExpired", 410, "Refresh Dashboard to open a current window.");
        try
        {
            using var connection = _connections.Create(); connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "FX26", "dashboard.dashboard.read"))
                return Failure<DashboardDrilldownPage>("ActionDenied", 403, "Dashboard unavailable.");
            var (zoneId, zone) = ReadTimeZone(connection, transaction, actor);
            if (zoneId != scope.TimeZoneId)
                return Failure<DashboardDrilldownPage>("DashboardWindowChanged", 409, "Profile timezone changed. Refresh Dashboard.");
            var date = Instant.FromDateTimeOffset(scope.CapturedAt).InZone(zone).Date;
            var localDate = new DateOnly(date.Year, date.Month, date.Day);
            var startUtc = zone.ResolveLocal(date.AtMidnight(), Resolvers.LenientResolver).ToInstant().ToDateTimeUtc();
            var endUtc = zone.ResolveLocal(date.PlusDays(1).AtMidnight(), Resolvers.LenientResolver).ToInstant().ToDateTimeUtc();
            bool SourceAllowed() => scope.Key switch
            {
                "tasks-due" => Allowed(connection, transaction, actor, "FX12", "tasks.task.read"),
                "calendar-today" => Allowed(connection, transaction, actor, "FX13", "calendar.event.read")
                    && (!scope.IncludeTaskEvents || Allowed(connection, transaction, actor, "FX12", "tasks.task.read")),
                "documents-recent" => Allowed(connection, transaction, actor, "FX20", "documents.page.read", "documents.library.read"),
                "notifications-unread" => Allowed(connection, transaction, actor, "FX06", "notifications.inbox.read"),
                _ => false
            };
            if (!SourceAllowed()) return Failure<DashboardDrilldownPage>("SourceUnavailable", 403, "Widget source unavailable.");
            DashboardWidget ReadPage(int limit, int offset) => scope.Key switch
            {
                "tasks-due" => ReadTasks(connection, transaction, actor, startUtc, endUtc, limit, offset),
                "calendar-today" => ReadCalendar(connection, transaction, actor, startUtc, endUtc, localDate, scope.IncludeTaskEvents, limit, offset),
                "documents-recent" => ReadDocuments(connection, transaction, actor, limit, offset),
                _ => ReadNotifications(connection, transaction, actor, limit, offset)
            };
            var widget = ReadPage(25, scope.Offset);
            if (widget.State == "Degraded") return Failure<DashboardDrilldownPage>("PersistenceUnavailable", 503, "Widget source temporarily unavailable.");
            if (widget.State == "Unavailable") return Failure<DashboardDrilldownPage>("SourceUnavailable", 403, "Widget source unavailable.");
            if (scope.Offset > 0 && widget.Items.Count == 0)
            {
                var first = ReadPage(1, 0);
                if (first.State is "Degraded" or "Unavailable")
                    return Failure<DashboardDrilldownPage>("SourceUnavailable", 503, "Widget source unavailable.");
                widget = widget with { Count = first.Count, State = first.State };
            }
            var next = (long)scope.Offset + widget.Items.Count < widget.Count && widget.Items.Count == 25
                ? _drilldownCursors.Encode(scope with { Offset = checked(scope.Offset + 25) }) : null;
            var previous = scope.Offset > 0 ? _drilldownCursors.Encode(scope with { Offset = Math.Max(0, scope.Offset - 25) }) : null;
            if (!SourceAllowed() || !Allowed(connection, transaction, actor, "FX26", "dashboard.dashboard.read"))
                return Failure<DashboardDrilldownPage>("SourceUnavailable", 403, "Widget source unavailable.");
            if (Expired()) return Failure<DashboardDrilldownPage>("DashboardWindowExpired", 410, "Refresh Dashboard to open a current window.");
            transaction.Commit();
            return IdentityOperationResult<DashboardDrilldownPage>.Success(new(widget, zoneId, scope.CapturedAt, localDate, next, previous));
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException)
        {
            return Failure<DashboardDrilldownPage>("PersistenceUnavailable", 503, "Widget source temporarily unavailable.");
        }
    }
}
