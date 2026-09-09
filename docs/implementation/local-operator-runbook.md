# Local SQL migration and bootstrap

Use only synthetic local data. API identity persistence is still transitional;
creating a SQL SuperAdmin does not yet make it available to the in-memory API.
The next slice replaces that API store. No public bootstrap endpoint exists.

Create an empty local `Nexora_Dev` database using your local SQL administrator.
Set `DOTNET_ENVIRONMENT=Development` and supply `NEXORA_SQL_CONNECTION` externally.
The scripts accept only loopback SQL and `Nexora_Dev` or a GUID-suffixed
`Nexora_Test_` target. There is no embedded database password, automatic remote
target or database creation in the operator commands. Migration credentials need
DDL permission; runtime credentials must later use least privilege.

Run from a terminal:

```powershell
./scripts/dev/migrate.ps1
./scripts/dev/bootstrap-superadmin.ps1
```

Bash equivalents are the `.sh` scripts. Migration applies ordered checked-in SQL
under a session deployment lock and journals normalized content hashes. Repeating
it verifies hashes and skips completed migrations. A failed migration must be
diagnosed and rolled forward; never modify an already applied migration.

Bootstrap prompts for a verified operator contact, display name, IANA timezone,
and hidden password/confirmation. Success exits 0, AlreadyBootstrapped exits 3,
invalid invocation exits 2, other failures exit 1 without logging SQL exceptions
or credentials. Keep the terminal interactive; input/output redirection is refused.
The permanent SQL closure marker must never be reset to recover an account.

SQL tests use a separate explicit `NEXORA_TEST_SQL_CONNECTION`, whose database must
be `Nexora_Test_<32 hex GUID>` and must not already exist. The runner creates this
database, uses synthetic fixtures and retains it for inspection. It refuses any
existing target and never reads application user databases. Generate a fresh GUID
for a repeat run. Test connections used locally encrypted loopback traffic with
certificate trust bypass for the SQL Express self-signed certificate; these runs
do not verify TLS certificate validation or production connection configuration.

```powershell
dotnet run --project tests/Nexora.IntegrationTests --configuration Release
```

SQL tests cover migration journal replay, bootstrap concurrency, audit-failure
rollback and permanent closure. They do not yet cover browser login, other
identity transactions, role administration, notification workers or full R1.
