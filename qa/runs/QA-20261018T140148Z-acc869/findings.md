# Backlog QA — QA-20261018T140148Z-acc869

20 tickets:2 confirmed product UX/visual defects,2 source-contract ambiguities,16 test/verification infrastructure issues. No confirmed Critical/High product exploit. Test infrastructure High does not imply product vulnerability. No fixes applied.

## NQA-001 — Official backend unit project cannot compile TagListCursor tests

TEST_INFRA; High/P1; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj -c Release --no-restore

Expected/source: Official unit gate must compile and enforce owner/filter/capability-bound cursor contract

Actual/impact: CS7036 missing documentsAllowed argument plus cascading CS1503; zero official unit tests executed Repro: 1/1.

Evidence: backend-unit.log. Likely cause/source (hypothesis where MEDIUM confidence): tests/Nexora.UnitTests/TagListCursorTests.cs:13; Infrastructure/Organization/TagListCursor.cs.

Fix: Align test calls with reviewed cursor v2 context; add true/false documentsAllowed cross-context denial assertions. Do not remove cursor checks.

Closure/regression: Official project compiles; all malformed/owner/query/limit/capability negatives execute.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-002 — Reviewed migration expectation ends0055 while current manifest installs86 files

TEST_INFRA; Medium/P1; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run qa-unit.csproj after excluding only compile-broken TagListCursorTests (original gate remains blocked)

Expected/source: Reviewed ordered migration list must match current approved files through0085

Actual/impact: Independent unchanged test fails exact equality;46 other xUnit cases pass Repro: 1/1.

Evidence: unit-independent-no-rebuild.log;results/unit-independent.trx;schema-verification.json. Likely cause/source (hypothesis where MEDIUM confidence): tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs:17.

Fix: Review and maintain independent expected ordered file list including current migrations; never compare manifest to itself to get green.

Closure/regression: Exact list/checksum/replay guard passes against reviewed files; malformed journal still fails.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-003 — Dashboard renders three copies of Quick Create after state updates

UX; Medium/P1; confidence HIGH; OPEN. Owner: Frontend.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; Edge;320x740/768x1024. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Login UserB; navigate Home; let dashboard/layout settle; count #dashboard-create-title; repeat at768 and320

Expected/source: One native Quick Create section per current owner context; UX26 consistency

Actual/impact: 3 sections in all4 repro attempts, repeated CTA groups and excessive vertical space. Production observation and internal screenshot confirm duplicate UI. Repro: 4/4.

Evidence: visual-repro-final.json;attempt1/visual-results.json;frontend-unit.log. Likely cause/source (hypothesis where MEDIUM confidence): web/Nexora.Web/src/App.tsx:3293/3311; GET /.

Fix: Use distinct stable sibling keys for DashboardQuickCreate and DashboardLayoutEditor while preserving actor/owner isolation remounts.

Closure/regression: Exactly one Quick Create after profile/layout/dashboard updates, back/forward and context switches; no duplicate React key warning.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-004 — Dashboard layout editor forces page-wide horizontal overflow on tablet/mobile

VISUAL; Medium/P1; confidence HIGH; OPEN. Owner: Frontend / UX.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; Edge;320x740/768x1024. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Home with default widget editor;768x1024 and320x740; inspect root scrollWidth and fieldset/heading bounds

Expected/source: UX10 responsive; no page-wide overflow or inaccessible controls

Actual/impact: 768px viewport scrollWidth834;320px viewport324; default fieldset/list min-content extends outside main. Reproduced twice per viewport. Repro: 4/4.

Evidence: visual-repro-final.json;attempt1/visual-results.json. Likely cause/source (hypothesis where MEDIUM confidence): web/Nexora.Web/src/DashboardLayoutEditor.tsx; styles.css; GET /.

Fix: Make layout fieldset/grid shrinkable (min-inline-size/min-width0), wrap controls at narrow widths; do not hide overflow to conceal inaccessible controls.

Closure/regression: All five required viewports and narrow-height editor usable by keyboard, root scrollWidth<=viewport; actions remain reachable.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-005 — Notification fixtures call an unregistered user-facing publish endpoint

TEST_INFRA; Medium/P1; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Execute notification SQL/functional journeys which POST /api/v1/admin/notifications

Expected/source: notifications.dispatch.publish is WORKER/SYSTEM, not an arbitrary user/SuperAdmin HTTP send capability

Actual/impact: Actual404 rather than obsolete expected403/201; downstream inbox/read/delete journey blocked by invalid prerequisite Repro: 2/2.

Evidence: sql-integration.log;functional-results.json;docs/action-catalog/modules/06-notifications.md. Likely cause/source (hypothesis where MEDIUM confidence): tests/Nexora.IntegrationTests/SqlApiIntegrationTests.cs:1133; functional/platform.spec.ts:8.

Fix: Create synthetic notification through approved trusted local dispatch/intent fixture; retain direct-user denial and three independent attempt assertions. Do not expose arbitrary publish endpoint.

Closure/regression: Real SQL inbox/read/unread/delete and owner denial run; no real delivery/provider send.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-006 — Documents tests still reject raw Markdown HTML instead of checking escaped rendering

TEST_INFRA; Medium/P1; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Create Markdown containing literal <script> text via old tests

Expected/source: FX20 current field contract: raw HTML escaped, no execution or external embed

Actual/impact: Current201 is expected by current feature contract; legacy tests expect422. Independent UI create/reload/version/publish/archive passes and script marker stays undefined. Repro: 2/2.

Evidence: sql-integration.log;functional-results.json;document-journey.json;docs/features/20-documents.md. Likely cause/source (hypothesis where MEDIUM confidence): tests/Nexora.IntegrationTests/SqlApiIntegrationTests.cs:549; functional/content.spec.ts:6.

Fix: Reconcile oracle with approved literal rendering; assert no script/URL execution, original body persistence and malicious link sanitization.

Closure/regression: Both old suites align after contract review, escape/security assertions remain stronger than status-only check.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-007 — Snippet SQL observer exposes CurrentVersion while pagination test reads VersionNumber

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run Snippets paging/conflict journey; inspect observer JSON field names

Expected/source: Observer must expose an explicitly agreed safe version field and test exact version/body

Actual/impact: Expected3, actualundefined; SELECT s.* returns CurrentVersion and the test polls nonexistent VersionNumber Repro: 1/1.

Evidence: functional-results.json;functional-execution.log. Likely cause/source (hypothesis where MEDIUM confidence): tests/Nexora.FunctionalTestOperator/Program.cs:104; functional/content.spec.ts:32.

Fix: Align safe observer DTO/test field with source schema after review; preserve exact3/version/body checks and no lost draft.

Closure/regression: Conflict/reapply case reaches SQL version3 and exact original draft; observer schema is documented.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-008 — Discovery journeys use obsolete Document archive confirmation selectors

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Open archive then wait for button Archive document using old helper

Expected/source: Current topology dialog requires scope preview, confirmation checkbox and Xác nhận

Actual/impact: Two90s timeouts; current UI Documents journey succeeds through real preview/checkbox/confirm Repro: 2/2.

Evidence: functional-results.json;document-journey.json;web/Nexora.Web/src/DocumentTreeDialog.tsx. Likely cause/source (hypothesis where MEDIUM confidence): web/Nexora.Web/functional/fixtures.ts:81; discovery.spec.ts.

Fix: Update topology-specific test flow to current accessible names and exact cohort preview. Never force-click or skip confirmation.

Closure/regression: Search/dashboard archive projections and restoration verify current source after actual cohort commands.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-009 — Task journey tries to edit readonly metadata status instead of dedicated transition

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Create Project/Task; choose Sửa; attempt #task-status Completed

Expected/source: Current action contract separates metadata update and completion

Actual/impact: Select is deliberately disabled for existing Task; test times out90s before lifecycle/purge assertions Repro: 1/1.

Evidence: functional-results.json. Likely cause/source (hypothesis where MEDIUM confidence): functional/content.spec.ts:61; App.tsx:3989.

Fix: Use the genuine completion action and current confirmation flow; keep direct API field-diff denial coverage.

Closure/regression: Create/edit/complete/cohort restore/purge end-to-end assertions all execute without weakening field guards.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-010 — Goal numeric journey targets removed first-Numeric progress editor

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run numeric Goal journey against current target manager

Expected/source: Explicit target-specific Numeric/Boolean dialogs per current target management contract

Actual/impact: Legacy Ghi progress/#goal-progress-value flow times out; API current metadata/definition/lifecycle history journey passes Repro: 1/1.

Evidence: functional-results.json;browser-cases.json. Likely cause/source (hypothesis where MEDIUM confidence): functional/resources.spec.ts:86; GoalTargetManager.tsx.

Fix: Select exact target row and native progress dialog; cover multiple targets and exact decimals, not first-target shortcut.

Closure/regression: UI numeric and Boolean progress, explicit complete/reopen/abandon and SQL history verified.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-011 — Finance journey assumes VND instead of selecting mandatory explicit currency

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Create record with amount123456.12345678 but omit currency

Expected/source: Currency required explicitly; never infer VND from Vietnamese locale

Actual/impact: Form correctly remains open; old test expects close/VND. Independent explicit-VND journey passes exact SQL decimal and usage guard. Repro: 1/1.

Evidence: functional-results.json;finance-journey.json. Likely cause/source (hypothesis where MEDIUM confidence): functional/resources.spec.ts:38; docs/features/27-finance.md.

Fix: Choose explicit currency; add blank-currency denial, zero/nonnegative boundary and separate currency summaries; use decimal strings for SQL oracle.

Closure/regression: Correct UI currency/date/amount persists; no float tolerance or inferred currency.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-012 — Owner revoked-link diagnostics conflict with legacy deleted-link omission expectations

SPEC_GAP; Medium/P1; confidence MEDIUM; OPEN. Owner: Product / Backend / QA.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Revoke own link; query owner state=Revoked; retain viewer invalidation assertions

Expected/source: UX04 exposes revoked-owner diagnostics; action catalogue says deleted/invalidated active management omits links; exact owner list semantics need reconciliation

Actual/impact: Two SQL tests expect empty; actual own Revoked row remains visible with token null/isActive false. No foreign viewer leak shown by this failure. Repro: 2/2.

Evidence: sql-integration.log;docs/ux-ui/modules/04-sharing.md;docs/action-catalog/modules/04-sharing.md. Likely cause/source (hypothesis where MEDIUM confidence): SqlSharingRepairTests.cs:143/404; SqlSharingService.cs:131; UX04 sections12/15.

Fix: Resolve owner Revoked diagnostics versus delete semantics with PO/source contract, then enforce same rule in API/query/UI/tests. Preserve old-token404 and no plaintext token reconstruction.

Closure/regression: Agreed owner list/get/revoke cases and all viewer401/404/source-revoke tests pass.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-013 — Historical Tag create replay after removal returns opaque committed ACK versus old ResourceUnavailable oracle

SPEC_GAP; Medium/P1; confidence MEDIUM; OPEN. Owner: Product security / Backend / QA.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Create with key, remove Tag, replay original create key/body

Expected/source: Exact per-Tag historical replay contract must specify safe ACK/source availability and current action/read gates

Actual/impact: Expected ResourceUnavailable, actual TagCommitted; no stale Tag body exposed. Current cross-module ACK convention differs from older test oracle. Repro: 1/1.

Evidence: sql-integration.log;tests/Nexora.IntegrationTests/SqlTagPaginationTests.cs. Likely cause/source (hypothesis where MEDIUM confidence): SqlTagPaginationTests.cs:42; SqlTagService receipt path.

Fix: Clarify original projection/allowed downgrade policy for Tags; preserve canonical intent, no duplicate writes and current authority. Do not blindly weaken test or return stale DTO.

Closure/regression: Original/changed payload, removed source, revoked module/read/action, keyless and parallel retry cases match agreed contract.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-014 — Tag namespace component fixture lacks current Documents Tags request handler

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Switch namespace from projects to documents in paging test

Expected/source: Source-owned Documents namespace boundary and cursor reset must remain tested

Actual/impact: MSW unhandled GET /api/v1/documents/tags?limit=25&namespace=documents; heading never appears Repro: 1/1.

Evidence: frontend-unit.log;frontend-unit.xml. Likely cause/source (hypothesis where MEDIUM confidence): src/OrganizationTagsScreen.test.tsx:43.

Fix: Register the current source route with accurate owner/permission/paging projection; keep onUnhandledRequest error.

Closure/regression: Namespace switch clears old items/cursor,25-row query and Document label assertions execute.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-015 — Generic empty-read fixtures no longer satisfy three enabled surface contracts

TEST_INFRA; Medium/P2; confidence MEDIUM; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run enabled Home/Bookmarks/ReadLater empty state cases

Expected/source: Component fixtures must match current concrete DTO/capability/layout contracts and settled async states

Actual/impact: Three failures: Home retains loading in fixture, Bookmarks/ReadLater fixture errors. Real-route settled scans do not show equivalent persistent loading. Repro: 3 cases/1 suite run.

Evidence: frontend-unit.xml;attempt1/visual-results.json. Likely cause/source (hypothesis where MEDIUM confidence): src/App.surface.test.tsx::emptyReadModel.

Fix: Investigate each strict response schema/async readiness; replace generic fallthrough models with reviewed factories, retaining error assertions. Root cause remains a fixture-contract hypothesis.

Closure/regression: All three empty surfaces settle under valid contract; denied/malformed/loading cases remain distinct.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-016 — Notification ETag component flow cannot reach expected unread control

TEST_INFRA; Medium/P2; confidence MEDIUM; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run Notifications keeps ETags component case

Expected/source: Current notification selection/read-state/ACK contract must retain list ETags

Actual/impact: Expected Đánh dấu chưa đọc button not found; current selected-item behavior/fixture shape needs diagnosis; not sufficient evidence of runtime product failure Repro: 1/1.

Evidence: frontend-unit.xml. Likely cause/source (hypothesis where MEDIUM confidence): src/App.test.tsx:764.

Fix: Reconcile fixture capabilities, three-channel projection and selection state with current UI; preserve ETag and denial assertions.

Closure/regression: Genuine read/unread/delete cases and independent captured request ETags verified.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-017 — Reminder picker test waits for pagination before initial source load settles

TEST_INFRA; Medium/P2; confidence MEDIUM; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Navigate fresh Reminders; evaluate options immediately; if target absent wait only for Load more Task sources

Expected/source: Wait for actual target OR real next-page control after initial load, not a premature empty-options snapshot

Actual/impact: Test fails because no next-page button exists; snapshot already contains source options on one page. Likely harness race; product root not established. Repro: 1/1.

Evidence: functional-results.json;functional-output/resources-Reminder-preset--d74fa--remove-retains-None-intent-desktop/error-context.md. Likely cause/source (hypothesis where MEDIUM confidence): functional/resources.spec.ts:119.

Fix: Use condition target-present or more-page-present, then select/page; add controlled slow initial-read case rather than sleep.

Closure/regression: Preset/None/Exact/remove run through real API/SQL, early and later-page targets both tested.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-018 — Module policy test applies superseded blanket local pause rule

TEST_INFRA; Medium/P2; confidence HIGH; OPEN. Owner: QA / .NET test engineer.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Preview System enabled for FX30/34/35 and require every confirmation disabled

Expected/source: DEC014 permits sufficiently contracted local/simulated code; real providers remain disabled

Actual/impact: At least one local Ready module allows confirmation; obsolete blanket assertion fails after successful registration-default checks Repro: 1/1.

Evidence: functional-results.json;docs/requirements/12-owner-decisions-local-e2e-implementation.md. Likely cause/source (hypothesis where MEDIUM confidence): functional/platform.spec.ts:62; DEC-20260909-014.

Fix: Bind assertions to current installed subset/state, test unsupported/live-provider denial separately. Never enable real provider workers for the test.

Closure/regression: Local configuration policy valid, unsupported scopes blocked and provider execution off despite grants.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-019 — Required Windows A/B/C capture-principal gate remains unexecuted

TEST_INFRA; High/P1; confidence HIGH; OPEN. Owner: Windows test infrastructure / Security QA.

Baseline/environment: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761; Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB; See case layer; desktop1366x768 for functional. Actor/data: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json. Preconditions: Exact dirty-source baseline612 paths; original tests unchanged.

Steps: Run original --require-os windows checks through compiled QA continuation

Expected/source: Actual runtime/operator/foreign processes required; SID strings in one process cannot prove boundary

Actual/impact: 30 custom checks pass,4 skip;1 required Windows foreign-principal case skipped makes exit1. Three Linux-only cases are N/A on this OS. Repro: 1/1.

Evidence: windows-custom-checks.log. Likely cause/source (hypothesis where MEDIUM confidence): scripts/dev/test-filesystem.ps1; Windows custom runner.

Fix: Provide controlled separate Windows principals/runtime/operator/foreign process fixture and run required gate; do not relax ACL policy or report skip as pass.

Closure/regression: Positive operator read, foreign denied, inheritance/traversal/restart/capture/cleanup proven with actual identities.

Dependencies: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

## NQA-020 — News journey waits for a legacy category-page heading

**ID**: NQA-020

**Type**: TEST_INFRA

**Title**: News journey waits for a legacy category-page heading

**Severity**: Medium

**Priority**: P2

**Confidence**: MEDIUM

**Status**: OPEN

**HEAD**: 1cd27a46a2ba1f24ab2b6f32f84771230f4b1761

**Environment**: Windows10.0.26200; .NET10.0.10/SDK10.0.302; SQL2019Express15.0.2000.5; Edge155.0.4283.62; isolated synthetic DB

**RouteOrEndpoint**: web/Nexora.Web/functional/platform.spec.ts:31; FX29 News route

**ViewportBrowser**: See case layer; desktop1366x768 for functional

**ActorData**: UserA/UserB or existing isolated fixture; IDs in seed-manifest.json/database-id-inventory.json

**Preconditions**: Exact dirty-source baseline612 paths; original tests unchanged

**StepsToReproduce**: Run the existing News category journey after its genuine module enable/grant prerequisites; wait for h1 News categories.

**ExpectedContract**: Locate the current News reader/category management using current feature and UX contracts; navigation alone cannot prove category CRUD.

**ActualImpact**: Legacy heading wait times out against the current reader layout before category assertions complete. Remaining operations are NOT_RUN in this journey.

**ReproRate**: 1/1

**EvidenceRefs**: functional-results.json;functional-execution.log;functional-output/

**LikelyCause**: Hypothesis: obsolete heading locator; requires contract and current DOM review before changing test. No product defect confirmed.

**RecommendedFix**: Review current category entry point and accessible heading, update navigation prerequisites/selectors without weakening category CRUD assertions, then rerun against real API/SQL.

**RegressionAndClosure**: Category create/edit/order/denial and persistence assertions execute on the current UI, with no bypass or forced click.

**DependenciesRelated**: Current requirements must win over old fixtures; no assertion weakening or provider/auth bypass

**SuggestedOwner**: QA / .NET test engineer
