# Local Release 1 implementation status

Status: active code-only implementation on PR #4 (`impl/m01-s00-scaffold`) under DEC-20260909-014. Current head: `e42516e2360ad2afd7d8a0144412a15086770983`; `d2c7c5e00c05714b9d7a54a09a38b64f0e20e33d` is the earlier blocker-remediation commit. This is not a merge, production or runtime-verification claim.

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
- SQL-backed Bookmarks manual-metadata slice: owner-scoped HTTP(S) URL/title/description records with canonical URL and inert health/status projection; list/search, create/edit and Active/Archived lifecycle use `ETag`/`If-Match`, idempotency, per-action grants and redacted audit. No URL fetch/navigation, tags, collections, sharing or Trash behavior is enabled.
- SQL-backed Snippets text/version slice: owner-scoped title/language/source/description with append-only versions; list/search, create/save-as-version, Active/Archived lifecycle and explicit clipboard copy use `ETag`/`If-Match`, idempotency, per-action grants and redacted audit. Source is escaped data and never executed or sent to a provider; history/diff/restore, export, tags, sharing and Trash remain gated.
- SQL-backed Read Later Bookmark-reference slice: owner-scoped queue entries with safe title/URL snapshots, `Unread`/`Reading`/`Read` state, explicit position metadata, source-availability projection, ETag/If-Match, idempotency and redacted audit. The React shell exposes `/read-later` with Bookmark picker, state filter, state/position updates and queue removal; it never copies body or follows URLs. News/body reader, cross-module read-state, search/tags, sharing and advanced lifecycle remain gated.
- SQL-backed FX24 tag catalog slice: owner-scoped namespace tags (`projects`, `documents`, `bookmarks`, `snippets`) with bounded search, optional validated color, usage-count projection, ETag/If-Match rename, idempotent create/rename/remove and delete protection when a future `ResourceTag` reference exists. The React shell exposes `/organize/tags`; assignment, Collections, Templates, sharing and provider behavior remain gated.
- Local FX32 Developer Toolbox pure slice: SQL-gated catalog plus bounded in-memory Base64, URL, HTML entity, hash, UUID, password, JSON and regex operations. The React shell exposes `/developer/tools`; no input/output persistence, code execution, clipboard auto-read, network calls, history or provider behavior is enabled.
- SQL-backed FX16 numeric Goals slice: owner-scoped Goal/GoalTarget/GoalProgress tables, bounded Goal CRUD, numeric progress events and explicit Draft/Active/Completed/Abandoned transitions. The React shell exposes `/goals`; task-linked/boolean targets, archive/trash/history, reminders and provider behavior remain gated.
- Productivity contract-alignment continuation: Projects enforce the Title compatibility mapping, required bounded Description, A–Z list order, bounds confirmation and terminal read-only rules; Tasks enforce Title/Project/lifecycle bounds, expose server-derived Overdue, and maintain a one-way Task → Calendar projection keyed by owner/task; Calendar exposes Day default plus Week/Month/Agenda selectors and rejects direct mutation of Task projections. Migrations `20260910_0019_productivity_contract_alignment.sql` and `20260910_0020_task_calendar_projection.sql` are required for this source slice.
- SQL-backed FX26-S01 Dashboard attention slice: owner-scoped due/overdue Tasks, today's Calendar Events, recent Draft/Published Documents and unread Notifications are projected through independent Ready/Empty/Unavailable/Degraded widgets. The React Home dashboard consumes `GET /api/v1/dashboard`; layout persistence, widget mutation, quick-create and provider widgets remain gated.
- SQL-backed FX25-S01 Global Search slice: bounded owner-scoped source queries across Projects, Tasks, Calendar Events, Documents, Bookmarks, Snippets and Goals with type/date/archive filters, deterministic ranking, safe previews and per-source capability/degraded states. The React shell exposes `/search`; saved searches, Recents, Command Palette and persisted index remain gated.
- SQL-backed FX25-S03 Favorites slice: owner-scoped typed Project/Task/Event/Document/Bookmark/Snippet/Goal references with source capability/lifecycle/Trash recheck, safe unavailable projection, cursor pagination, bounded rank, ETag/If-Match, durable safe-response idempotency and audit. The React shell exposes `/favorites`; no source payload snapshot or authority is copied.
- SELF authorization now resolves current module enablement from SQL on every protected feature request. Users, Admins and SuperAdmins retain the approved own-resource baseline; `AdminPermission` is reserved for administrative, cross-user and support operations and does not remove self-service access. Matching deny semantics remain applicable to those administrative paths.
- The documented Release 1 catalog is broader than this local runtime. Forward-only migration `20260910_0018_local_runtime_catalog_gate.sql` keeps every catalog row for traceability, marks only implemented local slices `Ready + SystemEnabled + RegistrationEnabled`, disables existing grants for gated modules, and leaves FX30/FX34/FX35 `Paused` with no provider execution.

## Catalog versus runtime availability

These labels are intentionally separate. A catalog row is not runtime evidence,
and a source implementation is not SQL/runtime verification.

| Classification | Modules | Meaning in this revision |
| --- | --- | --- |
| Documented R1 catalog | FX01–FX40 | The product catalog remains represented in SQL for policy/dependency traceability. |
| Locally implemented | FX01, FX02, FX03, FX06, FX08, FX09, FX11, FX12, FX13, FX16, FX20, FX21, FX22, FX23, FX24, FX25, FX26, FX27, FX32 | Source, SQL, authorization and local UI slices exist; this code-only run does not claim SQL runtime verification. |
| Runtime-available after the catalog gate | The locally implemented list above | Effective SQL state is `Ready`, `SystemEnabled=1`, `RegistrationEnabled=1`; new verified users receive grants only from this set. |
| Deliberately unimplemented | FX04, FX05, FX07, FX10, FX14, FX15, FX17, FX18, FX19, FX28, FX29, FX31, FX33, FX36, FX37, FX38, FX39, FX40 | Effective state is `Blocked`, system disabled and registration disabled; navigation exposes no usable module. |
| Provider/production gated | FX30, FX34, FX35 | Effective state is `Paused`, system disabled and registration disabled. No real provider, production, secret or paid-service execution is enabled. |

`/health/live` checks only process liveness. `/health/ready` returns `503`
unless SQL opens, the migration journal exists, every required migration through
`0020` is applied, and the bootstrap/security invariant is valid. Its response
contains only coarse dependency states. The API also fails fast unless
`NEXORA_IDEMPOTENCY_SECRET` is supplied separately from the SQL connection
string/password.

## Deliberately not claimed

The remaining Release 1 modules (sharing/support/emergency, notification delivery workers/push subscriptions, files/import-export, Trash advanced retention, reminders/planner/habits/time tracking/Pomodoro, advanced Goals targets/archive/trash/history, document folders/tags/history/share/import-export, bookmark tags/collections/refresh/sharing, snippet history/diff/restore/export/tags, Read Later News/body reader/organization/search, FX24 Collections/Templates, dashboard layout/widget mutation and quick-create, Search saved/recent/command/persisted-index actions, advanced Finance/Vault, News/shopping, Developer Toolbox advanced/history/network tools, GitHub/monitoring, assets/career/learning and local-safe automation/integrations) still require their own contracted vertical slices and code. Productivity history and aggregate deletion are persisted; Task → Calendar source projection is source-covered but SQL/runtime evidence is `Not run`; ICS import/export remains unimplemented. No placeholder or demo data is reported as complete.

Production deployment, public launch, real secrets/provider calls, real OAuth/payments, real-user imports and external destructive actions remain unapproved.

## Verification ownership

The current instruction remains code-only: no new tests, fixtures, demo records
or provider/runtime data were added. The following commands were actually run
on the current source; the matching CI workflow ran on
`e42516e2360ad2afd7d8a0144412a15086770983`:

- `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release` — **Pass**, 0 warnings, 0 errors (local run; restore required elevated local NuGet-config access; CI repeated it).
- `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release` — **Pass**, 0 warnings, 0 errors (local run; CI repeated it).
- `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release` — **Pass in CI run 147** as the existing unit-check step; no new tests were added and the suite does not prove SQL behavior.
- `npm ci --prefix web/Nexora.Web` — **Pass in CI run 147**.
- `npm run build --prefix web/Nexora.Web` — **Pass**, TypeScript and Vite production bundle completed locally and in CI run 147.
- `git diff --check` — **Pass**.
- `bash scripts/dev/verify.sh` — **Not run**: the Windows environment denied WSL/Bash instance creation (`E_ACCESSDENIED`) before the script executed.

The current workflow `34507159651` / run `147` is **Pass** (`success`) for
`Nexora local implementation checks`; Agent baseline `34507159640` / run `164`
is also **Pass**. `bash scripts/dev/verify.sh` ran in CI and passed; the local
Windows WSL invocation remains **Not run** because the host denied WSL/Bash
creation (`E_ACCESSDENIED`). SQL integration was **Not run** because
`NEXORA_TEST_SQL_CONNECTION` is absent; no SQL migration execution, health
endpoint, browser/E2E or manual QA runtime evidence is claimed. The manual flow
is documented in `pr4-review-qa-script.md` for the human owner.

