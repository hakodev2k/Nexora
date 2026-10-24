using Nexora.Application.DeveloperTools;
using Nexora.Application.Identity;
using Nexora.Infrastructure.DeveloperTools;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlToolboxExtraTests(SqlApiFixture fixture)
{
    [Fact]
    public async Task Offline_tools_use_current_SQL_authority_and_never_accept_JWT_payloads()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "User", DateTimeOffset.UtcNow, session.SessionId);
        var service = new LocalToolboxService(new SqlConnectionFactory(fixture.ConnectionString!));
        var before = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]");
        var commands = new[] {
            new ToolboxRunCommand("xml", "<r><a>Việt 😀</a></r>"),
            new ToolboxRunCommand("datetime", "0", new Dictionary<string,string>{{"unit","seconds"}}),
            new ToolboxRunCommand("csv", "title,note\r\nHọc,=1+1", new Dictionary<string,string>{{"header","true"}}),
            new ToolboxRunCommand("jwt", "")
        };
        foreach (var command in commands) Assert.True(service.Run(actor, command).Succeeded, command.ToolCode);
        var rejected = service.Run(actor, new ToolboxRunCommand("jwt", "synthetic.token.signature"));
        Assert.False(rejected.Succeeded); Assert.Equal(422, rejected.StatusCode); Assert.Null(rejected.Value);
        var xml = service.Run(actor, new ToolboxRunCommand("xml", "<!DOCTYPE r [<!ENTITY x SYSTEM 'https://example.invalid/never-fetch'>]><r>&x;</r>"));
        Assert.False(xml.Succeeded); Assert.Equal("XmlInvalid", xml.Code);
        Assert.Equal(before, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt]"));
        await fixture.SetModuleEnabledAsync(session.UserId, "FX32", false);
        foreach (var command in commands)
        {
            var denied = service.Run(actor, command);
            Assert.False(denied.Succeeded); Assert.Equal("ModuleUnavailable", denied.Code); Assert.Null(denied.Value);
        }
        await fixture.SetModuleEnabledAsync(session.UserId, "FX32", true);
        await fixture.ExecuteAsync("UPDATE [identity].[Session] SET RevokedAt=SYSUTCDATETIME() WHERE Id=@Id",
            new Microsoft.Data.SqlClient.SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = session.SessionId });
        foreach (var command in commands) Assert.False(service.Run(actor, command).Succeeded);
    }
}
