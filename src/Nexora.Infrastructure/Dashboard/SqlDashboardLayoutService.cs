using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Dashboard;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Dashboard;

public sealed class SqlDashboardLayoutService
    : IDashboardLayoutService
{
    private static readonly HashSet<string> Keys = ["tasks-due", "calendar-today", "documents-recent", "notifications-unread"];
    private static readonly HashSet<string> LayoutActions = ["update", "add_widget", "configure_widget", "remove_widget", "reorder_widget"];
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;
    private readonly SqlRequestReceiptStore _receipts;
    public SqlDashboardLayoutService(SqlConnectionFactory connections, string secret)
    { _connections = connections; _capabilities = new(connections); _receipts = new(secret); }
    private bool Allowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX26", actions);
    private bool LayoutAllowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string[] actions) =>
        Allowed(connection, transaction, actor, actions.Select(action => "dashboard.layout." + action).ToArray());

    public IdentityOperationResult<DashboardLayoutState> Get(IdentityPrincipal actor)
    {
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(connection, transaction, actor, "dashboard.dashboard.read")) return Denied<DashboardLayoutState>();
            var result = Read(connection, transaction, actor.OwnerId, false);
            if (!Allowed(connection, transaction, actor, "dashboard.dashboard.read")) return Denied<DashboardLayoutState>();
            transaction.Commit(); return IdentityOperationResult<DashboardLayoutState>.Success(new(result));
        }
        catch (Exception error) when (error is SqlException or JsonException) { return Fail<DashboardLayoutState>("LayoutUnavailable", 503); }
    }

    public IdentityOperationResult<DashboardLayout> Save(IdentityPrincipal actor, bool create, string? ifMatch, DashboardLayoutCommand command,
        string? key = null, string? traceId = null)
    {
        if (command.SchemaVersion != 1) return Fail<DashboardLayout>("MigrationRequired", 409);
        if (command.Widgets is null || command.Widgets.Count > 24 || command.Widgets.Any(widget => widget is null || widget.Id == Guid.Empty
                || !Keys.Contains(widget.Key ?? string.Empty) || widget.ProviderVersion != "1" || widget.ConfigVersion != 1
                || widget.WidthUnits is < 1 or > 2 || widget.Options.ValueKind != JsonValueKind.Object || widget.Options.EnumerateObject().Any())
            || command.Widgets.Where(widget => widget.Id.HasValue).Select(widget => widget.Id).Distinct().Count() != command.Widgets.Count(widget => widget.Id.HasValue)
            || (create && command.Widgets.Any(widget => widget.Id.HasValue))) return Fail<DashboardLayout>("ValidationFailed", 422);
        byte[] expected = [];
        if (!create && !ETag(ifMatch, out expected)) return Fail<DashboardLayout>("PreconditionRequired", 428);
        try
        {
            using var connection = _connections.Create(); connection.Open(); using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!LayoutAllowed(connection, transaction, actor, ["update"])) return Denied<DashboardLayout>();
            var receipt = default(ReceiptClaim);
            if (!string.IsNullOrWhiteSpace(key))
            {
                receipt = _receipts.TryClaim(connection, transaction, actor.UserId, "dashboard.layout.save", key,
                    JsonSerializer.Serialize(new { version = 1, create, etag = create ? null : Convert.ToBase64String(expected), widgets = command.Widgets }), DateTime.UtcNow);
                if (!receipt.IsClaimed)
                {
                    if (receipt.IsInvalid) return Fail<DashboardLayout>("ValidationFailed", 422);
                    if (receipt.IsConflict) return Fail<DashboardLayout>("IdempotencyConflict", 409);
                    if (!receipt.IsReplay) return Fail<DashboardLayout>("RequestInProgress", 409);
                    Receipt? original = null; try { original = JsonSerializer.Deserialize<Receipt>(receipt.ResultJson ?? string.Empty); } catch (JsonException) { }
                    if (original is null || original.Version != 1 || original.Id == Guid.Empty || original.UserId != actor.UserId || original.OwnerId != actor.OwnerId
                        || original.Create != create || original.Actions is null || !original.Actions.Contains("update")
                        || original.Actions.Distinct().Count() != original.Actions.Length || original.Actions.Any(action => !LayoutActions.Contains(action))
                        || receipt.ResultCode != "DashboardLayoutSaved" || receipt.ResultStatusCode != (create ? 201 : 200)) return Fail<DashboardLayout>("ReceiptUnavailable", 503);
                    if (!LayoutAllowed(connection, transaction, actor, original.Actions)) return Denied<DashboardLayout>();
                    return IdentityOperationResult<DashboardLayout>.Committed(original.Id, create ? 201 : 200, "DashboardLayoutSaved");
                }
            }
            var current = Read(connection, transaction, actor.OwnerId, true);
            if (create && current is not null) return Fail<DashboardLayout>("LayoutExists", 409);
            if (!create && current is null) return Fail<DashboardLayout>("ResourceUnavailable", 404);
            if (!create && !CryptographicOperations.FixedTimeEquals(expected, Convert.FromBase64String(current!.ETag[1..^1]))) return Fail<DashboardLayout>("RevisionConflict", 412);
            var previous = current?.Widgets ?? [];
            var previousById = previous.ToDictionary(widget => widget.Id!.Value);
            if (command.Widgets.Any(widget => widget.Id.HasValue && !previousById.ContainsKey(widget.Id.Value))) return Fail<DashboardLayout>("ResourceUnavailable", 404);
            var actions = new HashSet<string>(StringComparer.Ordinal) { "update" };
            if (command.Widgets.Any(widget => !widget.Id.HasValue)) actions.Add("add_widget");
            var retained = command.Widgets.Where(widget => widget.Id.HasValue).Select(widget => widget.Id!.Value).ToHashSet();
            if (previous.Any(widget => !retained.Contains(widget.Id!.Value))) actions.Add("remove_widget");
            if (!previous.Where(widget => retained.Contains(widget.Id!.Value)).Select(widget => widget.Id)
                .SequenceEqual(command.Widgets.Where(widget => widget.Id.HasValue).Select(widget => widget.Id))) actions.Add("reorder_widget");
            if (command.Widgets.Any(widget => widget.Id.HasValue && !SameConfig(previousById[widget.Id.Value], widget))) actions.Add("configure_widget");
            var required = actions.Order(StringComparer.Ordinal).ToArray();
            if (!LayoutAllowed(connection, transaction, actor, required)) return Denied<DashboardLayout>();
            var id = current?.Id ?? Guid.NewGuid();
            Execute(connection, transaction, create
                ? "INSERT INTO [discovery].[Dashboard]([Id],[OwnerId],[Title],[LayoutVersion],[CreatedByUserId],[UpdatedByUserId]) VALUES(@Id,@Owner,N'Home',1,@User,@User);"
                : "UPDATE [discovery].[Dashboard] SET [LayoutVersion]=[LayoutVersion]+1,[UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User WHERE [OwnerId]=@Owner AND [Id]=@Id;",
                ("@Id", SqlDbType.UniqueIdentifier, id), ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@User", SqlDbType.UniqueIdentifier, actor.UserId));
            Execute(connection, transaction, "UPDATE [discovery].[DashboardWidget] SET [Position]=[Position]+1000 WHERE [OwnerId]=@Owner AND [DashboardId]=@Id;",
                ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@Id", SqlDbType.UniqueIdentifier, id));
            foreach (var old in previous.Where(widget => !retained.Contains(widget.Id!.Value)))
                Execute(connection, transaction, "DELETE FROM [discovery].[DashboardWidget] WHERE [OwnerId]=@Owner AND [DashboardId]=@Root AND [Id]=@Id;",
                    ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@Root", SqlDbType.UniqueIdentifier, id), ("@Id", SqlDbType.UniqueIdentifier, old.Id!.Value));
            for (var index = 0; index < command.Widgets.Count; index++)
            {
                var widget = command.Widgets[index]; var widgetId = widget.Id ?? Guid.NewGuid();
                Execute(connection, transaction, widget.Id.HasValue
                    ? "UPDATE [discovery].[DashboardWidget] SET [WidgetKey]=@Key,[ProviderVersion]=@Provider,[SchemaVersion]=1,[WidthUnits]=@Width,[Enabled]=@Enabled,[Position]=@Position,[OptionsJson]=N'{}',[UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User WHERE [OwnerId]=@Owner AND [DashboardId]=@Root AND [Id]=@Id;"
                    : "INSERT INTO [discovery].[DashboardWidget]([Id],[OwnerId],[DashboardId],[WidgetKey],[ProviderVersion],[SchemaVersion],[WidthUnits],[Enabled],[Position],[OptionsJson],[CreatedByUserId],[UpdatedByUserId]) VALUES(@Id,@Owner,@Root,@Key,@Provider,1,@Width,@Enabled,@Position,N'{}',@User,@User);",
                    ("@Id", SqlDbType.UniqueIdentifier, widgetId), ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@Root", SqlDbType.UniqueIdentifier, id),
                    ("@Key", SqlDbType.NVarChar, widget.Key), ("@Provider", SqlDbType.NVarChar, widget.ProviderVersion), ("@Width", SqlDbType.TinyInt, widget.WidthUnits),
                    ("@Enabled", SqlDbType.Bit, widget.Enabled), ("@Position", SqlDbType.Int, index), ("@User", SqlDbType.UniqueIdentifier, actor.UserId));
            }
            Execute(connection, transaction, "INSERT INTO [security].[AuditEvent]([ActorUserId],[OwnerUserId],[ActionKey],[TargetType],[TargetId],[Result],[TraceId]) VALUES(@User,@User,N'dashboard.layout.update',N'Dashboard',@Id,'Succeeded',@Trace);",
                ("@User", SqlDbType.UniqueIdentifier, actor.UserId), ("@Id", SqlDbType.UniqueIdentifier, id), ("@Trace", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));
            _receipts.Complete(connection, transaction, receipt, "DashboardLayoutSaved", create ? 201 : 200,
                JsonSerializer.Serialize(new Receipt(1, id, actor.UserId, actor.OwnerId, create, required)));
            if (!LayoutAllowed(connection, transaction, actor, required)) return Denied<DashboardLayout>();
            var result = Allowed(connection, transaction, actor, "dashboard.dashboard.read") ? Read(connection, transaction, actor.OwnerId, false) : null;
            var readable = result is not null && Allowed(connection, transaction, actor, "dashboard.dashboard.read");
            if (!LayoutAllowed(connection, transaction, actor, required)) return Denied<DashboardLayout>();
            transaction.Commit(); return readable ? IdentityOperationResult<DashboardLayout>.Success(result!, create ? 201 : 200, "DashboardLayoutSaved")
                : IdentityOperationResult<DashboardLayout>.Committed(id, create ? 201 : 200, "DashboardLayoutSaved");
        }
        catch (SqlException error) when (error.Number is 2601 or 2627) { return Fail<DashboardLayout>("LayoutConflict", 409); }
        catch (Exception error) when (error is SqlException or JsonException) { return Fail<DashboardLayout>("LayoutUnavailable", 503); }
    }

    private static bool SameConfig(DashboardWidgetCommand before, DashboardWidgetCommand after) => before.Key == after.Key
        && before.ProviderVersion == after.ProviderVersion && before.ConfigVersion == after.ConfigVersion && before.WidthUnits == after.WidthUnits
        && before.Enabled == after.Enabled && before.Options.GetRawText() == after.Options.GetRawText();
    private static DashboardLayout? Read(SqlConnection connection, SqlTransaction transaction, Guid owner, bool update)
    {
        using var sql = connection.CreateCommand(); sql.Transaction = transaction;
        sql.CommandText = $"SELECT [Id],[LayoutVersion],[RowVersion] FROM [discovery].[Dashboard] {(update ? "WITH(UPDLOCK,HOLDLOCK)" : string.Empty)} WHERE [OwnerId]=@Owner;";
        Add(sql, "@Owner", SqlDbType.UniqueIdentifier, owner);
        Guid id; long version; string etag;
        using (var reader = sql.ExecuteReader()) { if (!reader.Read()) return null; id = reader.GetGuid(0); version = reader.GetInt64(1); etag = $"\"{Convert.ToBase64String(reader.GetFieldValue<byte[]>(2))}\""; }
        sql.CommandText = "SELECT [Id],[WidgetKey],[ProviderVersion],[SchemaVersion],[WidthUnits],[Enabled],[OptionsJson] FROM [discovery].[DashboardWidget] WHERE [OwnerId]=@Owner AND [DashboardId]=@Root ORDER BY [Position],[Id];";
        Add(sql, "@Root", SqlDbType.UniqueIdentifier, id); var widgets = new List<DashboardWidgetCommand>();
        using (var reader = sql.ExecuteReader()) while (reader.Read())
        {
            using var options = JsonDocument.Parse(reader.GetString(6));
            widgets.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetByte(4), reader.GetBoolean(5), options.RootElement.Clone()));
        }
        return new(id, 1, version, widgets, etag);
    }
    private static bool ETag(string? raw, out byte[] bytes)
    {
        bytes = []; if (string.IsNullOrWhiteSpace(raw)) return false; var token = raw.Trim();
        if (token.Length < 3 || token[0] != '"' || token[^1] != '"') return false;
        try { bytes = Convert.FromBase64String(token[1..^1]); return bytes.Length == 8 && Convert.ToBase64String(bytes) == token[1..^1]; } catch (FormatException) { return false; }
    }
    private static void Execute(SqlConnection connection, SqlTransaction transaction, string text, params (string Name, SqlDbType Type, object Value)[] values)
    { using var sql = connection.CreateCommand(); sql.Transaction = transaction; sql.CommandText = text; foreach (var value in values) Add(sql, value.Name, value.Type, value.Value); sql.ExecuteNonQuery(); }
    private static void Add(SqlCommand sql, string name, SqlDbType type, object value) => sql.Parameters.Add(name, type).Value = value;
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, "Dashboard layout unavailable or invalid.");
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 409);
    private sealed record Receipt(int Version, Guid Id, Guid UserId, Guid OwnerId, bool Create, string[] Actions);
}
