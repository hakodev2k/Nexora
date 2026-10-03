# Draft PR description — PR #4 R2 remediation

> Unposted draft. Do not publish, approve or merge as part of this task.

## Scope and authority

This working-tree repair is on `impl/m01-s00-scaffold` for PR #4.
Normative base is `main` at
`8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; reviewed PR head is
`6d5b58dbac330b0d0d9aceb9e0b2f4695b423316`. The source was not reset,
rebased, merged or pushed. The only implementation approval used is
`DEC-20260909-001`: M01 S00–S11, the required backend/frontend scaffold and
local development scripts. PR-only `DEC-20260909-014` is not used.

The current execution is code-only under `AGENTS.md`: no new or executed
functional tests, fixtures, demo data, browser/SQL runtime, provider/OAuth
execution, production data/secrets or CI run is claimed.

## Implemented in this working tree

- M01 source authority: explicit Admin SELF Allow/no Deny, User/SuperAdmin own
  baseline, current account/PersonalSpace/module/dependency checks, and
  `RegistrationEnabled` limited to verification-time default grants.
- M01 idempotency/CSRF: keyed credential proofs, separately signed anonymous
  session binding with a stable restart key, bounded CSRF refresh, safe status/body replay where the
  result is allowlisted, and 204 on admin self-demotion without a privileged
  response.
- M01 account delivery: encrypted durable local message envelope, SQL-backed
  bounded lease/fencing worker, private local operator capture and
  `Nexora.Local read-account-messages`; raw verification/reset tokens are not
  in application logs or a public HTTP mailbox.
- M01 passwords: versioned ASP.NET Identity `PasswordHasher` with the approved
  PBKDF2-HMAC-SHA512 cost, plus bounded legacy verification/rehash on successful
  login/reauthentication.
- M01 UI: vi/en resources for auth/profile/settings/admin shell, persisted theme
  binding, stable logical mutation keys and one bounded CSRF retry.
- Current authority/traceability artifacts and historical-document labels.

## Migration ceiling

New migration `20260913_0026_identity_local_delivery.sql` adds nullable
authenticated delivery-envelope and lease columns/indexes to
`identity.AccountMessageIntent`. Existing migrations were not changed and no
migration was executed. The owner must apply/verify it only on the approved
synthetic local SQL environment. Rollback is restore/forward-only; do not edit
an applied migration or reset its journal.

## Evidence

Release builds for Infrastructure, Local, API, Bootstrap, UnitTests and
IntegrationTests compile successfully with zero warnings/errors; the two test
projects were compile-only and no tests ran. `npm run build` (`tsc -b` + Vite)
also exits 0. These results do not prove SQL constraints, concurrency, DST,
security, usability, browser behavior or runtime acceptance.

SQL Server/migration execution, browser/manual QA, functional tests, CI and
independent security/migration/background/architecture review remain
`Not run`/`Pending`. Post-M01 R2 findings for Productivity, Calendar, Reminders,
Files, Documents, Sharing and Discovery remain explicitly scope-blocked under
main and are listed with repair cases in the remediation report.

See [`pr4-r2-remediation-report-20260913.md`](pr4-r2-remediation-report-20260913.md)
for the R2-01..24 and F01..F22 disposition, human QA handoff, exact commands
and residual gates.
