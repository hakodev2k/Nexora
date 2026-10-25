using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlShoppingRecordTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/shopping/records";
    private async Task<HttpResponseMessage> Send(SyntheticSession actor, HttpMethod method, string path, object body, string? etag = null, Guid? key = null) =>
        await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), actor.RawSessionHandle, etag);
    private async Task<JsonElement> Get(SyntheticSession actor, string path)
    { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, actor.RawSessionHandle); return await Json(response); }
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.Clone();
    }
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var admin = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(admin, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX31");
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        var scope = new PolicyScope(this, admin, module.GetProperty("id").GetGuid(), module.GetProperty("systemEnabled").GetBoolean(), module.GetProperty("registrationEnabled").GetBoolean());
        await Policy(scope, true, true); return scope;
    }
    private async Task Policy(PolicyScope scope, bool system, bool registration)
    {
        var module = (await Get(scope.Admin, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == scope.Module);
        if (module.GetProperty("systemEnabled").GetBoolean() == system && module.GetProperty("registrationEnabled").GetBoolean() == registration) return;
        using var previewResponse = await Send(scope.Admin, HttpMethod.Post, $"/api/v1/admin/modules/{scope.Module}/preview", new { systemEnabled = system, registrationEnabled = registration });
        var preview = await Json(previewResponse); Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        using var commit = await Send(scope.Admin, HttpMethod.Put, $"/api/v1/admin/modules/{scope.Module}/policy", new { systemEnabled = system, registrationEnabled = registration, previewToken = preview.GetProperty("previewToken").GetString() }, preview.GetProperty("etag").GetString()); await Json(commit);
    }
    private sealed record PolicyScope(SqlShoppingRecordTests Tests, SyntheticSession Admin, Guid Module, bool System, bool Registration) : IAsyncDisposable
    { public ValueTask DisposeAsync() => new(Tests.Policy(this, System, Registration)); }
    private async Task<Guid> Seller(SyntheticSession actor, string title = "Synthetic shop", Guid? key = null)
    {
        using var response = await Send(actor, HttpMethod.Post, Root + "/sellers", new { title, url = "https://example.invalid/shop" }, key: key);
        var ack = await Json(response, HttpStatusCode.Created); Assert.True(ack.GetProperty("committed").GetBoolean());
        Assert.False(ack.GetProperty("detailAvailable").GetBoolean()); Assert.False(ack.TryGetProperty("title", out _)); return ack.GetProperty("id").GetGuid();
    }
    private static object OrderBody(Guid? seller, string? number = null, bool confirm = false, object[]? lines = null) => new
    {
        sellerId = seller, orderNumber = number, purchasedOn = "2026-10-25", currency = "USD", shippingAmount = "1", taxAmount = "2", discountAmount = "3",
        notes = "Synthetic owner-only record", confirmDuplicateReference = confirm,
        lines = lines ?? [new { id = (Guid?)null, title = "Synthetic product", quantity = "2", unitAmount = "12.12345678", variant = "Blue" }]
    };
    private async Task<Guid> Order(SyntheticSession actor, Guid? seller, string? number = null)
    {
        using var response = await Send(actor, HttpMethod.Post, Root + "/orders", OrderBody(seller, number));
        return (await Json(response, HttpStatusCode.Created)).GetProperty("id").GetGuid();
    }
    [Fact]
    public async Task Seller_is_owner_scoped_stale_safe_and_opaque_replay_does_not_duplicate()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync(); var key = Guid.NewGuid();
        var id = await Seller(owner, key: key); Assert.Equal(id, await Seller(owner, key: key));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM shopping.Seller WHERE OwnerId=@Owner;", Id("@Owner", owner.OwnerId)));
        using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/sellers/{id}", foreign.RawSessionHandle); Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        var row = await Get(owner, $"{Root}/sellers/{id}"); var etag = row.GetProperty("etag").GetString();
        using var updated = await Send(owner, HttpMethod.Put, $"{Root}/sellers/{id}", new { title = "Updated shop", url = (string?)null }, etag); await Json(updated);
        using var stale = await Send(owner, HttpMethod.Put, $"{Root}/sellers/{id}", new { title = "Stale overwrite", url = (string?)null }, etag); Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        using var reused = await Send(owner, HttpMethod.Post, Root + "/sellers", new { title = "Different body", url = "https://example.invalid/shop" }, key: key); Assert.Equal(HttpStatusCode.Conflict, reused.StatusCode);
    }
    [Fact]
    public async Task Order_keeps_exact_actual_amounts_and_historical_seller_label()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var seller = await Seller(owner, "Original label"); var id = await Order(owner, seller);
        var order = await Get(owner, $"{Root}/orders/{id}"); Assert.Equal("24.24691356", order.GetProperty("totalAmount").GetString()); Assert.Equal("Draft", order.GetProperty("status").GetString());
        var original = await Get(owner, $"{Root}/sellers/{seller}");
        using var renamed = await Send(owner, HttpMethod.Put, $"{Root}/sellers/{seller}", new { title = "Renamed seller", url = (string?)null }, original.GetProperty("etag").GetString()); await Json(renamed);
        Assert.Equal("Original label", (await Get(owner, $"{Root}/orders/{id}")).GetProperty("sellerTitle").GetString());
        var list = await Get(owner, Root + "/orders?page=1"); Assert.Equal("24.24691356", list.GetProperty("items")[0].GetProperty("totalAmount").GetString());
    }
    [Fact]
    public async Task Foreign_seller_and_foreign_order_line_cannot_be_linked()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync(); var seller = await Seller(foreign); var order = await Order(foreign, seller);
        using var invalidOrder = await Send(owner, HttpMethod.Post, Root + "/orders", OrderBody(seller)); Assert.Equal(HttpStatusCode.NotFound, invalidOrder.StatusCode);
        var line = (await Get(foreign, $"{Root}/orders/{order}")).GetProperty("lines")[0].GetProperty("id").GetGuid();
        using var invalidWarranty = await Send(owner, HttpMethod.Post, Root + "/warranties", new { orderLineId = line, title = "Foreign source", coverageKind = "Unknown" }); Assert.Equal(HttpStatusCode.NotFound, invalidWarranty.StatusCode);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM shopping.Warranty WHERE OwnerId=@Owner;", Id("@Owner", owner.OwnerId)));
    }
    [Fact]
    public async Task Duplicate_order_reference_warns_until_owner_explicitly_confirms()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var reference = "Synthetic-" + Guid.NewGuid().ToString("N"); await Order(owner, null, reference);
        using var duplicate = await Send(owner, HttpMethod.Post, Root + "/orders", OrderBody(null, reference)); Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var confirmed = await Send(owner, HttpMethod.Post, Root + "/orders", OrderBody(null, reference, true)); await Json(confirmed, HttpStatusCode.Created);
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM shopping.PurchaseOrder WHERE OwnerId=@Owner;", Id("@Owner", owner.OwnerId)));
    }
    [Fact]
    public async Task Warranty_coverage_is_not_a_fake_date_and_pinned_lines_survive_edits()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var order = await Order(owner, null); var detail = await Get(owner, $"{Root}/orders/{order}"); var line = detail.GetProperty("lines")[0].GetProperty("id").GetGuid();
        using var reversed = await Send(owner, HttpMethod.Post, Root + "/warranties", new { orderLineId = line, title = "Bad date", coverageKind = "Fixed", startsOn = "2026-10-25", endsOn = "2026-10-24" }); Assert.Equal(HttpStatusCode.UnprocessableEntity, reversed.StatusCode);
        using var fakeLifetime = await Send(owner, HttpMethod.Post, Root + "/warranties", new { orderLineId = line, title = "Fake expiry", coverageKind = "Lifetime", startsOn = "2026-10-25", endsOn = "9999-12-31" }); Assert.Equal(HttpStatusCode.UnprocessableEntity, fakeLifetime.StatusCode);
        using var created = await Send(owner, HttpMethod.Post, Root + "/warranties", new { orderLineId = line, title = "Owner lifetime warranty", coverageKind = "Lifetime" }); var warranty = (await Json(created, HttpStatusCode.Created)).GetProperty("id").GetGuid();
        var saved = await Get(owner, $"{Root}/warranties/{warranty}"); Assert.Equal(JsonValueKind.Null, saved.GetProperty("endsOn").ValueKind);
        using var removeLine = await Send(owner, HttpMethod.Put, $"{Root}/orders/{order}", OrderBody(null), detail.GetProperty("etag").GetString()); Assert.Equal(HttpStatusCode.Conflict, removeLine.StatusCode);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM shopping.OrderLine WHERE OwnerId=@Owner AND OrderId=@Id;", Id("@Owner", owner.OwnerId), Id("@Id", order)));
    }
    [Fact]
    public async Task Module_disable_rejects_reads_and_replay_without_private_projection()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var key = Guid.NewGuid(); await Seller(owner, key: key); await Policy(scope, false, false);
        using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/sellers", owner.RawSessionHandle); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var replay = await Send(owner, HttpMethod.Post, Root + "/sellers", new { title = "Synthetic shop", url = "https://example.invalid/shop" }, key: key); Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        Assert.DoesNotContain("Synthetic shop", await replay.Content.ReadAsStringAsync());
    }
}
