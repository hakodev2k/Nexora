# M01 implementation evidence note — PR #4

Revision scope: `impl/m01-s00-scaffold`.

Status: `SLICE_IMPLEMENTATION_STARTED`, not `SLICE_VERIFIED_LOCALLY`.

## Authorization and scope

- Product Owner request: implement Nexora according to current docs and continue by dependency.
- Current repository authority: `DEC-20260909-014` approves full local Release 1 implementation slice-by-slice when the contract is sufficient; M01 remains a delivery/evidence label.
- This branch implements SQL-backed Identity, Module Platform, SuperAdmin access administration, Notifications inbox, Trash lifecycle, Settings preferences, Documents page core and the first owner-scoped Projects/Tasks/Calendar slice with local-safe scripts and frontend flows.
- It does not implement production deployment, provider execution, production secrets or production data. Remaining feature groups stay explicitly unimplemented in `local-e2e-status.md`.

## Backend layout correction

`M01` is a delivery milestone, not a backend module or bounded context. Runtime API code is therefore organized by feature/responsibility while keeping Minimal API style:

- `src/Nexora.Api/Features/Identity/**` for identity HTTP contracts and Minimal API route mapping; SQL authority lives in Infrastructure.
- `src/Nexora.Api/Features/Modules/**` for module catalog/policy HTTP contracts and Minimal API route mapping; SQL authority lives in Infrastructure.
- `src/Nexora.Api/Features/Access/**`, `Notifications/**`, `Trash/**`, `Settings/**`, `Documents/**`, and `Productivity/**` for their feature contracts and Minimal API route mapping.
- `src/Nexora.Api/Http/**` for HTTP result/problem helpers.
- `src/Nexora.Api/Security/**` for CSRF, session cookie and security-header support.
- `src/Nexora.Application/**` for application policies/use-case inputs.
- `src/Nexora.Domain/**` for domain policies and invariants.

The milestone name remains only in docs, evidence, tests and PR traceability. No runtime code remains under `src/Nexora.Api/M01`, and no MVC controller surface is used for the current slice.

## Current implemented code artifacts

- Local scaffold and deterministic scripts.
- ASP.NET Core Minimal API runtime surface under `/api/v1` for SQL-backed identity, access, notifications and productivity flows.
- SQL-backed module catalog/policy preview and commit with actor-bound previews and dependency checks.
- React/Vite local shell with identity, profile/security, notification inbox, Trash controls, theme preferences, Documents page core and Projects/Tasks/Calendar CRUD flows.
- Domain/application policy code used by identity, access, module, notification, idempotency and profile rules.
- SQL Server migrations for identity/platform/security/operations/notifications, productivity lifecycle, preferences and Documents page/version tables.

## API routes currently mapped

Identity / self:

- `GET /api/v1/auth/csrf`
- `POST /api/v1/auth/registrations`
- `POST /api/v1/auth/verifications`
- `POST /api/v1/auth/verifications/resend`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/reauth`
- `POST /api/v1/auth/password-resets`
- `POST /api/v1/auth/password-resets/confirm`
- `GET /api/v1/me`
- `PATCH /api/v1/me`
- `GET /api/v1/me/sessions`
- `DELETE /api/v1/me/sessions/{sessionId}`
- `POST /api/v1/me/sessions/revoke-all`
- No development mailbox endpoint is exposed. Account verification/reset delivery is represented by a durable SQL intent/outbox boundary; a separately approved local transport is required to obtain a token for manual QA.

Module policy smoke surface:

- `GET /api/v1/admin/modules`
- `POST /api/v1/admin/modules/{moduleId}/preview`
- `PUT /api/v1/admin/modules/{moduleId}/policy`

The current module policy API authenticates the SQL-backed session and requires the SuperAdmin role. Preview tokens are short-lived, signed and actor-bound; commit re-reads the locked SQL row and dependencies.

Local business surfaces:

- `GET/POST /api/v1/notifications*` and `POST /api/v1/admin/notifications`
- `GET/POST /api/v1/trash*`
- `GET/PUT /api/v1/settings/preferences*`
- `GET/POST/PUT /api/v1/documents*`
- `GET/POST/PUT/DELETE /api/v1/projects`, `/tasks` and `/calendar/events`

## Goal and requirement trace

| Goal/source | Bound implementation |
| --- | --- |
| M01-S00 / M01-AC00 | Pinned toolchain files, reproducible local scripts, Docker Compose local SQL/Redis profile, application CI scaffold. |
| M01-S02/S03/S04/S05/S06 API surface | Runtime routes for registration, verification, resend, login/logout/reauth, password reset, profile and sessions. |
| M01-S07/S09 API surface | Module catalog list, actor-bound policy preview and SQL-backed policy commit with dependency/paused guards. |
| M01-S01/S08/S09 policy foundation | Last-SuperAdmin, action grant and module dependency/paused policies. |
| M01 data contract | SQL Server migration artifact for identity/platform/security/operations/notifications tables. |
| Effective action status overlay | Local-safe routes are exposed only for contracted slices; paused provider execution remains disabled. |

## Verification actually executed in this ChatGPT runtime

The first scaffold revision was statically checked before later API additions:

```text
node --version => v22.16.0
npm --version => 10.9.2
python3 --version => Python 3.13.5
dotnet --info => dotnet: command not found

bash scripts/dev/doctor.sh
[missing] .NET SDK 10 (dotnet)
[ok] Node.js: v22.16.0
[ok] npm: 10.9.2
[ok] Python: Python 3.13.5
[optional-missing] Docker (docker)
S00 doctor completed with missing required tools; rerun with --strict to fail.

python3 scripts/dev/verify-s00.py
S00 static verification passed: scaffold files, .NET 10 pin, CSRF memory boundary, and paused-scope guards are present.
```

## Not run / not claimed

After the SQL-backed additions, this environment still lacks the .NET SDK, so these remain **Not run** here:

- `dotnet build`
- `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release`
- SQL Server migration execution
- SQL Server integration tests
- Browser/manual E2E tests

SQL-backed persistence, transaction locking, idempotency receipts, audit/outbox integration and real SuperAdmin authorization are implemented in this revision. Runtime migration, functional/integration/E2E evidence is still **Not run** and must be supplied by the human owner before a slice can be marked `SLICE_VERIFIED_LOCALLY`.

## Scope safety

This branch still does not implement Files, Sharing, Support/Emergency, Vault, Finance, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations. It does not enable FX30/FX34/FX35 workers and does not use production data/secrets/provider calls. Projects, Tasks, Calendar, Notifications, Trash lifecycle, Settings preferences and the Documents page core are implemented only in their documented local slices; advanced extensions remain open.

Last architecture note: Minimal API is the intended current API style for this branch.
