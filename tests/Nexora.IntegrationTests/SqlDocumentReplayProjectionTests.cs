using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlDocumentReplayProjectionTests(SqlApiFixture fixture)
{
    private SqlDocumentService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-document-replay-key");
    private static IdentityPrincipal Actor(SyntheticSession s, string role = "User") => new(s.UserId, s.OwnerId, role, DateTimeOffset.UtcNow, s.SessionId);
    private static DocumentCreateCommand Command(Guid? tag = null) => new("Synthetic replay page", "Document", "Markdown", "Private synthetic body", tag);

    [Fact]
    public async Task Originally_opaque_document_reply_stays_opaque_after_read_grant()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin");
        await Grant(session.UserId, "documents.page.create");
        var service = Service(); var key = Guid.NewGuid().ToString(); var command = Command();
        var original = service.Create(actor, command, key);
        Assert.True(original.Succeeded, original.Code); Assert.Null(original.Value); Assert.NotNull(original.Acknowledgement);
        await Grant(session.UserId, "documents.page.read");
        Assert.Equal(original, service.Create(actor, command, key));
        Assert.True(service.Get(actor, original.Acknowledgement.Id).Succeeded);
    }

    [Fact]
    public async Task Tag_rename_downgrades_replay_without_changing_page_revision_or_rewriting_history()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var tags = new SqlDocumentTagService(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-document-replay-key");
        var tag = tags.Create(actor, new("documents", "Original synthetic tag", null)); Assert.True(tag.Succeeded, tag.Code);
        var service = Service(); var key = Guid.NewGuid().ToString(); var command = Command(tag.Value!.Id);
        var original = service.Create(actor, command, key); Assert.True(original.Succeeded, original.Code); Assert.NotNull(original.Value);
        Assert.Equal(original, service.Create(actor, command, key));
        var renamed = tags.Rename(actor, tag.Value.Id, tag.Value.ETag, new("Renamed synthetic tag", null)); Assert.True(renamed.Succeeded, renamed.Code);
        var current = service.Get(actor, original.Value.Id); Assert.Equal(original.Value.ETag, current.Value!.ETag);
        Assert.Equal("Renamed synthetic tag", current.Value.TagName);
        var replay = service.Create(actor, command, key); Assert.True(replay.Succeeded); Assert.Null(replay.Value);
        Assert.Equal(original.Value.Id, replay.Acknowledgement!.Id); Assert.Equal(original.StatusCode, replay.StatusCode);
        var history = service.History(actor, original.Value.Id); Assert.True(history.Succeeded, history.Code);
        Assert.Equal("Original synthetic tag", Assert.Single(history.Value!.Items).TagName);
    }

    [Fact]
    public async Task Legacy_receipt_without_projection_proof_is_ACK_only()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session);
        var service = Service(); var key = Guid.NewGuid().ToString(); var command = Command();
        var original = service.Create(actor, command, key); Assert.True(original.Succeeded, original.Code);
        await fixture.ExecuteAsync("UPDATE [identity].[RequestReceipt] SET [ResultJson]=JSON_MODIFY([ResultJson], '$.ResponseDigest', NULL) WHERE [OperationKey]='documents.page.create' AND JSON_VALUE([ResultJson], '$.Id')=@Id;",
            new SqlParameter("@Id", SqlDbType.NVarChar, 36) { Value = original.Value!.Id.ToString() });
        var replay = service.Create(actor, command, key); Assert.True(replay.Succeeded); Assert.Null(replay.Value);
        Assert.Equal(original.Value.Id, replay.Acknowledgement!.Id);
    }

    private Task Grant(Guid user, string action) => fixture.ExecuteAsync("INSERT INTO [platform].[AdminPermission] ([UserId],[PermissionId],[Effect]) SELECT @User,[Id],'Allow' FROM [platform].[Permission] WHERE [ActionKey]=@Action;",
        new SqlParameter("@User", SqlDbType.UniqueIdentifier) { Value = user }, new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
}
