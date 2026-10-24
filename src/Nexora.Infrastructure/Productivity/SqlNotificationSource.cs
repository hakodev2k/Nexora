using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Notifications;

namespace Nexora.Infrastructure.Productivity;

public sealed partial class SqlProductivityService
{
    internal NotificationSource? ResolveNotificationSourceWithin(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string reference)
    {
        var parts = reference.Split(':');
        if (parts.Length != 2 || (!Guid.TryParseExact(parts[1], "N", out var id) && !Guid.TryParseExact(parts[1], "D", out id)) || id == Guid.Empty) return null;
        if (parts[0] == "Task")
        {
            var source = new SqlPlannerTaskSource(_capabilities).Read(connection, transaction, actor, id);
            return source is null ? null : new("Task", id, source.Title, $"/tasks/{id:D}");
        }
        if (parts[0] == "CalendarEvent")
        {
            var source = ReadPersonalEventWithin(connection, transaction, actor, id);
            return source is null ? null : new("Event", id, source.Title, $"/calendar/events/{id:D}");
        }
        return null;
    }
}
