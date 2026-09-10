# FX26-S01 — Dashboard attention widgets local slice

This document records the contract and implementation boundary for the first
Dashboard slice on PR #4. It is a read-only projection over existing SQL source
modules, not a new source of truth and not a claim that the complete Dashboard
module is implemented.

## Goal → requirement → acceptance → operation → code

| Goal / requirement | Acceptance boundary | Operation | Implementation |
| --- | --- | --- | --- |
| `NXG-FX26-G01`, `P03-DSH-001`, `P03-DSH-003`, `P03-DSH-007` | Owner sees bounded due/overdue Tasks, today's Calendar Events, recent Documents and unread Notifications using source filters and the owner's timezone boundary. | `getDashboard` | `IDashboardService.Get`, `SqlDashboardService`, `GET /api/v1/dashboard` |
| `NXG-FX26-G02`, `P03-DSH-002` | A disabled/missing source is `Unavailable` and a query failure is `Degraded`; other widgets remain present. | `getDashboard` | Per-widget capability checks and bounded SQL queries |
| `P03-DSH-004`, `FX-26-AC-002` | UI exposes independent loading/empty/unavailable/degraded states and accessible item lists without hidden source payloads. | `getDashboard` | React Home dashboard projection in `App.tsx` |

## API and response contract

`GET /api/v1/dashboard` requires the authenticated owner session and the
`FX26` module plus `dashboard.dashboard.read` capability. It returns a
`DashboardSnapshot` containing `timeZoneId`, `generatedAt` and four stable
widget IDs:

- `tasks-due`: active Tasks with `DueAt` before the end of the owner's local day;
  overdue items sort before today's items.
- `calendar-today`: scheduled/completed Events overlapping that local day.
- `documents-recent`: top five Draft/Published Documents by `UpdatedAt`.
- `notifications-unread`: top five unread, non-deleted Notifications.

Each widget contains `state` (`Ready`, `Empty`, `Unavailable` or `Degraded`),
`count`, a bounded `items` list, a safe message and its own refresh timestamp.
Only safe identity/title/status/time projections are returned. No body,
description, financial balance, Vault field, provider result or cross-owner
identifier is included.

## Security and failure boundary

- Every source query filters by `IdentityPrincipal.OwnerId`; notifications use
  the principal's `UserId` because that table is user-owned.
- Source capability and module checks run for every widget. Revocation or
  disablement produces `Unavailable` and never serves cached payload.
- SQL command timeout is bounded. A source SQL error becomes `Degraded`; a
  connection/bootstrap failure returns `PersistenceUnavailable` for the whole
  snapshot without guessing data.
- Dashboard is GET-only in this slice. Layout persistence, widget add/configure/
  remove/reorder, refresh mutation, quick-create and provider widgets remain
  contract-gated and are not exposed.
- No process, Redis or browser storage is used as authority. No external
  provider/network call is made.

## Evidence status

This is a code-only implementation update under the current execution amendment:
no new unit/integration/E2E tests, fixtures, mock/demo records or functional QA
claims were added. Static verifier, Python syntax, shell syntax, frontend build
and diff checks may be reported only when actually run; .NET/SQL/browser runtime
checks remain owner/CI dependencies when unavailable locally.
