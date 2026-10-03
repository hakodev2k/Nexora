using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlDigitalAssetTests(SqlApiFixture fixture)
{
    private const string Root = "/api/v1/assets/digital";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static object Metadata(string title="Synthetic digital",string domain="bücher.example",string? expiry=null,string? cost="12.34567890",string? currency="USD") =>
        new {title,kind="Domain",details=new {domain=new {name=domain,registrar="Synthetic registrar",autoRenewRecorded=true,nameserverNotes="Private domain note"}},expiresOn=expiry,cost,currency,notes="Private metadata"};
    private static object Body(string title="Synthetic digital",string state="Active") => new {metadata=Metadata(title),state};
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {expected}, received {response.StatusCode}: {content}");
        using var document = JsonDocument.Parse(content); return document.RootElement.Clone();
    }
    private async Task<JsonElement> Get(SyntheticSession user, string path)
    { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, user.RawSessionHandle); return await Json(response); }
    private Task<HttpResponseMessage> Send(SyntheticSession user, HttpMethod method, string path, object body, string? etag = null, Guid? key = null) =>
        SendCore(user, method, path, body, etag, key);
    private async Task<HttpResponseMessage> SendCore(SyntheticSession user, HttpMethod method, string path, object body, string? etag, Guid? key) =>
        await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), user.RawSessionHandle, etag);
    private async Task<JsonElement> Create(SyntheticSession user, string title = "Synthetic asset")
    { using var response = await Send(user, HttpMethod.Post, Root, Body(title)); return await Json(response, HttpStatusCode.Created); }
    private async Task<JsonElement> Change(SyntheticSession user, Guid id, string operation, string etag)
    { using var response = await Send(user, HttpMethod.Post, $"{Root}/{id}/{operation}", new { confirm = true }, etag); return await Json(response); }

    // Installation must preserve administrator policy. Tests enable the installed subset through real preview/commit APIs.
    private async Task<PolicyScope> Enable()
    {
        fixture.RequireAvailable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var module = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m => m.GetProperty("code").GetString() == "FX38").Clone();
        Assert.Equal("Ready", module.GetProperty("state").GetString());
        Assert.Contains("Manual metadata and renewals only", module.GetProperty("name").GetString());
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
    private sealed record PolicyScope(SqlDigitalAssetTests Tests, SyntheticSession Super, Guid Module, bool System, bool Registration) : IAsyncDisposable
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


    private async Task Zone(SyntheticSession owner,string zone)
    {
        using var current=await fixture.GetMeAsync(owner.RawSessionHandle); await Json(current);
        using var changed=await Send(owner,HttpMethod.Patch,"/api/v1/me",new{timeZoneId=zone},current.Headers.ETag?.Tag); await Json(changed);
    }
    private async Task<JsonElement> New(SyntheticSession owner,object metadata,string state="Active")
    { using var r=await Send(owner,HttpMethod.Post,Root,new{metadata,state}); return await Json(r,HttpStatusCode.Created); }
    private static string E(JsonElement value)=>value.GetProperty("etag").GetString()!;
    private static Guid I(JsonElement value)=>value.GetProperty("itemId").GetGuid();

    [Fact]
    public async Task Typed_metadata_IDNA_strict_values_and_current_owner_boundaries_hold()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();
        using(var anon=await fixture.Client.GetAsync(Root)) Assert.Equal(HttpStatusCode.Unauthorized,anon.StatusCode);
        foreach(var state in new[]{"", "Expired", "Archived", "Trash"})
        {using var bad=await Send(owner,HttpMethod.Post,Root,Body(state:state));Assert.Equal(HttpStatusCode.UnprocessableEntity,bad.StatusCode);}
        var created=await Create(owner);var id=I(created);var item=await Get(owner,$"{Root}/{id}");
        Assert.Equal("xn--bcher-kva.example",item.GetProperty("asciiName").GetString());Assert.Equal("bücher.example",item.GetProperty("unicodeName").GetString());
        Assert.Equal("12.3456789",item.GetProperty("metadata").GetProperty("cost").GetString());
        using(var forbidden=await Send(owner,HttpMethod.Post,Root,new{metadata=new{title="Unknown",kind="Domain",details=new{domain=new{name="test.example",password="private"}}},state="Active"}))Assert.Equal(HttpStatusCode.BadRequest,forbidden.StatusCode);
        using(var forbidden=await Send(owner,HttpMethod.Post,Root,new{metadata=new{title="Unknown",kind="OnlineService",details=new{onlineService=new{accountLabelEncrypted="private"}}},state="Active"}))Assert.Equal(HttpStatusCode.BadRequest,forbidden.StatusCode);
        foreach(var badMetadata in new object[]{Metadata(domain:"https://test.example"),Metadata(cost:"1e2"),Metadata(cost:"1.000000001"),Metadata(currency:null),
            new{title="Mismatch",kind="Domain",details=new{hosting=new{plan="Basic"}}},new{title="Multi",kind="Domain",details=new{domain=new{name="test.example"},hosting=new{}}}})
        {using var bad=await Send(owner,HttpMethod.Post,Root,new{metadata=badMetadata,state="Active"});Assert.Equal(HttpStatusCode.UnprocessableEntity,bad.StatusCode);}
        // Literal SANs fit individually but must also fit the actual JSON/UTF-16 SQL column.
        var largeSans=Enumerable.Range(0,100).Select(n=>new string('漢',248)+n.ToString("D3")).ToArray();
        using(var overflowSans=await Send(owner,HttpMethod.Post,Root,new{metadata=new{title="SAN aggregate overflow",kind="Certificate",details=new{certificate=new{subject="Synthetic",subjectAlternativeNames=largeSans}}},state="Active"}))Assert.Equal(HttpStatusCode.UnprocessableEntity,overflowSans.StatusCode);
        var typed=new object[]{
            new{title="Hosting",kind="Hosting",details=new{hosting=new{plan="Basic",storageLimitBytes="9223372036854775807",controlPanelUrl="https://example.invalid/panel"}}},
            new{title="Vps",kind="Vps",details=new{vps=new{hostName="test.example",ipAddress="2001:db8::1",cpuCount=2,memoryMiB="9007199254740993"}}},
            new{title="Certificate",kind="Certificate",details=new{certificate=new{subject="example.invalid",notBefore="2026-01-01T00:00:00+07:00",notAfter="2027-01-01T00:00:00Z",subjectAlternativeNames=new[]{"example.invalid","*.example.invalid"}}}},
            new{title="License",kind="License",details=new{license=new{product="Synthetic product",seats=1}}},
            new{title="Service",kind="OnlineService",details=new{onlineService=new{serviceUrl="https://example.invalid/service",plan="Free"}}}};
        foreach(var metadata in typed){var made=await New(owner,metadata);Assert.Equal("Active",(await Get(owner,$"{Root}/{I(made)}")).GetProperty("storedState").GetString());}
        using(var overflow=await Send(owner,HttpMethod.Post,Root,new{metadata=new{title="Overflow",kind="Vps",details=new{vps=new{memoryMiB="9223372036854775808"}}},state="Active"}))Assert.Equal(HttpStatusCode.UnprocessableEntity,overflow.StatusCode);
        foreach(var path in new[]{$"{Root}/{id}",$"{Root}/{id}/history",$"{Root}/{id}/renewals"})
        {using var cross=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,other.RawSessionHandle);Assert.Equal(HttpStatusCode.NotFound,cross.StatusCode);}
        using(var cross=await Send(other,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata()},E(created)))Assert.Equal(HttpStatusCode.NotFound,cross.StatusCode);
        using(var missing=await Send(owner,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata()}))Assert.Equal((HttpStatusCode)428,missing.StatusCode);
        using(var stale=await Send(owner,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata()},"\"stale\""))Assert.Equal(HttpStatusCode.PreconditionFailed,stale.StatusCode);
    }

    [Fact]
    public async Task Renewal_type_replacement_history_and_exact_lifecycle_keep_manual_provenance()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner);var id=I(created);var etag=E(created);
        using(var missing=await Send(owner,HttpMethod.Post,$"{Root}/{id}/record-renewal",new{newExpiry="2027-01-01",confirm=true},etag))Assert.Equal(HttpStatusCode.BadRequest,missing.StatusCode);
        etag=E(await Change(owner,id,"cancel",etag));
        using(var bad=await Send(owner,HttpMethod.Post,$"{Root}/{id}/record-renewal",new{renewedOn="2026-10-03",newExpiry="2027-10-03",confirm=false},etag))Assert.Equal(HttpStatusCode.UnprocessableEntity,bad.StatusCode);
        using(var renewal=await Send(owner,HttpMethod.Post,$"{Root}/{id}/record-renewal",new{renewedOn="2026-10-03",newExpiry="2027-10-03",confirm=true,notes="Private renewal"},etag))etag=E(await Json(renewal));
        var item=await Get(owner,$"{Root}/{id}");Assert.Equal("Canceled",item.GetProperty("state").GetString());Assert.Equal("12.3456789",item.GetProperty("metadata").GetProperty("cost").GetString());
        Assert.Equal(JsonValueKind.Null,(await Get(owner,$"{Root}/{id}/renewals")).GetProperty("items")[0].GetProperty("amount").ValueKind);
        var license=new{title="License replacement",kind="License",details=new{license=new{product="New product",seats=3}}};
        using(var noConfirm=await Send(owner,HttpMethod.Put,$"{Root}/{id}",new{metadata=license},etag))Assert.Equal(HttpStatusCode.UnprocessableEntity,noConfirm.StatusCode);
        using(var update=await Send(owner,HttpMethod.Put,$"{Root}/{id}",new{metadata=license,confirmTypeChange=true},etag))etag=E(await Json(update));
        var history=await Get(owner,$"{Root}/{id}/history");var first=history.GetProperty("items").EnumerateArray().Last().GetProperty("snapshot").GetProperty("fields");
        Assert.Equal("bücher.example",first.GetProperty("unicodeName").GetString());Assert.Equal("xn--bcher-kva.example",first.GetProperty("asciiName").GetString());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[DomainDetail] WHERE OwnerId=@Owner AND DigitalAssetId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[RenewalRecord] WHERE OwnerId=@Owner AND DigitalAssetId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        etag=E(await Change(owner,id,"archive",etag));etag=E(await Change(owner,id,"trash",etag));etag=E(await Change(owner,id,"restore",etag));
        Assert.Equal("Archived",(await Get(owner,$"{Root}/{id}")).GetProperty("state").GetString());etag=E(await Change(owner,id,"unarchive",etag));
        Assert.Equal("Canceled",(await Get(owner,$"{Root}/{id}")).GetProperty("state").GetString());
        Assert.Equal(first.ToString(),(await Get(owner,$"{Root}/{id}/history")).GetProperty("items").EnumerateArray().Last().GetProperty("snapshot").GetProperty("fields").ToString());
        etag=E(await Change(owner,id,"trash",etag));await Change(owner,id,"purge",etag);
        foreach(var table in new[]{"DigitalAsset","LicenseDetail","RenewalRecord","DigitalAssetVersion"})
            Assert.Equal(0,await fixture.ScalarIntAsync($"SELECT COUNT(*) FROM [assets].[{table}] WHERE OwnerId=@Owner AND {(table=="DigitalAsset"?"Id":"DigitalAssetId")}=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));
    }

    [Fact]
    public async Task Current_permissions_receipt_replay_and_renewal_reads_are_independent()
    {
        await using var scope=await Enable();var admin=await fixture.CreateActiveSessionAsync("Admin");
        using(var unset=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root,admin.RawSessionHandle))Assert.Equal(HttpStatusCode.Forbidden,unset.StatusCode);
        await Grant(scope,admin,"Allow","digital.asset.create","digital.renewal.record");var key=Guid.NewGuid();JsonElement created;
        using(var create=await Send(admin,HttpMethod.Post,Root,Body(),key:key))created=await Json(create,HttpStatusCode.Created);
        Assert.DoesNotContain("Private",created.ToString());
        using(var replay=await Send(admin,HttpMethod.Post,Root,Body(),key:key))Assert.Equal(created.ToString(),(await Json(replay,HttpStatusCode.Created)).ToString());
        await Grant(scope,admin,"Deny","digital.asset.create");using(var denied=await Send(admin,HttpMethod.Post,Root,Body(),key:key))Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        var id=I(created);var etag=E(created);var renewalKey=Guid.NewGuid();var body=new{renewedOn="2026-10-03",newExpiry="2027-01-01",amount="9007199254740993.12345678",currency="USD",notes="Private renewal",confirm=true};
        using(var renewed=await Send(admin,HttpMethod.Post,$"{Root}/{id}/record-renewal",body,etag,renewalKey))Assert.DoesNotContain("Private",(await Json(renewed)).ToString());
        foreach(var path in new[]{$"{Root}/{id}",$"{Root}/{id}/renewals",$"{Root}/{id}/history"})
        {using var denied=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,admin.RawSessionHandle);Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);}
        await Grant(scope,admin,"Allow","digital.asset.history");Assert.Equal("9007199254740993.12345678",(await Get(admin,$"{Root}/{id}/renewals")).GetProperty("items")[0].GetProperty("amount").GetString());
        await Grant(scope,admin,"Deny","digital.renewal.record");using(var revoked=await Send(admin,HttpMethod.Post,$"{Root}/{id}/record-renewal",body,etag,renewalKey))Assert.Equal(HttpStatusCode.Forbidden,revoked.StatusCode);
        await Grant(scope,admin,"Allow","digital.asset.update");using(var denied=await Send(admin,HttpMethod.Put,$"{Root}/{id}",new{metadata=Metadata()},etag))Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
        await Policy(scope,false,false);foreach(var actor in new[]{admin,scope.Super}){using var denied=await fixture.SendAuthenticatedAsync(HttpMethod.Get,Root,actor.RawSessionHandle);Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);}
    }

    [Fact]
    public async Task Expiry_days_DST_instants_extremes_and_selected_cursor_filters_are_exact()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();await Zone(owner,"America/New_York");
        fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-10-03T12:00:00Z");
        try
        {
            var prefix="Expiry_"+Guid.NewGuid().ToString("N");var created=await New(owner,Metadata(prefix,expiry:"2026-10-03"));var id=I(created);
            Assert.Equal("Active",(await Get(owner,$"{Root}/{id}")).GetProperty("state").GetString());
            Assert.Single((await Get(owner,$"{Root}?query={prefix}&from=2026-10-03&to=2026-10-04")).GetProperty("items").EnumerateArray());
            fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-10-04T04:00:00Z");Assert.Equal("Expired",(await Get(owner,$"{Root}/{id}")).GetProperty("state").GetString());
            var dst=await New(owner,Metadata(prefix+"DST",expiry:"2026-11-01"));fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-11-02T04:59:59Z");
            Assert.Equal("Active",(await Get(owner,$"{Root}/{I(dst)}")).GetProperty("state").GetString());fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-11-02T05:00:00Z");
            Assert.Equal("Expired",(await Get(owner,$"{Root}/{I(dst)}")).GetProperty("state").GetString());Assert.Equal(E(dst),(await Get(owner,$"{Root}/{I(dst)}")).GetProperty("etag").GetString());
            var cert=await New(owner,new{title=prefix+"Cert",kind="Certificate",details=new{certificate=new{subject="Synthetic",notAfter="2026-11-01T01:30:00-04:00"}},expiresOn="2027-01-01"});
            fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-11-01T05:29:59Z");Assert.Equal("Active",(await Get(owner,$"{Root}/{I(cert)}")).GetProperty("state").GetString());
            fixture.DigitalClock.Now=DateTimeOffset.Parse("2026-11-01T05:30:00Z");Assert.Equal("Expired",(await Get(owner,$"{Root}/{I(cert)}")).GetProperty("state").GetString());
            Assert.Contains((await Get(owner,$"{Root}?query={prefix}&state=Expired&from=2026-11-01&to=2026-11-02")).GetProperty("items").EnumerateArray(),x=>x.GetProperty("id").GetGuid()==I(cert));
            var max=await New(owner,Metadata(prefix+"Max",expiry:"9999-12-31"));
            Assert.Contains((await Get(owner,$"{Root}?query={prefix}&state=Active")).GetProperty("items").EnumerateArray(),x=>x.GetProperty("id").GetGuid()==I(max));
            var extremes=new List<Guid>();
            foreach(var instant in new[]{"0001-01-01T00:00:00Z","9999-12-31T23:59:59.9999999Z"})
                extremes.Add(I(await New(owner,new{title=prefix+"Extreme",kind="Certificate",details=new{certificate=new{subject="Extreme",notAfter=instant}}})));
            foreach(var zone in new[]{"America/New_York","Pacific/Auckland"})
            {
                await Zone(owner,zone);
                var early=(await Get(owner,$"{Root}?query={prefix}&state=Expired&from=0001-01-01&to=0001-01-02")).GetProperty("items").EnumerateArray().ToArray();
                if(zone=="Pacific/Auckland")Assert.Contains(early,x=>x.GetProperty("id").GetGuid()==extremes[0]);
                else Assert.DoesNotContain(early,x=>x.GetProperty("id").GetGuid()==extremes[0]);
                Assert.Contains((await Get(owner,$"{Root}?query={prefix}&state=Active&from=9999-12-30")).GetProperty("items").EnumerateArray(),x=>x.GetProperty("id").GetGuid()==extremes[1]);
            }
            await Zone(owner,"America/New_York");var pagePrefix="Paging_"+Guid.NewGuid().ToString("N");var expected=new HashSet<Guid>();
            for(var n=0;n<29;n++)expected.Add(I(await New(owner,Metadata(pagePrefix+"_"+n.ToString("D2"),expiry:"2028-01-01"))));
            var first=await Get(owner,$"{Root}?query={pagePrefix}");Assert.Equal(25,first.GetProperty("items").GetArrayLength());var cursor=first.GetProperty("nextCursor").GetGuid();
            var second=await Get(owner,$"{Root}?query={pagePrefix}&cursor={cursor}");Assert.Equal(4,second.GetProperty("items").GetArrayLength());
            Assert.True(expected.SetEquals(first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray()).Select(x=>x.GetProperty("id").GetGuid())));
            Assert.Empty((await Get(owner,$"{Root}?query=no-match&cursor={cursor}")).GetProperty("items").EnumerateArray());
        }
        finally{fixture.DigitalClock.Now=null;}
    }

    [Fact]
    public async Task Audit_failure_concurrency_immutable_rows_and_frozen_children_are_SQL_atomic()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var key=Guid.NewGuid();
        var concurrent=await Task.WhenAll(Send(owner,HttpMethod.Post,Root,Body(),key:key),Send(owner,HttpMethod.Post,Root,Body(),key:key));JsonElement made=default;
        foreach(var r in concurrent){using(r){var value=await Json(r,HttpStatusCode.Created);if(made.ValueKind==JsonValueKind.Undefined)made=value;else Assert.Equal(made.ToString(),value.ToString());}}
        var id=I(made);var etag=E(made);var count=await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]");
        await fixture.ExecuteAsync("CREATE OR ALTER TRIGGER [security].[TR_DigitalTestAuditFailure] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey='digital.renewal.record') THROW 51041,'Synthetic audit failure',1; END");
        try{using var failed=await Send(owner,HttpMethod.Post,$"{Root}/{id}/record-renewal",new{renewedOn="2026-10-03",newExpiry="2027-01-01",confirm=true},etag);Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);}
        finally{await fixture.ExecuteAsync("DROP TRIGGER [security].[TR_DigitalTestAuditFailure]");}
        Assert.Equal(count,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]"));Assert.Equal(etag,(await Get(owner,$"{Root}/{id}")).GetProperty("etag").GetString());
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[RenewalRecord] WHERE OwnerId=@Owner AND DigitalAssetId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        var immutable=await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync("UPDATE [assets].[DigitalAssetVersion] SET Reason='Changed' WHERE OwnerId=@Owner AND DigitalAssetId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));Assert.Equal(51039,immutable.Number);
        var wrongType=await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync("UPDATE [assets].[DigitalAsset] SET Kind='License' WHERE OwnerId=@Owner AND Id=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id)));Assert.Equal(547,wrongType.Number);
        etag=E(await Change(owner,id,"trash",etag));
        await fixture.ExecuteAsync("UPDATE [assets].[DomainDetail] SET Registrar='Synthetic out-of-band change' WHERE OwnerId=@Owner AND DigitalAssetId=@Id",Id("@Owner",owner.OwnerId),Id("@Id",id));
        foreach(var op in new[]{"restore","purge"}){using var refused=await Send(owner,HttpMethod.Post,$"{Root}/{id}/{op}",new{confirm=true},etag);Assert.Equal(HttpStatusCode.Conflict,refused.StatusCode);}
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [assets].[DigitalAsset] WHERE OwnerId=@Owner AND Id=@Id AND State='Trash'",Id("@Owner",owner.OwnerId),Id("@Id",id)));
        Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActorUserId=@User AND (RedactedDiffJson IS NOT NULL OR CHARINDEX('Private',ActionKey)>0)",Id("@User",owner.UserId)));
    }
}
