using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlReleaseCoverageTests(SqlApiFixture fixture)
{
    [Theory]
    [InlineData("organization", "projects", "organization", "FX24")]
    [InlineData("documents", "documents", "documents", "FX20")]
    [InlineData("organization", "documents", "documents", "FX24")]
    public async Task Deleted_Tag_create_replay_is_opaque_without_resurrection_and_requires_current_authority(
        string route, string ns, string schema, string module)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin");
        var authorityPrefixes = route == "organization" && ns == "documents"
            ? new[] { "organization", "documents" } : new[] { route };
        foreach (var prefix in authorityPrefixes)
            foreach (var operation in new[] { "create", "read", "remove" })
                await Permission(session, $"{prefix}.tag.{operation}", "Allow");
        var path = $"/api/v1/{route}/tags";
        var body = new { @namespace = ns, name = "Synthetic deleted replay", color = (string?)null };
        var key = Guid.NewGuid();
        using var created = await Send(session, HttpMethod.Post, path, body, key);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var tag = await Json(created);
        var id = tag.GetProperty("id").GetGuid();
        var etag = tag.GetProperty("etag").GetString()!;
        using var deleted = await Send(session, HttpMethod.Delete, $"{path}/{id}", new { }, etag: etag);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var auditCount = await AuditCount(session, id);
        Assert.Equal(0, await TagCount(schema, session, id));

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var replay = await Send(session, HttpMethod.Post, path, body, key);
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            var acknowledgement = await Json(replay);
            Assert.Equal(id, acknowledgement.GetProperty("id").GetGuid());
            Assert.True(acknowledgement.GetProperty("committed").GetBoolean());
            Assert.False(acknowledgement.GetProperty("detailAvailable").GetBoolean());
            Assert.Equal(3, acknowledgement.EnumerateObject().Count());
            Assert.Null(replay.Headers.ETag);
        }
        using var collision = await Send(session, HttpMethod.Post, path,
            new { @namespace = ns, name = "Changed request", color = (string?)null }, key);
        await Problem(collision, "IdempotencyConflict");
        var originalAction = $"{route}.tag.create";
        await Permission(session, originalAction, "Deny");
        using (var revokedActionReplay = await Send(session, HttpMethod.Post, path, body, key))
            await Problem(revokedActionReplay, "ModuleUnavailable");
        await Permission(session, originalAction, "Allow");
        await fixture.SetModuleEnabledAsync(session.UserId, module, false);
        using var revokedReplay = await Send(session, HttpMethod.Post, path, body, key);
        await Problem(revokedReplay, "ModuleUnavailable");
        Assert.Equal(0, await TagCount(schema, session, id));
        Assert.Equal(0, await TagNameCount(schema, session, ns, body.name));
        Assert.Equal(auditCount, await AuditCount(session, id));
    }

    [Theory]
    [InlineData("organization", "projects", "organization")]
    [InlineData("documents", "documents", "documents")]
    public async Task Concurrent_same_key_Tag_create_commits_one_resource_and_one_audit(string route, string ns, string schema)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var csrf = await fixture.GetCsrfAsync();
        var key = Guid.NewGuid();
        var path = $"/api/v1/{route}/tags";
        var body = new { @namespace = ns, name = "Synthetic concurrent create", color = (string?)null };
        var replies = await Task.WhenAll(
            fixture.SendJsonAsync(HttpMethod.Post, path, body, csrf, key, session.RawSessionHandle),
            fixture.SendJsonAsync(HttpMethod.Post, path, body, csrf, key, session.RawSessionHandle));
        using var first = replies[0];
        using var second = replies[1];
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var id = (await Json(first)).GetProperty("id").GetGuid();
        Assert.Equal(id, (await Json(second)).GetProperty("id").GetGuid());
        Assert.Equal(1, await TagCount(schema, session, id));
        Assert.Equal(1, await TagNameCount(schema, session, ns, body.name));
        Assert.Equal(1, await AuditCount(session, id));
    }

    [Theory]
    [InlineData("organization", "projects")]
    [InlineData("documents", "documents")]
    public async Task Tag_delete_rejects_foreign_owner_and_stale_revision_without_removing_current_resource(string route, string ns)
    {
        fixture.RequireAvailable();
        var owner = await fixture.CreateActiveSessionAsync();
        var foreign = await fixture.CreateActiveSessionAsync();
        var path = $"/api/v1/{route}/tags";
        using var created = await Send(owner, HttpMethod.Post, path,
            new { @namespace = ns, name = "Synthetic revision", color = (string?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = await Json(created);
        var id = original.GetProperty("id").GetGuid();
        var etag = original.GetProperty("etag").GetString()!;
        using var foreignDelete = await Send(foreign, HttpMethod.Delete, $"{path}/{id}", new { }, etag: etag);
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);
        using var renamed = await Send(owner, HttpMethod.Put, $"{path}/{id}",
            new { name = "Current synthetic revision", color = (string?)null }, etag: etag);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var current = await Json(renamed);
        using var staleDelete = await Send(owner, HttpMethod.Delete, $"{path}/{id}", new { }, etag: etag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleDelete.StatusCode);
        using var read = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"{path}/{id}", owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(current.GetRawText(), (await Json(read)).GetRawText());
    }

    [Fact]
    public async Task Admin_Goal_history_requires_explicit_Allow_and_rechecks_history_and_reader_revocation()
    {
        var (session, detail) = await AdminGoal();
        var id = detail.GetProperty("goal").GetProperty("id").GetGuid();
        foreach (var suffix in new[] { "progress-history", "metadata-history", "state-history", "target-history" })
        {
            var path = $"/api/v1/goals/{id}/{suffix}";
            using (var unset = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, session.RawSessionHandle))
                await Problem(unset, "ModuleUnavailable");
            await Permission(session, "goals.goal.history", "Allow");
            using (var allowed = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, session.RawSessionHandle))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Equal(JsonValueKind.Array, (await Json(allowed)).GetProperty("items").ValueKind);
            }
            await Permission(session, "goals.goal.history", "Deny");
            using (var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, session.RawSessionHandle))
                await Problem(denied, "ModuleUnavailable");
            await Permission(session, "goals.goal.history", "Allow");
            var readerAction = suffix is "progress-history" or "target-history" ? "goals.target.read" : "goals.goal.read";
            await Permission(session, readerAction, null);
            using (var readerRevoked = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, session.RawSessionHandle))
                await Problem(readerRevoked, "ModuleUnavailable");
            await Permission(session, readerAction, "Allow");
            await Permission(session, "goals.goal.history", null);
        }
    }

    [Theory]
    [InlineData("Archived", "goals.goal.archive", "Archived")]
    [InlineData("Unarchive", "goals.goal.unarchive", "Draft")]
    public async Task Admin_Goal_archive_actions_require_current_Allow_including_completed_replay(string transition, string action, string expectedStatus)
    {
        var (session, detail) = await AdminGoal();
        var id = detail.GetProperty("goal").GetProperty("id").GetGuid();
        var path = $"/api/v1/goals/{id}/transition";
        if (transition == "Unarchive")
        {
            await Permission(session, "goals.goal.archive", "Allow");
            using var archived = await Send(session, HttpMethod.Post, path, new { status = "Archived" }, etag: GoalEtag(detail));
            Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
            detail = await Json(archived);
        }
        var etag = GoalEtag(detail);
        var key = Guid.NewGuid();
        var body = new { status = transition };
        using (var unset = await Send(session, HttpMethod.Post, path, body, key, etag)) await Problem(unset, "ModuleUnavailable");
        await Permission(session, action, "Deny");
        using (var denied = await Send(session, HttpMethod.Post, path, body, key, etag)) await Problem(denied, "ModuleUnavailable");
        Assert.Equal(detail.GetRawText(), (await Goal(session, id)).GetRawText());
        await Permission(session, action, "Allow");
        using var committed = await Send(session, HttpMethod.Post, path, body, key, etag);
        Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
        var after = await Json(committed);
        Assert.Equal(expectedStatus, after.GetProperty("goal").GetProperty("status").GetString());
        Assert.NotEqual(etag, GoalEtag(after));
        var auditCount = await AuditCount(session, id);
        await Permission(session, action, null);
        using var revokedReplay = await Send(session, HttpMethod.Post, path, body, key, etag);
        await Problem(revokedReplay, "ModuleUnavailable");
        Assert.Equal(after.GetRawText(), (await Goal(session, id)).GetRawText());
        Assert.Equal(auditCount, await AuditCount(session, id));
    }

    [Theory]
    [InlineData("Update", "goals.target.update", "update-definition")]
    [InlineData("Remove", "goals.target.remove", "remove")]
    public async Task Admin_Goal_target_preview_does_not_preserve_revoked_commit_or_replay_authority(string mode, string action, string suffix)
    {
        var (session, detail) = await AdminGoal();
        var id = detail.GetProperty("goal").GetProperty("id").GetGuid();
        var targetId = detail.GetProperty("targets")[0].GetProperty("id").GetGuid();
        var path = $"/api/v1/goals/{id}/targets/{targetId}";
        var etag = GoalEtag(detail);
        object change = mode == "Update"
            ? new { mode, title = (string?)"Changed synthetic target", initialValue = (decimal?)0m, targetValue = (decimal?)20m }
            : new { mode, title = (string?)null, initialValue = (decimal?)null, targetValue = (decimal?)null };
        using (var unset = await Send(session, HttpMethod.Post, path + "/change-preview", change, etag: etag))
            await Problem(unset, "ModuleUnavailable");
        await Permission(session, action, "Deny");
        using (var denied = await Send(session, HttpMethod.Post, path + "/change-preview", change, etag: etag))
            await Problem(denied, "ModuleUnavailable");
        await Permission(session, action, "Allow");
        using var preview = await Send(session, HttpMethod.Post, path + "/change-preview", change, etag: etag);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var token = (await Json(preview)).GetProperty("previewToken").GetString()!;
        Assert.Equal(detail.GetRawText(), (await Goal(session, id)).GetRawText());
        var body = new { previewToken = token };
        var key = Guid.NewGuid();
        await Permission(session, action, null);
        using (var revoked = await Send(session, HttpMethod.Post, path + "/" + suffix, body, key, etag))
            await Problem(revoked, "ModuleUnavailable");
        Assert.Equal(detail.GetRawText(), (await Goal(session, id)).GetRawText());
        await Permission(session, action, "Allow");
        using var committed = await Send(session, HttpMethod.Post, path + "/" + suffix, body, key, etag);
        Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
        var after = await Json(committed);
        Assert.NotEqual(etag, GoalEtag(after));
        Assert.Equal("Draft", after.GetProperty("goal").GetProperty("status").GetString());
        if (mode == "Remove") Assert.Empty(after.GetProperty("targets").EnumerateArray());
        else
        {
            Assert.Equal("Changed synthetic target", after.GetProperty("targets")[0].GetProperty("title").GetString());
            Assert.Equal(20m, after.GetProperty("targets")[0].GetProperty("targetValue").GetDecimal());
        }
        var auditCount = await AuditCount(session, id);
        await Permission(session, action, "Deny");
        using var revokedReplay = await Send(session, HttpMethod.Post, path + "/" + suffix, body, key, etag);
        await Problem(revokedReplay, "ModuleUnavailable");
        Assert.Equal(after.GetRawText(), (await Goal(session, id)).GetRawText());
        Assert.Equal(auditCount, await AuditCount(session, id));
    }

    private async Task<(SyntheticSession Session, JsonElement Detail)> AdminGoal()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin");
        foreach (var action in new[] { "goals.goal.create", "goals.goal.read", "goals.target.create", "goals.target.read" })
            await Permission(session, action, "Allow");
        using var created = await Send(session, HttpMethod.Post, "/api/v1/goals", new
        {
            title = "Synthetic current authority", description = "SQL authority regression",
            startDate = (string?)null, endDate = (string?)null,
            numericTarget = new { title = "Synthetic target", initialValue = 0m, currentValue = 0m, targetValue = 10m }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (session, await Json(created));
    }

    // Fixture-only authority setup; all business resources and mutations use the HTTP boundary.
    private async Task Permission(SyntheticSession session, string action, string? effect)
    {
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Permission] WHERE [ActionKey]=@Action;", Text("@Action", action)));
        await fixture.ExecuteAsync("DELETE ap FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey=@Action;",
            Id("@User", session.UserId), Text("@Action", action));
        if (effect is not null)
            await fixture.ExecuteAsync("INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,@Effect FROM [platform].[Permission] WHERE ActionKey=@Action;",
                Id("@User", session.UserId), Text("@Action", action), Text("@Effect", effect));
    }

    private async Task<HttpResponseMessage> Send(SyntheticSession session, HttpMethod method, string path, object body,
        Guid? key = null, string? etag = null) => await fixture.SendJsonAsync(method, path, body,
            await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), session.RawSessionHandle, etag);
    private async Task<JsonElement> Goal(SyntheticSession session, Guid id)
    {
        using var read = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/goals/{id}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        return await Json(read);
    }
    private Task<int> TagCount(string schema, SyntheticSession session, Guid id) => fixture.ScalarIntAsync(
        $"SELECT COUNT(*) FROM [{schema}].[Tag] WHERE OwnerId=@Owner AND Id=@Id;", Id("@Owner", session.OwnerId), Id("@Id", id));
    private Task<int> TagNameCount(string schema, SyntheticSession session, string ns, string name) => fixture.ScalarIntAsync(
        $"SELECT COUNT(*) FROM [{schema}].[Tag] WHERE OwnerId=@Owner AND Namespace=@Namespace AND Name=@Name;",
        Id("@Owner", session.OwnerId), Text("@Namespace", ns), Text("@Name", name));
    private Task<int> AuditCount(SyntheticSession session, Guid id) => fixture.ScalarIntAsync(
        "SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND TargetId=@Id AND Result='Succeeded';", Id("@User", session.UserId), Id("@Id", id));
    private static string GoalEtag(JsonElement detail) => detail.GetProperty("goal").GetProperty("etag").GetString()!;
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
    private static async Task Problem(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await Json(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("goal", out _));
        Assert.False(problem.TryGetProperty("id", out _));
        Assert.False(problem.TryGetProperty("previewToken", out _));
    }
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static SqlParameter Text(string name, string value) => new(name, SqlDbType.NVarChar, 160) { Value = value };
}
