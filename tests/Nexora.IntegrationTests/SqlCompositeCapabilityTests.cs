using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlCompositeCapabilityTests(SqlApiFixture fixture)
{
    [Theory]
    [InlineData("User")]
    [InlineData("SuperAdmin")]
    public async Task Owner_baseline_requires_every_requested_action_to_be_resolved(string role)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync(role);
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, role, DateTimeOffset.UtcNow);
        var capabilities = new SqlSelfCapability(new SqlConnectionFactory(fixture.ConnectionString!));

        Assert.Equal(SqlCapabilityStatus.Allowed,
            capabilities.Evaluate(actor, "FX09", "settings.preference.read", "settings.preference.update"));
        Assert.Equal(SqlCapabilityStatus.ModuleUnavailable,
            capabilities.Evaluate(actor, "FX09", "settings.preference.read", "settings.not_registered"));
        Assert.Equal(SqlCapabilityStatus.ModuleUnavailable,
            capabilities.Evaluate(actor, "FX09", "settings.not_registered", "settings.preference.read"));
        Assert.Equal(SqlCapabilityStatus.Allowed,
            capabilities.Evaluate(actor, "FX09", "settings.preference.read", "settings.preference.read"));
    }

    [Fact]
    public async Task Admin_composition_requires_each_allow_and_rechecks_removed_and_denied_grants()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync("Admin");
        var actor = new IdentityPrincipal(session.UserId, session.OwnerId, "Admin", DateTimeOffset.UtcNow);
        var capabilities = new SqlSelfCapability(new SqlConnectionFactory(fixture.ConnectionString!));
        var actions = new[] { "settings.preference.read", "settings.preference.update" };
        Assert.Equal(SqlCapabilityStatus.PermissionDenied, capabilities.Evaluate(actor, "FX09", actions));

        await Grant("settings.preference.read", "Allow");
        Assert.Equal(SqlCapabilityStatus.Allowed,
            capabilities.Evaluate(actor, "FX09", "settings.preference.read"));
        Assert.Equal(SqlCapabilityStatus.PermissionDenied, capabilities.Evaluate(actor, "FX09", actions));

        await Grant("settings.preference.update", "Allow");
        Assert.Equal(SqlCapabilityStatus.Allowed, capabilities.Evaluate(actor, "FX09", actions));
        await fixture.ExecuteAsync("""
            DELETE ap FROM [platform].[AdminPermission] ap
            INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId]
            WHERE ap.[UserId] = @User AND p.[ActionKey] = 'settings.preference.update';
            """, new SqlParameter("@User", SqlDbType.UniqueIdentifier) { Value = session.UserId });
        Assert.Equal(SqlCapabilityStatus.PermissionDenied, capabilities.Evaluate(actor, "FX09", actions));
        await Grant("settings.preference.update", "Deny");
        Assert.Equal(SqlCapabilityStatus.PermissionDenied, capabilities.Evaluate(actor, "FX09", actions));

        async Task Grant(string action, string effect) => await fixture.ExecuteAsync("""
            INSERT INTO [platform].[AdminPermission] ([UserId], [PermissionId], [Effect])
            SELECT @User, [Id], @Effect FROM [platform].[Permission] WHERE [ActionKey] = @Action;
            """,
            new SqlParameter("@User", SqlDbType.UniqueIdentifier) { Value = session.UserId },
            new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action },
            new SqlParameter("@Effect", SqlDbType.VarChar, 16) { Value = effect });
    }
}
