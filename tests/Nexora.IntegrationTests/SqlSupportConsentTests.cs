using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlSupportConsentTests(SqlApiFixture fixture)
{
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };

    // FX05 is intentionally Blocked. Never manufacture Ready status/grants.
    // The attempted positive case is retained in support-before.trx and marked
    // BLOCKED in the worker report; this negative gate is not replay coverage.
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task Blocked_support_module_denies_owner_metadata_and_mutations_without_effects(string role)
    {
        fixture.RequireAvailable();
        var actor = await fixture.CreateActiveSessionAsync(role);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM platform.Module WHERE Code='FX05' AND State='Blocked' AND SystemEnabled=0 AND RegistrationEnabled=0;"));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM platform.UserModuleGrant g JOIN platform.Module m ON m.Id=g.ModuleId WHERE g.UserId=@Actor AND m.Code='FX05' AND g.Enabled=1;", Id("@Actor", actor.UserId)));
        var receiptsBefore = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt] WHERE OperationKey IN ('support.consent.grant','support.consent.revoke','support.session.end');");
        foreach (var path in new[] { "/api/v1/support/grants", "/api/v1/support/sessions" })
        {
            using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, actor.RawSessionHandle);
            await AssertUnavailable(response);
        }
        var key = Guid.NewGuid();
        var command = new { moduleCode = "FX11", durationMode = "24Hours", expiresAt = (DateTimeOffset?)null };
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var grant = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/support/grants", command, await fixture.GetCsrfAsync(), key, actor.RawSessionHandle);
            await AssertUnavailable(grant);
        }
        var etag = "\"" + Convert.ToBase64String(new byte[8]) + "\"";
        foreach (var kind in new[] { "grants", "sessions" })
        {
            using var response = await fixture.SendJsonAsync(HttpMethod.Delete, $"/api/v1/support/{kind}/{Guid.NewGuid()}", new { }, await fixture.GetCsrfAsync(), Guid.NewGuid(), actor.RawSessionHandle, etag);
            await AssertUnavailable(response);
        }
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.SupportGrant WHERE OwnerId=@Owner;", Id("@Owner", actor.OwnerId)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AccessSession WHERE OwnerId=@Owner OR ActorUserId=@Actor;", Id("@Owner", actor.OwnerId), Id("@Actor", actor.UserId)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AuditEvent WHERE ActorUserId=@Actor AND ActionKey IN ('support.consent.grant','support.consent.revoke','support.session.end');", Id("@Actor", actor.UserId)));
        Assert.Equal(receiptsBefore, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt] WHERE OperationKey IN ('support.consent.grant','support.consent.revoke','support.session.end');"));
    }

    private static async Task AssertUnavailable(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ModuleUnavailable", body.RootElement.GetProperty("code").GetString());
        Assert.False(body.RootElement.TryGetProperty("items", out _));
        Assert.False(body.RootElement.TryGetProperty("id", out _));
        Assert.False(body.RootElement.TryGetProperty("committed", out _));
    }
}
