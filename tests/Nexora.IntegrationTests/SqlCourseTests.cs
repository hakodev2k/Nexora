using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlCourseTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/learning/courses";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static object Body(string title = "Synthetic skill", string level = "ManualPercent") =>
        new { title, progressMode = level, provider = "Owner-entered provider", startedOn = "2026-09-30", notes = "Private tracking" };
    private static object Metadata(string title = "Synthetic skill") => new { title, progressMode = "ManualPercent", notes = "Edited tracking", startedOn = "2026-09-30" };
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, received {response.StatusCode}: {content}");
        using var document = JsonDocument.Parse(content); return document.RootElement.Clone();
    }
    private async Task<JsonElement> Get(SyntheticSession user, string path)
    { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, user.RawSessionHandle); return await Json(response); }
    private Task<HttpResponseMessage> Send(SyntheticSession user, HttpMethod method, string path, object body, string? etag = null, Guid? key = null) =>
        SendCore(user, method, path, body, etag, key);
    private async Task<HttpResponseMessage> SendCore(SyntheticSession user, HttpMethod method, string path, object body, string? etag, Guid? key) =>
        await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), user.RawSessionHandle, etag);
    private async Task<JsonElement> Create(SyntheticSession user, string title = "Synthetic purchase")
    { using var response = await Send(user, HttpMethod.Post, Root, Body(title)); return await Json(response, HttpStatusCode.Created); }
    private async Task<JsonElement> Change(SyntheticSession user, Guid id, string operation, string etag)
    { using var response = await Send(user, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); return await Json(response); }

    // Installation must preserve administrator policy. Tests enable the installed subset through real preview/commit APIs.
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX40").Clone();
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        Assert.Contains("Skills and Courses only", module.GetProperty("name").GetString());
        var scope = new PolicyScope(this, super, module.GetProperty("id").GetGuid(), module.GetProperty("systemEnabled").GetBoolean(), module.GetProperty("registrationEnabled").GetBoolean());
        await Policy(scope, true, true); return scope;
    }
    private async Task Policy(PolicyScope scope, bool system, bool registration)
    {
        var current = (await Get(scope.Super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == scope.Module);
        if (current.GetProperty("systemEnabled").GetBoolean() == system && current.GetProperty("registrationEnabled").GetBoolean() == registration) return;
        using var previewResponse = await Send(scope.Super, HttpMethod.Post, $"/api/v1/admin/modules/{scope.Module}/preview", new { systemEnabled = system, registrationEnabled = registration });
        var preview = await Json(previewResponse); Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        using var commit = await Send(scope.Super, HttpMethod.Put, $"/api/v1/admin/modules/{scope.Module}/policy", new { systemEnabled = system, registrationEnabled = registration, previewToken = preview.GetProperty("previewToken").GetString() }, preview.GetProperty("etag").GetString());
        await Json(commit);
    }
    private sealed record PolicyScope(SqlCourseTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
    { public ValueTask DisposeAsync() => new(Tests.Policy(this, System, Registration)); }
    private async Task Grant(PolicyScope scope, SyntheticSession target, string effect, params string[] actions)
    {
        using var current = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/admin/users/{target.UserId}/access", scope.Super.RawSessionHandle);
        await Json(current); var changes = actions.Select(action => new { actionKey = action, effect }).ToArray();
        using var previewResponse = await Send(scope.Super, HttpMethod.Post, $"/api/v1/admin/users/{target.UserId}/access/preview", new { kind = "permissions", changes });
        var preview = await Json(previewResponse);
        using var commit = await Send(scope.Super, HttpMethod.Put, $"/api/v1/admin/users/{target.UserId}/access/permissions", new { kind = "permissions", changes, previewToken = preview.GetProperty("previewToken").GetString() }, current.Headers.ETag?.Tag);
        await Json(commit);
    }



    private async Task<(JsonElement Item, string Tag)> Item(SyntheticSession owner, Guid id)
    { var current = await Get(owner, $"{Root}/{id}"); return (current, current.GetProperty("etag").GetString()!); }
    private async Task<(Guid Child, string Tag)> AddChild(SyntheticSession owner, Guid id, string title)
    {
        using var response = await Send(owner, HttpMethod.Post, $"{Root}/{id}/milestones", new { title }, (await Item(owner, id)).Tag);
        var ack = await Json(response); return (ack.GetProperty("milestoneId").GetGuid(), ack.GetProperty("milestoneETag").GetString()!);
    }
    private async Task<HttpResponseMessage> ChildCommand(SyntheticSession owner, Guid id, Guid child, string operation, object body,
        string? parentTag = null, string? childTag = null, Guid? key = null)
    {
        var parent = await Item(owner, id);
        var children = (await Get(owner, $"{Root}/{id}/milestones")).GetProperty("items").EnumerateArray();
        childTag ??= children.Single(c => c.GetProperty("id").GetGuid() == child).GetProperty("etag").GetString()!;
        var method = operation == "delete" ? HttpMethod.Delete : operation == "update" ? HttpMethod.Put : HttpMethod.Post;
        var path = $"{Root}/{id}/milestones/{child}" + (operation is "delete" or "update" ? "" : "/" + operation);
        using var request = new HttpRequestMessage(method, path);
        var csrf = await fixture.GetCsrfAsync(); request.Content = System.Net.Http.Json.JsonContent.Create(body);
        request.Headers.TryAddWithoutValidation("Cookie", csrf.CookieHeader + "; __Host-NexoraSession=" + owner.RawSessionHandle);
        request.Headers.TryAddWithoutValidation("X-CSRF-Token", csrf.RequestToken);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", (key ?? Guid.NewGuid()).ToString());
        request.Headers.TryAddWithoutValidation("If-Match", parentTag ?? parent.Tag);
        request.Headers.TryAddWithoutValidation("Milestone-If-Match", childTag);
        return await fixture.Client.SendAsync(request);
    }

    [Fact]
    public async Task Manual_progress_is_exact_explicit_and_preserves_completion_history_through_lifecycle()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        using (var unauth = await fixture.Client.GetAsync(Root)) Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        var id = (await Create(owner)).GetProperty("itemId").GetGuid(); var original = await Item(owner, id);
        Assert.Equal("Planned", original.Item.GetProperty("status").GetString()); Assert.False(original.Item.GetProperty("progressModeLocked").GetBoolean());
        using (var bad = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "0.123456789" }, original.Tag)) Assert.Equal((HttpStatusCode)422, bad.StatusCode);
        using (var bad = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "1.00000001" }, original.Tag)) Assert.Equal((HttpStatusCode)422, bad.StatusCode);
        using (var missing = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "0.12345678" })) Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using (var foreignWrite = await Send(foreign, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "1" }, original.Tag)) Assert.Equal(HttpStatusCode.NotFound, foreignWrite.StatusCode);
        using (var bypass = await Send(owner, HttpMethod.Put, $"{Root}/{id}", new { title = "Hidden write", progressMode = "ManualPercent", manualProgress = "1" }, original.Tag)) Assert.Equal(HttpStatusCode.BadRequest, bypass.StatusCode);
        using (var progress = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "0.12345678" }, original.Tag)) await Json(progress);
        var current = await Item(owner, id); Assert.Equal("0.12345678", current.Item.GetProperty("manualProgress").GetString()); Assert.Equal("Planned", current.Item.GetProperty("status").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id AND ManualProgress=0.12345678 AND FirstProgressAt IS NOT NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        using (var mode = await Send(owner, HttpMethod.Put, $"{Root}/{id}", new { title = "Change mode", progressMode = "Milestones" }, current.Tag)) Assert.Equal(HttpStatusCode.Conflict, mode.StatusCode);
        using (var start = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "1", start = true, startedOn = "2026-10-01" }, current.Tag)) await Json(start);
        current = await Item(owner, id); Assert.Equal("InProgress", current.Item.GetProperty("status").GetString());
        using (var wrongDate = await Send(owner, HttpMethod.Post, $"{Root}/{id}/complete", new { confirm = true, completedOn = "2026-09-30" }, current.Tag)) Assert.Equal((HttpStatusCode)422, wrongDate.StatusCode);
        using (var complete = await Send(owner, HttpMethod.Post, $"{Root}/{id}/complete", new { confirm = true, completedOn = "2026-10-02" }, current.Tag)) await Json(complete);
        current = await Item(owner, id); Assert.Equal("Completed", current.Item.GetProperty("status").GetString());
        using (var correction = await Send(owner, HttpMethod.Post, $"{Root}/{id}/progress", new { manualProgress = "0" }, current.Tag)) await Json(correction);
        current = await Item(owner, id); Assert.Equal("Completed", current.Item.GetProperty("status").GetString()); Assert.True(current.Item.GetProperty("progressModeLocked").GetBoolean());
        var tag = (await Change(owner, id, "archive", current.Tag)).GetProperty("etag").GetString()!;
        var nullDate = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [learning].[Course] SET CompletedOn=NULL WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id))); Assert.Equal(547, nullDate.Number);
        tag = (await Change(owner, id, "trash", tag)).GetProperty("etag").GetString()!;
        nullDate = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [learning].[Course] SET CompletedOn=NULL WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id))); Assert.Equal(547, nullDate.Number);
        tag = (await Change(owner, id, "restore", tag)).GetProperty("etag").GetString()!; Assert.Equal("Archived", (await Item(owner, id)).Item.GetProperty("status").GetString());
        tag = (await Change(owner, id, "unarchive", tag)).GetProperty("etag").GetString()!; Assert.Equal("Completed", (await Item(owner, id)).Item.GetProperty("status").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseCompletionHistory] WHERE OwnerId=@Owner AND CourseId=@Id AND CompletedOn='2026-10-02'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        tag = (await Change(owner, id, "trash", tag)).GetProperty("etag").GetString()!; await Change(owner, id, "purge", tag);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseCompletionHistory] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        var abandonedId = (await Create(owner, "Abandoned course")).GetProperty("itemId").GetGuid();
        await Change(owner, abandonedId, "abandon", (await Item(owner, abandonedId)).Tag);
        var abandoned = await Item(owner, abandonedId); Assert.Equal("Abandoned", abandoned.Item.GetProperty("status").GetString());
        using (var correction = await Send(owner, HttpMethod.Post, $"{Root}/{abandonedId}/progress", new { manualProgress = "1" }, abandoned.Tag)) await Json(correction);
        abandoned = await Item(owner, abandonedId); Assert.Equal("Abandoned", abandoned.Item.GetProperty("status").GetString());
        using (var locked = await Send(owner, HttpMethod.Post, $"{Root}/{abandonedId}/complete", new { confirm = true, completedOn = "2026-10-02" }, abandoned.Tag)) Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
    }

    [Fact]
    public async Task Milestone_counts_reorder_versions_and_frozen_owned_cohort_are_enforced()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        using var create = await Send(owner, HttpMethod.Post, Root, Body("Milestone course", "Milestones")); var id = (await Json(create, HttpStatusCode.Created)).GetProperty("itemId").GetGuid();
        var empty = await Item(owner, id); Assert.Equal(0, empty.Item.GetProperty("milestonesTotal").GetInt32()); Assert.Equal(JsonValueKind.Null, empty.Item.GetProperty("manualProgress").ValueKind);
        var first = await AddChild(owner, id, "First unit"); var second = await AddChild(owner, id, "Second unit");
        using (var stale = await ChildCommand(owner, id, first.Child, "completion", new { completed = true }, empty.Tag, first.Tag)) Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using (var done = await ChildCommand(owner, id, first.Child, "completion", new { completed = true })) await Json(done);
        var current = await Item(owner, id); Assert.Equal(1, current.Item.GetProperty("milestonesDone").GetInt32()); Assert.Equal(2, current.Item.GetProperty("milestonesTotal").GetInt32()); Assert.Equal("Planned", current.Item.GetProperty("status").GetString());
        using (var childStale = await ChildCommand(owner, id, first.Child, "update", new { title = "Stale child draft" }, current.Tag, first.Tag)) Assert.Equal(HttpStatusCode.PreconditionFailed, childStale.StatusCode);
        using (var move = await ChildCommand(owner, id, second.Child, "move", new { direction = "up" })) await Json(move);
        var children = (await Get(owner, $"{Root}/{id}/milestones")).GetProperty("items"); Assert.Equal(second.Child, children[0].GetProperty("id").GetGuid());
        using (var undo = await ChildCommand(owner, id, first.Child, "completion", new { completed = false })) await Json(undo);
        using (var remove = await ChildCommand(owner, id, first.Child, "delete", new { })) await Json(remove);
        current = await Item(owner, id); Assert.True(current.Item.GetProperty("progressModeLocked").GetBoolean());
        using (var mode = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata(), current.Tag)) Assert.Equal(HttpStatusCode.Conflict, mode.StatusCode);
        var tag = (await Change(owner, id, "archive", current.Tag)).GetProperty("etag").GetString()!;
        using (var blocked = await Send(owner, HttpMethod.Post, $"{Root}/{id}/milestones", new { title = "Hidden" }, tag)) Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        tag = (await Change(owner, id, "trash", tag)).GetProperty("etag").GetString()!;
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=(SELECT TrashBatchId FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id)", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        // Synthetic direct corruption exercises the frozen O-profile child version boundary.
        await fixture.ExecuteAsync("UPDATE [learning].[CourseMilestone] SET Title=N'Unexpected child mutation' WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id));
        using (var corrupt = await Send(owner, HttpMethod.Post, $"{Root}/{id}/restore", new { confirm = true }, tag)) Assert.Equal(HttpStatusCode.Conflict, corrupt.StatusCode);
        using (var corrupt = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, tag)) Assert.Equal(HttpStatusCode.Conflict, corrupt.StatusCode);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
    }
    [Fact]
    public async Task Actual_course_and_milestone_pages_are_owner_scoped_and_use_selected_boundaries()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var ids = new HashSet<Guid>(); for (var n = 0; n < 31; n++) ids.Add((await Create(owner, $"page-course-{n:00}")).GetProperty("itemId").GetGuid());
        await fixture.ExecuteAsync("UPDATE [learning].[Course] SET UpdatedAt='2026-10-01T00:00:00' WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId));
        var foreign = (await Create(other)).GetProperty("itemId").GetGuid();
        var first = await Get(owner, Root); Assert.Equal(25, first.GetProperty("items").GetArrayLength());
        var second = await Get(owner, $"{Root}?cursor={first.GetProperty("nextCursor").GetGuid()}"); Assert.Equal(6, second.GetProperty("items").GetArrayLength());
        var pageIds = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(c => c.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(31, pageIds.Distinct().Count()); Assert.True(ids.SetEquals(pageIds));
        foreach (var path in new[] { $"{Root}?cursor={foreign}", $"{Root}?cursor={pageIds[0]}&status=Archived", $"{Root}?cursor={pageIds[0]}&query=no-match", $"{Root}?query=%25" }) Assert.Empty((await Get(owner, path)).GetProperty("items").EnumerateArray());
        var id = pageIds[0]; var childIds = new HashSet<Guid>(); for (var n = 0; n < 31; n++) childIds.Add((await AddChild(owner, id, $"Unit {n:00}")).Child);
        var children = await Get(owner, $"{Root}/{id}/milestones"); Assert.Equal(25, children.GetProperty("items").GetArrayLength());
        var tail = await Get(owner, $"{Root}/{id}/milestones?cursor={children.GetProperty("nextCursor").GetGuid()}"); Assert.Equal(6, tail.GetProperty("items").GetArrayLength());
        Assert.True(childIds.SetEquals(children.GetProperty("items").EnumerateArray().Concat(tail.GetProperty("items").EnumerateArray()).Select(c => c.GetProperty("id").GetGuid())));
        var ownOtherId = (await Create(owner, "Other parent")).GetProperty("itemId").GetGuid(); var wrongChild = (await AddChild(owner, ownOtherId, "Other parent's unit")).Child;
        Assert.Empty((await Get(owner, $"{Root}/{id}/milestones?cursor={wrongChild}")).GetProperty("items").EnumerateArray());
        using (var foreignRead = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/milestones", other.RawSessionHandle)) Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);
        using (var crossParent = await ChildCommand(owner, id, wrongChild, "update", new { title = "Wrong" }, childTag: "\"supplied\"")) Assert.Equal(HttpStatusCode.NotFound, crossParent.StatusCode);
    }

    [Fact]
    public async Task Admin_exact_actions_current_receipt_dependencies_and_disabled_module_fail_closed()
    {
        await using var scope = await Enable(); var admin = await fixture.CreateActiveSessionAsync("Admin");
        using (var unset = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, unset.StatusCode);
        await Grant(scope, admin, "Allow", "learning.course.create"); var createKey = Guid.NewGuid(); JsonElement created;
        using (var r = await Send(admin, HttpMethod.Post, Root, Body("Private Course", "Milestones"), key: createKey)) created = await Json(r, HttpStatusCode.Created);
        Assert.DoesNotContain("Private tracking", created.ToString()); var id = created.GetProperty("itemId").GetGuid(); var tag = created.GetProperty("etag").GetString()!;
        await Grant(scope, admin, "Allow", "learning.course.update");
        using (var missingRead = await Send(admin, HttpMethod.Put, $"{Root}/{id}", Metadata(), tag)) Assert.Equal(HttpStatusCode.Forbidden, missingRead.StatusCode);
        await Grant(scope, admin, "Allow", "learning.course.read", "learning.course.milestone");
        var first = await AddChild(admin, id, "Pending unit");
        // Uncompleted definitions can be deleted with milestone authority alone.
        using (var pendingDelete = await ChildCommand(admin, id, first.Child, "delete", new { })) await Json(pendingDelete);
        var second = await AddChild(admin, id, "Completed unit");
        using (var noProgress = await ChildCommand(admin, id, second.Child, "completion", new { completed = true })) Assert.Equal(HttpStatusCode.Forbidden, noProgress.StatusCode);
        await Grant(scope, admin, "Allow", "learning.course.progress");
        using (var mark = await ChildCommand(admin, id, second.Child, "completion", new { completed = true })) await Json(mark);
        var parentTag = (await Item(admin, id)).Tag; var childTag = (await Get(admin, $"{Root}/{id}/milestones")).GetProperty("items")[0].GetProperty("etag").GetString()!;
        var deleteKey = Guid.NewGuid();
        await Grant(scope, admin, "Deny", "learning.course.progress");
        using (var deniedDelete = await ChildCommand(admin, id, second.Child, "delete", new { }, parentTag, childTag, deleteKey)) Assert.Equal(HttpStatusCode.Forbidden, deniedDelete.StatusCode);
        await Grant(scope, admin, "Allow", "learning.course.progress");
        using (var removed = await ChildCommand(admin, id, second.Child, "delete", new { }, parentTag, childTag, deleteKey)) await Json(removed);
        using (var replay = await ChildCommand(admin, id, second.Child, "delete", new { }, parentTag, childTag, deleteKey)) await Json(replay);
        await Grant(scope, admin, "Deny", "learning.course.progress");
        using (var revokedReplay = await ChildCommand(admin, id, second.Child, "delete", new { }, parentTag, childTag, deleteKey)) Assert.Equal(HttpStatusCode.Forbidden, revokedReplay.StatusCode);
        var user = await fixture.CreateActiveSessionAsync(); await Policy(scope, false, false);
        foreach (var actor in new[] { user, scope.Super }) { using var r = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, actor.RawSessionHandle); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); }
    }

    [Fact]
    public async Task Purge_pins_and_audit_failure_preserve_owned_children_and_do_not_delete_external_sources()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var key = Guid.NewGuid(); var results = await Task.WhenAll(Send(owner, HttpMethod.Post, Root, Body(), key: key), Send(owner, HttpMethod.Post, Root, Body(), key: key));
        var acks = new List<JsonElement>(); foreach (var r in results) using (r) acks.Add(await Json(r, HttpStatusCode.Created)); Assert.Equal(acks[0].ToString(), acks[1].ToString());
        var id = acks[0].GetProperty("itemId").GetGuid(); var child = await AddChild(owner, id, "Owned payload");
        foreach (var sql in new[] { "UPDATE [learning].[Course] SET Status='Archived',PreArchiveState=NULL WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [learning].[Course] SET ManualProgress=1 WHERE OwnerId=@Owner AND Id=@Id", "UPDATE [learning].[Course] SET CompletedOn='2026-10-02' WHERE OwnerId=@Owner AND Id=@Id" })
        { var e = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync(sql, Id("@Owner", owner.OwnerId), Id("@Id", id))); Assert.Equal(547, e.Number); }
        var badMarker = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [learning].[CourseMilestone] SET Completed=1,CompletedAt=NULL WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", child.Child))); Assert.Equal(547, badMarker.Number);
        var foreignParent = (await Create(other)).GetProperty("itemId").GetGuid();
        var badParent = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [learning].[CourseMilestone] SET CourseId=@Other WHERE OwnerId=@Owner AND Id=@Id", Id("@Other", foreignParent), Id("@Owner", owner.OwnerId), Id("@Id", child.Child))); Assert.Equal(547, badParent.Number);
        var externalId = (await Create(owner, "External linked Course")).GetProperty("itemId").GetGuid();
        using (var complete = await Send(owner, HttpMethod.Post, $"{Root}/{id}/complete", new { confirm = true, completedOn = "2026-10-02" }, (await Item(owner, id)).Tag)) await Json(complete);
        var tag = (await Change(owner, id, "trash", (await Item(owner, id)).Tag)).GetProperty("etag").GetString()!; var pin = Guid.NewGuid();
        await fixture.ExecuteAsync("INSERT [platform].[ResourceLink](Id,OwnerId,SourceResourceId,TargetResourceId,RelationType,State,UpdatedAt) VALUES(@Pin,@Owner,@Id,@Other,N'synthetic-retained','Unavailable',SYSUTCDATETIME())", Id("@Pin", pin), Id("@Owner", owner.OwnerId), Id("@Id", id), Id("@Other", externalId));
        using (var blocked = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, tag)) Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        await fixture.ExecuteAsync("UPDATE [platform].[ResourceLink] SET State='Detached' WHERE OwnerId=@Owner AND Id=@Pin", Id("@Owner", owner.OwnerId), Id("@Pin", pin));
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[SyntheticCourseAuditFailure] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey=N'learning.course.purge') THROW 51000,'Synthetic audit rejection',1; END;");
        var purgeKey = Guid.NewGuid();
        try { using var failed = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, tag, purgeKey); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[SyntheticCourseAuditFailure]"); }
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseCompletionHistory] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Course] c JOIN [platform].[Resource] r ON r.OwnerId=c.OwnerId AND r.Id=c.Id JOIN [operations].[TrashBatch] b ON b.OwnerId=c.OwnerId AND b.Id=c.TrashBatchId WHERE c.OwnerId=@Owner AND c.Id=@Id AND c.Status='Trash' AND r.Availability='Trash' AND b.State='Trashed'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        using (var purged = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, tag, purgeKey)) await Json(purged);
        using (var replay = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, tag, purgeKey)) await Json(replay);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[CourseCompletionHistory] WHERE OwnerId=@Owner AND CourseId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", externalId)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND ActionKey=N'learning.course.purge' AND TargetId=@Id", Id("@User", owner.UserId), Id("@Id", id)));
    }
}
