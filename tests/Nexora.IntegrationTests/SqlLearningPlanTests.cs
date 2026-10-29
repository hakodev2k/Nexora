using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Learning;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Persistence;
using Xunit;
namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlLearningPlanTests(SqlApiFixture fixture)
{
    private const string Secret="synthetic-learning-plan-receipt-key";
    private static SqlParameter Id(string name,Guid value)=>new(name,SqlDbType.UniqueIdentifier){Value=value};
    private static string Key()=>Guid.NewGuid().ToString();
    private static IdentityPrincipal Actor(SyntheticSession session)=>new(session.UserId,session.OwnerId,"User",DateTimeOffset.UtcNow,session.SessionId);
    private static T Value<T>(IdentityOperationResult<T> result) { Assert.True(result.Succeeded,$"{result.StatusCode} {result.Code}"); Assert.NotNull(result.Value); return result.Value!; }
    private static Guid Ack(IdentityOperationResult<object?> result)
    { Assert.True(result.Succeeded,$"{result.StatusCode} {result.Code}"); Assert.Null(result.Value); Assert.NotNull(result.Acknowledgement); Assert.True(result.Acknowledgement.Committed); Assert.False(result.Acknowledgement.DetailAvailable); return result.Acknowledgement.Id; }
    private async Task<Scope> Prepare()
    {
        fixture.RequireAvailable();
        var names=M01MigrationManifest.RequiredFileNames.Concat(new[]{"20261021_0086_learning_plans.sql"}).Distinct().ToArray();
        await new SqlMigrationRunner().ApplyAsync(fixture.ConnectionString!,Path.Combine(AppContext.BaseDirectory,"migrations"),names);
        // These are disposable fixture-only settings, not application activation.
        var old=await fixture.ScalarIntAsync("SELECT CONVERT(int,SystemEnabled) FROM [platform].[Module] WHERE Code='FX40'");
        var permissions=new Dictionary<string,string>();
        foreach(var action in SqlLearningPlanService.Actions)
            permissions[action]=await fixture.ScalarStringAsync("SELECT EffectiveStatus FROM [platform].[Permission] WHERE ActionKey=@Action",new SqlParameter("@Action",SqlDbType.NVarChar,160){Value=action});
        await fixture.ExecuteAsync("UPDATE [platform].[Module] SET SystemEnabled=1 WHERE Code='FX40'; UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey LIKE 'learning.plan.%';");
        return new Scope(fixture,old,permissions);
    }
    private sealed record Scope(SqlApiFixture Fixture,int Old,Dictionary<string,string> Permissions):IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Fixture.ExecuteAsync("UPDATE [platform].[Module] SET SystemEnabled=@Enabled WHERE Code='FX40'",new SqlParameter("@Enabled",SqlDbType.Bit){Value=Old==1});
            foreach(var pair in Permissions) await Fixture.ExecuteAsync("UPDATE [platform].[Permission] SET EffectiveStatus=@State WHERE ActionKey=@Action",
                new SqlParameter("@State",SqlDbType.VarChar,64){Value=pair.Value},new SqlParameter("@Action",SqlDbType.NVarChar,160){Value=pair.Key});
        }
    }
    private async Task EnableFor(SyntheticSession session)
    {
        await fixture.ExecuteAsync("""
            UPDATE [platform].[UserModuleGrant] SET Enabled=1 WHERE UserId=@User AND ModuleId IN(SELECT Id FROM [platform].[Module] WHERE Code IN('FX40','FX12','FX16'));
            INSERT [platform].[UserModuleGrant](UserId,ModuleId,Enabled)
            SELECT @User,m.Id,1 FROM [platform].[Module] m WHERE m.Code IN('FX40','FX12','FX16')
              AND NOT EXISTS(SELECT 1 FROM [platform].[UserModuleGrant] g WHERE g.UserId=@User AND g.ModuleId=m.Id);
            """,Id("@User",session.UserId));
    }
    [Fact]
    public async Task Typed_sources_owner_isolation_safe_unavailability_replay_order_and_unlink_preserve_source()
    {
        await using var scope=await Prepare(); var owner=await fixture.CreateActiveSessionAsync(); var foreign=await fixture.CreateActiveSessionAsync();
        await EnableFor(owner); await EnableFor(foreign);
        var actor=Actor(owner); var other=Actor(foreign); var factory=new SqlConnectionFactory(fixture.ConnectionString!);
        var service=new SqlLearningPlanService(factory,Secret,new(true));
        var course=Value(new SqlCourseService(factory,Secret).Create(actor,new("Synthetic Course","ManualPercent"),Key(),null)).ItemId;
        var goal=Guid.NewGuid(); var task=Guid.NewGuid(); var project=Guid.NewGuid();
        await fixture.ExecuteAsync("""
            INSERT [productivity].[Goal](Id,OwnerId,Title,Status) VALUES(@Goal,@Owner,N'Synthetic Goal','Draft');
            INSERT [productivity].[Project](Id,OwnerId,Name,Status,StartAt,EndAt) VALUES(@Project,@Owner,N'Synthetic project','NotStarted','2026-10-21','2026-10-22');
            INSERT [productivity].[Task](Id,OwnerId,ProjectId,Title,Status,StartAt,EndAt) VALUES(@Task,@Owner,@Project,N'Synthetic Task','NotStarted','2026-10-21','2026-10-22');
            """,Id("@Owner",owner.OwnerId),Id("@Goal",goal),Id("@Task",task),Id("@Project",project));
        var metadata=new LearningPlanMetadata("Synthetic Plan",new(2026,10,21),new(2026,10,22),null); var key=Key();
        var plan=Ack(service.Create(actor,metadata,key,null));
        Assert.Equal(plan,Ack(service.Create(actor,metadata,key,null)));
        Assert.Equal(409,service.Create(actor,metadata with {Title="Different intent"},key,null).StatusCode);
        Assert.Equal(404,service.Get(other,plan).StatusCode);
        Assert.Equal("No items",Value(service.Get(actor,plan)).ProgressLabel);
        foreach(var source in new[]{new LearningPlanSourceRef("Goal",goal),new("Task",task),new("Course",course)})
        {
            var before=Value(service.Get(actor,plan));
            Assert.Equal(plan,Ack(service.Link(actor,plan,before.Plan.ETag,new(source,new(2026,10,23)),Key(),null)));
        }
        var detail=Value(service.Get(actor,plan)); Assert.Equal(3,detail.Known); Assert.Equal(0,detail.Unavailable);
        Assert.All(detail.Items,item=>{Assert.Null(item.Projection.Percent); Assert.Equal("NoProgressMetric",item.Projection.Status);});
        Assert.Equal(422,service.Link(actor,plan,detail.Plan.ETag,new(new("Skill",goal),null),Key(),null).StatusCode);
        Assert.Equal(404,service.Link(other,plan,detail.Plan.ETag,new(new("Goal",goal),null),Key(),null).StatusCode);
        var otherPlan=Ack(service.Create(other,metadata,Key(),null)); var otherTag=Value(service.Get(other,otherPlan)).Plan.ETag;
        Assert.Equal(404,service.Link(other,otherPlan,otherTag,new(new("Goal",goal),null),Key(),null).StatusCode);
        Assert.Equal(409,service.Link(actor,plan,detail.Plan.ETag,new(new("Goal",goal),null),Key(),null).StatusCode);
        Assert.Equal(422,service.Reorder(actor,plan,detail.Plan.ETag,new([detail.Items[0].Id,detail.Items[0].Id]),Key(),null).StatusCode);
        var order=detail.Items.Reverse().Select(i=>i.Id).ToArray(); var reorderKey=Key();
        Ack(service.Reorder(actor,plan,detail.Plan.ETag,new(order),reorderKey,null));
        Ack(service.Reorder(actor,plan,detail.Plan.ETag,new(order),reorderKey,null)); // replay before stale validation
        Assert.Equal(order,Value(service.Get(actor,plan)).Items.Select(i=>i.Id));
        Assert.Equal(412,service.Reorder(actor,plan,detail.Plan.ETag,new(order),Key(),null).StatusCode);
        Assert.Equal(3,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[PlanItem] WHERE OwnerId=@Owner AND PlanId=@Plan",Id("@Owner",owner.OwnerId),Id("@Plan",plan)));
        await fixture.ExecuteAsync("UPDATE [productivity].[Goal] SET Status='Deleted' WHERE OwnerId=@Owner AND Id=@Goal",Id("@Owner",owner.OwnerId),Id("@Goal",goal));
        detail=Value(service.Get(actor,plan)); Assert.Equal(1,detail.Unavailable); Assert.Contains("Partially known",detail.ProgressLabel);
        var masked=detail.Items.Single(i=>i.Source.Kind=="Goal").Projection; Assert.Null(masked.Title); Assert.Null(masked.SourceState); Assert.Null(masked.Percent);
        Assert.Equal("Planned",detail.Plan.Status);
        await fixture.ExecuteAsync("UPDATE [platform].[UserModuleGrant] SET Enabled=0 WHERE UserId=@User AND ModuleId IN(SELECT Id FROM [platform].[Module] WHERE Code='FX12')",Id("@User",owner.UserId));
        detail=Value(service.Get(actor,plan)); Assert.Null(detail.Items.Single(i=>i.Source.Kind=="Task").Projection.Title);
        Assert.Equal(403,service.Sources(actor,"Task",null).StatusCode);
        var taskItem=detail.Items.Single(i=>i.Source.Kind=="Task"); Ack(service.Unlink(actor,plan,taskItem.Id,detail.Plan.ETag,Key(),null));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Task] WHERE OwnerId=@Owner AND Id=@Task AND StartAt='2026-10-21' AND EndAt='2026-10-22' AND Status='NotStarted'",Id("@Owner",owner.OwnerId),Id("@Task",task)));
        detail=Value(service.Get(actor,plan)); Ack(service.Complete(actor,plan,detail.Plan.ETag,new(true),Key(),null)); Assert.Equal("Completed",Value(service.Get(actor,plan)).Plan.Status);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [productivity].[Goal] WHERE OwnerId=@Owner AND Id=@Goal AND Status='Deleted'",Id("@Owner",owner.OwnerId),Id("@Goal",goal)));
        var foreignItem=Guid.NewGuid();
        var error=await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync("INSERT [learning].[PlanItem](Id,OwnerId,PlanId,ResourceKind,ResourceId,Position,CreatedByUserId,UpdatedByUserId) VALUES(@Item,@Foreign,@Plan,'Course',@Course,99,@User,@User)",Id("@Item",foreignItem),Id("@Foreign",foreign.OwnerId),Id("@Plan",plan),Id("@Course",course),Id("@User",foreign.UserId)));
        Assert.Equal(547,error.Number);
    }
    [Fact]
    public async Task Original_create_concurrent_replay_produces_one_root_and_owner_bound_pagination()
    {
        await using var scope=await Prepare(); var session=await fixture.CreateActiveSessionAsync(); await EnableFor(session); var actor=Actor(session);
        var service=new SqlLearningPlanService(new(fixture.ConnectionString!),Secret,new(true)); var body=new LearningPlanMetadata("Concurrent Plan",null,null,null); var key=Key();
        var writes=await Task.WhenAll(Task.Run(()=>service.Create(actor,body,key,null)),Task.Run(()=>service.Create(actor,body,key,null)));
        Assert.Equal(Ack(writes[0]),Ack(writes[1]));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [learning].[Plan] WHERE OwnerId=@Owner AND Title=N'Concurrent Plan'",Id("@Owner",session.OwnerId)));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND ActionKey='learning.plan.create'",Id("@User",session.UserId)));
        for(var n=0;n<26;n++) Ack(service.Create(actor,body with {Title=$"Page {n}"},Key(),null));
        var page=Value(service.List(actor,"Planned",null)); Assert.Equal(25,page.Items.Count); Assert.NotNull(page.NextCursor);
        var second=Value(service.List(actor,"Planned",page.NextCursor)); Assert.Equal(2,second.Items.Count); Assert.Empty(page.Items.Select(i=>i.Id).Intersect(second.Items.Select(i=>i.Id)));
        Assert.Equal(422,service.List(actor,"Completed",page.NextCursor).StatusCode);
        var foreign=Actor(await fixture.CreateActiveSessionAsync()); Assert.Equal(422,service.List(foreign,"Planned",page.NextCursor).StatusCode);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",session.SessionId));
        Assert.Equal(403,service.Create(actor,body,key,null).StatusCode);
    }
}
