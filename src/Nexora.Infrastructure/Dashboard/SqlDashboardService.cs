using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Dashboard;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Dashboard;

/// <summary>
/// Read-only owner dashboard. The source modules remain authoritative: this
/// service only projects bounded attention widgets and never stores or mutates
/// a duplicate business state. Each widget is isolated so a missing/failed
/// source is represented as degraded data instead of taking down the page.
/// </summary>
public sealed class SqlDashboardService : IDashboardService
{
    private const int ItemLimit = 5;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;

    public SqlDashboardService(SqlConnectionFactory connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<DashboardSnapshot> Get(IdentityPrincipal actor)
    {
        if (!ModuleAvailable(actor, "FX26", "dashboard.dashboard.read"))
            return Failure<DashboardSnapshot>("ModuleUnavailable", 409, "Dashboard is disabled or unavailable for this user.");

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            var (timeZoneId, timeZone) = ReadTimeZone(connection, actor.UserId);
            var generatedAt = DateTimeOffset.UtcNow;
            var localNow = TimeZoneInfo.ConvertTime(generatedAt, timeZone);
            var startLocal = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddDays(1);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone);

            var widgets = new[]
            {
                ReadTasks(connection, actor, startUtc, endUtc),
                ReadCalendar(connection, actor, startUtc, endUtc),
                ReadDocuments(connection, actor),
                ReadNotifications(connection, actor)
            };
            return IdentityOperationResult<DashboardSnapshot>.Success(
                new DashboardSnapshot(timeZoneId, generatedAt, widgets));
        }
        catch (SqlException)
        {
            return Failure<DashboardSnapshot>("PersistenceUnavailable", 503, "Dashboard sources are unavailable.");
        }
    }

    private DashboardWidget ReadTasks(SqlConnection connection, IdentityPrincipal actor,
        DateTime startUtc, DateTime endUtc)
    {
        const string module = "FX12";
        const string widgetId = "tasks-due";
        var refreshedAt = DateTimeOffset.UtcNow;
        if (!ModuleAvailable(actor, module, "tasks.task.read", "tasks.view"))
            return Unavailable(widgetId, "Tasks due and overdue", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT TOP (@Limit)
                    t.[Id], t.[Title], t.[Status], t.[DueAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [productivity].[Task] t WITH (NOLOCK)
                WHERE t.[OwnerId] = @OwnerId
                  AND t.[Status] NOT IN ('Completed','Skipped','Deleted')
                  AND t.[DueAt] IS NOT NULL
                  AND t.[DueAt] < @EndUtc
                ORDER BY CASE WHEN t.[DueAt] < @StartUtc THEN 0 ELSE 1 END,
                         t.[DueAt], t.[Id];
                """;
            Add(command, "@Limit", SqlDbType.Int, ItemLimit);
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@StartUtc", SqlDbType.DateTime2, startUtc);
            Add(command, "@EndUtc", SqlDbType.DateTime2, endUtc);
            var items = new List<DashboardItem>();
            var count = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                count = Convert.ToInt32(reader.GetInt64(4));
                items.Add(new DashboardItem(reader.GetGuid(0), "Task", reader.GetString(1),
                    reader.GetString(2), FromUtc(reader.GetDateTime(3)), null));
            }
            return Widget(widgetId, "Tasks due and overdue", module, refreshedAt, count, items);
        }
        catch (SqlException)
        {
            return Degraded(widgetId, "Tasks due and overdue", module, refreshedAt,
                "Tasks are temporarily unavailable.");
        }
    }

    private DashboardWidget ReadCalendar(SqlConnection connection, IdentityPrincipal actor,
        DateTime startUtc, DateTime endUtc)
    {
        const string module = "FX13";
        const string widgetId = "calendar-today";
        var refreshedAt = DateTimeOffset.UtcNow;
        if (!ModuleAvailable(actor, module, "calendar.event.read", "calendar.view"))
            return Unavailable(widgetId, "Calendar today", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT TOP (@Limit)
                    e.[Id], e.[Title], e.[Status], e.[StartAt], e.[EndAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [calendar].[Event] e WITH (NOLOCK)
                WHERE e.[OwnerId] = @OwnerId
                  AND e.[Status] IN ('Scheduled','Completed')
                  AND e.[StartAt] < @EndUtc
                  AND e.[EndAt] > @StartUtc
                ORDER BY e.[StartAt], e.[Id];
                """;
            Add(command, "@Limit", SqlDbType.Int, ItemLimit);
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@StartUtc", SqlDbType.DateTime2, startUtc);
            Add(command, "@EndUtc", SqlDbType.DateTime2, endUtc);
            var items = new List<DashboardItem>();
            var count = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                count = Convert.ToInt32(reader.GetInt64(5));
                var detail = $"Ends {FromUtc(reader.GetDateTime(4)):u}";
                items.Add(new DashboardItem(reader.GetGuid(0), "Event", reader.GetString(1),
                    reader.GetString(2), FromUtc(reader.GetDateTime(3)), detail));
            }
            return Widget(widgetId, "Calendar today", module, refreshedAt, count, items);
        }
        catch (SqlException)
        {
            return Degraded(widgetId, "Calendar today", module, refreshedAt,
                "Calendar is temporarily unavailable.");
        }
    }

    private DashboardWidget ReadDocuments(SqlConnection connection, IdentityPrincipal actor)
    {
        const string module = "FX20";
        const string widgetId = "documents-recent";
        var refreshedAt = DateTimeOffset.UtcNow;
        if (!ModuleAvailable(actor, module, "documents.page.read", "documents.library.read"))
            return Unavailable(widgetId, "Recent documents", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT TOP (@Limit)
                    p.[Id], p.[Title], p.[DocumentType], p.[Status], p.[UpdatedAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [documents].[Page] p WITH (NOLOCK)
                WHERE p.[OwnerId] = @OwnerId
                  AND p.[Status] IN ('Draft','Published')
                ORDER BY p.[UpdatedAt] DESC, p.[Id] DESC;
                """;
            Add(command, "@Limit", SqlDbType.Int, ItemLimit);
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            var items = new List<DashboardItem>();
            var count = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                count = Convert.ToInt32(reader.GetInt64(5));
                items.Add(new DashboardItem(reader.GetGuid(0), reader.GetString(2), reader.GetString(1),
                    reader.GetString(3), FromUtc(reader.GetDateTime(4)), null));
            }
            return Widget(widgetId, "Recent documents", module, refreshedAt, count, items);
        }
        catch (SqlException)
        {
            return Degraded(widgetId, "Recent documents", module, refreshedAt,
                "Documents are temporarily unavailable.");
        }
    }

    private DashboardWidget ReadNotifications(SqlConnection connection, IdentityPrincipal actor)
    {
        const string module = "FX06";
        const string widgetId = "notifications-unread";
        var refreshedAt = DateTimeOffset.UtcNow;
        if (!ModuleAvailable(actor, module, "notifications.inbox.read", "notifications.view"))
            return Unavailable(widgetId, "Unread notifications", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT TOP (@Limit)
                    n.[Id], n.[Title], n.[Kind], n.[CreatedAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [notifications].[Notification] n WITH (NOLOCK)
                WHERE n.[OwnerUserId] = @UserId
                  AND n.[ReadAt] IS NULL
                  AND n.[DeletedAt] IS NULL
                ORDER BY n.[CreatedAt] DESC, n.[Id] DESC;
                """;
            Add(command, "@Limit", SqlDbType.Int, ItemLimit);
            Add(command, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
            var items = new List<DashboardItem>();
            var count = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                count = Convert.ToInt32(reader.GetInt64(4));
                items.Add(new DashboardItem(reader.GetGuid(0), reader.GetString(2), reader.GetString(1),
                    "Unread", FromUtc(reader.GetDateTime(3)), null));
            }
            return Widget(widgetId, "Unread notifications", module, refreshedAt, count, items);
        }
        catch (SqlException)
        {
            return Degraded(widgetId, "Unread notifications", module, refreshedAt,
                "Notifications are temporarily unavailable.");
        }
    }

    private static (string Id, TimeZoneInfo Zone) ReadTimeZone(SqlConnection connection, Guid userId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT [TimeZoneId] FROM [identity].[User] WHERE [Id] = @UserId AND [IsDeleted] = 0;";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        var raw = command.ExecuteScalar() as string;
        if (string.IsNullOrWhiteSpace(raw))
            return ("UTC", TimeZoneInfo.Utc);
        try
        {
            return (raw, TimeZoneInfo.FindSystemTimeZoneById(raw));
        }
        catch (TimeZoneNotFoundException)
        {
            return ("UTC", TimeZoneInfo.Utc);
        }
        catch (InvalidTimeZoneException)
        {
            return ("UTC", TimeZoneInfo.Utc);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys)
    {
        try
        {
            return _capabilities.IsAllowed(actor, moduleCode, actionKeys);
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private static DashboardWidget Widget(string id, string title, string module, DateTimeOffset refreshedAt,
        int count, IReadOnlyList<DashboardItem> items) =>
        new(id, title, module, count == 0 ? "Empty" : "Ready", null, refreshedAt, count, items);

    private static DashboardWidget Unavailable(string id, string title, string module, DateTimeOffset refreshedAt) =>
        new(id, title, module, "Unavailable", "Source module or read capability is unavailable.", refreshedAt, 0,
            Array.Empty<DashboardItem>());

    private static DashboardWidget Degraded(string id, string title, string module, DateTimeOffset refreshedAt,
        string message) =>
        new(id, title, module, "Degraded", message, refreshedAt, 0, Array.Empty<DashboardItem>());

    private static DateTimeOffset FromUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);

    private static void Add(SqlCommand command, string name, SqlDbType type, object value)
    {
        command.Parameters.Add(name, type).Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);
}
