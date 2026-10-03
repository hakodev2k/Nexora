using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlPersonalAssetTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/assets/personal";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static object Body(string title = "Synthetic asset", string state = "Active") =>
        new { title, kind = "Device", state, brand = "Synthetic brand", model = "Model_%[", category = "Personal", notes = "Private owner definition" };
    private static object Metadata(string title = "Synthetic asset") => new { title, kind = "Electronics", brand = "Changed", model = "Model_%[", category = "Personal", notes = "Edited private note" };
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
    private async Task<JsonElement> Create(SyntheticSession user, string title = "Synthetic asset")
    { using var response = await Send(user, HttpMethod.Post, Root, Body(title)); return await Json(response, HttpStatusCode.Created); }
    private async Task<JsonElement> Change(SyntheticSession user, Guid id, string operation, string etag)
    { using var response = await Send(user, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); return await Json(response); }

    // Installation must preserve administrator policy. Tests enable the installed subset through real preview/commit APIs.
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX37").Clone();
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        Assert.Contains("Metadata and history only", module.GetProperty("name").GetString());
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
    private sealed record PolicyScope(SqlPersonalAssetTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
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
    public async Task Explicit_state_metadata_private_history_and_lifecycle_are_owner_scoped()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        using (var unauth = await fixture.Client.GetAsync(Root)) Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
        foreach (var state in new[] { "", "Loaned", "Archived", "Trash" })
        { using var invalid = await Send(owner, HttpMethod.Post, Root, Body(state: state)); Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode); }
        using (var unknown = await Send(owner, HttpMethod.Post, Root, new { title = "Tamper", kind = "Device", state = "Active", ownerId = foreign.OwnerId })) Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var created = await Create(owner); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        using (var cross = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", foreign.RawSessionHandle)) Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        using (var cross = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", foreign.RawSessionHandle)) Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        using (var cross = await Send(foreign, HttpMethod.Post, $"{Root}/{id}/transition", new { state = "Sold" }, etag)) Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
        using (var missing = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata())) Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using (var stale = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata(), "\"stale\"")) Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using (var bypass = await Send(owner, HttpMethod.Put, $"{Root}/{id}", new { title = "Bypass", kind = "Device", state = "Sold" }, etag)) Assert.Equal(HttpStatusCode.BadRequest, bypass.StatusCode);
        using (var serial = await Send(owner, HttpMethod.Put, $"{Root}/{id}", new { title = "Bypass", kind = "Device", serialEncrypted = "secret" }, etag)) Assert.Equal(HttpStatusCode.BadRequest, serial.StatusCode);
        using (var update = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata("Edited asset"), etag)) etag = (await Json(update)).GetProperty("etag").GetString()!;
        Assert.Equal("Active", (await Get(owner, $"{Root}/{id}")).GetProperty("state").GetString());
        foreach (var state in new[] { "Stored", "Repair", "Sold", "Disposed", "Lost", "Active", "Sold" })
        { using var transition = await Send(owner, HttpMethod.Post, $"{Root}/{id}/transition", new { state, reason = "Private state reason" }, etag); etag = (await Json(transition)).GetProperty("etag").GetString()!; }
        using (var noChange = await Send(owner, HttpMethod.Post, $"{Root}/{id}/transition", new { state = "Sold" }, etag)) Assert.Equal(HttpStatusCode.Conflict, noChange.StatusCode);
        var originalHistory = await Get(owner, $"{Root}/{id}/history"); Assert.Equal(9, originalHistory.GetProperty("items").GetArrayLength());
        var firstVersion = originalHistory.GetProperty("items")[8]; Assert.Equal(1, firstVersion.GetProperty("versionNumber").GetInt64()); Assert.Equal("Private owner definition", firstVersion.GetProperty("snapshot").GetProperty("fields").GetProperty("notes").GetString());
        etag = (await Change(owner, id, "archive", etag)).GetProperty("etag").GetString()!;
        using (var locked = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag)) Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        Assert.Equal(11, (await Get(owner, $"{Root}/{id}/history")).GetProperty("items").GetArrayLength());
        etag = (await Change(owner, id, "restore", etag)).GetProperty("etag").GetString()!;
        Assert.Equal("Archived", (await Get(owner, $"{Root}/{id}")).GetProperty("state").GetString());
        etag = (await Change(owner, id, "unarchive", etag)).GetProperty("etag").GetString()!;
        Assert.Equal("Sold", (await Get(owner, $"{Root}/{id}")).GetProperty("state").GetString());
        var history = await Get(owner, $"{Root}/{id}/history"); Assert.Equal(13, history.GetProperty("items").GetArrayLength());
        Assert.Equal(firstVersion.ToString(), history.GetProperty("items")[12].ToString());
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND (RedactedDiffJson IS NOT NULL OR ActionKey LIKE '%Private%' OR TargetType LIKE '%Private%')", Id("@User", owner.UserId)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt] WHERE OperationKey LIKE 'assets.asset.%' AND (CHARINDEX(N'Private owner definition',ResultJson)>0 OR CHARINDEX(N'Private state reason',ResultJson)>0 OR CHARINDEX(N'Edited private note',ResultJson)>0)", Id("@User", owner.UserId)));
    }

    [Fact]
    public async Task Name_keyset_and_history_paging_filter_before_cursor()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var ids = new HashSet<Guid>(); for (var n = 0; n < 31; n++) ids.Add((await Create(owner, n < 28 ? "Identical name" : $"Z-{n}")).GetProperty("itemId").GetGuid());
        var foreign = await Create(other, "Identical name"); var first = await Get(owner, Root); Assert.Equal(25, first.GetProperty("items").GetArrayLength());
        var cursor = first.GetProperty("nextCursor").GetGuid(); var second = await Get(owner, $"{Root}?cursor={cursor}"); Assert.Equal(6, second.GetProperty("items").GetArrayLength());
        Assert.Equal(ids.Order(), first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(i => i.GetProperty("id").GetGuid()).Order());
        using var sqlOrder = JsonDocument.Parse(await fixture.ScalarStringAsync("SELECT Id FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND State='Active' ORDER BY Title ASC,Id ASC FOR JSON PATH", Id("@Owner", owner.OwnerId)));
        Assert.Equal(sqlOrder.RootElement.EnumerateArray().Select(row => row.GetProperty("Id").GetGuid()), first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(row => row.GetProperty("id").GetGuid()));
        foreach (var unavailable in new[] { foreign.GetProperty("itemId").GetGuid(), Guid.NewGuid() })
            Assert.Empty((await Get(owner, $"{Root}?cursor={unavailable}")).GetProperty("items").EnumerateArray());
        foreach (var suffix in new[] { "state=Archived", "kind=Other", "category=Other" })
            Assert.Empty((await Get(owner, $"{Root}?cursor={cursor}&{suffix}")).GetProperty("items").EnumerateArray());
        Assert.Equal(25, (await Get(owner, $"{Root}?query={Uri.EscapeDataString("_%[")}")).GetProperty("items").GetArrayLength());
        var created = await Create(owner, "History subject"); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        for (var n = 0; n < 30; n++) { using var update = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata($"Version-{n}"), etag); etag = (await Json(update)).GetProperty("etag").GetString()!; }
        var h1 = await Get(owner, $"{Root}/{id}/history"); var hcursor = h1.GetProperty("nextCursor").GetGuid(); var h2 = await Get(owner, $"{Root}/{id}/history?cursor={hcursor}");
        Assert.Equal(25, h1.GetProperty("items").GetArrayLength()); Assert.Equal(6, h2.GetProperty("items").GetArrayLength());
        Assert.Equal(Enumerable.Range(1,31).Select(n => (long)n).Reverse(), h1.GetProperty("items").EnumerateArray().Concat(h2.GetProperty("items").EnumerateArray()).Select(v => v.GetProperty("versionNumber").GetInt64()));
        Assert.Empty((await Get(owner, $"{Root}/{id}/history?cursor={hcursor}&action=assets.asset.create")).GetProperty("items").EnumerateArray());
        var otherHistory = (await Get(other, $"{Root}/{foreign.GetProperty("itemId").GetGuid()}/history")).GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Empty((await Get(owner, $"{Root}/{id}/history?cursor={otherHistory}")).GetProperty("items").EnumerateArray());
        Assert.Single((await Get(owner, $"{Root}/{id}/history?action=assets.asset.create")).GetProperty("items").EnumerateArray());
        Assert.Equal(1L, (await Get(owner, $"{Root}/{id}/history?version=1")).GetProperty("items")[0].GetProperty("versionNumber").GetInt64());
        using (var badVersion = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history?version=9223372036854775808", owner.RawSessionHandle)) Assert.Equal(HttpStatusCode.UnprocessableEntity, badVersion.StatusCode);
        Assert.Empty((await Get(owner, $"{Root}/{id}/history?from=2000-01-01T00:00:00Z&to=2001-01-01T00:00:00Z")).GetProperty("items").EnumerateArray());
        using (var bad = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history?from=2026-10-01", owner.RawSessionHandle)) Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
    }

    [Fact]
    public async Task Independent_permissions_current_receipt_replay_and_disabled_module_fail_closed()
    {
        await using var scope = await Enable(); var admin = await fixture.CreateActiveSessionAsync("Admin");
        using (var unset = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, unset.StatusCode);
        await Grant(scope, admin, "Allow", "assets.asset.create"); var key = Guid.NewGuid(); JsonElement created;
        using (var create = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) created = await Json(create, HttpStatusCode.Created);
        Assert.DoesNotContain("Private owner", created.ToString()); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        using (var replay = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) Assert.Equal(created.ToString(), (await Json(replay, HttpStatusCode.Created)).ToString());
        await Grant(scope, admin, "Deny", "assets.asset.create"); using (var revoked = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        await Grant(scope, admin, "Allow", "assets.asset.update");
        using (var noRead = await Send(admin, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag)) Assert.Equal(HttpStatusCode.Forbidden, noRead.StatusCode);
        await Grant(scope, admin, "Allow", "assets.asset.history", "assets.asset.transition");
        Assert.Single((await Get(admin, $"{Root}/{id}/history")).GetProperty("items").EnumerateArray());
        using (var state = await Send(admin, HttpMethod.Post, $"{Root}/{id}/transition", new { state = "Stored" }, etag)) etag = (await Json(state)).GetProperty("etag").GetString()!;
        await Grant(scope, admin, "Allow", "assets.asset.read");
        using (var update = await Send(admin, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag)) etag = (await Json(update)).GetProperty("etag").GetString()!;
        await Grant(scope, admin, "Deny", "assets.asset.transition");
        using (var revoked = await Send(admin, HttpMethod.Post, $"{Root}/{id}/transition", new { state = "Sold" }, etag)) Assert.Equal(HttpStatusCode.Forbidden, revoked.StatusCode);
        await Grant(scope, admin, "Unset", "assets.asset.history"); using (var history = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
        await Policy(scope, false, false);
        foreach (var actor in new[] { admin, scope.Super }) { using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, actor.RawSessionHandle); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); }
    }

    [Fact]
    public async Task Sql_history_immutability_owner_and_null_safe_cohort_constraints_hold()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var created = await Create(owner); var id = created.GetProperty("itemId").GetGuid();
        foreach (var invalid in new[] {
            "UPDATE [assets].[PersonalAsset] SET State='Archived',PreArchiveState=NULL WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [assets].[PersonalAsset] SET State='Trash',PreTrashState=NULL WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [assets].[PersonalAsset] SET State='Loaned' WHERE OwnerId=@Owner AND Id=@Id",
            "UPDATE [assets].[PersonalAsset] SET Kind='Vps' WHERE OwnerId=@Owner AND Id=@Id" })
        { var error = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync(invalid, Id("@Owner", owner.OwnerId), Id("@Id", id))); Assert.Equal(547, error.Number); }
        var immutable = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [assets].[AssetVersion] SET Reason='Changed' WHERE OwnerId=@Owner AND AssetId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id))); Assert.Equal(51038, immutable.Number);
        var foreign = await Create(other); var foreignId = foreign.GetProperty("itemId").GetGuid();
        var ownerFK = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("INSERT [assets].[AssetVersion](Id,OwnerId,AssetId,VersionNumber,ActionKey,SafeSnapshotJson) VALUES(NEWID(),@Owner,@Foreign,999,'assets.asset.create',N'{\"schemaVersion\":1,\"resourceType\":\"PersonalAsset\"}')", Id("@Owner", owner.OwnerId), Id("@Foreign", foreignId))); Assert.Equal(547, ownerFK.Number);
        var trashed = await Change(owner, id, "trash", created.GetProperty("etag").GetString()!); var etag = trashed.GetProperty("etag").GetString()!;
        await fixture.ExecuteAsync("UPDATE [operations].[TrashMember] SET PreviousLifecycle='Archived' WHERE OwnerId=@Owner AND ResourceId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id));
        foreach (var operation in new[] { "restore", "purge" })
        { using var bad = await Send(owner, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); Assert.Equal(HttpStatusCode.Conflict, bad.StatusCode); }
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[AssetVersion] WHERE OwnerId=@Owner AND AssetId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
    }

    [Fact]
    public async Task Idempotency_audit_failure_and_pinned_purge_keep_history_atomic()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var key = Guid.NewGuid();
        var concurrent = await Task.WhenAll(Send(owner, HttpMethod.Post, Root, Body(), key: key), Send(owner, HttpMethod.Post, Root, Body(), key: key));
        JsonElement created = default; foreach (var response in concurrent) { using (response) { var value = await Json(response, HttpStatusCode.Created); if (created.ValueKind == JsonValueKind.Undefined) created = value; else Assert.Equal(created.ToString(), value.ToString()); } }
        var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[AssetVersion] WHERE OwnerId=@Owner AND AssetId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        var receipts = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]");
        await fixture.ExecuteAsync("CREATE OR ALTER TRIGGER [security].[TR_AssetTestAuditFailure] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='assets.asset.update') THROW 51039,'Synthetic audit failure',1; END");
        try
        { using var failed = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Metadata(), etag); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[TR_AssetTestAuditFailure]"); }
        Assert.Equal(receipts, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]"));
        Assert.Equal("Synthetic asset", (await Get(owner, $"{Root}/{id}")).GetProperty("title").GetString());
        Assert.Equal(etag, (await Get(owner, $"{Root}/{id}")).GetProperty("etag").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[AssetVersion] WHERE OwnerId=@Owner AND AssetId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        var source = await Create(owner, "Independent source"); var sourceId = source.GetProperty("itemId").GetGuid();
        var pinned = Guid.NewGuid();
        await fixture.ExecuteAsync("INSERT [platform].[ResourceLink](Id,OwnerId,SourceResourceId,TargetResourceId,RelationType,State,TargetVersion,UpdatedAt) VALUES(@Link,@Owner,@Source,@Target,'SyntheticHistoryPin','Unavailable',1,SYSUTCDATETIME())", Id("@Link", pinned), Id("@Owner", owner.OwnerId), Id("@Source", sourceId), Id("@Target", id), Id("@User", owner.UserId));
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        using (var blocked = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag)) Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        await fixture.ExecuteAsync("UPDATE [platform].[ResourceLink] SET State='Detached' WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", pinned));
        await Change(owner, id, "purge", etag);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[AssetVersion] WHERE OwnerId=@Owner AND AssetId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id AND Availability='Purged'", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[PersonalAsset] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", sourceId)));
    }
}
