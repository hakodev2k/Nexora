using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Persistence;

public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A Nexora SQL connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public SqlConnection Create() => new(_connectionString);
}
