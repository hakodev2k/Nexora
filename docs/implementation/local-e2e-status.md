# Local Release 1 implementation status

Status: active code-only implementation on PR #4 (`impl/m01-s00-scaffold`) under DEC-20260909-014. This is not a merge, production or runtime-verification claim.

## Implemented in this revision

- SQL-backed Identity service composition: registration, hashed email-verification/reset tokens, local-safe delivery boundary (without token logging), login/logout, HttpOnly session cookie authority, reauthentication, profile `ETag`/`If-Match`, session revocation, PersonalSpace provisioning, module grants, audit/outbox/notification intent and one-time SuperAdmin bootstrap utility.
- SQL-backed module catalog/policy preview and commit with server-side SuperAdmin authorization, signed short-lived previews, dependency checks and policy audit.
- SQL-backed SuperAdmin account administration for operational user listing, role changes, action/module grants and account disablement. Last-active-SuperAdmin and session-revocation guards are transactional; business-resource payloads are never returned by these endpoints.
- SQL-backed Notification Center inbox with owner isolation, unread watermark, mark read/unread using `ETag`/`If-Match`, bounded bulk soft-delete, durable outbox publication and per-channel delivery projection. Local delivery records never call an external provider; Browser Push is explicitly `PermissionUnavailable` until a local subscription is configured.
- SQL-backed Trash provider for Project/Task deletion batches with owner-only listing, aggregate restore rules, explicit `PURGE` confirmation, history cleanup and append-only audit. Calendar events remain cancel-only and are not placed in Trash.
- Owner-scoped SQL Productivity slice for Projects, flat Tasks and personal Calendar Events with mandatory time windows, priorities/tags/checklist JSON, lifecycle transitions, task/project history, aggregate Trash membership, event completion/cancellation, validation, idempotency and rowversion concurrency.
- React local shell with register/verify-token input, login/logout, reset-token input, profile/settings/session screens, CSRF kept in memory, idempotency headers, error/loading/empty states and server-projection-based module navigation.
- React local shell also exposes the SQL-backed Notification inbox, Trash batch restore/purge controls and non-secret Theme preference editor with explicit loading/empty/error/conflict states.
- SQL-backed Documents/Notes/Knowledge page core: owner-scoped list/detail, explicit type/editor selection, bounded Markdown/Block body, immutable save versions, publish/unpublish/archive lifecycle and `ETag`/`If-Match` concurrency. The React shell exposes the same local page create/save/lifecycle flow without rendering unsanitized HTML.
- SQL-backed Finance manual-record slice: owner-unique categories plus nonnegative decimal amount, explicit currency, date-only occurrence and private note; same-currency summaries, category dependency protection, `ETag`/`If-Match`, idempotency and redacted audit. Advanced ledger/account/budget/bill/CSV/delete semantics remain out of scope.
- SELF authorization now resolves current module enablement and per-action grants from SQL on every protected feature request. Ordinary Users and SuperAdmins use the approved own-resource baseline; Admin SELF access requires an explicit resolved `Allow`, with matching `Deny` winning and legacy action aliases retained only for local migration compatibility.
- Reference Release 1 catalog and productivity/calendar schema migration. Formerly paused modules remain real-provider-disabled; only explicitly local/simulated/integration-safe code may be added under DEC-014.

## Deliberately not claimed

The remaining Release 1 modules (sharing/support/emergency, notification delivery workers/push subscriptions, files/import-export, Trash advanced retention, reminders/planner/goals/habits/time tracking/Pomodoro, document folders/tags/history/share/import-export, bookmarks/snippets/read-later/organization/search/dashboard, advanced Finance/Vault, News/shopping, developer/GitHub/monitoring, assets/career/learning and local-safe automation/integrations) still require their own contracted vertical slices and code. Productivity history and aggregate deletion are persisted; task-calendar projection and ICS import/export are not yet implemented. No placeholder or demo data is reported as complete.

Production deployment, public launch, real secrets/provider calls, real OAuth/payments, real-user imports and external destructive actions remain unapproved.

## Verification ownership

The current instruction is code-only. The agent ran only the frontend `npm run build` and the structural `python3 scripts/dev/verify-s00.py` check on the working tree; no .NET build was possible because the SDK is unavailable. Functional tests, SQL integration tests, E2E/browser tests, manual QA, fixture/mock-data preparation and runtime migration checks were not performed by the agent and remain owner work.
