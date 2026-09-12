using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Local;

public static class LocalSqlTarget
{
    public static string Validate(string? connectionString, string? environment)
    {
        if (environment != "Development" || string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Development and an explicit local SQL connection are required.");
        var builder = new SqlConnectionStringBuilder(connectionString);
        var server = builder.DataSource;
        if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)) server = server[4..];
        var host = server.Split(',', '\\')[0];
        if (host is not ("localhost" or "127.0.0.1" or "." or "(local)" or "(localdb)"))
            throw new InvalidOperationException("Only loopback SQL targets are allowed for local commands.");
        if (builder.InitialCatalog != "Nexora_Dev" &&
            !(builder.InitialCatalog.StartsWith("Nexora_Test_", StringComparison.Ordinal) &&
              Guid.TryParseExact(builder.InitialCatalog[12..], "N", out _)))
            throw new InvalidOperationException("Use Nexora_Dev or Nexora_Test_<32 hex GUID>.");
        if (builder.AttachDBFilename.Length != 0 || builder.FailoverPartner.Length != 0)
            throw new InvalidOperationException("Attached files and failover servers are not supported for local commands.");
        builder.ConnectTimeout = 15;
        builder.PersistSecurityInfo = false;
        return builder.ConnectionString;
    }
}
