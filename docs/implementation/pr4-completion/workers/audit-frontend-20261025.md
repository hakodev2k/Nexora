# Frontend audit and News category recovery repair — 2026-10-25

## Task brief

Owner: `/root/frontend_audit`; coordinator: `/root`. Branch `impl/m01-s00-scaffold`,
starting HEAD `7a75bb6f9b84053bb646649ef9df7fec2836d23b`. The current explicit user
request authorizes source fixes, focused new tests and parallel review; it supersedes
the preceding code-only execution amendment for this task. No provider, production,
real personal data or deployment authorization is inferred.

Loaded: root `AGENTS.md`, `.agents/skills/nexora-engineering/SKILL.md`,
`.ai/profiles/nexora-implementation-agent.md`, Technical Lead README/core rules,
`.ai/verification.md`, documentation/delivery/current scope, DEC-014 and current
D01–D09 decisions, goals README/system/template; all nine `frontend` and five
`verification` paths in `.ai/routing.json`. Read FX24/FX29 feature and module goals,
FX29 action and UX contracts, common behavior and M01 readiness. No agent settings
were changed; baseline package verification is not application QA.

Bounded plan: review frontend recovery boundaries and recent Collections/Toolbox
surfaces, reproduce a confirmed defect at the component boundary, change only the
smallest recovery condition, run focused tests then affected frontend regression
and compilation. No API, schema, migration, permission or lifecycle contract changes.
Rollback: revert the two NewsCategory source/test changes; no data rollback needed.

## Reviewed source and limitation

Read source: `CollectionsScreen.tsx`, `collectionApi.ts`, `CourseScreen.tsx`
(recovery/load/write portion), `ResourceFormDialog.tsx` and its tests,
`ToolboxUrlHeaders.ts`, `ToolboxMoreOffline.ts`, `NewsCategoryScreen.tsx` and tests,
`newsCategoryApi.ts`, `NewsArticleDialog.tsx`, `RecentsScreen.tsx`,
`navigationGuard.ts`, and relevant App route/key/tool integration and API types.
This is a bounded source review, **not** a claim to have deeply reviewed every UI,
popup, operation, permission path or all forty modules. No browser was launched.

## FE-20261025-01 — ambiguous News category saves unlock editing

| Field | Evidence / disposition |
|---|---|
| Feature/actions | FX29; `news.category.create`, `news.category.update` |
| Severity | Medium; confirmed frontend recovery defect |
| Root cause | The uncertain-write condition recognized network status 0 and 503, but omitted HTTP 408 and other server/proxy 5xx responses. These responses do not establish that the original server command rolled back. |
| User impact | The form became editable after a potentially committed write. Editing generated a fresh idempotency key, allowing a new create or divergent update before reconciling the original outcome. Duplicate persisted effects were a risk, not reproduced against SQL. |
| Reproduce | Open New category or Edit category, save, return 408/500/502/504 from the category API boundary; name input incorrectly remains enabled and Save retains its normal label. |
| Contract | `docs/features/00-shared-behavior.md` FX-COM-006, FX-COM-AC-003 (same-key/body retry, no duplicate effects); FX29-S01 action binding; NXG-SYS-05/13/14. No new product behavior decision. |
| Classification | AUTO-RESOLVABLE |
| Fix | Treat 408 and every >=500 status as uncertain, preserving existing frozen name/original request key and Retry same save flow. Current 401/403/404 clearing, 412 comparison and RequestInProgress behavior retained. |
| Verification criteria | For create and update at each new status, input disabled, retry offered, exact original API arguments/body/key reused; existing denial/conflict/discard tests still pass. |
| State | FIXED and component TESTED; SQL/API/browser release acceptance NOT VERIFIED. |

Changed files: `web/Nexora.Web/src/NewsCategoryScreen.tsx` and
`web/Nexora.Web/src/NewsCategoryScreen.test.tsx`. Expanded two existing
parameterized cases with 408/500/502/504: **8 additional concrete cases**, no
assertion removed or weakened, no dependency/fixture contract altered.

## FE-20261025-02 — incomplete coverage denominator

Coordinator source review found `vitest.config.ts` counted only App/api/i18n,
excluding every newer extracted module/helper. Updated `coverage.include` to
`src/**/*.{ts,tsx}`; only tests, test setup/helpers, declaration files and entry
bootstrap `main.tsx` are excluded. No threshold added or existing assertion
disabled. This improves measurement; it does not create feature acceptance.

Executed `npm run test:unit:coverage -- --reporter=dot`, exit0, 25 files passed,
**209 passed / 1 existing skip**, 24.99s. V8 full-source coverage:
**28.73% statements (4884/16998), 25.58% branches (4214/16469),
23.38% functions (889/3802), 39.03% lines (3359/8606)**.
This run overlaps the prior 209-case regression run; do not sum them.
Coverage source and generated Cobertura are retained locally at
`web/Nexora.Web/artifacts/coverage/cobertura-coverage.xml`; the command log is
`../evidence/oct25-frontend/coverage-final.log`. Logs/artifacts are ignored by
repository policy; this report retains the actual result. Coordinator decides
which exact evidence artifacts to publish.

Cobertura parsing confirms **92 measured production files, 18 with zero covered
lines**. These are code-execution metrics, not feature coverage percentages.

Examples of zero executed lines: RecentsScreen, CommandPalette,
DashboardDrilldownDialog, DocumentMarkdownPreview, DocumentTreeDialog,
FileMetadataDialog, goalNumbers and several Automation dialogs. Collections,
Course, Skill, WorkLog, URL/header and offline-tool helpers have very low line
coverage. Existing suite green is demonstrably insufficient for full module
coverage. Backend and E2E coverage do not follow from these component metrics.

## Actual execution

Windows local; Node 24.17.0; existing installed repository dependencies; Vitest
5.0.0/jsdom. Commands executed in `web/Nexora.Web`.

| Source | Command / layer | Actual result | Evidence |
|---|---|---|---|
| Starting HEAD, unchanged frontend | `npm run test:unit -- --reporter=dot` | 25 files passed; 201 passed, 1 existing skip; 64.88s | Tool execution session 88899; not reused as post-fix evidence |
| Added cases before production fix | `npm run test:unit -- src/NewsCategoryScreen.test.tsx --reporter=dot` | **8 failed / 16 passed**, exit 1; all 8 new cases fail `toBeDisabled`; 12.73s | Tool execution session 49428; failing history retained here |
| Final bounded fix | Same focused command | **24 passed / 0 failed**, exit 0; 13.40s | Tool execution session 30629 |

Final regression: `npm run test:unit -- --reporter=dot` exited 0: **25 files passed,
209 tests passed / 1 existing skip**, 19.55s. Retained log:
`../evidence/oct25-frontend/vitest-final.log`. Existing React act warning in
ResourceFormDialog's dirty navigation test remains visible and is not hidden.

Final compilation: `npm run build` exited 0; TypeScript and Vite succeeded,
379 modules, 5.46s Vite build. Retained log:
`../evidence/oct25-frontend/build-final.log`. Main bundle 1,120.15 kB / 288.97 kB
gzip; existing >500 kB advisory remains. No measured performance conclusion.
A focused run is a subset of the
full suite, never additive coverage. These are component tests with mocked API
boundaries; they do not prove SQL transaction/idempotency or E2E behavior.

Source hashes after fix (SHA256):

- `NewsCategoryScreen.tsx`: `C746D606F87F70BD97434C775CEA592BB8A4B7EBEBFC0940A6AC1B0B5E307565`
- `NewsCategoryScreen.test.tsx`: `C8C9DBE5924B636DECC59A25C32E28121C1C1FC65807B1587DA782D5ABC77B2D`

Coordinator independently reviewed the bounded status change: no source blocker
found; malformed ACK status0 and existing denial clearing are preserved. This is
source review, not independent runtime verification.
No commit/push performed by this worker. No integration/API/DB/browser/E2E or
performance/WCAG verification performed by this worker. News categories and all
other reviewed features remain **NOT VERIFIED** for full release readiness.
