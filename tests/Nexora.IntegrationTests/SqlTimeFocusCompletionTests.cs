using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlTimeFocusCompletionTests(SqlApiFixture fixture)
{
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        Assert.Equal(status, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
    private async Task<HttpResponseMessage> Post(SyntheticSession user, string path, object body, string? etag = null, Guid? key = null) =>
        await fixture.SendJsonAsync(HttpMethod.Post, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), user.RawSessionHandle, etag);

    [Fact]
    public async Task Time_purge_revalidates_owner_revision_confirmation_and_rolls_back_failed_audit()
    {
        fixture.RequireAvailable();
        var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var start = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero);
        using var createdResponse = await Post(owner, "/api/v1/time/entries", new { startAt = start, endAt = start.AddHours(2), description = "purge-" + Guid.NewGuid(), category = "Synthetic", confirmOverlap = false });
        var created = await Json(createdResponse, HttpStatusCode.Created); var id = created.GetProperty("id").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        using (var active = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, etag)) Assert.Equal(HttpStatusCode.Conflict, active.StatusCode);
        using var trashedResponse = await Post(owner, $"/api/v1/time/entries/{id}/trash", new { }, etag);
        var trashed = await Json(trashedResponse); etag = trashed.GetProperty("etag").GetString()!;
        using (var wrongOwner = await Post(foreign, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, etag)) Assert.Equal(HttpStatusCode.NotFound, wrongOwner.StatusCode);
        using (var missingVersion = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true })) Assert.Equal((HttpStatusCode)428, missingVersion.StatusCode);
        using (var stale = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, "\"stale\"")) Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using (var unconfirmed = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = false }, etag)) Assert.Equal(HttpStatusCode.UnprocessableEntity, unconfirmed.StatusCode);
        using var previewResponse = await Post(owner, $"/api/v1/time/entries/{id}/preview-purge", new { });
        var preview = await Json(previewResponse); Assert.False(preview.GetProperty("hasConversionPin").GetBoolean()); Assert.True(preview.GetProperty("correctionCount").GetInt32() > 0);
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[RejectTimePurge] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='time.entry.purge') THROW 51003,'Synthetic audit failure',1; END;");
        var key = Guid.NewGuid();
        try
        {
            using var failed = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, etag, key);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
            Assert.True(await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)) > 0);
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectTimePurge]"); }
        using var purged = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, etag, key);
        Assert.Equal(id, (await Json(purged)).GetProperty("entryId").GetGuid());
        using var replay = await Post(owner, $"/api/v1/time/entries/{id}/purge", new { confirmPermanentDeletion = true }, etag, key);
        Assert.Equal(id, (await Json(replay)).GetProperty("entryId").GetGuid());
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Correction] WHERE OwnerId=@Owner AND EntryId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='time.entry.purge'", new SqlParameter("@Id", id.ToString())));
    }

    [Fact]
    public async Task Completed_focus_conversion_uses_real_clock_is_atomic_and_deduplicates_across_keys()
    {
        fixture.RequireAvailable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        using var preference = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/focus/preferences", owner.RawSessionHandle);
        var preferences = await Json(preference);
        using var save = await fixture.SendJsonAsync(HttpMethod.Put, "/api/v1/focus/preferences", new { focusMinutes = 1, shortBreakMinutes = 1, longBreakMinutes = 1, cycleLength = 4 }, await fixture.GetCsrfAsync(), Guid.NewGuid(), owner.RawSessionHandle, preferences.GetProperty("etag").GetString());
        await Json(save);
        using var start = await Post(owner, "/api/v1/focus/sessions", new { phase = "Focus" });
        var session = await Json(start, HttpStatusCode.Created); var id = session.GetProperty("id").GetGuid();
        using (var premature = await Post(owner, $"/api/v1/focus/sessions/{id}/record-time", new { confirmOverlap = false }, session.GetProperty("etag").GetString())) Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(85);
        do
        {
            using var list = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/focus/sessions", owner.RawSessionHandle);
            session = (await Json(list)).GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id).Clone();
            if (session.GetProperty("state").GetString() == "Completed") break;
            await Task.Delay(1000);
        } while (DateTimeOffset.UtcNow < deadline);
        Assert.Equal("Completed", session.GetProperty("state").GetString());
        var etag = session.GetProperty("etag").GetString()!;
        using (var wrongOwner = await Post(foreign, $"/api/v1/focus/sessions/{id}/record-time", new { confirmOverlap = false }, etag)) Assert.Equal(HttpStatusCode.NotFound, wrongOwner.StatusCode);
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[RejectFocusConversion] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='focus.session.record_time') THROW 51004,'Synthetic audit failure',1; END;");
        var key = Guid.NewGuid();
        try
        {
            using var failed = await Post(owner, $"/api/v1/focus/sessions/{id}/record-time", new { confirmOverlap = false }, etag, key);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId)));
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusConversion] WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId)));
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectFocusConversion]"); }
        var results = await Task.WhenAll(Post(owner, $"/api/v1/focus/sessions/{id}/record-time", new { confirmOverlap = false }, etag, key), Post(owner, $"/api/v1/focus/sessions/{id}/record-time", new { confirmOverlap = false }, etag));
        var entries = new List<Guid>();
        foreach (var result in results) { using (result) { Assert.Contains(result.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Created }); using var d = JsonDocument.Parse(await result.Content.ReadAsStringAsync()); entries.Add(d.RootElement.GetProperty("entryId").GetGuid()); Assert.Equal(2, d.RootElement.EnumerateObject().Count()); } }
        Assert.Equal(entries[0], entries[1]);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusConversion] WHERE OwnerId=@Owner AND SessionId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(60000, await fixture.ScalarIntAsync("SELECT CAST(DATEDIFF_BIG(millisecond,StartAt,EndAt) AS int) FROM [time].[Entry] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", entries[0])));
        using var trashResponse = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/time/entries/{entries[0]}", owner.RawSessionHandle);
        var entry = await Json(trashResponse);
        using var trashedResponse = await Post(owner, $"/api/v1/time/entries/{entries[0]}/trash", new { }, entry.GetProperty("etag").GetString());
        var trashed = await Json(trashedResponse);
        using var pinned = await Post(owner, $"/api/v1/time/entries/{entries[0]}/purge", new { confirmPermanentDeletion = true }, trashed.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.Conflict, pinned.StatusCode);
    }

    [Fact]
    public async Task New_actions_fail_closed_for_anonymous_and_admin_unset()
    {
        fixture.RequireAvailable(); var admin = await fixture.CreateActiveSessionAsync("Admin");
        var routes = new[] { $"/api/v1/time/entries/{Guid.NewGuid()}/purge", $"/api/v1/focus/sessions/{Guid.NewGuid()}/record-time" };
        foreach (var route in routes)
        {
            using var anonymous = await fixture.SendJsonAsync(HttpMethod.Post, route, new { }, await fixture.GetCsrfAsync(), Guid.NewGuid()); Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using var unset = await Post(admin, route, new { }); Assert.Equal(HttpStatusCode.Forbidden, unset.StatusCode);
        }
    }
}
