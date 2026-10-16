# Nexora functional QA — 2026-10-01

Continue PR #4 (`impl/m01-s00-scaffold`), recovery base `46f29ae41e5a8606c357cceeee679a9c9a92c640`. The user explicitly authorizes local test implementation, synthetic data, SQL/tool installation, UI popup fixes and continuation after interruption. This supersedes the earlier code-only amendment for this task. No merge, production/provider execution, real user data, secrets or force push.

## Scope and evidence rules

The full plan covers 40 feature groups, 202 screen definitions and 733 action keys. The suite has 40 concrete workflows over five viewports: 1920×1080, 1366×768, 768×1024, 390×844, 320×568 (200 scheduled executions). Mutations, identity sessions, admin signed previews/grants, Notifications and source-preserving lifecycle have concrete workflows; the Files workflow only checks the disabled baseline. Its positive upload/download/restore/purge scenarios are **Not run**, not passed.

The suite prepares only prerequisite synthetic sources through real API services. Tested operations use the browser's normal login and UI. No mocked successful responses, cookie injection, storageState or rate-limit changes. Each worker keeps a normally established browser session. UUID names separate data across workflows; SQL checks verify IDs, OwnerId=PersonalSpace, values, lifecycle and source preservation. API-only setup never earns UI coverage. Toolbox workflows use real authenticated tool endpoints with independent Node crypto/encoding/JSON oracles. Settings, Profile, Search and Dashboard now have dedicated workflows.

Current source and raw JSON/JUnit must identify the actual run. Development failures and interrupted runs are excluded from final pass totals. API prerequisites and metadata/route probes never earn UI-action credit.

Goals: NXG-SYS-02/03/05/08/09/11/13/14/16; NXG-FX08/11/12/13/14/15/16/17/20/21/22/23/24/25/27-G01…G03. Exact assertions derive from current feature, API, UX and delivery contracts, especially FX-08-BR-003 (terminal Project restore retains children in Trash), optional numeric Goal target, Reminder remove retaining None intent, owner isolation, stale revision and finance decimal precision. Required independent migration/authorization/finance/cross-module review remains **Pending**. Self-review and green tests cannot close it.

## Reproduce

Use .NET 10, installed npm dependencies, a Playwright-compatible Chromium executable, HTTPS API and frontend reachable in the same local runtime. Supply private environment variables rather than source-controlled connection strings:

- `NEXORA_SQL_CONNECTION_STRING`: loopback SQL Server with `Nexora_Test_<32 lowercase hex>` database.
- `NEXORA_E2E_ACCOUNTS`: private JSON array of synthetic `{role,email,password}` records, mode 0600.
- `NEXORA_E2E_BASE_URL`: loopback URL without embedded credentials.
- `NEXORA_E2E_RUN_ID`: `nexora-e2e-<unique id>`; existing `NEXORA_E2E_OPERATOR_CLI` for baseline local operator.
- Optional `NEXORA_E2E_EXECUTABLE` and `NEXORA_E2E_SQL_OPERATOR`.

Build `tests/Nexora.FunctionalTestOperator`; only explicit `seed` creates a generated test database and eight synthetic accounts. `migrate` applies approved migrations without reseeding. `inventory`, `catalog`, `read-access UUID`, `read-profile UUID`, `read-session UUID` and `read-resource TYPE UUID` are read-only. Notification reads select only safe ID/owner/title/read/deleted fields; catalog/access JSON consumes all SQL FOR JSON chunks. Unknown commands and non-test databases are rejected. Never use the operator against production or log account manifests.

Run `scripts/dev/test-functional.sh` for all viewports, or append `--project desktop` for a focused first run. Start SQL/API/frontend first. A failed SQL startup is **Blocked/Not run**; never substitute SQLite or fake API responses. `npm run test:unit --prefix web/Nexora.Web`, web build, and SQL integration are separate gates.

Artifacts: `web/Nexora.Web/artifacts/functional-results.json`, `functional-junit.xml`; screenshots/video/trace disabled. Collect source revision, generated database/run ID (without credentials), migration count and test results in the report. Do not commit generated logs or private records. Cleanup is confined to generated test databases/data; the runner does not perform implicit destructive cleanup.

## Popup behavior

Bookmark, Tag, Finance category/record, Favorite add/rank, Read Later add/position, Goal progress, Planner pin, Habit check-in/schedule and Reminder configuration use compact dialogs. Dirty cancellation requires an explicit discard; failed requests keep the draft, busy dialogs resist Escape, background is inert and focus returns to the trigger. Complex Project/Task/Event/Document/Snippet/Goal/Habit editors retain full-page forms. Destructive actions use confirmation dialogs. Concurrent Bookmark/Tag/Finance edits offer explicit revision recovery without replacing the draft.

## Migration impact and rollback

`20261001_0031_trash_source_permissions.sql` inserts only missing metadata for four existing approved source restore/purge action keys. It does not alter role grants, existing permission status, owner checks or service authorization; it cannot enable an existing Blocked/Paused action. The prior service gates these exact source keys as well as FX08, but the keys were absent from SQL metadata.

The transaction uses XACT_ABORT and protected existence checks; migration journal/checksum guards and integration replay apply. Manifest now has 32 scripts. Old code can tolerate the extra metadata, so application rollback does not need destructive down-migration. If a status is incorrect, stop further rollout and prepare a reviewed forward correction; do not delete production permission rows automatically. Fresh apply/replay, restore/purge assertions and independent review remain required before accepting this migration. No production execution is authorized.

## Verified full-scope continuation

Executable test source `221603430c248013fb7912a0cd1c6c224aed294b`: **200/200** passed in one full run, 40 concrete workflows on five viewports, no retries/skips/flaky results. This is not exhaustive sign-off for all 40 feature groups, 202 definitions or 733 action keys. SQL remains real, isolated, synthetic and populated; 32 migrations and eight fixture accounts. The test respects unchanged login policy with `NEXORA_E2E_LOGIN_PACING_MS=7000`.

Seven workflows FN-034…040 add Notification mutations/retention/isolation, granted Admin API reads with denied administrative UI, SuperAdmin permission Allow/Deny/Unset and Admin SELF rechecks, entitlement dependency blockers and data retention, registration-default signed commits and paused-policy blockers, Planner reorder and Project edit/skip. A populated Calendar exposed test pagination assumptions; helpers now follow API cursors and visible UI Load more controls without clearing data or changing product page sizes.

Inventory source `fa4e46053f533693c3b13e101345e335f99324fb`: 40 module entries/gates × five viewports = 200 observations; 202 first proposed routes × five = 1010 observations, no navigation assertions failed and no horizontal overflow. Only 25 definitions directly match the route map; 177 need alternate route/layout/state reconciliation. A Home fallback is not screen implementation evidence.

SQL has 40 modules, 190 permission rows, 160 exact catalog action matches. Missing 573 metadata records do not automatically prove absent handlers, especially for delegated/public/legacy aliases. Eighteen modules are Blocked/Paused; fifteen feature groups lack candidate API handlers. Positive domain tests for these baseline gaps remain unexecuted. Do not unlock gates or resurrect retired scope to report success.

Coverage: 112 action rows Partial coverage, 610 Not run, 11 Excluded-retired, zero exhaustive Passed. Every fullScopePassed remains false. Full per-row matrices: `full-plan-modules.csv` (40), `full-plan-screens.csv` (202), `full-plan-actions.csv` (733); raw JSON/JUnit and evidence pack are separate generated artifacts.

Run `scripts/qa/reconcile-full-plan.py` to initialize the inventory, then `scripts/qa/full-inventory.cjs` against the actual running SQL/API/browser environment. Export after copying final 200-pass JSON to `full-200-results.json` in `NEXORA_QA_EVIDENCE_DIR`, using `NEXORA_QA_SOURCE_COMMIT` and `scripts/qa/export-full-ledger.py`. The exporter requires actual 200-pass statistics and unique 40/202/733 IDs, and cannot fabricate success from source or route references.

Required independent review remains **Pending**. Historical integration/unit/axe/baseline E2E evidence is recorded in the continuation report separately and is not part of the 200 execution count.


## Time/Focus API continuation (2026-10-01)

PR4 product checkpoint: `ba692f8e0ac072079184676996c6ee4fe5000110`. Migration manifest is now 34 scripts. Run `scripts/dev/test-functional.sh --grep 'FN-04[123]'` against the same isolated SQL/API/Vite namespace, normal eight-account manifest and private capture/operator setup. These workflows add actual Time/Focus mutations and SQL readbacks; the earlier 40-workflow/200-run and 733-action inventories remain historical evidence on their stated source.

Helpers use normal signed SuperAdmin access preview/commit to enable current test-user grants, preserving existing grants rather than changing all users. Focus retains its hard FX18 dependency. When testing Time module revocation on fresh fixtures where Focus is also enabled, the helper first requests dependent FX19 disable together with FX18 disable and explicitly restores both afterward; do not bypass dependency guards or write grant rows directly.

Additional operator projections are allowlisted TimeEntry/FocusSession, safe Focus completion channel/status metadata (no notification body), and owner-scoped stopped-entry timestamp series (no description/category). Test fixtures remain synthetic and persistent. No production/provider secret is required. `etag` is the repository wire field; strict DTOs reject foreign-owner/state/source overrides.

Observed final reconstruction evidence: 15/15 (3 workflows × 5 viewports), separate 5/5 stronger Time report oracle, 15/15 real SQL integration, 12/12 unit, frontend 69 pass/1 existing skip; compile/typecheck passed. See [exact evidence and limitations](evidence/time-focus-20261001/results.json) and [scope/contract trace](../delivery/09-pr4-local-module-implementation.md). Both modules remain Partial, independent review Pending, and 13 groups still lack candidate API handlers. No merge or full-scope sign-off.
