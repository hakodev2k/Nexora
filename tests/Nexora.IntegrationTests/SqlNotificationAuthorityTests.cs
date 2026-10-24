using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Notifications;
using Nexora.Domain.Access;
using Nexora.Infrastructure.Notifications;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlNotificationAuthorityTests(SqlApiFixture fixture)
{
    private SqlNotificationService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-notification-key");
    private static IdentityPrincipal Actor(SyntheticSession session, string role = "User") => new(session.UserId, session.OwnerId, role, DateTimeOffset.UtcNow, session.SessionId);
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };

    [Fact]
    public async Task Read_state_command_without_inbox_read_returns_stable_opaque_ACK_and_API_never_leaks_body()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin"); var service = Service();
        await Grant(session.UserId, "notifications.inbox.mark_read"); await Grant(session.UserId, "notifications.inbox.mark_all_read");
        var item = await Seed(session.UserId); var key = Guid.NewGuid().ToString(); var command = new NotificationMarkReadCommand(true, await ETag(item));
        var marked = service.MarkRead(actor, item, command, key); Assert.True(marked.Succeeded, marked.Code); Assert.Null(marked.Value); Assert.Equal(item, marked.Acknowledgement!.Id);
        Assert.Equal(marked, service.MarkRead(actor, item, command, key));
        var apiItem = await Seed(session.UserId); using var response = await fixture.SendJsonAsync(HttpMethod.Patch, $"/api/v1/notifications/{apiItem}", new { read = true }, await fixture.GetCsrfAsync(), Guid.NewGuid(), session.RawSessionHandle, await ETag(apiItem));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "committed", "detailAvailable", "id" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray()); Assert.False(json.RootElement.GetProperty("detailAvailable").GetBoolean());
        var batchKey = Guid.NewGuid().ToString(); var all = service.MarkAllRead(actor, batchKey); Assert.True(all.Succeeded); Assert.Null(all.Value); Assert.Equal(session.OwnerId, all.Acknowledgement!.Id);
        await Grant(session.UserId, "notifications.inbox.read");
        Assert.Equal(marked, service.MarkRead(actor, item, command, key)); Assert.Equal(all, service.MarkAllRead(actor, batchKey));
        var stored = await fixture.ScalarStringAsync("SELECT STRING_AGG(CONVERT(nvarchar(max),[ResultJson]),N'') FROM [identity].[RequestReceipt] WHERE [OperationKey] LIKE N'notifications.inbox.%';");
        Assert.DoesNotContain("Private canary", stored); Assert.DoesNotContain("Deliveries", stored);
        await RemoveGrant(session.UserId, "notifications.inbox.mark_read"); Assert.False(service.MarkRead(actor, item, command, key).Succeeded);
    }

    [Fact]
    public async Task Replay_rechecks_read_current_lifecycle_and_full_delivery_projection_without_second_write()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var item = await Seed(session.UserId); var key = Guid.NewGuid().ToString(); var command = new NotificationMarkReadCommand(true, await ETag(item));
        var marked = service.MarkRead(actor, item, command, key); Assert.True(marked.Succeeded, marked.Code); Assert.Equal(3, marked.Value!.Deliveries.Count);
        var replay = service.MarkRead(actor, item, command, key); Assert.True(replay.Succeeded); Assert.Equal(marked.Value.ETag, replay.Value!.ETag); Assert.Equal(marked.Value.ReadAt, replay.Value.ReadAt);
        Assert.Equal("IdempotencyConflict", service.MarkRead(actor, item, command with { Read = false }, key).Code);
        Assert.Equal("RevisionConflict", service.MarkRead(actor, item, command with { Read = false }).Code);
        await fixture.ExecuteAsync("UPDATE [notifications].[Delivery] SET [State]='Failed',[LastErrorCode]='SyntheticUnavailable',[UpdatedAt]=SYSUTCDATETIME() WHERE [NotificationId]=@Id AND [Channel]='Email';", Id("@Id", item));
        var changed = service.MarkRead(actor, item, command, key); Assert.True(changed.Succeeded); Assert.Null(changed.Value); Assert.NotNull(changed.Acknowledgement);
        Assert.Equal(marked.Value.ETag, await ETag(item));
        Assert.True(service.Delete(actor, new([item])).Succeeded);
        var removed = service.MarkRead(actor, item, command, key); Assert.True(removed.Succeeded); Assert.Null(removed.Value); Assert.NotNull(removed.Acknowledgement);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [TargetId]=@Id AND [ActionKey]='notifications.inbox.mark_read';", Id("@Id", item)));
        await fixture.SetModuleEnabledAsync(session.UserId, "FX06", false); Assert.False(service.MarkRead(actor, item, command, key).Succeeded);
    }

    [Fact]
    public async Task Current_read_revocation_returns_ACK_and_session_revocation_blocks_every_inbox_path()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin"); var service = Service();
        await Grant(session.UserId, "notifications.inbox.mark_read"); await Grant(session.UserId, "notifications.inbox.read");
        var item = await Seed(session.UserId); var key = Guid.NewGuid().ToString(); var command = new NotificationMarkReadCommand(true, await ETag(item));
        Assert.NotNull(service.MarkRead(actor, item, command, key).Value);
        await RemoveGrant(session.UserId, "notifications.inbox.read"); var replay = service.MarkRead(actor, item, command, key); Assert.True(replay.Succeeded); Assert.Null(replay.Value); Assert.NotNull(replay.Acknowledgement); Assert.False(service.List(actor).Succeeded);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET [RevokedAt]=SYSUTCDATETIME() WHERE [Id]=@Id;", Id("@Id", session.SessionId));
        Assert.False(service.MarkRead(actor, item, command, key).Succeeded); Assert.False(service.List(actor).Succeeded);
    }

    [Fact]
    public async Task Source_linked_private_snapshots_are_suppressed_until_a_registered_current_source_proof_exists()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var item = await Seed(session.UserId, "Task:" + Guid.NewGuid().ToString("N"));
        var inbox = service.List(actor); Assert.True(inbox.Succeeded); var projected = Assert.Single(inbox.Value!.Items);
        Assert.True(projected.SourceUnavailable); Assert.Null(projected.SourceRef); Assert.DoesNotContain("Private canary", projected.Title + projected.Body); Assert.Equal(3, projected.Deliveries.Count);
        var marked = service.MarkRead(actor, item, new(true, projected.ETag), Guid.NewGuid().ToString()); Assert.True(marked.Succeeded); Assert.True(marked.Value!.SourceUnavailable); Assert.Null(marked.Value.SourceRef);
        using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/notifications", session.RawSessionHandle); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("Private canary", body); Assert.DoesNotContain("Task:", body); using var json = JsonDocument.Parse(body); Assert.True(json.RootElement.GetProperty("items")[0].GetProperty("sourceUnavailable").GetBoolean());
    }

    [Fact]
    public async Task Mark_all_replay_keeps_original_watermark_and_never_marks_later_notifications()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        await Seed(session.UserId); var key = Guid.NewGuid().ToString(); var all = service.MarkAllRead(actor, key); Assert.True(all.Succeeded); Assert.Equal(1, all.Value!.UpdatedCount);
        var later = await Seed(session.UserId, createdAt: all.Value.Watermark.UtcDateTime.AddMinutes(1)); Assert.Equal(all, service.MarkAllRead(actor, key));
        Assert.Equal(later, Assert.Single(service.List(actor, unreadOnly: true).Value!.Items).Id);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId]=@User AND [ActionKey]='notifications.inbox.mark_all_read';", Id("@User", session.UserId)));
    }

    [Fact]
    public async Task Explicit_delete_rejects_foreign_or_excess_selection_and_replay_keeps_later_items_and_delivery()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var own = await Seed(session.UserId); var foreign = await Seed(other.UserId);
        Assert.Equal("ResourceUnavailable", service.Delete(actor, new([own, foreign])).Code); Assert.Equal(own, Assert.Single(service.List(actor).Value!.Items).Id);
        Assert.Equal("ValidationFailed", service.Delete(actor, new(Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray())).Code);
        var key = Guid.NewGuid().ToString(); var removed = service.Delete(actor, new([own]), key); Assert.Equal(204, removed.StatusCode);
        var later = await Seed(session.UserId); Assert.Equal(removed, service.Delete(actor, new([own]), key)); Assert.Equal(later, Assert.Single(service.List(actor).Value!.Items).Id);
        Assert.Equal(3, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [notifications].[Delivery] WHERE [NotificationId]=@Id;", Id("@Id", own)));
        Assert.Equal("IdempotencyConflict", service.Delete(actor, new([later]), key).Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Authority_change_during_read_state_write_rolls_back_state_audit_and_receipt(bool keyed)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service(); var item = await Seed(session.UserId); var etag = await ETag(item);
        await fixture.ExecuteAsync($"""
            CREATE TRIGGER [notifications].[NotificationAuthorityIntegrationGuard] ON [notifications].[Notification] AFTER UPDATE AS
            BEGIN
             SET NOCOUNT ON;
             UPDATE g SET [Enabled]=0 FROM [platform].[UserModuleGrant] g JOIN [platform].[Module] m ON m.[Id]=g.[ModuleId]
             WHERE m.[Code]='FX06' AND g.[UserId]='{session.UserId:D}' AND EXISTS(SELECT 1 FROM inserted WHERE [Id]='{item:D}');
            END;
            """);
        try
        {
            var before = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt];");
            var result = service.MarkRead(actor, item, new(true, etag), keyed ? Guid.NewGuid().ToString() : null); Assert.False(result.Succeeded); Assert.Equal("ModuleUnavailable", result.Code); Assert.Equal(etag, await ETag(item));
            Assert.Null(Assert.Single(service.List(actor).Value!.Items).ReadAt);
            Assert.Equal(before, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt];"));
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [TargetId]=@Id;", Id("@Id", item)));
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [notifications].[NotificationAuthorityIntegrationGuard];"); }
    }

    [Fact]
    public async Task Arbitrary_HTTP_publication_is_unavailable_even_for_SuperAdmin_and_SYSTEM_keys_are_not_grantable()
    {
        fixture.RequireAvailable(); Assert.False(ActionGrantPolicy.IsAdminGrantable("notifications.dispatch.publish")); Assert.False(ActionGrantPolicy.IsAdminGrantable("notifications.dispatch.deliver"));
        foreach (var role in new[] { "User", "SuperAdmin" })
        {
            var session = await fixture.CreateActiveSessionAsync(role); using var response = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/admin/notifications", new { kind = "System", title = "Unsafe forged intent", body = "Arbitrary content", logicalKey = Guid.NewGuid().ToString(), recipientUserId = session.UserId }, await fixture.GetCsrfAsync(), Guid.NewGuid(), session.RawSessionHandle);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [notifications].[Notification] WHERE [OwnerUserId]=@User;", Id("@User", session.UserId)));
        }
    }

    private async Task<Guid> Seed(Guid user, string? source = null, DateTime? createdAt = null)
    {
        var id = Guid.NewGuid();
        await fixture.ExecuteAsync("""
            INSERT INTO [notifications].[Notification]([Id],[OwnerUserId],[LogicalKey],[Kind],[Title],[Body],[SourceRef],[CreatedAt]) VALUES(@Id,@User,@Key,'System',N'Private canary title',N'Private canary body',@Source,@Created);
            INSERT INTO [notifications].[Delivery]([NotificationId],[Channel],[State],[LastErrorCode]) VALUES(@Id,'InApp','Delivered',NULL),(@Id,'Email','Pending',NULL),(@Id,'BrowserPush','PermissionUnavailable','PushPermissionUnavailable');
            """, Id("@Id", id), Id("@User", user), new SqlParameter("@Key", SqlDbType.NVarChar, 200) { Value = "synthetic-inbox:" + id.ToString("N") }, new SqlParameter("@Source", SqlDbType.NVarChar, 200) { Value = (object?)source ?? DBNull.Value }, new SqlParameter("@Created", SqlDbType.DateTime2) { Value = createdAt ?? DateTime.UtcNow.AddSeconds(-1) });
        return id;
    }
    private async Task<string> ETag(Guid id) => "\"" + Convert.ToBase64String(await fixture.ScalarBytesAsync("SELECT [RowVersion] FROM [notifications].[Notification] WHERE [Id]=@Id;", Id("@Id", id))) + "\"";
    private async Task Grant(Guid user, string action) => await fixture.ExecuteAsync("INSERT INTO [platform].[AdminPermission]([UserId],[PermissionId],[Effect]) SELECT @User,[Id],'Allow' FROM [platform].[Permission] WHERE [ActionKey]=@Action;", Id("@User", user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
    private async Task RemoveGrant(Guid user, string action) => await fixture.ExecuteAsync("DELETE g FROM [platform].[AdminPermission] g JOIN [platform].[Permission] p ON p.[Id]=g.[PermissionId] WHERE g.[UserId]=@User AND p.[ActionKey]=@Action;", Id("@User", user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
}
