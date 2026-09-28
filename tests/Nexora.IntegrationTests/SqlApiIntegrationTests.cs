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
    public async Task Local_background_workers_claim_an_empty_synthetic_queue_without_error()
    {
        _fixture.RequireAvailable();

        var result = await _fixture.ProcessPendingBackgroundWorkAsync();

        Assert.Equal(0, result.FileCleanups);
        Assert.Equal(0, result.Reminders.Dispatched);
        Assert.Equal(0, result.Reminders.Missed);
        Assert.Equal(0, result.Reminders.Canceled);
        Assert.Equal(0, result.Reminders.Deferred);
    }

    [Fact]
    public async Task Release_endpoint_groups_materialize_before_authentication()
    {
        _fixture.RequireAvailable();
        var endpoints = new[]
        {
            "/api/v1/dashboard",
            "/api/v1/projects",
            "/api/v1/tasks",
            "/api/v1/calendar/events",
            "/api/v1/planner",
            "/api/v1/habits",
            "/api/v1/goals",
            "/api/v1/organization/tags",
            "/api/v1/settings/preferences",
            "/api/v1/notifications",
            "/api/v1/favorites",
            "/api/v1/search?q=synthetic",
            "/api/v1/bookmarks",
            "/api/v1/read-later",
            "/api/v1/finance/categories",
            "/api/v1/snippets",
            "/api/v1/documents",
            "/api/v1/sharing/links",
            "/api/v1/support/grants",
            "/api/v1/developer/tools",
            "/api/v1/files",
            "/api/v1/trash",
            "/api/v1/reminders/Task/00000000-0000-0000-0000-000000000001"
        };

        foreach (var endpoint in endpoints)
        {
            using var response = await _fixture.Client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task User_productivity_workflow_is_idempotent_projected_and_owner_scoped()
    {
        _fixture.RequireAvailable();
        var owner = await _fixture.CreateActiveSessionAsync();
        var otherUser = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();
        var start = DateTimeOffset.UtcNow.AddDays(2).AddHours(9);
        var end = start.AddHours(2);
        var projectRequest = new
        {
            name = "Integration project",
            description = "A realistic owner-scoped project for API verification.",
            startAt = start,
            endAt = end,
            priority = "P1",
            tagsJson = "[]",
            notes = "Synthetic only",
            confirmTaskBounds = false
        };
        var projectKey = Guid.NewGuid();

        using var createdProject = await _fixture.SendJsonAsync(
            HttpMethod.Post, "/api/v1/projects", projectRequest, csrf, projectKey, owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdProject.StatusCode);
        var projectId = await ReadIdAsync(createdProject);

        using var replayedProject = await _fixture.SendJsonAsync(
            HttpMethod.Post, "/api/v1/projects", projectRequest, csrf, projectKey, owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, replayedProject.StatusCode);
        Assert.Equal(projectId, await ReadIdAsync(replayedProject));

        using var createdTask = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/tasks",
            new
            {
                projectId,
                title = "Verify projected calendar event",
                description = "Task body",
                status = "NotStarted",
                dueAt = end,
                startAt = start.AddMinutes(15),
                endAt = end.AddMinutes(-15),
                priority = "P2",
                tagsJson = "[]",
                acceptanceCriteriaJson = "[]",
                rank = 0,
                reminderAt = (DateTimeOffset?)null,
                confirmProjectTimeBounds = false,
                manageReminder = false
            },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdTask.StatusCode);
        var taskId = await ReadIdAsync(createdTask);

        using var calendar = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/calendar/events", owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, calendar.StatusCode);
        using (var calendarDocument = JsonDocument.Parse(await calendar.Content.ReadAsStringAsync()))
        {
            Assert.Contains(calendarDocument.RootElement.GetProperty("items").EnumerateArray(),
                item => item.GetProperty("taskId").GetGuid() == taskId && item.GetProperty("sourceKind").GetString() == "Task");
        }

        using var ownSearch = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/search?q=Integration", owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, ownSearch.StatusCode);

        using var foreignRead = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/projects/{projectId}", otherUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreignRead.StatusCode);

        using var foreignSearch = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/search?q=Integration", otherUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, foreignSearch.StatusCode);
        using var foreignSearchDocument = JsonDocument.Parse(await foreignSearch.Content.ReadAsStringAsync());
        Assert.DoesNotContain(foreignSearchDocument.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetGuid() == projectId);
    }

    [Fact]
    public async Task SuperAdmin_role_change_requires_an_explicit_admin_self_grant()
    {
        _fixture.RequireAvailable();
        var superAdmin = await _fixture.CreateActiveSessionAsync("SuperAdmin");
        var target = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using var initialAccess = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, $"/api/v1/admin/users/{target.UserId}/access", superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, initialAccess.StatusCode);
        var initialEtag = initialAccess.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(initialEtag));

        using var roleUpdate = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{target.UserId}/role",
            new { role = "Admin", ifMatch = initialEtag },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, roleUpdate.StatusCode);
        var roleEtag = roleUpdate.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(roleEtag));

        using var revoked = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/settings/preferences", target.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);

        var admin = await _fixture.CreateActiveSessionForUserAsync(target.UserId);
        using var noGrant = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/settings/preferences", admin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Forbidden, noGrant.StatusCode);

        using var grantUpdate = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{target.UserId}/permissions",
            new { actionKey = "settings.preference.read", effect = "Allow", ifMatch = roleEtag },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, grantUpdate.StatusCode);

        using var allowed = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/settings/preferences", admin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
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

    private static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
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
