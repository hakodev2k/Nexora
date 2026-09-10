# PR #4 owner-run manual QA script

This is a manual runtime checklist for the human owner. It is documented for
the code-only agent run and is not test evidence. Use only synthetic local
accounts/data and a loopback Development SQL Server. Do not use production
credentials, provider credentials, real user data or external endpoints.

## Login CSRF rotation

1. `GET /api/v1/auth/csrf`; retain the response token and confirm the CSRF
   cookie is present.
2. `POST /api/v1/auth/login` with a fresh UUID `Idempotency-Key` and the token
   from step 1. Confirm the successful response includes `X-CSRF-Token` and
   that its value differs from the pre-login token.
3. Send `PATCH /api/v1/me` with the response `X-CSRF-Token`, a fresh UUID
   `Idempotency-Key`, and the current profile `ETag` in `If-Match`. Expected:
   success or an ordinary validation/concurrency response, never `CsrfInvalid`.
4. Send logout, then session revoke/revoke-all where applicable, using the
   latest response token and a fresh UUID idempotency key. Expected: no CSRF
   failure caused by the login rotation.

## Developer Toolbox idempotency

1. Call `POST /api/v1/developer/tools/run` without `Idempotency-Key` while
   retaining a valid CSRF token. Expected: `422` with
   `IdempotencyKeyRequired`; the global CSRF/idempotency filter must remain
   enabled.
2. Repeat with a valid UUID `Idempotency-Key` and a pure local tool such as
   `uuid` or `base64`. Expected: success, no network/provider call, and no
   persisted input/output record.
3. Repeat the same request with the same key. Expected: the documented
   idempotency replay/conflict response, not a second execution.

## MFA reset fail-closed

With a synthetic account whose SQL `[identity].[MfaCredential].[State]` is
`Enabled`, request a reset and submit the reset token. Expected:
`MfaRecoveryRequired`; password hash, security stamp, token consumption and
session state remain unchanged. Reset requests for unknown email addresses must
retain the generic anti-enumeration accepted response.

## Module/readiness checks

After applying all migrations, inspect the SQL module projection and profile
projection. Only the locally implemented module list in
`local-e2e-status.md` should be usable; blocked/paused modules must not expose
an “Open module” action. Stop SQL or remove a required migration in a disposable
environment and confirm `/health/ready` returns `503` without secrets or stack
traces. `/health/live` should remain a process-liveness response.

Owner execution status for this code-only run: **Not run**.
