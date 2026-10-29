using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Discovery;

public sealed class SqlCommandPaletteService : ICommandPaletteService
{
    private sealed record Check(string Module, string Action);
    private sealed record Entry(string Key, string Title, string Group, CommandHandoff Handoff, params Check[] Checks);
    private static readonly Entry[] Registry = [
        new("home", "Mở Dashboard", "Navigate", new("home"), new Check("FX26", "dashboard.dashboard.read")),
        new("search", "Tìm kiếm toàn cục", "Navigate", new("search"), new Check("FX25", "discovery.search.query")),
        new("favorites", "Mở Favorites", "Navigate", new("favorites"), new Check("FX25", "discovery.favorite.read")),
        new("recents", "Mở lịch sử gần đây", "Navigate", new("recents"), new Check("FX25", "discovery.recent.read")),
        new("notifications", "Mở Inbox", "Navigate", new("notifications"), new Check("FX06", "notifications.inbox.read")),
        new("documents", "Mở thư viện Documents", "Navigate", new("module", "FX20"), new Check("FX20", "documents.library.read"), new Check("FX20", "documents.page.read")),
        new("tasks", "Mở Tasks", "Navigate", new("module", "FX12"), new Check("FX12", "tasks.task.read")),
        new("calendar", "Mở Calendar", "Navigate", new("module", "FX13"), new Check("FX13", "calendar.event.read")),
        new("finance", "Mở Finance thủ công", "Navigate", new("finance"), new Check("FX27", "finance.manual_record.read"), new Check("FX27", "finance.manual_summary.read"), new Check("FX27", "finance.manual_category.read")),
        new("create-project", "Tạo Project", "Create", new("module", "FX11", Mode: "create"), new Check("FX11", "projects.project.create")),
        new("create-task", "Tạo Task", "Create", new("module", "FX12", Mode: "create"), new Check("FX12", "tasks.task.create"), new Check("FX11", "projects.project.read")),
        new("create-event", "Tạo Calendar Event", "Create", new("module", "FX13", Mode: "create"), new Check("FX13", "calendar.event.create")),
        new("create-document", "Tạo Document", "Create", new("module", "FX20", Mode: "create"), new Check("FX20", "documents.page.create")),
        new("create-finance-record", "Tạo Finance record", "Create", new("finance", Mode: "create"), new Check("FX27", "finance.manual_record.create"), new Check("FX27", "finance.manual_category.read")),
        new("clear-recents", "Xóa lịch sử gần đây…", "Actions", new("recents", Mode: "clear"), new Check("FX25", "discovery.recent.clear"))
    ];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlDiscoverySources sources;
    public SqlCommandPaletteService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); sources = new(connections, secret); }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, params string[] actions) => capabilities.IsAllowed(c, tx, actor, "FX25", actions);
    private bool TargetAllowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Entry entry) => entry.Checks.All(check => capabilities.IsAllowed(c, tx, actor, check.Module, check.Action));
    public IdentityOperationResult<DiscoveryCommandPage> List(IdentityPrincipal actor, string? query = null)
    {
        if (query is { Length: > 500 }) return Fail<DiscoveryCommandPage>("ValidationFailed", 422);
        query = query?.Trim() ?? "";
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(c, tx, actor, "discovery.command.read")) return Fail<DiscoveryCommandPage>("ActionDenied", 403);
            var entries = Registry.Where(entry => (entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || entry.Key.Contains(query, StringComparison.OrdinalIgnoreCase)) && TargetAllowed(c, tx, actor, entry)).ToArray();
            var result = new DiscoveryCommandPage(entries.Select(entry => new DiscoveryCommand(entry.Key, entry.Title, entry.Group)).ToArray(),
                Allowed(c, tx, actor, "discovery.search.query"), Allowed(c, tx, actor, "discovery.recent.read"), Allowed(c, tx, actor, "discovery.command.execute"));
            if (!Allowed(c, tx, actor, "discovery.command.read")) return Fail<DiscoveryCommandPage>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<DiscoveryCommandPage>.Success(result);
        }
        catch (SqlException) { return Fail<DiscoveryCommandPage>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<CommandHandoff> Execute(IdentityPrincipal actor, CommandSelection selection)
    {
        if (selection is null) return Fail<CommandHandoff>("ValidationFailed", 422);
        Entry? entry = null;
        if (selection.Kind == "Command" && selection.Key is { Length: > 0 and <= 80 } && selection.ResourceType is null && selection.ResourceId is null)
        { entry = Registry.SingleOrDefault(item => item.Key == selection.Key); if (entry is null) return Fail<CommandHandoff>("ValidationFailed", 422); }
        else if (selection.Kind != "Resource" || selection.Key is not null || selection.ResourceType is null
            || !SqlDiscoverySources.Types.Contains(selection.ResourceType, StringComparer.Ordinal) || selection.ResourceId is null || selection.ResourceId == Guid.Empty)
            return Fail<CommandHandoff>("ValidationFailed", 422);
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            bool Authorized() => Allowed(c, tx, actor, "discovery.command.read", "discovery.command.execute");
            if (!Authorized()) return Fail<CommandHandoff>("ActionDenied", 403);
            CommandHandoff handoff;
            if (entry is not null)
            {
                if (!TargetAllowed(c, tx, actor, entry)) return Fail<CommandHandoff>("ActionDenied", 403);
                handoff = entry.Handoff;
            }
            else
            {
                if (sources.ReadWithin(c, tx, actor, selection.ResourceType!, selection.ResourceId!.Value) is null)
                    return Fail<CommandHandoff>("ResourceUnavailable", 404);
                handoff = new("resource", ResourceType: selection.ResourceType, ResourceId: selection.ResourceId);
            }
            if (!Authorized() || (entry is not null && !TargetAllowed(c, tx, actor, entry))) return Fail<CommandHandoff>("ActionDenied", 403);
            if (entry is null && sources.ReadWithin(c, tx, actor, selection.ResourceType!, selection.ResourceId!.Value) is null)
                return Fail<CommandHandoff>("ResourceUnavailable", 404);
            tx.Commit(); return IdentityOperationResult<CommandHandoff>.Success(handoff);
        }
        catch (SqlException) { return Fail<CommandHandoff>("PersistenceUnavailable", 503); }
    }
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, "Command unavailable.");
}
