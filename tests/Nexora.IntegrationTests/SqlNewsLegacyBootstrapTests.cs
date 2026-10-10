using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlNewsLegacyBootstrapTests(SqlApiFixture fixture)
{
    [Fact]
    public async Task Actual_legacy_bootstrap_rolls_back_initialization_failure_then_creates_exact_defaults_in_its_own_database()
    {
        fixture.RequireAvailable();
        var target = new SqlConnectionStringBuilder(LocalSqlTarget.Validate(fixture.ConnectionString!, "Development"));
        var database = "Nexora_Test_" + Guid.NewGuid().ToString("N");
        Assert.Matches("^Nexora_Test_[a-f0-9]{32}$", database);
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        var created = false;
        await using var server = new SqlConnection(master.ConnectionString); await server.OpenAsync();
        using (var absent = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@Name", server))
        {
            absent.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = database;
            Assert.Equal(0, Convert.ToInt32(await absent.ExecuteScalarAsync()));
        }
        try
        {
            using (var create = new SqlCommand("CREATE DATABASE [" + database + "]", server)) await create.ExecuteNonQueryAsync();
            created = true; target.InitialCatalog = database;
            await new SqlMigrationRunner().ApplyAsync(target.ConnectionString, Path.Combine(AppContext.BaseDirectory, "migrations"), M01MigrationManifest.RequiredFileNames);
            async Task<int> Count(string sql)
            {
                await using var c = new SqlConnection(target.ConnectionString); await c.OpenAsync();
                using var command = new SqlCommand(sql, c); return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            async Task Execute(string sql)
            {
                await using var c = new SqlConnection(target.ConnectionString); await c.OpenAsync();
                using var command = new SqlCommand(sql, c); await command.ExecuteNonQueryAsync();
            }
            var identity = new SqlIdentityService(new SqlConnectionFactory(target.ConnectionString));
            var command = new BootstrapSuperAdminCommand("legacy-news-" + Guid.NewGuid().ToString("N") + "@example.invalid", "News-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)) + "-aA!", "Etc/UTC", "Synthetic legacy bootstrap", "en");
            await Execute("CREATE TRIGGER [news].[RejectLegacyNewsMarker] ON [news].[OwnerInitialization] AFTER INSERT AS BEGIN THROW 51046,'Synthetic marker failure',1; END;");
            try
            {
                var failed = identity.BootstrapSuperAdmin(command);
                Assert.False(failed.Succeeded); Assert.Equal(503, failed.StatusCode); Assert.Null(failed.Value);
                foreach (var table in new[] { "[identity].[User]", "[platform].[PersonalSpace]", "[identity].[UserRole]", "[platform].[UserModuleGrant]", "[news].[Category]", "[news].[OwnerInitialization]", "[platform].[Resource]", "[security].[AuditEvent]" })
                    Assert.Equal(0, await Count("SELECT COUNT(*) FROM " + table));
                Assert.Equal(0, await Count("SELECT COUNT(*) FROM [platform].[SecurityInvariant] WHERE BootstrapCompletedAt IS NOT NULL"));
            }
            finally { await Execute("DROP TRIGGER [news].[RejectLegacyNewsMarker]"); }
            var winner = identity.BootstrapSuperAdmin(command); Assert.True(winner.Succeeded); Assert.Equal(201, winner.StatusCode);
            Assert.Equal(2, await Count("SELECT COUNT(*) FROM [news].[Category] c JOIN [platform].[PersonalSpace] p ON p.Id=c.OwnerId WHERE c.Name IN(N'AI News',N'Tech News')"));
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [news].[OwnerInitialization] i JOIN [platform].[PersonalSpace] p ON p.Id=i.OwnerId WHERE i.DefaultCategoryVersion=1"));
            var closed = identity.BootstrapSuperAdmin(command); Assert.False(closed.Succeeded); Assert.Equal(409, closed.StatusCode);
            Assert.Equal(2, await Count("SELECT COUNT(*) FROM [news].[Category]"));
        }
        finally
        {
            if (created)
            {
                // Only the absence-checked database created by this test is eligible for cleanup.
                Assert.Matches("^Nexora_Test_[a-f0-9]{32}$", database);
                using var ownPool = new SqlConnection(target.ConnectionString);
                SqlConnection.ClearPool(ownPool);
                using var drop = new SqlCommand("ALTER DATABASE [" + database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + database + "]", server);
                await drop.ExecuteNonQueryAsync();
            }
        }
    }
}
