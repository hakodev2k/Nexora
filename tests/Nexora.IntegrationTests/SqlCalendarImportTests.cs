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
public sealed class SqlCalendarImportTests(SqlApiFixture fixture)
{
    private const string Root="/api/v1/transfer/calendar";
    private static SqlParameter Id(string key,Guid value)=>new(key,SqlDbType.UniqueIdentifier){Value=value};
    private static string E(JsonElement item)=>item.GetProperty("etag").GetString()!;
    private static Guid I(JsonElement item,string name="id")=>item.GetProperty(name).GetGuid();
    private static string Entry(string uid,string extra="",string time="DTSTART:20261003T100000Z\r\nDTEND:20261003T110000Z")=>$"BEGIN:VEVENT\r\nUID:{uid}\r\nSUMMARY:Synthetic ICS event\r\nDESCRIPTION:Private literal text\r\n{time}\r\n{extra}END:VEVENT\r\n";
    private static string Calendar(string entries)=>"BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Synthetic SQL tests//EN\r\n"+entries+"END:VCALENDAR\r\n";
    private static async Task<JsonElement> Json(HttpResponseMessage response,HttpStatusCode status=HttpStatusCode.OK)
    {var text=await response.Content.ReadAsStringAsync();Assert.True(response.StatusCode==status,$"Expected{status}, got{response.StatusCode}: {text}");using var document=JsonDocument.Parse(text);return document.RootElement.Clone();}
    private async Task<HttpResponseMessage> Send(SyntheticSession owner,HttpMethod method,string path,object body,string? etag=null,Guid? key=null)=>await fixture.SendJsonAsync(method,path,body,await fixture.GetCsrfAsync(),key??Guid.NewGuid(),owner.RawSessionHandle,etag);
    private async Task<JsonElement> Get(SyntheticSession owner,string path){using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,owner.RawSessionHandle);return await Json(response);}
    private sealed record Scope(SqlCalendarImportTests Tests,SyntheticSession Super,IReadOnlyList<JsonElement> Originals):IAsyncDisposable
    {public async ValueTask DisposeAsync(){foreach(var module in Originals)await Tests.Policy(Super,I(module),module.GetProperty("systemEnabled").GetBoolean(),module.GetProperty("registrationEnabled").GetBoolean());}}
    private async Task<Scope> Enable()
    {
        fixture.RequireAvailable();var super=await fixture.CreateActiveSessionAsync("SuperAdmin");var modules=(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Where(m=>new[]{"FX07","FX10","FX13"}.Contains(m.GetProperty("code").GetString())).Select(m=>m.Clone()).ToArray();
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
    private async Task<JsonElement> Upload(SyntheticSession owner,string content)
    {
        var bytes=Encoding.UTF8.GetBytes(content);using var begin=await Send(owner,HttpMethod.Post,"/api/v1/files/upload-sessions",new{originalName="synthetic-import.ics",mediaType="text/calendar",expectedBytes=bytes.Length});var session=await Json(begin,HttpStatusCode.Created);
        using var body=new ByteArrayContent(bytes);body.Headers.ContentType=new MediaTypeHeaderValue("text/calendar");using var complete=await fixture.SendContentAsync(HttpMethod.Put,$"/api/v1/files/upload-sessions/{I(session)}/content",body,await fixture.GetCsrfAsync(),Guid.NewGuid(),owner.RawSessionHandle,uploadHandle:session.GetProperty("uploadHandle").GetString());return await Json(complete,HttpStatusCode.Created);
    }
    private async Task<JsonElement> Preview(SyntheticSession owner,JsonElement file,Guid? key=null)
    {using var response=await Send(owner,HttpMethod.Post,Root+"/imports",new{fileId=I(file),fileETag=E(file)},key:key);return await Json(response,HttpStatusCode.Created);}
    private async Task<JsonElement> Change(SyntheticSession owner,JsonElement batch,string action="commit",Guid? key=null)
    {using var response=await Send(owner,HttpMethod.Post,$"{Root}/imports/{I(batch,"batchId")}/{action}",new{confirmed=true},E(batch),key);return await Json(response);}
    [Fact]
    public async Task Mixed_import_preview_partial_commit_replay_and_terminal_uid_dedupe_are_atomic()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var uid="Case-"+new string('u',600)+Guid.NewGuid().ToString("N");
        var file=await Upload(owner,Calendar(Entry(uid,"STATUS:CANCELLED\r\nBEGIN:VALARM\r\nTRIGGER:-PT15M\r\nEND:VALARM\r\n")+Entry(uid)+Entry(Guid.NewGuid().ToString(),"RRULE:FREQ=DAILY\r\n")+Entry(Guid.NewGuid().ToString()).Replace("DESCRIPTION:Private literal text\r\n","")));
        var key=Guid.NewGuid();var batch=await Preview(owner,file,key);Assert.Equal(4,batch.GetProperty("totalCount").GetInt32());Assert.Equal(1,batch.GetProperty("acceptedCount").GetInt32());Assert.Equal(3,batch.GetProperty("skippedCount").GetInt32());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));Assert.Equal(I(batch,"batchId"),I(await Preview(owner,file,key),"batchId"));
        var rows=await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows");Assert.Equal(4,rows.GetProperty("items").GetArrayLength());Assert.Contains("ReminderIgnored",rows.GetProperty("items")[0].GetProperty("warnings").EnumerateArray().Select(v=>v.GetString()));
        var commitKey=Guid.NewGuid();var done=await Change(owner,batch,key:commitKey);Assert.Equal("Completed",done.GetProperty("state").GetString());Assert.Equal(1,done.GetProperty("appliedCount").GetInt32());Assert.Equal(I(done,"batchId"),I(await Change(owner,batch,key:commitKey),"batchId"));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] e JOIN [calendar].[ImportedUid] u ON u.OwnerId=e.OwnerId AND u.ManualEventId=e.Id JOIN [platform].[Resource] r ON r.OwnerId=e.OwnerId AND r.Id=e.Id WHERE e.OwnerId=@Owner AND e.Status='Scheduled' AND e.SourceKind='Manual' AND e.SourceUid IS NULL AND e.TaskId IS NULL AND LEN(u.Uid)>255",Id("@Owner",owner.OwnerId)));
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Reminder] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        var applied=(await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows?outcome=Applied")).GetProperty("items")[0].GetProperty("resultResourceId").GetGuid();var eventItem=await Get(owner,$"/api/v1/calendar/events/{applied}");
        using(var cancel=await fixture.SendJsonAsync(HttpMethod.Delete,$"/api/v1/calendar/events/{applied}",new{},await fixture.GetCsrfAsync(),Guid.NewGuid(),owner.RawSessionHandle,E(eventItem)))Assert.Equal(HttpStatusCode.NoContent,cancel.StatusCode);
        var duplicate=await Preview(owner,file);Assert.Equal(0,duplicate.GetProperty("acceptedCount").GetInt32());Assert.Equal(4,duplicate.GetProperty("skippedCount").GetInt32());
        Assert.Equal(2,await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",applied)));
    }
    [Fact]
    public async Task Current_source_authority_admin_AND_dependencies_wrong_owner_and_live_session_fail_closed()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();var file=await Upload(owner,Calendar(Entry(Guid.NewGuid().ToString())));var batch=await Preview(owner,file);
        using(var unauth=await fixture.Client.GetAsync(Root+"/imports"))Assert.Equal(HttpStatusCode.Unauthorized,unauth.StatusCode);
        using(var foreign=await Send(other,HttpMethod.Post,Root+"/imports",new{fileId=I(file),fileETag=E(file)}))Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);
        foreach(var suffix in new[]{"","/rows"}){using var denied=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/imports/{I(batch,"batchId")}{suffix}",other.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);}
        using(var unknown=await Send(owner,HttpMethod.Post,Root+"/imports",new{fileId=I(file),fileETag=E(file),ownerId=owner.OwnerId}))Assert.Equal(HttpStatusCode.BadRequest,unknown.StatusCode);
        var admin=await fixture.CreateActiveSessionAsync("Admin");await Grant(scope.Super,admin,"Allow","files.file.read","files.file.upload","files.file.download");var adminFile=await Upload(admin,Calendar(Entry(Guid.NewGuid().ToString())));
        await Grant(scope.Super,admin,"Allow","transfer.import.preview","calendar.ics.preview");using(var sourceDenied=await Send(admin,HttpMethod.Post,Root+"/imports",new{fileId=I(adminFile),fileETag=E(adminFile)}))Assert.Equal(HttpStatusCode.Forbidden,sourceDenied.StatusCode);
        await Grant(scope.Super,admin,"Allow","calendar.event.create","transfer.import.read","transfer.import.commit","transfer.import.cancel","calendar.ics.import");var adminBatch=await Preview(admin,adminFile);
        await Grant(scope.Super,admin,"Deny","calendar.ics.import");using(var denied=await Send(admin,HttpMethod.Post,$"{Root}/imports/{I(adminBatch,"batchId")}/commit",new{confirmed=true},E(adminBatch)))Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        using(var denied=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Root}/imports/{I(adminBatch,"batchId")}/rows",admin.RawSessionHandle))Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Id",Id("@Id",owner.SessionId));using(var revoked=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root+"/imports",owner.RawSessionHandle))Assert.Equal(HttpStatusCode.Unauthorized,revoked.StatusCode);
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] WHERE OwnerId=@Owner",Id("@Owner",admin.OwnerId)));
    }
    [Fact]
    public async Task Date_only_skipped_civil_day_survives_timezone_change_midday_query_update_and_cancel()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='Pacific/Apia' WHERE Id=@Id",Id("@Id",owner.UserId));
        var file=await Upload(owner,Calendar(Entry(Guid.NewGuid().ToString(),time:"DTSTART;VALUE=DATE:20111230\r\nDTEND;VALUE=DATE:20111231")));var batch=await Preview(owner,file);Assert.Equal(1,batch.GetProperty("acceptedCount").GetInt32());await Change(owner,batch);
        var row=(await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows?outcome=Applied")).GetProperty("items")[0];var id=row.GetProperty("resultResourceId").GetGuid();var original=await Get(owner,$"/api/v1/calendar/events/{id}");Assert.Equal("2011-12-30",original.GetProperty("startDate").GetString());
        await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='America/New_York' WHERE Id=@Id",Id("@Id",owner.UserId));var changed=await Get(owner,$"/api/v1/calendar/events/{id}");Assert.Equal("2011-12-30",changed.GetProperty("startDate").GetString());
        var midday=await Get(owner,"/api/v1/calendar/events?from=2011-12-30T17:00:00Z&to=2011-12-30T18:00:00Z");Assert.Contains(midday.GetProperty("items").EnumerateArray(),e=>I(e)==id);
        using(var edit=await Send(owner,HttpMethod.Put,$"/api/v1/calendar/events/{id}",new{title="Edited literal all-day",description="Dates remain exact",startAt="2011-12-30T00:00:00Z",endAt="2011-12-31T00:00:00Z",timeZoneId="America/New_York",isAllDay=true,startDate="2011-12-30",endDateExclusive="2011-12-31"},E(changed)))Assert.Equal("2011-12-30",(await Json(edit)).GetProperty("startDate").GetString());
        var latest=await Get(owner,$"/api/v1/calendar/events/{id}");using(var terminal=await Send(owner,HttpMethod.Post,$"/api/v1/calendar/events/{id}/transition",new{status="Completed",confirm=true},E(latest)))Assert.Equal("Completed",(await Json(terminal)).GetProperty("status").GetString());
    }
    [Fact]
    public async Task Preview_revision_zone_overlap_file_and_cancellation_conflicts_preserve_rows_and_retention()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var file=await Upload(owner,Calendar(Entry(Guid.NewGuid().ToString())));var batch=await Preview(owner,file);var id=I(batch,"batchId");
        using(var missing=await Send(owner,HttpMethod.Post,$"{Root}/imports/{id}/commit",new{confirmed=true}))Assert.Equal((HttpStatusCode)428,missing.StatusCode);
        using(var stale=await Send(owner,HttpMethod.Post,$"{Root}/imports/{id}/commit",new{confirmed=true},"\"stale\""))Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        await fixture.ExecuteAsync("UPDATE [identity].[User] SET TimeZoneId='Asia/Ho_Chi_Minh' WHERE Id=@Id",Id("@Id",owner.UserId));using(var zone=await Send(owner,HttpMethod.Post,$"{Root}/imports/{id}/commit",new{confirmed=true},E(batch)))Assert.Equal(HttpStatusCode.Conflict,zone.StatusCode);
        batch=await Preview(owner,file);using(var changed=await Send(owner,HttpMethod.Post,"/api/v1/calendar/events",new{title="Existing overlap",description="Own existing source",startAt="2026-10-03T10:00:00Z",endAt="2026-10-03T11:00:00Z",timeZoneId="Asia/Ho_Chi_Minh"}))await Json(changed,HttpStatusCode.Created);
        using(var overlap=await Send(owner,HttpMethod.Post,$"{Root}/imports/{I(batch,"batchId")}/commit",new{confirmed=true},E(batch)))Assert.Equal(HttpStatusCode.Conflict,overlap.StatusCode);
        batch=await Preview(owner,file);var rows=await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows");Assert.Contains("OverlapDetected",rows.GetProperty("items")[0].GetProperty("warnings").EnumerateArray().Select(v=>v.GetString()));
        var key=Guid.NewGuid();var canceled=await Change(owner,batch,"cancel",key);Assert.Equal("Canceled",canceled.GetProperty("state").GetString());await Change(owner,batch,"cancel",key);
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ImportRow] WHERE OwnerId=@Owner AND BatchId=@Id AND(CandidateJson IS NOT NULL OR ExternalKeyHash IS NOT NULL)",Id("@Owner",owner.OwnerId),Id("@Id",I(batch,"batchId"))));
        using(var locked=await Send(owner,HttpMethod.Post,$"{Root}/imports/{I(batch,"batchId")}/commit",new{confirmed=true},E(canceled)))Assert.Equal(HttpStatusCode.Conflict,locked.StatusCode);
        using(var pin=await Send(owner,HttpMethod.Post,$"/api/v1/files/{I(file)}/trash",new{},E(file))) { var denied=await Json(pin,HttpStatusCode.Conflict);Assert.Equal("ModuleUnavailable",denied.GetProperty("code").GetString()); }
        // Synthetic source revision change exercises report invalidation; rename is not installed.
        using(var rename=await Send(owner,HttpMethod.Patch,$"/api/v1/files/{I(file)}",new{originalName="renamed-synthetic.ics"},E(file)))Assert.Equal(HttpStatusCode.Conflict,rename.StatusCode);
        await fixture.ExecuteAsync("UPDATE [files].[FileObject] SET CurrentRevision=CurrentRevision+1,OriginalName=N'revised-synthetic.ics' WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",I(file)));
        using(var connection=new SqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(); using var transaction=connection.BeginTransaction();
            Assert.True(new Nexora.Infrastructure.Transfer.SqlImportFileRetention().IsRetained(connection,transaction,owner.OwnerId,I(file)));
            transaction.Rollback();
        }
        using(var oldPreview=await Send(owner,HttpMethod.Post,Root+"/imports",new{fileId=I(file),fileETag=E(file)}))Assert.Equal(HttpStatusCode.Conflict,oldPreview.StatusCode);
        Assert.Empty((await Get(owner,Root+"/imports")).GetProperty("items").EnumerateArray());
    }
    [Fact]
    public async Task Actual_row_and_operation_paging_and_audit_failure_rollback_do_not_delete_earlier_data()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var file=await Upload(owner,Calendar(string.Concat(Enumerable.Range(1,30).Select(i=>Entry("Unique-"+i+Guid.NewGuid())))));var batch=await Preview(owner,file);
        var first=await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows");Assert.Equal(25,first.GetProperty("items").GetArrayLength());var next=await Get(owner,$"{Root}/imports/{I(batch,"batchId")}/rows?cursor={first.GetProperty("nextCursor").GetInt32()}");Assert.Equal(5,next.GetProperty("items").GetArrayLength());
        for(var i=0;i<26;i++)await Preview(owner,file);var operations=await Get(owner,Root+"/imports");Assert.Equal(25,operations.GetProperty("items").GetArrayLength());Assert.Equal(2,(await Get(owner,Root+"/imports?cursor="+operations.GetProperty("nextCursor").GetGuid())).GetProperty("items").GetArrayLength());
        await fixture.ExecuteAsync("CREATE OR ALTER TRIGGER [security].[TR_SyntheticImportAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='transfer.import.commit') THROW 51055,'Synthetic audit failure',1; END;");
        try{using var failed=await Send(owner,HttpMethod.Post,$"{Root}/imports/{I(batch,"batchId")}/commit",new{confirmed=true},E(batch));Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);}finally{await fixture.ExecuteAsync("DROP TRIGGER [security].[TR_SyntheticImportAudit]");}
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));Assert.Equal(27,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ImportBatch] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        var done=await Change(owner,batch);Assert.Equal(30,done.GetProperty("appliedCount").GetInt32());Assert.Equal(30,done.GetProperty("totalCount").GetInt32());
    }
    [Fact]
    public async Task Private_upload_rechecks_expired_session_after_stream_and_rolls_back_owned_bytes()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();
        var root=Path.Combine(Path.GetTempPath(),"nexora-upload-authority-"+Guid.NewGuid().ToString("N"));
        var service=new Nexora.Infrastructure.Files.SqlFileService(new Nexora.Infrastructure.Persistence.SqlConnectionFactory(fixture.ConnectionString!),root);
        var actor=new Nexora.Application.Identity.IdentityPrincipal(owner.UserId,owner.OwnerId,"User",DateTimeOffset.UtcNow,owner.SessionId);
        var bytes=Encoding.UTF8.GetBytes("Synthetic private upload authority content.");
        var initiated=service.InitiateUpload(actor,new("authority.txt","text/plain",bytes.Length),Guid.NewGuid().ToString());Assert.True(initiated.Succeeded);var upload=initiated.Value!;
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(second,1,SYSUTCDATETIME()) WHERE Id=@Id",Id("@Id",owner.SessionId));
        using var stream=new DelayedSyntheticStream(bytes);
        var result=await service.CompleteUploadAsync(actor,upload.Id,upload.UploadHandle,stream,bytes.Length,Guid.NewGuid().ToString());
        Assert.True(stream.ReadStarted);Assert.False(result.Succeeded);Assert.Equal("ModuleUnavailable",result.Code);
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [files].[FileObject] WHERE OwnerId=@Owner",Id("@Owner",owner.OwnerId)));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [files].[UploadSession] WHERE OwnerId=@Owner AND Id=@Id AND State='Created' AND FileObjectId IS NULL",Id("@Owner",owner.OwnerId),Id("@Id",upload.Id)));
        Assert.Empty(Directory.GetFiles(root,"*",SearchOption.AllDirectories));
    }
    private sealed class DelayedSyntheticStream(byte[] bytes):MemoryStream(bytes)
    {
        public bool ReadStarted { get; private set; }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)
        { if(!ReadStarted){ReadStarted=true;await Task.Delay(2000,cancellationToken);}return await base.ReadAsync(buffer,cancellationToken); }
    }
    [Fact]
    public async Task Concurrent_same_UID_commits_deduplicate_without_hidden_calendar_reads_and_replay_rechecks_module()
    {
        await using var scope=await Enable();var admin=await fixture.CreateActiveSessionAsync("Admin");
        await Grant(scope.Super,admin,"Allow","files.file.read","files.file.upload","files.file.download","transfer.import.preview","transfer.import.commit","transfer.import.read","calendar.ics.preview","calendar.ics.import","calendar.event.create");
        var file=await Upload(admin,Calendar(Entry(Guid.NewGuid().ToString())));var first=await Preview(admin,file);var second=await Preview(admin,file);
        var firstKey=Guid.NewGuid();var secondKey=Guid.NewGuid();
        var responses=await Task.WhenAll(Send(admin,HttpMethod.Post,$"{Root}/imports/{I(first,"batchId")}/commit",new{confirmed=true},E(first),firstKey),Send(admin,HttpMethod.Post,$"{Root}/imports/{I(second,"batchId")}/commit",new{confirmed=true},E(second),secondKey));
        var results=new List<JsonElement>();foreach(var response in responses){using(response)results.Add(await Json(response));}
        Assert.Equal(1,results.Sum(r=>r.GetProperty("appliedCount").GetInt32()));Assert.Equal(1,results.Sum(r=>r.GetProperty("skippedCount").GetInt32()));Assert.All(results,r=>Assert.Equal(1,r.GetProperty("totalCount").GetInt32()));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[ImportedUid] WHERE OwnerId=@Owner",Id("@Owner",admin.OwnerId)));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [operations].[ImportRow] WHERE OwnerId=@Owner AND Outcome='Duplicate'",Id("@Owner",admin.OwnerId)));
        var fx10=scope.Originals.Single(m=>m.GetProperty("code").GetString()=="FX10");await Policy(scope.Super,I(fx10),false,fx10.GetProperty("registrationEnabled").GetBoolean());
        using var denied=await Send(admin,HttpMethod.Post,$"{Root}/imports/{I(first,"batchId")}/commit",new{confirmed=true},E(first),firstKey);Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [calendar].[Event] WHERE OwnerId=@Owner",Id("@Owner",admin.OwnerId)));
    }
    [Fact]
    public async Task Normal_upgrade_preserves_Windows_IANA_dates_replays_and_rejects_unrepresentable_legacy_boundaries()
    {
        fixture.RequireAvailable();var validated=LocalSqlTarget.Validate(fixture.ConnectionString,"Development");
        var name="Nexora_Test_"+Guid.NewGuid().ToString("N");var target=new SqlConnectionStringBuilder(validated){InitialCatalog=name};LocalSqlTarget.Validate(target.ConnectionString,"Development");
        var master=new SqlConnectionStringBuilder(validated){InitialCatalog="master"};var created=false;
        await using var control=new SqlConnection(master.ConnectionString);await control.OpenAsync();
        using(var exists=new SqlCommand("SELECT DB_ID(@Name)",control)){exists.Parameters.AddWithValue("@Name",name);Assert.IsType<DBNull>(await exists.ExecuteScalarAsync());}
        try
        {
            using(var create=new SqlCommand($"CREATE DATABASE [{name}]",control))await create.ExecuteNonQueryAsync();created=true;
            var runner=new SqlMigrationRunner();var directory=Path.Combine(AppContext.BaseDirectory,"migrations");
            await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames.TakeWhile(name=>name!="20261003_0041_calendar_ics_import.sql").ToArray());
            await using var connection=new SqlConnection(target.ConnectionString);await connection.OpenAsync();var owner=Guid.NewGuid();var user=Guid.NewGuid();var first=Guid.NewGuid();var second=Guid.NewGuid();
            using(var seed=new SqlCommand("""
                INSERT [identity].[User](Id,Email,NormalizedEmail,PasswordHash,SecurityStamp,State,EmailConfirmed,IsDeleted,DisplayName,TimeZoneId,Locale)
                VALUES(@User,'synthetic-migration@example.invalid','SYNTHETIC-MIGRATION@EXAMPLE.INVALID','synthetic-no-login-hash','synthetic-no-session','Active',0,0,'Synthetic migration','UTC','en');
                INSERT [platform].[PersonalSpace](Id,UserId,State,CreatedAt,UpdatedAt) VALUES(@Owner,@User,'Active',SYSUTCDATETIME(),SYSUTCDATETIME());
                INSERT [calendar].[Event](Id,OwnerId,Title,Description,StartAt,EndAt,TimeZoneId,IsAllDay,SourceKind)
                VALUES(@First,@Owner,'Preserved IANA','Synthetic legacy date','2026-10-02T17:00:00','2026-10-03T17:00:00','Asia/Ho_Chi_Minh',1,'Manual'),
                      (@Second,@Owner,'Preserved Windows','Synthetic legacy date','2026-10-02T17:00:00','2026-10-04T17:00:00','SE Asia Standard Time',1,'Manual');
                """,connection)){seed.Parameters.Add(Id("@User",user));seed.Parameters.Add(Id("@Owner",owner));seed.Parameters.Add(Id("@First",first));seed.Parameters.Add(Id("@Second",second));await seed.ExecuteNonQueryAsync();}
            using(var invalid=new SqlCommand("UPDATE [calendar].[Event] SET StartAt='0001-01-01T00:00:00',EndAt='0001-01-02T05:00:00',TimeZoneId='Etc/GMT+5' WHERE Id=@Id",connection)){invalid.Parameters.Add(Id("@Id",first));await invalid.ExecuteNonQueryAsync();}
            await Assert.ThrowsAsync<InvalidOperationException>(()=>runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames));
            using(var untouched=new SqlCommand("SELECT COUNT(*) FROM dbo.NexoraMigration WHERE Name='20261003_0041_calendar_ics_import.sql'",connection))Assert.Equal(0,Convert.ToInt32(await untouched.ExecuteScalarAsync()));
            using(var restore=new SqlCommand("UPDATE [calendar].[Event] SET StartAt='2026-10-02T17:00:00',EndAt='2026-10-03T17:00:00',TimeZoneId='Asia/Ho_Chi_Minh' WHERE Id=@Id",connection)){restore.Parameters.Add(Id("@Id",first));await restore.ExecuteNonQueryAsync();}
            await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames);await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames);
            using(var check=new SqlCommand("SELECT COUNT(*) FROM [calendar].[Event] e JOIN [platform].[Resource] r ON r.Id=e.Id AND r.OwnerId=e.OwnerId WHERE e.OwnerId=@Owner AND e.StartDate='2026-10-03' AND e.EndDateExclusive IN('2026-10-04','2026-10-05') AND e.CalendarUid IS NOT NULL",connection)){check.Parameters.Add(Id("@Owner",owner));Assert.Equal(2,Convert.ToInt32(await check.ExecuteScalarAsync()));}
            using(var constraints=new SqlCommand("UPDATE [calendar].[Event] SET StartDate=NULL WHERE Id=@Id",connection)){constraints.Parameters.Add(Id("@Id",first));var error=await Assert.ThrowsAsync<SqlException>(()=>constraints.ExecuteNonQueryAsync());Assert.Equal(547,error.Number);}
        }
        finally
        {
            // Only the exact new, absence-checked generated database is cleaned; retained browser data is untouched.
            if(created){SqlConnection.ClearAllPools();using var drop=new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];",control);await drop.ExecuteNonQueryAsync();}
        }
    }
}
