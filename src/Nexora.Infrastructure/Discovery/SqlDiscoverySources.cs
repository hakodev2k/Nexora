using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Goals;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Nexora.Infrastructure.Snippets;

namespace Nexora.Infrastructure.Discovery;

internal sealed class SqlDiscoverySources
{
    internal static readonly string[] Types = ["Project", "Task", "Event", "Document", "Bookmark", "Snippet", "Goal"];
    private readonly SqlProductivityService productivity;
    private readonly SqlDocumentService documents;
    private readonly SqlBookmarkService bookmarks;
    private readonly SqlSnippetService snippets;
    private readonly SqlGoalService goals;
    internal SqlDiscoverySources(SqlConnectionFactory connections, string secret)
    { productivity = new(connections, secret); documents = new(connections, secret); bookmarks = new(connections, secret); snippets = new(connections, secret); goals = new(connections, secret); }
    internal RecentSourceProjection? ReadWithin(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string type, Guid id) => type switch
    {
        "Project" or "Task" or "Event" => productivity.ReadRecentSourceWithin(c, tx, actor, type, id),
        "Document" => documents.ReadRecentSourceWithin(c, tx, actor, id),
        "Bookmark" => bookmarks.ReadRecentSourceWithin(c, tx, actor, id),
        "Snippet" => snippets.ReadRecentSourceWithin(c, tx, actor, id),
        "Goal" => goals.ReadRecentSourceWithin(c, tx, actor, id),
        _ => null
    };
}
