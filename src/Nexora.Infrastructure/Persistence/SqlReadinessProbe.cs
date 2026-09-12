using System.Data;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Checks only safe, non-sensitive dependency state required by the local API.
/// It deliberately returns coarse status codes so connection strings, SQL
/// errors and stack traces never cross the health endpoint boundary.
/// </summary>
public sealed class SqlReadinessProbe
{
    private static readonly string[] RequiredMigrations =
    [
        "20260909_0001_m01_identity_platform.sql",
        "20260909_0002_bootstrap_closure.sql",
        "20260910_0002_r1_catalog_and_productivity.sql",
        "20260910_0003_productivity_lifecycle.sql",
        "20260910_0004_notifications_inbox.sql",
        "20260910_0005_preferences.sql",
        "20260910_0006_documents_pages.sql",
        "20260910_0007_action_catalog_alignment.sql",
        "20260910_0008_finance_manual_records.sql",
        "20260910_0009_bookmarks_manual.sql",
        "20260910_0010_snippets_manual.sql",
        "20260910_0011_reading_queue_bookmarks.sql",
        "20260910_0012_organization_tags.sql",
        "20260910_0013_developer_toolbox_pure.sql",
        "20260910_0014_goals_numeric.sql",
        "20260910_0015_dashboard_attention.sql",
        "20260910_0016_global_search_source_query.sql",
        "20260910_0017_favorites_refs.sql",
        "20260910_0018_local_runtime_catalog_gate.sql",
        "20260910_0019_productivity_contract_alignment.sql",
        "20260910_0020_task_calendar_projection.sql",
        "20260911_0021_core_sharing_support_files.sql",
        "20260911_0022_reminders_scheduling.sql",
        "20260911_0023_planner_habits.sql"
    ];

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
        var names = new string[RequiredMigrations.Length];
        for (var index = 0; index < RequiredMigrations.Length; index++)
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

