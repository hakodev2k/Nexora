# SQL bootstrap evidence — continuation of PR #4

Authority: DEC-20260909-014; exact slice/goal/source/AC trace in
[bootstrap contract](identity-bootstrap-contract.md). Starting source:
`f61619120b51d3fa921ca7fc412aea959132c7dd`. Evidence applies to the bootstrap source
tree committed with this document, not to later changed application code.

## Implemented

SQL Infrastructure bootstrap transaction; additive permanent bootstrap-closure
migration; operator console and PowerShell/Bash launchers; local target validation;
ordered/checksummed migration journal with deployment locking; real SQL bootstrap
integration harness; dependency lockfiles; CI SQL job (execution pending).

## Commands actually executed

| Command | Actual result |
| --- | --- |
| `dotnet --info` | SDK 10.0.302 / runtime 10.0.10 on Windows x64 |
| `node --version`; `npm --version` | 24.17.0 / 11.13.0 |
| `docker version` | Client available; daemon unavailable; sandbox read initially denied, elevated diagnostic confirmed absent daemon |
| `dotnet build src/Nexora.Infrastructure/Nexora.Infrastructure.csproj --configuration Release` | Pass after approved escalation for NuGet configuration access; initial sandbox attempt failed |
| `dotnet build src/Nexora.Local/Nexora.Local.csproj --configuration Release` | Pass, 0 warnings/errors |
| `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release` | Pass, 0 warnings/errors |
| `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release` | Pass, 24 tests; existing memory-store tests do not prove SQL identity flows |
| `dotnet tests/Nexora.IntegrationTests/bin/Release/net10.0/Nexora.IntegrationTests.dll` with explicit synthetic connection | Pass on local SQL Express; migrations/journal replay, audit failure rollback, concurrent bootstrap, permanent closure assertions |
| `git diff --check` | Pass before final evidence-only edits; rerun on commit preparation |
| `python3 .ai/scripts/verify-baseline.py` | Not run: Python executable unavailable; command invocation failed |

Final SQL test target: `Nexora_Test_8c701dc4865c4e7ab202b90592f63327`.
Earlier synthetic targets retained for inspection:
`Nexora_Test_37b1c051c1a94eeca22dc7f573a5b902` and
`Nexora_Test_82395523571346be973d6d6920d74b87`. Tests created these targets and
modified only synthetic fixtures within them. No existing user database was read
or changed. Local encrypted SQL connection trusted the self-signed certificate;
production TLS validation was not tested. SQL Express is not the runbook's SQL
Server 2025 Developer reference profile; exact reference-profile evidence remains
pending. CI uses an isolated SQL Server 2022 Developer compatibility profile.

## Pending and next dependency

Interactive operator console journey, frontend build/lint/browser E2E, complete
identity SQL flows, full M01 acceptance, notifications/outbox, reference-profile
restore rehearsal and independent security/migration review are not verified by
this slice. CI configuration has been added but no CI pass is claimed here.
The current API still uses DevelopmentIdentityStore and DevelopmentModuleStore.
SQL bootstrap cannot yet log in through that API. Replace those stores with SQL
application services next, retaining the secure cookie/CSRF/ETag contracts.

Remaining R1 modules and post-M01 contract reconciliation are ongoing. No module,
phase, full local E2E or production completion is claimed. PR #4 stays unmerged.
