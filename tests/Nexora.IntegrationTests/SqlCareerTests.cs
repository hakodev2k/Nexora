using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlCareerTests(SqlApiFixture fixture)
{
    private const string Root="/api/v1/career";
    private static SqlParameter Id(string name, Guid value)=>new(name,SqlDbType.UniqueIdentifier){Value=value};
    private static Guid I(JsonElement j)=>j.GetProperty("itemId").GetGuid();
    private static string E(JsonElement j)=>j.GetProperty("etag").GetString()!;
    private static async Task<JsonElement> Json(HttpResponseMessage response,HttpStatusCode expected=HttpStatusCode.OK)
    {
        var text=await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode==expected,$"Expected {expected}, received {response.StatusCode}: {text}");
        using var doc=JsonDocument.Parse(text);return doc.RootElement.Clone();
    }
    private async Task<JsonElement> Get(SyntheticSession user,string path)
    {using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,user.RawSessionHandle);return await Json(response);}
    private async Task<HttpResponseMessage> Send(SyntheticSession user,HttpMethod method,string path,object body,string? etag=null,Guid? key=null)=>
        await fixture.SendJsonAsync(method,path,body,await fixture.GetCsrfAsync(),key??Guid.NewGuid(),user.RawSessionHandle,etag);
    private static object CompanyBody(string title="Synthetic company")=>new{metadata=new{title,url="https://example.invalid/company",industry="Synthetic industry",location="Local",notes="Private company"}};
    private static object Metadata(string title="Synthetic job",Guid? companyId=null)=>new{title,companyId,url="https://example.invalid/job",location="Local",workMode="Remote",employmentType="Owner-entered contract",salaryMin="-9007199254740993.12345678",salaryMax="99999999999999999999.99999999",currency="USD",salaryText="Private salary",description="<script>literal</script>",notes="Private job",source="Manual",discoveredOn="2026-10-01",appliedOn="2026-10-03"};
    private async Task<JsonElement> NewCompany(SyntheticSession owner,string title="Synthetic company")
    {using var r=await Send(owner,HttpMethod.Post,Root+"/companies",CompanyBody(title));return await Json(r,HttpStatusCode.Created);}
    private async Task<JsonElement> NewJob(SyntheticSession owner,string title="Synthetic job",Guid? companyId=null,string stage="Saved")
    {using var r=await Send(owner,HttpMethod.Post,Root+"/jobs",new{metadata=Metadata(title,companyId),stage});return await Json(r,HttpStatusCode.Created);}
    private async Task<JsonElement> Change(SyntheticSession owner,Guid id,string operation,string etag,Guid? key=null)
    {using var r=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/{operation}",new{confirm=true},etag,key);return await Json(r);}
    // Installation must preserve administrator policy. Tests enable the installed subset through real preview/commit APIs.
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX39").Clone();
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        Assert.Contains("Manual Companies and Jobs only", module.GetProperty("name").GetString());
        var scope = new PolicyScope(this, super, module.GetProperty("id").GetGuid(), module.GetProperty("systemEnabled").GetBoolean(), module.GetProperty("registrationEnabled").GetBoolean());
        await Policy(scope, true, true); return scope;
    }
    private async Task Policy(PolicyScope scope, bool system, bool registration)
    {
        var current = (await Get(scope.Super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("id").GetGuid() == scope.Module);
        if (current.GetProperty("systemEnabled").GetBoolean() == system && current.GetProperty("registrationEnabled").GetBoolean() == registration) return;
        using var previewResponse = await Send(scope.Super, HttpMethod.Post, $"/api/v1/admin/modules/{scope.Module}/preview", new { systemEnabled = system, registrationEnabled = registration });
        var preview = await Json(previewResponse); Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        using var commit = await Send(scope.Super, HttpMethod.Put, $"/api/v1/admin/modules/{scope.Module}/policy", new { systemEnabled = system, registrationEnabled = registration, previewToken = preview.GetProperty("previewToken").GetString() }, preview.GetProperty("etag").GetString());
        await Json(commit);
    }
    private sealed record PolicyScope(SqlCareerTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
    { public ValueTask DisposeAsync() => new(Tests.Policy(this, System, Registration)); }
    private async Task Grant(PolicyScope scope, SyntheticSession target, string effect, params string[] actions)
    {
        using var current = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/admin/users/{target.UserId}/access", scope.Super.RawSessionHandle);
        await Json(current); var changes = actions.Select(action => new { actionKey = action, effect }).ToArray();
        using var previewResponse = await Send(scope.Super, HttpMethod.Post, $"/api/v1/admin/users/{target.UserId}/access/preview", new { kind = "permissions", changes });
        var preview = await Json(previewResponse);
        using var commit = await Send(scope.Super, HttpMethod.Put, $"/api/v1/admin/users/{target.UserId}/access/permissions", new { kind = "permissions", changes, previewToken = preview.GetProperty("previewToken").GetString() }, current.Headers.ETag?.Tag);
        await Json(commit);
    }



    [Fact]
    public async Task Typed_metadata_owner_validation_dates_and_exact_salary_are_persisted()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();
        using(var unauth=await fixture.Client.GetAsync(Root+"/jobs"))Assert.Equal(HttpStatusCode.Unauthorized,unauth.StatusCode);
        var company=await NewCompany(owner);var job=await NewJob(owner,companyId:I(company));var id=I(job);
        var read=await Get(owner,$"{Root}/jobs/{id}");var m=read.GetProperty("metadata");
        Assert.Equal("-9007199254740993.12345678",m.GetProperty("salaryMin").GetString());Assert.Equal("99999999999999999999.99999999",m.GetProperty("salaryMax").GetString());
        Assert.Equal("2026-10-01",m.GetProperty("discoveredOn").GetString());Assert.Equal("Synthetic company",read.GetProperty("companyLabel").GetString());
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [career].[JobApplication] WHERE OwnerId=@Owner AND Id=@Id AND SalaryMin=-9007199254740993.12345678 AND SalaryMax=99999999999999999999.99999999 AND CompanyId IS NOT NULL",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        foreach(var path in new[]{$"/companies/{I(company)}",$"/jobs/{id}",$"/jobs/{id}/history"}){using var denied=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root+path,other.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);}
        using(var foreign=await Send(other,HttpMethod.Post,Root+"/jobs",new{metadata=Metadata(companyId:I(company)),stage="Saved"}))Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);
        foreach(var bad in new object[]{new{title="Bad",salaryMin="1e2",currency="USD"},new{title="Bad",salaryMin="1.123456789",currency="USD"},new{title="Bad",salaryMin="2",salaryMax="1",currency="USD"},new{title="Bad",salaryMin="1"},new{title="Bad",url="https://user:password@example.invalid"},new{title="Bad",workMode="Invented"}})
        {using var invalid=await Send(owner,HttpMethod.Post,Root+"/jobs",new{metadata=bad,stage="Saved"});Assert.Equal(HttpStatusCode.UnprocessableEntity,invalid.StatusCode);}
        using(var unknown=await Send(owner,HttpMethod.Post,Root+"/jobs",new{metadata=new{title="Unknown",resumeId=Guid.NewGuid()},stage="Saved"}))Assert.Equal(HttpStatusCode.BadRequest,unknown.StatusCode);
        using(var noEtag=await Send(owner,HttpMethod.Put,$"{Root}/jobs/{id}",new{metadata=Metadata()}))Assert.Equal((HttpStatusCode)428,noEtag.StatusCode);
        using(var stale=await Send(owner,HttpMethod.Put,$"{Root}/jobs/{id}",new{metadata=Metadata()},"\"stale\""))Assert.Equal(HttpStatusCode.PreconditionFailed,stale.StatusCode);
        var foreignCompany=I(await NewCompany(other)); var fk=await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync("UPDATE [career].[JobApplication] SET CompanyId=@Company WHERE OwnerId=@Owner AND Id=@Id",Id("@Company",foreignCompany),Id("@Owner",owner.OwnerId),Id("@Id",id)));Assert.Equal(547,fk.Number);
    }
    [Fact]
    public async Task Manual_stage_history_and_Trash_receipts_replay_without_repeating_lifecycle()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var company=await NewCompany(owner);var created=await NewJob(owner,companyId:I(company));var id=I(created);var etag=E(created);
        using(var bad=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/transition",new{stage="Saved",confirm=true},etag))Assert.Equal(HttpStatusCode.UnprocessableEntity,bad.StatusCode);
        using(var accepted=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/transition",new{stage="Accepted",confirm=true},etag))etag=E(await Json(accepted));
        using(var backward=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/transition",new{stage="Applied",confirm=true},etag))Assert.Equal(HttpStatusCode.UnprocessableEntity,backward.StatusCode);
        using(var applied=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/transition",new{stage="Applied",confirm=true,confirmReturnToProgress=true,reason="Private reconsideration"},etag))etag=E(await Json(applied));
        var original=(await Get(owner,$"{Root}/jobs/{id}/history")).GetProperty("items").EnumerateArray().Last().ToString();
        var key=Guid.NewGuid();var trashETag=etag;var trashed=await Change(owner,id,"trash",etag,key);etag=E(trashed);
        using(var locked=await Send(owner,HttpMethod.Put,$"{Root}/jobs/{id}",new{metadata=Metadata()},etag))Assert.Equal(HttpStatusCode.Conflict,locked.StatusCode);
        // Changing independent Company metadata must not corrupt this frozen Job.
        using(var editCompany=await Send(owner,HttpMethod.Put,$"{Root}/companies/{I(company)}",CompanyBody("Company renamed"),E(company)))await Json(editCompany);
        using(var replay=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/trash",new{confirm=true},trashETag,key))Assert.Equal(trashed.ToString(),(await Json(replay)).ToString());
        etag=E(await Change(owner,id,"restore",etag));Assert.Equal("Applied",(await Get(owner,$"{Root}/jobs/{id}")).GetProperty("stage").GetString());
        Assert.Equal(original,(await Get(owner,$"{Root}/jobs/{id}/history")).GetProperty("items").EnumerateArray().Last().ToString());
        var immutable=await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync("UPDATE [career].[ApplicationEvent] SET Note='Changed' WHERE OwnerId=@Owner AND ApplicationId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));Assert.Equal(51042,immutable.Number);
        etag=E(await Change(owner,id,"trash",etag));var purgeKey=Guid.NewGuid();var purged=await Change(owner,id,"purge",etag,purgeKey);
        using(var replay=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/purge",new{confirm=true},etag,purgeKey))Assert.Equal(purged.ToString(),(await Json(replay)).ToString());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [career].[ApplicationEvent] WHERE OwnerId=@Owner AND ApplicationId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [career].[Company] WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",I(company))));
    }
    private async Task<JsonElement> PreviewMerge(SyntheticSession owner,Guid source,Guid target)
    {using var r=await Send(owner,HttpMethod.Post,$"{Root}/companies/{source}/merge-preview",new{targetId=target});return await Json(r);}
    private static object MergeBody(JsonElement preview)=>new{targetId=preview.GetProperty("targetId").GetGuid(),sourceETag=preview.GetProperty("sourceETag").GetString(),targetETag=preview.GetProperty("targetETag").GetString(),previewToken=preview.GetProperty("previewToken").GetString(),confirm=true,keepTargetMetadata=true};
    [Fact]
    public async Task Merge_binds_exact_cohort_preserves_labels_and_replays_after_success()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var source=await NewCompany(owner,"Source company");var target=await NewCompany(owner,"Target company");var job=await NewJob(owner,companyId:I(source));var id=I(job);
        var preview=await PreviewMerge(owner,I(source),I(target));Assert.Single(preview.GetProperty("jobs").EnumerateArray());
        using(var edit=await Send(owner,HttpMethod.Put,$"{Root}/jobs/{id}",new{metadata=Metadata("Changed job",I(source))},E(job)))job=await Json(edit);
        using(var stale=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",MergeBody(preview)))Assert.Equal(HttpStatusCode.Conflict,stale.StatusCode);
        preview=await PreviewMerge(owner,I(source),I(target));var key=Guid.NewGuid();var body=MergeBody(preview);JsonElement merged;
        using(var r=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",body,key:key))merged=await Json(r);
        using(var replay=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",body,key:key))Assert.Equal(merged.ToString(),(await Json(replay)).ToString());
        var read=await Get(owner,$"{Root}/jobs/{id}");Assert.Equal(I(target),read.GetProperty("metadata").GetProperty("companyId").GetGuid());Assert.Equal("Target company",read.GetProperty("companyLabel").GetString());
        var history=(await Get(owner,$"{Root}/jobs/{id}/history")).GetProperty("items");Assert.Equal("Source company",history.EnumerateArray().Last().GetProperty("snapshot").GetProperty("fields").GetProperty("companyLabelSnapshot").GetString());
        Assert.Equal("career.company.merge",history[0].GetProperty("actionKey").GetString());
        Assert.Equal("Source company",(await Get(owner,$"{Root}/companies/{I(source)}")).GetProperty("metadata").GetProperty("title").GetString());
        using(var locked=await Send(owner,HttpMethod.Put,$"{Root}/companies/{I(source)}",CompanyBody(),E(merged)))Assert.Equal(HttpStatusCode.Conflict,locked.StatusCode);
        source=await NewCompany(owner);var frozen=await NewJob(owner,companyId:I(source));await Change(owner,I(frozen),"trash",E(frozen));
        using(var blocked=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge-preview",new{targetId=I(target)}))Assert.Equal(HttpStatusCode.Conflict,blocked.StatusCode);
    }
    [Fact]
    public async Task Independent_Admin_actions_redact_sources_recheck_replay_and_module_policy()
    {
        await using var scope=await Enable();var admin=await fixture.CreateActiveSessionAsync("Admin");
        using(var unset=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root+"/jobs",admin.RawSessionHandle))Assert.Equal(HttpStatusCode.Forbidden,unset.StatusCode);
        await Grant(scope,admin,"Allow","career.company.create","career.job.create","career.job.history","career.job.read","career.company.merge");
        var company=await NewCompany(admin,"Secret company label");var target=await NewCompany(admin,"Another secret");
        using(var association=await Send(admin,HttpMethod.Post,Root+"/jobs",new{metadata=Metadata(companyId:I(company)),stage="Saved"}))Assert.Equal(HttpStatusCode.Forbidden,association.StatusCode);
        await Grant(scope,admin,"Allow","career.company.read");var job=await NewJob(admin,companyId:I(company));await Grant(scope,admin,"Deny","career.company.read");
        Assert.Equal(JsonValueKind.Null,(await Get(admin,$"{Root}/jobs/{I(job)}")).GetProperty("companyLabel").ValueKind);
        Assert.Equal(JsonValueKind.Null,(await Get(admin,$"{Root}/jobs/{I(job)}/history")).GetProperty("items")[0].GetProperty("snapshot").GetProperty("fields").GetProperty("companyLabelSnapshot").ValueKind);
        Assert.Empty((await Get(admin,Root+"/jobs?query=Secret%20company")).GetProperty("items").EnumerateArray());
        await Grant(scope,admin,"Deny","career.job.read");var preview=await PreviewMerge(admin,I(company),I(target));Assert.Equal(JsonValueKind.Null,preview.GetProperty("sourceMetadata").ValueKind);Assert.Equal(JsonValueKind.Null,preview.GetProperty("jobs")[0].GetProperty("title").ValueKind);
        await Grant(scope,admin,"Allow","career.job.update");using(var update=await Send(admin,HttpMethod.Put,$"{Root}/jobs/{I(job)}",new{metadata=Metadata()},E(job)))Assert.Equal(HttpStatusCode.Forbidden,update.StatusCode);
        var key=Guid.NewGuid();var body=MergeBody(preview);using(var merge=await Send(admin,HttpMethod.Post,$"{Root}/companies/{I(company)}/merge",body,key:key))await Json(merge);
        await Grant(scope,admin,"Deny","career.company.merge");using(var deniedReplay=await Send(admin,HttpMethod.Post,$"{Root}/companies/{I(company)}/merge",body,key:key))Assert.Equal(HttpStatusCode.Forbidden,deniedReplay.StatusCode);
        await Policy(scope,false,false);using(var disabled=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root+"/capabilities",scope.Super.RawSessionHandle))Assert.Equal(HttpStatusCode.Forbidden,disabled.StatusCode);
    }
    [Fact]
    public async Task Selected_paging_company_and_job_filters_and_history_are_real()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var prefix=Guid.NewGuid().ToString("N");var jobs=new HashSet<Guid>();var companies=new HashSet<Guid>();
        for(var n=0;n<29;n++){var company=await NewCompany(owner,prefix+n.ToString("D2"));companies.Add(I(company));jobs.Add(I(await NewJob(owner,prefix+n.ToString("D2"),I(company))));}
        foreach(var (path,expected) in new[]{("companies",companies),("jobs",jobs)})
        {var first=await Get(owner,$"{Root}/{path}?query={prefix}");Assert.Equal(25,first.GetProperty("items").GetArrayLength());var cursor=first.GetProperty("nextCursor").GetGuid();var second=await Get(owner,$"{Root}/{path}?query={prefix}&cursor={cursor}");Assert.Equal(4,second.GetProperty("items").GetArrayLength());Assert.True(expected.SetEquals(first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(x=>x.GetProperty("id").GetGuid())));Assert.Empty((await Get(owner,$"{Root}/{path}?query=no-match&cursor={cursor}")).GetProperty("items").EnumerateArray());}
        Assert.Equal(25,(await Get(owner,$"{Root}/jobs?query={prefix}&dateField=Discovered&from=2026-10-01&to=2026-10-02&stage=Saved&location=Local")).GetProperty("items").GetArrayLength());
        Assert.Empty((await Get(owner,$"{Root}/jobs?query={prefix}&dateField=Applied&from=2026-10-01&to=2026-10-03")).GetProperty("items").EnumerateArray());
        var job=await NewJob(owner);var id=I(job);var etag=E(job);for(var n=0;n<29;n++){using var update=await Send(owner,HttpMethod.Put,$"{Root}/jobs/{id}",new{metadata=Metadata("History "+n)},etag);etag=E(await Json(update));}
        var history=await Get(owner,$"{Root}/jobs/{id}/history");Assert.Equal(25,history.GetProperty("items").GetArrayLength());var secondHistory=await Get(owner,$"{Root}/jobs/{id}/history?cursor={history.GetProperty("nextCursor").GetGuid()}");Assert.Equal(5,secondHistory.GetProperty("items").GetArrayLength());
    }
    [Fact]
    public async Task Receipt_replay_preserves_changed_association_prerequisites_but_not_unchanged_ones()
    {
        await using var scope=await Enable();var admin=await fixture.CreateActiveSessionAsync("Admin");
        await Grant(scope,admin,"Allow","career.company.create","career.company.read","career.job.create","career.job.read","career.job.update");
        var first=await NewCompany(admin);var second=await NewCompany(admin);var createKey=Guid.NewGuid();var createBody=new{metadata=Metadata(companyId:I(first)),stage="Saved"};JsonElement job;
        using(var create=await Send(admin,HttpMethod.Post,Root+"/jobs",createBody,key:createKey))job=await Json(create,HttpStatusCode.Created);
        var changedKey=Guid.NewGuid();var changedBody=new{metadata=Metadata("Changed",I(second))};var oldETag=E(job);JsonElement changed;
        using(var update=await Send(admin,HttpMethod.Put,$"{Root}/jobs/{I(job)}",changedBody,oldETag,changedKey))changed=await Json(update);
        var unchangedKey=Guid.NewGuid();var unchangedBody=new{metadata=Metadata("Unchanged association",I(second))};var currentETag=E(changed);JsonElement unchanged;
        using(var update=await Send(admin,HttpMethod.Put,$"{Root}/jobs/{I(job)}",unchangedBody,currentETag,unchangedKey))unchanged=await Json(update);
        await Grant(scope,admin,"Deny","career.company.read");
        using(var deniedCreate=await Send(admin,HttpMethod.Post,Root+"/jobs",createBody,key:createKey))Assert.Equal(HttpStatusCode.Forbidden,deniedCreate.StatusCode);
        using(var deniedChange=await Send(admin,HttpMethod.Put,$"{Root}/jobs/{I(job)}",changedBody,oldETag,changedKey))Assert.Equal(HttpStatusCode.Forbidden,deniedChange.StatusCode);
        using(var replay=await Send(admin,HttpMethod.Put,$"{Root}/jobs/{I(job)}",unchangedBody,currentETag,unchangedKey))Assert.Equal(unchanged.ToString(),(await Json(replay)).ToString());
    }
    [Fact]
    public async Task New_merge_rejects_expired_preview_changed_references_and_foreign_targets()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();
        var source=await NewCompany(owner);var target=await NewCompany(owner);var foreign=await NewCompany(other);var job=await NewJob(owner,companyId:I(source));
        using(var denied=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge-preview",new{targetId=I(foreign)}))Assert.Equal(HttpStatusCode.NotFound,denied.StatusCode);
        var preview=await PreviewMerge(owner,I(source),I(target));
        fixture.DigitalClock.Now=DateTimeOffset.UtcNow.AddMinutes(6);
        try{using var expired=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",MergeBody(preview));Assert.Equal(HttpStatusCode.Conflict,expired.StatusCode);}finally{fixture.DigitalClock.Now=null;}
        await fixture.ExecuteAsync("INSERT [platform].[ResourceLink](Id,OwnerId,SourceResourceId,TargetResourceId,RelationType,State,UpdatedAt) VALUES(NEWID(),@Owner,@Source,@Target,'Synthetic retention','Active',SYSUTCDATETIME())",Id("@Owner",owner.OwnerId),Id("@Source",I(source)),Id("@Target",I(job)));
        using(var changed=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",MergeBody(preview)))Assert.Equal(HttpStatusCode.Conflict,changed.StatusCode);
        preview=await PreviewMerge(owner,I(source),I(target));Assert.Single(preview.GetProperty("retainedReferences").EnumerateArray());var key=Guid.NewGuid();var body=MergeBody(preview);JsonElement merged;
        using(var merge=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",body,key:key))merged=await Json(merge);
        fixture.DigitalClock.Now=DateTimeOffset.UtcNow.AddMinutes(6);
        try{using var replay=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",body,key:key);Assert.Equal(merged.ToString(),(await Json(replay)).ToString());}finally{fixture.DigitalClock.Now=null;}
        var current=await Get(owner,$"{Root}/jobs/{I(job)}");var trashed=await Change(owner,I(job),"trash",E(current));
        using(var pinned=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{I(job)}/purge",new{confirm=true},E(trashed)))Assert.Equal(HttpStatusCode.Conflict,pinned.StatusCode);
        // A retained source identity survives merge; the generic reference is never silently retargeted.
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND SourceResourceId=@Source AND TargetResourceId=@Target AND State='Active'",Id("@Owner",owner.OwnerId),Id("@Source",I(source)),Id("@Target",I(job))));
    }
    [Fact]
    public async Task Atomic_audit_failure_concurrency_and_frozen_payload_reject_partial_writes()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var source=await NewCompany(owner);var target=await NewCompany(owner);var job=await NewJob(owner,companyId:I(source));var id=I(job);var preview=await PreviewMerge(owner,I(source),I(target));
        await fixture.ExecuteAsync("CREATE OR ALTER TRIGGER [security].[TR_CareerTestFailure] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='career.company.merge') THROW 51043,'Synthetic audit failure',1; END");
        try{using var failed=await Send(owner,HttpMethod.Post,$"{Root}/companies/{I(source)}/merge",MergeBody(preview));Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);}finally{await fixture.ExecuteAsync("DROP TRIGGER [security].[TR_CareerTestFailure]");}
        Assert.Equal(I(source),(await Get(owner,$"{Root}/jobs/{id}")).GetProperty("metadata").GetProperty("companyId").GetGuid());Assert.Single((await Get(owner,$"{Root}/jobs/{id}/history")).GetProperty("items").EnumerateArray());Assert.Equal(E(job),(await Get(owner,$"{Root}/jobs/{id}")).GetProperty("etag").GetString());
        var sameKey=Guid.NewGuid();var requests=await Task.WhenAll(Send(owner,HttpMethod.Post,Root+"/jobs",new{metadata=Metadata(),stage="Offer"},key:sameKey),Send(owner,HttpMethod.Post,Root+"/jobs",new{metadata=Metadata(),stage="Offer"},key:sameKey));JsonElement made=default;foreach(var r in requests){using(r){var value=await Json(r,HttpStatusCode.Created);if(made.ValueKind==JsonValueKind.Undefined)made=value;else Assert.Equal(made.ToString(),value.ToString());}}
        var etag=E(await Change(owner,id,"trash",E(job)));await fixture.ExecuteAsync("UPDATE [career].[JobApplication] SET Notes='Synthetic out-of-band modification' WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id));
        // Use the actual new ETag so the frozen cohort, rather than optimistic concurrency, must refuse.
        etag=E(await Get(owner,$"{Root}/jobs/{id}"));foreach(var operation in new[]{"restore","purge"}){using var refused=await Send(owner,HttpMethod.Post,$"{Root}/jobs/{id}/{operation}",new{confirm=true},etag);Assert.Equal(HttpStatusCode.Conflict,refused.StatusCode);}
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND RedactedDiffJson IS NOT NULL",Id("@User",owner.UserId)));
    }
}
