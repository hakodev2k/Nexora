using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Bookmarks;
using Nexora.Application.Identity;
using Nexora.Application.Reading;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Reading;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlBookmarkReadingAuthorityTests(SqlApiFixture fixture)
{
    private SqlBookmarkService Bookmarks() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-bookmark-reading-key");
    private SqlReadingService Reading() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-bookmark-reading-key");
    private static IdentityPrincipal Actor(SyntheticSession session, string role = "User") => new(session.UserId, session.OwnerId, role, DateTimeOffset.UtcNow, session.SessionId);
    private static BookmarkCommand Bookmark(string title = "inert owner metadata") => new("https://example.invalid/path?owner=synthetic", title, "safe description");

    [Fact]
    public async Task Bookmark_command_only_ACK_replays_without_elevation_after_new_read_grant()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        await Grant(session.UserId, "bookmarks.bookmark.create"); await Grant(session.UserId, "bookmarks.bookmark.archive");
        var service = Bookmarks(); var key = Guid.NewGuid().ToString();
        var created = service.Create(actor, Bookmark(), key);
        Assert.True(created.Succeeded, created.Code); Assert.Null(created.Value); Assert.True(created.Acknowledgement!.Committed);
        Assert.False(created.Acknowledgement.DetailAvailable); Assert.Equal(created, service.Create(actor, Bookmark(), key));
        Assert.Equal("IdempotencyConflict", service.Create(actor, Bookmark("changed|description:value"), key).Code);
        Assert.Equal(1, await CountBookmarks(session.OwnerId));
        await Grant(session.UserId, "bookmarks.bookmark.read");
        Assert.Equal(created, service.Create(actor, Bookmark(), key)); // A completed opaque protocol result stays opaque.
        var current = service.Get(actor, created.Acknowledgement.Id).Value!;
        await RemoveGrant(session.UserId, "bookmarks.bookmark.read");
        var transitionKey = Guid.NewGuid().ToString();
        var archived = service.Transition(actor, current.Id, current.ETag, "Archived", transitionKey);
        Assert.True(archived.Succeeded, archived.Code); Assert.Null(archived.Value); Assert.Equal(current.Id, archived.Acknowledgement!.Id);
        await Grant(session.UserId, "bookmarks.bookmark.read");
        Assert.Equal(archived, service.Transition(actor, current.Id, current.ETag, "Archived", transitionKey));
        await RemoveGrant(session.UserId, "bookmarks.bookmark.archive");
        Assert.False(service.Transition(actor, current.Id, current.ETag, "Archived", transitionKey).Succeeded);
        await RemoveGrant(session.UserId, "bookmarks.bookmark.read");
        var csrf = await fixture.GetCsrfAsync();
        using var api = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/bookmarks", Bookmark(), csrf, Guid.NewGuid(), session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, api.StatusCode);
        using var body = JsonDocument.Parse(await api.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "committed", "detailAvailable", "id" }, body.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.True(body.RootElement.GetProperty("committed").GetBoolean()); Assert.False(body.RootElement.GetProperty("detailAvailable").GetBoolean());
    }

    [Fact]
    public async Task Bookmark_update_requires_current_read_exact_ETag_and_never_replays_stale_details()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        foreach (var action in new[] { "bookmarks.bookmark.create", "bookmarks.bookmark.read", "bookmarks.bookmark.update" }) await Grant(session.UserId, action);
        var service = Bookmarks(); var created = service.Create(actor, Bookmark()).Value!; var key = Guid.NewGuid().ToString();
        var updated = service.Update(actor, created.Id, created.ETag, Bookmark("owner changed"), key);
        Assert.True(updated.Succeeded, updated.Code); Assert.Equal(updated, service.Update(actor, created.Id, created.ETag, Bookmark("owner changed"), key));
        Assert.Equal("PreconditionRequired", service.Update(actor, created.Id, updated.Value!.ETag.Trim('"'), Bookmark()).Code);
        var newest = service.Update(actor, created.Id, updated.Value.ETag, Bookmark("newest owner metadata")); Assert.True(newest.Succeeded, newest.Code);
        var replay = service.Update(actor, created.Id, created.ETag, Bookmark("owner changed"), key);
        Assert.True(replay.Succeeded); Assert.Null(replay.Value); Assert.NotNull(replay.Acknowledgement);
        await RemoveGrant(session.UserId, "bookmarks.bookmark.read");
        Assert.False(service.Update(actor, created.Id, newest.Value!.ETag, Bookmark()).Succeeded);
        Assert.False(service.Update(actor, created.Id, created.ETag, Bookmark("owner changed"), key).Succeeded);
        await Grant(session.UserId, "bookmarks.bookmark.read"); Assert.Equal("newest owner metadata", service.Get(actor, created.Id).Value!.Title);
    }

    [Fact]
    public async Task Archived_bookmark_metadata_is_readonly_until_explicit_unarchive()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Bookmarks();
        var created = service.Create(actor, Bookmark()).Value!; var archived = service.Transition(actor, created.Id, created.ETag, "Archived").Value!;
        Assert.Empty(service.List(actor).Value!.Items); Assert.Equal(archived.Id, Assert.Single(service.List(actor, includeArchived: true).Value!.Items).Id);
        Assert.Equal("LifecycleLocked", service.Update(actor, archived.Id, archived.ETag, Bookmark("denied archived edit")).Code);
        Assert.Equal(created.Title, service.Get(actor, created.Id).Value!.Title);
        var active = service.Transition(actor, archived.Id, archived.ETag, "Active").Value!;
        Assert.True(service.Update(actor, active.Id, active.ETag, Bookmark("allowed after unarchive")).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Bookmark_before_commit_authority_recheck_rolls_back_fresh_and_keyless_writes(bool keyed)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        await fixture.ExecuteAsync("""
            CREATE TRIGGER [knowledge].[RevokeBookmarkAuthorityForTest] ON [knowledge].[Bookmark] AFTER INSERT AS
            BEGIN
                UPDATE g SET [Enabled] = 0 FROM [platform].[UserModuleGrant] g
                INNER JOIN [platform].[Module] m ON m.[Id] = g.[ModuleId] AND m.[Code] = 'FX21'
                INNER JOIN inserted i ON i.[CreatedByUserId] = g.[UserId];
            END;
            """);
        try
        {
            var result = Bookmarks().Create(actor, Bookmark(), keyed ? Guid.NewGuid().ToString() : null);
            Assert.False(result.Succeeded); Assert.Equal("ModuleUnavailable", result.Code); Assert.Equal(0, await CountBookmarks(session.OwnerId));
            Assert.True(Bookmarks().List(actor).Succeeded);
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [knowledge].[RevokeBookmarkAuthorityForTest];"); }
    }

    [Fact]
    public async Task Both_lists_reach_all_rows_bind_cursors_and_reject_cross_owner_references()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var actor = Actor(session); var bookmarks = Bookmarks(); var reading = Reading(); var ids = new List<Guid>();
        for (var index = 0; index < 27; index++)
        {
            var bookmark = bookmarks.Create(actor, Bookmark($"Page {index:D3}")); Assert.True(bookmark.Succeeded, bookmark.Code); ids.Add(bookmark.Value!.Id);
            Assert.True(reading.Save(actor, new("Bookmark", bookmark.Value.Id)).Succeeded);
        }
        var first = bookmarks.List(actor).Value!; var next = bookmarks.List(actor, cursor: first.NextCursor).Value!;
        Assert.Equal(25, first.Items.Count); Assert.Equal(2, next.Items.Count); Assert.Null(next.NextCursor);
        Assert.Equal(ids.Order(), first.Items.Concat(next.Items).Select(i => i.Id).Order());
        Assert.Equal("InvalidCursor", bookmarks.List(Actor(other), cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", bookmarks.List(actor, query: "Page", cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", bookmarks.List(actor, includeArchived: true, cursor: first.NextCursor).Code);
        Assert.Equal("InvalidCursor", bookmarks.List(actor, limit: 10, cursor: first.NextCursor).Code);
        Assert.Equal("ResourceUnavailable", bookmarks.Get(Actor(other), ids[0]).Code); Assert.False(reading.Save(Actor(other), new("Bookmark", ids[0])).Succeeded);
        var queue = reading.List(actor).Value!; var queueNext = reading.List(actor, cursor: queue.NextCursor).Value!;
        Assert.Equal(25, queue.Items.Count); Assert.Equal(2, queueNext.Items.Count); Assert.Null(queueNext.NextCursor);
        Assert.Equal(27, queue.Items.Concat(queueNext.Items).Select(i => i.Id).Distinct().Count());
        Assert.Equal("InvalidCursor", reading.List(Actor(other), cursor: queue.NextCursor).Code);
        Assert.Equal("InvalidCursor", reading.List(actor, state: "Read", cursor: queue.NextCursor).Code);
        Assert.Equal("InvalidCursor", reading.List(actor, limit: 10, cursor: queue.NextCursor).Code);
        // API cursors use the API's configured secret, not the separate direct-service test key.
        foreach (var endpoint in new[] { "/api/v1/bookmarks", "/api/v1/read-later" })
        {
            using var apiFirst = await fixture.SendAuthenticatedAsync(HttpMethod.Get, endpoint, session.RawSessionHandle); Assert.Equal(HttpStatusCode.OK, apiFirst.StatusCode);
            using var firstBody = JsonDocument.Parse(await apiFirst.Content.ReadAsStringAsync());
            var cursor = firstBody.RootElement.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
            using var apiNext = await fixture.SendAuthenticatedAsync(HttpMethod.Get, endpoint + "?cursor=" + Uri.EscapeDataString(cursor), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.OK, apiNext.StatusCode);
            using var nextBody = JsonDocument.Parse(await apiNext.Content.ReadAsStringAsync()); Assert.Equal(2, nextBody.RootElement.GetProperty("items").GetArrayLength());
        }
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    [InlineData("~")]
    public async Task Bookmark_search_is_literal_and_bounded(string literal)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Bookmarks();
        Assert.True(service.Create(actor, Bookmark("ordinary")).Succeeded); var special = service.Create(actor, Bookmark("literal" + literal)); Assert.True(special.Succeeded);
        Assert.Equal(special.Value!.Id, Assert.Single(service.List(actor, query: literal).Value!.Items).Id);
        Assert.Equal("ValidationFailed", service.List(actor, query: new string('a', 201)).Code);
    }

    [Fact]
    public async Task Reading_save_update_remove_replay_durably_without_duplicate_effects_or_source_changes()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmark = Bookmarks().Create(actor, Bookmark()).Value!; var service = Reading(); var save = new SaveReadingCommand("Bookmark", bookmark.Id); var saveKey = Guid.NewGuid().ToString();
        var saved = service.Save(actor, save, saveKey); Assert.True(saved.Succeeded, saved.Code); Assert.Equal(saved, service.Save(actor, save, saveKey));
        Assert.Equal(saved.Value, service.Save(actor, save).Value); Assert.Single(service.List(actor).Value!.Items);
        var updateKey = Guid.NewGuid().ToString(); var position = new UpdateReadingCommand("Reading", 0.25m);
        var changed = service.Update(actor, saved.Value!.Id, saved.Value.ETag, position, updateKey); Assert.True(changed.Succeeded, changed.Code);
        Assert.Equal(changed, service.Update(actor, saved.Value.Id, saved.Value.ETag, position, updateKey));
        Assert.Equal("IdempotencyConflict", service.Update(actor, saved.Value.Id, saved.Value.ETag, position with { Progress = 0.5m }, updateKey).Code);
        Assert.Equal("ValidationFailed", service.Update(actor, saved.Value.Id, changed.Value!.ETag, new("Reading", 0.123456789m)).Code);
        Assert.Equal("PreconditionRequired", service.Remove(actor, saved.Value.Id, changed.Value.ETag.Trim('"')).Code);
        var read = service.Update(actor, saved.Value.Id, changed.Value.ETag, new("Read", null)); Assert.True(read.Succeeded); Assert.Equal(0.25m, read.Value!.Progress); Assert.NotNull(read.Value.ReadAt);
        var replay = service.Update(actor, saved.Value.Id, saved.Value.ETag, position, updateKey); Assert.True(replay.Succeeded); Assert.Null(replay.Value); Assert.NotNull(replay.Acknowledgement);
        var removeKey = Guid.NewGuid().ToString(); var removed = service.Remove(actor, read.Value.Id, read.Value.ETag, removeKey);
        Assert.True(removed.Succeeded); Assert.Equal(204, removed.StatusCode); Assert.Equal(removed, service.Remove(actor, read.Value.Id, read.Value.ETag, removeKey));
        Assert.Empty(service.List(actor).Value!.Items); Assert.True(Bookmarks().Get(actor, bookmark.Id).Succeeded);
    }

    [Fact]
    public async Task Reading_command_only_ACK_stays_opaque_and_source_revocation_blocks_replay_but_not_remove()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        foreach (var action in new[] { "bookmarks.bookmark.create", "bookmarks.bookmark.read", "reading.item.save", "reading.item.read", "reading.item.remove" }) await Grant(session.UserId, action);
        var bookmark = Bookmarks().Create(actor, Bookmark()).Value!; var service = Reading(); var saveKey = Guid.NewGuid().ToString(); var save = new SaveReadingCommand("Bookmark", bookmark.Id);
        var saved = service.Save(actor, save, saveKey); Assert.True(saved.Succeeded); Assert.Null(saved.Value); Assert.Equal(saved, service.Save(actor, save, saveKey));
        await Grant(session.UserId, "reading.queue.read"); Assert.Equal(saved, service.Save(actor, save, saveKey));
        var item = Assert.Single(service.List(actor).Value!.Items); await RemoveGrant(session.UserId, "reading.queue.read");
        var readKey = Guid.NewGuid().ToString(); var command = new UpdateReadingCommand("Read", null);
        var read = service.Update(actor, item.Id, item.ETag, command, readKey); Assert.True(read.Succeeded); Assert.Null(read.Value); Assert.NotNull(read.Acknowledgement);
        await Grant(session.UserId, "reading.queue.read"); Assert.Equal(read, service.Update(actor, item.Id, item.ETag, command, readKey));
        var current = Assert.Single(service.List(actor).Value!.Items); await RemoveGrant(session.UserId, "bookmarks.bookmark.read");
        Assert.False(service.Save(actor, save, saveKey).Succeeded); Assert.False(service.Update(actor, item.Id, item.ETag, command, readKey).Succeeded);
        Assert.False(Assert.Single(service.List(actor).Value!.Items).SourceAvailable);
        var removeKey = Guid.NewGuid().ToString(); var removed = service.Remove(actor, current.Id, current.ETag, removeKey);
        Assert.True(removed.Succeeded); Assert.Equal(removed, service.Remove(actor, current.Id, current.ETag, removeKey)); Assert.Equal(1, await CountBookmarks(session.OwnerId));
    }

    [Fact]
    public async Task Missing_source_blocks_state_and_historical_replay_but_queue_remove_remains_available()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmark = Bookmarks().Create(actor, Bookmark()).Value!; var service = Reading(); var key = Guid.NewGuid().ToString(); var save = new SaveReadingCommand("Bookmark", bookmark.Id);
        Assert.True(service.Save(actor, save, key).Succeeded);
        await fixture.ExecuteAsync("DELETE FROM [knowledge].[Bookmark] WHERE [Id] = @Id AND [OwnerId] = @Owner;", P("@Id", bookmark.Id), P("@Owner", actor.OwnerId));
        var item = Assert.Single(service.List(actor).Value!.Items); Assert.False(item.SourceAvailable);
        var replay = service.Save(actor, save, key); Assert.True(replay.Succeeded); Assert.Null(replay.Value); Assert.NotNull(replay.Acknowledgement);
        Assert.Equal("SourceUnavailable", service.Save(actor, save, Guid.NewGuid().ToString()).Code);
        Assert.Equal("SourceUnavailable", service.Update(actor, item.Id, item.ETag, new("Read", null)).Code);
        var removeKey = Guid.NewGuid().ToString(); var removed = service.Remove(actor, item.Id, item.ETag, removeKey);
        Assert.True(removed.Succeeded); Assert.Equal(204, removed.StatusCode); Assert.Equal(removed, service.Remove(actor, item.Id, item.ETag, removeKey));
    }

    [Fact]
    public async Task Active_Trash_blocks_Bookmark_and_Reading_source_projection_even_when_status_is_Active()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmarks = Bookmarks(); var bookmark = bookmarks.Create(actor, Bookmark()).Value!; var reading = Reading();
        var save = new SaveReadingCommand("Bookmark", bookmark.Id); var key = Guid.NewGuid().ToString();
        var saved = reading.Save(actor, save, key).Value!;
        await fixture.ExecuteAsync("""
            INSERT INTO [platform].[TrashItem] ([Id],[OwnerId],[ResourceType],[ResourceId],[DeletionBatchId],[PriorStatus])
            VALUES (NEWID(),@Owner,'Bookmark',@Bookmark,NEWID(),'Active');
            """, P("@Owner", actor.OwnerId), P("@Bookmark", bookmark.Id));
        Assert.Empty(bookmarks.List(actor, includeArchived: true).Value!.Items);
        Assert.Equal("ResourceUnavailable", bookmarks.Get(actor, bookmark.Id).Code);
        Assert.Equal("ResourceUnavailable", bookmarks.Update(actor, bookmark.Id, bookmark.ETag, Bookmark("denied trash edit")).Code);
        Assert.False(Assert.Single(reading.List(actor).Value!.Items).SourceAvailable);
        var replay = reading.Save(actor, save, key); Assert.True(replay.Succeeded); Assert.Null(replay.Value); Assert.NotNull(replay.Acknowledgement);
        Assert.Equal("SourceUnavailable", reading.Save(actor, save, Guid.NewGuid().ToString()).Code);
        Assert.Equal("SourceUnavailable", reading.Update(actor, saved.Id, saved.ETag, new("Read", null)).Code);
        Assert.True(reading.Remove(actor, saved.Id, saved.ETag).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reading_before_commit_source_recheck_rolls_back_fresh_and_keyless_saves(bool keyed)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var bookmark = Bookmarks().Create(actor, Bookmark()).Value!;
        await fixture.ExecuteAsync("""
            CREATE TRIGGER [knowledge].[RevokeReadingSourceForTest] ON [knowledge].[ReadingItem] AFTER INSERT AS
            BEGIN
                UPDATE g SET [Enabled] = 0 FROM [platform].[UserModuleGrant] g
                INNER JOIN [platform].[Module] m ON m.[Id] = g.[ModuleId] AND m.[Code] = 'FX21'
                INNER JOIN inserted i ON i.[CreatedByUserId] = g.[UserId];
            END;
            """);
        try
        {
            var result = Reading().Save(actor, new("Bookmark", bookmark.Id), keyed ? Guid.NewGuid().ToString() : null);
            Assert.False(result.Succeeded); Assert.Equal("ModuleUnavailable", result.Code); Assert.Empty(Reading().List(actor).Value!.Items); Assert.True(Bookmarks().Get(actor, bookmark.Id).Succeeded);
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [knowledge].[RevokeReadingSourceForTest];"); }
    }

    [Fact]
    public async Task Read_with_position_diff_requires_both_actions_and_replay_retains_that_dependency()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        foreach (var action in new[] { "bookmarks.bookmark.create", "bookmarks.bookmark.read", "reading.item.save", "reading.queue.read", "reading.item.read" }) await Grant(session.UserId, action);
        var bookmark = Bookmarks().Create(actor, Bookmark()).Value!; var reading = Reading(); var item = reading.Save(actor, new("Bookmark", bookmark.Id)).Value!;
        var command = new UpdateReadingCommand("Read", 0.9m); var key = Guid.NewGuid().ToString();
        Assert.False(reading.Update(actor, item.Id, item.ETag, command, key).Succeeded);
        var unchanged = Assert.Single(reading.List(actor).Value!.Items); Assert.Equal("Unread", unchanged.State); Assert.Equal(0m, unchanged.Progress);
        await Grant(session.UserId, "reading.item.position");
        var changed = reading.Update(actor, item.Id, item.ETag, command, key); Assert.True(changed.Succeeded, changed.Code);
        Assert.Equal("Read", changed.Value!.State); Assert.Equal(0.9m, changed.Value.Progress);
        Assert.Equal(changed, reading.Update(actor, item.Id, item.ETag, command, key));
        await RemoveGrant(session.UserId, "reading.item.position");
        Assert.False(reading.Update(actor, item.Id, item.ETag, command, key).Succeeded);
        Assert.Equal(0.9m, Assert.Single(reading.List(actor).Value!.Items).Progress);
    }

    [Fact]
    public async Task Historical_receipts_without_original_projection_proof_replay_only_committed_ACK()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmarks = Bookmarks(); var key = Guid.NewGuid().ToString(); var created = bookmarks.Create(actor, Bookmark(), key);
        Assert.True(created.Succeeded); var bookmark = created.Value!;
        var bookmarkAudits = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId] = @User;", P("@User", actor.UserId));
        await fixture.ExecuteAsync("""
            UPDATE [identity].[RequestReceipt] SET [ResultJson] = @Legacy
            WHERE [OperationKey] = 'bookmarks.bookmark.create' AND JSON_VALUE([ResultJson], '$.Id') = @Id;
            """, new SqlParameter("@Legacy", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(bookmark) },
            new SqlParameter("@Id", SqlDbType.NVarChar, 36) { Value = bookmark.Id.ToString() });
        var replay = bookmarks.Create(actor, Bookmark(), key); Assert.True(replay.Succeeded); Assert.Null(replay.Value);
        Assert.Equal(bookmark.Id, replay.Acknowledgement!.Id); Assert.Equal(created.StatusCode, replay.StatusCode);
        Assert.Equal(bookmarkAudits, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId] = @User;", P("@User", actor.UserId)));
        var reading = Reading(); var save = new SaveReadingCommand("Bookmark", bookmark.Id); var saveKey = Guid.NewGuid().ToString();
        var saved = reading.Save(actor, save, saveKey); Assert.True(saved.Succeeded); var item = saved.Value!;
        var readingAudits = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId] = @User;", P("@User", actor.UserId));
        await fixture.ExecuteAsync("""
            UPDATE [identity].[RequestReceipt] SET [ResultJson] = JSON_MODIFY([ResultJson], '$.DetailAvailable', NULL)
            WHERE [OperationKey] = 'reading.item.save' AND JSON_VALUE([ResultJson], '$.Id') = @Id;
            """, new SqlParameter("@Id", SqlDbType.NVarChar, 36) { Value = item.Id.ToString() });
        var queueReplay = reading.Save(actor, save, saveKey); Assert.True(queueReplay.Succeeded); Assert.Null(queueReplay.Value);
        Assert.Equal(item.Id, queueReplay.Acknowledgement!.Id); Assert.Equal(saved.StatusCode, queueReplay.StatusCode);
        Assert.Equal(readingAudits, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId] = @User;", P("@User", actor.UserId)));
        Assert.Equal(1, await CountBookmarks(actor.OwnerId)); Assert.Single(reading.List(actor).Value!.Items);
    }

    [Fact]
    public async Task Concurrent_duplicate_save_and_position_update_do_not_invert_source_queue_locks()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var bookmark = Bookmarks().Create(actor, Bookmark()).Value!; var service = Reading(); var source = new SaveReadingCommand("Bookmark", bookmark.Id);
        Assert.True(service.Save(actor, source).Succeeded);
        for (var index = 0; index < 8; index++)
        {
            var item = Assert.Single(service.List(actor).Value!.Items); using var start = new ManualResetEventSlim(false);
            var save = Task.Run(() => { start.Wait(); return service.Save(actor, source); });
            var update = Task.Run(() => { start.Wait(); return service.Update(actor, item.Id, item.ETag, new("Reading", (index + 1) / 10m)); });
            start.Set(); var results = await Task.WhenAll(save, update).WaitAsync(TimeSpan.FromSeconds(45));
            Assert.All(results, result => Assert.True(result.Succeeded, result.Code)); Assert.Single(service.List(actor).Value!.Items);
        }
    }

    private static SqlParameter P(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private Task<int> CountBookmarks(Guid owner) => fixture.ScalarIntAsync("SELECT COUNT(*) FROM [knowledge].[Bookmark] WHERE [OwnerId] = @Owner;", P("@Owner", owner));
    private Task RemoveGrant(Guid user, string action) => fixture.ExecuteAsync("""
        DELETE ap FROM [platform].[AdminPermission] ap INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
        WHERE ap.[UserId] = @User AND p.[ActionKey] = @Action;
        """, P("@User", user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
    private Task Grant(Guid user, string action) => fixture.ExecuteAsync("""
        INSERT INTO [platform].[AdminPermission] ([UserId], [PermissionId], [Effect])
        SELECT @User, [Id], 'Allow' FROM [platform].[Permission] WHERE [ActionKey] = @Action;
        """, P("@User", user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
}
