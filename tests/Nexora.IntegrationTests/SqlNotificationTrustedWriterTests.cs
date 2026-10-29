using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Focus;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlNotificationTrustedWriterTests(SqlApiFixture fixture)
{
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private SqlFocusService Finisher() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-focus-finisher-test-key");
    private async Task<JsonElement> Send(SyntheticSession actor, HttpMethod method, string path, object body, HttpStatusCode expected = HttpStatusCode.OK, string? etag = null)
    {
        using var response = await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), Guid.NewGuid(), actor.RawSessionHandle, etag);
        Assert.Equal(expected, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.Clone();
    }
    private async Task<JsonElement> Get(SyntheticSession actor, string path)
    {
        using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, actor.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return json.RootElement.Clone();
    }
    private async Task<(SyntheticSession Actor, Guid Session)> Due()
    {
        fixture.RequireAvailable();
        // Real migrated readiness/default grants, not a test status promotion.
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM platform.Module WHERE Code IN ('FX18','FX19') AND State='Ready' AND SystemEnabled=1 AND RegistrationEnabled=1;"));
        var actor = await fixture.CreateActiveSessionAsync();
        var started = await Send(actor, HttpMethod.Post, "/api/v1/focus/sessions", new { phase = "Focus" }, HttpStatusCode.Created);
        var session = started.GetProperty("id").GetGuid();
        // Explicit owned fixture clock boundary only. No completed state, fake
        // notification or authority is injected. The real finisher must act.
        await fixture.ExecuteAsync("UPDATE [time].[FocusSession] SET StartedAt=DATEADD(second,-PlannedSeconds-1,SYSUTCDATETIME()),LastRunAt=DATEADD(second,-PlannedSeconds-1,SYSUTCDATETIME()) WHERE Id=@Id AND OwnerId=@Owner AND State='Running' AND ElapsedMilliseconds=0;", Id("@Id", session), Id("@Owner", actor.OwnerId));
        return (actor, session);
    }
    private Task<int> Notifications(Guid session) => fixture.ScalarIntAsync("SELECT COUNT(*) FROM notifications.Notification WHERE LogicalKey=@Key;", new SqlParameter("@Key", SqlDbType.NVarChar, 200) { Value = $"focus.completed:{session:N}" });
    private Task<int> Audits(Guid session) => fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AuditEvent WHERE TargetId=@Id AND ActionKey='focus.session.finish_phase';", Id("@Id", session));
    private Task<int> Running(Guid session) => fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusSession] WHERE Id=@Id AND State='Running' AND IsActive=1;", Id("@Id", session));
    private async Task AssertCompleted(SyntheticSession actor, Guid session)
    {
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusSession] WHERE Id=@Id AND OwnerId=@Owner AND State='Completed' AND IsActive=0 AND LastRunAt IS NULL AND CompletedAt IS NOT NULL AND ElapsedMilliseconds=CAST(PlannedSeconds AS bigint)*1000;", Id("@Id", session), Id("@Owner", actor.OwnerId)));
        Assert.Equal(1, await Notifications(session)); Assert.Equal(1, await Audits(session));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM security.AuditEvent WHERE TargetId=@Id AND ActionKey='focus.session.finish_phase' AND ActorUserId IS NULL AND OwnerUserId=@User;", Id("@Id", session), Id("@User", actor.UserId)));
        Assert.Equal(3, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM notifications.Delivery d JOIN notifications.Notification n ON n.Id=d.NotificationId WHERE n.LogicalKey=@Key AND n.OwnerUserId=@User AND ((d.Channel='InApp' AND d.State='Delivered' AND d.Attempts=1 AND d.LastErrorCode IS NULL) OR (d.Channel='Email' AND d.State='NotApplicable' AND d.Attempts=0 AND d.LastErrorCode='ProviderUnavailable') OR (d.Channel='BrowserPush' AND d.State='PermissionUnavailable' AND d.Attempts=0 AND d.LastErrorCode='ProviderUnavailable'));", new SqlParameter("@Key", SqlDbType.NVarChar, 200) { Value = $"focus.completed:{session:N}" }, Id("@User", actor.UserId)));
    }

    [Fact]
    public async Task Concurrent_and_repeated_finishers_commit_one_phase_one_logical_notification_and_three_truthful_channels()
    {
        var (actor, session) = await Due();
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => Finisher().FinishDueAsync(CancellationToken.None))));
        await AssertCompleted(actor, session);
        var version = await fixture.ScalarBytesAsync("SELECT RowVersion FROM [time].[FocusSession] WHERE Id=@Id", Id("@Id", session));
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => Finisher().FinishDueAsync(CancellationToken.None))));
        await AssertCompleted(actor, session);
        Assert.Equal(version, await fixture.ScalarBytesAsync("SELECT RowVersion FROM [time].[FocusSession] WHERE Id=@Id", Id("@Id", session)));
    }

    private async Task SetGrant(SyntheticSession admin, SyntheticSession target, bool enabled)
    {
        var access = await Get(admin, $"/api/v1/admin/users/{target.UserId}/access");
        var module = access.GetProperty("moduleGrants").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX19");
        var changes = new[] { new { moduleId = module.GetProperty("moduleId").GetGuid(), enabled } };
        var preview = await Send(admin, HttpMethod.Post, $"/api/v1/admin/users/{target.UserId}/access/preview", new { kind = "modules", changes });
        Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        await Send(admin, HttpMethod.Put, $"/api/v1/admin/users/{target.UserId}/access/modules", new { kind = "modules", changes, previewToken = preview.GetProperty("previewToken").GetString() }, etag: preview.GetProperty("etag").GetString());
    }

    [Fact]
    public async Task Current_owner_module_revocation_prevents_completion_and_notification_until_legitimate_restore()
    {
        var (actor, session) = await Due(); var admin = await fixture.CreateActiveSessionAsync("SuperAdmin");
        await SetGrant(admin, actor, false);
        try
        {
            await Finisher().FinishDueAsync(CancellationToken.None);
            Assert.Equal(1, await Running(session)); Assert.Equal(0, await Notifications(session)); Assert.Equal(0, await Audits(session));
        }
        finally { await SetGrant(admin, actor, true); }
        await Finisher().FinishDueAsync(CancellationToken.None); await AssertCompleted(actor, session);
    }

    [Fact]
    public async Task Notification_insert_failure_rolls_back_phase_and_audit_then_bounded_retry_recovers_once()
    {
        var (actor, session) = await Due();
        await fixture.ExecuteAsync($"""
            CREATE TRIGGER notifications.FocusNotificationFailureFixture ON notifications.Notification AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted WHERE LogicalKey=N'focus.completed:{session:N}')
                    THROW 51001,'Synthetic notification persistence unavailable',1;
            END;
            """);
        try
        {
            await Finisher().FinishDueAsync(CancellationToken.None);
            Assert.Equal(1, await Running(session)); Assert.Equal(0, await Notifications(session)); Assert.Equal(0, await Audits(session));
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusSession] WHERE Id=@Id AND CompletionFailures=1 AND NextCompletionAttemptAt>SYSUTCDATETIME();", Id("@Id", session)));
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER notifications.FocusNotificationFailureFixture;"); }
        await fixture.ExecuteAsync("UPDATE [time].[FocusSession] SET NextCompletionAttemptAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Id=@Id AND OwnerId=@Owner AND State='Running';", Id("@Id", session), Id("@Owner", actor.OwnerId));
        await Finisher().FinishDueAsync(CancellationToken.None); await AssertCompleted(actor, session);
    }

    [Fact]
    public async Task Authority_change_inside_completion_rolls_back_phase_notification_and_audit()
    {
        var (actor, session) = await Due();
        await fixture.ExecuteAsync($"""
            CREATE TRIGGER [time].FocusAuthorityChangeFixture ON [time].FocusSession AFTER UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS(SELECT 1 FROM inserted WHERE Id='{session:D}' AND State='Completed')
                    UPDATE g SET Enabled=0 FROM platform.UserModuleGrant g JOIN platform.Module m ON m.Id=g.ModuleId
                    WHERE g.UserId='{actor.UserId:D}' AND m.Code='FX19';
            END;
            """);
        try
        {
            await Finisher().FinishDueAsync(CancellationToken.None);
            Assert.Equal(1, await Running(session)); Assert.Equal(0, await Notifications(session)); Assert.Equal(0, await Audits(session));
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM platform.UserModuleGrant g JOIN platform.Module m ON m.Id=g.ModuleId WHERE g.UserId=@User AND m.Code='FX19' AND g.Enabled=1;", Id("@User", actor.UserId)));
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [time].FocusAuthorityChangeFixture;"); }
        await Finisher().FinishDueAsync(CancellationToken.None); await AssertCompleted(actor, session);
    }
}
