extern alias api;

using System.Data;
using System.Net;
using System.Security.Cryptography;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Persistence;
using Xunit;
using Xunit.Sdk;
using ApiProgram = api::Program;

namespace Nexora.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlApiCollection : ICollectionFixture<SqlApiFixture>
{
    public const string Name = "sql-api";
}

/// <summary>
/// Owns one loopback-only, generated SQL Server database and a TestServer host.
/// It deliberately fails closed when no synthetic SQL connection was supplied:
/// a required integration gate may not become green by silently skipping.
/// </summary>
public sealed class SqlApiFixture : IAsyncLifetime
{
    private const string SqlConnectionEnvironment = "NEXORA_TEST_SQL_CONNECTION";
    private readonly string _runRoot = Path.Combine(
        Path.GetTempPath(),
        "nexora-sql-api-" + Guid.NewGuid().ToString("N"));
    private readonly string _idempotencySecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _csrfSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly string _localMessageKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<string, string?> _previousEnvironment = new(StringComparer.Ordinal);
    private readonly string _captureDirectory;
    private string? _masterConnectionString;
    private NexoraApiFactory? _factory;
    private HttpClient? _client;
    private bool _databaseCreated;

    public SqlApiFixture()
    {
        _captureDirectory = Path.Combine(_runRoot, "captures");
    }

    public string? BlockedReason { get; private set; }

    public string? DatabaseName { get; private set; }

    public string? ConnectionString { get; private set; }

    public SqlReadinessResult? ReadinessAt0024 { get; private set; }

    public SqlReadinessResult? ReadinessAfterFullMigration { get; private set; }

    public SqlReadinessResult? ReadinessAfterBootstrap { get; private set; }

    public HttpClient Client => _client ?? throw new InvalidOperationException("The SQL/API test host is unavailable.");

    public bool Available =>
        BlockedReason is null &&
        ConnectionString is not null &&
        DatabaseName is not null &&
        _client is not null;

    public async Task InitializeAsync()
    {
        var supplied = Environment.GetEnvironmentVariable(SqlConnectionEnvironment);
        if (string.IsNullOrWhiteSpace(supplied))
        {
            BlockedReason = "NEXORA_TEST_SQL_CONNECTION was not supplied.";
            return;
        }

        try
        {
            var validated = LocalSqlTarget.Validate(supplied, "Development");
            var source = new SqlConnectionStringBuilder(validated);
            DatabaseName = "Nexora_Test_" + Guid.NewGuid().ToString("N");

            var master = new SqlConnectionStringBuilder(source.ConnectionString)
            {
                InitialCatalog = "master"
            };
            _masterConnectionString = master.ConnectionString;

            await CreateDatabaseAsync(DatabaseName);
            var database = new SqlConnectionStringBuilder(source.ConnectionString)
            {
                InitialCatalog = DatabaseName
            };
            ConnectionString = LocalSqlTarget.Validate(database.ConnectionString, "Development");

            var sourceMigrations = Path.Combine(AppContext.BaseDirectory, "migrations");
            var stagedMigrations = Path.Combine(_runRoot, "migrations-0024");
            Directory.CreateDirectory(stagedMigrations);
            foreach (var migration in Directory.EnumerateFiles(sourceMigrations, "*.sql", SearchOption.TopDirectoryOnly)
                         .Where(path => !Path.GetFileName(path).StartsWith("20260913_0025_", StringComparison.Ordinal) &&
                                        !Path.GetFileName(path).StartsWith("20260913_0026_", StringComparison.Ordinal)))
            {
                File.Copy(migration, Path.Combine(stagedMigrations, Path.GetFileName(migration)));
            }

            var migrations = new SqlMigrationRunner();
            await migrations.ApplyAsync(ConnectionString, stagedMigrations);
            ReadinessAt0024 = await new SqlReadinessProbe(new SqlConnectionFactory(ConnectionString)).CheckAsync();
            Require(
                !ReadinessAt0024.Ready &&
                string.Equals(ReadinessAt0024.Dependencies["requiredMigrations"], "MissingRequired", StringComparison.Ordinal),
                "A journal ending at 0024 must be not ready for the M01 binary.");

            await migrations.ApplyAsync(ConnectionString, sourceMigrations);
            await migrations.ApplyAsync(ConnectionString, sourceMigrations);
            var expectedMigrationCount = Directory.GetFiles(sourceMigrations, "*.sql").Length;
            Require(
                await ScalarIntAsync("SELECT COUNT(*) FROM dbo.NexoraMigration;") == expectedMigrationCount,
                "Migration replay must not add journal rows.");
            ReadinessAfterFullMigration = await new SqlReadinessProbe(new SqlConnectionFactory(ConnectionString)).CheckAsync();
            Require(
                !ReadinessAfterFullMigration.Ready &&
                string.Equals(ReadinessAfterFullMigration.Dependencies["requiredMigrations"], "Ready", StringComparison.Ordinal),
                "A fully migrated but unbootstrapped database must remain not ready.");

            await AssertJournalChecksumGuardAsync(migrations, sourceMigrations);

            var bootstrap = new SqlBootstrapSuperAdmin(ConnectionString);
            var bootstrapPassword = "M01-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(12)) + "-aA!";
            var bootstrapCommand = new SqlBootstrapSuperAdminCommand
            {
                Email = "bootstrap-" + Guid.NewGuid().ToString("N") + "@example.invalid",
                DisplayName = "Synthetic bootstrap",
                TimeZoneId = "Etc/UTC",
                PasswordHash = bootstrapPassword
            };

            await ExecuteAsync(
                """
                CREATE TRIGGER [security].[RejectBootstrapForIntegrationTest]
                ON [security].[AuditEvent] AFTER INSERT AS
                BEGIN
                    THROW 51002, 'Synthetic audit failure', 1;
                END;
                """);
            try
            {
                try
                {
                    await bootstrap.ExecuteAsync(bootstrapCommand);
                    throw new InvalidOperationException("The synthetic bootstrap audit failure was not raised.");
                }
                catch (SqlException exception) when (exception.Number == 51002)
                {
                    // Expected: all bootstrap writes must be rolled back.
                }

                Require(await ScalarIntAsync("SELECT COUNT(*) FROM [identity].[User];") == 0,
                    "A bootstrap audit failure must roll back the user.");
                Require(await ScalarIntAsync("SELECT COUNT(*) FROM [platform].[PersonalSpace];") == 0,
                    "A bootstrap audit failure must roll back the personal space.");
                Require(await ScalarIntAsync("SELECT COUNT(*) FROM [identity].[UserRole];") == 0,
                    "A bootstrap audit failure must roll back roles.");
                Require(await ScalarIntAsync(
                    "SELECT COUNT(*) FROM [platform].[SecurityInvariant] WHERE [BootstrapCompletedAt] IS NOT NULL;") == 0,
                    "A bootstrap audit failure must not close the bootstrap gate.");
            }
            finally
            {
                await ExecuteAsync("DROP TRIGGER [security].[RejectBootstrapForIntegrationTest];");
            }

            var bootstrapResults = await Task.WhenAll(
                bootstrap.ExecuteAsync(bootstrapCommand),
                bootstrap.ExecuteAsync(bootstrapCommand));
            Require(bootstrapResults.Count(result => result == BootstrapOutcome.Created) == 1,
                "Exactly one concurrent bootstrap must create the first SuperAdmin.");
            Require(bootstrapResults.Count(result => result == BootstrapOutcome.AlreadyBootstrapped) == 1,
                "The concurrent bootstrap loser must be closed.");
            Require(await ScalarIntAsync(
                "SELECT COUNT(*) FROM [identity].[User] WHERE [State] = 'Active' AND [EmailConfirmed] = 1 AND [VerifiedAt] IS NOT NULL;") == 1,
                "The synthetic bootstrap must leave one active principal.");

            // Synthetic corruption models a later role-row loss. The durable
            // bootstrap completion marker must keep the bootstrap gate closed.
            await ExecuteAsync(
                """
                DELETE ur
                FROM [identity].[UserRole] AS ur
                INNER JOIN [identity].[Role] AS r ON r.[Id] = ur.[RoleId]
                WHERE r.[Code] = 'SuperAdmin';
                """);
            Require(await bootstrap.ExecuteAsync(bootstrapCommand) == BootstrapOutcome.AlreadyBootstrapped,
                "The bootstrap completion marker must remain closed after a role-row loss.");
            ReadinessAfterBootstrap = await new SqlReadinessProbe(new SqlConnectionFactory(ConnectionString)).CheckAsync();
            Require(ReadinessAfterBootstrap.Ready, "A fully migrated and bootstrapped database must be ready.");

            SetApiEnvironment();
            _factory = new NexoraApiFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
                BaseAddress = new Uri("https://localhost")
            });
        }
        catch (Exception exception)
        {
            // Do not put a connection string, synthetic credential, or raw
            // capture content in test output. The type is enough to separate a
            // missing runtime from a product failure in the handoff.
            BlockedReason = "fixture setup failed: " + exception.GetType().Name;
            await DisposeCreatedResourcesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await DisposeCreatedResourcesAsync();
    }

    public void RequireAvailable()
    {
        if (Available)
        {
            return;
        }

        if (string.Equals(BlockedReason, "NEXORA_TEST_SQL_CONNECTION was not supplied.", StringComparison.Ordinal))
        {
            throw new XunitException("BLOCKED: real SQL/API integration fixture is unavailable (" + BlockedReason + ").");
        }

        throw new InvalidOperationException(
            "The SQL/API integration fixture setup failed (" + (BlockedReason ?? "unknown setup state") + ").");
    }

    public async Task<int> ScalarIntAsync(string statement, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(RequireConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(statement, connection);
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<string> ScalarStringAsync(string statement, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(RequireConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(statement, connection);
        command.Parameters.AddRange(parameters);
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("The synthetic SQL fixture returned no string.");
    }

    public async Task<byte[]> ScalarBytesAsync(string statement, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(RequireConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(statement, connection);
        command.Parameters.AddRange(parameters);
        return (byte[])(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("The synthetic SQL fixture returned no binary value."));
    }

    public async Task ExecuteAsync(string statement, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(RequireConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(statement, connection);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<SyntheticSession> CreateActiveSessionAsync()
    {
        RequireAvailable();
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var rawSessionHandle = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var securityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var email = "profile-" + userId.ToString("N") + "@example.invalid";

        await ExecuteAsync(
            """
            INSERT INTO [identity].[User]
                ([Id], [Email], [NormalizedEmail], [PasswordHash], [SecurityStamp], [State],
                 [EmailConfirmed], [VerifiedAt], [IsDeleted], [DisplayName], [TimeZoneId], [Locale])
            VALUES
                (@userId, @email, @email, @passwordHash, @securityStamp, 'Active',
                 1, @now, 0, 'Original display', 'Etc/UTC', 'en');

            INSERT INTO [platform].[PersonalSpace] ([Id], [UserId], [State], [CreatedAt], [UpdatedAt])
            VALUES (@ownerId, @userId, 'Active', @now, @now);

            INSERT INTO [identity].[UserRole] ([UserId], [RoleId])
            SELECT @userId, [Id] FROM [identity].[Role] WHERE [Code] = 'User';

            INSERT INTO [identity].[Session]
                ([Id], [UserId], [HandleHash], [DeviceLabel], [SecurityStamp], [CreatedAt],
                 [LastSeenAt], [IdleExpiresAt], [AbsoluteExpiresAt], [RecentAuthenticatedAt])
            VALUES
                (@sessionId, @userId, @handleHash, 'Synthetic integration', @securityStamp, @now,
                 @now, @idleExpiresAt, @absoluteExpiresAt, @now);
            """,
            Parameter("@userId", SqlDbType.UniqueIdentifier, userId),
            Parameter("@ownerId", SqlDbType.UniqueIdentifier, ownerId),
            Parameter("@sessionId", SqlDbType.UniqueIdentifier, sessionId),
            Parameter("@email", SqlDbType.NVarChar, email, 320),
            Parameter("@passwordHash", SqlDbType.NVarChar, "synthetic-no-login-hash", 1024),
            Parameter("@securityStamp", SqlDbType.NVarChar, securityStamp, 128),
            Parameter("@handleHash", SqlDbType.Binary, SHA256.HashData(Encoding.UTF8.GetBytes(rawSessionHandle)), 32),
            Parameter("@now", SqlDbType.DateTime2, now),
            Parameter("@idleExpiresAt", SqlDbType.DateTime2, now.AddHours(8)),
            Parameter("@absoluteExpiresAt", SqlDbType.DateTime2, now.AddDays(7)));

        return new SyntheticSession(userId, ownerId, sessionId, rawSessionHandle);
    }

    public async Task MoveProfileReceiptToCurrentNamespaceAsync(Guid userId, Guid idempotencyKey)
    {
        RequireAvailable();
        var currentSubjectHash = Hmac("user:" + userId.ToString("N"));
        await ExecuteAsync(
            """
            UPDATE [identity].[RequestReceipt]
            SET [SubjectHash] = @currentSubjectHash
            WHERE [OperationKey] = 'identity.profile.update'
              AND [KeyHash] = @keyHash;
            """,
            Parameter("@currentSubjectHash", SqlDbType.Binary, currentSubjectHash, 32),
            Parameter("@keyHash", SqlDbType.Binary, Hmac("key:" + idempotencyKey), 32));
    }

    public async Task<CsrfContext> GetCsrfAsync()
    {
        RequireAvailable();
        using var response = await Client.GetAsync("/api/v1/auth/csrf");
        Require(response.StatusCode == HttpStatusCode.OK, "CSRF issuance must return HTTP 200.");
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.ToArray()
            : Array.Empty<string>();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("requestToken").GetString();
        Require(!string.IsNullOrWhiteSpace(token), "CSRF issuance must return a request token.");
        var cookieHeader = string.Join("; ", setCookies.Select(value => value.Split(';', 2)[0]));
        Require(cookieHeader.Contains("__Host-NexoraCsrf=", StringComparison.Ordinal) &&
                cookieHeader.Contains("__Host-NexoraAnonymousSession=", StringComparison.Ordinal),
            "CSRF issuance must set both private cookies.");
        return new CsrfContext(token!, cookieHeader);
    }

    public async Task<HttpResponseMessage> SendJsonAsync(
        HttpMethod method,
        string path,
        object payload,
        CsrfContext csrf,
        Guid idempotencyKey,
        string? sessionHandle = null,
        string? ifMatch = null)
    {
        RequireAvailable();
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("X-CSRF-Token", csrf.RequestToken);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey.ToString());
        if (!string.IsNullOrWhiteSpace(ifMatch))
        {
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        var cookies = sessionHandle is null
            ? csrf.CookieHeader
            : csrf.CookieHeader + "; __Host-NexoraSession=" + sessionHandle;
        request.Headers.TryAddWithoutValidation("Cookie", cookies);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetMeAsync(string sessionHandle)
    {
        RequireAvailable();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.TryAddWithoutValidation("Cookie", "__Host-NexoraSession=" + sessionHandle);
        return await Client.SendAsync(request);
    }

    private async Task CreateDatabaseAsync(string database)
    {
        if (_masterConnectionString is null ||
            !Regex.IsMatch(database, "^Nexora_Test_[0-9a-f]{32}$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("The synthetic integration database name is invalid.");
        }

        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using (var exists = new SqlCommand("SELECT DB_ID(@name);", connection))
        {
            exists.Parameters.Add(Parameter("@name", SqlDbType.NVarChar, database, 128));
            var existingDatabase = await exists.ExecuteScalarAsync();
            if (existingDatabase is not null && existingDatabase is not DBNull)
            {
                throw new InvalidOperationException("A generated integration database unexpectedly already exists.");
            }
        }

        await using var create = new SqlCommand($"CREATE DATABASE [{database}];", connection);
        await create.ExecuteNonQueryAsync();
        _databaseCreated = true;
    }

    private async Task AssertJournalChecksumGuardAsync(SqlMigrationRunner migrations, string sourceMigrations)
    {
        var checksumDirectory = Path.Combine(_runRoot, "journal-checksum");
        Directory.CreateDirectory(checksumDirectory);
        var source = Directory.EnumerateFiles(sourceMigrations, "*.sql", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .First();
        var destination = Path.Combine(checksumDirectory, Path.GetFileName(source));
        await File.WriteAllTextAsync(destination, await File.ReadAllTextAsync(source) + Environment.NewLine + "-- synthetic checksum probe");

        var rejected = false;
        try
        {
            await migrations.ApplyAsync(RequireConnectionString(), checksumDirectory);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Require(rejected, "An applied migration with a different checksum must be rejected.");
    }

    private byte[] Hmac(string value)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_idempotencySecret));
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private void SetApiEnvironment()
    {
        SetEnvironment("DOTNET_ENVIRONMENT", "Development");
        SetEnvironment("NEXORA_SQL_CONNECTION_STRING", RequireConnectionString());
        SetEnvironment("ConnectionStrings__NexoraSql", RequireConnectionString());
        SetEnvironment("NEXORA_IDEMPOTENCY_SECRET", _idempotencySecret);
        SetEnvironment("NEXORA_CSRF_SECRET", _csrfSecret);
        SetEnvironment("NEXORA_LOCAL_MESSAGE_KEY", _localMessageKey);
        SetEnvironment("NEXORA_LOCAL_MESSAGE_CAPTURE_PATH", _captureDirectory);
    }

    private void SetEnvironment(string name, string value)
    {
        if (!_previousEnvironment.ContainsKey(name))
        {
            _previousEnvironment[name] = Environment.GetEnvironmentVariable(name);
        }

        Environment.SetEnvironmentVariable(name, value);
    }

    private async Task DisposeCreatedResourcesAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        _client = null;
        _factory = null;

        foreach (var pair in _previousEnvironment)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }

        _previousEnvironment.Clear();

        if (_databaseCreated && DatabaseName is not null && _masterConnectionString is not null)
        {
            if (!Regex.IsMatch(DatabaseName, "^Nexora_Test_[0-9a-f]{32}$", RegexOptions.CultureInvariant))
            {
                throw new InvalidOperationException("Refusing to clean a non-generated SQL database.");
            }

            try
            {
                await using var connection = new SqlConnection(_masterConnectionString);
                await connection.OpenAsync();
                await using var drop = new SqlCommand(
                    $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END;",
                    connection);
                drop.Parameters.Add(Parameter("@name", SqlDbType.NVarChar, DatabaseName, 128));
                await drop.ExecuteNonQueryAsync();
                _databaseCreated = false;
            }
            catch
            {
                throw new InvalidOperationException("Synthetic SQL integration database cleanup failed.");
            }
        }

        try
        {
            if (Directory.Exists(_runRoot))
            {
                Directory.Delete(_runRoot, recursive: true);
            }
        }
        catch
        {
            throw new InvalidOperationException("Synthetic SQL integration capture cleanup failed.");
        }
    }

    private string RequireConnectionString() =>
        ConnectionString ?? throw new InvalidOperationException("The synthetic SQL connection was not initialized.");

    private static SqlParameter Parameter(string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
        parameter.Value = value;
        return parameter;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class NexoraApiFactory : WebApplicationFactory<ApiProgram>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
        }
    }
}

public sealed record SyntheticSession(Guid UserId, Guid OwnerId, Guid SessionId, string RawSessionHandle);

public sealed record CsrfContext(string RequestToken, string CookieHeader);
