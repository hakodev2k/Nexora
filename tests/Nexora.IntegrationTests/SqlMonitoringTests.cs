using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Monitoring;
using Nexora.Infrastructure.Monitoring;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlMonitoringTests
{
    private readonly SqlApiFixture fixture;
    public SqlMonitoringTests(SqlApiFixture fixture)=>this.fixture=fixture;
    private const string Root="/api/v1/monitoring/monitors";
    private static SqlParameter Id(string name,Guid value)=>new(name,SqlDbType.UniqueIdentifier){Value=value};
    private static Guid I(JsonElement item)=>item.GetProperty("id").GetGuid();
    private static string E(JsonElement item)=>item.GetProperty("etag").GetString()!;
    private static void Denied<T>(IdentityOperationResult<T> result){Assert.False(result.Succeeded);Assert.Equal(403,result.StatusCode);Assert.Null(result.Value);}
    private static object Metadata(string title="Synthetic monitor",string target="https://monitor.example.com/health",int interval=17,int? expected=null)=>new{schemaVersion=1,title,kind="Http",target,intervalSeconds=interval,expectedStatus=expected};
    private static async Task<JsonElement> Json(HttpResponseMessage response,HttpStatusCode expected=HttpStatusCode.OK)
    {Assert.True(response.StatusCode==expected,$"Expected {expected}, got {response.StatusCode}; private payload excluded.");using var d=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return d.RootElement.Clone();}
    private async Task<HttpResponseMessage> Send(SyntheticSession actor,HttpMethod method,string path,object body,string? etag=null,Guid? key=null)=>await fixture.SendJsonAsync(method,path,body,await fixture.GetCsrfAsync(),key??Guid.NewGuid(),actor.RawSessionHandle,etag);
    private async Task<JsonElement> Get(SyntheticSession actor,string path)
    {using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,actor.RawSessionHandle);return await Json(response);}
    private async Task<JsonElement> Create(SyntheticSession actor,string title="Synthetic monitor",bool enabled=true)
    {using var response=await Send(actor,HttpMethod.Post,Root,new{metadata=Metadata(title),enabled});return await Json(response,HttpStatusCode.Created);}
    private sealed record Scope(SqlMonitoringTests Tests,JsonElement Original):IAsyncDisposable
    {
        public async ValueTask DisposeAsync()=>await Tests.fixture.ExecuteAsync("UPDATE [platform].[Module] SET State=@State,SystemEnabled=@System,RegistrationEnabled=@Registration WHERE Code='FX36'",new SqlParameter("@State",SqlDbType.VarChar,32){Value=Original.GetProperty("state").GetString()},new SqlParameter("@System",SqlDbType.Bit){Value=Original.GetProperty("systemEnabled").GetBoolean()},new SqlParameter("@Registration",SqlDbType.Bit){Value=Original.GetProperty("registrationEnabled").GetBoolean()});
    }
    private async Task<Scope> Enable()
    {
        fixture.RequireAvailable();var super=await fixture.CreateActiveSessionAsync("SuperAdmin");var original=(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(x=>x.GetProperty("code").GetString()=="FX36").Clone();
        await fixture.ExecuteAsync("UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1 WHERE Code='FX36'");return new(this,original);
    }
    private async Task Grant(SyntheticSession super,SyntheticSession actor,string effect,params string[] keys)
    {
        using var current=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/admin/users/{actor.UserId}/access",super.RawSessionHandle);await Json(current);
        var changes=keys.Select(actionKey=>new{actionKey,effect}).ToArray();using var previewResponse=await Send(super,HttpMethod.Post,$"/api/v1/admin/users/{actor.UserId}/access/preview",new{kind="permissions",changes});var preview=await Json(previewResponse);
        using var commit=await Send(super,HttpMethod.Put,$"/api/v1/admin/users/{actor.UserId}/access/permissions",new{kind="permissions",changes,previewToken=preview.GetProperty("previewToken").GetString()},current.Headers.ETag?.Tag);await Json(commit);
    }

    [Fact] public async Task Configuration_is_typed_private_and_never_fabricates_an_observation()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();using var created=await Send(owner,HttpMethod.Post,Root,new{metadata=Metadata("  Synthetic HTTP monitor  ","HTTPS://MONITOR.EXAMPLE.COM:443/health",2147483647,204),enabled=true});var ack=await Json(created,HttpStatusCode.Created);
        Assert.Equal(new[]{"enabled","etag","id","state"},ack.EnumerateObject().Select(p=>p.Name).Order().ToArray());Assert.Equal("Unknown",ack.GetProperty("state").GetString());Assert.DoesNotContain("target",await created.Content.ReadAsStringAsync());
        var item=await Get(owner,$"{Root}/{I(ack)}");Assert.Equal("Synthetic HTTP monitor",item.GetProperty("metadata").GetProperty("title").GetString());Assert.Equal("https://monitor.example.com/health",item.GetProperty("metadata").GetProperty("target").GetString());Assert.Equal(2147483647,item.GetProperty("metadata").GetProperty("intervalSeconds").GetInt32());Assert.Equal(JsonValueKind.Null,item.GetProperty("lastObservedAt").ValueKind);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [monitoring].[Monitor] WHERE OwnerId=@Owner AND Id=@Id AND State='Unknown' AND LastObservedAt IS NULL AND HeartbeatTokenHash IS NULL",Id("@Owner",owner.OwnerId),Id("@Id",I(ack))));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId WHERE r.OwnerId=@Owner AND r.Id=@Id AND r.Availability='Active' AND rt.ContractVersion='monitoring-http-config-v1' AND JSON_VALUE(rt.CapabilitiesJson,'$.share')='false' AND JSON_VALUE(rt.CapabilitiesJson,'$.support')='false'",Id("@Owner",owner.OwnerId),Id("@Id",I(ack))));
        foreach(var assignment in new[]{"LastObservedAt=SYSUTCDATETIME()","HeartbeatTokenHash=CONVERT(binary(32),1)","State='Up'","Kind='CronHeartbeat'","IntervalSeconds=0","ExpectedStatus=600"})await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync($"UPDATE [monitoring].[Monitor] SET {assignment} WHERE Id=@Id",Id("@Id",I(ack))));
        using var unsupported=await Send(owner,HttpMethod.Post,$"{Root}/{I(ack)}/check",new{});Assert.Equal(HttpStatusCode.NotFound,unsupported.StatusCode);
    }
    [Fact] public async Task Binding_requires_explicit_choices_and_rejects_privileged_extra_fields()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();
        foreach(var body in new object[]{new{metadata=Metadata()},new{metadata=new{schemaVersion=1,title="Synthetic",kind="Http",target="https://monitor.example.com/",intervalSeconds=10},enabled=true},new{metadata=Metadata(),enabled=true,ownerId=owner.OwnerId},new{metadata=Metadata(),enabled=true,state="Up"},new{metadata=Metadata(),enabled=true,lastObservedAt="2026-10-04T00:00:00Z"}})
        {using var bad=await Send(owner,HttpMethod.Post,Root,body);Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);}
        using var zero=await Send(owner,HttpMethod.Post,Root,new{metadata=Metadata(interval:0),enabled=false});Assert.Equal(HttpStatusCode.UnprocessableEntity,zero.StatusCode);
        var created=await Create(owner,enabled:false);using var injected=await Send(owner,HttpMethod.Put,$"{Root}/{I(created)}",new{metadata=Metadata(),enabled=true},E(created));Assert.Equal(HttpStatusCode.BadRequest,injected.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [monitoring].[Monitor] WHERE OwnerId=@Owner AND Enabled=0 AND State='Paused'",Id("@Owner",owner.OwnerId)));
    }
    [Theory]
    [InlineData("http://127.0.0.1/")][InlineData("http://2130706433/")][InlineData("http://0x7f000001/")]
    [InlineData("http://[::1]/")][InlineData("http://localhost/")][InlineData("http://host.local/")]
    [InlineData("https://user:password@example.com/")][InlineData("https://example.com/?token=hidden")]
    [InlineData("https://example.com/#fragment")][InlineData("https://example.com:8443/")]
    [InlineData("https://%65xample.com/")][InlineData("https://example.com\\@localhost/")]
    public async Task Lexical_only_target_boundaries_reject_unsafe_representations_without_persisting(string target)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();using var bad=await Send(owner,HttpMethod.Post,Root,new{metadata=Metadata(target:target),enabled=true});Assert.Equal(HttpStatusCode.UnprocessableEntity,bad.StatusCode);Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [monitoring].[Monitor] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
    }
    [Fact] public async Task Filtering_and_real_cursor_paging_never_include_foreign_or_inactive_records()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();var marker="monitor_"+Guid.NewGuid().ToString("N")+"_%[";var ids=new HashSet<Guid>();
        for(var index=0;index<31;index++)ids.Add(I(await Create(owner,marker+index.ToString("D2"))));var foreign=await Create(other,marker+"foreign");var filtered=await Create(owner,"Unmatched synthetic monitor");
        var path=Root+"?query="+Uri.EscapeDataString(marker);var first=await Get(owner,path);Assert.Equal(25,first.GetProperty("items").GetArrayLength());var second=await Get(owner,path+"&cursor="+first.GetProperty("nextCursor").GetGuid());Assert.Equal(6,second.GetProperty("items").GetArrayLength());Assert.Equal(JsonValueKind.Null,second.GetProperty("nextCursor").ValueKind);
        var combined=first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).ToArray();Assert.Equal(31,combined.Select(I).Distinct().Count());Assert.True(ids.SetEquals(combined.Select(I)));var titles=combined.Select(x=>x.GetProperty("metadata").GetProperty("title").GetString()).ToArray();Assert.Equal(titles.Order().ToArray(),titles);
        foreach(var cursor in new[]{I(foreign),I(filtered),Guid.NewGuid()}){using var bad=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path+"&cursor="+cursor,owner.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,bad.StatusCode);}
        using var foreignRead=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{I(foreign)}",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,foreignRead.StatusCode);Assert.DoesNotContain("foreign",await foreignRead.Content.ReadAsStringAsync());
        await fixture.ExecuteAsync("UPDATE [platform].[Resource] SET Availability='Trash' WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",I(filtered)));using var inactive=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{I(filtered)}",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,inactive.StatusCode);
        Assert.Empty((await Get(owner,path+"&state=Paused")).GetProperty("items").EnumerateArray());
    }
    [Fact] public async Task Metadata_receipt_replays_original_ack_after_later_writes_and_checks_new_versions_only()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner);var id=I(created);var key=Guid.NewGuid();var body=new{metadata=Metadata("Updated private monitor",interval:23,expected:200)};
        using var update=await Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created),key);var ack=await Json(update);using var pause=await Send(owner,HttpMethod.Post,$"{Root}/{id}/pause",new{confirmed=true},E(ack));var paused=await Json(pause);
        using var replay=await Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created),key);var original=await Json(replay);Assert.Equal(ack.GetRawText(),original.GetRawText());Assert.Equal(E(paused),E(await Get(owner,$"{Root}/{id}")));
        using var conflict=await Send(owner,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata("Different")},E(created),key);Assert.Equal(HttpStatusCode.Conflict,conflict.StatusCode);
        using var stale=await Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created));Assert.Equal(HttpStatusCode.PreconditionFailed,stale.StatusCode);using var missing=await Send(owner,HttpMethod.Put,$"{Root}/{id}",body);Assert.Equal((HttpStatusCode)428,missing.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='monitoring.monitor.update'",Id("@Id",id)));
    }
    [Fact] public async Task Pause_resume_noop_and_audit_failure_keep_preferences_registry_and_receipts_atomic()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner,enabled:false);var id=I(created);var before=await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id",Id("@Id",id));
        using var noConfirm=await Send(owner,HttpMethod.Post,$"{Root}/{id}/resume",new{confirmed=false},E(created));Assert.Equal(HttpStatusCode.UnprocessableEntity,noConfirm.StatusCode);
        var noopKey=Guid.NewGuid();using var noop=await Send(owner,HttpMethod.Post,$"{Root}/{id}/pause",new{confirmed=true},E(created),noopKey);var unchanged=await Json(noop);Assert.Equal(E(created),E(unchanged));Assert.Equal(before,await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id",Id("@Id",id)));Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='monitoring.monitor.pause'",Id("@Id",id)));
        await fixture.ExecuteAsync("CREATE TRIGGER [security].[RejectSyntheticMonitorAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='monitoring.monitor.resume') THROW 51046,'Synthetic audit failure',1; END;");var key=Guid.NewGuid();
        try{using var failed=await Send(owner,HttpMethod.Post,$"{Root}/{id}/resume",new{confirmed=true},E(created),key);Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);Assert.Equal(E(created),E(await Get(owner,$"{Root}/{id}")));Assert.Equal(before,await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id",Id("@Id",id)));}
        finally{await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectSyntheticMonitorAudit]");}
        using var resume=await Send(owner,HttpMethod.Post,$"{Root}/{id}/resume",new{confirmed=true},E(created),key);var ack=await Json(resume);Assert.True(ack.GetProperty("enabled").GetBoolean());Assert.Equal("Unknown",ack.GetProperty("state").GetString());using var noopReplay=await Send(owner,HttpMethod.Post,$"{Root}/{id}/pause",new{confirmed=true},E(created),noopKey);Assert.Equal(unchanged.GetRawText(),(await Json(noopReplay)).GetRawText());
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='monitoring.monitor.resume'",Id("@Id",id)));
    }
    [Theory]
    [InlineData("read")][InlineData("create")][InlineData("update")][InlineData("pause")][InlineData("resume")]
    public async Task Admin_requires_each_exact_action_and_update_independently_requires_read(string name)
    {
        await using var scope=await Enable();var super=await fixture.CreateActiveSessionAsync("SuperAdmin");var actor=await fixture.CreateActiveSessionAsync("Admin");
        using(var absent=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root+"/capabilities",actor.RawSessionHandle))Assert.Equal(HttpStatusCode.Forbidden,absent.StatusCode);
        using(var absentCreate=await Send(actor,HttpMethod.Post,Root,new{metadata=Metadata(),enabled=true}))Assert.Equal(HttpStatusCode.Forbidden,absentCreate.StatusCode);
        await Grant(super,actor,"Allow",SqlMonitoringService.Actions);var created=await Create(actor,enabled:name!="resume");var id=I(created);var action="monitoring.monitor."+name;await Grant(super,actor,"Deny",action);
        HttpResponseMessage denied=name switch{"read"=>await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{id}",actor.RawSessionHandle),"create"=>await Send(actor,HttpMethod.Post,Root,new{metadata=Metadata(),enabled=true}),"update"=>await Send(actor,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata("Changed")},E(created)),_=>await Send(actor,HttpMethod.Post,$"{Root}/{id}/{name}",new{confirmed=true},E(created))};using(denied)Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        if(name=="read"){using var alsoDenied=await Send(actor,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata("Changed")},E(created));Assert.Equal(HttpStatusCode.Forbidden,alsoDenied.StatusCode);}
        await Grant(super,actor,"Allow",action);Assert.True((await Get(actor,Root+"/capabilities")).GetProperty(action).GetBoolean());
        var source=await Get(actor,$"{Root}/{id}");Assert.Equal("Synthetic monitor",source.GetProperty("metadata").GetProperty("title").GetString());
    }
    [Fact] public async Task Current_SQL_role_owner_dependency_and_revoked_session_override_stale_principals()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner);var source=new SqlMonitoringService(new SqlConnectionFactory(fixture.ConnectionString!),"synthetic-monitor-receipt-secret");var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,"User",DateTimeOffset.UtcNow,owner.SessionId);
        Assert.True(source.Get(actor,I(created)).Succeeded);
        await fixture.ExecuteAsync("UPDATE [platform].[UserModuleGrant] SET Enabled=0 WHERE UserId=@User AND ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX01')",Id("@User",owner.UserId));Assert.False(source.Get(actor,I(created)).Succeeded);await fixture.ExecuteAsync("UPDATE [platform].[UserModuleGrant] SET Enabled=1 WHERE UserId=@User AND ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX01')",Id("@User",owner.UserId));
        await fixture.ExecuteAsync("INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='Admin'",Id("@User",owner.UserId));Assert.False(source.Get(actor,I(created)).Succeeded);await fixture.ExecuteAsync("DELETE [identity].[UserRole] WHERE UserId=@User AND RoleId=(SELECT Id FROM [identity].[Role] WHERE Code='Admin')",Id("@User",owner.UserId));
        var wrong=await fixture.CreateActiveSessionAsync();Assert.False(source.Get(actor with{OwnerId=wrong.OwnerId},I(created)).Succeeded);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",owner.SessionId));Assert.False(source.Get(actor,I(created)).Succeeded);Assert.False(source.Update(actor,I(created),E(created),new(new(1,"Hidden","Http","https://monitor.example.com/",1,null)),Guid.NewGuid().ToString(),null).Succeeded);
        using var anonymous=await fixture.Client.GetAsync(Root);Assert.Equal(HttpStatusCode.Unauthorized,anonymous.StatusCode);
    }
    [Theory]
    [InlineData("stamp")][InlineData("idle")][InlineData("absolute")][InlineData("deleted")]
    [InlineData("moduleGrant")][InlineData("space")][InlineData("unverified")]
    public async Task Current_account_and_session_boundaries_deny_reads_and_receipt_replay(string boundary)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner);
        var source=new SqlMonitoringService(new SqlConnectionFactory(fixture.ConnectionString!),"synthetic-monitor-receipt-secret");
        var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,"User",DateTimeOffset.UtcNow,owner.SessionId);
        var key=Guid.NewGuid().ToString();var body=new MonitorUpdate(new(1,"Private revision","Http","https://monitor.example.com/",1,null));
        Assert.True(source.Update(actor,I(created),E(created),body,key,null).Succeeded);
        var sql=boundary switch {
            "stamp"=>"UPDATE [identity].[User] SET SecurityStamp=NEWID() WHERE Id=@User",
            "idle"=>"UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Id=@Session",
            "absolute"=>"UPDATE [identity].[Session] SET AbsoluteExpiresAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Id=@Session",
            "deleted"=>"UPDATE [identity].[User] SET State='Deleted',IsDeleted=1,DeletedAt=SYSUTCDATETIME() WHERE Id=@User",
            "moduleGrant"=>"UPDATE [platform].[UserModuleGrant] SET Enabled=0 WHERE UserId=@User AND ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX36')",
            "space"=>"UPDATE [platform].[PersonalSpace] SET State='Suspended' WHERE Id=@Owner",
            _=>"UPDATE [identity].[User] SET EmailConfirmed=0,VerifiedAt=NULL WHERE Id=@User"};
        await fixture.ExecuteAsync(sql,Id("@User",owner.UserId),Id("@Session",owner.SessionId),Id("@Owner",owner.OwnerId));
        Denied(source.Get(actor,I(created)));Denied(source.List(actor,null,null,null));
        Denied(source.Update(actor,I(created),E(created),body,key,null));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='monitoring.monitor.update'",Id("@Id",I(created))));
    }
    [Theory][InlineData("User")][InlineData("Admin")][InlineData("SuperAdmin")]
    public async Task Unsupported_actions_fail_closed_even_with_stale_resolved_permission_and_allow(string role)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync(role);
        var type=typeof(SqlMonitoringService).Assembly.GetType("Nexora.Infrastructure.Authorization.SqlSelfCapability",throwOnError:true)!;
        var capability=Activator.CreateInstance(type,new SqlConnectionFactory(fixture.ConnectionString!))!;
        var evaluate=type.GetMethod("IsAllowed",new[]{typeof(SqlConnection),typeof(SqlTransaction),typeof(IdentityPrincipal),typeof(string),typeof(string[])})!;
        var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,role,DateTimeOffset.UtcNow,owner.SessionId);
        const string key="monitoring.monitor.check";
        using var connection=new SqlConnection(fixture.ConnectionString!);await connection.OpenAsync();using var transaction=connection.BeginTransaction();
        using var command=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM [platform].[Permission] WHERE ActionKey=@Key) INSERT [platform].[Permission](ActionKey,EffectiveStatus) VALUES(@Key,'Resolved'); UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey=@Key; INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,'Allow' FROM [platform].[Permission] WHERE ActionKey=@Key; SELECT COUNT(*) FROM [platform].[Permission] WHERE ActionKey=@Key AND EffectiveStatus='Resolved'",connection,transaction);
        command.Parameters.AddWithValue("@Key",key);command.Parameters.AddWithValue("@User",owner.UserId);
        try{Assert.Equal(1,Convert.ToInt32(await command.ExecuteScalarAsync()));Assert.False((bool)evaluate.Invoke(capability,new object[]{connection,transaction,actor,"FX36",new[]{key}})!);}
        finally{transaction.Rollback();}
    }
    [Fact] public async Task Concurrent_new_updates_have_one_winner_and_same_key_retry_has_one_effect()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner);var id=I(created);var body=new{metadata=Metadata("One concurrent change")};
        var results=await Task.WhenAll(Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created)),Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created)));
        try{Assert.Equal(1,results.Count(r=>r.StatusCode==HttpStatusCode.OK));Assert.Equal(1,results.Count(r=>r.StatusCode==HttpStatusCode.PreconditionFailed));}finally{foreach(var r in results)r.Dispose();}
        var current=await Get(owner,$"{Root}/{id}");var key=Guid.NewGuid();var duplicateBody=new{metadata=Metadata("Original retry acknowledgement")};var duplicates=await Task.WhenAll(Send(owner,HttpMethod.Put,$"{Root}/{id}",duplicateBody,E(current),key),Send(owner,HttpMethod.Put,$"{Root}/{id}",duplicateBody,E(current),key));
        try{Assert.Equal((await Json(duplicates[0])).GetRawText(),(await Json(duplicates[1])).GetRawText());}finally{foreach(var r in duplicates)r.Dispose();}
        Assert.Equal(2,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='monitoring.monitor.update'",Id("@Id",id)));
    }
}



