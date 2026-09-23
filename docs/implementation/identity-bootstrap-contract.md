# SQL bootstrap implementation contract

Authority: DEC-20260909-014 and existing M01-S01/TX01/M01-AC01.
Sources: P01-SHL-002, P01-PDS-001, P01-RBAC-004, P01-PLT-001.
Goals: NXG-SYS-01/03/05/08/14, NXG-FX02-G02. No user action key or HTTP endpoint;
operator command `bootstrap-superadmin` is the operation binding.

## Local command and transaction

Run `scripts/dev/bootstrap-superadmin.ps1` or `.sh` after migration, with
`DOTNET_ENVIRONMENT=Development` and `NEXORA_SQL_CONNECTION` supplied externally.
Accept only a loopback SQL server and database `Nexora_Dev` or a generated
`Nexora_Test_` database. No default database password. Console prompts collect
email, display name, IANA timezone and password (hidden, twice). Redirected input
is rejected; password command arguments are unsupported. No HTTP bootstrap route.

Validate before SQL: email using existing normalization, display name 1–100,
known IANA timezone and existing password policy. Hash using the existing
PBKDF2-SHA512 format and fresh random salt; no plaintext in logs, SQL or receipts.

Within one SQL transaction: acquire UPDLOCK/HOLDLOCK on SecurityInvariant Id=1;
fail closed if absent; reject if BootstrapCompletedAt is set or an elevated
SuperAdmin role has ever been assigned in the current database. Insert verified
Active User, distinct PersonalSpace Id, User and SuperAdmin roles, metadata-only
audit; set BootstrapCompletedAt. Commit once. Any failure rolls back every write.
No fake Ready modules or grants are seeded by bootstrap. Later registration
and installed module seeding implement their own contracts.

Migration 0002 adds nullable BootstrapCompletedAt without dropping data; existing
SuperAdmin rows close bootstrap during upgrade. Preserve 0001 unchanged. Rollback
uses the prior binary with the additive column retained; never reopen bootstrap
by clearing the marker. Test databases are synthetic and isolated.

## Verification plan and boundaries

| AC/goal | Assertion | Evidence |
| --- | --- | --- |
| M01-AC01 / SYS05 / FX02-G02 | Two concurrent bootstrap attempts yield one Created and one AlreadyBootstrapped | Real SQL integration test |
| P01-SHL-002 / SYS03 | Closed marker prevents replay after role changes | Real SQL integration test |
| P01-PDS-001 / SYS01 | One verified principal, distinct owner, User+SuperAdmin roles | Real SQL query assertions |
| M01-AC01 / SYS08 | Persisted audit contains no password/hash/contact | SQL projection assertion |
| TX01 / SYS05 | Forced audit failure rolls back principal/space/roles/marker | SQL fault injection in isolated test DB |
| SYS14 | Record actual build/test/migration revision | PR and implementation evidence |

Operator console is this story's UI. Browser login integration remains the next
dependency. Independent security/migration review remains Pending. Provider calls
are absent. SQL readiness and tests must actually run before claiming verification.

EF SQL provider pin: 10.0.11, matching the existing runbook and
[Microsoft's published package](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.11).
Observed local SDK: 10.0.302; Node: 24.17.0/npm 11.13.0. Existing research pins differ;
compatibility is measured by builds, not inferred from version discovery.
