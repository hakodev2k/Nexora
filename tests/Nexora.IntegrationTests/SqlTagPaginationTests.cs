using System.Net;
using System.Text.Json;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Organization;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlTagPaginationTests(SqlApiFixture fixture)
{
    [Fact]
    public async Task Completed_Tag_mutations_replay_without_a_second_write_and_reject_changed_payload()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow);
        var service = new SqlTagService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-tag-key");
        var createKey = Guid.NewGuid().ToString();
        var command = new TagCreateCommand("projects", "name|color:#FFFFFF", null);
        var created = service.Create(actor, command, createKey);
        Assert.True(created.Succeeded, created.Code);
        Assert.Equal(created, service.Create(actor, command, createKey));
        Assert.Equal("IdempotencyConflict", service.Create(actor, command with { Name = command.Name.ToUpperInvariant() }, createKey).Code);
        Assert.Single(service.List(actor, "projects").Value!.Items);

        var renameKey = Guid.NewGuid().ToString();
        var rename = new TagRenameCommand("new display", null);
        var changed = service.Rename(actor, created.Value!.Id, created.Value.ETag, rename, renameKey);
        Assert.True(changed.Succeeded, changed.Code);
        Assert.Equal(changed, service.Rename(actor, created.Value.Id, created.Value.ETag, rename, renameKey));
        Assert.Equal("IdempotencyConflict", service.Rename(actor, created.Value.Id, created.Value.ETag,
            rename with { Name = "NEW DISPLAY" }, renameKey).Code);

        var removeKey = Guid.NewGuid().ToString();
        var removed = service.Remove(actor, changed.Value!.Id, changed.Value.ETag, removeKey);
        Assert.True(removed.Succeeded, removed.Code); Assert.Equal(204, removed.StatusCode);
        Assert.Equal(removed, service.Remove(actor, changed.Value.Id, changed.Value.ETag, removeKey));
        Assert.Empty(service.List(actor, "projects").Value!.Items);
        Assert.Equal("ResourceUnavailable", service.Create(actor, command, createKey).Code);
        await fixture.SetModuleEnabledAsync(actor.UserId, "FX24", false);
        Assert.False(service.Remove(actor, changed.Value.Id, changed.Value.ETag, removeKey).Succeeded);
    }

    [Fact]
    public async Task Tag_catalog_can_reach_every_page_and_refresh_an_exact_ID_without_cross_owner_reads()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var other = await fixture.CreateActiveSessionAsync();
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow);
        var service = new SqlTagService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-tag-key");
        var created = new List<Guid>();
        for (var index = 0; index < 27; index++)
        {
            var result = service.Create(actor, new TagCreateCommand("projects", $"Page {index:D3}", null));
            Assert.True(result.Succeeded, result.Code); created.Add(result.Value!.Id);
        }
        var first = service.List(actor, "projects"); Assert.True(first.Succeeded);
        Assert.Equal(25, first.Value!.Items.Count); Assert.NotNull(first.Value.NextCursor);
        var next = service.List(actor, "projects", cursor: first.Value.NextCursor);
        Assert.True(next.Succeeded); Assert.Equal(2, next.Value!.Items.Count); Assert.Null(next.Value.NextCursor);
        Assert.Equal(created.Order(), first.Value.Items.Concat(next.Value.Items).Select(t => t.Id).Order());

        using var detail = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            $"/api/v1/organization/tags/{created[^1]}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode); Assert.NotNull(detail.Headers.ETag);
        using var foreignDetail = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            $"/api/v1/organization/tags/{created[^1]}", other.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreignDetail.StatusCode);
        using var apiFirst = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            "/api/v1/organization/tags?namespace=projects", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, apiFirst.StatusCode);
        using var firstJson = JsonDocument.Parse(await apiFirst.Content.ReadAsStringAsync());
        Assert.Equal(25, firstJson.RootElement.GetProperty("items").GetArrayLength());
        var encoded = Uri.EscapeDataString(firstJson.RootElement.GetProperty("nextCursor").GetString()!);
        using var apiNext = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            $"/api/v1/organization/tags?namespace=projects&cursor={encoded}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, apiNext.StatusCode);
        using var nextJson = JsonDocument.Parse(await apiNext.Content.ReadAsStringAsync());
        Assert.Equal(2, nextJson.RootElement.GetProperty("items").GetArrayLength());
        using var foreignPage = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            $"/api/v1/organization/tags?namespace=projects&cursor={encoded}", other.RawSessionHandle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreignPage.StatusCode);
        using var changedFilters = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            $"/api/v1/organization/tags?namespace=documents&cursor={encoded}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, changedFilters.StatusCode);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    [InlineData("\\")]
    public async Task Tag_name_search_treats_SQL_wildcards_as_literal_text(string literal)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow);
        var service = new SqlTagService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-tag-key");
        Assert.True(service.Create(actor, new TagCreateCommand("projects", "ordinary", null)).Succeeded);
        var special = service.Create(actor, new TagCreateCommand("projects", "literal" + literal, null));
        Assert.True(special.Succeeded);
        var matches = service.List(actor, "projects", literal);
        Assert.True(matches.Succeeded); Assert.Equal(special.Value!.Id, Assert.Single(matches.Value!.Items).Id);
        using var tooLong = await fixture.SendAuthenticatedAsync(HttpMethod.Get,
            "/api/v1/organization/tags?query=" + new string('a', 51), session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLong.StatusCode);
    }
}
