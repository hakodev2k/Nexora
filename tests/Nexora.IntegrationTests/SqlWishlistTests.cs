using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlWishlistTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/shopping/wishlist";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static object Body(string title = "Synthetic purchase", string quantity = "1.12345678", string? amount = "12345678901234567890.12345678", string? currency = "USD") =>
        new { title, quantity, targetAmount = amount, currency, notes = "Private synthetic note", url = "https://example.invalid/item" };
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
        var module = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX31").Clone();
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        Assert.Contains("Wishlist only", module.GetProperty("name").GetString());
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
    private sealed record PolicyScope(SqlWishlistTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
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
    public async Task Lifecycle_preserves_decimals_and_archive_state_in_real_deletion_cohorts()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var created = await Create(owner); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        var item = await Get(owner, $"{Root}/{id}");
        Assert.Equal("1.12345678", item.GetProperty("quantity").GetString()); Assert.Equal("12345678901234567890.12345678", item.GetProperty("targetAmount").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [shopping].[WishlistItem] w JOIN [platform].[Resource] r ON r.OwnerId=w.OwnerId AND r.Id=w.Id WHERE w.OwnerId=@Owner AND w.Id=@Id AND r.Availability='Active' AND w.Quantity=1.12345678 AND w.TargetAmount=12345678901234567890.12345678", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        using (var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", foreign.RawSessionHandle)) Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        foreach (var operation in new[] { "purge", "unarchive", "restore" })
        { using var preview = await Send(owner, HttpMethod.Post, $"{Root}/{id}/preview-{operation}", new { }); Assert.Equal(HttpStatusCode.Conflict, preview.StatusCode); }
        using (var missing = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body())) Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using (var stale = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body(), "\"stale\"")) Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using (var update = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body("Updated synthetic"), etag)) etag = (await Json(update)).GetProperty("etag").GetString()!;
        etag = (await Change(owner, id, "mark-purchased", etag)).GetProperty("etag").GetString()!;
        etag = (await Change(owner, id, "archive", etag)).GetProperty("etag").GetString()!;
        using (var locked = await Send(owner, HttpMethod.Put, $"{Root}/{id}", Body(), etag)) Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [shopping].[WishlistItem] w JOIN [operations].[TrashBatch] b ON b.OwnerId=w.OwnerId AND b.Id=w.TrashBatchId JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id AND m.ResourceId=w.Id WHERE w.OwnerId=@Owner AND w.Id=@Id AND w.PreTrashState='Archived' AND w.PreArchiveState='Purchased' AND b.State='Trashed' AND m.PreviousLifecycle='Archived' AND m.Depth=0", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [shopping].[WishlistItem] SET PreArchiveState=NULL WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [shopping].[WishlistItem] SET PreTrashState=NULL WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        using (var other = await Send(foreign, HttpMethod.Post, $"{Root}/{id}/restore", new { confirm = true }, etag)) Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        etag = (await Change(owner, id, "restore", etag)).GetProperty("etag").GetString()!;
        Assert.Equal("Archived", (await Get(owner, $"{Root}/{id}")).GetProperty("status").GetString());
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[TrashBatch] WHERE OwnerId=@Owner AND RootResourceId=@Id AND State='Restored' AND RestoredAt IS NOT NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        etag = (await Change(owner, id, "unarchive", etag)).GetProperty("etag").GetString()!;
        Assert.Equal("Purchased", (await Get(owner, $"{Root}/{id}")).GetProperty("status").GetString());
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        using var purge = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag); await Json(purge);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [shopping].[WishlistItem] WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id WHERE b.OwnerId=@Owner AND b.RootResourceId=@Id AND b.State='Purged' AND m.PurgedAt IS NOT NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id AND Availability='Purged' AND PurgedAt IS NOT NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
    }

    [Fact]
    public async Task Purge_rejects_pins_and_rolls_back_payload_cohort_registry_and_receipt_on_audit_failure()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        var created = await Create(owner); var id = created.GetProperty("itemId").GetGuid(); var etag = created.GetProperty("etag").GetString()!;
        var source = (await Create(owner, "Synthetic pin source")).GetProperty("itemId").GetGuid(); var link = Guid.NewGuid();
        await fixture.ExecuteAsync("INSERT [platform].[ResourceLink](Id,OwnerId,SourceResourceId,TargetResourceId,RelationType,State,UpdatedAt) VALUES(@Link,@Owner,@Source,@Id,'SyntheticReference','Active',SYSUTCDATETIME())", Id("@Link", link), Id("@Owner", owner.OwnerId), Id("@Source", source), Id("@Id", id));
        etag = (await Change(owner, id, "trash", etag)).GetProperty("etag").GetString()!;
        using (var preview = await Send(owner, HttpMethod.Post, $"{Root}/{id}/preview-purge", new { })) Assert.Equal(1, (await Json(preview)).GetProperty("referenceCount").GetInt32());
        using (var pinned = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag)) Assert.Equal(HttpStatusCode.Conflict, pinned.StatusCode);
        // Synthetic negative reference setup is detached after proving the actual production guard.
        await fixture.ExecuteAsync("UPDATE [platform].[ResourceLink] SET State='Detached' WHERE OwnerId=@Owner AND Id=@Id", Id("@Owner", owner.OwnerId), Id("@Id", link));
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[RejectWishlistPurge] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='shopping.wishlist.purge') THROW 51005,'Synthetic audit failure',1; END;");
        var key = Guid.NewGuid();
        try
        {
            using var failed = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, key); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Equal("Trash", (await Get(owner, $"{Root}/{id}")).GetProperty("status").GetString());
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id WHERE b.OwnerId=@Owner AND b.RootResourceId=@Id AND b.State='Trashed' AND m.PurgedAt IS NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id AND Availability='Trash' AND PurgedAt IS NULL", Id("@Owner", owner.OwnerId), Id("@Id", id)));
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectWishlistPurge]"); }
        using (var purge = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, key)) await Json(purge);
        using (var replay = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = true }, etag, key)) Assert.Equal(id, (await Json(replay)).GetProperty("itemId").GetGuid());
        using (var conflict = await Send(owner, HttpMethod.Post, $"{Root}/{id}/purge", new { confirm = false }, etag, key)) Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='shopping.wishlist.purge'", Id("@Id", id)));
    }

    [Fact]
    public async Task Updated_pagination_search_and_cursor_boundaries_remain_owner_and_state_scoped()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var marker = "list_" + Guid.NewGuid().ToString("N") + "_%["; var expected = new HashSet<Guid>(); JsonElement firstCreated = default;
        for (var index = 0; index < 31; index++) { var created = await Create(owner, marker + index); if (index == 0) firstCreated = created; expected.Add(created.GetProperty("itemId").GetGuid()); }
        var firstId = firstCreated.GetProperty("itemId").GetGuid();
        using (var update = await Send(owner, HttpMethod.Put, $"{Root}/{firstId}", Body(marker + "updated"), firstCreated.GetProperty("etag").GetString())) await Json(update);
        var foreignId = (await Create(foreign, marker + "foreign")).GetProperty("itemId").GetGuid();
        var filtered = (await Create(owner, "Not in filter")).GetProperty("itemId").GetGuid();
        var path = Root + "?query=" + Uri.EscapeDataString(marker); var first = await Get(owner, path);
        Assert.Equal(25, first.GetProperty("items").GetArrayLength()); Assert.Equal(firstId, first.GetProperty("items")[0].GetProperty("id").GetGuid());
        var second = await Get(owner, path + "&cursor=" + first.GetProperty("nextCursor").GetGuid());
        Assert.Equal(6, second.GetProperty("items").GetArrayLength()); Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        var items = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).ToArray();
        var ids = items.Select(item => item.GetProperty("id").GetGuid()).ToArray(); Assert.Equal(31, ids.Distinct().Count()); Assert.True(expected.SetEquals(ids));
        var dates = items.Select(item => item.GetProperty("updatedAt").GetDateTimeOffset()).ToArray(); Assert.Equal(dates.OrderDescending().ToArray(), dates);
        foreach (var cursor in new[] { foreignId, filtered, Guid.NewGuid() }) Assert.Empty((await Get(owner, path + "&cursor=" + cursor)).GetProperty("items").EnumerateArray());
        Assert.Empty((await Get(owner, Root + "?status=Archived&query=" + Uri.EscapeDataString(marker))).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Cohort_integrity_rejects_other_owner_links_and_inconsistent_frozen_membership()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var created = await Create(owner); var id = created.GetProperty("itemId").GetGuid();
        var etag = (await Change(owner, id, "trash", created.GetProperty("etag").GetString()!)).GetProperty("etag").GetString()!;
        var other = await Create(foreign); var otherId = other.GetProperty("itemId").GetGuid();
        await Change(foreign, otherId, "trash", other.GetProperty("etag").GetString()!);
        await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync("UPDATE [shopping].[WishlistItem] SET TrashBatchId=(SELECT TrashBatchId FROM [shopping].[WishlistItem] WHERE OwnerId=@Foreign AND Id=@Other) WHERE OwnerId=@Owner AND Id=@Id", Id("@Foreign", foreign.OwnerId), Id("@Other", otherId), Id("@Owner", owner.OwnerId), Id("@Id", id)));
        // Deliberately corrupt only a synthetic negative fixture; an API command must fail closed rather than silently repair it.
        await fixture.ExecuteAsync("UPDATE [operations].[TrashMember] SET PreviousLifecycle='Purchased' WHERE OwnerId=@Owner AND ResourceId=@Id", Id("@Owner", owner.OwnerId), Id("@Id", id));
        foreach (var operation in new[] { "restore", "purge" })
        { using var rejected = await Send(owner, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode); }
        Assert.Equal("Trash", (await Get(owner, $"{Root}/{id}")).GetProperty("status").GetString());
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey IN ('shopping.wishlist.restore','shopping.wishlist.purge')", Id("@Id", id)));
    }

    [Fact]
    public async Task Exact_grants_validation_and_current_enablement_cannot_be_bypassed_by_receipt_replay()
    {
        await using var scope = await Enable(); var admin = await fixture.CreateActiveSessionAsync("Admin"); var user = await fixture.CreateActiveSessionAsync();
        using (var anonymous = await fixture.Client.GetAsync(Root)) Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using (var unset = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, unset.StatusCode);
        await Grant(scope, admin, "Allow", "shopping.wishlist.create");
        admin = await fixture.CreateActiveSessionForUserAsync(admin.UserId);
        var key = Guid.NewGuid(); JsonElement created;
        using (var create = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) created = await Json(create, HttpStatusCode.Created);
        Assert.False(created.TryGetProperty("title", out _)); Assert.False(created.TryGetProperty("notes", out _));
        using (var noRead = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{created.GetProperty("itemId").GetGuid()}", admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, noRead.StatusCode);
        await Grant(scope, admin, "Deny", "shopping.wishlist.create"); admin = await fixture.CreateActiveSessionForUserAsync(admin.UserId);
        using (var replay = await Send(admin, HttpMethod.Post, Root, Body(), key: key)) Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        await Grant(scope, admin, "Allow", "shopping.wishlist.read", "shopping.wishlist.update"); admin = await fixture.CreateActiveSessionForUserAsync(admin.UserId);
        Assert.Single((await Get(admin, Root)).GetProperty("items").EnumerateArray());
        await Grant(scope, admin, "Unset", "shopping.wishlist.read"); admin = await fixture.CreateActiveSessionForUserAsync(admin.UserId);
        using (var noPrerequisite = await Send(admin, HttpMethod.Put, $"{Root}/{created.GetProperty("itemId").GetGuid()}", Body(), created.GetProperty("etag").GetString())) Assert.Equal(HttpStatusCode.Forbidden, noPrerequisite.StatusCode);
        foreach (var body in new[] { Body(quantity: "0"), Body(quantity: "1.123456789"), Body(amount: "-1"), Body(currency: null), Body(amount: null), Body(currency: "ZZZ"), Body(title: new string('x', 201)) })
        { using var invalid = await Send(user, HttpMethod.Post, Root, body); Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode); }
        using (var unknown = await Send(user, HttpMethod.Post, Root, new { title = "Synthetic", quantity = "1", ownerId = Guid.NewGuid() })) Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var ownerItem = await Create(user); var id = ownerItem.GetProperty("itemId").GetGuid();
        using (var unconfirmed = await Send(user, HttpMethod.Post, $"{Root}/{id}/trash", new { confirm = false }, ownerItem.GetProperty("etag").GetString())) Assert.Equal(HttpStatusCode.UnprocessableEntity, unconfirmed.StatusCode);
        await Policy(scope, false, false);
        using (var disabled = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, user.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, disabled.StatusCode);
        using (var disabledSuper = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root, scope.Super.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, disabledSuper.StatusCode);
    }
}
