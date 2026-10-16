using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Bookmarks;
using Nexora.Application.Identity;
using Nexora.Application.Snippets;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Discovery;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Snippets;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlSearchPaginationAuthorityTests(SqlApiFixture fixture)
{
    private SqlConnectionFactory Connections() => new(fixture.ConnectionString!);
    private SqlSearchService Search() => new(Connections(), "synthetic-search-cursor-key");
    private SqlBookmarkService Bookmarks() => new(Connections(), "synthetic-search-source-key");
    private SqlSnippetService Snippets() => new(Connections(), "synthetic-search-source-key");
    private static IdentityPrincipal Actor(SyntheticSession session, string role = "User") => new(session.UserId, session.OwnerId, role, DateTimeOffset.UtcNow, session.SessionId);
    private static BookmarkCommand Bookmark(string title, string? description = null) => new("https://example.invalid/search", title, description);

    [Fact]
    public async Task Mixed_sources_paginate_every_hit_with_one_SQL_GUID_order_and_stable_full_counts()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmarks = Bookmarks(); var snippets = Snippets(); var expected = new List<(string Type, Guid Id)>();
        for (var index = 0; index < 14; index++)
        {
            var bookmark = bookmarks.Create(actor, Bookmark("needle")); Assert.True(bookmark.Succeeded, bookmark.Code); expected.Add(("Bookmark", bookmark.Value!.Id));
            var snippet = snippets.Create(actor, new SnippetCommand("needle", "text", "private source remains inert", "safe snippet")); Assert.True(snippet.Succeeded, snippet.Code); expected.Add(("Snippet", snippet.Value!.Id));
        }
        // Source identity includes its type: unrelated tables may legitimately contain the same UUID.
        await fixture.ExecuteAsync("UPDATE [knowledge].[Bookmark] SET [Id] = @SharedId WHERE [OwnerId] = @Owner AND [Id] = @OldId;",
            P("@SharedId", expected[1].Id), P("@OldId", expected[0].Id), P("@Owner", actor.OwnerId));
        expected[0] = ("Bookmark", expected[1].Id);
        var fixedTime = new DateTime(2026, 10, 13, 3, 0, 0, DateTimeKind.Utc);
        await fixture.ExecuteAsync("""
            UPDATE [knowledge].[Bookmark] SET [UpdatedAt] = @At WHERE [OwnerId] = @Owner;
            UPDATE [knowledge].[Snippet] SET [UpdatedAt] = @At WHERE [OwnerId] = @Owner;
            """, P("@Owner", actor.OwnerId), new SqlParameter("@At", SqlDbType.DateTime2) { Value = fixedTime });
        var service = Search(); var first = service.Search(actor, "needle"); Assert.True(first.Succeeded, first.Code); Assert.Equal(25, first.Value!.Items.Count); Assert.NotNull(first.Value.NextCursor);
        Assert.DoesNotContain(first.Value.Providers, p => p.State == "Degraded");
        Assert.Equal(14, Assert.Single(first.Value.Providers, p => p.ResourceType == "Bookmark").Count);
        var next = service.Search(actor, "needle", cursor: first.Value.NextCursor); Assert.True(next.Succeeded, next.Code); Assert.Equal(3, next.Value!.Items.Count); Assert.Null(next.Value.NextCursor);
        Assert.Equal(14, Assert.Single(next.Value.Providers, p => p.ResourceType == "Bookmark").Count); // A consumed provider still has14 full-query matches.
        var ordered = expected.OrderBy(item => new SqlGuid(item.Id)).ThenBy(item => item.Type, StringComparer.Ordinal);
        Assert.Equal(ordered, first.Value.Items.Concat(next.Value.Items).Select(item => (item.ResourceType, item.Id)));
        Assert.DoesNotContain(first.Value.Items.Concat(next.Value.Items), item => item.Snippet?.Contains("private source", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task Cursor_binds_owner_query_type_date_archive_and_limit_and_API_uses_its_own_cursor_secret()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        for (var index = 0; index < 3; index++) Assert.True(Bookmarks().Create(actor, Bookmark("needle" + index)).Succeeded);
        var service = Search(); var first = service.Search(actor, "needle", resourceType: "Bookmark", limit: 1).Value!; Assert.NotNull(first.NextCursor);
        Assert.Equal("InvalidCursor", service.Search(Actor(other), "needle", "Bookmark", limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "changed", "Bookmark", limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Snippet", limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Bookmark", from: new DateOnly(2026, 1, 1), limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Bookmark", to: new DateOnly(2026, 12, 31), limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Bookmark", includeArchived: true, limit: 1, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Bookmark", limit: 2, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.Search(actor, "needle", "Bookmark", limit: 1, cursor: first.NextCursor + "!").Code);
        using var apiFirst = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/search?q=needle&resourceType=Bookmark&limit=1", session.RawSessionHandle); Assert.Equal(HttpStatusCode.OK, apiFirst.StatusCode);
        using var firstJson = JsonDocument.Parse(await apiFirst.Content.ReadAsStringAsync()); var cursor = firstJson.RootElement.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        var firstId = firstJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();
        using var apiNext = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/search?q=needle&resourceType=Bookmark&limit=1&cursor=" + Uri.EscapeDataString(cursor), session.RawSessionHandle); Assert.Equal(HttpStatusCode.OK, apiNext.StatusCode);
        using var nextJson = JsonDocument.Parse(await apiNext.Content.ReadAsStringAsync()); Assert.NotEqual(firstId, nextJson.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Empty(service.Search(Actor(other), "needle").Value!.Items);
    }

    [Fact]
    public async Task Revoked_source_read_on_continuation_exposes_no_historical_snippet_or_count()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        foreach (var action in new[] { "discovery.search.query", "bookmarks.bookmark.create", "bookmarks.bookmark.read" }) await Grant(session.UserId, action);
        for (var index = 0; index < 3; index++) Assert.True(Bookmarks().Create(actor, Bookmark("needle" + index, "private safe metadata")).Succeeded);
        var service = Search(); var first = service.Search(actor, "needle", limit: 1).Value!; Assert.Single(first.Items); Assert.NotNull(first.NextCursor);
        await fixture.ExecuteAsync("""
            DELETE ap FROM [platform].[AdminPermission] ap INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
            WHERE ap.[UserId] = @User AND p.[ActionKey] = 'bookmarks.bookmark.read';
            """, P("@User", actor.UserId));
        var next = service.Search(actor, "needle", limit: 1, cursor: first.NextCursor); Assert.True(next.Succeeded, next.Code); Assert.Empty(next.Value!.Items); Assert.Null(next.Value.NextCursor);
        var provider = Assert.Single(next.Value.Providers, p => p.ResourceType == "Bookmark"); Assert.Equal("Unavailable", provider.State); Assert.Equal(0, provider.Count);
        await fixture.SetModuleEnabledAsync(session.UserId, "FX25", false); Assert.False(service.Search(actor, "needle", limit: 1, cursor: first.NextCursor).Succeeded);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    [InlineData("\\")]
    public async Task Search_wildcards_are_literal_and_query_is_bounded(string literal)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        Assert.True(Bookmarks().Create(actor, Bookmark("ordinary")).Succeeded); var special = Bookmarks().Create(actor, Bookmark("literal" + literal)); Assert.True(special.Succeeded);
        var page = Search().Search(actor, literal, "Bookmark"); Assert.True(page.Succeeded); Assert.Equal(special.Value!.Id, Assert.Single(page.Value!.Items).Id);
        Assert.Equal("ValidationFailed", Search().Search(actor, new string('a', 501)).Code);
    }

    [Fact]
    public async Task Active_Trash_and_deleted_Documents_never_match_even_with_archive_included_and_body_match_is_authorized()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var bookmarks = Bookmarks();
        var bookmark = bookmarks.Create(actor, Bookmark("needle bookmark")).Value!;
        var documentService = new SqlDocumentService(Connections(), "synthetic-search-source-key");
        var document = documentService.Create(actor, new("unrelated document title", "Document", "Markdown", "needle <script>inert</script>")).Value!;
        var service = Search(); var bodyMatch = service.Search(actor, "needle", "Document"); Assert.True(bodyMatch.Succeeded);
        var hit = Assert.Single(bodyMatch.Value!.Items); Assert.Equal(document.Id, hit.Id); Assert.Contains("‹script›", hit.Snippet!); Assert.DoesNotContain("<script>", hit.Snippet!);
        await fixture.ExecuteAsync("""
            INSERT INTO [platform].[TrashItem] ([Id],[OwnerId],[ResourceType],[ResourceId],[DeletionBatchId],[PriorStatus])
            VALUES (NEWID(),@Owner,'Bookmark',@Bookmark,NEWID(),'Active');
            UPDATE [documents].[Page] SET [DeletedAt] = SYSUTCDATETIME() WHERE [OwnerId] = @Owner AND [Id] = @Document;
            """, P("@Owner", actor.OwnerId), P("@Bookmark", bookmark.Id), P("@Document", document.Id));
        var after = service.Search(actor, "needle", includeArchived: true); Assert.True(after.Succeeded, after.Code); Assert.Empty(after.Value!.Items);
        Assert.Equal(0, Assert.Single(after.Value.Providers, p => p.ResourceType == "Document").Count);
        Assert.Equal(0, Assert.Single(after.Value.Providers, p => p.ResourceType == "Bookmark").Count);
    }

    [Fact]
    public async Task Rank_seek_prioritizes_exact_then_prefix_then_body_and_max_date_fails_safely()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var bookmarks = Bookmarks();
        var body = bookmarks.Create(actor, Bookmark("other", "needle body match")).Value!;
        var prefix = bookmarks.Create(actor, Bookmark("needle prefix")).Value!; var exact = bookmarks.Create(actor, Bookmark("needle")).Value!;
        var service = Search(); var first = service.Search(actor, "needle", "Bookmark", limit: 1).Value!; Assert.Equal(exact.Id, Assert.Single(first.Items).Id);
        var second = service.Search(actor, "needle", "Bookmark", limit: 1, cursor: first.NextCursor).Value!; Assert.Equal(prefix.Id, Assert.Single(second.Items).Id);
        var third = service.Search(actor, "needle", "Bookmark", limit: 1, cursor: second.NextCursor).Value!; Assert.Equal(body.Id, Assert.Single(third.Items).Id); Assert.Null(third.NextCursor);
        Assert.Equal("DateRangeUnavailable", service.Search(actor, "needle", to: DateOnly.MaxValue).Code);
        Assert.Empty(service.Search(actor, " ").Value!.Items); Assert.Equal("ValidationFailed", service.Search(actor, "needle", "Vault").Code);
        Assert.Equal("ValidationFailed", service.Search(actor, "needle", from: new DateOnly(2026, 2, 2), to: new DateOnly(2026, 1, 1)).Code);
    }

    [Fact]
    public async Task Failed_provider_is_Degraded_while_another_provider_remains_authorized_and_ready()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        Assert.True(Snippets().Create(actor, new("needle", "text", "inert", "safe snippet")).Succeeded);
        await fixture.ExecuteAsync("EXEC sp_rename N'knowledge.Bookmark', N'BookmarkUnavailableForSearchTest';");
        try
        {
            var result = Search().Search(actor, "needle"); Assert.True(result.Succeeded, result.Code);
            Assert.Equal("Snippet", Assert.Single(result.Value!.Items).ResourceType);
            Assert.Equal("Degraded", Assert.Single(result.Value.Providers, p => p.ResourceType == "Bookmark").State);
            Assert.Equal("Ready", Assert.Single(result.Value.Providers, p => p.ResourceType == "Snippet").State);
        }
        finally { await fixture.ExecuteAsync("EXEC sp_rename N'knowledge.BookmarkUnavailableForSearchTest', N'Bookmark';"); }
    }

    [Theory]
    [InlineData("Etc/UTC", "2026-10-13", "2026-10-13T00:00:00Z", "2026-10-14T00:00:00Z")]
    [InlineData("Asia/Bangkok", "2026-10-13", "2026-10-12T17:00:00Z", "2026-10-13T17:00:00Z")]
    [InlineData("America/New_York", "2026-03-08", "2026-03-08T05:00:00Z", "2026-03-09T04:00:00Z")]
    [InlineData("America/New_York", "2026-11-01", "2026-11-01T04:00:00Z", "2026-11-02T05:00:00Z")]
    public async Task Profile_local_day_filters_include_start_exclude_next_day_and_preserve_short_long_DST_days(
        string zone, string dayText, string firstUtc, string nextUtc)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        await SetZone(actor.UserId, zone);
        var day = DateOnly.ParseExact(dayText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var start = DateTimeOffset.Parse(firstUtc, System.Globalization.CultureInfo.InvariantCulture).UtcDateTime;
        var end = DateTimeOffset.Parse(nextUtc, System.Globalization.CultureInfo.InvariantCulture).UtcDateTime;
        var values = new[] { start.AddTicks(-1), start, end.AddTicks(-1), end }; var expected = new List<Guid>();
        for (var index = 0; index < values.Length; index++)
        {
            var result = Bookmarks().Create(actor, Bookmark("date needle " + index)); Assert.True(result.Succeeded, result.Code);
            var id = result.Value!.Id;
            await fixture.ExecuteAsync("UPDATE [knowledge].[Bookmark] SET [UpdatedAt] = @At WHERE [OwnerId] = @Owner AND [Id] = @Id;",
                P("@Owner", actor.OwnerId), P("@Id", id), new SqlParameter("@At", SqlDbType.DateTime2) { Scale = 7, Value = values[index] });
            if (index is 1 or 2) expected.Add(id);
        }
        var page = Search().Search(actor, "date needle", "Bookmark", from: day, to: day); Assert.True(page.Succeeded, page.Code);
        Assert.Equal(expected.Order(), page.Value!.Items.Select(item => item.Id).Order());
        Assert.Equal(2, Assert.Single(page.Value.Providers).Count);
    }

    [Fact]
    public async Task Dated_cursor_rejects_profile_timezone_change_and_unknown_zone_never_guesses_UTC()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        for (var index = 0; index < 3; index++) Assert.True(Bookmarks().Create(actor, Bookmark("timezone needle" + index)).Succeeded);
        var service = Search(); var from = new DateOnly(2020, 1, 1); var to = new DateOnly(2030, 1, 1);
        var first = service.Search(actor, "timezone needle", "Bookmark", from, to, limit: 1); Assert.True(first.Succeeded); Assert.NotNull(first.Value!.NextCursor);
        await SetZone(actor.UserId, "Asia/Bangkok");
        Assert.Equal("InvalidCursor", service.Search(actor, "timezone needle", "Bookmark", from, to, limit: 1, cursor: first.Value.NextCursor).Code);
        await SetZone(actor.UserId, "Invalid/NotAnIanaZone");
        var unavailable = service.Search(actor, "timezone needle", "Bookmark", from, to);
        Assert.False(unavailable.Succeeded); Assert.Equal("TimeZoneUnavailable", unavailable.Code); Assert.Equal(503, unavailable.StatusCode);
        Assert.True(service.Search(actor, "timezone needle", "Bookmark").Succeeded); // No-date search intentionally has no profile-day dependency.
    }

    [Fact]
    public async Task Entire_skipped_local_day_is_rejected_instead_of_clamped_or_returned_as_empty()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        await SetZone(actor.UserId, "Pacific/Apia");
        var result = Search().Search(actor, "needle", from: new DateOnly(2011, 12, 30), to: new DateOnly(2011, 12, 30));
        Assert.False(result.Succeeded); Assert.Equal("DateRangeUnavailable", result.Code); Assert.Equal(422, result.StatusCode);
    }

    private Task SetZone(Guid user, string zone) => fixture.ExecuteAsync("UPDATE [identity].[User] SET [TimeZoneId] = @Zone WHERE [Id] = @User;",
        P("@User", user), new SqlParameter("@Zone", SqlDbType.NVarChar, 128) { Value = zone });

    private static SqlParameter P(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private Task Grant(Guid user, string action) => fixture.ExecuteAsync("""
        INSERT INTO [platform].[AdminPermission] ([UserId], [PermissionId], [Effect]) SELECT @User, [Id], 'Allow'
        FROM [platform].[Permission] WHERE [ActionKey] = @Action;
        """, P("@User", user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
}
