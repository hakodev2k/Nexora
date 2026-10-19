# PR #4 — Feature completion, blockers and decision-ready implementation handoff (2026-10-19)

> **DECISION UPDATE (2026-10-19):** Product Owner explicitly confirmed **D01–D09 = Option A**. [Owner decision record](../requirements/13-owner-decisions-20261019-pr4-blockers.md) and [six implementation contracts](pr4-decision-contracts-20261019/README.md) supersede every earlier PROPOSED D01–D09 choice in this historical review. **Important D01 label correction:** The user's actual UI Option A was *threat model + ADR first, no secret implementation*; this review originally reversed the D01 A/B labels. All other chosen A semantics are documented in the signed decision record. PR remains Draft, not accepted.

> **Status: REVIEWED PARTIALLY; PRODUCT INCOMPLETE; NOT MERGE-READY.** This is a review of the committed remote PR head `4d9876ac00457f8888b864e0dc63721b5a316eeb`, not a claim to have executed tests or independently security-audited all 1,101 changed files. PR remains draft/open against main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Revalidate this review if the source head changes.

## 0. Authorization, sources, and evidence limits

Requested by owner: inspect all feature groups, distinguish Completed/In progress/Pending/Blocked, offer choices where a genuine decision is needed, and capture concrete implementation documentation. **This document does not approve product decisions, change code/production/provider grants, resolve security gates, or merge.** Approved local, contract-complete work continues under `DEC-20260909-014`; do not treat genuinely ambiguous business decisions as permission to guess. Do not re-label an open PO decision as `Approved`.

Source precedence: `AGENTS.md`, `docs/requirements/12-owner-decisions-local-e2e-implementation.md`, `docs/delivery/01-current-scope.md`, `docs/features/90-open-decisions.md`, `docs/features/README.md`, corresponding FX feature/action/UX/DB contracts, and actual API registrations. Cross-referenced investigations: `docs/implementation/full-product-rerun-20261016/{missing-capabilities,verification}.md`, `docs/implementation/remediation-plan-20261016/{implementation-plan,p1-work-packages,checkpoint-20261018}.md`, `qa/runs/QA-20261018T140148Z-acc869/{summary,findings,implementation-gaps}.md/csv` (the QA run used local source `1cd27a4` plus a dirty tree, **not** this merged PR revision).

Evidence: latest GitHub Actions run [39151552763](https://github.com/hakodev2k/Nexora/actions/runs/39151552763) actually ran on `4d9876a`, completed **FAILURE** on 2026-10-18 UTC. This review inspected GitHub PR metadata/changed-file names, selected feature code/registration and current requirement/runbook sources, actual latest CI job logs, and the preserved earlier QA manifests. It **downloaded and parsed** the current run's sanitized browser artifact `12144743847` (`playwright-junit.xml`); JUnit contains safe case-level results but deliberately redacts assertion details involving secret-bearing browser traces. It did **not** execute any local application/test, semantically certify 733 action contracts or 2,028 source declarations, or inspect each of 1,101 files line-by-line. Do not claim full-code audit/completeness from this review.

### Completion vocabulary

- `ACCEPTED / COMPLETED`: all committed requirements for the exact scope traced to shipped API/UI/data plus passing fresh end-to-end, negative, boundary and independent review as required.
- `IN_PROGRESS`: at least one installed vertical path/source slice exists, but scope, verification or sign-off incomplete. These rows do **not** mean every requirement has a handler.
- `PENDING`: required local workflow/action absent or not demonstrated by a registered endpoint/service/UX path.
- `BLOCKED_DECISION`: exact behavior needs owner/security approval that cannot be inferred from existing approved policy.
- `GATED_EXECUTION`: local/synthetic implementation may exist, but real provider, production or operation is explicitly not authorized.
- `UNVERIFIED`: implementation evidence insufficient, especially after a new revision; never silently convert to `PASS`.

**No FX group is certified COMPLETED by this review.** 733 actions, 40 FX and historical 202-screen inventory are scopes for semantic tracing, not acceptance percentages. Earlier QA's 87 `NOT_IMPLEMENTED` actions come from its own checkpoint and cover only five FX groups, not the entire remaining business backlog.

## 1. Current 40-FX feature disposition — grouped inventory, not action acceptance

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

For each row: check the **exact** current requirements and action catalog first; absence from one endpoint does not prove a runtime omission if another approved boundary implements it. Conversely, an endpoint name or passing happy-path cannot certify AC completion.

## 2. Latest exact-head CI defects and investigation queues

Run `39151552763` at `4d9876a`:
- Backend xUnit **47 passed / 2 failed / 0 skipped**: `M01PolicyAndCaptureContractTests.Admin_grant_mutation_uses_the_explicit_admin_grantable_manifest` rejects current `documents.page.restore_version` metadata, and `Local_release_migration_manifest_contains_all_reviewed_release_files` expects only migrations through 0058 though 0059..0085 now exist. Review current grant policy/approved action source **before** amending assertion; preserve independent migration expected list/checksum.
- Real SQL/API **250 passed / 2 failed / 0 skipped**: `SqlApiIntegrationTests.cs:549` expects 422 for raw HTML in Markdown but got 201 (likely stale oracle given approved literal escaped rendering; prove no XSS and correct source semantics before change). `SqlSharingRepairTests.cs:408` expects empty owner list but revoked record remains visible inactive/token-null (contract ambiguity; do not weaken viewer 404).
- Frontend Vitest **149 passed / 2 failed / 1 skipped**: `App.surface.test.tsx` Home still says “Đang tải các widget…” under fixture; `App.test.tsx` cannot find Notification “Đánh dấu chưa đọc” control. Diagnose fixture vs real settled behavior; assert exact ETag/revision and privacy states.
- Browser E2E **8 passed / 2 failed / 0 skipped** across desktop and mobile. The downloaded sanitized JUnit artifact `12144743847` identifies **the same failing test on both viewports**: `m01-identity.spec.ts > M01 register, local capture, verify, login, and keyboard-visible controls` (desktop 10.048s; mobile 4.793s). Password reset, controlled bootstrap failure, public/authenticated UI smokes all passed for their tested journeys. **The sanitized failure message masks the exact assertion** because secret-bearing call logs are excluded. Diagnose in isolated synthetic browser/API/operator environment without publishing raw call logs or secrets; don't infer a definite product root cause from this report.
- Windows ACL gate **FAIL**: 30 passed / 4 skipped on Windows; required A/B/C actual-process-identity capture test permanently skipped. A SID string in one process is not a substitute.
- Linux filesystem passed; agent-baseline passed in distinct workflow `39151552808`. Not a substitute for application tests.
- Earlier QA `QA-20261018T140148Z-acc869` (different source snapshot) has 20 findings: 2 reproduced product UX defects (`NQA-003` duplicate Quick Create, `NQA-004` mobile/tablet horizontal overflow), 2 semantic/spec questions (`NQA-012/013`), 16 test/verification issues. Product UX repro on 320/768 needs rerun on current remote head. `App.tsx:3293,3311` places QuickCreate and LayoutEditor as siblings with identical `key={contextKey}`; inspect React identity/reconciliation. `DashboardLayoutEditor.tsx` requires shrinkable layout, not hidden root overflow.

### Minimum priority closure order (no new PO decision required)
1. P0: compare current action catalog, ACL and migrations; repair xUnit grant/migration **oracle or product defect based on approved source**. Implement Windows A/B/C actual process test, fail closed. Preserve all security gates.
2. P0: establish SQL failure cause with expected current Markdown literal-safe semantics and revoked ShareLink owner-view semantics. No unauthorized share or unescaped HTML acceptance.
3. P1: fix duplicate QuickCreate and responsive layout without suppressing accessible controls; add assertions across profile/layout refresh, 320/390/768/desktop, keyboard/focus and role/context switches.
4. P1: fix actual frontend Notification/Home test prerequisites or product loading bug; test action current owner/ETag, error/revocation, and no protected followup read after opaque ACK.
5. P1: inspect `m01-identity.spec.ts` registration/verification/login/keyboard steps on both viewports against the sanitized JUnit's **2 failures**, distinguish fixture readiness/test expectation from an actual identity defect and repair appropriately; do not publish secret-bearing raw browser calls. Rerun **all** jobs on the resulting exact source revision, not historical green runs. Capture run/artifact/revision for each.
6. P1: independent security/DB reviewer signs off migration 0059..0085, current sharing/notifications/finance/documents/Goals/Habits/Planner and cross-module provider permissions. Full semantic forward/reverse audit of current active requirements remains pending; do not claim code completeness from QA inventories.

## 3. Decision register — options now resolved by Product Owner (2026-10-19)

**The Product Owner approved Option A for all D01–D09 on 2026-10-19, recorded in the linked decision document.** Remaining specialist/implementation gates are not thereby approved. Existing policies (e.g. hybrid recovery; no operator plaintext; explicit currency; default-deny sensitive fields; provider pauses) are non-negotiable without an explicit replacement PO decision.

| ID | Decision required and options | Recommended safe direction | Decision / affected scope |
|---|---|---|---|
| `NXPR4-D01` Vault crypto/recovery | A: threat model and architecture ADR **only**, no secret-bearing runtime; B: develop encrypted Vault after approved security ADR. | OWNER_CONFIRMED A; architecture/independent specialist review OPEN; no secret handlers. |
| `NXPR4-D02` Advanced Finance | A: approved manual records only for current work; B: separately contract advanced ledger/budget/FX. | OWNER_CONFIRMED A; advanced business scope remains gated. |
| `NXPR4-D03` Task/Reminder extensions | A: flat Task+one reminder; B: separately contract recurrence/subtasks/snooze/attachments. | OWNER_CONFIRMED A; extensions remain gated. |
| `NXPR4-D04` External provider activation | A: zero external calls, local/synthetic adapters only; B: provider-specific reviewed live enablement. | OWNER_CONFIRMED A; real FX30/34/35 paused and public read-only provider execution OFF in this run. |
| `NXPR4-D05` Sensitive sharing/support fields | A: default deny until reviewed per-resource field allowlist; B: select specific safe fields. | OWNER_CONFIRMED A; allowlist review still OPEN. |
| `NXPR4-D06` Learning Plan source types | A: Goal/Task/Course references only; B: expand to Skill with a new explicit product contract. | OWNER_CONFIRMED A; action/DB source mismatch corrected in documentation, schema/code/tests still pending. |
| `NXPR4-D07` Revoked ShareLink listing | A: owner can read safe inactive token-null own revoked diagnostics; B: omit revoked from owner list, separate audit. | OWNER_CONFIRMED A; external viewer still uniform 404, tests/review pending. |
| `NXPR4-D08` Removed Tag create replay | A: canonical proven original create returns opaque committed ACK; B: reconciliation/conflict when not proven. | OWNER_CONFIRMED A for provable receipt, unprovable legacy fails closed; tests/review pending. |
| `NXPR4-D09` PR release gate | A: keep Draft until fully accepted; B: formal R1 rescope with new PO baseline. | OWNER_CONFIRMED A; no merge/production/provider execution. |

### Convert a selected decision into executable documentation (per decision ID)

When owner chooses an option, create/update an ADR/PO decision record with: exact decision date and authorized actor; referenced current requirement/BR/AC/action/status; selected option and rejected alternative; changed behavior vs existing approved policy; API routes, typed request/response/error matrix and action/owner/role/field guards; SQL tables/migration up/rollback/compatibility; UI flow/errors/dirty draft/revocation and accessibility; original receipt replay/idempotency; current source projection and privacy; async worker lease/retry/external boundary where relevant; explicit **allowed/denied/race/replay/rollback** acceptance cases; owners, test runner and independent security/DB reviewer; source revision and actual evidence. Mark `IMPLEMENTATION_READY` only once the complete contract is reviewed and **all necessary approvals are recorded**. Never mark `Accepted` without executed evidence.

#### Implementable conditional handoffs

- **D01:** Threat model (owner/operator/attacker/lost-device/missing-root/backup); compare reviewed AEAD/KDF/wrapping/rotation algorithms and dependency choices; define versioned encrypted package, associated data, unwrap/re-auth, recovery authorization and audit, zero-plaintext operator path, key retention and decrypt-compatible rollback. Use `docs/features/28-vault.md`, security ADR, `docs/design-database/07-*.md`, and `NXG-FX28-G01..G03`. Only then create new local Vault API/SQL/UI vertical slices and independent negative/tamper/restoration tests. **No speculative secret feature before review.**
- **D02:** For B, specify account vs manual record aggregates, double-entry or non-ledger invariant, currency/FX source/rate instant, transfer reversals, debt interest, decimal precision, immutable audit, migration from existing manual records (no silent reinterpretation) and full acceptance. Keep `finance.manual_record.*` current authority independent.
- **D03:** For B, define recurrence instance identity/DST/idempotent regeneration, subtask parent/Trash cascade, reminder capacity/one-per-task override, snooze lifecycle, Files source rights, terminal tasks and conflict matrix. Separate migration + rollback and negative tests.
- **D04:** Implement safe stored adapters first; explicit per-provider allowlist/SSRF DNS rebinding and redirect revalidation/size/time/rate/credential redaction; exact owner consent/actions, durable attempts, partial/degraded truth, live vs simulated flags. Real `FX30/34/35` side effects disabled until explicit approval; never conflate configuration with execution.
- **D05:** For each source (Finance, Assets, Career, Learning, Vault, Files, Documents), make a field-level `SELF / SHARE / SUPPORT / EMERGENCY` matrix with copy/export restrictions, owner preview, pinned version/file, revocation/expiry and positive/negative exact query/JSON tests. Keep sensitive access fail-closed while matrix unapproved.
- **D06:** Reconcile `docs/features/40-learning-and-work-log.md`, action keys `learning.plan.*`, UX40, physical `PlanItem`, and original task/goal/course owner proof. Use same-TX source participants, exact reference types and stable rank, unavailable source not zero/complete, link/unlink retains source, current and historical projections separate. Tests: wrong-owner, stale target, revoke mid-read, duplicate link, reorder/replay, zero denominator.
- **D07:** Write owner list state predicate, `active/revoked/expired` filter semantics and cursor HMAC scope; independent external token resolution must remain 404 after revocation even if owner metadata remains accessible. Update `SqlSharingRepairTests.cs:408` from approved contract, not simply “expect nonempty”.
- **D08:** Document original create action receipt payload/status/owner/action/digest, current authority proof and no-second-create invariant; distinguish verifiable canonical receipts from ambiguous legacy ones; test replay after Tag delete, owner/module revoke, changed payload and tampered key; response contains no current/old Tag secrets.
- **D09:** Publish a normalized feature/action/AC → API/UI/SQL → test/run/revision → independent review register. Scope reduction needs Product Owner change log, not a test-ignore label.

## 4. Machine-actionable work packages (dependency order and definition of done)

| Work package | Prerequisite | Core changes/evidence to supply | DoD / blocking gate |
|---|---|---|---|
| `PR4-QA0` CI contract repairs | Current approved tests/contracts reviewed | Check `tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs` grant manifest + migrations 0059..0085; update after independent oracle review, no blanket Allow. | xUnit all green, SQL bootstrap/rollback+manifest checksum. |
| `PR4-QA1` Windows ACL | Windows runner with **three actual independently-identified processes/security principals** | Runtime writer, authorized operator, foreign actor access tests + ACL deny + cleanup, no SID spoofing. | Required A/B/C executed PASS, no skip; reviewer signs. |
| `PR4-QA2` SQL/API/UX regression | D07/D08 and literal Markdown contract resolution | Assert XSS-safe literal escaped rendering, owner-revoked token-null semantics, Notification ETag; fix real product errors only if source proves. | 252+ source-equivalent integration tests full pass (exact future count recorded), frontend, browser and privacy negatives green. |
| `PR4-UX` Dashboard correction | No business gate | Distinct stable sibling keys `App.tsx:3293,3311`, prevent 3 repeated QuickCreate; shrink layout editor; preserve keyboard order/focus. | One QuickCreate; no 320/390/768/desktop scroll overflow; locale/owner/refresh/Back/dirty modal tests. |
| `PR4-FX24` Collection/Template | Registered owner/source tag provider & source projection contract | SQL Collection/Member/Template, server actions, exact membership, template instantiation via original target create+receipt, native UI. | All 16 earlier missing actions traced; positive + cross-owner/revoke/stale/Trash/source-missing tests. |
| `PR4-FX28` Vault | Approved D01 security ADR and independent reviewer | Key hierarchy/wrapped version/encrypted package/recovery, isolated SQL/API/native UX; no operator plaintext. | Security review, tamper/deletion/rotation/lost-key/restore tests, no real secrets/provider. |
| `PR4-FX33` GitHub Discovery | Stored canonical query/snapshot schema; D04 network guard | Stored repository/query/saved/snapshot/compare, later read-only outbound adapter behind explicit gate. | 17 earlier absent actions tracked; UTC-week+stars/ties/staleness, revoke/partial/failure tests; real fetch only if approved. |
| `PR4-FX40` Learning | D06 typed plan refs + Files/notification dependency | Skill merge, Plan, Certification/version/renewal; retain existing Skill/Course/WorkLog and their history. | 24 earlier absent actions traced; versions/ID replay/source revocation/wrong-owner/reminder tests. |
| `PR4-BREADTH` Remaining FX | No blanket approval needed for existing approved local slices | Incrementally reconcile DOC/NEWS/SHOP/CAR/NTF/GOALS/HABITS/ORGANIZATION/FACETS/ADVANCED, and other 40-FX gaps in `p1-work-packages.md`. | Every active normative AC has verified forward/reverse code+runtime evidence or explicit pending/blocked decision. |
| `PR4-RELEASE` Final signoff | All prior gates | Rerun complete SQL/API, backend+frontend unit, browser UI desktop/mobile, Windows ACL, security & migration independent review, clean upgrade/rollback/restart. | Final SHA green, no unresolved High/Critical correctness/security gates, actual product/PO acceptance; only then ready-for-review. |

### Required test matrix for every implemented action

Actor owner A/foreign B/role revoked; action absent/Deny/Allow; module enabled/disabled; ACTIVE/Archived/Trash/Purged source; before/after expiry/revoke; quoted ETag current/stale/malformed; idempotency same key/same body, same key/different body, concurrent duplicate, first-commit lost response, legacy receipt; pagination >25, owner/filter/tampered cursor; source unavailable/degraded; cancel/dirty/keyboard/mobile; XSS/privacy/log/URL/audit; SQL transaction atomicity, migration/rollback and clean startup. Record `NOT_RUN` for any unexecuted variant. Synthetic local SQL only; no user/production data.

## 5. Confirmed owner choice and publication

**Recorded 2026-10-19: D01=A, D02=A, D03=A, D04=A, D05=A, D06=A, D07=A, D08=A, D09=A.** Source: the user's explicit choice; see [decision record](../requirements/13-owner-decisions-20261019-pr4-blockers.md). Detailed [API/SQL/UX/security/acceptance contracts](pr4-decision-contracts-20261019/README.md) carry the chosen A meanings, superseding earlier hypothetical options. The choice does not certify implementation, tests, independent review, secret handling or external provider execution; PR remains Draft.

**Review conclusion:** `REQUEST CHANGES / KEEP DRAFT` advisory. Source change to this documentation alone does not fix CI, accept an AC, execute QA, or discharge independent security review. 
