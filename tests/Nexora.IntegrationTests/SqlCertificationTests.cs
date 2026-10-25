using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlCertificationTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/learning/certifications";
    private static object Body(string title = "Synthetic certificate") => new { title, issuer = "Synthetic issuer", issuedOn = "2025-01-01", expiresOn = "2026-01-01", verificationUrl = "https://example.invalid/verification" };
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var content = await response.Content.ReadAsStringAsync(); Assert.True(response.StatusCode == expected, $"Expected {expected}, received {response.StatusCode}: {content}");
        using var doc = JsonDocument.Parse(content); return doc.RootElement.Clone();
    }
    private async Task<HttpResponseMessage> Send(SyntheticSession actor, HttpMethod method, string path, object body, string? etag = null, Guid? key = null) =>
        await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), actor.RawSessionHandle, etag);
    private async Task<JsonElement> Get(SyntheticSession actor, string path)
    { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, actor.RawSessionHandle); return await Json(response); }
    private async Task<Guid> Create(SyntheticSession actor, string title = "Synthetic certificate")
    { using var response = await Send(actor, HttpMethod.Post, Root, Body(title)); var ack = await Json(response, HttpStatusCode.Created); Assert.True(ack.GetProperty("committed").GetBoolean()); Assert.False(ack.GetProperty("detailAvailable").GetBoolean()); Assert.False(ack.TryGetProperty("title", out _)); return ack.GetProperty("id").GetGuid(); }
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var admin = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(admin, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(row => row.GetProperty("code").GetString() == "FX40");
        var scope = new PolicyScope(this, admin, module.GetProperty("id").GetGuid(), module.GetProperty("systemEnabled").GetBoolean(), module.GetProperty("registrationEnabled").GetBoolean());
        await SetPolicy(scope, true, true); return scope;
    }
    private async Task SetPolicy(PolicyScope scope, bool system, bool registration)
    {
        var module = (await Get(scope.Admin, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == scope.Module);
        if (module.GetProperty("systemEnabled").GetBoolean() == system && module.GetProperty("registrationEnabled").GetBoolean() == registration) return;
        using var previewResponse = await Send(scope.Admin, HttpMethod.Post, $"/api/v1/admin/modules/{scope.Module}/preview", new { systemEnabled = system, registrationEnabled = registration });
        var preview = await Json(previewResponse); Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        using var commit = await Send(scope.Admin, HttpMethod.Put, $"/api/v1/admin/modules/{scope.Module}/policy", new { systemEnabled = system, registrationEnabled = registration, previewToken = preview.GetProperty("previewToken").GetString() }, preview.GetProperty("etag").GetString()); await Json(commit);
    }
    private sealed record PolicyScope(SqlCertificationTests Tests, SyntheticSession Admin, Guid Module, bool System, bool Registration) : IAsyncDisposable
    { public ValueTask DisposeAsync() => new(Tests.SetPolicy(this, System, Registration)); }

    [Fact]
    public async Task SQL_rejects_archived_certification_with_missing_prior_state()
    {
        await using var policy = await Enable(); var actor = await fixture.CreateActiveSessionAsync(); var id = await Create(actor);
        var error = await Assert.ThrowsAsync<SqlException>(() => fixture.ExecuteAsync(
            "UPDATE [learning].[Certification] SET Status='Archived',PreArchiveState=NULL WHERE OwnerId=@Owner AND Id=@Id;",
            new SqlParameter("@Owner", SqlDbType.UniqueIdentifier) { Value = actor.OwnerId },
            new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id }));
        Assert.Equal(547, error.Number);
        Assert.Equal("Active", (await Get(actor, $"{Root}/{id}")).GetProperty("status").GetString());
    }
    [Fact]
    public async Task Renewal_retains_original_expiry_history_and_same_command_replay_is_opaque_and_unique()
    {
        await using var policy = await Enable(); var actor = await fixture.CreateActiveSessionAsync(); var id = await Create(actor);
        var first = await Get(actor, $"{Root}/{id}"); var tag = first.GetProperty("etag").GetString(); var key = Guid.NewGuid();
        var body = new { issuedOn = "2026-01-01", expiresOn = "2027-01-01", reason = "Synthetic renewal" };
        using (var response = await Send(actor, HttpMethod.Post, $"{Root}/{id}/renew", body, tag, key)) Assert.Equal(id, (await Json(response)).GetProperty("id").GetGuid());
        using (var response = await Send(actor, HttpMethod.Post, $"{Root}/{id}/renew", body, tag, key))
        { var ack = await Json(response); Assert.Equal(id, ack.GetProperty("id").GetGuid()); Assert.False(ack.TryGetProperty("expiresOn", out _)); }
        var history = (await Get(actor, $"{Root}/{id}/history")).GetProperty("items");
        Assert.Equal(2, history.GetArrayLength()); Assert.Equal("2", history[0].GetProperty("versionNumber").GetString());
        Assert.Equal("2027-01-01", history[0].GetProperty("snapshot").GetProperty("expiresOn").GetString());
        Assert.Equal("2026-01-01", history[1].GetProperty("snapshot").GetProperty("expiresOn").GetString());
        using var stale = await Send(actor, HttpMethod.Put, $"{Root}/{id}", new { title = "Stale update", issuer = "Issuer" }, tag);
        await Json(stale, HttpStatusCode.PreconditionFailed);
    }
    [Fact]
    public async Task Foreign_owner_and_current_module_revocation_never_read_or_replay_private_certifications()
    {
        await using var policy = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var key = Guid.NewGuid(); using var created = await Send(owner, HttpMethod.Post, Root, Body(), key: key); var id = (await Json(created, HttpStatusCode.Created)).GetProperty("id").GetGuid();
        using (var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}", foreign.RawSessionHandle)) await Json(response, HttpStatusCode.NotFound);
        using (var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{Root}/{id}/history", foreign.RawSessionHandle)) await Json(response, HttpStatusCode.NotFound);
        await fixture.SetModuleEnabledAsync(owner.UserId, "FX40", false);
        using var replay = await Send(owner, HttpMethod.Post, Root, Body(), key: key); await Json(replay, HttpStatusCode.Forbidden);
    }
    [Fact]
    public async Task Archive_preview_is_revision_bound_and_archived_certificate_cannot_be_edited_or_renewed()
    {
        await using var policy = await Enable(); var actor = await fixture.CreateActiveSessionAsync(); var id = await Create(actor);
        var first = await Get(actor, $"{Root}/{id}"); var tag = first.GetProperty("etag").GetString();
        using var previewResponse = await Send(actor, HttpMethod.Post, $"{Root}/{id}/preview-archive", new { }); var preview = await Json(previewResponse);
        var body = new { confirm = true, expiresAt = preview.GetProperty("expiresAt").GetString(), previewToken = preview.GetProperty("previewToken").GetString() };
        using (var changed = await Send(actor, HttpMethod.Put, $"{Root}/{id}", new { title = "Updated certificate", issuer = "Issuer" }, tag)) await Json(changed);
        using (var stale = await Send(actor, HttpMethod.Post, $"{Root}/{id}/archive", body, tag)) await Json(stale, HttpStatusCode.PreconditionFailed);
        var current = await Get(actor, $"{Root}/{id}");
        using var freshResponse = await Send(actor, HttpMethod.Post, $"{Root}/{id}/preview-archive", new { }); var fresh = await Json(freshResponse);
        using (var archive = await Send(actor, HttpMethod.Post, $"{Root}/{id}/archive", new { confirm = true, expiresAt = fresh.GetProperty("expiresAt").GetString(), previewToken = fresh.GetProperty("previewToken").GetString() }, current.GetProperty("etag").GetString())) await Json(archive);
        var archived = await Get(actor, $"{Root}/{id}"); Assert.Equal("Archived", archived.GetProperty("status").GetString());
        using var renew = await Send(actor, HttpMethod.Post, $"{Root}/{id}/renew", new { issuedOn = "2026-01-01", expiresOn = "2027-01-01" }, archived.GetProperty("etag").GetString()); await Json(renew, HttpStatusCode.Conflict);
    }
}
