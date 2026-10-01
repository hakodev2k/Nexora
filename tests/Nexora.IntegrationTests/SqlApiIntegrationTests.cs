using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
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
    public async Task Real_SQL_migration_upgrade_and_readiness_gate_are_available()
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
        Assert.Equal(4, await _fixture.ScalarIntAsync("""
            SELECT COUNT(*) FROM [platform].[Permission]
            WHERE ActionKey IN ('projects.project.restore','projects.project.purge','tasks.task.restore','tasks.task.purge')
              AND EffectiveStatus = 'Resolved';
            """));
    }

    [Fact]
    public async Task Unknown_IANA_profile_timezone_is_rejected_without_a_SQL_write()
    {
        _fixture.RequireAvailable();
        var session = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();
        using var current = await _fixture.GetMeAsync(session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        var before = await _fixture.ScalarBytesAsync(
            "SELECT [RowVersion] FROM [identity].[User] WHERE [Id] = @id;",
            Parameter("@id", SqlDbType.UniqueIdentifier, session.UserId));
        using var rejected = await _fixture.SendJsonAsync(HttpMethod.Patch, "/api/v1/me",
            new { displayName = "Must not persist", timeZoneId = "Invalid/TimeZone", locale = "en" },
            csrf, Guid.NewGuid(), session.RawSessionHandle, current.Headers.ETag?.Tag);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        await AssertProblemCodeAsync(rejected, "ValidationFailed");
        Assert.Equal(before, await _fixture.ScalarBytesAsync(
            "SELECT [RowVersion] FROM [identity].[User] WHERE [Id] = @id;",
            Parameter("@id", SqlDbType.UniqueIdentifier, session.UserId)));
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
    public async Task SuperAdmin_access_preview_binds_the_exact_change_before_role_and_permission_commit()
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

        using var rolePreview = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/admin/users/{target.UserId}/access/preview",
            new { kind = "role", role = "Admin" },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, rolePreview.StatusCode);
        var rolePreviewToken = await ReadStringPropertyAsync(rolePreview, "previewToken");
        Assert.False(string.IsNullOrWhiteSpace(rolePreviewToken));

        using var roleUpdate = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{target.UserId}/access/role",
            new { kind = "role", role = "Admin", previewToken = rolePreviewToken },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle,
            initialEtag);
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

        using var permissionPreview = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/admin/users/{target.UserId}/access/preview",
            new { kind = "permissions", changes = new[] { new { actionKey = "settings.preference.read", effect = "Allow" } } },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, permissionPreview.StatusCode);
        var permissionPreviewToken = await ReadStringPropertyAsync(permissionPreview, "previewToken");
        Assert.False(string.IsNullOrWhiteSpace(permissionPreviewToken));

        using var grantUpdate = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{target.UserId}/access/permissions",
            new { kind = "permissions", changes = new[] { new { actionKey = "settings.preference.read", effect = "Allow" } }, previewToken = permissionPreviewToken },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle,
            roleEtag);
        Assert.Equal(HttpStatusCode.OK, grantUpdate.StatusCode);

        using var allowed = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/settings/preferences", admin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Access_commit_rejects_a_preview_token_when_the_change_body_differs()
    {
        _fixture.RequireAvailable();
        var superAdmin = await _fixture.CreateActiveSessionAsync("SuperAdmin");
        var target = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using var initialAccess = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, $"/api/v1/admin/users/{target.UserId}/access", superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, initialAccess.StatusCode);
        var etag = initialAccess.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));

        var previewChanges = new[] { new { actionKey = "access.user.read", effect = "Allow" } };
        using var preview = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/admin/users/{target.UserId}/access/preview",
            new { kind = "permissions", changes = previewChanges },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var previewToken = await ReadStringPropertyAsync(preview, "previewToken");
        Assert.False(string.IsNullOrWhiteSpace(previewToken));

        using var changedCommit = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{target.UserId}/access/permissions",
            new
            {
                kind = "permissions",
                changes = new[] { new { actionKey = "settings.preference.read", effect = "Allow" } },
                previewToken
            },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle,
            etag);
        Assert.Equal(HttpStatusCode.Conflict, changedCommit.StatusCode);
        await AssertProblemCodeAsync(changedCommit, "PreviewStale");

        Assert.Equal(0, await _fixture.ScalarIntAsync(
            """
            SELECT COUNT(*)
            FROM [platform].[AdminPermission] ap
            INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
            WHERE ap.[UserId] = @userId AND p.[ActionKey] IN ('access.user.read', 'settings.preference.read');
            """,
            Parameter("@userId", SqlDbType.UniqueIdentifier, target.UserId)));
    }

    [Fact]
    public async Task Admin_cannot_read_or_change_SUPER_only_access_even_after_an_explicit_self_grant()
    {
        _fixture.RequireAvailable();
        var superAdmin = await _fixture.CreateActiveSessionAsync("SuperAdmin");
        var admin = await _fixture.CreateActiveSessionAsync("Admin");
        var ordinaryUser = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using var initialAdminAccess = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, $"/api/v1/admin/users/{admin.UserId}/access", superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, initialAdminAccess.StatusCode);
        var adminEtag = initialAdminAccess.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(adminEtag));

        using var grantReadPreview = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/admin/users/{admin.UserId}/access/preview",
            new { kind = "permissions", changes = new[] { new { actionKey = "access.user.read", effect = "Allow" } } },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, grantReadPreview.StatusCode);
        var grantReadPreviewToken = await ReadStringPropertyAsync(grantReadPreview, "previewToken");
        Assert.False(string.IsNullOrWhiteSpace(grantReadPreviewToken));

        using var grantRead = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/admin/users/{admin.UserId}/access/permissions",
            new { kind = "permissions", changes = new[] { new { actionKey = "access.user.read", effect = "Allow" } }, previewToken = grantReadPreviewToken },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle,
            adminEtag);
        Assert.Equal(HttpStatusCode.OK, grantRead.StatusCode);

        using var ordinaryList = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/admin/users", ordinaryUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Forbidden, ordinaryList.StatusCode);

        using var ordinaryProfile = await _fixture.GetMeAsync(ordinaryUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, ordinaryProfile.StatusCode);
        Assert.False(await ReadBooleanAsync(ordinaryProfile, "canViewAdminAccess"));

        var refreshedAdminSession = await _fixture.CreateActiveSessionForUserAsync(admin.UserId);
        using var adminProfile = await _fixture.GetMeAsync(refreshedAdminSession.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, adminProfile.StatusCode);
        Assert.False(await ReadBooleanAsync(adminProfile, "canViewAdminAccess"));

        using var allowedMetadataList = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, "/api/v1/admin/users", refreshedAdminSession.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, allowedMetadataList.StatusCode);

        using var deniedDetail = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get, $"/api/v1/admin/users/{ordinaryUser.UserId}/access", refreshedAdminSession.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Forbidden, deniedDetail.StatusCode);

        using var deniedMutation = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/admin/users/{ordinaryUser.UserId}/access/preview",
            new { kind = "role", role = "Admin" },
            csrf,
            Guid.NewGuid(),
            refreshedAdminSession.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Forbidden, deniedMutation.StatusCode);
        await AssertProblemCodeAsync(deniedMutation, "PermissionDenied");

        Assert.Equal(1, await _fixture.ScalarIntAsync(
            """
            SELECT COUNT(*)
            FROM [identity].[UserRole] ur
            INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
            WHERE ur.[UserId] = @userId AND r.[Code] = 'User';
            """,
            Parameter("@userId", SqlDbType.UniqueIdentifier, ordinaryUser.UserId)));
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

    [Fact]
    public async Task User_content_libraries_finance_and_organization_workflows_are_owner_scoped_and_revision_safe()
    {
        _fixture.RequireAvailable();
        var owner = await _fixture.CreateActiveSessionAsync();
        var otherUser = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using var unsafeDocument = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/documents",
            new { title = "Unsafe synthetic document", documentType = "Note", editorMode = "Markdown", body = "<script>alert('x')</script>" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsafeDocument.StatusCode);
        await AssertProblemCodeAsync(unsafeDocument, "UnsafeMarkup");

        using var createdDocument = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/documents",
            new { title = "Owner-only research notes", documentType = "Note", editorMode = "Markdown", body = "Synthetic, local-only note body." },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdDocument.StatusCode);
        var document = await ReadResourceAsync(createdDocument);

        using var savedDocument = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/documents/{document.Id}",
            new { title = "Owner-only research notes", body = "Updated synthetic body.", changeNote = "Checked revision behavior" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            document.ETag);
        Assert.Equal(HttpStatusCode.OK, savedDocument.StatusCode);
        var savedDocumentResource = await ReadResourceAsync(savedDocument);

        using var staleDocumentSave = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/documents/{document.Id}",
            new { title = "Stale write", body = "This must not replace the current document.", changeNote = "Stale" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            document.ETag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleDocumentSave.StatusCode);
        await AssertProblemCodeAsync(staleDocumentSave, "RevisionConflict");

        using var archivedDocument = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/documents/{document.Id}/transition",
            new { status = "Archived" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            savedDocumentResource.ETag);
        Assert.Equal(HttpStatusCode.OK, archivedDocument.StatusCode);
        await AssertStringPropertyAsync(archivedDocument, "status", "Archived");

        using var foreignDocument = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/documents/{document.Id}",
            otherUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreignDocument.StatusCode);

        using var createdBookmark = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/bookmarks",
            new { url = "https://example.invalid/nexora-research?source=synthetic", title = "Synthetic research source", description = "Safe local metadata only" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdBookmark.StatusCode);
        var bookmark = await ReadResourceAsync(createdBookmark);

        using var archivedBookmark = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/bookmarks/{bookmark.Id}/transition",
            new { status = "Archived" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            bookmark.ETag);
        Assert.Equal(HttpStatusCode.OK, archivedBookmark.StatusCode);
        var archivedBookmarkResource = await ReadResourceAsync(archivedBookmark);

        using var readingItem = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/read-later",
            new { sourceType = "Bookmark", sourceId = bookmark.Id },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, readingItem.StatusCode);
        var reading = await ReadResourceAsync(readingItem);

        using var readItem = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/v1/read-later/{reading.Id}",
            new { state = "Read", progress = 1m },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            reading.ETag);
        Assert.Equal(HttpStatusCode.OK, readItem.StatusCode);
        await AssertStringPropertyAsync(readItem, "state", "Read");

        using var createdSnippet = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/snippets",
            new { title = "Safe synthetic snippet", language = "csharp", body = "return 42;", description = "Never executed by the test." },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdSnippet.StatusCode);
        var snippet = await ReadResourceAsync(createdSnippet);

        using var archivedSnippet = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/snippets/{snippet.Id}/transition",
            new { status = "Archived" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            snippet.ETag);
        Assert.Equal(HttpStatusCode.OK, archivedSnippet.StatusCode);
        await AssertStringPropertyAsync(archivedSnippet, "status", "Archived");

        using var createdTag = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/organization/tags",
            new { @namespace = "documents", name = "important", color = "#2563eb" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdTag.StatusCode);
        var tag = await ReadResourceAsync(createdTag);

        using var renamedTag = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/organization/tags/{tag.Id}",
            new { name = "validated", color = "#0f766e" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            tag.ETag);
        Assert.Equal(HttpStatusCode.OK, renamedTag.StatusCode);
        await AssertStringPropertyAsync(renamedTag, "name", "validated");

        using var createdCategory = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/finance/categories",
            new { title = "Synthetic transport", ifMatch = (string?)null },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdCategory.StatusCode);
        var category = await ReadResourceAsync(createdCategory);

        using var createdRecord = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/finance/records",
            new { categoryId = category.Id, amount = "123.45", currencyCode = "USD", occurredOn = DateOnly.FromDateTime(DateTime.UtcNow), note = "Synthetic decimal amount" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdRecord.StatusCode);

        using var ownerRecords = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/finance/records?currencyCode=USD",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, ownerRecords.StatusCode);
        await AssertCollectionContainsIdAsync(ownerRecords, "items", category.Id, "categoryId");

        using var foreignBookmark = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/bookmarks/{bookmark.Id}",
            otherUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreignBookmark.StatusCode);

        // Prove the API does not depend on a client-side disabled button.
        await _fixture.SetModuleEnabledAsync(owner.UserId, "FX27", enabled: false);
        using var disabledFinance = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/finance/categories",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Conflict, disabledFinance.StatusCode);
        await AssertProblemCodeAsync(disabledFinance, "ModuleUnavailable");

        // Keep the archive update referenced so the compiler and the test
        // make the lifecycle state explicit rather than silently ignoring it.
        Assert.NotEqual(bookmark.ETag, archivedBookmarkResource.ETag);
    }

    [Fact]
    public async Task User_file_lifecycle_keeps_active_lists_private_content_and_global_trash_consistent()
    {
        _fixture.RequireAvailable();
        await _fixture.SetModuleRuntimeAvailabilityAsync("FX07", enabled: false);
        var disabledUser = await _fixture.CreateActiveSessionAsync();
        using var disabledFiles = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/files",
            disabledUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Conflict, disabledFiles.StatusCode);
        await AssertProblemCodeAsync(disabledFiles, "ModuleUnavailable");

        // FX07 is intentionally fail-closed in the shipped local catalog.
        // Exercise its real operational path in the generated test database
        // only, after proving that default. New synthetic users then receive
        // the normal per-user grant at session creation.
        await _fixture.SetModuleRuntimeAvailabilityAsync("FX07", enabled: true);
        var owner = await _fixture.CreateActiveSessionAsync();
        var otherUser = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();
        var bytes = Encoding.UTF8.GetBytes("Synthetic private file lifecycle content.");

        using var unsafeInitiate = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/files/upload-sessions",
            new { originalName = "../escape.txt", mediaType = "text/plain", expectedBytes = bytes.Length },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unsafeInitiate.StatusCode);
        await AssertProblemCodeAsync(unsafeInitiate, "ValidationFailed");

        using var initiated = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/files/upload-sessions",
            new { originalName = "synthetic-lifecycle.txt", mediaType = "text/plain", expectedBytes = bytes.Length },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, initiated.StatusCode);
        var upload = await ReadUploadSessionAsync(initiated);

        using var uploadBody = new ByteArrayContent(bytes);
        uploadBody.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var completed = await _fixture.SendContentAsync(
            HttpMethod.Put,
            $"/api/v1/files/upload-sessions/{upload.Id}/content",
            uploadBody,
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            uploadHandle: upload.Handle);
        Assert.Equal(HttpStatusCode.Created, completed.StatusCode);
        var file = await ReadResourceAsync(completed);

        using var ownerContent = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/files/{file.Id}/content",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, ownerContent.StatusCode);
        Assert.Equal("Synthetic private file lifecycle content.", await ownerContent.Content.ReadAsStringAsync());
        Assert.Equal("no-store", ownerContent.Headers.CacheControl?.ToString());

        using var foreignContent = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/files/{file.Id}/content",
            otherUser.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreignContent.StatusCode);

        using var renamed = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/v1/files/{file.Id}",
            new { originalName = "renamed-synthetic-lifecycle.txt" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            file.ETag);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var renamedFile = await ReadResourceAsync(renamed);

        using var staleTrash = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/files/{file.Id}/trash",
            new { },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            file.ETag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleTrash.StatusCode);
        await AssertProblemCodeAsync(staleTrash, "RevisionConflict");

        using var trashed = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/files/{file.Id}/trash",
            new { },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            renamedFile.ETag);
        Assert.Equal(HttpStatusCode.NoContent, trashed.StatusCode);

        using var activeFilesAfterTrash = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/files",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, activeFilesAfterTrash.StatusCode);
        await AssertCollectionExcludesIdAsync(activeFilesAfterTrash, "items", file.Id, "id");

        using var unavailableTrashedContent = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/files/{file.Id}/content",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, unavailableTrashedContent.StatusCode);

        using var firstTrashPage = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/trash",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, firstTrashPage.StatusCode);
        var firstBatchId = await ReadTrashBatchAsync(firstTrashPage, file.Id);

        using var restored = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/trash/batches/{firstBatchId}/restore",
            new { },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        await AssertIntegerPropertyAsync(restored, "restoredCount", 1);

        using var activeFilesAfterRestore = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/files",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, activeFilesAfterRestore.StatusCode);
        await AssertCollectionContainsIdAsync(activeFilesAfterRestore, "items", file.Id, "id");

        using var refreshedFile = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/files/{file.Id}",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, refreshedFile.StatusCode);
        var restoredFile = await ReadResourceAsync(refreshedFile);

        using var retrash = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/files/{file.Id}/trash",
            new { },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            restoredFile.ETag);
        Assert.Equal(HttpStatusCode.NoContent, retrash.StatusCode);

        using var secondTrashPage = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/trash",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, secondTrashPage.StatusCode);
        var secondBatchId = await ReadTrashBatchAsync(secondTrashPage, file.Id);

        using var purged = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/trash/batches/{secondBatchId}/purge",
            new { confirmation = "PURGE" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NoContent, purged.StatusCode);

        using var missingFile = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/files/{file.Id}",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, missingFile.StatusCode);
        Assert.Equal(0, await _fixture.ScalarIntAsync(
            "SELECT COUNT(*) FROM [files].[FileObject] WHERE [OwnerId] = @ownerId AND [Id] = @fileId;",
            Parameter("@ownerId", SqlDbType.UniqueIdentifier, owner.OwnerId),
            Parameter("@fileId", SqlDbType.UniqueIdentifier, file.Id)));

        var background = await _fixture.ProcessPendingBackgroundWorkAsync();
        Assert.True(background.FileCleanups >= 1);
        Assert.Equal(1, await _fixture.ScalarIntAsync(
            "SELECT COUNT(*) FROM [files].[StorageCleanup] WHERE [OwnerId] = @ownerId AND [FileObjectId] = @fileId AND [State] = 'Completed';",
            Parameter("@ownerId", SqlDbType.UniqueIdentifier, owner.OwnerId),
            Parameter("@fileId", SqlDbType.UniqueIdentifier, file.Id)));

        await _fixture.SetModuleRuntimeAvailabilityAsync("FX07", enabled: false);
    }

    [Fact]
    public async Task Read_only_sharing_hides_invalid_links_and_never_revives_revoked_capabilities()
    {
        _fixture.RequireAvailable();
        await _fixture.SetModuleRuntimeAvailabilityAsync("FX04", enabled: false);
        var disabledUser = await _fixture.CreateActiveSessionAsync();
        using (var disabledLinks = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            "/api/v1/sharing/links",
            disabledUser.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.Conflict, disabledLinks.StatusCode);
            await AssertProblemCodeAsync(disabledLinks, "ModuleUnavailable");
        }

        await _fixture.SetModuleRuntimeAvailabilityAsync("FX04", enabled: true);
        try
        {
            var owner = await _fixture.CreateActiveSessionAsync();
            var permittedViewer = await _fixture.CreateActiveSessionAsync();
            var wrongViewer = await _fixture.CreateActiveSessionAsync();
            var csrf = await _fixture.GetCsrfAsync();
            var start = DateTimeOffset.UtcNow.AddDays(2);
            var end = start.AddHours(2);

            using var createdProject = await _fixture.SendJsonAsync(
                HttpMethod.Post,
                "/api/v1/projects",
                new
                {
                    name = "Synthetic shared project",
                    description = "A live projection with no private history.",
                    startAt = start,
                    endAt = end,
                    priority = "P2",
                    tagsJson = "[]",
                    notes = "Synthetic only",
                    confirmTaskBounds = false
                },
                csrf,
                Guid.NewGuid(),
                owner.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Created, createdProject.StatusCode);
            var project = await ReadResourceAsync(createdProject);

            using var createdPublic = await _fixture.SendJsonAsync(
                HttpMethod.Post,
                "/api/v1/sharing/links",
                new
                {
                    resourceType = "Project",
                    resourceId = project.Id,
                    mode = "PublicLink",
                    expiresAt = (DateTimeOffset?)null,
                    allowedUserIds = Array.Empty<Guid>(),
                    noExpiry = true
                },
                csrf,
                Guid.NewGuid(),
                owner.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Created, createdPublic.StatusCode);
            var publicLink = await ReadShareLinkAsync(createdPublic);
            Assert.False(string.IsNullOrWhiteSpace(publicLink.Token));

            using (var ownerList = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/sharing/links", owner.RawSessionHandle))
            {
                Assert.Equal(HttpStatusCode.OK, ownerList.StatusCode);
                using var listDocument = JsonDocument.Parse(await ownerList.Content.ReadAsStringAsync());
                var listed = listDocument.RootElement.GetProperty("items").EnumerateArray()
                    .Single(item => item.GetProperty("id").GetGuid() == publicLink.Id);
                Assert.Equal(JsonValueKind.Null, listed.GetProperty("token").ValueKind);
            }

            using (var anonymousResolve = await _fixture.Client.GetAsync($"/api/v1/sharing/resolve/{Uri.EscapeDataString(publicLink.Token)}"))
            {
                Assert.Equal(HttpStatusCode.OK, anonymousResolve.StatusCode);
                await AssertNestedStringPropertyAsync(anonymousResolve, "project", "name", "Synthetic shared project");
                Assert.Equal("noindex, nofollow, noarchive", anonymousResolve.Headers.GetValues("X-Robots-Tag").Single());
            }

            using var createdRestricted = await _fixture.SendJsonAsync(
                HttpMethod.Post,
                "/api/v1/sharing/links",
                new
                {
                    resourceType = "Project",
                    resourceId = project.Id,
                    mode = "RestrictedUsers",
                    expiresAt = (DateTimeOffset?)null,
                    allowedUserIds = new[] { permittedViewer.UserId },
                    noExpiry = true
                },
                csrf,
                Guid.NewGuid(),
                owner.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Created, createdRestricted.StatusCode);
            var restrictedLink = await ReadShareLinkAsync(createdRestricted);

            using (var anonymousRestricted = await _fixture.Client.GetAsync($"/api/v1/sharing/resolve/{Uri.EscapeDataString(restrictedLink.Token)}"))
            {
                Assert.Equal(HttpStatusCode.NotFound, anonymousRestricted.StatusCode);
                Assert.DoesNotContain("Synthetic shared project", await anonymousRestricted.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }

            using (var wrongAccount = await _fixture.SendAuthenticatedAsync(
                HttpMethod.Get,
                $"/api/v1/sharing/resolve/{Uri.EscapeDataString(restrictedLink.Token)}",
                wrongViewer.RawSessionHandle))
            {
                Assert.Equal(HttpStatusCode.NotFound, wrongAccount.StatusCode);
                Assert.DoesNotContain("Synthetic shared project", await wrongAccount.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }

            using (var permittedAccount = await _fixture.SendAuthenticatedAsync(
                HttpMethod.Get,
                $"/api/v1/sharing/resolve/{Uri.EscapeDataString(restrictedLink.Token)}",
                permittedViewer.RawSessionHandle))
            {
                Assert.Equal(HttpStatusCode.OK, permittedAccount.StatusCode);
                await AssertNestedStringPropertyAsync(permittedAccount, "project", "name", "Synthetic shared project");
            }

            using (var revoked = await _fixture.SendJsonAsync(
                HttpMethod.Delete,
                $"/api/v1/sharing/links/{publicLink.Id}",
                new { },
                csrf,
                Guid.NewGuid(),
                owner.RawSessionHandle,
                publicLink.ETag))
            {
                Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
            }
            using (var revokedResolve = await _fixture.Client.GetAsync($"/api/v1/sharing/resolve/{Uri.EscapeDataString(publicLink.Token)}"))
            {
                Assert.Equal(HttpStatusCode.NotFound, revokedResolve.StatusCode);
                Assert.DoesNotContain("Synthetic shared project", await revokedResolve.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }

            using var createdBeforeDisable = await _fixture.SendJsonAsync(
                HttpMethod.Post,
                "/api/v1/sharing/links",
                new
                {
                    resourceType = "Project",
                    resourceId = project.Id,
                    mode = "PublicLink",
                    expiresAt = (DateTimeOffset?)null,
                    allowedUserIds = Array.Empty<Guid>(),
                    noExpiry = true
                },
                csrf,
                Guid.NewGuid(),
                owner.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Created, createdBeforeDisable.StatusCode);
            var disabledLink = await ReadShareLinkAsync(createdBeforeDisable);

            await _fixture.SetModuleRuntimeAvailabilityAsync("FX04", enabled: false);
            using (var disabledResolve = await _fixture.Client.GetAsync($"/api/v1/sharing/resolve/{Uri.EscapeDataString(disabledLink.Token)}"))
            {
                Assert.Equal(HttpStatusCode.NotFound, disabledResolve.StatusCode);
            }

            await _fixture.SetModuleRuntimeAvailabilityAsync("FX04", enabled: true);
            using var reenabledResolve = await _fixture.Client.GetAsync($"/api/v1/sharing/resolve/{Uri.EscapeDataString(disabledLink.Token)}");
            Assert.Equal(HttpStatusCode.NotFound, reenabledResolve.StatusCode);
            Assert.DoesNotContain("Synthetic shared project", await reenabledResolve.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }
        finally
        {
            await _fixture.SetModuleRuntimeAvailabilityAsync("FX04", enabled: false);
        }
    }

    [Fact]
    public async Task Notification_inbox_keeps_three_delivery_states_owner_scope_and_selection_safe()
    {
        _fixture.RequireAvailable();
        var superAdmin = await _fixture.CreateActiveSessionAsync("SuperAdmin");
        var owner = await _fixture.CreateActiveSessionAsync();
        var otherUser = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();

        using (var ordinaryPublish = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/admin/notifications",
            new
            {
                kind = "System",
                title = "Synthetic denied notification",
                body = "Only a super administrator may publish this local fixture.",
                sourceRef = (string?)null,
                logicalKey = "synthetic-denied-" + Guid.NewGuid().ToString("N"),
                recipientUserId = owner.UserId
            },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.Forbidden, ordinaryPublish.StatusCode);
            await AssertProblemCodeAsync(ordinaryPublish, "PermissionDenied");
        }

        using (var sensitivePublish = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/admin/notifications",
            new
            {
                kind = "System",
                title = "Synthetic rejected notification",
                body = "A reset token must never be sent through the general inbox.",
                sourceRef = (string?)null,
                logicalKey = "synthetic-sensitive-" + Guid.NewGuid().ToString("N"),
                recipientUserId = owner.UserId
            },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, sensitivePublish.StatusCode);
            await AssertProblemCodeAsync(sensitivePublish, "SensitivePayloadRejected");
        }

        var firstLogicalKey = "synthetic-inbox-first-" + Guid.NewGuid().ToString("N");
        var firstPublishRequest = new
        {
            kind = "System",
            title = "Synthetic inbox item",
            body = "This contains a safe local summary only.",
            sourceRef = "local://synthetic",
            logicalKey = firstLogicalKey,
            recipientUserId = owner.UserId
        };
        var firstPublishKey = Guid.NewGuid();
        using var firstPublished = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/admin/notifications",
            firstPublishRequest,
            csrf,
            firstPublishKey,
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, firstPublished.StatusCode);
        var first = await ReadResourceAsync(firstPublished);

        using (var replayedPublish = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/admin/notifications",
            firstPublishRequest,
            csrf,
            firstPublishKey,
            superAdmin.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.Created, replayedPublish.StatusCode);
            Assert.Equal(first.Id, await ReadIdAsync(replayedPublish));
        }

        string firstListEtag;
        using (var ownerInbox = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/notifications", owner.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, ownerInbox.StatusCode);
            using var inboxDocument = JsonDocument.Parse(await ownerInbox.Content.ReadAsStringAsync());
            Assert.Equal(1, inboxDocument.RootElement.GetProperty("unreadCount").GetInt32());
            var item = inboxDocument.RootElement.GetProperty("items").EnumerateArray().Single(candidate => candidate.GetProperty("id").GetGuid() == first.Id);
            firstListEtag = item.GetProperty("etag").GetString()!;
            var deliveries = item.GetProperty("deliveries").EnumerateArray().ToArray();
            Assert.Equal(3, deliveries.Length);
            Assert.Contains(deliveries, delivery => delivery.GetProperty("channel").GetString() == "InApp" && delivery.GetProperty("state").GetString() == "Pending");
            Assert.Contains(deliveries, delivery => delivery.GetProperty("channel").GetString() == "Email" && delivery.GetProperty("state").GetString() == "Pending");
            Assert.Contains(deliveries, delivery => delivery.GetProperty("channel").GetString() == "BrowserPush" && delivery.GetProperty("state").GetString() == "PermissionUnavailable");
        }

        using (var foreignInbox = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/notifications", otherUser.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, foreignInbox.StatusCode);
            await AssertCollectionExcludesIdAsync(foreignInbox, "items", first.Id, "id");
        }

        using var markedRead = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/v1/notifications/{first.Id}",
            new { read = true },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            firstListEtag);
        Assert.Equal(HttpStatusCode.OK, markedRead.StatusCode);
        var markedFirst = await ReadResourceAsync(markedRead);

        using (var staleMark = await _fixture.SendJsonAsync(
            HttpMethod.Patch,
            $"/api/v1/notifications/{first.Id}",
            new { read = false },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            firstListEtag))
        {
            Assert.Equal(HttpStatusCode.PreconditionFailed, staleMark.StatusCode);
            await AssertProblemCodeAsync(staleMark, "RevisionConflict");
        }

        using (var markAll = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/notifications/mark-all-read",
            new { },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, markAll.StatusCode);
            await AssertIntegerPropertyAsync(markAll, "updatedCount", 0);
        }

        using var secondPublished = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/admin/notifications",
            new
            {
                kind = "System",
                title = "Synthetic post-watermark item",
                body = "This notification was created after mark-all completed.",
                sourceRef = (string?)null,
                logicalKey = "synthetic-inbox-second-" + Guid.NewGuid().ToString("N"),
                recipientUserId = owner.UserId
            },
            csrf,
            Guid.NewGuid(),
            superAdmin.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, secondPublished.StatusCode);
        var second = await ReadResourceAsync(secondPublished);

        using (var unreadOnly = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/notifications?unreadOnly=true", owner.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.OK, unreadOnly.StatusCode);
            await AssertCollectionContainsIdAsync(unreadOnly, "items", second.Id, "id");
            await AssertCollectionExcludesIdAsync(unreadOnly, "items", first.Id, "id");
        }

        using (var deleteSelected = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/notifications/delete",
            new { notificationIds = new[] { first.Id } },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle))
        {
            Assert.Equal(HttpStatusCode.NoContent, deleteSelected.StatusCode);
        }

        using var afterDelete = await _fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/notifications", owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, afterDelete.StatusCode);
        await AssertCollectionExcludesIdAsync(afterDelete, "items", first.Id, "id");
        await AssertCollectionContainsIdAsync(afterDelete, "items", second.Id, "id");
        Assert.NotEqual(first.ETag, markedFirst.ETag);
    }

    [Fact]
    public async Task User_planning_goals_habits_and_reminders_keep_time_and_lifecycle_state_explicit()
    {
        _fixture.RequireAvailable();
        var owner = await _fixture.CreateActiveSessionAsync();
        var csrf = await _fixture.GetCsrfAsync();
        var projectStart = DateTimeOffset.UtcNow.AddDays(3).AddHours(2);
        var projectEnd = projectStart.AddHours(4);
        var plannedDate = DateOnly.FromDateTime(projectStart.UtcDateTime);
        var habitLocalDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow, "Asia/Ho_Chi_Minh").DateTime);

        using var createdProject = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/projects",
            new
            {
                name = "Synthetic planning project",
                description = "A local project used to test planner and reminder boundaries.",
                startAt = projectStart,
                endAt = projectEnd,
                priority = "P2",
                tagsJson = "[]",
                notes = "No external effects",
                confirmTaskBounds = false
            },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdProject.StatusCode);
        var project = await ReadResourceAsync(createdProject);

        using var createdTask = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/tasks",
            new
            {
                projectId = project.Id,
                title = "Plan a local-only review",
                description = "Task has an explicit interval and no hidden time conversion.",
                status = "NotStarted",
                dueAt = projectEnd,
                startAt = projectStart.AddMinutes(30),
                endAt = projectEnd.AddMinutes(-30),
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
        var task = await ReadResourceAsync(createdTask);

        Assert.Equal("NotStarted", await _fixture.ScalarStringAsync(
            "SELECT [Status] FROM [productivity].[Project] WHERE [Id] = @projectId;",
            Parameter("@projectId", SqlDbType.UniqueIdentifier, project.Id)));
        Assert.Equal("NotStarted", await _fixture.ScalarStringAsync(
            "SELECT [Status] FROM [productivity].[Task] WHERE [Id] = @taskId;",
            Parameter("@taskId", SqlDbType.UniqueIdentifier, task.Id)));
        Assert.Equal(1, await _fixture.ScalarIntAsync(
            """
            SELECT CASE WHEN taskRow.[Status] IN ('NotStarted', 'InProgress')
                              AND projectRow.[Status] IN ('NotStarted', 'InProgress')
                              AND taskRow.[DeletedAt] IS NULL
                              AND projectRow.[DeletedAt] IS NULL
                         THEN 1 ELSE 0 END
            FROM [productivity].[Task] taskRow
            INNER JOIN [productivity].[Project] projectRow
              ON projectRow.[Id] = taskRow.[ProjectId]
             AND projectRow.[OwnerId] = taskRow.[OwnerId]
            WHERE taskRow.[OwnerId] = @ownerId AND taskRow.[Id] = @taskId;
            """,
            Parameter("@ownerId", SqlDbType.UniqueIdentifier, owner.OwnerId),
            Parameter("@taskId", SqlDbType.UniqueIdentifier, task.Id)));

        using var pin = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/planner/pins",
            new { taskId = task.Id, planDate = plannedDate, notes = "Keyboard-safe planner pin" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, pin.StatusCode);
        var plannerPin = await ReadResourceAsync(pin);

        using var changedPin = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/planner/pins/{plannerPin.Id}",
            new { planDate = plannedDate, notes = "Updated synthetic pin" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            plannerPin.ETag);
        Assert.Equal(HttpStatusCode.OK, changedPin.StatusCode);

        using var planner = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/planner?from={plannedDate:yyyy-MM-dd}&to={plannedDate:yyyy-MM-dd}",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, planner.StatusCode);
        await AssertCollectionContainsIdAsync(planner, "pins", plannerPin.Id, "id");

        using var reminderSource = await _fixture.SendAuthenticatedAsync(
            HttpMethod.Get,
            $"/api/v1/reminders/Task/{task.Id}",
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, reminderSource.StatusCode);
        var sourceEtag = await ReadStringPropertyAsync(reminderSource, "sourceETag");
        Assert.False(string.IsNullOrWhiteSpace(sourceEtag));

        using var configuredReminder = await _fixture.SendJsonAsync(
            HttpMethod.Put,
            $"/api/v1/reminders/Task/{task.Id}",
            new { configType = "BeforeStart15m", exactAt = (DateTimeOffset?)null, sourceETag = sourceEtag },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, configuredReminder.StatusCode);
        await AssertNestedStringPropertyAsync(configuredReminder, "reminder", "configType", "BeforeStart15m");

        using var createdGoal = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/goals",
            new
            {
                title = "Synthetic release quality",
                description = "Keep progress explicit rather than inferred from unrelated tasks.",
                startDate = plannedDate,
                endDate = plannedDate.AddDays(14),
                numericTarget = new { title = "Coverage", initialValue = 0m, currentValue = 0m, targetValue = 100m }
            },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdGoal.StatusCode);
        var goal = await ReadGoalDetailAsync(createdGoal);

        using var activeGoal = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/goals/{goal.Id}/transition",
            new { status = "Active" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            goal.ETag);
        Assert.Equal(HttpStatusCode.OK, activeGoal.StatusCode);
        var activeGoalResource = await ReadGoalDetailAsync(activeGoal);

        using var progressedGoal = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/goals/{goal.Id}/targets/{goal.TargetId}/progress",
            new { currentValue = 50m, note = "Verified local progress" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            activeGoalResource.ETag);
        Assert.Equal(HttpStatusCode.OK, progressedGoal.StatusCode);
        var progressedGoalResource = await ReadGoalDetailAsync(progressedGoal);

        using var completedGoal = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/goals/{goal.Id}/transition",
            new { status = "Completed" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            progressedGoalResource.ETag);
        Assert.Equal(HttpStatusCode.OK, completedGoal.StatusCode);
        await AssertNestedStringPropertyAsync(completedGoal, "goal", "status", "Completed");

        using var createdHabit = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            "/api/v1/habits",
            new
            {
                title = "Synthetic daily walk",
                kind = "Count",
                targetCount = 1,
                unit = "walk",
                effectiveFrom = habitLocalDate,
                weekdayMask = (byte)127,
                timeZoneId = "Asia/Ho_Chi_Minh",
                reminderLocalTime = new TimeOnly(8, 30)
            },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, createdHabit.StatusCode);
        var habit = await ReadNestedResourceAsync(createdHabit, "habit");

        using var checkedInHabit = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/habits/{habit.Id}/check-ins",
            new { localDate = habitLocalDate, count = 1, note = "Synthetic local date check-in" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            habit.ETag);
        Assert.Equal(HttpStatusCode.OK, checkedInHabit.StatusCode);
        var checkedInHabitResource = await ReadNestedResourceAsync(checkedInHabit, "habit");

        using var pausedHabit = await _fixture.SendJsonAsync(
            HttpMethod.Post,
            $"/api/v1/habits/{habit.Id}/transition",
            new { state = "Paused" },
            csrf,
            Guid.NewGuid(),
            owner.RawSessionHandle,
            checkedInHabitResource.ETag);
        Assert.Equal(HttpStatusCode.OK, pausedHabit.StatusCode);
        await AssertNestedStringPropertyAsync(pausedHabit, "habit", "state", "Paused");
    }

    private static async Task AssertDisplayNameAsync(HttpResponseMessage response, string expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty("displayName").GetString());
    }

    private static async Task<bool> ReadBooleanAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(propertyName).GetBoolean();
    }

    private static async Task<(Guid Id, string ETag)> ReadResourceAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var etag = response.Headers.ETag?.Tag
            ?? root.GetProperty("etag").GetString();
        Assert.False(string.IsNullOrWhiteSpace(etag));
        return (root.GetProperty("id").GetGuid(), etag!);
    }

    private static async Task<(Guid Id, string Handle)> ReadUploadSessionAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var handle = root.GetProperty("uploadHandle").GetString();
        Assert.False(string.IsNullOrWhiteSpace(handle));
        return (root.GetProperty("id").GetGuid(), handle!);
    }

    private static async Task<(Guid Id, string ETag, string Token)> ReadShareLinkAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var etag = response.Headers.ETag?.Tag
            ?? root.GetProperty("etag").GetString();
        var token = root.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(etag));
        Assert.False(string.IsNullOrWhiteSpace(token));
        return (root.GetProperty("id").GetGuid(), etag!, token!);
    }

    private static async Task<Guid> ReadTrashBatchAsync(HttpResponseMessage response, Guid resourceId)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("items").EnumerateArray().Single(candidate =>
            candidate.GetProperty("resourceType").GetString() == "File" &&
            candidate.GetProperty("resourceId").GetGuid() == resourceId);
        return item.GetProperty("deletionBatchId").GetGuid();
    }

    private static async Task<(Guid Id, string ETag)> ReadNestedResourceAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement.GetProperty(propertyName);
        var etag = response.Headers.ETag?.Tag
            ?? root.GetProperty("etag").GetString();
        Assert.False(string.IsNullOrWhiteSpace(etag));
        return (root.GetProperty("id").GetGuid(), etag!);
    }

    private static async Task<(Guid Id, string ETag, Guid TargetId)> ReadGoalDetailAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var goal = document.RootElement.GetProperty("goal");
        var etag = response.Headers.ETag?.Tag
            ?? goal.GetProperty("etag").GetString();
        Assert.False(string.IsNullOrWhiteSpace(etag));
        var target = document.RootElement.GetProperty("targets").EnumerateArray().Single();
        return (goal.GetProperty("id").GetGuid(), etag!, target.GetProperty("id").GetGuid());
    }

    private static async Task AssertStringPropertyAsync(HttpResponseMessage response, string propertyName, string expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty(propertyName).GetString());
    }

    private static async Task AssertIntegerPropertyAsync(HttpResponseMessage response, string propertyName, int expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty(propertyName).GetInt32());
    }

    private static async Task<string?> ReadStringPropertyAsync(HttpResponseMessage response, string propertyName)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(propertyName).GetString();
    }

    private static async Task AssertNestedStringPropertyAsync(HttpResponseMessage response, string parentProperty,
        string propertyName, string expected)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected, document.RootElement.GetProperty(parentProperty).GetProperty(propertyName).GetString());
    }

    private static async Task AssertCollectionContainsIdAsync(HttpResponseMessage response, string collectionProperty,
        Guid expectedId, string idProperty)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(document.RootElement.GetProperty(collectionProperty).EnumerateArray(),
            item => item.GetProperty(idProperty).GetGuid() == expectedId);
    }

    private static async Task AssertCollectionExcludesIdAsync(HttpResponseMessage response, string collectionProperty,
        Guid unexpectedId, string idProperty)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(document.RootElement.GetProperty(collectionProperty).EnumerateArray(),
            item => item.GetProperty(idProperty).GetGuid() == unexpectedId);
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
