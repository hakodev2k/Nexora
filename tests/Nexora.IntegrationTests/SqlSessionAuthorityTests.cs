using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;
using Nexora.Infrastructure.Focus;
using Nexora.Infrastructure.Organization;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Xunit;

namespace Nexora.IntegrationTests;

// Authorized by the current user attachment, section 7, which supersedes
// older code-only amendments. All records belong to SqlApiFixture's generated
// local test database; no production or retained development data is used.
[Collection(SqlApiCollection.Name)]
public sealed class SqlSessionAuthorityTests(SqlApiFixture fixture)
{
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static IdentityPrincipal Actor(SyntheticSession session) =>
        new(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow, session.SessionId);

    [Theory]
    [InlineData("missing")]
    [InlineData("revoked")]
    [InlineData("expired")]
    [InlineData("stamp")]
    [InlineData("absolute")]
    [InlineData("wrong-user")]
    [InlineData("unverified")]
    public async Task Self_requests_require_current_session_across_modules(string invalidation)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = Actor(session);
        if (invalidation == "missing") actor = actor with { SessionId = null };
        else if (invalidation == "wrong-user") actor = actor with { SessionId = (await fixture.CreateActiveSessionAsync()).SessionId };
        else if (invalidation == "unverified")
            await fixture.ExecuteAsync("UPDATE [identity].[User] SET State='PendingVerification',EmailConfirmed=0,VerifiedAt=NULL WHERE Id=@User;", Id("@User", session.UserId));
        else
        {
            var assignment = invalidation switch
            {
                "revoked" => "RevokedAt=SYSUTCDATETIME()",
                "expired" => "IdleExpiresAt=DATEADD(minute,-1,SYSUTCDATETIME())",
                "absolute" => "AbsoluteExpiresAt=DATEADD(minute,-1,SYSUTCDATETIME())",
                _ => "SecurityStamp=NEWID()"
            };
            await fixture.ExecuteAsync("UPDATE [identity].[Session] SET " + assignment + " WHERE Id=@Id;", Id("@Id", session.SessionId));
        }
        var service = new SqlProductivityService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-session-authority");
        var start = DateTimeOffset.UtcNow.AddDays(1);
        Assert.False(service.CreateProject(actor, new ProjectCommand("Denied", "Synthetic project", start, start.AddDays(1)), Guid.NewGuid().ToString("D")).Succeeded);
        Assert.False(service.ListProjects(actor).Succeeded);
        Assert.False(new SqlTagService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-session-tags").List(actor, "projects").Succeeded);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Project] WHERE OwnerId=@Owner;", Id("@Owner", session.OwnerId)));
    }

    [Fact]
    public async Task Session_expiry_during_execution_rolls_back_mutation_and_receipt()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var service = new SqlProductivityService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-expiry-authority");
        var key = Guid.NewGuid().ToString("D");
        // Only this generated fixture database is instrumented. NOLOCK is an
        // observation barrier, never application authorization or evidence of commit.
        await fixture.ExecuteAsync($"CREATE TRIGGER [security].[PauseExpiryAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActorUserId='{session.UserId:D}' AND ActionKey='productivity.project.create') WAITFOR DELAY '00:00:05'; END;");
        Task<IdentityOperationResult<ProjectRecord>>? mutation = null;
        try
        {
            await fixture.ExecuteAsync("UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(second,3,SYSUTCDATETIME()) WHERE Id=@Id;", Id("@Id", session.SessionId));
            var start = DateTimeOffset.UtcNow.AddDays(1);
            mutation = Task.Run(() => service.CreateProject(Actor(session), new("Expiry boundary", "Synthetic project", start, start.AddDays(1)), key));
            var reached = false;
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                if (await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WITH(NOLOCK) WHERE ActorUserId=@User AND ActionKey='productivity.project.create';", Id("@User", session.UserId)) > 0) { reached = true; break; }
                await Task.Delay(25);
            }
            Assert.True(reached, "Mutation must reach the in-transaction barrier before session expiry.");
            var result = await mutation;
            Assert.False(result.Succeeded);
            Assert.Null(result.Acknowledgement);
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Project] WHERE OwnerId=@Owner;", Id("@Owner", session.OwnerId)));
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND ActionKey='productivity.project.create';", Id("@User", session.UserId)));
            await fixture.ExecuteAsync("UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(hour,1,SYSUTCDATETIME()) WHERE Id=@Id;", Id("@Id", session.SessionId));
            // A rolled-back request leaves no successful receipt; the same key
            // must execute exactly once after the session becomes valid again.
            var retried = service.CreateProject(Actor(session), new("Expiry boundary", "Synthetic project", start, start.AddDays(1)), key);
            Assert.True(retried.Succeeded, retried.Code);
            Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Project] WHERE OwnerId=@Owner;", Id("@Owner", session.OwnerId)));
        }
        finally
        {
            if (mutation is not null) await mutation;
            await fixture.ExecuteAsync("DROP TRIGGER IF EXISTS [security].[PauseExpiryAudit];");
        }
    }

    [Fact]
    public async Task Trusted_focus_completion_does_not_borrow_a_request_session()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = Actor(session);
        var focus = new SqlFocusService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-focus-authority");
        var started = focus.Start(actor, new("ShortBreak"), Guid.NewGuid().ToString("D"));
        Assert.True(started.Succeeded, started.Code);
        await fixture.ExecuteAsync("UPDATE [time].[FocusSession] SET StartedAt=DATEADD(second,-PlannedSeconds-1,SYSUTCDATETIME()),LastRunAt=DATEADD(second,-PlannedSeconds-1,SYSUTCDATETIME()) WHERE OwnerId=@Owner AND Id=@Id;", Id("@Owner", session.OwnerId), Id("@Id", started.Value!.Id));
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Id;", Id("@Id", session.SessionId));
        Assert.False(focus.List(actor, null, false).Succeeded);
        await focus.FinishDueAsync(CancellationToken.None);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[FocusSession] WHERE OwnerId=@Owner AND Id=@Id AND State='Completed';", Id("@Owner", session.OwnerId), Id("@Id", started.Value.Id)));
    }

    [Fact]
    public async Task Task_api_rejects_generic_semantic_fields_and_uses_dedicated_commands()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = Actor(session);
        var service = new SqlProductivityService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-task-api");
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var project = service.CreateProject(actor, new("API boundary", "Synthetic project", start, start.AddDays(1)));
        Assert.True(project.Succeeded, project.Code);
        var created = service.CreateTask(actor, new(project.Value!.Id, "API Task", null, "NotStarted", null, start, start.AddHours(1)));
        Assert.True(created.Succeeded, created.Code);
        var task = created.Value!;
        var csrf = await fixture.GetCsrfAsync();
        using var rejected = await fixture.SendJsonAsync(HttpMethod.Put, "/api/v1/tasks/" + task.Id,
            new { title = task.Title, description = (string?)null, dueAt = (DateTimeOffset?)null, startAt = start, endAt = start.AddHours(1), status = "Completed" },
            csrf, Guid.NewGuid(), session.RawSessionHandle, task.ETag);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(task.ETag, service.GetTask(actor, task.Id).Value!.ETag);
        using var reordered = await fixture.SendJsonAsync(HttpMethod.Put, "/api/v1/tasks/" + task.Id + "/rank",
            new { rank = 7 }, csrf, Guid.NewGuid(), session.RawSessionHandle, task.ETag);
        Assert.Equal(System.Net.HttpStatusCode.OK, reordered.StatusCode);
        var current = service.GetTask(actor, task.Id).Value!;
        Assert.Equal(7, current.Rank);
        Assert.Equal("NotStarted", current.Status);
    }

    [Fact]
    public async Task Private_query_rechecks_session_after_waiting_for_resource_lock()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var service = new SqlProductivityService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-query-authority");
        var start = DateTimeOffset.UtcNow.AddDays(1);
        var created = service.CreateProject(Actor(session), new("Query boundary", "Private synthetic body", start, start.AddDays(1)));
        Assert.True(created.Succeeded, created.Code);
        using var blocker = new SqlConnection(fixture.ConnectionString!);
        await blocker.OpenAsync();
        using var transaction = blocker.BeginTransaction(IsolationLevel.Serializable);
        using (var locked = new SqlCommand("UPDATE [productivity].[Project] SET Name=Name WHERE OwnerId=@Owner AND Id=@Id;", blocker, transaction))
        {
            locked.Parameters.Add(Id("@Owner", session.OwnerId)); locked.Parameters.Add(Id("@Id", created.Value!.Id));
            Assert.Equal(1, await locked.ExecuteNonQueryAsync());
        }
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(second,3,SYSUTCDATETIME()) WHERE Id=@Id;", Id("@Id", session.SessionId));
        Task<IdentityOperationResult<ProjectRecord>>? query = null;
        var released = false;
        try
        {
            query = Task.Run(() => service.GetProject(Actor(session), created.Value!.Id));
            using var observer = new SqlConnection(fixture.ConnectionString!); await observer.OpenAsync();
            var reached = false; var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline)
            {
                using var probe = new SqlCommand("SET LOCK_TIMEOUT 100; UPDATE [identity].[Session] SET SecurityStamp=SecurityStamp WHERE Id=@Id;", observer);
                probe.Parameters.Add(Id("@Id", session.SessionId));
                try { await probe.ExecuteNonQueryAsync(); }
                catch (SqlException error) when (error.Number == 1222) { reached = true; break; }
                await Task.Delay(25);
            }
            Assert.True(reached, "Query must hold current session authority while blocked on its resource.");
            await Task.Delay(3500);
            transaction.Rollback(); released = true;
            var result = await query;
            Assert.False(result.Succeeded); Assert.Null(result.Value); Assert.Null(result.Acknowledgement);
        }
        finally
        {
            if (!released) transaction.Rollback();
            if (query is not null) await query;
        }
    }
}
