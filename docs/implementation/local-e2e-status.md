# Local Release 1 implementation status

Status: active code-only implementation on PR #4 (`impl/m01-s00-scaffold`) under DEC-20260909-014. The earlier snapshot was represented by code commit `e42516e2360ad2afd7d8a0144412a15086770983` and CI/evidence revision `a3a0a7b84a124a8a9063d35d65641119e2824718` (workflow run `149`). The 2026-09-11 continuation adds source code for FX04/FX05/FX07; source implementation commit is `6fb229ab7d56b6ee7091b7e85ea028c47c1a893b`. This is not a merge, production or runtime-verification claim.

## 2026-09-11 continuation update

This code-only continuation implements bounded local source slices for the
Sharing Engine, Support consent/session shell and private Files/Attachments
service. It adds the forward-only migration
`20260911_0021_core_sharing_support_files.sql`, registers the services and
routes, adds server-derived UI screens, and updates the SQL capability/readiness
gates. It does not add tests, fixtures, demo data, provider calls or production
configuration.

The current FX14 batch adds forward-only migration
`20260911_0022_reminders_scheduling.sql`, owner-scoped Task/manual Calendar
Event reminder configuration, SQL intent/audit records, source-reconciliation
triggers, a local dispatcher shell and the `/modules/FX14` screen. The
dispatcher produces only a local InApp notification projection; Email and
BrowserPush are recorded as unavailable/permission-limited and no external
provider is called.

The current FX15/FX17 batch adds forward-only migration
`20260911_0023_planner_habits.sql`. FX15 persists owner-scoped Task pins by
local plan date and rank; it validates active same-owner Task/Project sources
and never alters the Task, its Calendar projection or reminder. FX17 persists
owner-scoped Habits, non-overlapping effective-dated schedules and one check-in
per Habit/local date. Its check-in and streak evaluation use the saved Habit
timezone; local reminder time is metadata only and no delivery/provider work is
enqueued. Both modules expose typed API and React screens with ETag, idempotency
and audit for their lifecycle-sensitive mutations.

Important availability distinction: FX04, FX05 and FX07 have source coverage,
but the existing local catalog gate `20260910_0018_local_runtime_catalog_gate.sql`
still leaves them `Blocked`, system-disabled and registration-disabled. The
source is therefore not claimed as runtime-available: FX05 Emergency remains
`DecisionBlocked` pending Q-02, FX07 still lacks the approved cover/replacement/
cleanup contract, and all three lack SQL/browser isolation evidence. Q-02
emergency access is still an explicit `DecisionBlocked` response until duration
and recent-auth contracts are approved.

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
- SQL-backed FX14 local reminder slice: one owner-scoped current configuration per active Task or manual Calendar Event, `None`/`BeforeStart15m`/future `Exact` validation, ETag/If-Match plus idempotency, source-revision and lifecycle recheck, durable schedule/invalidation outbox rows and audit. `ReminderDispatchWorker` creates a deduplicated local inbox projection only; it never executes email/push providers.
- SQL-backed FX15 Planner slice: owner-scoped `PlannerPin` records reference existing active Tasks by local plan date only. Day/Week planning, pin/unpin, move, keyboard-accessible reorder and notes use source capability/lifecycle rechecks, ETag/If-Match, idempotency and audit. Planner never changes Task status/times/reminders, duplicates a Task, creates an Event, auto-carries work, or exposes a separate share projection.
- SQL-backed FX17 Habits slice: owner-scoped Boolean/Count Habits with IANA timezone, effective-dated weekday/target schedules, Active/Paused/Archived lifecycle, and unique local-date check-ins. Initial schedules may begin on the current local date; later schedule changes begin after it and preserve previous schedule rows. Streaks ignore unscheduled/paused dates and stop at missed scheduled target days. Local reminder-time metadata is stored without FX14/provider dispatch; no social/team, Calendar, Trash/purge or external behavior is enabled.
- Productivity contract-alignment continuation: Projects enforce the Title compatibility mapping, required bounded Description, A–Z list order, bounds confirmation and terminal read-only rules; Tasks enforce Title/Project/lifecycle bounds, expose server-derived Overdue, and maintain a one-way Task → Calendar projection keyed by owner/task; Calendar exposes Day default plus Week/Month/Agenda selectors and rejects direct mutation of Task projections. Migrations `20260910_0019_productivity_contract_alignment.sql` and `20260910_0020_task_calendar_projection.sql` are required for this source slice.
- SQL-backed FX26-S01 Dashboard attention slice: owner-scoped due/overdue Tasks, today's Calendar Events, recent Draft/Published Documents and unread Notifications are projected through independent Ready/Empty/Unavailable/Degraded widgets. The React Home dashboard consumes `GET /api/v1/dashboard`; layout persistence, widget mutation, quick-create and provider widgets remain gated.
- SQL-backed FX25-S01 Global Search slice: bounded owner-scoped source queries across Projects, Tasks, Calendar Events, Documents, Bookmarks, Snippets and Goals with type/date/archive filters, deterministic ranking, safe previews and per-source capability/degraded states. The React shell exposes `/search`; saved searches, Recents, Command Palette and persisted index remain gated.
- SQL-backed FX25-S03 Favorites slice: owner-scoped typed Project/Task/Event/Document/Bookmark/Snippet/Goal references with source capability/lifecycle/Trash recheck, safe unavailable projection, cursor pagination, bounded rank, ETag/If-Match, durable safe-response idempotency and audit. The React shell exposes `/favorites`; no source payload snapshot or authority is copied.
- SELF authorization now resolves current module enablement from SQL on every protected feature request. Users, Admins and SuperAdmins retain the approved own-resource baseline; `AdminPermission` is reserved for administrative, cross-user and support operations and does not remove self-service access. Matching deny semantics remain applicable to those administrative paths.
- The documented Release 1 catalog is broader than this local runtime. Forward-only migration `20260910_0018_local_runtime_catalog_gate.sql` keeps every catalog row for traceability, marks only approved locally available slices `Ready + SystemEnabled + RegistrationEnabled`, disables existing grants for gated modules, and leaves FX04/FX05/FX07 fail-closed alongside the explicitly incomplete modules; FX30/FX34/FX35 remain `Paused` with no provider execution.

## Catalog versus runtime availability

These labels are intentionally separate. A catalog row is not runtime evidence,
and a source implementation is not SQL/runtime verification.

| Classification | Modules | Meaning in this revision |
| --- | --- | --- |
| Documented R1 catalog | FX01–FX40 | The product catalog remains represented in SQL for policy/dependency traceability. |
| Locally implemented | FX01, FX02, FX03, FX04, FX05, FX06, FX07, FX08, FX09, FX11, FX12, FX13, FX14, FX15, FX16, FX17, FX20, FX21, FX22, FX23, FX24, FX25, FX26, FX27, FX32 | Source, SQL, authorization and local UI slices exist; this code-only run does not claim SQL runtime verification. FX04/05/07 are bounded source slices and remain catalog-gated because their approved runtime contract/evidence is incomplete. |
| Runtime-available after the catalog gate | The locally implemented list above excluding FX04, FX05 and FX07 | Effective SQL state is `Ready`, `SystemEnabled=1`, `RegistrationEnabled=1`; migration `0023` promotes FX15 and FX17 (and preserves FX14) for the local runtime only. |
| Source implemented but catalog-gated | FX04, FX05, FX07 | Source/route/UI coverage exists, but effective state remains `Blocked`, system-disabled and registration-disabled; navigation intentionally exposes no usable module until an approved gate change. |
| Deliberately unimplemented | FX10, FX18, FX19, FX28, FX29, FX31, FX33, FX36, FX37, FX38, FX39, FX40 | Effective state is `Blocked`, system disabled and registration disabled; no source slice is claimed. FX10 remains blocked by the Vault/key and backup/RPO/RTO decision gates. |
| Provider/production gated | FX30, FX34, FX35 | Effective state is `Paused`, system disabled and registration disabled. No real provider, production, secret or paid-service execution is enabled. |

`/health/live` checks only process liveness. `/health/ready` returns `503`
unless SQL opens, the migration journal exists, every required migration through
`0023` is applied, and the bootstrap/security invariant is valid. Its response
contains only coarse dependency states. The API also fails fast unless
`NEXORA_IDEMPOTENCY_SECRET` is supplied separately from the SQL connection
string/password.

## Historical status snapshot superseded by the continuation

The single-line inventory below is retained as historical context from before
the 2026-09-11 FX04/FX05/FX07 source slices. Use the catalog/runtime table above
and `pr4-continuation-handoff-20260911.md` for the current status. The remaining
gaps are support business-data reads, emergency execution, file cover limits /
replacement / cleanup worker, notification delivery workers and the other
unimplemented modules listed in the matrix.

The remaining Release 1 gaps (support business-data reads/emergency execution, notification delivery workers/push subscriptions, file cover limits/replacement/cleanup, import-export, Trash advanced retention, reminders provider/DST/restart evidence, Planner source-lifecycle race evidence, Habits DST/SQL replay/Trash-reminder integration, time tracking/Pomodoro, advanced Goals targets/archive/trash/history, document folders/tags/history/share/import-export, bookmark tags/collections/refresh/sharing, snippet history/diff/restore/export/tags, Read Later News/body reader/organization/search, FX24 Collections/Templates, dashboard layout/widget mutation and quick-create, Search saved/recent/command/persisted-index actions, advanced Finance/Vault, News/shopping, Developer Toolbox advanced/history/network tools, GitHub/monitoring, assets/career/learning and local-safe automation/integrations) still require their own contracted vertical slices and code. Productivity history and aggregate deletion are persisted; Task → Calendar source projection is source-covered but SQL/runtime evidence is `Not run`; ICS import/export remains unimplemented. No placeholder or demo data is reported as complete.

Production deployment, public launch, real secrets/provider calls, real OAuth/payments, real-user imports and external destructive actions remain unapproved.

## Verification ownership

The current instruction remains code-only: no new tests, fixtures, demo records
or provider/runtime data were added. Earlier CI/build claims below are
historical evidence for the earlier source snapshot. Current-continuation
evidence consists only of commands that actually ran in this environment:

- `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release` — **Blocked** on the current source: the environment has no `dotnet` executable (`/bin/bash: dotnet: command not found`).
- `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release` — **Blocked** on the current source for the same missing-tool reason.
- `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release` — **Blocked** for the same missing-tool reason.
- `npm ci --prefix web/Nexora.Web --ignore-scripts` — **Pass** for the current continuation.
- `npm run build --prefix web/Nexora.Web` — **Pass**: TypeScript and Vite production bundle completed for the current continuation.
- `python3 .ai/scripts/verify-baseline.py` — **Pass**: baseline package and 15 gate tests; application tests were not run.
- `python3 scripts/dev/verify-s00.py` — **Pass**: source, migration-runner and
  no-bypass structural checks.
- `git diff --check` — **Pass** for the current continuation.

Historical snapshot evidence:

- `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release` — **Pass**, 0 warnings, 0 errors (local run; restore required elevated local NuGet-config access; CI repeated it).
- `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release` — **Pass**, 0 warnings, 0 errors (local run; CI repeated it).
- `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release` — **Pass in CI run 149** as the existing unit-check step; no new tests were added and the suite does not prove SQL behavior.
- `npm ci --prefix web/Nexora.Web` — **Pass in CI run 149**.
- `npm run build --prefix web/Nexora.Web` — **Pass**, TypeScript and Vite production bundle completed locally and in CI run 149.
- `git diff --check` — **Pass**.
- `bash scripts/dev/verify.sh` — **Not run**: the Windows environment denied WSL/Bash instance creation (`E_ACCESSDENIED`) before the script executed.

The prior workflow `34508111597` / run `149` is **Pass** (`success`) for the
earlier source snapshot; it does not verify this continuation. The current
environment has no SQL runtime/connection, so SQL migration execution, readiness,
storage/scan, sharing/support authorization, browser/E2E, manual QA and
independent security review are **Not run**. `bash scripts/dev/verify.sh`,
`scripts/dev/migrate.*` and application unit tests were not rerun in this
environment. The manual flow is
documented in `pr4-review-qa-script.md` for the human owner.

