using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Application.Bookmarks;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Organization;
using Nexora.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using System.Data;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlTemplateTests(SqlApiFixture fixture)
{
    private SqlTemplateService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-template-receipt-key", TimeProvider.System);
    private static string Key() => Guid.NewGuid().ToString("D");
    private static TemplateWrite Body(string name = "Reading template") => new(name, new("Read next", "https://example.invalid/reading", "Synthetic template seed"));
    private static IdentityPrincipal Actor(SyntheticSession s) => new(s.UserId, s.OwnerId, "User", DateTimeOffset.UtcNow, s.SessionId);

    [Fact]
    public async Task Template_library_is_owner_scoped_and_writes_replay_opaque_without_recreation()
    {
        fixture.RequireAvailable();
        var a = Actor(await fixture.CreateActiveSessionAsync());
        var b = Actor(await fixture.CreateActiveSessionAsync());
        var service = Service(); var key = Key();
        var created = service.Create(a, Body(), key, null);
        Assert.True(created.Succeeded, created.Code); Assert.Null(created.Value);
        var id = created.Acknowledgement!.Id;
        var read = service.Get(a, id); Assert.True(read.Succeeded, read.Code);
        Assert.Equal("FX21", read.Value!.Template.ModuleCode);
        Assert.Equal(404, service.Get(b, id).StatusCode);
        Assert.Empty(service.List(b, null, "Active", 1).Value!.Items);
        Assert.Equal(created, service.Create(a, Body(), key, null));
        Assert.Equal(409, service.Create(a, Body("Changed intent"), key, null).StatusCode);
        Assert.Equal(412, service.Update(a, id, Body("Changed"), "\"AAAAAAAAAAA=\"", Key(), null).StatusCode);
        var change = service.Update(a, id, Body("Changed"), read.Value.Template.ETag, Key(), null);
        Assert.True(change.Succeeded, change.Code);
        var after = service.Get(a, id).Value!;
        Assert.Equal("Changed", after.Template.Name);
        Assert.Equal(428, service.Update(a, id, Body(), "W/\"AAAAAAAAAAA=\"", Key(), null).StatusCode);
        Assert.Equal(403, service.Create(b with { OwnerId = a.OwnerId }, Body(), Key(), null).StatusCode);
        await fixture.SetModuleEnabledAsync(a.UserId, "FX24", false);
        Assert.Equal(403, service.Create(a, Body(), key, null).StatusCode);
    }

    [Fact]
    public async Task Native_template_lifecycle_restores_exact_state_and_purge_replay_never_recreates_a_template()
    {
        fixture.RequireAvailable(); var a = Actor(await fixture.CreateActiveSessionAsync()); var service = Service();
        var createKey = Key(); var created = service.Create(a, Body(), createKey, null); Assert.True(created.Succeeded, created.Code);
        var id = created.Acknowledgement!.Id;
        TemplateSummary Current() => service.Get(a, id).Value!.Template;
        var current = Current();
        Assert.Equal(409, service.Transition(a, id, "restore", current.ETag, Key(), null).StatusCode);
        Assert.Equal(409, service.Transition(a, id, "purge", current.ETag, Key(), null).StatusCode);
        Assert.True(service.Transition(a, id, "archive", current.ETag, Key(), null).Succeeded);
        Assert.Equal("Archived", Current().Status);
        Assert.True(service.Get(a, id).Value!.CanUse); // Archive hides; it is not Trash.
        Assert.True(service.Draft(a, id, Current().ETag).Succeeded);
        Assert.True(service.Update(a, id, Body("Archived seed edit"), Current().ETag, Key(), null).Succeeded);
        Assert.True(service.Transition(a, id, "trash", Current().ETag, Key(), null).Succeeded);
        Assert.Equal("Trash", Current().Status);
        Assert.False(service.Get(a, id).Value!.CanUse);
        Assert.Equal(409, service.Draft(a, id, Current().ETag).StatusCode);
        Assert.True(service.Transition(a, id, "restore", Current().ETag, Key(), null).Succeeded);
        Assert.Equal("Archived", Current().Status);
        Assert.True(service.Transition(a, id, "unarchive", Current().ETag, Key(), null).Succeeded);
        Assert.Equal("Active", Current().Status);
        Assert.True(service.Transition(a, id, "trash", Current().ETag, Key(), null).Succeeded);
        var purgeKey = Key(); var etag = Current().ETag;
        var purge = service.Transition(a, id, "purge", etag, purgeKey, null); Assert.True(purge.Succeeded, purge.Code);
        Assert.Equal(404, service.Get(a, id).StatusCode);
        Assert.Equal(purge, service.Transition(a, id, "purge", etag, purgeKey, null));
        Assert.Equal(created, service.Create(a, Body(), createKey, null));
        Assert.Empty(service.List(a, null, "Active", 1).Value!.Items);
    }

    [Fact]
    public async Task Draft_requires_active_current_revision_and_current_target_create_authority()
    {
        fixture.RequireAvailable(); var a = Actor(await fixture.CreateActiveSessionAsync()); var service = Service();
        var result = service.Create(a, Body(), Key(), null); Assert.True(result.Succeeded, result.Code);
        var id = result.Acknowledgement!.Id; var current = service.Get(a, id).Value!;
        Assert.True(current.CanUse);
        var draft = service.Draft(a, id, current.Template.ETag); Assert.True(draft.Succeeded, draft.Code);
        Assert.Equal(current.Seed, draft.Value!.Seed);
        // This is the normal target command, not an organization-owned insert.
        // FX24-AC-001: a later template edit cannot mutate the created resource.
        var bookmarks = new SqlBookmarkService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-bookmark-receipt-key");
        var target = bookmarks.Create(a, new BookmarkCommand(draft.Value.Seed.Url!, draft.Value.Seed.Title!, draft.Value.Seed.Description), Key());
        Assert.True(target.Succeeded, target.Code);
        Assert.True(service.Update(a, id, Body("New template name") with { Seed = new("New suggested title", "https://example.invalid/changed", null) }, current.Template.ETag, Key(), null).Succeeded);
        Assert.Equal("Read next", bookmarks.Get(a, target.Value!.Id).Value!.Title);
        Assert.Equal("https://example.invalid/reading", bookmarks.Get(a, target.Value.Id).Value!.Url);
        current = service.Get(a, id).Value!;
        Assert.Equal(412, service.Draft(a, id, "\"AAAAAAAAAAA=\"").StatusCode);
        await fixture.SetModuleEnabledAsync(a.UserId, "FX21", false);
        Assert.True(service.Get(a, id).Succeeded); Assert.False(service.Get(a, id).Value!.CanUse);
        Assert.Equal(409, service.Draft(a, id, current.Template.ETag).StatusCode);
    }

    [Fact]
    public async Task Trash_constraint_rejects_missing_prior_state_instead_of_accepting_SQL_unknown()
    {
        fixture.RequireAvailable(); var a = Actor(await fixture.CreateActiveSessionAsync()); var service = Service();
        var created = service.Create(a, Body(), Key(), null); Assert.True(created.Succeeded, created.Code);
        var id = created.Acknowledgement!.Id;
        using var connection = new SqlConnectionFactory(fixture.ConnectionString!).Create(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE organization.Template SET Status='Trash',PreTrashStatus=NULL,DeletionBatchId=NEWID() WHERE OwnerId=@Owner AND Id=@Id;";
        command.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = a.OwnerId;
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        var failure = Assert.Throws<SqlException>(() => command.ExecuteNonQuery());
        Assert.Equal(547, failure.Number);
        Assert.Equal("Active", service.Get(a, id).Value!.Template.Status);
    }
}
