# FX01–FX40 implementation ledger — current continuation

Status: PRODUCT_INCOMPLETE. No whole FX group is ACCEPTED.

**2026-10-25 audit overlay:** [733 exact action inventory, detailed quality and screen checklists](16-full-feature-inventory-20261025.md) retain declared versus observed status explicitly; [current remediation/QA/release report](17-release-audit-20261025.md) records actual new execution. This continues this ledger rather than promoting whole modules. Current user authorization permits QA/tests and supersedes older code-only execution statements for this audit only. No new Release Ready claim follows from green baseline CI or source token matches.

The40rows below retain the explicitly historical2026-10-19 baseline inventory.
They are backlog starting points, not current runtime certification. Current delta
source fixes and tests appear in worker reports and07-final-verification.md.
Each module still requires exact AC→API/service/SQL/UI→current runtime evidence.
Ordinary missing authorized code is NOT_STARTED, not BLOCKED_BY_APPROVAL.
D01 Vault, D02 advanced Finance, D03 Task extensions, D04 real providers, D05
sensitive allowlists and D06 partial Plan contracts retain their precise gates.

| FX | Feature | Disposition at PR head | Present bounded surface / priority remaining proof |
|---|---|---|---|
| 01 | Identity/Profile | IN_PROGRESS | Registration/login/session/profile/reset installed; MFA TOTP/recovery and same-account restore remain unshipped or gated; test full state graph. |
| 02 | Users/Roles/Action Permissions | IN_PROGRESS | Admin grant and user-module workflows installed; exact grantable manifest conflict in latest unit test, bulk/race/last-SuperAdmin review open. |
| 03 | Module Platform | IN_PROGRESS | Catalog/policy/defaults and runtime navigation present; enable/disable/migration/recovery and paused-module truth still require acceptance. |
| 04 | Read-only Sharing | IN_PROGRESS | `SharingEndpoints` includes preview/list/create/update/revoke/resolve; owner-revoked metadata semantics and full source/projection/epoch/share modes still need evidence. |
| 05 | Support/Emergency/Security | IN_PROGRESS | `SupportEndpoints` registered; consent/emergency boundary, audit-before-access, sensitive projection and leak-negative evidence incomplete. |
| 06 | Notifications/Delivery | IN_PROGRESS | Inbox/read state and trusted writer work present; source-open all types and independent InApp/Email/BrowserPush effects not accepted. No arbitrary SuperAdmin publish API. |
| 07 | Files/Attachments | IN_PROGRESS | File endpoints and cleanup worker exist; clean/quarantine/retained files/download permissions and actual Windows three-principal ACL gate incomplete. |
| 08 | Trash/Activity/Audit | IN_PROGRESS | Trash API and source-specific lifecycle subsets exist; per-FX cohorts/retention/history/purge/authority not fully certified. |
| 09 | Settings/Shell | IN_PROGRESS | Settings/profile/navigation/dirty-modal guards present; responsive/a11y and dashboard duplicate/overflow defects remain. |
| 10 | Import/Export/Backup/Restore | IN_PROGRESS | Calendar ICS plus Documents import/export endpoints exist; full version/file fidelity and cross-module durable backup/restore remain unverified. |
| 11 | Projects | IN_PROGRESS | Projects CRUD/states/cohorts present; complete state/cascade/race/Trash/permission matrix not signed off. |
| 12 | Tasks/Kanban/Table | IN_PROGRESS | Flat Task flows and Calendar interaction exist; full lifecycle/UX matrix pending; recurrence/subtasks/snooze/attachments are separately gated. |
| 13 | Calendar/ICS | IN_PROGRESS | Personal Event and ICS import/export paths present; DST/races/exact ownership and full negative cases pending. |
| 14 | Reminders | IN_PROGRESS | Reminder scheduling/worker and chooser installed; stale/cancel/lease/retry/three-channel acceptance pending; standalone extensions gated. |
| 15 | Daily/Weekly Planner | IN_PROGRESS | Pins/reorder/week queries and authority repairs exist; source-unavailable, race/replay/permission matrix not certified. |
| 16 | Goals/Targets | IN_PROGRESS | Numeric/Boolean goals, change previews, versions, archive and progress history present; `goals.target.link_task/unlink_task` absent in earlier action trace, Trash and full acceptance remain. |
| 17 | Habits | IN_PROGRESS | Schedule/checkin/correction/state/authority repairs present; real reminders and complete lifecycle/replay evidence pending. |
| 18 | Time Tracking | IN_PROGRESS | Time entry/timer source paths present; overlap/source relation/history/restart and complete acceptance pending. |
| 19 | Pomodoro/Focus | IN_PROGRESS | Focus worker/conversion present; restart/clock/phase overlap/authority tests remain. |
| 20 | Documents/Knowledge | IN_PROGRESS | Page/version/tag/history/topology, restore and format transfer code **present**; complete visual editor, compare, shared-source/file dependency, fidelity and integrated acceptance not proven. Do not use stale Oct16 “restore absent” claim for current head. |
| 21 | Bookmarks | IN_PROGRESS | Manual CRUD, archive/Trash, authority/replay improvements present; broader Tag/Collection/source-refresh/share and full regression pending. |
| 22 | Code Snippets | IN_PROGRESS | Text CRUD, version/history and protected UX present; source-specific restore/export/template and all AC/regression pending. |
| 23 | Read Later | IN_PROGRESS | Bookmark/News references and position work present; full reader/provider/body/current-source semantics and negative tests pending. |
| 24 | Tags/Collections/Templates | IN_PROGRESS / PENDING EXPANSIONS | Organization Tag CRUD and Documents tag catalog installed; no registered Collection/Template vertical flow. Earlier QA found 16 absent actions. |
| 25 | Search/Favorites/Saved/Recents/Palette | IN_PROGRESS | Search/Favorites + **SavedSearch, Recent, CommandPalette registered at current head**; all provider/facet semantics, private counts, end-to-end paths and keyboard evidence pending. |
| 26 | Dashboard/Widgets | IN_PROGRESS / UX FAIL | Dashboard + persisted layout/quick-create/drilldown code present; QA confirms duplicate Quick Create and horizontal overflow; stale initial loading/unit checks fail. |
| 27 | Personal Finance | IN_PROGRESS / BUSINESS-GATED EXPANSIONS | Manual records with exact currency/date/authority present; ledger/accounts/budgets/debts/FX/transfers/deletion need separate scope decision; latest currency acceptance still subject to final tests. |
| 28 | Vault | PENDING / SECURITY_CONTRACT_GATED | No Vault feature workflow registered in `Program.cs`; 28 absent actions in earlier QA. Crypto/recovery ADR and specialist review required before handling secrets. |
| 29 | News/RSS/Topic Watch | IN_PROGRESS | Category and stored reader/read history + Reading integration installed; TopicWatch, bounded ingestion, provider truth and full notifications path remain pending. |
| 30 | Price Tracking | GATED_EXECUTION / PENDING | Approved local/simulated code only if contracted; real price provider/refresh/alert execution remains PO-paused. No claim of usable end-to-end real tracking. |
| 31 | Wishlist/Shopping | IN_PROGRESS | Wishlist API/UI exists; Comparison/Orders/Sellers/Warranty coverage requires semantic workflow audit; do not assume done from Wishlist. |
| 32 | Developer Toolbox | IN_PROGRESS | Local Base64/URL/HTML/hash/UUID/password/JSON/regex set present; remaining permitted tools, persistence and network-policy cases pending. |
| 33 | GitHub Discovery | PENDING / PROVIDER-GATED | No GitHub Discovery workflow registered; 17 absent actions in earlier QA. Typed stored search/snapshots/saved queries then guarded adapter are still required. |
| 34 | Automation/Scheduler | IN_PROGRESS / GATED_EXECUTION | Local definitions/runs/retry/schedule worker code installed; real external worker/provider effects remain paused; local full lifecycle/restart evidence incomplete. |
| 35 | Integrations/Webhooks/n8n | IN_PROGRESS / GATED_EXECUTION | IntegrationConnection configuration endpoint exists; real OAuth/webhook/send/sync/provider workflow not approved or proven. |
| 36 | Monitoring/Job Operations | IN_PROGRESS / NETWORK-GATED | Monitoring config/API exists; authorized target probing with SSRF controls, degraded states and live execution need dedicated proof/approval. |
| 37 | Personal Assets/Devices | IN_PROGRESS | Metadata CRUD exists; accessory/repair/loan/warranty/serial disclosure/Trash/share and acceptance unclear. |
| 38 | Digital Assets | IN_PROGRESS | Metadata/renewal model and UI exists; subtype/current provider/certificate/secret references and guarded inspection not accepted. |
| 39 | Career/Resume | IN_PROGRESS | Companies/Jobs/Appointment Calendar link endpoints present; version-pinned Resume/share/application flows not demonstrated and history/projection needs verification. |
| 40 | Skills/Courses/Learning/Work Log | IN_PROGRESS / PENDING EXPANSIONS | Skills, Courses, evidence, manual Work Log/history/Trash installed; Skill merge, Learning Plan, Certification/renewals absent in earlier QA (24 actions across FX40). |


## Current implementation delta

- FX16: five exact shipped Goals actions eligibility repaired, no grants changed.
- FX18/19: Focus/Time request, exact-capability and modal recovery repairs.
- FX20: bounded real version differences and import-mode stale-preview defense.
- FX09/26/27: Dashboard loading/React keys, shell and Finance dialog usability.
- FX32: regex bounds/private errors/UTF8/legacy warning checkpoint94fac58.
- FX04/20: SQL oracles reconciled to current D07/literal-safe Markdown contracts.

These deltas do not complete all lifecycle/source/negative/race/UX requirements.
FX24 Collections/Templates, FX33 storedDiscovery, FX31 Orders/Sellers/Warranty,
FX39 Resume and FX40 Plans/Certifications remain genuine implementation backlog.
Do not treat absent endpoints alone as proof that all related behavior is absent.

## Oct23 current-code delta

The table above is historical and is not overwritten as acceptance. Current
increment: FX04 bounded authorized pagination, FX24 ambiguous replay fail-closed,
FX32 four offline tools, FX40 typed gated Plan vertical plus list/detail routing,
and FX07 real-principal Windows verification harness. States are CODE_IMPLEMENTED
subsets / VERIFICATION_PENDING. Plan readiness remains false. FX01–FX40 as whole
modules remain IN_PROGRESS; none becomes VERIFIED/ACCEPTED from these builds.
Current limitations and ordinary unfinished code are in [09](09-continuation-20261023.md).

## Oct24 R1 code-only increment (supersedes absent-code statements above)

All40 whole-module statuses remain IN_PROGRESS or specifically gated. No whole
module is VERIFIED/ACCEPTED by this increment. Historical CI counts do not certify
these new executable bytes. Details: [plan12](12-r1-implementation-20261024.md) and
[root handoff](workers/r1-20261024-root-integration.md).

| FX | New CODE_IMPLEMENTED subset | Ordinary unfinished work / acceptance |
|---|---|---|
| 24 | Collections API/SQL/ordered current-readable source references/native list/detail UI | Templates still absent; source expansion and SQL/API/UI/concurrency acceptance pending |
| 32 | Browser-only bounded text diff and color/palette/contrast with SQL authority seam | YAML/conversion/date arithmetic/cron/Markdown/formatters/QR/URL/header/certificate, history/favorites/output workflows remain |
| 40 | Skill merge with retained evidence/history; Plan archive/unarchive; typed source open; native Course detail and immutable uncertain-write recovery | Plan readiness false; Plan Trash/progress enrichment, Certifications/renewals and complete owner runtime acceptance remain |
| 09/25 | Finite internal deep links/login return paths for Collections/Courses and context-keyed private screens | Complete shell/keyboard/responsive/journey acceptance remains |

FX01–08,10–23,26–31,33–39 have no new domain-capability acceptance in this increment;
their earlier ledger gaps remain open, subject to exact current code reconciliation.

### Subsequent FX32 increment

URL parser and pasted header inspector are now CODE_IMPLEMENTED with browser-only
payloads/current SQL authority, not ordinary missing tools. Native per-tool routes,
catalog name/category filter and scoped filter restoration are also code-present.
See [14](14-toolbox-navigation-url-headers-20261024.md). Their privacy/runtime gates
remain separate; no whole-FX32/R1 acceptance. Earlier URL/header absent-code rows
are historical; remaining other tool/history/output gaps still apply.
