using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlWorkLogCoverageTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/learning/work-logs";

    [Fact]
    public async Task Manual_create_and_update_replay_stay_opaque_and_append_each_snapshot_once()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var key = Guid.NewGuid();
        var body = Body("Original private work", 60);
        using var created = await Send(session, HttpMethod.Post, Root, body, key);
        var id = await Ack(created, HttpStatusCode.Created);
        using (var replay = await Send(session, HttpMethod.Post, Root, body, key))
            Assert.Equal(id, await Ack(replay, HttpStatusCode.Created));
        using (var collision = await Send(session, HttpMethod.Post, Root, Body("Changed create", 60), key))
            await Problem(collision, HttpStatusCode.Conflict, "IdempotencyConflict");
        var original = await Get(session, id);
        Assert.Equal("2026-10-18", original.GetProperty("recordedOn").GetString());
        Assert.Equal("Active", original.GetProperty("status").GetString());
        var updateKey = Guid.NewGuid();
        var update = Body("Updated private work", 90);
        using (var saved = await Send(session, HttpMethod.Put, $"{Root}/{id}", update, updateKey, Etag(original)))
            Assert.Equal(id, await Ack(saved, HttpStatusCode.OK));
        var current = await Get(session, id);
        Assert.NotEqual(Etag(original), Etag(current));
        using (var replay = await Send(session, HttpMethod.Put, $"{Root}/{id}", update, updateKey, Etag(original)))
            Assert.Equal(id, await Ack(replay, HttpStatusCode.OK));
        var versions = await History(session, id);
        Assert.Equal(2, versions.GetArrayLength());
        Assert.Equal(new[] { "Updated", "Created" }, versions.EnumerateArray().Select(x => x.GetProperty("event").GetString()));
        Assert.Equal(new long[] { 2, 1 }, versions.EnumerateArray().Select(x => x.GetProperty("sequence").GetInt64()));
        Assert.Equal("Updated private work", versions[0].GetProperty("title").GetString());
        Assert.Equal("Original private work", versions[1].GetProperty("title").GetString());
        Assert.False(versions[0].TryGetProperty("etag", out _));
        Assert.Equal(2, await AuditCount(session, id));
    }

    [Fact]
    public async Task Manual_minutes_accept_exact_bounds_and_reject_outside_without_creating_entries()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        foreach (var minutes in new[] { 0, 1441 })
        {
            using var invalid = await Send(session, HttpMethod.Post, Root, Body("Invalid duration", minutes));
            await Problem(invalid, HttpStatusCode.UnprocessableEntity, "ValidationFailed");
        }
        foreach (var minutes in new[] { 1, 1440 })
        {
            var id = await Create(session, Body("Boundary duration", minutes));
            Assert.Equal(minutes, (await Get(session, id)).GetProperty("minutes").GetInt32());
        }
        using var listed = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        Assert.Equal(2, (await Json(listed)).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Archive_requires_confirmation_preserves_readonly_metadata_and_unarchive_restores_Active()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var id = await Create(session, Body("Archive lifecycle", 45));
        var original = await Get(session, id);
        using (var declined = await Send(session, HttpMethod.Post, $"{Root}/{id}/archive", new { confirm = false }, etag: Etag(original)))
            await Problem(declined, HttpStatusCode.UnprocessableEntity, "ValidationFailed");
        var key = Guid.NewGuid();
        using (var archived = await Send(session, HttpMethod.Post, $"{Root}/{id}/archive", new { confirm = true }, key, Etag(original)))
            await Ack(archived, HttpStatusCode.OK);
        var current = await Get(session, id);
        Assert.Equal("Archived", current.GetProperty("status").GetString());
        using (var edit = await Send(session, HttpMethod.Put, $"{Root}/{id}", Body("Forbidden archived edit", 10), etag: Etag(current)))
            await Problem(edit, HttpStatusCode.Conflict, "LifecycleLocked");
        using (var replay = await Send(session, HttpMethod.Post, $"{Root}/{id}/archive", new { confirm = true }, key, Etag(original)))
            await Ack(replay, HttpStatusCode.OK);
        Assert.Equal(current.GetRawText(), (await Get(session, id)).GetRawText());
        using (var unarchived = await Send(session, HttpMethod.Post, $"{Root}/{id}/unarchive", new { confirm = true }, etag: Etag(current)))
            await Ack(unarchived, HttpStatusCode.OK);
        Assert.Equal("Active", (await Get(session, id)).GetProperty("status").GetString());
        Assert.Equal(new[] { "Unarchived", "Archived", "Created" },
            (await History(session, id)).EnumerateArray().Select(x => x.GetProperty("event").GetString()));
    }

    [Theory]
    [InlineData(false, "Active")]
    [InlineData(true, "Archived")]
    public async Task Trash_and_restore_preserve_actual_prior_state_and_exact_history(bool archiveFirst, string expected)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var id = await Create(session, Body("Exact prior state", 75));
        if (archiveFirst)
        {
            var active = await Get(session, id);
            using var archived = await Send(session, HttpMethod.Post, $"{Root}/{id}/archive", new { confirm = true }, etag: Etag(active));
            await Ack(archived, HttpStatusCode.OK);
        }
        var prior = await Get(session, id);
        var preview = await Preview(session, id, "trash", Etag(prior));
        Assert.Equal(expected, preview.GetProperty("fromStatus").GetString());
        Assert.Equal("Trash", preview.GetProperty("toStatus").GetString());
        Assert.Equal(prior.GetRawText(), (await Get(session, id)).GetRawText());
        var key = Guid.NewGuid();
        var body = Confirmation(preview);
        using (var trashed = await Send(session, HttpMethod.Post, $"{Root}/{id}/trash", body, key, Etag(prior)))
            await Ack(trashed, HttpStatusCode.OK);
        var trash = await Get(session, id);
        Assert.Equal("Trash", trash.GetProperty("status").GetString());
        Assert.Equal(expected, trash.GetProperty("preTrashState").GetString());
        using (var replay = await Send(session, HttpMethod.Post, $"{Root}/{id}/trash", body, key, Etag(prior)))
            await Ack(replay, HttpStatusCode.OK);
        var restore = await Preview(session, id, "restore", Etag(trash));
        Assert.NotEqual(Guid.Empty, restore.GetProperty("batchId").GetGuid());
        using (var restored = await Send(session, HttpMethod.Post, $"{Root}/{id}/restore", Confirmation(restore), etag: Etag(trash)))
            await Ack(restored, HttpStatusCode.OK);
        var current = await Get(session, id);
        Assert.Equal(expected, current.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, current.GetProperty("preTrashState").ValueKind);
        Assert.Equal(archiveFirst ? 4 : 3, (await History(session, id)).GetArrayLength());
        if (archiveFirst)
        {
            using var edit = await Send(session, HttpMethod.Put, $"{Root}/{id}", Body("Still readonly", 10), etag: Etag(current));
            await Problem(edit, HttpStatusCode.Conflict, "LifecycleLocked");
        }
    }

    [Fact]
    public async Task Purge_disposes_private_history_and_retains_only_completed_opaque_replay_and_registry_tombstone()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var id = await Create(session, Body("Disposable purge source", 20));
        var active = await Get(session, id);
        var trashPreview = await Preview(session, id, "trash", Etag(active));
        using (var trashed = await Send(session, HttpMethod.Post, $"{Root}/{id}/trash", Confirmation(trashPreview), etag: Etag(active)))
            await Ack(trashed, HttpStatusCode.OK);
        var trash = await Get(session, id);
        var preview = await Preview(session, id, "purge", Etag(trash));
        Assert.Equal(2, preview.GetProperty("historyCount").GetInt64());
        Assert.Equal(0, preview.GetProperty("referenceCount").GetInt64());
        var body = Confirmation(preview);
        var key = Guid.NewGuid();
        using (var purged = await Send(session, HttpMethod.Post, $"{Root}/{id}/purge", body, key, Etag(trash)))
            await Ack(purged, HttpStatusCode.OK);
        using (var missing = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", session.RawSessionHandle))
            await Problem(missing, HttpStatusCode.NotFound, "ResourceUnavailable");
        using (var history = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", session.RawSessionHandle))
            await Problem(history, HttpStatusCode.NotFound, "ResourceUnavailable");
        Assert.Equal(0, await Count(session, id, "learning.WorkLog"));
        Assert.Equal(0, await Count(session, id, "learning.WorkLogVersion", "WorkLogId"));
        Assert.Equal("Purged", await fixture.ScalarStringAsync("SELECT Availability FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id;", Id("@Owner", session.OwnerId), Id("@Id", id)));
        var audits = await AuditCount(session, id);
        using (var replay = await Send(session, HttpMethod.Post, $"{Root}/{id}/purge", body, key, Etag(trash)))
            Assert.Equal(id, await Ack(replay, HttpStatusCode.OK));
        Assert.Equal(audits, await AuditCount(session, id));
    }

    [Fact]
    public async Task Foreign_owner_and_stale_revision_commands_leave_current_source_and_history_unchanged()
    {
        fixture.RequireAvailable();
        var owner = await fixture.CreateActiveSessionAsync();
        var foreign = await fixture.CreateActiveSessionAsync();
        var id = await Create(owner, Body("Own private work", 30));
        var original = await Get(owner, id);
        foreach (var suffix in new[] { "", "/history" })
        {
            using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}{suffix}", foreign.RawSessionHandle);
            await Problem(denied, HttpStatusCode.NotFound, "ResourceUnavailable");
        }
        using (var foreignEdit = await Send(foreign, HttpMethod.Put, $"{Root}/{id}", Body("Foreign edit", 10), etag: Etag(original)))
            await Problem(foreignEdit, HttpStatusCode.NotFound, "ResourceUnavailable");
        using (var saved = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body("Current own work", 40), etag: Etag(original)))
            await Ack(saved, HttpStatusCode.OK);
        var current = await Get(owner, id);
        using (var stale = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body("Stale edit", 10), etag: Etag(original)))
            await Problem(stale, HttpStatusCode.PreconditionFailed, "RevisionConflict");
        using (var stalePreview = await Send(owner, HttpMethod.Post, $"{Root}/{id}/trash-preview", new { }, etag: Etag(original)))
            await Problem(stalePreview, HttpStatusCode.PreconditionFailed, "RevisionConflict");
        using (var missingRevision = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body("Missing revision", 10)))
            await Problem(missingRevision, (HttpStatusCode)428, "PreconditionRequired");
        Assert.Equal(current.GetRawText(), (await Get(owner, id)).GetRawText());
        Assert.Equal(2, (await History(owner, id)).GetArrayLength());
        Assert.Equal(2, await AuditCount(owner, id));
    }

    [Fact]
    public async Task Current_Admin_grants_gate_update_replay_and_preview_commit_and_private_history()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin");
        foreach (var action in new[] { "create", "read" }) await Permission(session, "learning.worklog." + action, "Allow");
        var id = await Create(session, Body("Admin source", 30));
        var original = await Get(session, id);
        var key = Guid.NewGuid();
        var body = Body("Admin updated source", 35);
        using (var unset = await Send(session, HttpMethod.Put, $"{Root}/{id}", body, key, Etag(original)))
            await Problem(unset, HttpStatusCode.Forbidden, "ModuleUnavailable");
        await Permission(session, "learning.worklog.update", "Allow");
        using (var updated = await Send(session, HttpMethod.Put, $"{Root}/{id}", body, key, Etag(original)))
            await Ack(updated, HttpStatusCode.OK);
        var current = await Get(session, id);
        await Permission(session, "learning.worklog.update", "Deny");
        using (var revokedReplay = await Send(session, HttpMethod.Put, $"{Root}/{id}", body, key, Etag(original)))
            await Problem(revokedReplay, HttpStatusCode.Forbidden, "ModuleUnavailable");
        await Permission(session, "learning.worklog.trash", "Allow");
        var preview = await Preview(session, id, "trash", Etag(current));
        await Permission(session, "learning.worklog.trash", null);
        using (var revokedCommit = await Send(session, HttpMethod.Post, $"{Root}/{id}/trash", Confirmation(preview), etag: Etag(current)))
            await Problem(revokedCommit, HttpStatusCode.Forbidden, "ModuleUnavailable");
        Assert.Equal(current.GetRawText(), (await Get(session, id)).GetRawText());
        Assert.Equal(2, (await History(session, id)).GetArrayLength());
        await Permission(session, "learning.worklog.read", "Deny");
        using var history = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", session.RawSessionHandle);
        await Problem(history, HttpStatusCode.Forbidden, "ModuleUnavailable");
    }

    [Fact]
    public async Task Category_literal_title_and_inclusive_date_filters_match_gross_report_and_exclude_Trash()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var first = await Create(session, Body("Literal % work", 60, "Focus"));
        await Create(session, Body("Literal % work second", 90, "Focus"));
        await Create(session, Body("Literal other", 200, "Focus"));
        await Create(session, Body("Literal % other category", 300, "Focus extended"));
        var query = "?from=2026-10-18&to=2026-10-18&category=Focus&query=%25";
        using (var list = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + query, session.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal(2, (await Json(list)).GetProperty("items").GetArrayLength());
        }
        using (var report = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/report" + query, session.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, report.StatusCode);
            var result = await Json(report);
            Assert.Equal(2, result.GetProperty("entries").GetInt64());
            Assert.Equal(150, result.GetProperty("grossMinutes").GetInt64());
            Assert.Equal("NotDeduplicated", result.GetProperty("overlap").GetString());
        }
        var active = await Get(session, first);
        var preview = await Preview(session, first, "trash", Etag(active));
        using (var trash = await Send(session, HttpMethod.Post, $"{Root}/{first}/trash", Confirmation(preview), etag: Etag(active)))
            await Ack(trash, HttpStatusCode.OK);
        using var after = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/report" + query, session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.Equal(90, (await Json(after)).GetProperty("grossMinutes").GetInt64());
    }

    private static object Body(string title, int minutes, string category = "Synthetic category") => new
    { title, recordedOn = "2026-10-18", minutes, notes = "Synthetic private notes", category, employer = "Synthetic employer" };
    private async Task<Guid> Create(SyntheticSession session, object body)
    {
        using var created = await Send(session, HttpMethod.Post, Root, body);
        return await Ack(created, HttpStatusCode.Created);
    }
    private async Task<JsonElement> Get(SyntheticSession session, Guid id)
    {
        using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Json(response);
    }
    private async Task<JsonElement> History(SyntheticSession session, Guid id)
    {
        using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await Json(response)).GetProperty("items");
    }
    private async Task<JsonElement> Preview(SyntheticSession session, Guid id, string operation, string etag)
    {
        using var response = await Send(session, HttpMethod.Post, $"{Root}/{id}/{operation}-preview", new { }, etag: etag);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await Json(response);
    }
    private static object Confirmation(JsonElement preview) => new { confirm = true, previewToken = preview.GetProperty("previewToken").GetString()! };
    private async Task<HttpResponseMessage> Send(SyntheticSession session, HttpMethod method, string path, object body, Guid? key = null, string? etag = null) =>
        await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), session.RawSessionHandle, etag);
    private static string Etag(JsonElement row) => row.GetProperty("etag").GetString()!;
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.Clone();
    }
    private static async Task<Guid> Ack(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        var ack = await Json(response);
        Assert.True(ack.GetProperty("committed").GetBoolean());
        Assert.False(ack.GetProperty("detailAvailable").GetBoolean());
        Assert.Equal(3, ack.EnumerateObject().Count());
        Assert.Null(response.Headers.ETag);
        return ack.GetProperty("id").GetGuid();
    }
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await Json(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("notes", out _));
        Assert.False(problem.TryGetProperty("previewToken", out _));
    }
    // Disposable fixture policy only; all WorkLog domain writes use the application boundary.
    private async Task Permission(SyntheticSession session, string action, string? effect)
    {
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Permission] WHERE ActionKey=@Action;", Text("@Action", action)));
        await fixture.ExecuteAsync("DELETE ap FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey=@Action;", Id("@User", session.UserId), Text("@Action", action));
        if (effect is not null)
            await fixture.ExecuteAsync("INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,@Effect FROM [platform].[Permission] WHERE ActionKey=@Action;", Id("@User", session.UserId), Text("@Action", action), Text("@Effect", effect));
    }
    private Task<int> Count(SyntheticSession session, Guid id, string table, string idColumn = "Id")
    {
        var parts = table.Split('.');
        return fixture.ScalarIntAsync($"SELECT COUNT(*) FROM [{parts[0]}].[{parts[1]}] WHERE OwnerId=@Owner AND [{idColumn}]=@Id;", Id("@Owner", session.OwnerId), Id("@Id", id));
    }
    private Task<int> AuditCount(SyntheticSession session, Guid id) => fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND TargetId=@Id AND Result='Succeeded';", Id("@User", session.UserId), Id("@Id", id));
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static SqlParameter Text(string name, string value) => new(name, SqlDbType.NVarChar, 160) { Value = value };
}
