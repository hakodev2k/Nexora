using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Local;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlNewsMigrationTests(SqlApiFixture fixture)
{
    [Fact]
    public async Task Normal_upgrade_backfills_inactive_existing_owner_preserves_flags_and_grants_and_replay_does_not_revive_names()
    {
        fixture.RequireAvailable(); var database = "Nexora_Test_" + Guid.NewGuid().ToString("N");
        Assert.Matches("^Nexora_Test_[a-f0-9]{32}$", database);
        var target = new SqlConnectionStringBuilder(LocalSqlTarget.Validate(fixture.ConnectionString!, "Development"));
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        await using var server = new SqlConnection(master.ConnectionString); await server.OpenAsync(); var created = false;
        using (var absence = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@Name", server))
        { absence.Parameters.Add("@Name", SqlDbType.NVarChar, 128).Value = database; Assert.Equal(0, Convert.ToInt32(await absence.ExecuteScalarAsync())); }
        try
        {
            using (var create = new SqlCommand("CREATE DATABASE [" + database + "]", server)) await create.ExecuteNonQueryAsync(); created = true; target.InitialCatalog = database;
            var runner = new SqlMigrationRunner(); var directory = Path.Combine(AppContext.BaseDirectory, "migrations");
            Assert.Equal("20261005_0046_news_categories.sql", M01MigrationManifest.RequiredFileNames.Last());
            await runner.ApplyAsync(target.ConnectionString, directory, M01MigrationManifest.RequiredFileNames.Take(M01MigrationManifest.RequiredFileNames.Count - 1).ToArray());
            var user = Guid.NewGuid(); var owner = Guid.NewGuid();
            await using var c = new SqlConnection(target.ConnectionString); await c.OpenAsync();
            using (var arrange = new SqlCommand("""
                INSERT [identity].[User](Id,Email,NormalizedEmail,PasswordHash,SecurityStamp,State,EmailConfirmed,VerifiedAt,IsDeleted,DisplayName,TimeZoneId,Locale)
                    VALUES(@User,@Email,@Email,'Synthetic inactive-owner hash',NEWID(),'Active',1,SYSUTCDATETIME(),0,'Synthetic inactive owner','Etc/UTC','en');
                INSERT [platform].[PersonalSpace](Id,UserId,State) VALUES(@Owner,@User,'Suspended');
                INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='User';
                INSERT [platform].[UserModuleGrant](UserId,ModuleId,Enabled) SELECT @User,Id,0 FROM [platform].[Module] WHERE Code='FX29';
                """, c))
            {
                arrange.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = user; arrange.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = owner;
                arrange.Parameters.Add("@Email", SqlDbType.NVarChar, 320).Value = "news-migration-" + user.ToString("N") + "@example.invalid";
                await arrange.ExecuteNonQueryAsync();
            }
            async Task<int> Count(string sql)
            { using var q = new SqlCommand(sql, c); return Convert.ToInt32(await q.ExecuteScalarAsync()); }
            async Task<string> Flags()
            {
                using var q = new SqlCommand("SELECT Code,SystemEnabled,RegistrationEnabled,SharingEnabled,SharingEpoch FROM [platform].[Module] ORDER BY Code FOR JSON PATH", c);
                using var reader = await q.ExecuteReaderAsync(); var text = new System.Text.StringBuilder();
                while (await reader.ReadAsync()) text.Append(reader.GetString(0)); return text.ToString();
            }
            var flags = await Flags();
            var policyRevision = await Count("SELECT CONVERT(int,PolicyRevision) FROM [platform].[Module] WHERE Code='FX29'");
            await runner.ApplyAsync(target.ConnectionString, directory, M01MigrationManifest.RequiredFileNames);
            Assert.Equal(flags, await Flags());
            Assert.Equal(policyRevision + 1, await Count("SELECT CONVERT(int,PolicyRevision) FROM [platform].[Module] WHERE Code='FX29' AND State='Ready'"));
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [identity].[User]"));
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [platform].[PersonalSpace] WHERE State='Suspended'"));
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [platform].[UserModuleGrant] WHERE Enabled=0"));
            Assert.Equal(2, await Count("SELECT COUNT(*) FROM [news].[Category] WHERE Name IN(N'AI News',N'Tech News')"));
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [news].[OwnerInitialization] WHERE DefaultCategoryVersion=1"));
            Assert.Equal(2, await Count("SELECT COUNT(*) FROM [platform].[Resource] r JOIN [news].[Category] n ON n.Id=r.Id AND n.OwnerId=r.OwnerId"));
            using (var rename = new SqlCommand("UPDATE [news].[Category] SET Name=N'Renamed migrated category' WHERE Name=N'AI News'", c)) await rename.ExecuteNonQueryAsync();
            await runner.ApplyAsync(target.ConnectionString, directory, M01MigrationManifest.RequiredFileNames);
            Assert.Equal(1, await Count("SELECT COUNT(*) FROM [news].[Category] WHERE Name=N'Renamed migrated category'"));
            Assert.Equal(0, await Count("SELECT COUNT(*) FROM [news].[Category] WHERE Name=N'AI News'"));
            Assert.Equal(2, await Count("SELECT COUNT(*) FROM [news].[Category]")); Assert.Equal(flags, await Flags());
            Assert.Equal(policyRevision + 1, await Count("SELECT CONVERT(int,PolicyRevision) FROM [platform].[Module] WHERE Code='FX29'"));
        }
        finally
        {
            if (created)
            {
                Assert.Matches("^Nexora_Test_[a-f0-9]{32}$", database);
                using var ownPool = new SqlConnection(target.ConnectionString); SqlConnection.ClearPool(ownPool);
                using var drop = new SqlCommand("ALTER DATABASE [" + database + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + database + "]", server); await drop.ExecuteNonQueryAsync();
            }
        }
    }
}
