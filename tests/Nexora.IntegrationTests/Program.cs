using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;

// Only an explicitly supplied local synthetic test connection is accepted. Never use app configuration.
var supplied = Environment.GetEnvironmentVariable("NEXORA_TEST_SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(supplied))
{
    Console.Error.WriteLine("NOT RUN: NEXORA_TEST_SQL_CONNECTION is required (local synthetic database only).");
    return 2;
}
string? database = null;
string? masterConnection = null;
try
{
    var builder = new SqlConnectionStringBuilder(LocalSqlTarget.Validate(supplied, "Development"));
    database = builder.InitialCatalog;
    if (!database.StartsWith("Nexora_Test_", StringComparison.Ordinal) ||
        !Guid.TryParseExact(database["Nexora_Test_".Length..], "N", out _))
        throw new InvalidOperationException("Integration tests require a generated Nexora_Test_<GUID> database name.");
    builder.InitialCatalog = "master";
    masterConnection = builder.ConnectionString;
    await using (var master = new SqlConnection(masterConnection))
    {
        await master.OpenAsync();
        await using var exists = new SqlCommand("SELECT DB_ID(@name)", master);
        exists.Parameters.AddWithValue("@name", database);
        var existingDatabase = await exists.ExecuteScalarAsync();
        if (existingDatabase is not null && existingDatabase is not DBNull)
            throw new InvalidOperationException("Integration target must not already exist.");
        // Identifier has already been restricted to a fixed prefix and a GUID.
        await using var create = new SqlCommand($"CREATE DATABASE [{database}]", master);
        await create.ExecuteNonQueryAsync();
    }
    builder.InitialCatalog = database;
    var connection = builder.ConnectionString;
    await using var sql = new SqlConnection(connection);
    await sql.OpenAsync();
    var migrations = new SqlMigrationRunner();
    var migrationDirectory = Path.Combine(AppContext.BaseDirectory, "migrations");
    var expectedMigrationCount = M01MigrationManifest.RequiredFileNames.Count;
    await migrations.ApplyAsync(connection, migrationDirectory, M01MigrationManifest.RequiredFileNames);
    await migrations.ApplyAsync(connection, migrationDirectory, M01MigrationManifest.RequiredFileNames);
    Require(await Count("SELECT COUNT(*) FROM dbo.NexoraMigration") == expectedMigrationCount, "Migrations journal once on replay");
    Require(await Count("SELECT COUNT(*) FROM dbo.NexoraMigration WHERE Name LIKE '20260910_%'") == 0,
        "M01 migration runner must not apply R1 migrations");
    Console.WriteLine("PASS: empty database migration and journal replay.");

    var bootstrap = new SqlBootstrapSuperAdmin(connection);
    var command = new SqlBootstrapSuperAdminCommand
    {
        Email = "bootstrap@example.invalid", DisplayName = "Synthetic operator", TimeZoneId = "Etc/UTC",
        PasswordHash = "synthetic-noncredential-hash-only"
    };
    await Execute("""
        CREATE TRIGGER [security].[RejectBootstrapForTest] ON [security].[AuditEvent] AFTER INSERT AS
        BEGIN THROW 51002, 'Synthetic audit failure', 1; END;
        """);
    try { await bootstrap.ExecuteAsync(command); throw new InvalidOperationException("Expected rollback failure."); }
    catch (SqlException error) when (error.Number == 51002) { }
    Require(await Count("SELECT COUNT(*) FROM [identity].[User]") == 0, "Audit failure must roll back user");
    Require(await Count("SELECT COUNT(*) FROM [platform].[PersonalSpace]") == 0, "Audit failure must roll back owner");
    Require(await Count("SELECT COUNT(*) FROM [identity].[UserRole]") == 0, "Audit failure must roll back roles");
    Require(await Count("SELECT COUNT(*) FROM [platform].[SecurityInvariant] WHERE BootstrapCompletedAt IS NOT NULL") == 0,
        "Audit failure must leave bootstrap available");
    await Execute("DROP TRIGGER [security].[RejectBootstrapForTest]");
    Console.WriteLine("PASS: audit failure atomically rolls back bootstrap.");

    var results = await Task.WhenAll(bootstrap.ExecuteAsync(command), bootstrap.ExecuteAsync(command));
    Require(results.Count(x => x == BootstrapOutcome.Created) == 1, "Exactly one concurrent bootstrap succeeds");
    Require(results.Count(x => x == BootstrapOutcome.AlreadyBootstrapped) == 1, "Concurrent loser is closed");
    Require(await Count("SELECT COUNT(*) FROM [identity].[User] WHERE State='Active' AND EmailConfirmed=1 AND VerifiedAt IS NOT NULL") == 1,
        "One verified active principal");
    Require(await Count("SELECT COUNT(*) FROM [platform].[PersonalSpace] WHERE Id <> UserId") == 1, "Owner differs from UserId");
    Require(await Count("SELECT COUNT(*) FROM [identity].[UserRole]") == 2, "Base User and SuperAdmin roles");
    Require(await Count("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE ActionKey='identity.bootstrap.completed' AND RedactedDiffJson IS NULL") == 1,
        "One metadata-only audit");
    Console.WriteLine("PASS: concurrent bootstrap creates one principal, owner, role pair and audit.");

    // Synthetic corruption simulates later loss of role assignment; the permanent closure must still win.
    await Execute("DELETE ur FROM [identity].[UserRole] ur JOIN [identity].[Role] r ON r.Id=ur.RoleId WHERE r.Code='SuperAdmin'");
    Require(await bootstrap.ExecuteAsync(command) == BootstrapOutcome.AlreadyBootstrapped, "Marker prevents bootstrap reopening");
    Console.WriteLine("PASS: bootstrap remains closed after role removal.");
    Console.WriteLine("PASS: SQL integration checks completed against a disposable synthetic database.");
    return 0;

    async Task Execute(string statement)
    {
        await using var cmd = new SqlCommand(statement, sql) { CommandTimeout = 30 };
        await cmd.ExecuteNonQueryAsync();
    }
    async Task<int> Count(string statement)
    {
        await using var cmd = new SqlCommand(statement, sql);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: SQL integration checks ({error.GetType().Name}). No credentials logged.");
    return 1;
}
finally
{
    if (database is not null && masterConnection is not null)
    {
        try
        {
            await using var master = new SqlConnection(masterConnection);
            await master.OpenAsync();
            // The catalog is constrained to Nexora_Test_<GUID> before interpolation.
            await using var drop = new SqlCommand(
                $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]; END;",
                master);
            drop.Parameters.AddWithValue("@name", database);
            await drop.ExecuteNonQueryAsync();
        }
        catch (Exception)
        {
            Console.Error.WriteLine("WARN: Test-owned synthetic database cleanup failed.");
        }
    }

}
static void Require(bool result, string invariant)
{
    if (!result) throw new InvalidOperationException(invariant);
}
