# Core sharing, support and files slice — 2026-09-11

This note records the local source implementation continued on PR #4,
`impl/m01-s00-scaffold`, under `DEC-20260909-014`. It is a code handoff, not
runtime acceptance evidence. The current amendment forbids new tests, fixtures,
demo data and provider execution.

## Delivered source

| Area | SQL authority | API | UI | Security boundary |
| --- | --- | --- | --- | --- |
| Sharing | `security.ShareLink`, `security.ShareAllowedUser` | `/api/v1/sharing/links*`, `/api/v1/sharing/resolve/{token}` | `/sharing`, `/share/{token}` | Token hash only at rest; one-time raw token response; owner/source/viewer/epoch checks; seven-day default or explicit no-expiry/custom expiry; read-only allow-list projection |
| Support | `security.SupportGrant`, `security.AccessSession` | `/api/v1/support/grants*`, `/api/v1/support/sessions*`, `/api/v1/support/emergency` | `/support` | PersonalSpace owner consent; exact module/action gate; explicit AdminPermission; durable opening audit; no impersonation or export |
| Files | `files.FileObject`, `files.FileReference`, `files.UploadSession` | `/api/v1/files*` | `/files` | Private generated storage paths; upload handle hash; quarantine until scan clean; owner/source/lifecycle/reference checks |

The forward-only migration is
`database/migrations/20260911_0021_core_sharing_support_files.sql`. It also
registers action metadata, creates source/module invalidation triggers and is
included in `SqlReadinessProbe.RequiredMigrations`. The checked-in
`scripts/dev/migrate.sh` and `.ps1` invoke `Nexora.Local migrate`, so the
deployment lock, normalized checksum journal and pending-file replay are used
instead of applying SQL files directly one by one.

## Intentional fail-closed behavior

- `FX04`, `FX05` and `FX07` remain `Blocked`, system-disabled and
  registration-disabled by `20260910_0018_local_runtime_catalog_gate.sql`.
  Source coverage does not make these modules runtime-available.
- Emergency access remains `DecisionBlocked` until Q-02 approves duration and
  recent-auth requirements. The blocked path performs no target/module/business
  data read and no write.
- Files are locally scanned with conservative type/signature/text/archive
  checks. No external antivirus or provider delivery is claimed.
- Support currently exposes consent/session metadata only. It does not expose a
  business-data read surface through an Admin session.

## Known bounded gaps

- Sharing supports Project and Published/Archived Document projections only;
  no edit/comment/assignment, Vault, calendar or private-note projection.
- Files do not yet implement cover-specific 5 MiB/25MP validation, binary
  replacement revisions, persisted `DetectedType`, durable staged-upload
  cleanup or external AV.
- File purge reports cleanup pending when physical deletion fails; a durable
  cleanup retry worker is still required.
- SQL/browser/manual and independent security verification remain pending.

## Current command evidence

| Command | Result |
| --- | --- |
| `python3 .ai/scripts/verify-baseline.py` | PASS — baseline package and 15 gate tests; application tests not run |
| `python3 scripts/dev/verify-s00.py` | PASS — source/migration/script wiring and static boundary checks |
| `npm ci --prefix web/Nexora.Web --ignore-scripts` | PASS |
| `npm run build --prefix web/Nexora.Web` | PASS — TypeScript/Vite production bundle |
| `git diff --check` | PASS |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release` | Blocked — `dotnet: command not found` |
| `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release` | Blocked — `dotnet: command not found` |
| `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release` | Blocked — `dotnet: command not found` |
| SQL migration/runtime, including `scripts/dev/migrate.*`, browser/E2E, independent review | Not run |

Do not merge PR #4 as part of this continuation.
