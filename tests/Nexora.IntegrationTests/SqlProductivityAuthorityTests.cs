using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;
using Nexora.Application.Reminders;
using Nexora.Infrastructure.Reminders;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Xunit;
namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlProductivityAuthorityTests(SqlApiFixture fixture)
{
    private SqlProductivityService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-productivity-authority");
    private static IdentityPrincipal Actor(SyntheticSession s, string role = "User") => new(s.UserId, s.OwnerId, role, DateTimeOffset.UtcNow, s.SessionId);
    private static SqlParameter Id(string name, Guid id) => new(name, SqlDbType.UniqueIdentifier) { Value = id };
    private Task Grant(SyntheticSession s, string action) => fixture.ExecuteAsync("INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,'Allow' FROM [platform].[Permission] WHERE ActionKey=@Action;", Id("@User",s.UserId), new SqlParameter("@Action",SqlDbType.NVarChar,160){Value=action});
    private static ProjectCommand Project(string title) { var start=DateTimeOffset.UtcNow.AddDays(1); return new(title,"Synthetic project",start,start.AddDays(2)); }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fresh_keyless_and_keyed_mutations_hold_current_grant_until_commit(bool keyed)
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(session,"Admin");
        await Grant(session,"projects.project.create"); var body=Project("Current authority"); var key=keyed?Guid.NewGuid().ToString("D"):null;
        // Only the generated fixture database is instrumented; the observer is not application authority.
        await fixture.ExecuteAsync($"CREATE TRIGGER [security].[PauseProjectAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActorUserId='{session.UserId:D}' AND ActionKey='productivity.project.create') WAITFOR DELAY '00:00:05'; END;");
        Task<IdentityOperationResult<ProjectRecord>>? mutation=null;
        try
        {
            mutation=Task.Run(()=>Service().CreateProject(actor,body,key)); var limit=DateTime.UtcNow.AddSeconds(4); var reached=false;
            while(DateTime.UtcNow<limit)
            { if(await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WITH(NOLOCK) WHERE ActorUserId=@User AND ActionKey='productivity.project.create';",Id("@User",session.UserId))>0){reached=true;break;} await Task.Delay(25); }
            Assert.True(reached,"Mutation must reach the uncommitted audit boundary before the revocation attempt.");
            using var revoke=new SqlConnection(fixture.ConnectionString!); await revoke.OpenAsync();
            using var denied=new SqlCommand("SET LOCK_TIMEOUT 250; UPDATE ap SET Effect='Deny' FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey='projects.project.create';",revoke);
            denied.Parameters.Add(Id("@User",session.UserId)); var locked=await Assert.ThrowsAsync<SqlException>(async()=>await denied.ExecuteNonQueryAsync()); Assert.Equal(1222,locked.Number);
            var committed=await mutation; Assert.True(committed.Succeeded,committed.Code);
            await fixture.ExecuteAsync("UPDATE ap SET Effect='Deny' FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey='projects.project.create';",Id("@User",session.UserId));
            var rejected=Service().CreateProject(actor,body,keyed?key:null); Assert.False(rejected.Succeeded);
            Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Project] WHERE OwnerId=@Owner;",Id("@Owner",session.OwnerId)));
        }
        finally { if(mutation is not null) await mutation; await fixture.ExecuteAsync("DROP TRIGGER IF EXISTS [security].[PauseProjectAudit];"); }
    }
    [Fact]
    public async Task Task_transition_replay_uses_trusted_action_not_title_or_description_delimiters()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(session,"Admin");
        foreach(var actionKey in new[]{"projects.project.create","projects.project.read","tasks.task.create","tasks.task.complete","tasks.task.read"})await Grant(session,actionKey);
        var service=Service(); var p=service.CreateProject(actor,Project("Transition authority")); Assert.True(p.Succeeded,p.Code);
        var start=p.Value!.StartAt!.Value;
        var task=service.CreateTask(actor,new(p.Value.Id,"Title |status:InProgress","Description |status:NotStarted","NotStarted",null,start,start.AddHours(1))); Assert.True(task.Succeeded,task.Code);
        var key=Guid.NewGuid().ToString("D"); var result=service.TransitionTask(actor,task.Value!.Id,task.Value.ETag,"Completed",null,key); Assert.True(result.Succeeded,result.Code);
        // Admin has no start/revert grant: user text must not add either prerequisite on replay.
        var replay=service.TransitionTask(actor,task.Value.Id,task.Value.ETag,"Completed",null,key); Assert.True(replay.Succeeded,replay.Code); Assert.Equal(result.Value!.ETag,replay.Value!.ETag);
        Assert.Equal(2,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[TaskHistory] WHERE OwnerId=@Owner AND TaskId=@Id;",Id("@Owner",session.OwnerId),Id("@Id",task.Value.Id)));
        await fixture.ExecuteAsync("UPDATE ap SET Effect='Deny' FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey='tasks.task.complete';",Id("@User",session.UserId));
        Assert.False(service.TransitionTask(actor,task.Value.Id,task.Value.ETag,"Completed",null,key).Succeeded);
    }
    [Fact]
    public async Task Create_and_replay_without_read_return_only_committed_identity()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(session,"Admin");
        await Grant(session,"projects.project.create"); var body=Project("Private projection"); var key=Guid.NewGuid().ToString("D");
        var result=Service().CreateProject(actor,body,key);
        Assert.True(result.Succeeded,result.Code); Assert.Null(result.Value);
        Assert.NotNull(result.Acknowledgement); Assert.True(result.Acknowledgement.Committed); Assert.False(result.Acknowledgement.DetailAvailable);
        var replay=Service().CreateProject(actor,body,key);
        Assert.True(replay.Succeeded,replay.Code); Assert.Null(replay.Value); Assert.Equal(result.Acknowledgement,replay.Acknowledgement);
        await Grant(session,"projects.project.read");
        var readable=Service().CreateProject(actor,body,key);
        Assert.True(readable.Succeeded,readable.Code); Assert.NotNull(readable.Value); Assert.Equal(result.Acknowledgement.Id,readable.Value.Id);
        await fixture.ExecuteAsync("UPDATE ap SET Effect='Deny' FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey='projects.project.read';",Id("@User",session.UserId));
        var privateReplay=Service().CreateProject(actor,body,key);
        Assert.True(privateReplay.Succeeded,privateReplay.Code); Assert.Null(privateReplay.Value); Assert.Equal(result.Acknowledgement,privateReplay.Acknowledgement);
        await Grant(session,"projects.project.trash");
        var trashed=Service().DeleteProject(actor,readable.Value.Id,readable.Value.ETag,Guid.NewGuid().ToString("D"));
        Assert.True(trashed.Succeeded,trashed.Code);
        var trashedReplay=Service().CreateProject(actor,body,key);
        Assert.True(trashedReplay.Succeeded,trashedReplay.Code); Assert.Null(trashedReplay.Value); Assert.Equal(result.Acknowledgement,trashedReplay.Acknowledgement);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Project] WHERE OwnerId=@Owner;",Id("@Owner",session.OwnerId)));
    }
    [Fact]
    public async Task Task_domain_creation_can_run_while_worker_holds_owner_update_lock()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync(); var actor=Actor(session); var service=Service();
        var project=service.CreateProject(actor,Project("Worker lock compatibility")); Assert.True(project.Succeeded,project.Code);
        using var connection=new SqlConnection(fixture.ConnectionString!); await connection.OpenAsync(); using var tx=connection.BeginTransaction(IsolationLevel.Serializable);
        using(var owner=new SqlCommand("SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner;",connection,tx)) { owner.Parameters.Add(Id("@Owner",session.OwnerId)); Assert.Equal(session.OwnerId,(Guid)owner.ExecuteScalar()!); }
        var start=project.Value!.StartAt!.Value;
        var create=Task.Run(()=>service.CreateTask(actor,new(project.Value.Id,"Existing Task domain",null,"NotStarted",null,start,start.AddHours(1)),Guid.NewGuid().ToString("D")));
        var result=await create.WaitAsync(TimeSpan.FromSeconds(10)); Assert.True(result.Succeeded,result.Code); tx.Rollback();
    }
    [Theory]
    [InlineData("status")]
    [InlineData("criteria")]
    [InlineData("rank")]
    [InlineData("reminder")]
    public async Task Generic_task_update_cannot_mutate_dedicated_fields(string field)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var project = service.CreateProject(actor, Project("Task boundary")); Assert.True(project.Succeeded, project.Code);
        var start = project.Value!.StartAt!.Value;
        var created = service.CreateTask(actor, new(project.Value.Id, "Task boundary", null, "NotStarted", null, start, start.AddHours(1)));
        Assert.True(created.Succeeded, created.Code); var task = created.Value!;
        var body = new TaskCommand(task.ProjectId, task.Title, task.Description, task.Status, task.DueAt, task.StartAt, task.EndAt,
            task.Priority, task.TagsJson, task.AcceptanceCriteriaJson, task.Rank, task.ReminderAt);
        body = field switch {
            "status" => body with { Status = "Completed" },
            "criteria" => body with { AcceptanceCriteriaJson = "[\"new criterion\"]" },
            "rank" => body with { Rank = 100 },
            _ => body with { ReminderAt = start.AddMinutes(-15), ManageReminder = true }
        };
        var rejected = service.UpdateTask(actor, task.Id, task.ETag, body, Guid.NewGuid().ToString("D"));
        Assert.False(rejected.Succeeded); Assert.Equal("DedicatedCommandRequired", rejected.Code);
        var unchanged = service.GetTask(actor, task.Id); Assert.Equal(task.ETag, unchanged.Value!.ETag);
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[TaskHistory] WHERE OwnerId=@Owner AND TaskId=@Id;", Id("@Owner",session.OwnerId),Id("@Id",task.Id)));
    }

    [Fact]
    public async Task Dedicated_criteria_and_rank_replay_preserve_task_topology_and_history()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync(); var actor=Actor(session); var service=Service();
        var project=service.CreateProject(actor,Project("Dedicated commands")); Assert.True(project.Succeeded,project.Code);
        var start=project.Value!.StartAt!.Value;
        var created=service.CreateTask(actor,new(project.Value.Id,"Dedicated task",null,"InProgress",null,start,start.AddHours(1))); Assert.True(created.Succeeded,created.Code);
        var task=created.Value!; var key=Guid.NewGuid().ToString("D");
        var criteria=service.SetTaskCriteria(actor,task.Id,task.ETag,"[\"criterion\"]",key); Assert.True(criteria.Succeeded,criteria.Code);
        var replay=service.SetTaskCriteria(actor,task.Id,task.ETag,"[\"criterion\"]",key); Assert.True(replay.Succeeded,replay.Code); Assert.Equal(criteria.Value!.ETag,replay.Value!.ETag);
        var rank=service.ReorderTask(actor,task.Id,criteria.Value.ETag,42,Guid.NewGuid().ToString("D")); Assert.True(rank.Succeeded,rank.Code);
        Assert.Equal(task.ProjectId,rank.Value!.ProjectId); Assert.Equal("InProgress",rank.Value.Status); Assert.Equal(42,rank.Value.Rank);
        Assert.Equal(criteria.Value.AcceptanceCriteriaJson,rank.Value.AcceptanceCriteriaJson);
        Assert.Equal(3,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[TaskHistory] WHERE OwnerId=@Owner AND TaskId=@Id;",Id("@Owner",session.OwnerId),Id("@Id",task.Id)));
        Assert.False(service.ReorderTask(actor,task.Id,task.ETag,43).Succeeded);
    }

    [Fact]
    public async Task Delimiter_payloads_do_not_share_task_receipt_digest()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync(); var actor=Actor(session); var service=Service();
        var project=service.CreateProject(actor,Project("Digest collision")); Assert.True(project.Succeeded,project.Code); var start=project.Value!.StartAt!.Value;
        var first=new TaskCommand(project.Value.Id,"A|description:B","C","NotStarted",null,start,start.AddHours(1)); var key=Guid.NewGuid().ToString("D");
        Assert.True(service.CreateTask(actor,first,key).Succeeded);
        var conflict=service.CreateTask(actor,first with { Title="A",Description="B|description:C" },key);
        Assert.False(conflict.Succeeded); Assert.Equal("IdempotencyConflict",conflict.Code);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Task] WHERE OwnerId=@Owner;",Id("@Owner",session.OwnerId)));
    }

    [Fact]
    public async Task Terminal_task_revert_requires_revert_not_start_and_replay_retains_that_authority()
    {
        fixture.RequireAvailable(); var s=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(s,"Admin");
        foreach(var action in new[]{"projects.project.create","projects.project.read","tasks.task.create","tasks.task.read","tasks.task.complete","tasks.task.revert"}) await Grant(s,action);
        var service=Service(); var p=service.CreateProject(actor,Project("Revert authority")); Assert.True(p.Succeeded,p.Code);
        var start=p.Value!.StartAt!.Value; var task=service.CreateTask(actor,new(p.Value.Id,"Revert",null,"NotStarted",null,start,start.AddHours(1))); Assert.True(task.Succeeded,task.Code);
        var completed=service.TransitionTask(actor,task.Value!.Id,task.Value.ETag,"Completed",null); Assert.True(completed.Succeeded,completed.Code);
        var key=Guid.NewGuid().ToString("D"); var reverted=service.TransitionTask(actor,task.Value.Id,completed.Value!.ETag,"InProgress","Resume work",key); Assert.True(reverted.Succeeded,reverted.Code);
        var replay=service.TransitionTask(actor,task.Value.Id,completed.Value.ETag,"InProgress","Resume work",key); Assert.True(replay.Succeeded,replay.Code); Assert.Equal(reverted.Value!.ETag,replay.Value!.ETag);
        var changedReason=service.TransitionTask(actor,task.Value.Id,completed.Value.ETag,"InProgress","Other reason",key); Assert.False(changedReason.Succeeded); Assert.Equal("IdempotencyConflict",changedReason.Code);
        await fixture.ExecuteAsync("UPDATE ap SET Effect='Deny' FROM [platform].[AdminPermission] ap JOIN [platform].[Permission] p ON p.Id=ap.PermissionId WHERE ap.UserId=@User AND p.ActionKey='tasks.task.revert';",Id("@User",s.UserId));
        Assert.False(service.TransitionTask(actor,task.Value.Id,completed.Value.ETag,"InProgress","Resume work",key).Succeeded);
    }

    [Fact]
    public async Task Reminder_cannot_use_update_permission_or_whitespace_to_bypass_remove_and_has_safe_ack()
    {
        fixture.RequireAvailable(); var s=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(s,"Admin");
        foreach(var action in new[]{"projects.project.create","projects.project.read","tasks.task.create","tasks.task.read","tasks.task.update","reminders.configuration.set"}) await Grant(s,action);
        var service=Service(); var p=service.CreateProject(actor,Project("Reminder permissions")); Assert.True(p.Succeeded,p.Code);
        var start=p.Value!.StartAt!.Value; var task=service.CreateTask(actor,new(p.Value.Id,"Reminder",null,"NotStarted",null,start,start.AddHours(1))); Assert.True(task.Succeeded,task.Code);
        var reminders=new SqlReminderService(new SqlConnectionFactory(fixture.ConnectionString!),"synthetic-reminder-authority-key");
        var request=new ReminderSetCommand("BeforeStart15m",null,task.Value!.ETag); var key=Guid.NewGuid().ToString("D");
        Assert.False(reminders.Set(actor,"Task",task.Value.Id,null,request,key).Succeeded);
        await Grant(s,"tasks.task.set_reminder");
        var saved=reminders.Set(actor,"Task",task.Value.Id,null,request,key); Assert.True(saved.Succeeded,saved.Code); Assert.Null(saved.Value); Assert.Equal(task.Value.Id,saved.Acknowledgement!.Id);
        var replay=reminders.Set(actor,"Task",task.Value.Id,null,request,key); Assert.True(replay.Succeeded,replay.Code); Assert.Null(replay.Value); Assert.Equal(saved.Acknowledgement,replay.Acknowledgement);
        await Grant(s,"reminders.configuration.read"); var current=reminders.Get(actor,"Task",task.Value.Id); Assert.True(current.Succeeded,current.Code);
        await Grant(s,"reminders.configuration.remove");
        var denied=reminders.Set(actor,"Task",task.Value.Id,current.Value!.Reminder!.ETag,new(" None ",null,current.Value.SourceETag),Guid.NewGuid().ToString("D"));
        Assert.False(denied.Succeeded); Assert.Equal(current.Value.Reminder.ETag,reminders.Get(actor,"Task",task.Value.Id).Value!.Reminder!.ETag);
        await Grant(s,"tasks.task.remove_reminder"); await Grant(s,"tasks.task.complete");
        var completed=service.TransitionTask(actor,task.Value.Id,current.Value.SourceETag,"Completed",null); Assert.True(completed.Succeeded,completed.Code);
        var terminal=reminders.Get(actor,"Task",task.Value.Id); Assert.True(terminal.Succeeded,terminal.Code);
        var removed=reminders.Remove(actor,"Task",task.Value.Id,terminal.Value!.Reminder!.ETag,new(terminal.Value.SourceETag)); Assert.False(removed.Succeeded); Assert.Equal("LifecycleLocked",removed.Code);
    }

    [Fact]
    public async Task Event_generic_status_is_denied_and_terminal_transition_replays()
    {
        fixture.RequireAvailable(); var s=await fixture.CreateActiveSessionAsync(); var actor=Actor(s); var service=Service(); var start=DateTimeOffset.UtcNow.AddDays(1);
        var command=new EventCommand("Event","Synthetic event",start,start.AddHours(1),"Asia/Ho_Chi_Minh");
        var created=service.CreateEvent(actor,command); Assert.True(created.Succeeded,created.Code); var item=created.Value!;
        var bypass=service.UpdateEvent(actor,item.Id,item.ETag,command with { Status="Completed" }); Assert.False(bypass.Succeeded); Assert.Equal("DedicatedCommandRequired",bypass.Code);
        var key=Guid.NewGuid().ToString("D"); var done=service.TransitionEvent(actor,item.Id,item.ETag,"Completed",key); Assert.True(done.Succeeded,done.Code);
        var replay=service.TransitionEvent(actor,item.Id,item.ETag,"Completed",key); Assert.True(replay.Succeeded,replay.Code); Assert.Equal(done.Value!.ETag,replay.Value!.ETag);
    }

}
