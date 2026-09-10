using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

var connectionString = Environment.GetEnvironmentVariable("NEXORA_SQL_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var sqlPassword = Environment.GetEnvironmentVariable("NEXORA_SQL_PASSWORD");
    if (string.IsNullOrEmpty(sqlPassword))
    {
        Console.Error.WriteLine("NEXORA_SQL_PASSWORD or NEXORA_SQL_CONNECTION_STRING must be supplied; no default database credential is permitted.");
        return 2;
    }

    var connectionBuilder = new SqlConnectionStringBuilder
    {
        DataSource = Environment.GetEnvironmentVariable("NEXORA_SQL_SERVER") ?? "localhost,14333",
        InitialCatalog = Environment.GetEnvironmentVariable("NEXORA_SQL_DATABASE") ?? "NexoraLocal",
        UserID = Environment.GetEnvironmentVariable("NEXORA_SQL_USER") ?? "sa",
        Password = sqlPassword,
        TrustServerCertificate = true,
        Encrypt = true
    };
    connectionString = connectionBuilder.ConnectionString;
}

var email = Environment.GetEnvironmentVariable("NEXORA_BOOTSTRAP_EMAIL");
var password = Environment.GetEnvironmentVariable("NEXORA_BOOTSTRAP_PASSWORD");
var timeZoneId = Environment.GetEnvironmentVariable("NEXORA_BOOTSTRAP_TIMEZONE") ?? "UTC";
var displayName = Environment.GetEnvironmentVariable("NEXORA_BOOTSTRAP_DISPLAY_NAME");

if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
{
    Console.Error.WriteLine("NEXORA_BOOTSTRAP_EMAIL and NEXORA_BOOTSTRAP_PASSWORD must be supplied by the local bootstrap wrapper.");
    return 2;
}

var service = new SqlIdentityService(new SqlConnectionFactory(connectionString));
var result = service.BootstrapSuperAdmin(new BootstrapSuperAdminCommand(email, password, timeZoneId, displayName));
if (!result.Succeeded || result.Value is null)
{
    Console.Error.WriteLine($"Bootstrap failed: {result.Code} ({result.Title})");
    return result.StatusCode >= 500 ? 3 : 1;
}

Console.WriteLine($"SuperAdmin bootstrap completed for {email.Trim()} (user {result.Value.UserId}, personal space {result.Value.PersonalSpaceId}).");
return 0;
