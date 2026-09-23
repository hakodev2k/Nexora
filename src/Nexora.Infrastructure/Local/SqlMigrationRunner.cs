using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Local;

public sealed class SqlMigrationRunner
{
    public async Task ApplyAsync(
        string connectionString,
        string directory,
        IReadOnlyCollection<string> migrationFileNames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migrationFileNames);
        var target = LocalSqlTarget.Validate(connectionString, "Development");
        await using var connection = new SqlConnection(target);
        await connection.OpenAsync(cancellationToken);
        await Run("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'Nexora.Migrations',
                @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 15000;
            IF @result < 0 THROW 51003, 'Migration lock unavailable.', 1;
            IF OBJECT_ID(N'dbo.NexoraMigration', N'U') IS NULL
                CREATE TABLE dbo.NexoraMigration (
                    Name nvarchar(200) NOT NULL PRIMARY KEY,
                    ContentHash binary(32) NOT NULL,
                    AppliedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME());
            """);
        try
        {
            foreach (var file in ResolveMigrationFiles(directory, migrationFileNames))
            {
                var name = Path.GetFileName(file);
                var content = await File.ReadAllTextAsync(file, cancellationToken);
                // Normalize checkout line endings so Windows/Linux journals agree.
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n")));
                await using var read = new SqlCommand("SELECT ContentHash FROM dbo.NexoraMigration WHERE Name=@name", connection);
                read.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
                if (await read.ExecuteScalarAsync(cancellationToken) is byte[] applied)
                {
                    if (!CryptographicOperations.FixedTimeEquals(applied, hash))
                        throw new InvalidOperationException("An applied migration has changed; use a new migration.");
                    continue;
                }
                // Repository migrations own their transaction and are idempotent after a crash before journaling.
                foreach (var batch in Regex.Split(content, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    if (!string.IsNullOrWhiteSpace(batch)) await Run(batch);
                await using var journal = new SqlCommand("INSERT dbo.NexoraMigration(Name,ContentHash) VALUES(@name,@hash)", connection);
                journal.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
                journal.Parameters.Add("@hash", SqlDbType.Binary, 32).Value = hash;
                await journal.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        finally
        {
            // Release on the same physical pooled connection, also after a failed batch.
            await Run("IF @@TRANCOUNT > 0 ROLLBACK; EXEC sys.sp_releaseapplock @Resource=N'Nexora.Migrations', @LockOwner='Session';");
        }

        async Task Run(string sql)
        {
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 60 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static IReadOnlyList<string> ResolveMigrationFiles(
        string directory,
        IReadOnlyCollection<string> migrationFileNames)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("A migration directory is required.", nameof(directory));
        if (migrationFileNames.Count == 0)
            throw new ArgumentException("At least one reviewed migration is required.", nameof(migrationFileNames));

        var fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
            throw new DirectoryNotFoundException("The reviewed migration directory does not exist.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<string>(migrationFileNames.Count);
        foreach (var name in migrationFileNames)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                !name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal) ||
                !names.Add(name))
            {
                throw new ArgumentException("Migration selections must be unique SQL file names.", nameof(migrationFileNames));
            }

            var file = Path.Combine(fullDirectory, name);
            if (!File.Exists(file))
                throw new FileNotFoundException("A reviewed migration file is missing.", file);

            files.Add(file);
        }

        return files.Order(StringComparer.Ordinal).ToArray();
    }
}
