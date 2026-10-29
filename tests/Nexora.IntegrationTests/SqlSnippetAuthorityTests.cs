using System.Data;
using System.Net;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Snippets;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Snippets;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlSnippetAuthorityTests(SqlApiFixture fixture)
{
    private SqlSnippetService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-snippet-authority");
    private static IdentityPrincipal Actor(SyntheticSession session, string role = "User") => new(session.UserId, session.OwnerId, role, DateTimeOffset.UtcNow, session.SessionId);
    private static SqlParameter Id(string key, Guid value) => new(key, SqlDbType.UniqueIdentifier) { Value = value };
    private Task Grant(SyntheticSession session, string action) => fixture.ExecuteAsync("INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,'Allow' FROM [platform].[Permission] WHERE ActionKey=@Action;", Id("@User",session.UserId), new SqlParameter("@Action",SqlDbType.NVarChar,160){Value=action});

    [Fact]
    public async Task Detail_API_exposes_current_ETag_only_to_the_authorized_owner()
    {
        fixture.RequireAvailable();var session=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();
        var created=Service().Create(Actor(session),new("ETag detail","plaintext","Synthetic detail source",null));Assert.True(created.Succeeded,created.Code);
        using var readable=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/snippets/{created.Value!.Id}",session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK,readable.StatusCode);Assert.Equal(created.Value.ETag,readable.Headers.ETag?.ToString());
        using var foreign=await fixture.SendAuthenticatedAsync(HttpMethod.Get,$"/api/v1/snippets/{created.Value.Id}",other.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound,foreign.StatusCode);Assert.Null(foreign.Headers.ETag);Assert.DoesNotContain("Synthetic detail source",await foreign.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_and_historical_replay_never_disclose_source_without_current_read()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync("Admin"); var actor=Actor(session,"Admin");
        var service=Service(); var key=Guid.NewGuid().ToString("D"); var body=new SnippetCommand("Private synthetic snippet","plaintext","Synthetic source only",null);
        Assert.All(service.Capabilities(actor).Value!, pair => Assert.False(pair.Value));
        Assert.False(service.Create(actor,body,key).Succeeded);
        await Grant(session,"snippets.snippet.create");
        var capabilities=service.Capabilities(actor); Assert.True(capabilities.Succeeded,capabilities.Code); Assert.True(capabilities.Value!["snippets.snippet.create"]); Assert.False(capabilities.Value["snippets.snippet.read"]);
        var created=service.Create(actor,body,key); Assert.True(created.Succeeded,created.Code); Assert.Null(created.Value); Assert.NotNull(created.Acknowledgement);
        Assert.Equal(created,service.Create(actor,body,key));
        await Grant(session,"snippets.snippet.read");
        var readable=service.Create(actor,body,key); Assert.True(readable.Succeeded,readable.Code); Assert.Equal(body.Body,readable.Value!.Body);
        await Grant(session,"snippets.snippet.save");
        var saved=service.Save(actor,readable.Value.Id,readable.Value.ETag,body with { Body="Next synthetic source" },Guid.NewGuid().ToString("D")); Assert.True(saved.Succeeded,saved.Code);
        var historical=service.Create(actor,body,key); Assert.True(historical.Succeeded,historical.Code); Assert.Null(historical.Value); Assert.Equal(created.Acknowledgement,historical.Acknowledgement);
        await fixture.ExecuteAsync("UPDATE g SET Effect='Deny' FROM [platform].[AdminPermission] g JOIN [platform].[Permission] p ON p.Id=g.PermissionId WHERE g.UserId=@User AND p.ActionKey='snippets.snippet.read';",Id("@User",session.UserId));
        var privateSave=service.Save(actor,saved.Value!.Id,saved.Value.ETag,body,Guid.NewGuid().ToString("D")); Assert.True(privateSave.Succeeded,privateSave.Code); Assert.Null(privateSave.Value); Assert.NotNull(privateSave.Acknowledgement);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Session;",Id("@Session",session.SessionId));
        Assert.False(service.Capabilities(actor).Succeeded);
        Assert.False(service.Create(actor,body).Succeeded); Assert.False(service.Create(actor,body,key).Succeeded); Assert.False(service.Get(actor,readable.Value.Id).Succeeded);
        Assert.Equal(3,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [knowledge].[SnippetVersion] WHERE OwnerId=@Owner;",Id("@Owner",session.OwnerId)));
    }

    [Fact]
    public async Task All_pages_are_reachable_and_cursor_is_owner_filter_limit_and_integrity_bound()
    {
        fixture.RequireAvailable(); var session=await fixture.CreateActiveSessionAsync();var other=await fixture.CreateActiveSessionAsync();var actor=Actor(session);var service=Service();var ids=new HashSet<Guid>();
        for(var i=0;i<27;i++){var result=service.Create(actor,new($"Page {i:D3}","plaintext","Synthetic text",null));Assert.True(result.Succeeded,result.Code);ids.Add(result.Value!.Id);}
        var first=service.List(actor);Assert.True(first.Succeeded,first.Code);Assert.Equal(25,first.Value!.Items.Count);Assert.NotNull(first.Value.NextCursor);
        var next=service.List(actor,cursor:first.Value.NextCursor);Assert.True(next.Succeeded,next.Code);Assert.Equal(2,next.Value!.Items.Count);Assert.Null(next.Value.NextCursor);Assert.Equal(ids.Order(),first.Value.Items.Concat(next.Value.Items).Select(x=>x.Id).Order());
        Assert.Equal("InvalidCursor",service.List(Actor(other),cursor:first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor",service.List(actor,includeArchived:true,cursor:first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor",service.List(actor,query:"Page",cursor:first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor",service.List(actor,limit:26,cursor:first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor",service.List(actor,cursor:first.Value.NextCursor+"x").Code);
        Assert.Equal("ResourceUnavailable",service.Get(Actor(other),ids.First()).Code);
    }

    [Theory]
    [InlineData("%")][InlineData("_")][InlineData("[")][InlineData("~")]
    public async Task Search_is_literal_and_overlong_query_is_rejected(string query)
    {
        fixture.RequireAvailable();var session=await fixture.CreateActiveSessionAsync();var actor=Actor(session);var service=Service();
        Assert.True(service.Create(actor,new("literal "+query,"plaintext","Synthetic source",null)).Succeeded);
        Assert.True(service.Create(actor,new("unrelated","plaintext","Synthetic source",null)).Succeeded);
        var result=service.List(actor,query:query);Assert.True(result.Succeeded,result.Code);Assert.Single(result.Value!.Items);Assert.Equal("literal "+query,result.Value.Items[0].Title);
        Assert.Equal("ValidationFailed",service.List(actor,query:new string('a',201)).Code);
    }

    [Fact]
    public async Task Save_and_transition_replay_have_one_effect_and_archived_source_is_readonly()
    {
        fixture.RequireAvailable();var session=await fixture.CreateActiveSessionAsync();var actor=Actor(session);var service=Service();var body=new SnippetCommand("title|language:plaintext","plaintext","<script>literal synthetic data</script>",null);
        var created=service.Create(actor,body);Assert.True(created.Succeeded,created.Code);
        var saveKey=Guid.NewGuid().ToString("D");var command=body with{Title="Updated |language:plaintext",Body="Unicode Ω\nSecond line"};var saved=service.Save(actor,created.Value!.Id,created.Value.ETag,command,saveKey);Assert.True(saved.Succeeded,saved.Code);
        Assert.Equal(saved,service.Save(actor,created.Value.Id,created.Value.ETag,command,saveKey));Assert.Equal("IdempotencyConflict",service.Save(actor,created.Value.Id,created.Value.ETag,command with{Title="Changed"},saveKey).Code);
        var key=Guid.NewGuid().ToString("D");var archived=service.Transition(actor,saved.Value!.Id,saved.Value.ETag,"Archived",key);Assert.True(archived.Succeeded,archived.Code);Assert.Equal(archived,service.Transition(actor,saved.Value.Id,saved.Value.ETag,"Archived",key));
        Assert.Equal("LifecycleLocked",service.Save(actor,archived.Value!.Id,archived.Value.ETag,body,Guid.NewGuid().ToString("D")).Code);
        Assert.Equal(2,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [knowledge].[SnippetVersion] WHERE OwnerId=@Owner;",Id("@Owner",session.OwnerId)));
        Assert.Equal(command.Body,service.Get(actor,saved.Value.Id).Value!.Body);
    }
}
