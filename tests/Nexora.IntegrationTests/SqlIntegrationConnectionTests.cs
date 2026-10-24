using System.Data;
using System.Net;
using System.Text.Json;
using Nexora.Application.Identity;
using Nexora.Application.Integrations;
using Nexora.Infrastructure.Integrations;
using Nexora.Infrastructure.Persistence;
using Xunit;
namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlIntegrationConnectionTests(SqlApiFixture fixture)
{
    private async Task<SyntheticSession> Owner()
    {
        fixture.RequireAvailable();
        await fixture.SetModuleRuntimeAvailabilityAsync("FX35", true);
        var session = await fixture.CreateActiveSessionAsync();
        await fixture.SetModuleEnabledAsync(session.UserId, "FX35", true);
        return session;
    }
    private static Microsoft.Data.SqlClient.SqlParameter Parameter(string name, SqlDbType type, object value) => new(name, type) { Value = value };
    private SqlIntegrationConnectionService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-connection-receipt");
    private static IdentityPrincipal Actor(SyntheticSession session) => new(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow, session.SessionId);
    private static IntegrationConnectionCommand Body(string title) => new(1, "n8n", 1, title, "https://synthetic.example.com/");
    [Fact]
    public async Task Configuration_persists_pages_and_same_key_replay_rechecks_current_owner_and_authority()
    {
        var session = await Owner(); var actor = Actor(session); var service = Service();
        try
        {
            var keys = new List<string>(); var ids = new List<Guid>();
            for (var i = 0; i < 27; i++)
            { var key = Guid.NewGuid().ToString("D"); var saved = service.Create(actor, Body("Connection " + i.ToString("D2")), key, null); Assert.True(saved.Succeeded, saved.Code); keys.Add(key); ids.Add(saved.Value!.Id); }
            var page = service.List(actor, null, null, "n8n", "Disabled"); Assert.True(page.Succeeded); Assert.Equal(25, page.Value!.Items.Count); Assert.NotNull(page.Value.NextCursor);
            var next = service.List(actor, page.Value.NextCursor, null, "n8n", "Disabled"); Assert.True(next.Succeeded); Assert.Equal(2, next.Value!.Items.Count); Assert.Null(next.Value.NextCursor);
            Assert.Equal(ids.Order(), page.Value.Items.Concat(next.Value.Items).Select(x => x.Id).Order());
            Assert.All(page.Value.Items, x => { Assert.Equal("Disabled", x.State); Assert.Equal("Unconfigured", x.AuthMode); Assert.False(x.RealOperationsAvailable); Assert.Null(x.LastTestAt); });
            Assert.False(service.List(actor, page.Value.NextCursor, "changed", "n8n", "Disabled").Succeeded);
            var foreign = await fixture.CreateActiveSessionAsync();
            Assert.Equal(404, service.Get(Actor(foreign), ids[0]).StatusCode);
            Assert.False(service.List(Actor(foreign), page.Value.NextCursor, null, "n8n", "Disabled").Succeeded);
            var original = service.Get(actor, ids[0]).Value!; var beforeAudit = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AuditEvent WHERE ActionKey='integrations.connection.create';");
            var replay = service.Create(actor, Body("Connection 00"), keys[0], null); Assert.True(replay.Succeeded); Assert.Equal(ids[0], replay.Value!.Id);
            Assert.Equal(beforeAudit, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AuditEvent WHERE ActionKey='integrations.connection.create';"));
            Assert.Equal(original.ETag, service.Get(actor, ids[0]).Value!.ETag);
            Assert.Equal("IdempotencyConflict", service.Create(actor, Body("different"), keys[0], null).Code);
            var updateKey = Guid.NewGuid().ToString("D"); Assert.True(service.Update(actor, ids[0], original.ETag, Body("Renamed"), updateKey, null).Succeeded);
            Assert.Equal("RevisionConflict", service.Update(actor, ids[0], original.ETag, Body("Stale"), Guid.NewGuid().ToString("D"), null).Code);
            Assert.True(service.Update(actor, ids[0], original.ETag, Body("Renamed"), updateKey, null).Succeeded);
            var current = service.Get(actor, ids[0]).Value!;
            var disableKey = Guid.NewGuid().ToString("D"); Assert.True(service.Disable(actor, ids[0], current.ETag, new(true), disableKey, null).Succeeded);
            var disabled = service.Get(actor, ids[0]).Value!; Assert.NotEqual(current.ETag, disabled.ETag);
            Assert.True(service.Disable(actor, ids[0], current.ETag, new(true), disableKey, null).Succeeded); Assert.Equal(disabled.ETag, service.Get(actor, ids[0]).Value!.ETag);
            await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Id;", Parameter("@Id", SqlDbType.UniqueIdentifier, session.SessionId));
            Assert.False(service.Create(actor, Body("Connection 00"), keys[0], null).Succeeded);
        }
        finally { await fixture.SetModuleRuntimeAvailabilityAsync("FX35", false); }
    }
    [Fact]
    public async Task API_commands_require_CSRF_owner_revision_and_reject_unapproved_fields()
    {
        var session = await Owner();
        try
        {
            var csrf = await fixture.GetCsrfAsync();
            using var saved = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/integrations/connections/", Body("API connection"), csrf, Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
            using var ack = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
            Assert.Single(ack.RootElement.EnumerateObject()); var id = ack.RootElement.GetProperty("id").GetGuid();
            using var detail = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/integrations/connections/" + id, session.RawSessionHandle); Assert.Equal(HttpStatusCode.OK, detail.StatusCode); Assert.NotNull(detail.Headers.ETag);
            using var wrongField = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/integrations/connections/", new { schemaVersion=1, providerKey="n8n", connectorVersion=1, title="rejected", endpoint="https://synthetic.example.com/", ownerId=session.OwnerId }, csrf, Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.BadRequest, wrongField.StatusCode);
            using var unsafeOrigin = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/integrations/connections/", Body("rejected") with { Endpoint="https://synthetic.example.com/secret/.." }, csrf, Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, unsafeOrigin.StatusCode);
            using var noRevision = await fixture.SendJsonAsync(HttpMethod.Put, "/api/v1/integrations/connections/" + id, Body("missing revision"), csrf, Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.PreconditionRequired, noRevision.StatusCode);
            using var noConfirmation = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/integrations/connections/" + id + "/disable", new IntegrationDisableCommand(false), csrf, Guid.NewGuid(), session.RawSessionHandle, detail.Headers.ETag!.ToString());
            Assert.Equal(HttpStatusCode.UnprocessableEntity, noConfirmation.StatusCode);
            using var csrfRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/integrations/connections/" + id + "/disable") { Content=System.Net.Http.Json.JsonContent.Create(new IntegrationDisableCommand(true)) };
            csrfRequest.Headers.TryAddWithoutValidation("Cookie", "__Host-NexoraSession=" + session.RawSessionHandle);
            csrfRequest.Headers.TryAddWithoutValidation("If-Match", detail.Headers.ETag!.ToString());
            csrfRequest.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString("D"));
            using var noCsrf = await fixture.Client.SendAsync(csrfRequest);
            Assert.Equal(HttpStatusCode.Forbidden, noCsrf.StatusCode);
            using var csrfError = JsonDocument.Parse(await noCsrf.Content.ReadAsStringAsync()); Assert.Equal("CsrfInvalid", csrfError.RootElement.GetProperty("code").GetString());
            using var noEffectRoute = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/integrations/connections/" + id + "/test", new { }, csrf, Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.NotFound, noEffectRoute.StatusCode);
        }
        finally { await fixture.SetModuleRuntimeAvailabilityAsync("FX35", false); }
    }
    [Fact]
    public async Task Audit_failure_rolls_back_connection_resource_and_receipt()
    {
        var session = await Owner();
        try
        {
            await fixture.ExecuteAsync("CREATE TRIGGER security.RejectConnectionAudit ON security.AuditEvent AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='integrations.connection.create') THROW 51055,'Synthetic connection audit failure',1; END;");
            var result = Service().Create(Actor(session), Body("Rollback"), Guid.NewGuid().ToString("D"), null);
            Assert.False(result.Succeeded); Assert.Equal("StorageUnavailable", result.Code);
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM automation.Connection WHERE OwnerId=@Owner;", Parameter("@Owner", SqlDbType.UniqueIdentifier, session.OwnerId)));
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM platform.Resource WHERE OwnerId=@Owner;", Parameter("@Owner", SqlDbType.UniqueIdentifier, session.OwnerId)));
        }
        finally
        { await fixture.ExecuteAsync("DROP TRIGGER IF EXISTS security.RejectConnectionAudit;"); await fixture.SetModuleRuntimeAvailabilityAsync("FX35", false); }
    }
}
