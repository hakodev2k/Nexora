using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Domain.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;

if (args is not ["bootstrap-superadmin"] and not ["migrate"])
{
    Console.Error.WriteLine("Usage: Nexora.Local migrate | bootstrap-superadmin (bootstrap requires interactive console)");
    return 2;
}
var connectionSetting = Environment.GetEnvironmentVariable("NEXORA_SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionSetting))
{
    var sqlPassword = Environment.GetEnvironmentVariable("NEXORA_SQL_PASSWORD");
    if (string.IsNullOrEmpty(sqlPassword))
    {
        Console.Error.WriteLine("NEXORA_SQL_CONNECTION or NEXORA_SQL_PASSWORD must be supplied; no default database credential is permitted.");
        return 2;
    }

    var connectionBuilder = new SqlConnectionStringBuilder
    {
        DataSource = Environment.GetEnvironmentVariable("NEXORA_SQL_SERVER") ?? "localhost,14333",
        InitialCatalog = Environment.GetEnvironmentVariable("NEXORA_SQL_DATABASE") ?? "Nexora_Dev",
        UserID = Environment.GetEnvironmentVariable("NEXORA_SQL_USER") ?? "sa",
        Password = sqlPassword,
        TrustServerCertificate = true,
        Encrypt = true
    };
    connectionSetting = connectionBuilder.ConnectionString;
}

try
{
    var connection = LocalSqlTarget.Validate(connectionSetting,
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"));
    if (args[0] == "migrate")
    {
        var configuredMigrationDirectory = Environment.GetEnvironmentVariable("NEXORA_MIGRATIONS_DIR");
        var migrationDirectory = string.IsNullOrWhiteSpace(configuredMigrationDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "migrations")
            : configuredMigrationDirectory!;
        if (!Path.IsPathRooted(migrationDirectory))
            migrationDirectory = Path.GetFullPath(migrationDirectory);

        await new SqlMigrationRunner().ApplyAsync(connection, migrationDirectory);
        Console.WriteLine("Local migrations applied and checksums verified.");
        return 0;
    }
    if (Console.IsInputRedirected || Console.IsOutputRedirected)
        throw new InvalidOperationException("Bootstrap requires an interactive console with hidden password input.");
    Console.Write("Verified operator email: ");
    var email = Console.ReadLine() ?? "";
    Console.Write("Display name: ");
    var name = Console.ReadLine() ?? "";
    Console.Write("IANA timezone: ");
    var zone = Console.ReadLine() ?? "";
    Console.Write("Password: ");
    var password = ReadPassword();
    Console.Write("Confirm password: ");
    if (password != ReadPassword()) throw new InvalidOperationException("Password confirmation does not match.");
    var policy = PasswordPolicy.Validate(password);
    if (!policy.Allowed) throw new InvalidOperationException(policy.Message);
    var outcome = await new SqlBootstrapSuperAdmin(connection).ExecuteAsync(new SqlBootstrapSuperAdminCommand
    {
        Email = email, DisplayName = name, TimeZoneId = zone,
        PasswordHash = new PasswordHashService().Hash(password)
    });
    Console.WriteLine(outcome);
    return outcome == BootstrapOutcome.Created ? 0 : 3;
}
catch (Exception)
{
    // Do not print provider exceptions: SQL messages can contain identifiers and credentials.
    Console.Error.WriteLine("Bootstrap failed. Check local target, migrations, input and database availability. No credentials were logged.");
    return 1;
}

static string ReadPassword()
{
    var buffer = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return buffer.ToString(); }
        if (key.Key == ConsoleKey.Backspace) { if (buffer.Length > 0) buffer.Length--; continue; }
        if (!char.IsControl(key.KeyChar))
        {
            if (buffer.Length >= 512) throw new InvalidOperationException("Password is too long.");
            buffer.Append(key.KeyChar);
        }
    }
}
