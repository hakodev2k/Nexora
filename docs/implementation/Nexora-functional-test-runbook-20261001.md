# Nexora functional QA — 2026-10-01

Continue PR #4 (`impl/m01-s00-scaffold`), baseline `949dc9bde6d7857ab887b26819790440ec176648`. The user explicitly authorizes local test implementation, synthetic data, SQL/tool installation, UI popup fixes and continuation after interruption. This supersedes the earlier code-only amendment for this task. No merge, production/provider execution, real user data, secrets or force push.

## Scope and evidence rules

The full plan covers 40 feature groups, 202 screen definitions and 733 action keys. The new suite has 18 concrete workflows over five viewports: 1920×1080, 1366×768, 768×1024, 390×844, 320×568 (90 scheduled executions). Seventeen workflows exercise real mutations; the Files workflow only checks the disabled baseline. Its positive upload/download/restore/purge scenarios are **Not run**, not passed.

The suite prepares only prerequisite synthetic sources through real API services. Tested operations use the browser's normal login and UI. No mocked successful responses, cookie injection, storageState or rate-limit changes. Each worker keeps a normally established browser session. UUID names separate data across workflows; SQL checks verify IDs, OwnerId=PersonalSpace, values, lifecycle and source preservation. API-only setup never earns UI coverage. Pure local tools require separate independent output oracles and are not covered by these CRUD workflows.

The previous reported 18 desktop passes preceded an execution-host interruption. That source/evidence was not persisted and the host restored an older workspace. Do not reuse that count as evidence for this reconstructed source. Current SQL, integration, browser and multi-viewport results must be recorded afresh. JSON/JUnit are emitted by the new run, never filled with planned successes.

Goals: NXG-SYS-02/03/05/08/09/11/13/14/16; NXG-FX08/11/12/13/14/15/16/17/20/21/22/23/24/25/27-G01…G03. Exact assertions derive from current feature, API, UX and delivery contracts, especially FX-08-BR-003 (terminal Project restore retains children in Trash), optional numeric Goal target, Reminder remove retaining None intent, owner isolation, stale revision and finance decimal precision. Required independent migration/authorization/finance/cross-module review remains **Pending**. Self-review and green tests cannot close it.

## Reproduce

Use .NET 10, installed npm dependencies, a Playwright-compatible Chromium executable, HTTPS API and frontend reachable in the same local runtime. Supply private environment variables rather than source-controlled connection strings:

- `NEXORA_SQL_CONNECTION_STRING`: loopback SQL Server with `Nexora_Test_<32 lowercase hex>` database.
- `NEXORA_E2E_ACCOUNTS`: private JSON array of synthetic `{role,email,password}` records, mode 0600.
- `NEXORA_E2E_BASE_URL`: loopback URL without embedded credentials.
- `NEXORA_E2E_RUN_ID`: `nexora-e2e-<unique id>`; existing `NEXORA_E2E_OPERATOR_CLI` for baseline local operator.
- Optional `NEXORA_E2E_EXECUTABLE` and `NEXORA_E2E_SQL_OPERATOR`.

Build `tests/Nexora.FunctionalTestOperator`; only explicit `seed` creates a generated test database and eight synthetic accounts. `migrate` applies approved migrations without reseeding. `inventory` and `read-resource TYPE UUID` are read-only. Unknown commands and non-test databases are rejected. Never use the operator against production or log account manifests.

Run `scripts/dev/test-functional.sh` for all viewports, or append `--project desktop` for a focused first run. Start SQL/API/frontend first. A failed SQL startup is **Blocked/Not run**; never substitute SQLite or fake API responses. `npm run test:unit --prefix web/Nexora.Web`, web build, and SQL integration are separate gates.

Artifacts: `web/Nexora.Web/artifacts/functional-results.json`, `functional-junit.xml`; screenshots/video/trace disabled. Collect source revision, generated database/run ID (without credentials), migration count and test results in the report. Do not commit generated logs or private records. Cleanup is confined to generated test databases/data; the runner does not perform implicit destructive cleanup.

## Popup behavior

Bookmark, Tag, Finance category/record, Favorite add/rank, Read Later add/position, Goal progress, Planner pin, Habit check-in/schedule and Reminder configuration use compact dialogs. Dirty cancellation requires an explicit discard; failed requests keep the draft, busy dialogs resist Escape, background is inert and focus returns to the trigger. Complex Project/Task/Event/Document/Snippet/Goal/Habit editors retain full-page forms. Destructive actions use confirmation dialogs. Concurrent Bookmark/Tag/Finance edits offer explicit revision recovery without replacing the draft.

## Migration impact and rollback

`20261001_0031_trash_source_permissions.sql` inserts only missing metadata for four existing approved source restore/purge action keys. It does not alter role grants, existing permission status, owner checks or service authorization; it cannot enable an existing Blocked/Paused action. The prior service gates these exact source keys as well as FX08, but the keys were absent from SQL metadata.

The transaction uses XACT_ABORT and protected existence checks; migration journal/checksum guards and integration replay apply. Manifest now has 32 scripts. Old code can tolerate the extra metadata, so application rollback does not need destructive down-migration. If a status is incorrect, stop further rollout and prepare a reviewed forward correction; do not delete production permission rows automatically. Fresh apply/replay, restore/purge assertions and independent review remain required before accepting this migration. No production execution is authorized.

## Gaps that must remain visible

The 18 workflows are not exhaustive coverage of 733 actions. Identity/security/session/recovery, admin previews/grants, notification worker/channel states, sharing/support, Files positives, toolbox outputs, all pagination/boundaries/timezones and other domain slices need their dedicated suites. Keep unexecuted/gated/paused/retired scope separate. Coverage manifest rows are planned bindings only and cannot imply a passed action from a module's successful workflow.
