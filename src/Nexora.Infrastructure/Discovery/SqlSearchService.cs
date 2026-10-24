using System.Data;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Identity;

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
    private const int PerSourceLimit = 26;
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlSelfCapability _capabilities;

    private readonly SearchCursor _cursor;

    public SqlSearchService(SqlConnectionFactory connections, string? cursorSecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _capabilities = new SqlSelfCapability(connections);
        _cursor = new SearchCursor(cursorSecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<SearchPage> Search(IdentityPrincipal actor, string? query = null,
        string? resourceType = null, DateOnly? from = null, DateOnly? to = null,
        bool includeArchived = false, int? limit = null, string? cursor = null)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length > MaxQueryLength)
            return Failure<SearchPage>("ValidationFailed", 422, "Search query must be at most 500 characters.");
        var normalizedType = NormalizeResourceType(resourceType);
        if (resourceType is not null && normalizedType is null)
            return Failure<SearchPage>("ValidationFailed", 422, "Search resource type is invalid.");
        if (from is not null && to is not null && to < from)
            return Failure<SearchPage>("ValidationFailed", 422, "Search end date cannot be before its start date.");

        try { return SearchCurrent(actor, normalizedQuery, normalizedType, from, to, includeArchived, limit, cursor); }
        catch (SqlException) { return Failure<SearchPage>("SearchUnavailable", 503, "Search is temporarily unavailable."); }
    }

    private IdentityOperationResult<SearchPage> SearchCurrent(IdentityPrincipal actor, string normalizedQuery,
        string? normalizedType, DateOnly? from, DateOnly? to, bool includeArchived, int? limit, string? cursor)
    {
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var result = SearchWithin(connection, transaction, actor, normalizedQuery, normalizedType, from, to, includeArchived, limit, cursor);
        if (result.Succeeded) transaction.Commit();
        return result;
    }

    // Discovery-owned participant for running a stored definition under the same
    // current-query/source authority and SQL transaction. The caller commits.
    internal IdentityOperationResult<SearchPage> SearchWithin(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string normalizedQuery, string? normalizedType, DateOnly? from, DateOnly? to,
        bool includeArchived, int? limit, string? cursor)
    {
        if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.search.query"))
            return Failure<SearchPage>("ModuleUnavailable", 409, "Search is disabled or unavailable for this user.");
        var take = Math.Clamp(limit ?? MaxLimit, 1, MaxLimit);
        var dateRange = SqlOwnerLocalDateRange.Read(connection, transaction, actor, from, to);
        if (!dateRange.Succeeded) return Failure<SearchPage>(dateRange.Code, dateRange.StatusCode, dateRange.Title);
        var range = dateRange.Value!;
        var fromUtc = range.FromUtc;
        var toUtc = range.ToUtc;
        var cursorContext = new SearchCursor.Context(actor.OwnerId, normalizedQuery, normalizedType, from, to, includeArchived, take,
            range.TimeZoneId, range.TimeZoneVersion, fromUtc, toUtc);
        SearchCursor.Position? after = null;
        if (!string.IsNullOrEmpty(cursor) && !_cursor.TryDecode(cursor, cursorContext, out after))
            return Failure<SearchPage>("InvalidCursor", 422, "Search cursor does not match this query.");
        if (normalizedQuery.Length == 0)
        {

            return IdentityOperationResult<SearchPage>.Success(new SearchPage(
                normalizedQuery, normalizedType, includeArchived,
                Array.Empty<SearchResult>(), Array.Empty<SearchProviderStatus>(), null));
        }

        var like = $"%{EscapeLike(normalizedQuery)}%";
        var starts = $"{EscapeLike(normalizedQuery)}%";

        var candidates = new List<Candidate>();
        var providers = new List<SearchProviderStatus>();
        var taskSourceAvailable = ModuleAvailable(connection, transaction, actor, "FX12", "tasks.task.read");
        AddSource(connection, transaction, actor, normalizedType, "Project", "FX11", "projects.project.read", null,
            "/modules/FX11", """
            SELECT TOP (@Limit) p.[Id], p.[Name] AS [Title], p.[Description] AS [Snippet], p.[Status], p.[UpdatedAt],
                   CONVERT(int, CASE WHEN p.[Name] = @Query THEN 0 WHEN p.[Name] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Project] p
            WHERE p.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Project'
                    AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL) AND p.[Status] <> 'Deleted'
              AND (@IncludeArchived = 1 OR p.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR p.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR p.[UpdatedAt] < @ToUtc)
              AND (p.[Name] LIKE @Like ESCAPE N'\' OR p.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], p.[UpdatedAt] DESC, p.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);
        AddSource(connection, transaction, actor, normalizedType, "Task", "FX12", "tasks.task.read", null,
            "/modules/FX12", """
            SELECT TOP (@Limit) t.[Id], t.[Title], t.[Description] AS [Snippet], t.[Status], t.[UpdatedAt],
                   CONVERT(int, CASE WHEN t.[Title] = @Query THEN 0 WHEN t.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Task] t
            WHERE t.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = t.[OwnerId] AND tr.[ResourceType] = 'Task'
                    AND tr.[ResourceId] = t.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL) AND t.[Status] <> 'Deleted'
              AND (@FromUtc IS NULL OR t.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR t.[UpdatedAt] < @ToUtc)
              AND (t.[Title] LIKE @Like ESCAPE N'\' OR t.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], t.[UpdatedAt] DESC, t.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);
        AddSource(connection, transaction, actor, normalizedType, "Event", "FX13", "calendar.event.read", null,
            "/modules/FX13", """
            SELECT TOP (@Limit) e.[Id], e.[Title], e.[Description] AS [Snippet], e.[Status], e.[UpdatedAt],
                   CONVERT(int, CASE WHEN e.[Title] = @Query THEN 0 WHEN e.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [calendar].[Event] e
            LEFT JOIN [productivity].[Task] taskRow
              ON taskRow.[Id] = e.[TaskId] AND taskRow.[OwnerId] = e.[OwnerId]
            WHERE e.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = e.[OwnerId] AND tr.[ResourceType] = 'Event'
                    AND tr.[ResourceId] = e.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL) AND e.[Status] <> 'Deleted'
              AND (e.[TaskId] IS NULL OR (@TaskSourceAvailable = 1 AND taskRow.[Id] IS NOT NULL AND taskRow.[Status] <> 'Deleted'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] taskTrash
                      WHERE taskTrash.[OwnerId] = taskRow.[OwnerId] AND taskTrash.[ResourceType] = 'Task'
                        AND taskTrash.[ResourceId] = taskRow.[Id] AND taskTrash.[RestoredAt] IS NULL AND taskTrash.[PurgedAt] IS NULL)))
              AND (@FromUtc IS NULL OR e.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR e.[UpdatedAt] < @ToUtc)
              AND (e.[Title] LIKE @Like ESCAPE N'\' OR e.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], e.[UpdatedAt] DESC, e.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after, taskSourceAvailable);
        AddSource(connection, transaction, actor, normalizedType, "Document", "FX20", "documents.page.read", "documents.library.read",
            "/modules/FX20", """
            SELECT TOP (@Limit) p.[Id], p.[Title], LEFT(p.[Body], 512) AS [Snippet], p.[Status], p.[UpdatedAt],
                   CONVERT(int, CASE WHEN p.[Title] = @Query THEN 0 WHEN p.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [documents].[Page] p
            WHERE p.[OwnerId] = @OwnerId
              AND EXISTS(SELECT 1 FROM [documents].[VisiblePageV1] visible WHERE visible.OwnerId=p.OwnerId AND visible.Id=p.Id)
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Document'
                    AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
              AND p.[DeletedAt] IS NULL
              AND (@IncludeArchived = 1 OR p.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR p.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR p.[UpdatedAt] < @ToUtc)
              AND (p.[Title] LIKE @Like ESCAPE N'\' OR p.[Body] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], p.[UpdatedAt] DESC, p.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);
        AddSource(connection, transaction, actor, normalizedType, "Bookmark", "FX21", "bookmarks.bookmark.read", null,
            "/bookmarks", """
            SELECT TOP (@Limit) b.[Id], b.[Title], b.[Description] AS [Snippet], b.[Status], b.[UpdatedAt],
                   CONVERT(int, CASE WHEN b.[Title] = @Query THEN 0 WHEN b.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [knowledge].[Bookmark] b
            WHERE b.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = b.[OwnerId] AND tr.[ResourceType] = 'Bookmark'
                    AND tr.[ResourceId] = b.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
              AND b.[Status] IN ('Active', 'Archived')
              AND (@IncludeArchived = 1 OR b.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR b.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR b.[UpdatedAt] < @ToUtc)
              AND (b.[Title] LIKE @Like ESCAPE N'\' OR b.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], b.[UpdatedAt] DESC, b.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);
        AddSource(connection, transaction, actor, normalizedType, "Snippet", "FX22", "snippets.snippet.read", null,
            "/snippets", """
            SELECT TOP (@Limit) s.[Id], v.[Title], LEFT(v.[Description], 512) AS [Snippet], s.[Status], s.[UpdatedAt],
                   CONVERT(int, CASE WHEN v.[Title] = @Query THEN 0 WHEN v.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [knowledge].[Snippet] s
            INNER JOIN [knowledge].[SnippetVersion] v
                ON v.[OwnerId] = s.[OwnerId] AND v.[SnippetId] = s.[Id] AND v.[VersionNumber] = s.[CurrentVersion]
            WHERE s.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = s.[OwnerId] AND tr.[ResourceType] = 'Snippet'
                    AND tr.[ResourceId] = s.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL)
              AND s.[Status] IN ('Active', 'Archived')
              AND (@IncludeArchived = 1 OR s.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR s.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR s.[UpdatedAt] < @ToUtc)
              AND (v.[Title] LIKE @Like ESCAPE N'\' OR v.[Language] LIKE @Like ESCAPE N'\' OR v.[SourceText] LIKE @Like ESCAPE N'\' OR v.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], s.[UpdatedAt] DESC, s.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);
        AddSource(connection, transaction, actor, normalizedType, "Goal", "FX16", "goals.goal.read", null,
            "/goals", """
            SELECT TOP (@Limit) g.[Id], g.[Title], g.[Description] AS [Snippet], g.[Status], g.[UpdatedAt],
                   CONVERT(int, CASE WHEN g.[Title] = @Query THEN 0 WHEN g.[Title] LIKE @Starts ESCAPE N'\' THEN 1 ELSE 2 END) AS [Score],
                   COUNT_BIG(1) OVER() AS [TotalCount]
            FROM [productivity].[Goal] g
            WHERE g.[OwnerId] = @OwnerId
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                  WHERE tr.[OwnerId] = g.[OwnerId] AND tr.[ResourceType] = 'Goal'
                    AND tr.[ResourceId] = g.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL) AND g.[Status] <> 'Deleted'
              AND (@IncludeArchived = 1 OR g.[Status] <> 'Archived')
              AND (@FromUtc IS NULL OR g.[UpdatedAt] >= @FromUtc)
              AND (@ToUtc IS NULL OR g.[UpdatedAt] < @ToUtc)
              AND (g.[Title] LIKE @Like ESCAPE N'\' OR g.[Description] LIKE @Like ESCAPE N'\')
            ORDER BY [Score], g.[UpdatedAt] DESC, g.[Id] ASC;
            """, normalizedQuery, like, starts, fromUtc, toUtc, includeArchived, take + 1, candidates, providers, after);

        var ordered = candidates
            .OrderBy(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Result.UpdatedAt)
            .ThenBy(candidate => new SqlGuid(candidate.Result.Id))
            .ThenBy(candidate => candidate.Result.ResourceType, StringComparer.Ordinal)
            .Take(take + 1)
            .ToArray();
        var pageRows = ordered.Take(take).ToArray();
        var items = pageRows.Select(candidate => candidate.Result).ToArray();
        var last = pageRows.LastOrDefault();
        var nextCursor = ordered.Length > take && last is not null
            ? _cursor.Encode(cursorContext, new SearchCursor.Position(last.Score, last.Result.UpdatedAt.UtcDateTime,
                last.Result.Id, last.Result.ResourceType)) : null;
        if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.search.query"))
            return Failure<SearchPage>("ModuleUnavailable", 409, "Search is disabled or unavailable for this user.");

        return IdentityOperationResult<SearchPage>.Success(new SearchPage(
            normalizedQuery, normalizedType, includeArchived, items, providers, nextCursor));
    }

    private void AddSource(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string? requestedType,
        string resourceType, string moduleCode, string actionKey, string? legacyActionKey, string route,
        string sql, string query, string like, string starts, DateTime? fromUtc, DateTime? toUtc,
        bool includeArchived, int limit, List<Candidate> candidates, List<SearchProviderStatus> providers,
        SearchCursor.Position? after, bool taskSourceAvailable = true)
    {
        if (requestedType is not null && !string.Equals(requestedType, resourceType, StringComparison.Ordinal))
            return;
        if (!ModuleAvailable(connection, transaction, actor, moduleCode, actionKey, legacyActionKey))
        {
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, "Unavailable",
                "Source module or read capability is unavailable.", 0));
            return;
        }
        try
        {
            var sourceCandidates = new List<Candidate>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = 3;
            // Source SQL is trusted and retains all owner/lifecycle filters before count and seek.
            command.CommandText = PageSourceSql(sql);
            Add(command, "@Limit", SqlDbType.Int, Math.Min(limit, PerSourceLimit));
            Add(command, "@AfterScore", SqlDbType.Int, (object?)after?.Score ?? DBNull.Value);
            Add(command, "@AfterUpdatedAt", SqlDbType.DateTime2, (object?)after?.UpdatedAt ?? DBNull.Value);
            Add(command, "@AfterId", SqlDbType.UniqueIdentifier, (object?)after?.Id ?? DBNull.Value);
            Add(command, "@AfterType", SqlDbType.VarChar, (object?)after?.ResourceType ?? DBNull.Value, 32);
            Add(command, "@SourceType", SqlDbType.VarChar, resourceType, 32);
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@Query", SqlDbType.NVarChar, query, 500);
            Add(command, "@Like", SqlDbType.NVarChar, like, 1004);
            Add(command, "@Starts", SqlDbType.NVarChar, starts, 1002);
            Add(command, "@FromUtc", SqlDbType.DateTime2, (object?)fromUtc ?? DBNull.Value);
            Add(command, "@ToUtc", SqlDbType.DateTime2, (object?)toUtc ?? DBNull.Value);
            Add(command, "@IncludeArchived", SqlDbType.Bit, includeArchived);
            Add(command, "@TaskSourceAvailable", SqlDbType.Bit, taskSourceAvailable);
            using var reader = command.ExecuteReader();
            var total = 0;
            while (reader.Read())
            {
                total = Math.Max(total, (int)Math.Min(int.MaxValue, reader.GetInt64(6)));
                if (reader.IsDBNull(0)) continue;
                var title = reader.GetString(1);
                var snippet = reader.IsDBNull(2) ? null : reader.GetString(2);
                var status = reader.IsDBNull(3) ? null : reader.GetString(3);
                var updatedAt = new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc), TimeSpan.Zero);
                var resourceId = reader.GetGuid(0);
                sourceCandidates.Add(new Candidate(new SearchResult(resourceId, resourceType, moduleCode,
                    title, SafeSnippet(snippet), status, updatedAt, RouteFor(resourceType, resourceId)), reader.GetInt32(5)));
            }
            reader.Close();
            if (!ModuleAvailable(connection, transaction, actor, moduleCode, actionKey, legacyActionKey))
            {
                providers.Add(new SearchProviderStatus(resourceType, moduleCode, "Unavailable",
                    "Source module or read capability is unavailable.", 0));
                return;
            }
            candidates.AddRange(sourceCandidates);
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, total == 0 ? "Empty" : "Ready", null, total));
        }
        catch (SqlException)
        {
            providers.Add(new SearchProviderStatus(resourceType, moduleCode, "Degraded",
                "This source is temporarily unavailable.", 0));
        }
    }

    private static string PageSourceSql(string sourceSql)
    {
        var order = sourceSql.LastIndexOf("ORDER BY", StringComparison.Ordinal);
        var matches = sourceSql[..order].Replace("SELECT TOP (@Limit)", "SELECT", StringComparison.Ordinal);
        return $"""
            WITH sourceMatches AS ({matches}),
            sourceCount AS (SELECT COALESCE(MAX([TotalCount]), CONVERT(bigint, 0)) AS [TotalCount] FROM sourceMatches)
            SELECT pageRow.[Id], pageRow.[Title], pageRow.[Snippet], pageRow.[Status], pageRow.[UpdatedAt], pageRow.[Score], sourceCount.[TotalCount]
            FROM sourceCount
            OUTER APPLY (
                SELECT TOP (@Limit) [Id], [Title], [Snippet], [Status], [UpdatedAt], [Score]
                FROM sourceMatches
                WHERE @AfterScore IS NULL OR [Score] > @AfterScore
                   OR ([Score] = @AfterScore AND ([UpdatedAt] < @AfterUpdatedAt
                   OR ([UpdatedAt] = @AfterUpdatedAt AND ([Id] > @AfterId
                   OR ([Id] = @AfterId AND @SourceType COLLATE Latin1_General_100_BIN2 > @AfterType COLLATE Latin1_General_100_BIN2)))))
                ORDER BY [Score], [UpdatedAt] DESC, [Id] ASC
            ) pageRow
            ORDER BY pageRow.[Score], pageRow.[UpdatedAt] DESC, pageRow.[Id] ASC;
            """;
    }

    private bool ModuleAvailable(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string moduleCode, string actionKey, string? legacyActionKey = null) => legacyActionKey is null
        ? _capabilities.IsAllowed(connection, transaction, actor, moduleCode, actionKey)
        : _capabilities.IsAllowed(connection, transaction, actor, moduleCode, actionKey, legacyActionKey);

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

    private static string RouteFor(string resourceType, Guid resourceId) =>
        $"/resources/{Uri.EscapeDataString(resourceType)}/{resourceId:N}";

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
