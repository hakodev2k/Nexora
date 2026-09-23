using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Infrastructure.Local;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Checks only safe, non-sensitive dependency state required by the local API.
/// It deliberately returns coarse status codes so connection strings, SQL
/// errors and stack traces never cross the health endpoint boundary.
/// </summary>
public sealed class SqlReadinessProbe
{
    private static readonly IReadOnlyList<string> RequiredMigrations = M01MigrationManifest.RequiredFileNames;

    private readonly SqlConnectionFactory _connections;

    public SqlReadinessProbe(SqlConnectionFactory connections)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
    }

    public async Task<SqlReadinessResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var dependencies = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sql"] = "Unknown",
            ["migrationJournal"] = "Unknown",
            ["requiredMigrations"] = "Unknown",
            ["bootstrapSecurityInvariant"] = "Unknown",
            ["redis"] = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NEXORA_REDIS_CONNECTION_STRING"))
                ? "NotConfiguredOptional"
                : "DegradedOptional"
        };

        try
        {
            await using var connection = _connections.Create();
            await connection.OpenAsync(cancellationToken);
            dependencies["sql"] = "Ready";

            if (!await TableExistsAsync(connection, "dbo", "NexoraMigration", cancellationToken))
            {
                dependencies["migrationJournal"] = "Missing";
                dependencies["requiredMigrations"] = "NotChecked";
                dependencies["bootstrapSecurityInvariant"] = "NotChecked";
                return NotReady(dependencies);
            }

            dependencies["migrationJournal"] = "Ready";
            var applied = await ReadAppliedMigrationsAsync(connection, cancellationToken);
            dependencies["requiredMigrations"] = RequiredMigrations.All(name => applied.Contains(name))
                ? "Ready"
                : "MissingRequired";

            var invariant = await ReadSecurityInvariantAsync(connection, cancellationToken);
            dependencies["bootstrapSecurityInvariant"] = invariant switch
            {
                SecurityInvariantState.Valid => "Ready",
                SecurityInvariantState.Missing => "Missing",
                SecurityInvariantState.NotBootstrapped => "NotBootstrapped",
                _ => "Invalid"
            };

            return RequiredMigrations.All(name => applied.Contains(name)) &&
                   invariant == SecurityInvariantState.Valid
                ? new SqlReadinessResult(true, "Ready", dependencies)
                : NotReady(dependencies);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException)
        {
            dependencies["sql"] = "Unavailable";
            dependencies["migrationJournal"] = "NotChecked";
            dependencies["requiredMigrations"] = "NotChecked";
            dependencies["bootstrapSecurityInvariant"] = "NotChecked";
            return NotReady(dependencies);
        }
        catch (InvalidOperationException)
        {
            dependencies["sql"] = "Unavailable";
            dependencies["migrationJournal"] = "NotChecked";
            dependencies["requiredMigrations"] = "NotChecked";
            dependencies["bootstrapSecurityInvariant"] = "NotChecked";
            return NotReady(dependencies);
        }
    }

    private static SqlReadinessResult NotReady(IReadOnlyDictionary<string, string> dependencies) =>
        new(false, "NotReady", dependencies);

    private static async Task<bool> TableExistsAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN OBJECT_ID(@objectName, N'U') IS NULL THEN 0 ELSE 1 END;";
        command.Parameters.Add("@objectName", SqlDbType.NVarChar, 517).Value = $"{schema}.{table}";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task<HashSet<string>> ReadAppliedMigrationsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var names = new string[RequiredMigrations.Count];
        for (var index = 0; index < RequiredMigrations.Count; index++)
        {
            var parameterName = "@migration" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            names[index] = parameterName;
            command.Parameters.Add(parameterName, SqlDbType.NVarChar, 200).Value = RequiredMigrations[index];
        }

        command.CommandText = $"SELECT [Name] FROM [dbo].[NexoraMigration] WHERE [Name] IN ({string.Join(',', names)});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var applied = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(reader.GetString(0));
        }

        return applied;
    }

    private static async Task<SecurityInvariantState> ReadSecurityInvariantAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT [LastActiveSuperAdminGuard], [BootstrapCompletedAt]
            FROM [platform].[SecurityInvariant]
            WHERE [Id] = 1;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return SecurityInvariantState.Missing;
        }

        return reader.GetBoolean(0) && !reader.IsDBNull(1)
            ? SecurityInvariantState.Valid
            : SecurityInvariantState.NotBootstrapped;
    }

    private enum SecurityInvariantState
    {
        Valid,
        Missing,
        NotBootstrapped
    }
}

public sealed record SqlReadinessResult(
    bool Ready,
    string Status,
    IReadOnlyDictionary<string, string> Dependencies);

