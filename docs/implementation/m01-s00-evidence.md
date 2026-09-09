# M01 implementation evidence note — PR #4

Revision scope: `impl/m01-s00-scaffold`.

Status: `SLICE_IMPLEMENTATION_STARTED`, not `SLICE_VERIFIED_LOCALLY`.

## Authorization and scope

- Product Owner request: implement Nexora according to current docs and continue by dependency.
- Current repository authority: `DEC-20260909-001` approves M01 stories S00-S11 plus backend/frontend scaffold and local scripts.
- This branch currently implements M01 local scaffold, feature-based Minimal API identity runtime surface, development module-policy runtime surface, domain/application policy code, unit-test harness, and a SQL migration artifact.
- It does not implement full M01, business modules, paused modules, production deployment, provider calls, production secrets or production data.

## Backend layout correction

`M01` is a delivery milestone, not a backend module or bounded context. Runtime API code is therefore organized by feature/responsibility while keeping Minimal API style:

- `src/Nexora.Api/Features/Identity/**` for identity HTTP contracts, Minimal API route mapping, and the temporary development identity store.
- `src/Nexora.Api/Features/Modules/**` for module catalog/policy HTTP contracts, Minimal API route mapping, and the temporary development module store.
- `src/Nexora.Api/Http/**` for HTTP result/problem helpers.
- `src/Nexora.Api/Security/**` for CSRF, temporary development SuperAdmin proof, session cookie and password hashing support.
- `src/Nexora.Application/**` for application policies/use-case inputs.
- `src/Nexora.Domain/**` for domain policies and invariants.

The milestone name remains only in docs, evidence, tests and PR traceability. No runtime code remains under `src/Nexora.Api/M01`, and no MVC controller surface is used for the current slice.

## Current implemented code artifacts

- M01 local scaffold and deterministic scripts.
- ASP.NET Core Minimal API runtime surface under `/api/v1` for S02-S06 identity flows.
- Development-only module catalog/policy surface for S07/S09 smoke coverage.
- React/Vite local shell that calls the identity API surface and module catalog smoke API.
- Domain/application policy code used by identity, access, module, idempotency and profile rules.
- Unit-test harness and tests for identity/access/module policy/runtime service behavior.
- SQL Server migration artifact for the M01 identity/platform/security/operations/notifications tables.

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
- `GET /api/v1/dev/account-messages` in Development only, for captured local verification/reset tokens.

Module policy smoke surface:

- `GET /api/v1/admin/modules`
- `POST /api/v1/admin/modules/{moduleId}/preview`
- `PUT /api/v1/admin/modules/{moduleId}/policy`

The current module policy API is explicitly development-gated by `X-Nexora-Dev-SuperAdmin: true` plus Development environment. This is only a temporary local scaffold for route/contract smoke coverage. It is not the final authorization model and must be replaced by SQL-backed session/grant authority before M01 is verified.

## Goal and requirement trace

| Goal/source | Bound implementation |
| --- | --- |
| M01-S00 / M01-AC00 | Pinned toolchain files, reproducible local scripts, Docker Compose local SQL/Redis profile, application CI scaffold. |
| M01-S02/S03/S04/S05/S06 API surface | Runtime routes for registration, verification, resend, login/logout/reauth, password reset, profile and sessions. |
| M01-S07/S09 API smoke surface | Module catalog list, module policy preview, module policy commit route shape with development-only SuperAdmin proof guard. |
| M01-S01/S08/S09 policy foundation | Last-SuperAdmin, action grant and module dependency/paused policies. |
| M01 data contract | SQL Server migration artifact for identity/platform/security/operations/notifications tables. |
| Effective action status overlay | Only M01-approved identity/control/session/profile/module-policy action routes are exposed; no non-M01 business handlers are added. |

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

After the API/runtime additions, this environment still lacks the .NET SDK, so these remain **Not run** here:

- `dotnet build`
- `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release`
- SQL Server migration execution
- SQL Server integration tests
- Browser/manual E2E tests

The current identity and module stores are development in-memory implementations used to expose and exercise the M01 API surface. SQL-backed persistence, transaction locking, idempotency receipts, audit/outbox integration, real SuperAdmin authorization and true integration evidence still need follow-up implementation before M01 can be called verified.

## Scope safety

This branch does not implement Files, Sharing, Support/Emergency, Vault, Finance, Projects, Tasks, Calendar, Documents, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations. It does not enable FX30/FX34/FX35 workers and does not use production data/secrets/provider calls.

Last architecture note: Minimal API is the intended current API style for this branch.
