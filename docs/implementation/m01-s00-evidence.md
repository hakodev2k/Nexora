# M01 implementation evidence note — PR #4

Revision scope: `impl/m01-s00-scaffold`.

Status: `SLICE_IMPLEMENTATION_STARTED`, not `SLICE_VERIFIED_LOCALLY`.

## Authorization and scope

- Product Owner request: implement Nexora according to current docs and continue by dependency.
- Current repository authority: `DEC-20260909-001` approves M01 stories S00-S11 plus backend/frontend scaffold and local scripts.
- This branch currently implements M01 local scaffold, the first identity API runtime surface, domain/application policy code, unit-test harness, and a SQL migration artifact.
- It does not implement full M01, business modules, paused modules, production deployment, provider calls, production secrets or production data.

## Current implemented code artifacts

- M01 local scaffold and deterministic scripts.
- ASP.NET Core API runtime surface under `/api/v1` for S02-S06 identity flows.
- React/Vite local identity shell that calls the API surface.
- Domain/application policy code used by identity, access, module, idempotency and profile rules.
- Unit-test harness and tests for policy/runtime service behavior.
- SQL Server migration artifact for the M01 identity/platform/security/operations/notifications tables.

## API routes currently mapped

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

## Goal and requirement trace

| Goal/source | Bound implementation |
| --- | --- |
| M01-S00 / M01-AC00 | Pinned toolchain files, reproducible local scripts, Docker Compose local SQL/Redis profile, application CI scaffold. |
| M01-S02/S03/S04/S05/S06 API surface | Runtime routes for registration, verification, resend, login/logout/reauth, password reset, profile and sessions. |
| M01-S01/S08/S09 policy foundation | Last-SuperAdmin, action grant and module dependency/paused policies. |
| M01 data contract | SQL Server migration artifact for identity/platform/security/operations/notifications tables. |
| Effective action status overlay | Only M01-approved identity/control/session/profile action routes are exposed; no non-M01 action handlers are added. |

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

The current runtime store is a development in-memory implementation used to expose and exercise the M01 API surface. SQL-backed persistence, transaction locking, idempotency receipts, audit/outbox integration and true integration evidence still need follow-up implementation before M01 can be called verified.

## Scope safety

This branch does not implement Files, Sharing, Support/Emergency, Vault, Finance, Projects, Tasks, Calendar, Documents, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations. It does not enable FX30/FX34/FX35 workers and does not use production data/secrets/provider calls.
