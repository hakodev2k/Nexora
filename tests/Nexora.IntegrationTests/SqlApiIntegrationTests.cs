using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlApiIntegrationTests
{
    private readonly SqlApiFixture _fixture;

    public SqlApiIntegrationTests(SqlApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Real_SQL_migration_upgrade_and_readiness_gate_are_available()
    {
        _fixture.RequireAvailable();

        Assert.NotNull(_fixture.ReadinessBeforeM01Completion);
        Assert.Equal("NotReady", _fixture.ReadinessBeforeM01Completion!.Status);
        Assert.Equal("MissingRequired", _fixture.ReadinessBeforeM01Completion.Dependencies["requiredMigrations"]);

        Assert.NotNull(_fixture.ReadinessAfterM01Migration);
        Assert.Equal("NotReady", _fixture.ReadinessAfterM01Migration!.Status);
        Assert.Equal("Ready", _fixture.ReadinessAfterM01Migration.Dependencies["requiredMigrations"]);

        Assert.NotNull(_fixture.ReadinessAfterBootstrap);
        Assert.True(_fixture.ReadinessAfterBootstrap!.Ready);
        Assert.Equal("Ready", _fixture.ReadinessAfterBootstrap.Status);
    }

    [Fact]
    public async Task Anonymous_CSRF_registration_replay_has_one_effect_and_changed_body_conflicts()
    {
        _fixture.RequireAvailable();
        var csrf = await _fixture.GetCsrfAsync();
        var idempotencyKey = Guid.NewGuid();
        var email = "register-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var password = "M01-" + Guid.NewGuid().ToString("N") + "-aA!";
        var accepted = new
        {
            email,
            password,
            timeZoneId = "Asia/Ho_Chi_Minh",
            displayName = "Synthetic registration"
        };

        using var first = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/auth/registrations",
            accepted,
            csrf,
            idempotencyKey);
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);

        using var replay = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/auth/registrations",
            accepted,
            csrf,
            idempotencyKey);
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);

        using var changedBody = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/auth/registrations",
            new
            {
                email,
                password,
                timeZoneId = "Asia/Ho_Chi_Minh",
                displayName = "Different synthetic display"
            },
            csrf,
            idempotencyKey);
        Assert.Equal(HttpStatusCode.Conflict, changedBody.StatusCode);
        await AssertProblemCodeAsync(changedBody, "IdempotencyConflict");

        var count = await _fixture.ScalarIntAsync(
            "SELECT COUNT(*) FROM [identity].[User] WHERE [NormalizedEmail] = @email;",
            Parameter("@email", SqlDbType.NVarChar, email, 320));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Current_namespace_receipt_replays_original_safe_result_and_rechecks_authority()
    {
        _fixture.RequireAvailable();
        var session = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using var current = await _fixture.GetMeAsync(session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        var etag = current.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));

        var idempotencyKey = Guid.NewGuid();
        const string firstDisplay = "Receipt-safe display";
        var patch = new
        {
            displayName = firstDisplay,
            timeZoneId = (string?)null,
            locale = (string?)null
        };

        using var first = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            "/api/v1/me",
            patch,
            csrf,
            idempotencyKey,
            session.RawSessionHandle,
            etag);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await AssertDisplayNameAsync(first, firstDisplay);

        var versionAfterEffect = await _fixture.ScalarBytesAsync(
            "SELECT [RowVersion] FROM [identity].[User] WHERE [Id] = @id;",
            Parameter("@id", SqlDbType.UniqueIdentifier, session.UserId));
        await _fixture.MoveProfileReceiptToCurrentNamespaceAsync(session.UserId, idempotencyKey);

        using var replay = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            "/api/v1/me",
            patch,
            csrf,
            idempotencyKey,
            session.RawSessionHandle,
            etag);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        await AssertDisplayNameAsync(replay, firstDisplay);

        var versionAfterReplay = await _fixture.ScalarBytesAsync(
            "SELECT [RowVersion] FROM [identity].[User] WHERE [Id] = @id;",
            Parameter("@id", SqlDbType.UniqueIdentifier, session.UserId));
        Assert.Equal(versionAfterEffect, versionAfterReplay);

        using var changedBody = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            "/api/v1/me",
            new
            {
                displayName = "Different receipt body",
                timeZoneId = (string?)null,
                locale = (string?)null
            },
            csrf,
            idempotencyKey,
            session.RawSessionHandle,
            etag);
        Assert.Equal(HttpStatusCode.Conflict, changedBody.StatusCode);
        await AssertProblemCodeAsync(changedBody, "IdempotencyConflict");

        await _fixture.ExecuteAsync(
            "UPDATE [identity].[User] SET [State] = 'Disabled' WHERE [Id] = @id;",
            Parameter("@id", SqlDbType.UniqueIdentifier, session.UserId));

        using var disabledReplay = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            "/api/v1/me",
            patch,
            csrf,
            idempotencyKey,
            session.RawSessionHandle,
            etag);
        Assert.Equal(HttpStatusCode.Forbidden, disabledReplay.StatusCode);
    }

    private static async Task AssertDisplayNameAsync(HttpResponseMessage response, string expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty("displayName").GetString());
    }

    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty("code").GetString());
    }

    private static SqlParameter Parameter(string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
        parameter.Value = value;
        return parameter;
    }
}
