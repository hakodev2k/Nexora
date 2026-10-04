using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Local;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlCalendarExportTests(SqlApiFixture fixture)
{
    private const string Root="/api/v1/transfer/calendar/exports";
    private static SqlParameter Id(string key,Guid value)=>new(key,SqlDbType.UniqueIdentifier){Value=value};
    private static string E(JsonElement item)=>item.GetProperty("etag").GetString()!;
    private static Guid I(JsonElement item,string name="id")=>item.GetProperty(name).GetGuid();
    private static string Entry(string uid,string extra="",string time="DTSTART:20261003T100000Z\r\nDTEND:20261003T110000Z")=>$"BEGIN:VEVENT\r\nUID:{uid}\r\nSUMMARY:Synthetic ICS event\r\nDESCRIPTION:Private literal text\r\n{time}\r\n{extra}END:VEVENT\r\n";
    private static string Calendar(string entries)=>"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Synthetic SQL tests//EN\r\n"+entries+"END:VCALENDAR\r\n";
    private static async Task<JsonElement> Json(HttpResponseMessage response,HttpStatusCode status=HttpStatusCode.OK)
    {var text=await response.Content.ReadAsStringAsync();Assert.True(response.StatusCode==status,$"Expected{status}, got{response.StatusCode}; private response excluded.");using var document=JsonDocument.Parse(text);return document.RootElement.Clone();}
    private async Task<HttpResponseMessage> Send(SyntheticSession owner,HttpMethod method,string path,object body,string? etag=null,Guid? key=null)=>await fixture.SendJsonAsync(method,path,body,await fixture.GetCsrfAsync(),key??Guid.NewGuid(),owner.RawSessionHandle,etag);
    private async Task<JsonElement> Get(SyntheticSession owner,string path){using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,owner.RawSessionHandle);return await Json(response);}
    private sealed record Scope(SqlCalendarExportTests Tests,SyntheticSession Super,IReadOnlyList<JsonElement> Originals):IAsyncDisposable
    {public async ValueTask DisposeAsync(){foreach(var module in Originals)await Tests.Policy(Super,I(module),module.GetProperty("systemEnabled").GetBoolean(),module.GetProperty("registrationEnabled").GetBoolean());}}
    private async Task<Scope> Enable()
    {
        fixture.RequireAvailable();var super=await fixture.CreateActiveSessionAsync("SuperAdmin");var modules=(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Where(m=>new[]{"FX07","FX10","FX13","FX11","FX12"}.Contains(m.GetProperty("code").GetString())).Select(m=>m.Clone()).ToArray();
        foreach(var module in modules){Assert.Equal("Ready",module.GetProperty("state").GetString());await Policy(super,I(module),true,true);}return new(this,super,modules);
    }
    private async Task Policy(SyntheticSession super,Guid module,bool enabled,bool registration)
    {
        var current=(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m=>I(m)==module);
        if(current.GetProperty("systemEnabled").GetBoolean()==enabled&&current.GetProperty("registrationEnabled").GetBoolean()==registration)return;
        using var previewResponse=await Send(super,HttpMethod.Post,$"/api/v1/admin/modules/{module}/preview",new{systemEnabled=enabled,registrationEnabled=registration});var preview=await Json(previewResponse);Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        using var commit=await Send(super,HttpMethod.Put,$"/api/v1/admin/modules/{module}/policy",new{systemEnabled=enabled,registrationEnabled=registration,previewToken=preview.GetProperty("previewToken").GetString()},E(preview));await Json(commit);
    }
    private async Task Grant(SyntheticSession super,SyntheticSession user,string effect,params string[] keys)
    {
        using var current=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/admin/users/{user.UserId}/access",super.RawSessionHandle);await Json(current);
        var changes=keys.Select(actionKey=>new{actionKey,effect}).ToArray();using var previewResponse=await Send(super,HttpMethod.Post,$"/api/v1/admin/users/{user.UserId}/access/preview",new{kind="permissions",changes});var preview=await Json(previewResponse);
        using var commit=await Send(super,HttpMethod.Put,$"/api/v1/admin/users/{user.UserId}/access/permissions",new{kind="permissions",changes,previewToken=preview.GetProperty("previewToken").GetString()},current.Headers.ETag?.Tag);await Json(commit);
    }
    private static object Filter(string[]? kinds=null,string[]? manual=null,string[]? tasks=null,object? range=null)=>new{schemaVersion=1,sourceKinds=kinds??["Manual"],manualStatuses=manual??["Scheduled","Completed","Canceled"],taskStatuses=tasks??Array.Empty<string>(),range,containmentMode="FullyContained"};
    private async Task<JsonElement> PreviewExport(SyntheticSession owner,object filter)
    {using var response=await Send(owner,HttpMethod.Post,Root+"/preview",filter);return await Json(response);}
    private async Task<JsonElement> Generate(SyntheticSession owner,object filter,JsonElement preview,Guid? key=null)
    {using var response=await Send(owner,HttpMethod.Post,Root,new{filter,previewToken=preview.GetProperty("previewToken").GetString(),confirmed=true},key:key);return await Json(response,HttpStatusCode.Created);}
    private async Task<string> Bytes(SyntheticSession owner,Guid job)
    {using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{job}/content",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal("text/calendar",response.Content.Headers.ContentType!.MediaType);Assert.Equal("no-store",response.Headers.CacheControl!.ToString());return await response.Content.ReadAsStringAsync();}
    private async Task<JsonElement> Event(SyntheticSession owner,string title="Synthetic exported event",string start="2030-01-02T10:00:00Z",string end="2030-01-02T11:00:00Z",bool allDay=false,string? first=null,string? last=null)
    {using var response=await Send(owner,HttpMethod.Post,"/api/v1/calendar/events",new{title,description="Literal, private body; \\ newline\nUnicode 日本語 😀",startAt=start,endAt=end,timeZoneId="UTC",isAllDay=allDay,startDate=first,endDateExclusive=last});return await Json(response,HttpStatusCode.Created);}
    private async Task<JsonElement> TaskRecord(SyntheticSession owner,string title="Synthetic exported task")
    {
        using var projectResponse=await Send(owner,HttpMethod.Post,"/api/v1/projects",new{name="Owned export project",description="Project body excluded",startAt="2030-01-01T00:00:00Z",endAt="2030-01-05T00:00:00Z",priority="P2"});var project=await Json(projectResponse,HttpStatusCode.Created);
        using var taskResponse=await Send(owner,HttpMethod.Post,"/api/v1/tasks",new{projectId=I(project),title,description="Task safe description",status="NotStarted",startAt="2030-01-02T10:00:00Z",endAt="2030-01-02T11:00:00Z",priority=(string?)null});return await Json(taskResponse,HttpStatusCode.Created);
    }
    [Fact] public async Task Explicit_snapshot_selection_containment_business_projection_and_private_output_are_real()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var exact=await Event(owner);await Event(owner,"Crosses left","2030-01-01T23:00:00Z","2030-01-02T02:00:00Z");await Event(owner,"Crosses right","2030-01-02T23:00:00Z","2030-01-03T02:00:00Z");
        await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='UTC' WHERE Id=@User",Id("@User",owner.UserId));
        var filter=Filter(range:new{start="2030-01-02",end="2030-01-03"});var preview=await PreviewExport(owner,filter);Assert.Equal(1,preview.GetProperty("count").GetInt32());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        var key=Guid.NewGuid();var job=await Generate(owner,filter,preview,key);var id=I(job,"jobId");var content=await Bytes(owner,id);
        Assert.Contains("SUMMARY:Synthetic exported event",content);Assert.DoesNotContain("Crosses",content);Assert.DoesNotContain(I(exact).ToString(),content);Assert.DoesNotContain(owner.OwnerId.ToString(),content);Assert.DoesNotContain("VALARM",content);Assert.Contains("DTSTART:20300102T100000Z",content);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportArtifact] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [files].[FileObject] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner AND FileObjectId IS NOT NULL",Id("@Owner",owner.OwnerId)));
        var replay=await Generate(owner,filter,preview,key);Assert.Equal(job.GetRawText(),replay.GetRawText());Assert.Equal(3,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        var files=await Get(owner,"/api/v1/files");Assert.Empty(files.GetProperty("items").EnumerateArray());
        foreach(var suffix in new[]{"","/content"}){using var generic=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/files/{id}{suffix}",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,generic.StatusCode);}
    }
    [Fact] public async Task Task_only_admin_AND_reads_nullable_priority_current_status_and_missing_projection_fail_closed()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var task=await TaskRecord(owner);var filter=Filter(["Task"],[],["NotStarted"]);
        var preview=await PreviewExport(owner,filter);Assert.Equal(1,preview.GetProperty("count").GetInt32());var job=await Generate(owner,filter,preview);var content=await Bytes(owner,I(job,"jobId"));
        Assert.Contains("X-NEXORA-STATUS:NotStarted",content);Assert.DoesNotContain("X-NEXORA-PRIORITY",content);Assert.Contains("X-NEXORA-PROJECT:Owned export project",content);Assert.DoesNotContain("Project body excluded",content);
        await fixture.ExecuteAsync("UPDATE [identity].[UserRole] SET RoleId=(SELECT Id FROM [identity].[Role] WHERE Code='Admin') WHERE UserId=@User",Id("@User",owner.UserId));
        await Grant(scope.Super,owner,"Allow","transfer.export.request","transfer.export.read","transfer.export.download","calendar.ics.export","tasks.task.read","projects.project.read");
        await PreviewExport(owner,filter); // No calendar.event.read grant: Task-only must work.
        await Grant(scope.Super,owner,"Deny","projects.project.read");using(var denied=await Send(owner,HttpMethod.Post,Root+"/preview",filter))Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        await Grant(scope.Super,owner,"Allow","projects.project.read");
        await fixture.ExecuteAsync("DELETE FROM [calendar].[Event] WHERE OwnerId=@Owner AND TaskId=@Task",Id("@Owner",owner.OwnerId),Id("@Task",I(task)));
        using var missing=await Send(owner,HttpMethod.Post,Root+"/preview",filter);var error=await Json(missing,HttpStatusCode.UnprocessableEntity);Assert.Equal("SourceCompatibilityUnavailable",error.GetProperty("code").GetString());
        using var saved=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{I(job,"jobId")}/content",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,saved.StatusCode);
    }
    [Fact] public async Task Authoritative_all_day_custom_skipped_civil_date_and_strict_explicit_range_are_preserved()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='Pacific/Apia' WHERE Id=@User",Id("@User",owner.UserId));
        await Event(owner,"Skipped civil date","2011-12-30T00:00:00Z","2011-12-31T00:00:00Z",true,"2011-12-30","2011-12-31");
        var filter=Filter(range:new{start="2011-12-30",end="2011-12-31"});var preview=await PreviewExport(owner,filter);Assert.Equal(1,preview.GetProperty("count").GetInt32());var job=await Generate(owner,filter,preview);Assert.Contains("DTSTART;VALUE=DATE:20111230",await Bytes(owner,I(job,"jobId")));
        using(var missingStart=await Send(owner,HttpMethod.Post,Root+"/preview",Filter(range:new{end="2030-01-01"})))Assert.Equal(HttpStatusCode.BadRequest,missingStart.StatusCode);
        using(var missingEnd=await Send(owner,HttpMethod.Post,Root+"/preview",Filter(range:new{start="2030-01-01"})))Assert.Equal(HttpStatusCode.BadRequest,missingEnd.StatusCode);
        await Event(owner);using var mixed=await Send(owner,HttpMethod.Post,Root+"/preview",filter);var error=await Json(mixed,HttpStatusCode.UnprocessableEntity);Assert.Equal("UnsafeRange",error.GetProperty("code").GetString());
    }
    [Fact] public async Task Cohort_token_session_and_source_precision_changes_require_safe_fresh_preview()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var item=await Event(owner);var filter=Filter();var preview=await PreviewExport(owner,filter);
        await fixture.ExecuteAsync("UPDATE [calendar].[Event] SET Description=N'Changed saved body' WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",I(item)));
        using(var stale=await Send(owner,HttpMethod.Post,Root,new{filter,previewToken=preview.GetProperty("previewToken").GetString(),confirmed=true}))Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        var fresh=await PreviewExport(owner,filter);var otherSession=await fixture.CreateActiveSessionForUserAsync(owner.UserId);
        using(var session=await Send(otherSession,HttpMethod.Post,Root,new{filter,previewToken=fresh.GetProperty("previewToken").GetString(),confirmed=true}))Assert.Equal(HttpStatusCode.Conflict,session.StatusCode);
        using(var tooLarge=await Send(owner,HttpMethod.Post,Root,new{filter,previewToken=new string('a',2049),confirmed=true}))Assert.Equal(HttpStatusCode.Conflict,tooLarge.StatusCode);
        await fixture.ExecuteAsync("UPDATE [calendar].[Event] SET StartAt=DATEADD(nanosecond,100,StartAt) WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",I(item)));
        using var precise=await Send(owner,HttpMethod.Post,Root+"/preview",filter);var error=await Json(precise,HttpStatusCode.UnprocessableEntity);Assert.Equal("SourcePrecisionUnsupported",error.GetProperty("code").GetString());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
    }
    [Fact] public async Task Artifact_expiry_original_replay_and_owner_source_revocation_cannot_bypass_download()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();await Event(owner);var filter=Filter();var preview=await PreviewExport(owner,filter);var key=Guid.NewGuid();var job=await Generate(owner,filter,preview,key);var id=I(job,"jobId");
        foreach(var suffix in new[]{"","/content"}){using var foreign=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{id}{suffix}",other.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);}
        fixture.DigitalClock.Now=job.GetProperty("expiresAt").GetDateTimeOffset();
        try
        {
            using var expired=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{id}/content",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.Gone,expired.StatusCode);
            Assert.Equal("Expired",(await Get(owner,$"{Root}/{id}")).GetProperty("state").GetString());var replay=await Generate(owner,filter,preview,key);Assert.Equal(job.GetRawText(),replay.GetRawText());
            Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportArtifact] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        }
        finally{fixture.DigitalClock.Now=null;}
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",owner.SessionId));using var revoked=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{id}/content",owner.RawSessionHandle);Assert.Equal(HttpStatusCode.Unauthorized,revoked.StatusCode);
    }
    [Fact] public async Task Atomic_audit_failure_concurrent_receipt_and_real_twenty_five_row_report_pages()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await Event(owner);var filter=Filter();var preview=await PreviewExport(owner,filter);var key=Guid.NewGuid();var body=new{filter,previewToken=preview.GetProperty("previewToken").GetString(),confirmed=true};
        await fixture.ExecuteAsync("CREATE OR ALTER TRIGGER [security].[TR_SyntheticExportAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='transfer.export.request') THROW 51056,'Synthetic audit failure',1; END;");
        try{using var failed=await Send(owner,HttpMethod.Post,Root,body,key:key);Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);}finally{await fixture.ExecuteAsync("DROP TRIGGER [security].[TR_SyntheticExportAudit]");}
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        var responses=await System.Threading.Tasks.Task.WhenAll(Send(owner,HttpMethod.Post,Root,body,key:key),Send(owner,HttpMethod.Post,Root,body,key:key));
        try{var a=await Json(responses[0],HttpStatusCode.Created);var b=await Json(responses[1],HttpStatusCode.Created);Assert.Equal(a.GetRawText(),b.GetRawText());}finally{foreach(var response in responses)response.Dispose();}
        for(var n=0;n<26;n++)await Generate(owner,filter,preview);var first=await Get(owner,Root);Assert.Equal(25,first.GetProperty("items").GetArrayLength());var second=await Get(owner,Root+"?cursor="+first.GetProperty("nextCursor").GetGuid());Assert.Equal(2,second.GetProperty("items").GetArrayLength());
        Assert.Equal(27,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
    }
    [Fact] public async Task Every_selected_source_action_is_AND_gated_and_source_loss_hides_reports_before_paging()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await Event(owner);var task=await TaskRecord(owner);
        var filter=Filter(["Manual","Task"],["Scheduled"],["NotStarted"]);var preview=await PreviewExport(owner,filter);Assert.Equal(2,preview.GetProperty("count").GetInt32());var key=Guid.NewGuid();var job=await Generate(owner,filter,preview,key);var id=I(job,"jobId");
        await fixture.ExecuteAsync("UPDATE [identity].[UserRole] SET RoleId=(SELECT Id FROM [identity].[Role] WHERE Code='Admin') WHERE UserId=@User",Id("@User",owner.UserId));
        var actions=new[]{"transfer.export.request","transfer.export.read","transfer.export.download","calendar.ics.export","calendar.event.read","tasks.task.read","projects.project.read"};await Grant(scope.Super,owner,"Allow",actions);
        foreach(var action in actions)
        {
            await Grant(scope.Super,owner,"Deny",action);
            var path=action=="transfer.export.read"?$"{Root}/{id}":action=="transfer.export.download"?$"{Root}/{id}/content":Root+"/preview";
            using var denied=action is "transfer.export.read" or "transfer.export.download"?await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,owner.RawSessionHandle):await Send(owner,HttpMethod.Post,path,filter);
            Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);await Grant(scope.Super,owner,"Allow",action);
        }
        await fixture.ExecuteAsync("UPDATE [productivity].[Project] SET Status='Deleted',DeletedAt=SYSUTCDATETIME() WHERE OwnerId=@Owner AND Id=(SELECT ProjectId FROM [productivity].[Task] WHERE OwnerId=@Owner AND Id=@Task)",Id("@Owner",owner.OwnerId),Id("@Task",I(task)));
        using(var unavailable=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/{id}/content",owner.RawSessionHandle))Assert.Equal(HttpStatusCode.NotFound,unavailable.StatusCode);
        Assert.Empty((await Get(owner,Root)).GetProperty("items").EnumerateArray());
        using(var replay=await Send(owner,HttpMethod.Post,Root,new{filter,previewToken=preview.GetProperty("previewToken").GetString(),confirmed=true},key:key))Assert.Equal(HttpStatusCode.NotFound,replay.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
    }
    [Fact] public async Task Unrepresentable_midnight_and_invalid_choices_are_rejected_without_jobs()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await Event(owner);await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='Asia/Ho_Chi_Minh' WHERE Id=@User",Id("@User",owner.UserId));
        using(var range=await Send(owner,HttpMethod.Post,Root+"/preview",Filter(range:new{start="0001-01-01",end="2030-01-03"})))Assert.Equal("UnsafeRange",(await Json(range,HttpStatusCode.UnprocessableEntity)).GetProperty("code").GetString());
        using(var wrong=await Send(owner,HttpMethod.Post,Root+"/preview",Filter(["Manual"],["Skipped"],[])))Assert.Equal(HttpStatusCode.UnprocessableEntity,wrong.StatusCode);
        using(var unknown=await Send(owner,HttpMethod.Post,Root+"/preview",new{schemaVersion=1,sourceKinds=new[]{"Manual"},manualStatuses=new[]{"Scheduled"},taskStatuses=Array.Empty<string>(),range=(object?)null,containmentMode="FullyContained",ownerId=owner.OwnerId}))Assert.Equal(HttpStatusCode.BadRequest,unknown.StatusCode);
        using(var implicitAll=await Send(owner,HttpMethod.Post,Root+"/preview",new{schemaVersion=1,sourceKinds=new[]{"Manual"},manualStatuses=new[]{"Scheduled"},taskStatuses=Array.Empty<string>(),containmentMode="FullyContained"}))Assert.Equal(HttpStatusCode.BadRequest,implicitAll.StatusCode);
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ExportJob] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
    }
}
