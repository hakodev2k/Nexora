# SQL-backed identity slice

Status: implementation on PR #4 under `DEC-20260909-014` (local/synthetic
environment only). This note records the boundary for the identity vertical
slice; it is not a production-readiness claim.

## Contract binding

| Goal / requirement | API operation | Persistence/evidence boundary |
| --- | --- | --- |
| `SYS-SEC-001`, `SYS-SEC-002` · `AUTH-001..010` | `register`, `verify`, `resendVerification`, `login`, `logout`, `requestReset`, `confirmReset` | `identity.User`, `UserRole`, `OneTimeToken`, `AccountMessageIntent`, `operations.Outbox`, `security.AuditEvent` |
| `P01-PDS-001`, `P01-USR-001..005` | `getMe`, `updateMe` | `platform.PersonalSpace`, SQL `rowversion` and `If-Match` compare-and-swap |
| `P01-AUT-001..012` | `listSessions`, `revokeSession`, `revokeAll`, `reauth` | SQL session handle digest, security-stamp invalidation and session ownership predicate |
| local operator bootstrap | `BootstrapSuperAdmin` application use case | `platform.SecurityInvariant` singleton lock, active-SuperAdmin count, role + PersonalSpace transaction |

## Security and consistency rules

- SQL Server is authoritative; the old `DevelopmentIdentityStore` is not
  registered by the final composition root.
- Passwords, session handles and one-time tokens are stored only as hashes.
  A raw account token is passed only to an explicitly registered local-safe
  `IAccountMessageSink` after commit; the SQL outbox and account-message intent
  never contain the raw token.
- Verification/reset consume tokens under a serializable transaction with row
  locks. Verification creates at most one `PersonalSpace` and grants only
  system-enabled, ready, registration-enabled modules.
- Every authenticated query resolves `OwnerId` from `platform.PersonalSpace`
  joined to the server-validated session. No request body/header can choose the
  owner. Session and profile mutations include the authenticated user predicate.
- Profile writes require an exact quoted eight-byte `If-Match` value. Password
  reset rotates the security stamp and revokes all sessions in the same SQL
  transaction, then records an in-app security notification and durable outbox
  event.
- Generic reset/resend/duplicate-registration responses do not disclose whether
  an email is registered. Authentication failures use a fixed-cost dummy hash
  path for missing users.

## Local-provider boundary

Real email/provider execution is disabled. The implementation emits durable
intent/outbox records and may be composed with a local synthetic sink for manual
verification during local E2E. Any mailbox-reading endpoint must be separately
authenticated and operator-audited; no unauthenticated development mailbox is
part of this slice.

## Verification status

The current code-only instruction assigns functional testing, manual QA,
fixture/mock-data design and runtime verification to the human owner. The agent
did not add or run unit, integration or browser/E2E tests. This environment has
no .NET SDK, SQL Server, Docker or `sqlcmd`, so .NET build, migration execution
and SQL runtime checks are **Not run**. The frontend build and structural static
boundary check are recorded in `docs/implementation/local-e2e-status.md`.
