# PR #4 review-fix task brief — 2026-09-13

> **Superseded historical brief.** This file records an earlier working-tree
> review and is not current evidence. Its `fcb1f75...` snapshot and
> `DEC-20260909-014` scope claim must not be used as authority. The current
> authority manifest, R2 disposition and verification log are in
> [`pr4-r2-remediation-report-20260913.md`](pr4-r2-remediation-report-20260913.md)
> and [`pr4-r2-authority-manifest-20260913.md`](pr4-r2-authority-manifest-20260913.md),
> pinned to main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` and head
> `6d5b58dbac330b0d0d9aceb9e0b2f4695b423316`.

## Task brief

| Field | Current value |
| --- | --- |
| Requested outcome | Revalidate the supplied F01–F22 review input against the current source and implement the smallest safe local fixes on PR #4. |
| Repository / PR | `hakodev2k/Nexora`, PR #4, branch `impl/m01-s00-scaffold`; do not merge or push to `main`. |
| HEAD / review baseline | `HEAD=fcb1f75fd8dccfa0ba1f15b49569e04c694569ce`; this equals review head baseline `fcb1f75fd8dccfa0ba1f15b49569e04c694569ce`; review base `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Changes below are uncommitted working-tree changes on that HEAD. |
| Authorization | User-requested code repair in the existing local PR; Product Owner decision `DEC-20260909-014` permits slice-by-slice local E2E implementation with sufficient contracts. No production, provider/OAuth worker, real secret/data, paid service, external destructive operation or merge authorization was inferred. |
| Review input | The requested `Nexora-PR4-review-vi.md` was searched for under the repository and `D:\Projects`; it is absent. The supplied F01–F22 list and the closest repository handoff/QA notes were used as review input only; current requirements and PO decisions remain authoritative. |
| Amendment | Code-only. No new, changed or executed unit/integration/E2E/browser tests, fixtures, mock/demo records or functional runtime evidence. Human owner owns SQL, browser, data and functional verification. |
| Account model | `OwnerId` is `PersonalSpaceId`; SQL Server is authoritative; Redis is rebuildable cache only. All source/projection/replay/worker paths remain owner and current-authority scoped. |
| Independent review | Pending. Security, migration, background-job and cross-module changes require an independent reviewer; self-review and build output do not satisfy that gate. |

## Authority, goals and routed rules

Requirements read for the affected slices include `docs/README.md`,
`docs/delivery/README.md`, `docs/delivery/01-current-scope.md`,
`docs/requirements/12-owner-decisions-local-e2e-implementation.md`, the M01
API contract, the applicable feature/action/UX/database/architecture contracts,
`docs/goals/README.md`, `docs/goals/01-system-goals.md` and the relevant module
goal files. The primary goal bindings used below are:

- F01/F03/F08/F21: `NXG-FX07-G01..G03`, `NXG-FX08-G01..G03`, plus `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-09`.
- F02/F04/F05/F06/F13/F14/F17/F22: `NXG-FX09-G01..G03`, `NXG-FX11-G01..G03`, `NXG-FX12-G01..G03`, `NXG-FX13-G01..G03`, `NXG-FX14-G01..G03`, plus `NXG-SYS-01`, `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-04`, `NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-09`, `NXG-SYS-10`, `NXG-SYS-11`.
- F07/F12/F15/F16: `NXG-FX09-G01..G03`, `NXG-FX20-G01..G03`, `NXG-FX25-G01..G03`, plus `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-08`, `NXG-SYS-11`, `NXG-SYS-14`.
- F09/F10/F11/F19/F20: `NXG-FX01-G01..G03`, `NXG-FX06-G01..G03`, plus `NXG-SYS-01`, `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-14`, `NXG-SYS-16`.
- F18: `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-04`, `NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-14`.

Routed rules/skills actually read before affected work:

- `AGENTS.md`, `.agents/skills/nexora-engineering/SKILL.md`, `.ai/profiles/nexora-implementation-agent.md`, `.ai/roles/technical-lead/README.md`, `.ai/roles/technical-lead/rules/core-rules.md`, `.ai/verification.md`.
- `.ai/rules/dotnet-backend-developer/{authorization-rules,background-processing-rules,migration-safety-rules,security-rules}.md` and `.ai/rules/privacy-engineer/privacy-access-control-rules.md`.
- `.ai/rules/react-developer/{accessibility-rules,browser-security-rules,form-validation-rules}.md`.
- `.ai/rules/security-engineer/{identity-access-rules,secrets-management-rules,threat-modeling-rules}.md`.
- `.ai/rules/software-architect/{data-ownership-rules,module-boundary-rules,transaction-consistency-rules}.md`.
- Routed backend/database/security/frontend skills under `.ai/skills/`, including authentication/authorization, background/idempotency/input validation, migration/concurrency, modular boundaries, frontend security/accessibility and security review guidance.

## Bounded implementation batches

| Batch | Scope and affected layers | Gate/rollback boundary |
| --- | --- | --- |
| B1 | F01/F02/F03/F06/F08: source-action lifecycle dispatch, Task-owned Calendar projection guards, purge participants, account/space capability checks, upload owner/handle-first I/O. Backend, SQL and additive migration. | FX07 remains gated. No migration was run. SQL transaction rollback is the business rollback; schema recovery is forward-only. |
| B2 | F04/F05/F22: one reminder policy/canonical row, embedded Task reconciliation, Task history, durable claim/lease/retry and local-only notification projection. | Real Email/BrowserPush/provider effects remain disabled. Human must verify SQL interleavings and restart behavior. |
| B3 | F07/F10/F11/F12: draft/base/server conflict handling, dirty-leave modal, stable UI intent keys, bounded CSRF refresh, per-endpoint JSON body ceilings. | Browser/runtime and lost-response checks remain pending; credentials/session/reauth responses are not replayed. |
| B4 | F09/F13/F14/F15/F16/F17: local identity delivery boundary remains unavailable without an approved transport contract; nullable Task priority; IANA/all-day conversion; typed resource links plus safe internal return; theme shell; bounded range/cursor improvements. | F09 is blocked by the missing delivery contract. F16 remains partial where the full locale catalog is absent; search continuation remains bounded by its existing slice contract. |
| B5 | F18/F19/F20/F21: source-participant orchestration, audit actor/owner semantics, current matrix snapshot, durable exact-path file cleanup. | Independent boundary/security/migration review and SQL/storage fault injection remain pending. |

## Migration and recovery impact

`database/migrations/20260913_0025_review_hardening.sql` is a new forward-only
migration and was not executed. It makes Task priority nullable without
rewriting existing `P3` values, adds reminder dispatch lease/retry columns and
indexing, adds upload-attempt ownership columns, and creates the durable
owner-scoped `files.StorageCleanup` table. It is intentionally not a down
migration and no existing migration was edited.

Deployment recovery must first take the normal SQL backup/schema-journal
checkpoint, apply the migration through the repository migration owner only in
an authorized disposable/local environment, and verify constraints/indexes and
the migration checksum. If application rollback is needed, leave the additive
schema in place and deploy the prior application only after it is confirmed to
ignore the new nullable/additive columns; if a schema rollback is required,
use an approved forward compensating migration after backup/impact review.
Do not restore over live data or run destructive SQL as part of this task.
Existing `Task.Priority` values retain their meaning; no blanket historical
audit backfill or file deletion was performed.

## Revalidated finding register and human QA handoff

Status vocabulary is intentionally split: `Fixed in source` means the bounded
implementation is present; `Build verified` is only compile/typecheck/static
evidence; `Runtime pending` means SQL/browser/functional proof is still owned by
the human; `Partial` means a material contract/catalog remainder is still in
source; `Blocked by contract` means the missing product/security transport
contract prevents safe implementation.

| ID | Requirement / action / AC binding | Status | Files and source change | Build/static evidence | Human QA: action/input → expected result | Remaining gate |
| --- | --- | --- | --- | --- | --- | --- |
| F01 | `docs/features/08-trash-activity-and-audit.md` `FX-08-BR-001..006`, `FX-08-AC-001..003`; `FX08-S01/S02`; `lifecycle.resource.restore`, `lifecycle.resource.purge`; source `projects.project.restore/purge`, `tasks.task.restore/purge`. | Fixed in source; Build verified; Runtime pending | `SqlTrashService.cs`: source action/lifecycle evaluation is transactional; unsupported/mixed batches fail closed. | Backend build 0 errors; source review. | Disable source module or deny one source action; try Project, Task and mixed batch restore/purge, then revoke between preview/commit → no mutation, success audit or success receipt on deny. | Independent security review and SQL runtime. |
| F02 | `docs/features/12-tasks.md` `FX-12-BR-006`; Calendar/Search/Favorites source-read and lifecycle guards; `tasks.task.read`, `calendar.event.read`, `calendar.calendar.read`, discovery source-open actions. | Fixed in source; Build verified; Runtime pending | `SqlProductivityService.cs`, `SqlSearchService.cs`, `SqlFavoriteService.cs`: Task projections are maintained by source writes and rechecked on Calendar/detail/Search/Favorites reads. | Backend/frontend builds verified; no runtime projection evidence. | Task active→Trash→restore; toggle FX12/FX13; edit Task while Calendar read is off; inspect Search/Favorite Event → Trash/disabled source is hidden, restore refreshes current projection, manual events remain. | SQL/browser owner QA. |
| F03 | `docs/features/08-trash-activity-and-audit.md` `FX-08-BR-001..006`, `FX-08-AC-001..003`; DB no-action FK/order contract; `tasks.task.purge`, `projects.project.purge`. | Fixed in source; Build verified; Runtime pending | `SqlTrashService.cs`: explicit Event projection, PlannerPin, Reminder, TaskHistory, child TrashItem, Task and Project deletion order; independent FileReference blocks. | Migration/source review; backend build 0 errors. | Purge Task with no refs/projection/pin, then each ref, and Project with children; inject SQL failure at each boundary → refs are handled per contract and the entire transaction rolls back. | SQL FK/concurrency/fault-injection and independent migration review. |
| F04 | `docs/features/14-reminders-and-scheduling.md` `FX-14-BR-001`, `FX-14-BR-005`, `FX-14-AC-001..004`; `reminders.configuration.set/remove`; `tasks.task.set_reminder/remove_reminder`. | Fixed in source; Build verified; Runtime pending | `ReminderPolicy.cs`, `SqlReminderService.cs`, `SqlProductivityService.cs`, Task request/UI: preset can be before Start; expired config is stateful, not a Task [Start,End] validation failure. | Backend/frontend builds verified. | Preset, exact past/future, Title-only edit with expired reminder, complete/skip → save succeeds where policy allows, ETag changes correctly, no stale alert is created. | SQL/browser/reminder dispatch QA. |
| F05 | `docs/features/14-reminders-and-scheduling.md` `FX-14-BR-005..006`, `FX-14-AC-001..004`; `reminders.schedule.dispatch/invalidate`. | Fixed in source; Build verified; Runtime pending | `SqlReminderService.cs`, `ReminderDispatchWorker.cs`, migration `0025`: leased bounded claim, `NextAttemptAt`, attempts/final disposition and per-item exception isolation. | Backend build 0 errors; no worker runtime. | 25 blocked + 1 eligible, poison item, transient SQL, restart, two workers → eligible item is not starved; one logical notification only; retries bounded and authorization never bypassed. | SQL timing/interleaving and worker restart QA. |
| F06 | `NXG-SYS-01`, `NXG-SYS-03`, `NXG-SYS-06`, `NXG-SYS-10`; `FX-14-BR-001`, `FX-14-BR-005`; `reminders.schedule.dispatch`. | Fixed in source; Build verified; Runtime pending | `SqlSelfCapability.cs`, `SqlReminderService.cs`, `SqlAdminAccessService.cs`: active User, non-deleted User and active PersonalSpace are required both for request capability and dispatch. | Backend build 0 errors; source review only. | Disable/delete User or suspend PersonalSpace immediately before and during due dispatch → no new notification; re-enable does not replay stale alert. | SQL account/space interleaving QA and independent security review. |
| F07 | `docs/ux-ui/global/05-forms-and-validation.md` UX-05 dirty/conflict contract; `docs/features/20-documents.md` `FX-20-BR-003`, `FX-20-AC-004`; `documents.page.save`. | Fixed in source for Documents; Build verified; Browser pending | `App.tsx`, `api.ts`, `styles.css`, `SqlDocumentService.cs`: draft/base revision stays local; 412 conflict panel offers reload/reapply/cancel; shared guard offers Save changes/Discard changes/Keep editing; revoke clears protected shell. | Frontend build verified; no browser QA. | Two tabs cause 412, navigation/back/refresh with dirty draft, revoke access → draft is not overwritten; explicit choice is required; only authorized nonsensitive draft may remain in memory. | Browser/accessibility/security review. |
| F08 | `docs/features/07-files-and-attachments.md` `FX-07-BR-001..006`, `FX-07-AC-001..003`; `files.file.upload`; FX07 gated. | Fixed in source; Build verified; FX07 runtime blocked | `SqlFileService.cs`, `FileServiceContracts.cs`, migration `0025`: owner+handle+state are read/claimed before receipt/path I/O; attempt path is owner/session/attempt scoped. | Backend build 0 errors; migration not run. | Wrong owner/handle, replay, same-session concurrency and cancellation → no other attempt’s staging is touched; exact cleanup only. | FX07 readiness, SQL/storage runtime and independent security review. |
| F09 | `docs/implementation/identity-sql-backed-slice.md`; M01 API operations `register`, `verify`, `resendVerification`, `requestReset`, `confirmReset`; identity delivery/TTL/audit/scope contract. | Blocked by contract; source remains fail-closed | `LocalAccountMessageSink.cs`, `SqlIdentityService.cs`, `Program.cs`, identity UI were not changed to claim Sent; raw token is not logged or publicly exposed. | No safe delivery implementation to build/accept. | Only after an approved local delivery adapter/inbox contract: register→receive→verify and reset→receive→confirm, owner/operator authorization, expiry/resend invalidation → token is delivered only to the authorized local channel; otherwise UI says unavailable, never fake Sent. | Product/security decision for transport, auth, audit, TTL and crash/retry semantics. |
| F10 | M01 API contract “same key/body replay after current authority; same key/different body 409; no credential/session replay”; `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-06`. | Fixed in bounded source slices; Build verified; Runtime pending; Partial for services outside the repaired slices | Product/Favorites/Notifications/Trash/Documents/Snippets/Bookmarks receipts now persist safe result/status where implemented; `api.ts` and affected forms/actions retain keys per logical intent. | Backend/frontend builds verified; no lost-response runtime evidence. | Lose response then retry same key/body; reuse key with changed body; revoke before replay; retry login/reset/reauth outputs → no duplicate effect; mismatch 409; revoked replay denied; credentials/session outputs never replayed. | Full service/catalog audit and runtime QA; remaining generic service receipts are not claimed complete. |
| F11 | `docs/architecture/04-authorization-and-sensitive-data.md`; CSRF pair/rotation contract; `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-06`. | Fixed in source; Build verified; Runtime pending | `web/Nexora.Web/src/api.ts`: memory token cache has single-flight refresh and one bounded retry only for `CsrfInvalid`, preserving body and idempotency key. | Frontend build verified; no two-tab/runtime evidence. | Expire/rotate cookie, use two tabs, restart API and retry mutation → one bounded CSRF refresh, no duplicate business mutation or infinite retry. | Browser/runtime QA. |
| F12 | `docs/features/20-documents.md` `FX-20-BR-003`, `FX-20-AC-004`; payload contract body max 1 MiB UTF-8; bounded request filter. | Fixed in source; Build verified; Runtime pending | `EndpointSecurityFilters.cs` stays 64 KiB by default; Documents/Snippets/Toolbox groups receive intentional bounded 8 MiB JSON ceiling, while service byte validation remains 1 MiB. | Backend build 0 errors; source/static review. | Payload below/at/over domain limit, multibyte/escaped JSON, response 413 → server remains bounded, validates UTF-8/domain bytes, and UI keeps draft. | API/runtime payload QA; verify exact deployed server limit. |
| F13 | `docs/features/12-tasks.md` `FX-12-AC-004`; `tasks.task.create/update`; optional priority contract. | Fixed in source; Build verified; migration pending | Task DTO/service/UI accept null; migration drops old NOT NULL/default/check and adds nullable check; existing P3 is untouched. | Backend/frontend build verified; migration not run. | Create/edit/filter/sort Task with no priority and old P3 rows → blank is valid, ordering is explicit, old values retain P3 meaning. | Authorized SQL migration and runtime QA. |
| F14 | `docs/features/13-calendar.md` `FX-13-BR-002`, `FX-13-AC-003`; `calendar.calendar.read`, event read/create/update. | Fixed in source for bounded Calendar range/all-day slice; Build verified; Runtime pending | `SqlProductivityService.cs`, `App.tsx`: IANA profile conversion, DST round-trip rejection, exclusive all-day end and server interval overlap query. | Backend/frontend builds verified; no DST/browser evidence. | Browser timezone differs from owner profile, DST gap/overlap, overnight/multi-day all-day → no guessed instant or date shift; interval-overlap events appear. | SQL/browser/date-time QA; full ICS remains outside this fix. |
| F15 | `docs/features/25-search-favorites-and-command-palette.md` `FX-25-BR-001`, `FX-25-BR-003`, `FX-25-AC-001`; `calendar.projection.open`, discovery source-open. | Fixed in source; Build verified; Runtime pending | Search/Favorites emit typed `/resources/{type}/{id}` links; login return accepts only same-origin canonical resource paths without query/hash/external authority; detail resolvers recheck owner/gate/lifecycle. | Backend/frontend builds verified. | Open item, expire session, refresh/back, foreign owner, revoked/trashed/invalid return path → correct item opens only when authorized; otherwise generic unavailable and no unsafe redirect. | Browser/security QA. |
| F16 | `docs/features/09-settings-and-app-shell.md` `FX-09-BR-001..005`, `FX-09-AC-001..003`; `settings.preference.update`; locale vi/en contract. | Partial source fix; Build verified; Runtime pending | `App.tsx`, `styles.css`: System/Light/Dark applies to root with OS listener and resets on session/owner change; locale sets shell lang/date formatting but hardcoded Vietnamese strings remain. | Frontend build verified; no contrast/locale runtime. | Reload, system theme change, logout/other owner, vi/en and date formats → theme does not leak owner state; supported locale behavior is observed honestly; no claim of full translation until completed. | Complete app-owned vi/en string catalog and accessibility QA. |
| F17 | `docs/architecture/05-api-and-frontend-contracts.md` bounded default25/max100 stable cursor; `FX-25` bounded search/favorites contracts; Calendar range query. | Fixed in source for Projects/Tasks/Calendar; Build verified; Runtime pending | Projects, Tasks and Calendar now use bounded server keyset cursors scoped to their filter and expose `NextCursor`; the UI provides load-more. Calendar range filtering remains server-side and Favorites retains its cursor. Search remains a bounded per-source slice with no unbounded query. | Backend/frontend builds verified; no multi-page runtime. | More than one page, equal sort keys, insert/delete between pages, old event in current range → no unbounded query; load-more continues from a stable keyset and filtered events remain visible. | SQL/browser pagination QA; add search continuation only under a separately approved contract. |
| F18 | `docs/architecture/02-module-boundaries.md`; `NXG-SYS-04`, `NXG-SYS-14`; source-owned provider/orchestration rule. | Partial source fix; Build verified; Independent review pending | Task source now owns Calendar projection maintenance and consumers recheck source; Trash/Search/Dashboard/Sharing still contain direct cross-schema reads and the monolithic frontend remains. | Backend/frontend builds verified; no architecture/boundary check run. | Disable/revoke source and inspect every consumer; run boundary/build review → no cross-owner data or circular dependency; remaining direct SQL is catalogued, not silently declared compliant. | Gradual source-participant refactor and independent architecture/security review. |
| F19 | `docs/design-database/01-conventions-and-integrity.md`; actor/User versus OwnerUser/PersonalSpace semantics; `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-05`, `NXG-SYS-14`. | Fixed in source writers; historical backfill pending | Changed audited writers to use `actor.UserId` for `ActorUserId` and `OwnerUserId`; no blanket history overwrite or unverified backfill. | Backend build 0 errors; static writer review. | Use distinct UserId/PersonalSpaceId and inspect audit from each affected module → actor and owner user resolve correctly; legacy rows are retained and identified for mapped backfill only. | Independent data review and approved mapping/backfill plan. |
| F20 | `docs/goals/05-task-and-evidence-template.md`; `NXG-SYS-14`, `NXG-SYS-16`; canonical requirement/action→code→evidence trace. | Fixed in source docs; Build verified; current CI/runtime pending | Added current working-tree snapshot to `full-phase-traceability-matrix.md`, this per-finding brief and unposted PR draft; historical rows are explicitly labeled. | `git diff --check` and builds are recorded below; no CI was triggered. | Verify every linked file/action/goal and compare PR/check-run/SHA at handoff → current source, build evidence, runtime acceptance and historical inventory remain distinct. | Final reviewer/CI/SQL/browser evidence must be attached at a committed SHA. |
| F21 | `docs/features/07-files-and-attachments.md` `FX-07-BR-005`, `FX-07-AC-003`; `files.file.purge`; durable exact-path cleanup. | Fixed in source for durable tombstone/worker; Build verified; FX07 runtime and crash-boundary review pending | `SqlFileService.cs`, `FileCleanupWorker.cs`, migration `0025`: cleanup intent is transactional, exact owner path is deleted after metadata, bounded retry/final failure state is durable. | Backend build 0 errors; migration/worker not runtime tested. | Fault-inject before/after metadata, delete and process restart → binary is deleted last or durable Pending/Failed intent remains; no other owner/path is deleted. | FX07 gate, storage fault injection, independent migration/security review. |
| F22 | `docs/features/14-reminders-and-scheduling.md` `FX-14-BR-001`, `FX-14-BR-005`, `FX-14-AC-001..004`; `tasks.task.set_reminder/remove_reminder`, `reminders.configuration.set/remove`. | Fixed in source; Build verified; Runtime pending | `SqlProductivityService.cs` treats `calendar.Reminder` as canonical config and `Task.ReminderAt` as compatibility projection; embedded and FX14 commands update source revision/history transactionally. | Backend/frontend build verified; migration/runtime not executed. | Create after migration, edit/preset/remove/reload both screens, dispatch once and inspect TaskHistory → one current config, one notification occurrence, reminder change history present, no stale replay after re-enable. | Authorized migration plus SQL/browser/worker QA. |

## Verification actually run in this code-only run

Environment: Windows PowerShell, repository working tree, `HEAD`
`fcb1f75fd8dccfa0ba1f15b49569e04c694569ce`, branch
`impl/m01-s00-scaffold`. No test command was run.

| Command | Result |
| --- | --- |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore` | Pass, exit 0, 0 warnings and 0 errors. |
| `npm run build` from `web/Nexora.Web` | Pass, exit 0; TypeScript project build and Vite production bundle completed. |
| `git -c safe.directory='D:/Projects/ASP_NET_Core_Developer_2026/Nexora' diff --check` | Pass: no whitespace errors; Git reported only normal LF→CRLF working-tree warnings. |
| SQL migration execution / `migrate.*` / readiness | Not run; no authorized SQL environment and code-only amendment. |
| Unit/integration/E2E/browser/functional tests | Not run by explicit amendment. |
| Provider/OAuth/network worker, Docker/SQL service startup, fixtures/demo records | Not run/created. |
| Independent review | Pending; none claimed. |

The untracked `src/Nexora.Bootstrap/packages.lock.json` was present in the
worktree and was not created or modified by this task.
