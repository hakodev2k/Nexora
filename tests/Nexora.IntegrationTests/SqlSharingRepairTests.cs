using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Modules;
using Nexora.Application.Sharing;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Modules;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Sharing;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlSharingRepairTests
{
    private readonly SqlApiFixture fixture;
    public SqlSharingRepairTests(SqlApiFixture fixture) => this.fixture = fixture;
    private const string Links="/api/v1/sharing/links";
    private static SqlParameter Id(string name,Guid value)=>new(name,SqlDbType.UniqueIdentifier){Value=value};
    private static Guid I(JsonElement value)=>value.GetProperty("id").GetGuid();
    private static string E(JsonElement value)=>value.GetProperty("etag").GetString()!;
    private static string Token(JsonElement value)=>value.GetProperty("token").GetString()!;
    private static async Task<JsonElement> Json(HttpResponseMessage response,HttpStatusCode expected=HttpStatusCode.OK)
    {
        Assert.True(response.StatusCode==expected,$"Expected {expected}, got {response.StatusCode}; private payload excluded.");
        using var document=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return document.RootElement.Clone();
    }
    private async Task<HttpResponseMessage> Send(SyntheticSession actor,HttpMethod method,string path,object body,string? etag=null,Guid? key=null)=>
        await fixture.SendJsonAsync(method,path,body,await fixture.GetCsrfAsync(),key??Guid.NewGuid(),actor.RawSessionHandle,etag);
    private async Task<JsonElement> Get(SyntheticSession actor,string path)
    {using var response=await fixture.SendAuthenticatedAsync(HttpMethod.Get,path,actor.RawSessionHandle);return await Json(response);}
    private async Task<HttpResponseMessage> Resolve(JsonElement link,SyntheticSession? viewer=null)=>viewer is null
        ?await fixture.Client.GetAsync("/api/v1/sharing/resolve/"+Token(link))
        :await fixture.SendAuthenticatedAsync(HttpMethod.Get,"/api/v1/sharing/resolve/"+Token(link),viewer.RawSessionHandle);
    private sealed record Scope(SqlSharingRepairTests Tests,SyntheticSession Super,IReadOnlyList<JsonElement> Originals):IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            foreach(var m in Originals) await Tests.fixture.ExecuteAsync("UPDATE [platform].[Module] SET State=@State,SystemEnabled=@System,RegistrationEnabled=@Registration,SharingEnabled=@Sharing WHERE Id=@Id;",
                Id("@Id",I(m)),new("@State",SqlDbType.VarChar,24){Value=m.GetProperty("state").GetString()},new("@System",SqlDbType.Bit){Value=m.GetProperty("systemEnabled").GetBoolean()},new("@Registration",SqlDbType.Bit){Value=m.GetProperty("registrationEnabled").GetBoolean()},new("@Sharing",SqlDbType.Bit){Value=m.GetProperty("sharingEnabled").GetBoolean()});
        }
    }
    private async Task<Scope> Enable()
    {
        fixture.RequireAvailable();var super=await fixture.CreateActiveSessionAsync("SuperAdmin");
        var originals=(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Where(m=>new[]{"FX04","FX11","FX12","FX20"}.Contains(m.GetProperty("code").GetString())).Select(m=>m.Clone()).ToArray();
        Assert.Equal(4,originals.Length);
        foreach(var module in originals)await fixture.ExecuteAsync("UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1,SharingEnabled=CASE WHEN Code='FX12' THEN 0 ELSE 1 END WHERE Id=@Id",Id("@Id",I(module)));
        return new(this,super,originals);
    }
    private async Task<JsonElement> Project(SyntheticSession owner)
    {using var response=await Send(owner,HttpMethod.Post,"/api/v1/projects",new{name="Synthetic shared project",description="Approved project description",startAt="2000-01-01T00:00:00Z",endAt="2030-01-05T00:00:00Z",priority="P2"});return await Json(response,HttpStatusCode.Created);}
    private async Task<JsonElement> Document(SyntheticSession owner)
    {using var response=await Send(owner,HttpMethod.Post,"/api/v1/documents",new{title="Synthetic shared document",documentType="Note",editorMode="Markdown",body="Literal approved content"});return await Json(response,HttpStatusCode.Created);}
    private async Task<JsonElement> Create(SyntheticSession owner,Guid source,string type="Project",string mode="PublicLink",Guid[]? users=null)
    {using var response=await Send(owner,HttpMethod.Post,Links,new{resourceType=type,resourceId=source,mode,noExpiry=true,allowedUserIds=users??[]});return await Json(response,HttpStatusCode.Created);}
    private async Task<JsonElement> Module(SyntheticSession super,string code)=>(await Get(super,"/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(m=>m.GetProperty("code").GetString()==code).Clone();
    private async Task<JsonElement> Policy(SyntheticSession super,string code,object change)
    {
        var module=await Module(super,code);using var previewResponse=await Send(super,HttpMethod.Post,$"/api/v1/admin/modules/{I(module)}/preview",change);var preview=await Json(previewResponse);Assert.Empty(preview.GetProperty("blockers").EnumerateArray());
        var body=JsonSerializer.SerializeToElement(change).EnumerateObject().ToDictionary(p=>p.Name,p=>(object)p.Value.Clone());body["previewToken"]=preview.GetProperty("previewToken").GetString()!;
        using var commit=await Send(super,HttpMethod.Put,$"/api/v1/admin/modules/{I(module)}/policy",body,E(preview));return await Json(commit);
    }
    private async Task Grant(SyntheticSession super,SyntheticSession actor,string effect,params string[] actions)
    {
        using var current=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/admin/users/{actor.UserId}/access",super.RawSessionHandle);await Json(current);
        var changes=actions.Select(actionKey=>new{actionKey,effect}).ToArray();using var previewResponse=await Send(super,HttpMethod.Post,$"/api/v1/admin/users/{actor.UserId}/access/preview",new{kind="permissions",changes});var preview=await Json(previewResponse);
        using var commit=await Send(super,HttpMethod.Put,$"/api/v1/admin/users/{actor.UserId}/access/permissions",new{kind="permissions",changes,previewToken=preview.GetProperty("previewToken").GetString()},current.Headers.ETag?.Tag);await Json(commit);
    }

    [Fact] public async Task Project_projection_is_live_nullable_and_exactly_excludes_legacy_due_and_private_fields()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);
        const string acceptance="[\"Literal acceptance <script>\",{\"text\":\"Ordered checklist\",\"checked\":true}]";
        using var taskResponse=await Send(owner,HttpMethod.Post,"/api/v1/tasks",new{projectId=I(project),title="Shared task",description="Approved description",status="NotStarted",dueAt="2030-01-01T00:00:00Z",startAt="2001-01-01T00:00:00Z",endAt="2001-01-02T00:00:00Z",priority=(string?)null,tagsJson="[\"approved tag\"]",acceptanceCriteriaJson=acceptance,manageReminder=false});var task=await Json(taskResponse,HttpStatusCode.Created);
        var link=await Create(owner,I(project));using var resolved=await Resolve(link);var data=await Json(resolved);var shared=data.GetProperty("project").GetProperty("tasks")[0];
        Assert.Equal(JsonValueKind.Null,shared.GetProperty("priority").ValueKind);Assert.True(shared.GetProperty("isOverdue").GetBoolean());Assert.Equal(acceptance,shared.GetProperty("acceptanceCriteriaJson").GetString());Assert.Equal("[\"approved tag\"]",shared.GetProperty("tagsJson").GetString());
        foreach(var field in new[]{"dueAt","reminderAt","history","reason","ownerId","securityStamp"})Assert.False(shared.TryGetProperty(field,out _));
        await fixture.ExecuteAsync("UPDATE [productivity].[Task] SET Title='Changed live title',Status='Completed' WHERE Id=@Id AND OwnerId=@Owner",Id("@Id",I(task)),Id("@Owner",owner.OwnerId));
        using var live=await Resolve(link);var liveTask=(await Json(live)).GetProperty("project").GetProperty("tasks")[0];Assert.Equal("Changed live title",liveTask.GetProperty("title").GetString());Assert.False(liveTask.GetProperty("isOverdue").GetBoolean());
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",owner.SessionId));using var publicAfterLogout=await Resolve(link);Assert.Equal(HttpStatusCode.OK,publicAfterLogout.StatusCode);
    }

    [Fact] public async Task Each_Admin_source_AND_prerequisite_blocks_create_update_revoke_and_public_resolution()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        await fixture.ExecuteAsync("INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='Admin'",Id("@User",owner.UserId));
        var actions=new[]{"sharing.link.read","sharing.link.create","sharing.link.update","sharing.link.revoke","projects.project.read","projects.project.share"};await Grant(scope.Super,owner,"Allow",actions);
        foreach(var action in new[]{"sharing.link.read","projects.project.read","projects.project.share"})
        {
            await Grant(scope.Super,owner,"Deny",action);
            var expected = action == "sharing.link.read" ? HttpStatusCode.Conflict : HttpStatusCode.NotFound;
            using var create=await Send(owner,HttpMethod.Post,Links,new{resourceType="Project",resourceId=I(project),mode="PublicLink",noExpiry=true});Assert.Equal(expected,create.StatusCode);
            using var update=await Send(owner,HttpMethod.Patch,$"{Links}/{I(link)}",new{mode="PublicLink",noExpiry=true},E(link));Assert.Equal(expected,update.StatusCode);
            using var revoke=await Send(owner,HttpMethod.Delete,$"{Links}/{I(link)}",new{},E(link));Assert.Equal(expected,revoke.StatusCode);
            using var publicRead=await Resolve(link);Assert.Equal(HttpStatusCode.NotFound,publicRead.StatusCode);
            Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE OwnerId=@Owner AND IsDeleted=0",Id("@Owner",owner.OwnerId)));
            await Grant(scope.Super,owner,"Allow",action);
        }
        using var restored=await Resolve(link);Assert.Equal(HttpStatusCode.OK,restored.StatusCode);
    }

    [Fact] public async Task Registration_is_independent_and_global_sharing_disable_is_permanent_with_one_epoch_step()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        var epoch=await fixture.ScalarIntAsync("SELECT CONVERT(int,SharingEpoch) FROM [platform].[Module] WHERE Code='FX04'");
        foreach(var code in new[]{"FX04","FX11","FX20"})await Policy(scope.Super,code,new{registrationEnabled=false});
        using(var stillValid=await Resolve(link))Assert.Equal(HttpStatusCode.OK,stillValid.StatusCode);
        Assert.Equal(epoch,await fixture.ScalarIntAsync("SELECT CONVERT(int,SharingEpoch) FROM [platform].[Module] WHERE Code='FX04'"));
        await Policy(scope.Super,"FX04",new{sharingEnabled=false});Assert.Equal(epoch+1,await fixture.ScalarIntAsync("SELECT CONVERT(int,SharingEpoch) FROM [platform].[Module] WHERE Code='FX04'"));
        using(var invalid=await Resolve(link))Assert.Equal(HttpStatusCode.NotFound,invalid.StatusCode);
        await Policy(scope.Super,"FX04",new{sharingEnabled=true});using(var permanent=await Resolve(link))Assert.Equal(HttpStatusCode.NotFound,permanent.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE Id=@Id AND IsDeleted=1 AND InvalidatedAt IS NOT NULL",Id("@Id",I(link))));
        var replacement=await Create(owner,I(project));Assert.NotEqual(Token(link),Token(replacement));using var current=await Resolve(replacement);Assert.Equal(HttpStatusCode.OK,current.StatusCode);
    }

    [Fact] public async Task Source_disable_affects_its_own_links_and_rollback_preserves_links_policy_epoch_and_audit()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var doc=await Document(owner);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Published' WHERE Id=@Id",Id("@Id",I(doc)));
        var projectLink=await Create(owner,I(project));var documentLink=await Create(owner,I(doc),"Document");
        await Policy(scope.Super,"FX11",new{sharingEnabled=false});using(var invalid=await Resolve(projectLink))Assert.Equal(HttpStatusCode.NotFound,invalid.StatusCode);using(var unaffected=await Resolve(documentLink))Assert.Equal(HttpStatusCode.OK,unaffected.StatusCode);
        await Policy(scope.Super,"FX11",new{sharingEnabled=true});using(var permanent=await Resolve(projectLink))Assert.Equal(HttpStatusCode.NotFound,permanent.StatusCode);
        var before=await fixture.ScalarIntAsync("SELECT CONVERT(int,SharingEpoch) FROM [platform].[Module] WHERE Code='FX20'");
        await using(var connection=new SqlConnection(fixture.ConnectionString)){await connection.OpenAsync();using var tx=connection.BeginTransaction();using var update=new SqlCommand("UPDATE [platform].[Module] SET SharingEnabled=0 WHERE Code='FX20';",connection,tx);await update.ExecuteNonQueryAsync();tx.Rollback();}
        Assert.Equal(before,await fixture.ScalarIntAsync("SELECT CONVERT(int,SharingEpoch) FROM [platform].[Module] WHERE Code='FX20'"));using(var restored=await Resolve(documentLink))Assert.Equal(HttpStatusCode.OK,restored.StatusCode);
        await fixture.ExecuteAsync("UPDATE [platform].[Module] SET SystemEnabled=0 WHERE Code='FX20'; UPDATE [platform].[Module] SET SystemEnabled=1 WHERE Code='FX20';");using var systemPermanent=await Resolve(documentLink);Assert.Equal(HttpStatusCode.NotFound,systemPermanent.StatusCode);
    }

    [Fact] public async Task Document_Draft_suspends_but_metadata_and_revoke_work_without_reviving_invalidated_tokens()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var doc=await Document(owner);
        using(var draftCreate=await Send(owner,HttpMethod.Post,Links,new{resourceType="Document",resourceId=I(doc),mode="PublicLink",noExpiry=true}))Assert.Equal(HttpStatusCode.NotFound,draftCreate.StatusCode);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Published' WHERE Id=@Id",Id("@Id",I(doc)));var link=await Create(owner,I(doc),"Document");
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Draft' WHERE Id=@Id",Id("@Id",I(doc)));using(var suspended=await Resolve(link))Assert.Equal(HttpStatusCode.NotFound,suspended.StatusCode);
        using var updatedResponse=await Send(owner,HttpMethod.Patch,$"{Links}/{I(link)}",new{mode="PublicLink",noExpiry=true},E(link));var updated=await Json(updatedResponse);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Archived',PreArchiveStatus='Published' WHERE Id=@Id",Id("@Id",I(doc)));using(var archived=await Resolve(link))Assert.Equal(HttpStatusCode.OK,archived.StatusCode);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Draft',PreArchiveStatus=NULL WHERE Id=@Id",Id("@Id",I(doc)));using(var revoke=await Send(owner,HttpMethod.Delete,$"{Links}/{I(link)}",new{},E(updated)))Assert.Equal(HttpStatusCode.NoContent,revoke.StatusCode);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Published' WHERE Id=@Id",Id("@Id",I(doc)));using(var permanent=await Resolve(link))Assert.Equal(HttpStatusCode.NotFound,permanent.StatusCode);
        Assert.Empty((await Get(owner,Links)).GetProperty("items").EnumerateArray());
    }

    [Fact] public async Task All_audiences_expiry_and_foreign_owner_commands_are_enforced_without_title_disclosure()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var allowed=await fixture.CreateActiveSessionAsync();var wrong=await fixture.CreateActiveSessionAsync();var project=await Project(owner);
        var auth=await Create(owner,I(project),mode:"AuthenticatedLink");var restricted=await Create(owner,I(project),mode:"RestrictedUsers",users:[allowed.UserId]);
        using(var anon=await Resolve(auth))Assert.Equal(HttpStatusCode.NotFound,anon.StatusCode);using(var eligible=await Resolve(auth,allowed))Assert.Equal(HttpStatusCode.OK,eligible.StatusCode);
        using(var incorrect=await Resolve(restricted,wrong)){Assert.Equal(HttpStatusCode.NotFound,incorrect.StatusCode);Assert.DoesNotContain("Synthetic shared project",await incorrect.Content.ReadAsStringAsync());}
        using(var correct=await Resolve(restricted,allowed))Assert.Equal(HttpStatusCode.OK,correct.StatusCode);
        using(var foreign=await Send(wrong,HttpMethod.Patch,$"{Links}/{I(restricted)}",new{mode="PublicLink",noExpiry=true},E(restricted)))Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",allowed.SessionId));using(var revokedViewer=await Resolve(restricted,allowed))Assert.Equal(HttpStatusCode.NotFound,revokedViewer.StatusCode);
        await fixture.ExecuteAsync("UPDATE [security].[ShareLink] SET ExpiresAt=SYSUTCDATETIME() WHERE Id=@Id",Id("@Id",I(auth)));using var expired=await Resolve(auth,wrong);Assert.Equal(HttpStatusCode.NotFound,expired.StatusCode);Assert.DoesNotContain("Synthetic shared project",await expired.Content.ReadAsStringAsync());
    }

    [Fact] public async Task Stale_principals_policy_preview_and_unsupported_policy_activation_fail_closed_in_SQL()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        var sharing=new SqlSharingService(new SqlConnectionFactory(fixture.ConnectionString!));var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,"User",DateTimeOffset.UtcNow,owner.SessionId);
        Assert.True(sharing.List(actor).Succeeded);await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",owner.SessionId));
        Assert.False(sharing.List(actor).Succeeded);Assert.False(sharing.Update(actor,I(link),E(link),new("PublicLink",null,[],true),Guid.NewGuid().ToString()).Succeeded);
        var policy=new SqlModulePolicyService(new SqlConnectionFactory(fixture.ConnectionString!));var superActor=new IdentityPrincipal(scope.Super.UserId,scope.Super.OwnerId,"SuperAdmin",DateTimeOffset.UtcNow,scope.Super.SessionId);var module=await Module(scope.Super,"FX04");
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RecentAuthenticatedAt=DATEADD(minute,-6,SYSUTCDATETIME()) WHERE Id=@Session",Id("@Session",scope.Super.SessionId));Assert.False(policy.Preview(I(module),new(null,null,false),superActor).Succeeded);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RecentAuthenticatedAt=SYSUTCDATETIME() WHERE Id=@Session",Id("@Session",scope.Super.SessionId));var unsupported=await Module(scope.Super,"FX12");using var previewResponse=await Send(scope.Super,HttpMethod.Post,$"/api/v1/admin/modules/{I(unsupported)}/preview",new{sharingEnabled=true});var preview=await Json(previewResponse);Assert.Contains(preview.GetProperty("blockers").EnumerateArray(),b=>b.GetProperty("code").GetString()=="ProviderContractUnavailable");
        using var commit=await Send(scope.Super,HttpMethod.Put,$"/api/v1/admin/modules/{I(unsupported)}/policy",new{sharingEnabled=true,previewToken=preview.GetProperty("previewToken").GetString()},E(preview));Assert.Equal(HttpStatusCode.Conflict,commit.StatusCode);
    }

    [Fact] public async Task Normal_upgrade_carries_all_three_modes_preserves_hashes_invalidations_and_replays()
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
            await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames.TakeWhile(n=>n!="20261004_0043_sharing_policy_authority.sql").ToArray());
            await using var connection=new SqlConnection(target.ConnectionString);await connection.OpenAsync();var owner=Guid.NewGuid();var user=Guid.NewGuid();var project=Guid.NewGuid();var doc=Guid.NewGuid();
            // Create in the connection's outer scope, not inside parameterized sp_executesql.
            using(var snapshot=new SqlCommand("CREATE TABLE #Before(Id uniqueidentifier,TokenHash binary(32),ResourceType varchar(32),ResourceId uniqueidentifier,Mode varchar(32),ExpiresAt datetime2(7) NULL,IssuedSharingEpoch bigint,IsDeleted bit,RevokedAt datetime2(7) NULL,InvalidatedAt datetime2(7) NULL,InvalidationReason varchar(64) NULL);",connection))await snapshot.ExecuteNonQueryAsync();
            using(var seed=new SqlCommand("""
                UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1 WHERE Code IN('FX04','FX11','FX20');
                INSERT [identity].[User](Id,Email,NormalizedEmail,PasswordHash,SecurityStamp,State,EmailConfirmed,VerifiedAt,IsDeleted,DisplayName,TimeZoneId,Locale)
                VALUES(@User,'synthetic-sharing-upgrade@example.invalid','SYNTHETIC-SHARING-UPGRADE@EXAMPLE.INVALID','synthetic-no-login-hash','synthetic-no-session','Active',1,SYSUTCDATETIME(),0,'Synthetic migration','UTC','en');
                INSERT [platform].[PersonalSpace](Id,UserId,State,CreatedAt,UpdatedAt) VALUES(@Owner,@User,'Active',SYSUTCDATETIME(),SYSUTCDATETIME());
                INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='User';
                INSERT [platform].[UserModuleGrant](UserId,ModuleId,Enabled) SELECT @User,Id,1 FROM [platform].[Module] WHERE State='Ready' AND SystemEnabled=1;
                INSERT [productivity].[Project](Id,OwnerId,Name,Description,Status,StartAt,EndAt,Priority)
                VALUES(@Project,@Owner,'Preserved project','Approved existing source','NotStarted','2030-01-01','2030-01-02','P2');
                INSERT [documents].[Page](Id,OwnerId,Title,DocumentType,EditorMode,Body,Status)
                VALUES(@Document,@Owner,'Preserved document','Note','Markdown','Approved existing source','Published');
                INSERT [security].[ShareLink](Id,OwnerId,CreatedByUserId,UpdatedByUserId,ResourceType,ResourceId,TokenHash,Mode,ExpiresAt,IssuedSharingEpoch)
                SELECT NEWID(),@Owner,@User,@User,source.ResourceType,source.ResourceId,CRYPT_GEN_RANDOM(32),mode.Mode,DATEADD(day,7,SYSUTCDATETIME()),m.SharingEpoch
                FROM (VALUES('Project',@Project),('Document',@Document)) source(ResourceType,ResourceId)
                CROSS JOIN (VALUES('PublicLink'),('AuthenticatedLink'),('RestrictedUsers')) mode(Mode)
                CROSS JOIN [platform].[Module] m WHERE m.Code='FX04';
                INSERT [security].[ShareAllowedUser](OwnerId,ShareLinkId,UserId) SELECT @Owner,Id,@User FROM [security].[ShareLink] WHERE OwnerId=@Owner AND Mode='RestrictedUsers';
                INSERT [security].[ShareLink](OwnerId,CreatedByUserId,UpdatedByUserId,ResourceType,ResourceId,TokenHash,Mode,IsDeleted,InvalidatedAt,InvalidationReason,RevokedAt)
                VALUES(@Owner,@User,@User,'Project',@Project,CRYPT_GEN_RANDOM(32),'PublicLink',1,SYSUTCDATETIME(),'SourceDeleted',SYSUTCDATETIME());
                INSERT [security].[ShareLink](OwnerId,CreatedByUserId,UpdatedByUserId,ResourceType,ResourceId,TokenHash,Mode,ExpiresAt)
                VALUES(@Owner,@User,@User,'Project',@Project,CRYPT_GEN_RANDOM(32),'PublicLink',DATEADD(day,-1,SYSUTCDATETIME()));
                INSERT #Before SELECT Id,TokenHash,ResourceType,ResourceId,Mode,ExpiresAt,IssuedSharingEpoch,IsDeleted,RevokedAt,InvalidatedAt,InvalidationReason FROM [security].[ShareLink] WHERE OwnerId=@Owner;
                """,connection)){seed.Parameters.Add(Id("@User",user));seed.Parameters.Add(Id("@Owner",owner));seed.Parameters.Add(Id("@Project",project));seed.Parameters.Add(Id("@Document",doc));await seed.ExecuteNonQueryAsync();}
            using(var legacy=new SqlCommand("SELECT COUNT(*) FROM [security].[ShareLink] l JOIN [platform].[Module] m ON m.Code='FX04' WHERE l.OwnerId=@Owner AND l.IsDeleted=0 AND l.RevokedAt IS NULL AND l.ExpiresAt>SYSUTCDATETIME() AND l.IssuedSharingEpoch=m.SharingEpoch AND m.RegistrationEnabled=1 AND m.SystemEnabled=1 AND m.State='Ready';",connection)){legacy.Parameters.Add(Id("@Owner",owner));Assert.Equal(6,Convert.ToInt32(await legacy.ExecuteScalarAsync()));}
            await VerifyLegacyBackupRestore(control,connection,name,owner);
            await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames);await runner.ApplyAsync(target.ConnectionString,directory,M01MigrationManifest.RequiredFileNames);
            using(var unchanged=new SqlCommand("""
                SELECT COUNT(*) FROM (
                    SELECT Id,TokenHash,ResourceType,ResourceId,Mode,ExpiresAt,IssuedSharingEpoch,IsDeleted,RevokedAt,InvalidatedAt,InvalidationReason FROM #Before
                    EXCEPT SELECT Id,TokenHash,ResourceType,ResourceId,Mode,ExpiresAt,IssuedSharingEpoch,IsDeleted,RevokedAt,InvalidatedAt,InvalidationReason FROM [security].[ShareLink] WHERE OwnerId=@Owner
                ) changes;
                """,connection)){unchanged.Parameters.Add(Id("@Owner",owner));Assert.Equal(0,Convert.ToInt32(await unchanged.ExecuteScalarAsync()));}
            using(var policy=new SqlCommand("SELECT COUNT(*) FROM [platform].[Module] WHERE (Code IN('FX04','FX11','FX20') AND SharingEnabled<>RegistrationEnabled) OR (Code NOT IN('FX04','FX11','FX20') AND SharingEnabled<>0);",connection))Assert.Equal(0,Convert.ToInt32(await policy.ExecuteScalarAsync()));
            using(var registration=new SqlCommand("UPDATE [platform].[Module] SET RegistrationEnabled=0 WHERE Code IN('FX04','FX11','FX20'); SELECT COUNT(*) FROM [security].[ShareLink] WHERE OwnerId=@Owner AND IsDeleted=0 AND ExpiresAt>SYSUTCDATETIME();",connection)){registration.Parameters.Add(Id("@Owner",owner));Assert.Equal(6,Convert.ToInt32(await registration.ExecuteScalarAsync()));}
            using(var sourceDisable=new SqlCommand("UPDATE [platform].[Module] SET SharingEnabled=0 WHERE Code='FX11'; UPDATE [platform].[Module] SET SharingEnabled=1 WHERE Code='FX11'; SELECT COUNT(*) FROM [security].[ShareLink] WHERE OwnerId=@Owner AND ResourceType='Document' AND IsDeleted=0;",connection)){sourceDisable.Parameters.Add(Id("@Owner",owner));Assert.Equal(3,Convert.ToInt32(await sourceDisable.ExecuteScalarAsync()));}
        }
        finally
        {
            // Clean only the exact generated, absence-checked fixture DB; retained browser data is untouched.
            if(created){SqlConnection.ClearAllPools();using var drop=new SqlCommand($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}];",control);await drop.ExecuteNonQueryAsync();}
        }
    }

    private static string SqlLiteral(string value)=>"N'"+value.Replace("'","''",StringComparison.Ordinal)+"'";
    private static async Task VerifyLegacyBackupRestore(SqlConnection control,SqlConnection source,string sourceName,Guid owner)
    {
        var restoreName="Nexora_Test_"+Guid.NewGuid().ToString("N");
        string backupRoot,dataRoot,logRoot;
        using(var paths=new SqlCommand("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')),CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')),CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'));",control))
        {using var reader=await paths.ExecuteReaderAsync();Assert.True(await reader.ReadAsync());backupRoot=reader.GetString(0);dataRoot=reader.GetString(1);logRoot=reader.GetString(2);}
        static string Under(string parent,string leaf)
        {
            Assert.False(string.IsNullOrWhiteSpace(parent));Assert.DoesNotContain("..",leaf);Assert.DoesNotContain("/",leaf);Assert.DoesNotContain("\\",leaf);
            var separator=parent.Contains('\\')?"\\":"/";return parent.TrimEnd('\\','/')+separator+leaf;
        }
        var backup=Under(backupRoot,"nexora-sharing-"+Guid.NewGuid().ToString("N")+".bak");
        var data=Under(dataRoot,restoreName+".mdf");var log=Under(logRoot,restoreName+"_log.ldf");
        using(var absent=new SqlCommand("SELECT DB_ID(@Name)",control)){absent.Parameters.AddWithValue("@Name",restoreName);Assert.IsType<DBNull>(await absent.ExecuteScalarAsync());}
        using(var absentFile=new SqlCommand("DECLARE @Exists int; EXEC master.dbo.xp_fileexist @Path,@Exists OUTPUT; SELECT @Exists;",control)){absentFile.Parameters.AddWithValue("@Path",backup);Assert.Equal(0,Convert.ToInt32(await absentFile.ExecuteScalarAsync()));}
        var names=new Dictionary<int,string>();using(var files=new SqlCommand("SELECT name,type FROM sys.database_files",source)){using var reader=await files.ExecuteReaderAsync();while(await reader.ReadAsync())names.Add(reader.GetByte(1),reader.GetString(0));}Assert.Equal(2,names.Count);
        var backupCreated=false;var restoreAttempted=false;
        try
        {
            using(var save=new SqlCommand($"BACKUP DATABASE [{sourceName}] TO DISK=@Backup WITH COPY_ONLY,INIT,CHECKSUM;",control)){save.Parameters.AddWithValue("@Backup",backup);await save.ExecuteNonQueryAsync();backupCreated=true;}
            restoreAttempted=true;
            using(var restore=new SqlCommand($"RESTORE DATABASE [{restoreName}] FROM DISK=@Backup WITH CHECKSUM,MOVE {SqlLiteral(names[0])} TO {SqlLiteral(data)},MOVE {SqlLiteral(names[1])} TO {SqlLiteral(log)};",control)){restore.Parameters.AddWithValue("@Backup",backup);await restore.ExecuteNonQueryAsync();}
            using(var schema=new SqlCommand($"SELECT COUNT(*) FROM [{restoreName}].sys.columns c JOIN [{restoreName}].sys.tables t ON t.object_id=c.object_id JOIN [{restoreName}].sys.schemas s ON s.schema_id=t.schema_id WHERE s.name='platform' AND t.name='Module' AND c.name='SharingEnabled';",control))Assert.Equal(0,Convert.ToInt32(await schema.ExecuteScalarAsync()));
            using(var preserved=new SqlCommand($"SELECT COUNT(*) FROM (SELECT Id,TokenHash,ResourceType,ResourceId,Mode,ExpiresAt,IssuedSharingEpoch,IsDeleted,RevokedAt,InvalidatedAt,InvalidationReason FROM #Before EXCEPT SELECT Id,TokenHash,ResourceType,ResourceId,Mode,ExpiresAt,IssuedSharingEpoch,IsDeleted,RevokedAt,InvalidatedAt,InvalidationReason FROM [{restoreName}].[security].[ShareLink] WHERE OwnerId=@Owner) differences;",source)){preserved.Parameters.Add(Id("@Owner",owner));Assert.Equal(0,Convert.ToInt32(await preserved.ExecuteScalarAsync()));}
            using(var count=new SqlCommand($"SELECT COUNT(*) FROM [{restoreName}].[security].[ShareLink] WHERE OwnerId=@Owner",control)){count.Parameters.Add(Id("@Owner",owner));Assert.Equal(8,Convert.ToInt32(await count.ExecuteScalarAsync()));}
        }
        finally
        {
            // Restore target was absence-checked and generated here; never touch retained databases.
            if(restoreAttempted){using var drop=new SqlCommand($"IF DB_ID(N'{restoreName}') IS NOT NULL BEGIN ALTER DATABASE [{restoreName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{restoreName}]; END",control);await drop.ExecuteNonQueryAsync();}
            // Exact new server-local filename only; no wildcard, recursive or foreign cleanup.
            if(backupCreated){using var remove=new SqlCommand("EXEC master.sys.xp_delete_files @Path",control);remove.Parameters.AddWithValue("@Path",backup);await remove.ExecuteNonQueryAsync();}
        }
    }

    [Fact] public async Task Policy_preview_discloses_exact_invalidation_cohort_and_rejects_unreviewed_new_links()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        await fixture.ExecuteAsync("UPDATE [security].[ShareLink] SET ExpiresAt=DATEADD(day,-1,SYSUTCDATETIME()) WHERE Id=@Id",Id("@Id",I(link)));
        var document=await Document(owner);await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Published' WHERE Id=@Id",Id("@Id",I(document)));await Create(owner,I(document),"Document");await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Draft' WHERE Id=@Id",Id("@Id",I(document)));
        var module=await Module(scope.Super,"FX04");var count=await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE IsDeleted=0");
        using var previewResponse=await Send(scope.Super,HttpMethod.Post,$"/api/v1/admin/modules/{I(module)}/preview",new{sharingEnabled=false});var preview=await Json(previewResponse);
        Assert.Equal(count,preview.GetProperty("affectedSharingLinks").GetInt64());Assert.True(preview.GetProperty("affectedUsers").GetInt64()>0);
        var added=await Create(owner,I(project));using var stale=await Send(scope.Super,HttpMethod.Put,$"/api/v1/admin/modules/{I(module)}/policy",new{sharingEnabled=false,previewToken=preview.GetProperty("previewToken").GetString()},E(preview));var rejected=await Json(stale,HttpStatusCode.Conflict);Assert.Equal("PreviewStale",rejected.GetProperty("code").GetString());
        Assert.True((await Module(scope.Super,"FX04")).GetProperty("sharingEnabled").GetBoolean());using(var retained=await Resolve(added))Assert.Equal(HttpStatusCode.OK,retained.StatusCode);
        await Policy(scope.Super,"FX04",new{sharingEnabled=false});Assert.Equal(0,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE IsDeleted=0"));
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    [InlineData(true,true)]
    public async Task Installed_readiness_preserves_flags_replays_and_rejects_automatic_activation(bool system,bool sharing)
    {
        await using var scope=await Enable();
        await fixture.ExecuteAsync("UPDATE [platform].[Module] SET State='Blocked',SystemEnabled=@System,SharingEnabled=@Sharing WHERE Code='FX04'",new SqlParameter("@System",SqlDbType.Bit){Value=system},new SqlParameter("@Sharing",SqlDbType.Bit){Value=sharing});
        var before=await Module(scope.Super,"FX04");
        var root=AppContext.BaseDirectory;while(!Directory.Exists(Path.Combine(root,"database","migrations"))){root=Directory.GetParent(root)?.FullName??throw new InvalidOperationException("Migration source unavailable.");}
        var sql=await File.ReadAllTextAsync(Path.Combine(root,"database","migrations","20261004_0044_sharing_subset_readiness.sql"));
        if(system&&sharing){await Assert.ThrowsAsync<SqlException>(()=>fixture.ExecuteAsync(sql));var rejected=await Module(scope.Super,"FX04");Assert.Equal("Blocked",rejected.GetProperty("state").GetString());Assert.Equal(E(before),E(rejected));return;}
        await fixture.ExecuteAsync(sql);var installed=await Module(scope.Super,"FX04");Assert.Equal("Ready",installed.GetProperty("state").GetString());Assert.Equal(system,installed.GetProperty("systemEnabled").GetBoolean());Assert.Equal(sharing,installed.GetProperty("sharingEnabled").GetBoolean());Assert.Equal(before.GetProperty("registrationEnabled").GetBoolean(),installed.GetProperty("registrationEnabled").GetBoolean());
        await fixture.ExecuteAsync(sql);var replay=await Module(scope.Super,"FX04");Assert.Equal(E(installed),E(replay));
    }
    [Fact] public async Task Global_policy_disable_and_resolve_finish_without_a_policy_link_deadlock()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        await using var connection=new SqlConnection(fixture.ConnectionString);await connection.OpenAsync();using var transaction=connection.BeginTransaction();
        using(var hold=new SqlCommand("SELECT Id FROM [platform].[Module] WITH(XLOCK,HOLDLOCK) WHERE Code='FX04'",connection,transaction))Assert.NotNull(await hold.ExecuteScalarAsync());
        var request=Resolve(link);var watch=System.Diagnostics.Stopwatch.StartNew();var blocked=false;
        while(watch.Elapsed<TimeSpan.FromSeconds(10))
        {
            using var waiting=new SqlCommand("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND blocking_session_id=@@SPID",connection,transaction);
            if(Convert.ToInt32(await waiting.ExecuteScalarAsync())>0){blocked=true;break;}await Task.Delay(20);
        }
        Assert.True(blocked,"Resolve must wait for the real policy boundary.");
        using(var disable=new SqlCommand("UPDATE [platform].[Module] SET SharingEnabled=0 WHERE Code='FX04'",connection,transaction)){disable.CommandTimeout=5;await disable.ExecuteNonQueryAsync();}
        transaction.Commit();using var response=await request.WaitAsync(TimeSpan.FromSeconds(10));Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE Id=@Id AND IsDeleted=1 AND InvalidatedAt IS NOT NULL",Id("@Id",I(link))));
    }
    [Fact]
    public async Task Copy_created_authority_is_owner_scoped_current_and_never_returns_token()
    {
        await using var scope = await Enable();
        var owner = await fixture.CreateActiveSessionAsync();
        var other = await fixture.CreateActiveSessionAsync();
        var project = await Project(owner); var link = await Create(owner, I(project));
        var path = $"{Links}/{I(link)}/copy-capability";
        var capability = await Get(owner, path);
        Assert.True(capability.GetProperty("allowed").GetBoolean());
        Assert.Single(capability.EnumerateObject());
        using (var foreign = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, other.RawSessionHandle))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        await fixture.ExecuteAsync("INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='Admin'", Id("@User", owner.UserId));
        await Grant(scope.Super, owner, "Allow", "sharing.link.read", "sharing.link.create", "sharing.link.revoke", "projects.project.read", "projects.project.share");
        await fixture.ExecuteAsync("DELETE p FROM [platform].[AdminPermission] p JOIN [platform].[Permission] a ON a.Id=p.PermissionId WHERE p.UserId=@User AND a.ActionKey='sharing.link.create'", Id("@User", owner.UserId));
        using (var absent = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle))
            Assert.Equal(HttpStatusCode.Conflict, absent.StatusCode);
        await Grant(scope.Super, owner, "Deny", "sharing.link.create");
        using (var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle))
            Assert.Equal(HttpStatusCode.Conflict, denied.StatusCode);
        await Grant(scope.Super, owner, "Allow", "sharing.link.create");
        using (var revoked = await Send(owner, HttpMethod.Delete, $"{Links}/{I(link)}", new { }, E(link)))
            Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        using var unavailable = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode);
    }

    [Fact]
    public async Task Owner_item_read_and_exact_update_retry_return_current_metadata_without_repeating_audit()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        var project = await Project(owner); var link = await Create(owner, I(project)); var path = $"{Links}/{I(link)}";
        var initial = await Get(owner, path); Assert.Equal(I(link), I(initial));
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("token").ValueKind);
        var other = await fixture.CreateActiveSessionAsync();
        using (var foreign = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, other.RawSessionHandle))
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        var key = Guid.NewGuid(); var body = new { mode = "AuthenticatedLink", noExpiry = true };
        using var first = await Send(owner, HttpMethod.Patch, path, body, E(initial), key); var updated = await Json(first);
        using var later = await Send(owner, HttpMethod.Patch, path, new { mode = "PublicLink", noExpiry = true }, E(updated)); var latest = await Json(later);
        var audits = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='sharing.link.update'", Id("@Id", I(link)));
        using var retry = await Send(owner, HttpMethod.Patch, path, body, E(initial), key); var recovered = await Json(retry);
        Assert.Equal(E(latest), E(recovered)); Assert.Equal("PublicLink", recovered.GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, recovered.GetProperty("token").ValueKind);
        Assert.Equal(audits, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='sharing.link.update'", Id("@Id", I(link))));
        using var mismatch = await Send(owner, HttpMethod.Patch, path, new { mode = "PublicLink", noExpiry = true }, E(initial), key);
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        await fixture.ExecuteAsync("INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='Admin'", Id("@User", owner.UserId));
        await Grant(scope.Super, owner, "Allow", "sharing.link.read", "projects.project.read", "projects.project.share");
        using var deniedReplay = await Send(owner, HttpMethod.Patch, path, body, E(initial), key);
        Assert.Equal(HttpStatusCode.Conflict, deniedReplay.StatusCode);
    }

    [Fact]
    public async Task Exact_committed_update_replay_after_expiry_does_not_extend_or_reactivate_the_link()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        var project = await Project(owner); var link = await Create(owner, I(project)); var path = $"{Links}/{I(link)}";
        var key = Guid.NewGuid(); var expiry = DateTimeOffset.UtcNow.AddSeconds(4);
        var body = new { mode = "PublicLink", expiresAt = expiry, noExpiry = false };
        using var first = await Send(owner, HttpMethod.Patch, path, body, E(link), key); var committed = await Json(first);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE Id=@Id AND ExpiresAt>SYSUTCDATETIME()", Id("@Id", I(link))) != 0)
        { Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15)); await Task.Delay(50); }
        using var retry = await Send(owner, HttpMethod.Patch, path, body, E(link), key); var recovered = await Json(retry);
        Assert.Equal(E(committed), E(recovered)); Assert.False(recovered.GetProperty("isActive").GetBoolean());
        Assert.Equal(committed.GetProperty("expiresAt").GetString(), recovered.GetProperty("expiresAt").GetString());
        using var fresh = await Send(owner, HttpMethod.Patch, path, body, E(recovered)); Assert.Equal(HttpStatusCode.UnprocessableEntity, fresh.StatusCode);
        using var viewer = await Resolve(link); Assert.Equal(HttpStatusCode.NotFound, viewer.StatusCode);
    }

    [Fact]
    public async Task Copy_created_checks_current_document_projection_lifecycle_and_version()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        var document = await Document(owner);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Published' WHERE Id=@Id", Id("@Id", I(document)));
        var link = await Create(owner, I(document), "Document"); var path = $"{Links}/{I(link)}/copy-capability";
        Assert.True((await Get(owner, path)).GetProperty("allowed").GetBoolean());
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Draft' WHERE Id=@Id", Id("@Id", I(document)));
        using (var draft = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle))
            Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
        await fixture.ExecuteAsync("UPDATE [documents].[Page] SET Status='Archived',PreArchiveStatus='Published' WHERE Id=@Id", Id("@Id", I(document)));
        Assert.True((await Get(owner, path)).GetProperty("allowed").GetBoolean());
        await fixture.ExecuteAsync("UPDATE [security].[ShareLink] SET ProjectionVersion='unsupported' WHERE Id=@Id", Id("@Id", I(link)));
        using var unsupported = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, unsupported.StatusCode);
    }

    [Theory]
    [InlineData("Copy")]
    [InlineData("List")]
    [InlineData("Resolve")]
    [InlineData("Update")]
    [InlineData("Revoke")]
    public async Task Source_delete_and_sharing_source_first_reads_finish_without_a_link_source_deadlock(string operation)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var project=await Project(owner);var link=await Create(owner,I(project));
        await using var connection=new SqlConnection(fixture.ConnectionString);await connection.OpenAsync();using var transaction=connection.BeginTransaction();
        using(var hold=new SqlCommand("SELECT Id FROM [productivity].[Project] WITH(XLOCK,HOLDLOCK) WHERE Id=@Id",connection,transaction)){hold.Parameters.Add(Id("@Id",I(project)));Assert.NotNull(await hold.ExecuteScalarAsync());}
        Task<HttpResponseMessage> request=operation switch {
            "Copy"=>fixture.SendAuthenticatedAsync(HttpMethod.Get,$"{Links}/{I(link)}/copy-capability",owner.RawSessionHandle),
            "List"=>fixture.SendAuthenticatedAsync(HttpMethod.Get,Links,owner.RawSessionHandle),
            "Resolve"=>Resolve(link),
            "Update"=>Send(owner,HttpMethod.Patch,$"{Links}/{I(link)}",new{mode="PublicLink",noExpiry=true},E(link)),
            _=>Send(owner,HttpMethod.Delete,$"{Links}/{I(link)}",new{},E(link))
        };
        // Coordinate on actual SQL blocking, not a guessed business-clock delay.
        var watch=System.Diagnostics.Stopwatch.StartNew();var blocked=false;
        while(watch.Elapsed<TimeSpan.FromSeconds(10))
        {
            using var waiting=new SqlCommand("SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND blocking_session_id=@@SPID",connection,transaction);
            if(Convert.ToInt32(await waiting.ExecuteScalarAsync())>0){blocked=true;break;}
            await Task.Delay(20);
        }
        Assert.True(blocked,"The real sharing request must reach the held source boundary.");
        using(var deleted=new SqlCommand("UPDATE [productivity].[Project] SET Status='Deleted',DeletedAt=SYSUTCDATETIME() WHERE Id=@Id",connection,transaction)){deleted.CommandTimeout=5;deleted.Parameters.Add(Id("@Id",I(project)));await deleted.ExecuteNonQueryAsync();}
        transaction.Commit();using var response=await request.WaitAsync(TimeSpan.FromSeconds(10));
        if(operation=="List")Assert.Empty((await Json(response)).GetProperty("items").EnumerateArray());else Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[ShareLink] WHERE Id=@Id AND IsDeleted=1 AND InvalidatedAt IS NOT NULL",Id("@Id",I(link))));
        using var permanent=await Resolve(link);Assert.Equal(HttpStatusCode.NotFound,permanent.StatusCode);
    }
}
