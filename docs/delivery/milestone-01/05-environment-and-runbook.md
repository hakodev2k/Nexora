# M01 environment and runbook specification

**Design only — scripts, lockfiles, migrations, service processes and evidence below are future deliverables. Do not run these as existing commands.** [Prior local roadmap](../../roadmap/03-local-development-roadmap.md) remains a broader reference; M01 acceptance follows this slice.

## Research pins (2026-09-08)

| Dependency | Selected research baseline | Pin artifact required after approval |
| --- | --- | --- |
| .NET SDK |10.0.400; ASP.NET runtime10.0.11 | global.json exact SDK, runtime/package inventory |
| SQL Server |2025 Developer CU8,17.0.4075.5, local Windows x64 | edition/build/collation/compatibility recorded; Developer not production license |
| Node/npm |Node24.20.0 LTS; bundled npm11.19.0 | exact engines/tool-version inventory |
| React/react-dom |19.2.8 matched | exact package versions and package-lock integrity |
| Vite |8.2.2 stable; exclude8.3 beta | exact pin and tested React plugin pairing |
| EF Core/SQL provider |10.0.11 matched, [official releases](https://github.com/dotnet/efcore/releases) | Exact package/tool pins and SQL integration test in S00 |
| TypeScript, Vite React plugin, test runner | Supported TS/React toolchain | S00 resolves **all exact package patches** from official releases, checks advisories/peer deps, commits lockfiles only after implementation approval |
| Redis |8.2.9 research pin, [official release notes](https://redis.io/docs/latest/operate/oss_and_stack/stack-with-enterprise/release-notes/redisce/redisos-8.2-release-notes/); rebuildable cache | Optional local Linux/WSL2 adapter in S00; patch/advisory recheck and outage test before enabling; M01 correctness must pass with cache disabled |

Release source links and observation limits: [research evidence](../03-reference-evidence.md). Pins are not proof of compatibility or vulnerability-free operation. If security patch released before implementation, developer updates pin record and compatibility evidence under technical authority; not a business question. No install or restore executed now. Package-patch resolution is a bounded first implementation task, not an unbounded Open product decision.

## Local topology and configuration

Windows x64 reference profile, loopback React HTTPS5173 proxy `/api`→API HTTPS7043; SQL TCP14333; optional Redis private6379. API and worker share one SQL deployment, separate logical process responsibilities. Synthetic data only. File/key roots outside checkout/webroot. Validate ports/OS/RAM free space; do not bind dependencies public to solve connection errors.

App config contract: Environment=Development; ConnectionStrings:SqlServer (secret external), optional Redis endpoint; FileStorage:Root; Crypto:KeyProvider/KeyRingPath references, Notifications:EmailAdapter/PushAdapter, Jobs:Enabled/lease, Bootstrap:Enabled/one-time proof source. Frontend receives public config only. Missing key/SQL fails safe. ASP.NET development secret manager avoids commits but is not encrypted key storage; operator uses OS-protected key provider. HTTPS certificate validation remains enabled.

## Planned scripts — contract, not implemented files

| Future artifact | Inputs / effect | Required result / failure |
| --- | --- | --- |
| scripts/local/doctor.ps1 | Read-only versions/OS/ports/config-shape checks, no values | Version report; mismatch exits nonzero with safe remediation |
| scripts/local/configure.ps1 | Explicit Development profile/paths; protected prompts | Non-secret settings + key references; no overwrite of unrelated config |
| scripts/local/dependencies.ps1 | Start/Verify selected local SQL/cache adapters only | Private endpoints reachable; no DB create/delete in Verify |
| scripts/local/migrate.ps1 | Explicit Nexora_Dev target, migration credential and journal lock | Apply reviewed ordered migrations; fail before mismatch; journal each outcome; no production default |
| scripts/local/seed.ps1 | Fixed role/catalog/reference definitions; optional Synthetic flag | Idempotent reference rows, no default credentials; no fake Ready modules |
| scripts/local/bootstrap.ps1 | Operator local access, verified bootstrap contact, password protected input | TX01 single SuperAdmin, one-time bootstrap closure; no HTTP bypass |
| scripts/local/run.ps1 | Validated profile and pinned artifacts | API/worker/UI startup health; safe shutdown; no auto migration |
| scripts/local/verify.ps1 | Isolated test target + synthetic data | API/schema/SQL/auth/keyboard/artifact report; no mutation of developer real data |
| scripts/local/restore-rehearsal.ps1 | Explicit isolated target, manifest/SQL/files/key refs | Restore proof and report; outward delivery disabled during rehearsal; verify retained revocation barriers |

## Runbook sequence after implementation approval

1. Read approved commit and inspect clean working tree. Verify SDK/SQL/Node/tool versions match pin inventory.
2. Restore locked dependencies only; record lock hashes. Configure local certificates/private endpoints/external key references without printing secret values.
3. Verify SQL connectivity before migrations. Target guard must show Development/Nexora_Dev; migration identity separated from least-privilege runtime identity.
4. Apply approved migration journal, seed reference catalog, perform protected bootstrap once. Repeat setup must be safe or refuse clearly.
5. Start API/worker/UI. `/health/live` reveals process-only status; `/health/ready` returns minimal ready/degraded status, detailed dependency diagnostics operator-only, never connection strings.
6. Execute S02..S10 synthetic journeys; inspect captured email and push test outcomes labeled simulation. A capture adapter must not report real-user Delivered.
7. Stop/restart worker during a delivery and revoke a session between API requests. Verify no lost mutation/duplicate intent/stale positive authorization.
8. Restore candidate into isolated target with outbound send disabled, validate checksums/key availability/schema versions and authority revocation. Record evidence; do not claim production RPO/RTO from local rehearsal.

## Failure/recovery procedures

SQL down: readiness false; return503, no success toast. Redis down: SQL fallback, report cache degraded; no dropped required events. Provider failure: intent remains durable; exponential bounded retry with jitter, max8 attempts/24h technical test profile, terminal Failed visible to operator; never block independent channel. Migration failure: application refuses affected module readiness; inspect journal, choose reviewed roll-forward or restore isolated backup; never ad-hoc edit DB to pretend success. Missing crypto root: fail closed, no plaintext fallback/key regeneration that strands existing data.

## Evidence and production boundary

M01 required outputs: build commit, exact tool/package inventory, migration journal, clean-start report, test result links, SQL integrity assertions, redacted outbox/delivery evidence, responsive/keyboard screenshots and restore rehearsal manifest. All currently **Not run / not implemented**.

Production hosting/provider budget, capacity/SLA and target RPO/RTO are [P-H08](../02-decision-proposals.md). Local pins do not choose a paid provider or production license. Production runbook additionally needs credential rotation, backups off-host, monitored jobs/storage alerts, incident owner/escalation, and real Email/Web Push transport proof. These are production gates, not reasons to block all design work.
