using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Discovery;

/// <summary>
/// Bounded source-query implementation of the first Global Search slice. It
/// deliberately avoids a second search authority: every result is filtered by
/// the current owner and source capability at query time. A persisted index,
/// saved queries, favorites and recents remain separate slices.
/// </summary>
public sealed class SqlSearchService : ISearchService
{
    private const int MaxQueryLength = 500;
    private const int MaxLimit = 25;
    private const int PerSourceLimit = 25;
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;

    public SqlSearchService(SqlConnectionFactory connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<SearchPage> Search(IdentityPrincipal actor, string? query = null,
        string? resourceType = null, DateOnly? from = null, DateOnly? to = null,
        bool includeArchived = false, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX25", "discovery.search.query"))
            return Failure<SearchPage>("ModuleUnavailable", 409, "Search is disabled or unavailable for this user.");

        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length > MaxQueryLength)
            return Failure<SearchPage>("ValidationFailed", 422, "Search query must be at most 500 characters.");
        var normalizedType = NormalizeResourceType(resourceType);
        if (resourceType is not null && normalizedType is null)
            return Failure<SearchPage>("ValidationFailed", 422, "Search resource type is invalid.");
        if (from is not null && to is not null && to < from)
            return Failure<SearchPage>("ValidationFailed", 422, "Search end date cannot be before its start date.");

        var take = Math.Clamp(limit ?? MaxLimit, 1, MaxLimit);
        if (normalizedQuery.Length == 0)
            return IdentityOperationResult<SearchPage>.Success(new SearchPage(
                normalizedQuery, normalizedType, includeArchived,
                Array.Empty<SearchResult>(), Array.Empty<SearchProviderStatus>(), null));

        var like = $"%{EscapeLike(normalizedQuery)}%";
        var starts = $"{EscapeLike(normalizedQuery)}%";
        var fromUtc = from?.ToDateTime(TimeOnly.MinValue);
        var toUtc = to?.AddDays(1).ToDateTime(TimeOnly.MinValue);
        using var connection = _connections.Create();
        connection.Open();

        var candidates = new List<Candidate>();
        var providers = new List<SearchProviderStatus>();
        AddSource(connection, actor, normalizedType, "Project", "FX11", "projects.project.read", "projects.view",
            "/modules/FX11", """
            SELECT TOP (@Limit) p.[Id], p.[Name] AS [Title], p.[Description] AS [Snippet], p.[Status], p.[UpdatedAt],
                   CONVERT(int, CASE WHEN p.[Name] = @Query THEN 0 WHEN p.[Name] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Project] p WITH (NOLOCK)
            WHERE p.[OwnerId] = @OwnerId AND p.[Status] <> 'Deleted'
              AND (@IncludeArchived = 1 OR p.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR p.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR p.[UpdatedAt] < @ToUtc)
              AND (p.[Name] LIKE @Like ESCAPE N'\' OR p.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], p.[UpdatedAt] DESC, p.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Task", "FX12", "tasks.task.read", "tasks.view",
            "/modules/FX12", """
            SELECT TOP (@Limit) t.[Id], t.[Title], t.[Description] AS [Snippet], t.[Status], t.[UpdatedAt],
                   CONVERT(int, CASE WHEN t.[Title] = @Query THEN 0 WHEN t.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Task] t WITH (NOLOCK)
            WHERE t.[OwnerId] = @OwnerId AND t.[Status] <> 'Deleted'
              AND (@FromUtc IS NULL OR t.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR t.[UpdatedAt] < @ToUtc)
              AND (t.[Title] LIKE @Like ESCAPE N'\' OR t.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], t.[UpdatedAt] DESC, t.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Event", "FX13", "calendar.event.read", "calendar.view",
            "/modules/FX13", """
            SELECT TOP (@Limit) e.[Id], e.[Title], e.[Description] AS [Snippet], e.[Status], e.[UpdatedAt],
                   CONVERT(int, CASE WHEN e.[Title] = @Query THEN 0 WHEN e.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [calendar].[Event] e WITH (NOLOCK)
            WHERE e.[OwnerId] = @OwnerId AND e.[Status] <> 'Deleted'
              AND (@FromUtc IS NULL OR e.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR e.[UpdatedAt] < @ToUtc)
              AND (e.[Title] LIKE @Like ESCAPE N'\' OR e.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], e.[UpdatedAt] DESC, e.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Document", "FX20", "documents.page.read", "documents.library.read",
            "/modules/FX20", """
            SELECT TOP (@Limit) p.[Id], p.[Title], LEFT(p.[Body], 512) AS [Snippet], p.[Status], p.[UpdatedAt],
                   CONVERT(int, CASE WHEN p.[Title] = @Query THEN 0 WHEN p.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [documents].[Page] p WITH (NOLOCK)
            WHERE p.[OwnerId] = @OwnerId
              AND (@IncludeArchived = 1 OR p.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR p.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR p.[UpdatedAt] < @ToUtc)
              AND (p.[Title] LIKE @Like ESCAPE N'\' OR p.[Body] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], p.[UpdatedAt] DESC, p.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Bookmark", "FX21", "bookmarks.bookmark.read", null,
            "/bookmarks", """
            SELECT TOP (@Limit) b.[Id], b.[Title], b.[Description] AS [Snippet], b.[Status], b.[UpdatedAt],
                   CONVERT(int, CASE WHEN b.[Title] = @Query THEN 0 WHEN b.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [knowledge].[Bookmark] b WITH (NOLOCK)
            WHERE b.[OwnerId] = @OwnerId
              AND (@IncludeArchived = 1 OR b.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR b.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR b.[UpdatedAt] < @ToUtc)
              AND (b.[Title] LIKE @Like ESCAPE N'\' OR b.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], b.[UpdatedAt] DESC, b.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Snippet", "FX22", "snippets.snippet.read", null,
            "/snippets", """
            SELECT TOP (@Limit) s.[Id], v.[Title], LEFT(v.[Description], 512) AS [Snippet], s.[Status], s.[UpdatedAt],
                   CONVERT(int, CASE WHEN v.[Title] = @Query THEN 0 WHEN v.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [knowledge].[Snippet] s WITH (NOLOCK)
            INNER JOIN [knowledge].[SnippetVersion] v WITH (NOLOCK)
                ON v.[OwnerId] = s.[OwnerId] AND v.[SnippetId] = s.[Id] AND v.[VersionNumber] = s.[CurrentVersion]
            WHERE s.[OwnerId] = @OwnerId
              AND (@IncludeArchived = 1 OR s.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR s.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR s.[UpdatedAt] < @ToUtc)
              AND (v.[Title] LIKE @Like ESCAPE N'\' OR v.[Language] LIKE @Like ESCAPE N'\' OR v.[SourceText] LIKE @Like ESCAPE N'\' OR v.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], s.[UpdatedAt] DESC, s.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);
        AddSource(connection, actor, normalizedType, "Goal", "FX16", "goals.goal.read", null,
            "/goals", """
            SELECT TOP (@Limit) g.[Id], g.[Title], g.[Description] AS [Snippet], g.[Status], g.[UpdatedAt],
                   CONVERT(int, CASE WHEN g.[Title] = @Query THEN 0 WHEN g.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Goal] g WITH (NOLOCK)
            WHERE g.[OwnerId] = @OwnerId AND g.[Status] <> 'Deleted'
              AND (@IncludeArchived = 1 OR g.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR g.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR g.[UpdatedAt] < @ToUtc)
              AND (g.[Title] LIKE @Like ESCAPE N'\' OR g.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], g.[UpdatedAt] DESC, g.[Id] DESC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take, candidates, providers);

        var items = candidates
            .OrderBy(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Result.UpdatedAt)
            .ThenBy(candidate => candidate.Result.Id)
            .Take(take)
            .Select(candidate => candidate.Result)
            .ToArray();
        return IdentityOperationResult<SearchPage>.Success(new SearchPage(
            normalizedQuery, normalizedType, includeArchived, items, providers, null));
    }

    private void AddSource(SqlConnection connection, IdentityPrincipal actor, string? requestedType,
        string resourceType, string moduleCode, string actionKey, string? legacyActionKey, string route,
        string sql, string query, string like, string starts, DateTime? fromUtc, DateTime? toUtc,
        bool includeArchived, int limit, List<Candidate> candidates, List<SearchProviderStatus> providers)
    {
        if (requestedType is not null && !string.Equals(requestedType, resourceType, StringComparison.Ordinal))
            return;
        var refreshedAt = DateTimeOffset.UtcNow;
        if (!ModuleAvailable(actor, moduleCode, actionKey, legacyActionKey))
        {
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, "Unavailable",
                "Source module or read capability is unavailable.", 0));
            return;
        }
        try
        {
            using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = sql;
            Add(command, "@Limit", SqlDbType.Int, Math.Min(limit, PerSourceLimit));
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@Query", SqlDbType.NVarChar, query, 500);
            Add(command, "@Like", SqlDbType.NVarChar, like, 1004);
            Add(command, "@Starts", SqlDbType.NVarChar, starts, 1002);
            Add(command, "@FromUtc", SqlDbType.DateTime2, (object?)fromUtc ?? DBNull.Value);
            Add(command, "@ToUtc", SqlDbType.DateTime2, (object?)toUtc ?? DBNull.Value);
            Add(command, "@IncludeArchived", SqlDbType.Bit, includeArchived);
            using var reader = command.ExecuteReader();
            var total = 0;
            while (reader.Read())
            {
                total = Math.Max(total, Convert.ToInt32(reader.GetInt64(6)));
                var title = reader.GetString(1);
                var snippet = reader.IsDBNull(2) ? null : reader.GetString(2);
                var status = reader.IsDBNull(3) ? null : reader.GetString(3);
                var updatedAt = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc), TimeSpan.Zero);
                candidates.Add(new Candidate(new SearchResult(reader.GetGuid(0), resourceType, moduleCode,
                    title, SafeSnippet(snippet), status, updatedAt, route), reader.GetInt32(5)));
            }
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, total == 0 ? "Empty" : "Ready", null, total));
        }
        catch (SqlException)
        {
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, "Degraded",
                "This source is temporarily unavailable.", 0));
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, string actionKey, string? legacyActionKey = null)
    {
        try
        {
            return legacyActionKey is null
                ? _capabilities.IsAllowed(actor, moduleCode, actionKey)
                : _capabilities.IsAllowed(actor, moduleCode, actionKey, legacyActionKey);
        }
        catch (SqlException)
        {
            return false;
        }
    }

    private static string? NormalizeResourceType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "project" or "projects" => "Project",
            "task" or "tasks" => "Task",
            "event" or "events" or "calendar" => "Event",
            "document" or "documents" or "note" or "knowledge" => "Document",
            "bookmark" or "bookmarks" => "Bookmark",
            "snippet" or "snippets" => "Snippet",
            "goal" or "goals" => "Goal",
            _ => null
        };
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal)
        .Replace("^", "\\^", StringComparison.Ordinal);

    private static string? SafeSnippet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var compact = Whitespace.Replace(value, " ").Trim().Replace("<", "‹").Replace(">", "›");
        return compact.Length <= 240 ? compact : compact[..240] + "…";
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is null ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size.Value);
        parameter.Value = value;
    }

    private sealed record Candidate(SearchResult Result, int Score);

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);
}
