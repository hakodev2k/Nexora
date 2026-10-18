using System.Data;
using NodaTime;
using NodaTime.TimeZones;
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
public sealed partial class SqlDashboardService : IDashboardService
{
    private const int ItemLimit = 5;
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;
    private readonly DashboardDrilldownCursor _drilldownCursors;
    private readonly TimeProvider _clock;

    public SqlDashboardService(SqlConnectionFactory connections, string? secret = null, TimeProvider? clock = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _capabilities = new SqlSelfCapability(connections);
        _drilldownCursors = new(secret ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        _clock = clock ?? TimeProvider.System;
    }

    public IdentityOperationResult<DashboardSnapshot> Get(IdentityPrincipal actor) => ReadSnapshot(actor, false);
    public IdentityOperationResult<DashboardSnapshot> Refresh(IdentityPrincipal actor) => ReadSnapshot(actor, true);
    public IdentityOperationResult<DashboardCreateTarget> OpenCreate(IdentityPrincipal actor, string resourceType)
    {
        var target = resourceType switch
        {
            "Project" => (Module: "FX11", Action: "projects.project.create"),
            "Task" => (Module: "FX12", Action: "tasks.task.create"),
            "CalendarEvent" => (Module: "FX13", Action: "calendar.event.create"),
            "Document" => (Module: "FX20", Action: "documents.page.create"),
            "FinanceRecord" => (Module: "FX27", Action: "finance.manual_record.create"),
            _ => default
        };
        if (target.Module is null)
            return Failure<DashboardCreateTarget>("ValidationFailed", 422, "Unsupported create workflow.");
        try
        {
            using var connection = _connections.Create(); connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            bool Authorized() => Allowed(connection, transaction, actor, "FX26", "dashboard.dashboard.read", "dashboard.quick_create.open")
                && Allowed(connection, transaction, actor, target.Module, target.Action)
                && (resourceType != "Task" || Allowed(connection, transaction, actor, "FX11", "projects.project.read"))
                && (resourceType != "FinanceRecord" || Allowed(connection, transaction, actor, "FX27", "finance.manual_category.read"));
            if (!Authorized()) return Failure<DashboardCreateTarget>("ActionDenied", 403, "Create workflow unavailable.");
            var result = new DashboardCreateTarget(resourceType, target.Module);
            if (!Authorized()) return Failure<DashboardCreateTarget>("ActionDenied", 403, "Create workflow unavailable.");
            transaction.Commit();
            return IdentityOperationResult<DashboardCreateTarget>.Success(result);
        }
        catch (SqlException)
        {
            return Failure<DashboardCreateTarget>("PersistenceUnavailable", 503, "Create workflow unavailable.");
        }
    }

    private IdentityOperationResult<DashboardSnapshot> ReadSnapshot(IdentityPrincipal actor, bool refresh)
    {
        if (!ModuleAvailable(actor, "FX26", "dashboard.dashboard.read"))
            return Failure<DashboardSnapshot>("ModuleUnavailable", 409, "Dashboard is disabled or unavailable for this user.");

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "FX26", "dashboard.dashboard.read")) return Failure<DashboardSnapshot>("ModuleUnavailable",409,"Dashboard unavailable.");
            if (refresh && !Allowed(connection, transaction, actor, "FX26", "dashboard.widget.refresh")) return Failure<DashboardSnapshot>("ActionDenied",403,"Dashboard refresh unavailable.");
            var (timeZoneId, timeZone) = ReadTimeZone(connection, transaction, actor);
            var generatedAt = _clock.GetUtcNow();
            var today = Instant.FromDateTimeOffset(generatedAt).InZone(timeZone).Date;
            var startUtc = timeZone.ResolveLocal(today.AtMidnight(), Resolvers.LenientResolver).ToInstant().ToDateTimeUtc();
            var endUtc = timeZone.ResolveLocal(today.PlusDays(1).AtMidnight(), Resolvers.LenientResolver).ToInstant().ToDateTimeUtc();
            var localDate = new DateOnly(today.Year,today.Month,today.Day);
            var tasksAllowed = Allowed(connection, transaction, actor, "FX12", "tasks.task.read");
            var widgets = new[]
            {
                ReadTasks(connection, transaction, actor, startUtc, endUtc),
                ReadCalendar(connection, transaction, actor, startUtc, endUtc, localDate, tasksAllowed),
                ReadDocuments(connection, transaction, actor),
                ReadNotifications(connection, transaction, actor)
            };
            for (var index=0; index<widgets.Length; index++)
            {
                var widget=widgets[index];
                string[] actions=widget.SourceModule switch { "FX12"=>["tasks.task.read"],"FX13"=>["calendar.event.read"],"FX20"=>["documents.page.read","documents.library.read"],_=>["notifications.inbox.read"] };
                if (!Allowed(connection,transaction,actor,widget.SourceModule,actions)
                    || (widget.SourceModule=="FX13" && tasksAllowed && !Allowed(connection,transaction,actor,"FX12","tasks.task.read")))
                    widgets[index]=Unavailable(widget.Id,widget.Title,widget.SourceModule,generatedAt);
            }
            if (!Allowed(connection,transaction,actor,"FX26","dashboard.dashboard.read")) return Failure<DashboardSnapshot>("ModuleUnavailable",409,"Dashboard unavailable.");
            if (refresh && !Allowed(connection,transaction,actor,"FX26","dashboard.widget.refresh")) return Failure<DashboardSnapshot>("ActionDenied",403,"Dashboard refresh unavailable.");
            for (var index = 0; index < widgets.Length; index++)
                if (widgets[index].State is "Ready" or "Empty")
                    widgets[index] = widgets[index] with { DrilldownToken = _drilldownCursors.Encode(new(1, actor.UserId, actor.OwnerId,
                        widgets[index].Id, generatedAt, timeZoneId, tasksAllowed, 0)) };
            transaction.Commit();
            return IdentityOperationResult<DashboardSnapshot>.Success(
                new DashboardSnapshot(timeZoneId, generatedAt, widgets));
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            return Failure<DashboardSnapshot>("PersistenceUnavailable", 503, "Dashboard sources are unavailable.");
        }
    }

    private DashboardWidget ReadTasks(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        DateTime startUtc, DateTime endUtc, int limit = ItemLimit, int offset = 0)
    {
        const string module = "FX12";
        const string widgetId = "tasks-due";
        var refreshedAt = _clock.GetUtcNow();
        if (!Allowed(connection, transaction, actor, module, "tasks.task.read"))
            return Unavailable(widgetId, "Tasks due and overdue", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT
                    t.[Id], t.[Title], t.[Status], t.[DueAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [productivity].[Task] t
                INNER JOIN [productivity].[Project] p ON p.[Id]=t.[ProjectId] AND p.[OwnerId]=t.[OwnerId]
                WHERE t.[OwnerId] = @OwnerId
                  AND t.[Status] IN ('NotStarted','InProgress') AND t.[DeletedAt] IS NULL
                  AND p.[Status] IN ('NotStarted','InProgress') AND p.[DeletedAt] IS NULL
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId]=t.[OwnerId]
                    AND ((tr.[ResourceType]='Task' AND tr.[ResourceId]=t.[Id]) OR (tr.[ResourceType]='Project' AND tr.[ResourceId]=p.[Id]))
                    AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
                  AND t.[DueAt] IS NOT NULL
                  AND t.[DueAt] < @EndUtc
                ORDER BY CASE WHEN t.[DueAt] < @StartUtc THEN 0 ELSE 1 END,
                         t.[DueAt], t.[Id] OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                """;
            Add(command, "@Limit", SqlDbType.Int, limit);
            Add(command, "@Offset", SqlDbType.Int, offset);
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

    private DashboardWidget ReadCalendar(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        DateTime startUtc, DateTime endUtc, DateOnly localDate, bool tasksAllowed, int limit = ItemLimit, int offset = 0)
    {
        const string module = "FX13";
        const string widgetId = "calendar-today";
        var refreshedAt = _clock.GetUtcNow();
        if (!Allowed(connection, transaction, actor, module, "calendar.event.read"))
            return Unavailable(widgetId, "Calendar today", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT
                    e.[Id], e.[Title], e.[Status], e.[StartAt], e.[EndAt],
                    COUNT_BIG(1) OVER() AS [TotalCount], e.[IsAllDay],e.[StartDate],e.[EndDateExclusive]
                FROM [calendar].[Event] e
                LEFT JOIN [productivity].[Task] t ON t.[OwnerId]=e.[OwnerId] AND t.[Id]=e.[TaskId]
                LEFT JOIN [productivity].[Project] p ON p.[OwnerId]=t.[OwnerId] AND p.[Id]=t.[ProjectId]
                WHERE e.[OwnerId] = @OwnerId
                  AND e.[Status] IN ('Scheduled','Completed')
                  AND ((e.[IsAllDay]=0 AND e.[StartAt]<@EndUtc AND e.[EndAt]>@StartUtc)
                    OR (e.[IsAllDay]=1 AND e.[StartDate]<=@LocalDate AND e.[EndDateExclusive]>@LocalDate))
                  AND (e.[TaskId] IS NULL OR (@TasksAllowed=1 AND t.[Id] IS NOT NULL AND p.[Id] IS NOT NULL
                    AND t.[DeletedAt] IS NULL AND p.[DeletedAt] IS NULL AND t.[Status]<>'Deleted' AND p.[Status]<>'Deleted'))
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId]=e.[OwnerId]
                    AND ((tr.[ResourceType]='Event' AND tr.[ResourceId]=e.[Id]) OR (e.[TaskId] IS NOT NULL AND
                      ((tr.[ResourceType]='Task' AND tr.[ResourceId]=t.[Id]) OR (tr.[ResourceType]='Project' AND tr.[ResourceId]=p.[Id]))))
                    AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
                ORDER BY e.[StartAt], e.[Id] OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                """;
            Add(command, "@Limit", SqlDbType.Int, limit);
            Add(command, "@Offset", SqlDbType.Int, offset);
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@StartUtc", SqlDbType.DateTime2, startUtc);
            Add(command, "@EndUtc", SqlDbType.DateTime2, endUtc);
            Add(command, "@LocalDate", SqlDbType.Date, localDate.ToDateTime(TimeOnly.MinValue));
            Add(command, "@TasksAllowed", SqlDbType.Bit, tasksAllowed);
            var items = new List<DashboardItem>();
            var count = 0;
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                count = Convert.ToInt32(reader.GetInt64(5));
                var allDay = reader.GetBoolean(6);
                items.Add(new DashboardItem(reader.GetGuid(0), "Event", reader.GetString(1),
                    reader.GetString(2), allDay ? null : FromUtc(reader.GetDateTime(3)), null,
                    allDay && !reader.IsDBNull(7) ? DateOnly.FromDateTime(reader.GetDateTime(7)) : null,
                    allDay && !reader.IsDBNull(8) ? DateOnly.FromDateTime(reader.GetDateTime(8)) : null,
                    allDay ? null : FromUtc(reader.GetDateTime(4))));
            }
            return Widget(widgetId, "Calendar today", module, refreshedAt, count, items) with
                { Message = tasksAllowed ? null : "Personal Events only; Task projections are unavailable under current permissions." };
        }
        catch (SqlException)
        {
            return Degraded(widgetId, "Calendar today", module, refreshedAt,
                "Calendar is temporarily unavailable.");
        }
    }

    private DashboardWidget ReadDocuments(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, int limit = ItemLimit, int offset = 0)
    {
        const string module = "FX20";
        const string widgetId = "documents-recent";
        var refreshedAt = _clock.GetUtcNow();
        if (!Allowed(connection, transaction, actor, module, "documents.page.read", "documents.library.read"))
            return Unavailable(widgetId, "Recent documents", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT
                    p.[Id], p.[Title], p.[DocumentType], p.[Status], p.[UpdatedAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [documents].[Page] p
                WHERE p.[OwnerId] = @OwnerId
                  AND EXISTS(SELECT 1 FROM [documents].[VisiblePageV1] visible WHERE visible.OwnerId=p.OwnerId AND visible.Id=p.Id)
                  AND p.[Status] IN ('Draft','Published')
                  AND p.[DeletedAt] IS NULL
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId]=p.[OwnerId]
                    AND tr.[ResourceType]='Document' AND tr.[ResourceId]=p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
                ORDER BY p.[UpdatedAt] DESC, p.[Id] DESC OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                """;
            Add(command, "@Limit", SqlDbType.Int, limit);
            Add(command, "@Offset", SqlDbType.Int, offset);
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

    private DashboardWidget ReadNotifications(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, int limit = ItemLimit, int offset = 0)
    {
        const string module = "FX06";
        const string widgetId = "notifications-unread";
        var refreshedAt = _clock.GetUtcNow();
        if (!Allowed(connection, transaction, actor, module, "notifications.inbox.read"))
            return Unavailable(widgetId, "Unread notifications", module, refreshedAt);

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 3;
            command.CommandText = """
                SELECT
                    n.[Id], CONVERT(nvarchar(100),N'Nguồn thông báo không khả dụng'), n.[Kind], n.[CreatedAt],
                    COUNT_BIG(1) OVER() AS [TotalCount]
                FROM [notifications].[Notification] n
                WHERE n.[OwnerUserId] = @UserId
                  AND n.[ReadAt] IS NULL
                  AND n.[DeletedAt] IS NULL
                ORDER BY n.[CreatedAt] DESC, n.[Id] DESC OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
                """;
            Add(command, "@Limit", SqlDbType.Int, limit);
            Add(command, "@Offset", SqlDbType.Int, offset);
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

    private static (string Id, DateTimeZone Zone) ReadTimeZone(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor)
    {
        using var sql=connection.CreateCommand();sql.Transaction=transaction;
        sql.CommandText="SELECT u.[TimeZoneId] FROM [identity].[User] u INNER JOIN [platform].[PersonalSpace] p ON p.[UserId]=u.[Id] WHERE u.[Id]=@User AND p.[Id]=@Owner;";
        Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(sql,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);
        var id=sql.ExecuteScalar() as string; var zone=id is null ? null : DateTimeZoneProviders.Tzdb.GetZoneOrNull(id);
        if (zone is null) throw new InvalidOperationException("Profile timezone unavailable.");
        return (id!,zone);
    }
    private bool Allowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string module, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, module, actions);
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
