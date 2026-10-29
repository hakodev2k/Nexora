using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlSkillTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/learning/skills";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static object Body(string title = "Synthetic skill", string level = "Beginner") =>
        new { title, level, description = "Private owner definition", category = "Engineering", lastUsed = "2026-09-30" };
    private static object Metadata(string title = "Synthetic skill") => new { title, description = "Edited definition", category = "Personal", lastUsed = "2026-10-01" };
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
    private sealed record PolicyScope(SqlSkillTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
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


    [Fact]
    public async Task Skill_metadata_assessment_and_archive_cohort_are_real_and_owner_scoped()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        using (var unauth = await fixture.Client.GetAsync(Root)) Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        var created = await Create(owner, "  Ａpi Skill  "); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        var item = await Get(owner, $"{Root}/{id}"); Assert.Equal("Beginner", item.GetProperty("level").GetString());
        Assert.Equal("2026-09-30", item.GetProperty("lastUsed").GetString()); Assert.Equal("Engineering", item.GetProperty("category").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] s JOIN [platform].[Resource] r ON r.OwnerId=s.OwnerId AND r.Id=s.Id WHERE s.OwnerId=@Owner AND s.Id=@Id AND s.NormalizedTitle=N'API SKILL' AND r.Availability='Active'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        var receiptCount = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]");
        var createAuditCount = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND ActionKey=N'learning.skill.create'", Id("@User", owner.UserId));
        using (var duplicate = await Send(owner, HttpMethod.Post, Root, Body("api skill"))) Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(receiptCount, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]"));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId)));
        Assert.Equal(createAuditCount, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND ActionKey=N'learning.skill.create'", Id("@User", owner.UserId)));
        using (var ownOther = await Send(foreign, HttpMethod.Post, Root, Body("api skill"))) await Json(ownOther, HttpStatusCode.Created);
        using (var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", foreign.RawSessionHandle)) Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using (var denied = await Send(foreign, HttpMethod.Post, $"{Root}/{id}/proficiency", new { level = "Expert" }, etag)) Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using (var missing = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata())) Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using (var stale = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata(), "\"stale\"")) Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using (var bypass = await Send(owner, HttpMethod.Put, $"{Root}/{id}", new { title = "Changed", level = "Expert" }, etag)) Assert.Equal(HttpStatusCode.BadRequest, bypass.StatusCode);
        using (var update = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata("Edited skill"), etag)) etag = (await Json(update)).GetProperty("etag").GetString()!;
        Assert.Equal("Beginner", (await Get(owner, $"{Root}/{id}")).GetProperty("level").GetString());
        using (var assess = await Send(owner, HttpMethod.Post, $"{Root}/{id}/proficiency", new { level = "Advanced" }, etag)) etag = (await Json(assess)).GetProperty("etag").GetString()!;
        Assert.Equal("Advanced", (await Get(owner, $"{Root}/{id}")).GetProperty("level").GetString());
        etag = (await Change(owner, id, "archive", etag)).GetProperty("etag").GetString()!;
        using (var duplicate = await Send(owner, HttpMethod.Post, Root, Body("edited skill"))) Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using (var locked = await Send(owner, HttpMethod.Post, $"{Root}/{id}/proficiency", new { level = "Expert" }, etag)) Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        using (var duplicate = await Send(owner, HttpMethod.Post, Root, Body("edited skill"))) Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        etag = (await Change(owner, id, "restore", etag)).GetProperty("etag").GetString()!;
        Assert.Equal("Archived", (await Get(owner, $"{Root}/{id}")).GetProperty("status").GetString());
        etag = (await Change(owner, id, "unarchive", etag)).GetProperty("etag").GetString()!;
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id AND Level='Advanced' AND Status='Active' AND TrashBatchId IS NULL AND PreArchiveState IS NULL AND PreTrashState IS NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id WHERE b.OwnerId=@Owner AND b.RootResourceId=@Id AND b.State='Restored' AND m.PreviousLifecycle='Archived'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
    }

    [Fact]
    public async Task Pagination_uniqueness_and_normalized_length_do_not_hide_or_round_owner_records()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var ids = new HashSet<Guid>(); for (var n = 0; n < 31; n++) ids.Add((await Create(owner, $"skill-{n:00}")).GetProperty("itemId").GetGuid());
        // The API created every payload. Only synthetic server timestamps are tied to exercise the keyset boundary.
        await fixture.ExecuteAsync("UPDATE [learning].[Skill] SET UpdatedAt='2026-10-01T00:00:00' WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId));
        var foreign = await Create(other); var first = await Get(owner, Root);
        Assert.Equal(25, first.GetProperty("items").GetArrayLength()); var cursor = first.GetProperty("nextCursor").GetGuid();
        var second = await Get(owner, $"{Root}?cursor={cursor}"); Assert.Equal(6, second.GetProperty("items").GetArrayLength());
        Assert.Equal(ids.Order(), first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(i => i.GetProperty("id").GetGuid()).Order());
        foreach (var unavailable in new[] { foreign.GetProperty("itemId").GetGuid(), Guid.NewGuid() })
            Assert.Empty((await Get(owner, $"{Root}?cursor={unavailable}")).GetProperty("items").EnumerateArray());
        Assert.Empty((await Get(owner, $"{Root}?cursor={cursor}&status=Archived")).GetProperty("items").EnumerateArray());
        await Create(owner, "Literal_%[ Skill"); Assert.Single((await Get(owner, $"{Root}?query={Uri.EscapeDataString("_%[")}")).GetProperty("items").EnumerateArray());
        // Compatibility ligatures expand to several normalized characters, exceeding the retained SQL name bound.
        using (var expanded = await Send(owner, HttpMethod.Post, Root, Body(new string('\uFB03', 100)))) Assert.Equal(HttpStatusCode.UnprocessableEntity, expanded.StatusCode);
        using (var invalid = await Send(owner, HttpMethod.Post, Root, Body("Invalid", "Certified"))) Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        using (var ownerField = await Send(owner, HttpMethod.Post, Root, new { title = "Tamper", level = "Beginner", ownerId = other.OwnerId })) Assert.Equal(HttpStatusCode.BadRequest, ownerField.StatusCode);
        Assert.Equal(32, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId)));
    }

    [Fact]
    public async Task Admin_action_composition_replay_and_disabled_module_fail_closed()
    {
        await using var scope = await Enable(); var admin = await fixture.CreateActiveSessionAsync("Admin");
        using (var unset = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, unset.StatusCode);
        await Grant(scope, admin, "Allow", "learning.skill.create");
        using (var noAssessment = await Send(admin, HttpMethod.Post, Root, Body())) Assert.Equal(HttpStatusCode.Forbidden, noAssessment.StatusCode);
        await Grant(scope, admin, "Allow", "learning.skill.proficiency");
        var key = Guid.NewGuid(); JsonElement created; using (var result = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) created = await Json(result, HttpStatusCode.Created);
        Assert.DoesNotContain("Private owner", created.ToString()); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        using (var replay = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) Assert.Equal(created.ToString(), (await Json(replay, HttpStatusCode.Created)).ToString());
        await Grant(scope, admin, "Deny", "learning.skill.proficiency");
        using (var revoked = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        await Grant(scope, admin, "Allow", "learning.skill.update");
        using (var noRead = await Send(admin, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag)) Assert.Equal(HttpStatusCode.Forbidden, noRead.StatusCode);
        await Grant(scope, admin, "Allow", "learning.skill.read");
        using (var update = await Send(admin, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag)) etag = (await Json(update)).GetProperty("etag").GetString()!;
        using (var noLevel = await Send(admin, HttpMethod.Post, $"{Root}/{id}/proficiency", new { level = "Expert" }, etag)) Assert.Equal(HttpStatusCode.Forbidden, noLevel.StatusCode);
        Assert.Equal("Beginner", (await Get(admin, $"{Root}/{id}")).GetProperty("level").GetString());
        var user = await fixture.CreateActiveSessionAsync(); await Policy(scope, false, false);
        foreach (var actor in new[] { user, scope.Super }) { using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, actor.RawSessionHandle); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); }
    }

    [Fact]
    public async Task Sql_constraints_and_frozen_cohort_reject_invalid_owner_and_state()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var item = await Create(owner); var id = item.GetProperty("itemId").GetGuid();
        foreach (var invalidSql in new[] {
            "UPDATE [learning].[Skill] SET Status='Archived',PreArchiveState=NULL WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [learning].[Skill] SET MergedIntoId=Id WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [learning].[Skill] SET Level='Verified' WHERE OwnerId=@Owner AND Id=@Id" })
        {
            var error = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync(invalidSql, Id("@Owner", owner.OwnerId), Id("@Id", id)));
            Assert.Equal(547, error.Number);
        }
        var other = await Create(foreign); var otherId = other.GetProperty("itemId").GetGuid();
        await Change(foreign, otherId, "trash", other.GetProperty("etag").GetString()!);
        var foreignBatch = Guid.Parse(await fixture.ScalarStringAsync("SELECT CONVERT(nvarchar(36),TrashBatchId) FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", foreign.OwnerId), Id("@Id", otherId)));
        var crossOwner = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [learning].[Skill] SET Status='Trash',PreTrashState='Active',TrashBatchId=@Batch WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id), Id("@Batch", foreignBatch)));
        Assert.Equal(547, crossOwner.Number);
        var trashed = await Change(owner, id, "trash", item.GetProperty("etag").GetString()!); var etag = trashed.GetProperty("etag").GetString()!;
        await fixture.ExecuteAsync("UPDATE [operations].[TrashMember] SET PreviousLifecycle='Archived' WHERE OwnerId=@Owner AND ResourceId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id));
        foreach (var operation in new[] { "restore", "purge" })
        { using var invalid = await Send(owner, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); Assert.Equal(HttpStatusCode.Conflict, invalid.StatusCode); }
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id AND Status='Trash'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        await Create(owner, "résumé"); await Create(owner, "resume"); // BIN2 normalized uniqueness does not collapse accent-distinct names.
    }

    [Fact]
    public async Task Purge_reference_guards_audit_rollback_and_idempotency_are_atomic()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        var key = Guid.NewGuid(); var body = Body();
        var simultaneous = await Task.WhenAll(Send(owner, HttpMethod.Post, Root, body, key: key), Send(owner, HttpMethod.Post, Root, body, key: key));
        var values = new List<JsonElement>(); foreach (var result in simultaneous) { using (result) values.Add(await Json(result, HttpStatusCode.Created)); }
        Assert.Equal(values[0].ToString(), values[1].ToString()); var id = values[0].GetProperty("itemId").GetGuid();
        using (var mismatch = await Send(owner, HttpMethod.Post, Root, Body("Other request"), key: key)) Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        var etag = (await Change(owner, id, "trash", values[0].GetProperty("etag").GetString()!)).GetProperty("etag").GetString()!;
        using (var locked = await Send(owner, HttpMethod.Post, $"{Root}/{id}/proficiency", new { level = "Expert" }, etag)) Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        using (var cancel = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = false }, etag)) Assert.Equal(HttpStatusCode.UnprocessableEntity, cancel.StatusCode);
        var pin = Guid.NewGuid();
        await fixture.ExecuteAsync("INSERT [platform].[ResourceLink](Id,OwnerId,SourceResourceId,TargetResourceId,RelationType,State,UpdatedAt) VALUES(@Pin,@Owner,@Id,@Id,N'synthetic-retained','Unavailable',SYSUTCDATETIME());", Id("@Pin", pin), Id("@Owner", owner.OwnerId), Id("@Id", id));
        Assert.Equal(1, (await Json(await Send(owner, HttpMethod.Post, $"{Root}/{id}/preview-purge", new { }))).GetProperty("referenceCount").GetInt32());
        using (var pinned = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag)) Assert.Equal(HttpStatusCode.Conflict, pinned.StatusCode);
        await fixture.ExecuteAsync("UPDATE [platform].[ResourceLink] SET State='Detached' WHERE Id=@Pin AND OwnerId=@Owner", Id("@Pin", pin), Id("@Owner", owner.OwnerId));
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[SyntheticSkillAuditFailure] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey=N'learning.skill.purge') THROW 51000,'Synthetic audit rejection',1; END;");
        var purgeKey = Guid.NewGuid();
        try { using var failed = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, purgeKey); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[SyntheticSkillAuditFailure];"); }
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] s JOIN [platform].[Resource] r ON r.OwnerId=s.OwnerId AND r.Id=s.Id JOIN [operations].[TrashBatch] b ON b.OwnerId=s.OwnerId AND b.Id=s.TrashBatchId WHERE s.OwnerId=@Owner AND s.Id=@Id AND s.Status='Trash' AND r.Availability='Trash' AND b.State='Trashed'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        using (var purged = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, purgeKey)) await Json(purged);
        using (var replay = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, purgeKey)) await Json(replay);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Skill] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActionKey=N'learning.skill.purge' AND TargetId=@Id", Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id AND Availability='Purged' AND PurgedAt IS NOT NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
    }
}
