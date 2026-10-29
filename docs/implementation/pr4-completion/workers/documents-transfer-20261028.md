# Existing Documents MD/DOCX transfer verification — 2026-10-28

Owner shopping_completion; coordinator root; PR4 branch `impl/m01-s00-scaffold`, HEAD `a0215f7b3e85d33503cff9077b41cc065240cb81` plus worktree. Current DEC014 authorizes isolated synthetic automated QA; no Files ACL/security boundary bypass, real provider, unstarted format or production deployment. No production edit approved without reproduced defect and root review. Browser writer requires coordinator release. Operator read-only additions coordinated with root, not edited by this worker.

Loaded engineering/profile/Technical Lead and matching backend/frontend/security/database/verification/owner-isolation routing; current scope/goals, FX10/20, normative features95, action10/20, UX20 create/editor/history and existing export/import technical contracts under remediation-plan-20261016. Historical code-only/human-executes-QA sections in those contracts are superseded by current DEC014; semantic/security boundaries remain.

Exact binding: FX10 BR001/002/003, FX20 BR002/003/008 and applicable AC001/004; P03-CNT002/003/008/009 and P03-VER001; NXG-FX10-G01/G02, NXG-FX20-G01/G02/G03, NXG-SYS02/03/05/07/08/13/14. Actions `documents.format.import`, `documents.page.create`, `transfer.import.preview/commit/read`, `files.file.read/download/upload`; `documents.format.export`, `documents.page.read`, `transfer.export.request/read/download`, dynamic history for old versions. No Support/Emergency export.

## Bounded existing contract / planned execution

Existing native DocumentExportDialog → registered DocumentExportEndpoints → SqlDocumentExportService creates synchronous private artifact for exact saved version; preview no mutation, explicit format/warnings/confirmation, opaque UUID replay, current owner/source/action on status/download. Exact MD bytes and supported DOCX XML content will be inspected from actual browser download, not inferred from Ready status. No general Files artifact or public URL.

Existing DocumentImportPanel → clean owned Files read participant → DocumentImportCodec → SqlDocumentImportService creates only new Draft with explicit type/mode and version1. Preview/apply edits memory only; final manual Save is confirmed/keyed and atomic with provenance. Same command/key replay returns opaque same page identity. No overwrite, inferred immutable mode/type, auto-save or remote converter.

Planned cases for each MD/DOCX: normal UI source creation/export preview+confirm/download → real Files UI upload/scan of downloaded bytes → explicit new-page type/mode and source-picker preview → apply-without-Save proof → manual import/reload/API-body+SQL-owner/version/provenance. Negative unsupported format, wrong owner job/content/source, current capability revoked download, and scanner-rejected script payload remain exact applicable boundaries. No change of ScanState/Clean flag or direct file storage access. Files runtime permission failure is BLOCKED, never bypassed through SQL.

Source services recheck current actor/owner/actions/source revisions in SQL unit; immutable receipt/preview/current-authority and clean byte digest enforced. Technical budgets and lossy warnings remain visible; roundtrip supports basic semantics only, not general DOCX fidelity. Separate Files three-principal ACL acceptance remains pending and cannot be proven by same-principal upload/browser tests.

Status: contract/source discovery complete; tests authoring. No new browser run or transfer acceptance PASS recorded at this checkpoint. Independent security/format review and human product acceptance remain pending.

## Actual initial execution and failure preservation

Authored `functional/document-transfer.spec.ts`,2format scenarios. Strict tsc passes after correcting UUID-header type handling with a real UUID assertion; no production logic changed. Normal Ready-only module policy/user grants use SuperAdmin preview/commit; cleanup attempts every owned restoration step independently and aggregates errors.

1. First desktop run **2FAIL** at an oracle selector: wrapped `<label>Format<select>...</select></label>` does not have exact name `Format` under this label API. Corrected only test locators to scoped anchored combobox role/name for Format/Loại file/Cách áp dụng. Assertions retained. Preserved `artifacts/document-transfer-review/desktop-initial-{junit.xml,results.json}`.
2. Second desktop run **2FAIL / environment**: MD reached actual source create, export preview, explicit generation, same-command replay and exact downloaded MD-byte comparison; then UserB login failed `ERR_CONNECTION_REFUSED` to localhost5173 as runtime ports went down. DOCX could not start login. No complete transfer case PASS. All owner grant/policy cleanup steps were attempted but API was unreachable, so restoration is **NOT VERIFIED**, escalated to coordinator; no silent success. Preserved `desktop-runtime-down-{junit.xml,results.json}`. No trace ZIP was available to recover the original in-memory access snapshot from this failed run; coordinator must reconcile the known owned runtime baseline before rerun.

Safe synthetic pre-mutation access checkpoint added for subsequent runs (`<project>-<syntheticUserId>-access-before.json`) with only module IDs/states/original user enabled bits. No passwords, cookies, headers, tokens or private storage paths. This test harness recovery improvement does not retroactively prove restoration of the failed run. Shared browser writer released while coordinator restores owned runtime; mobile not yet executed. No Files scan or ACL gate was altered.

## Fresh isolated execution and observed UI remediation — 2026-10-29

Coordinator stopped and removed only the old SID/process/SQL-marker-owned test environment and created a new runtime `nexora-e2e-audit-cf6948078be746e7a431fb2fecba5f47`, dedicated SQL Server database `Nexora_Test_cf6948078be746e7a431fb2fecba5f47`, 96 migrations and8 generated synthetic accounts. This resolves the operational environment without retroactively verifying the interrupted run's original policy/grant restoration.

Actual desktop attempts preserved under ignored `artifacts/document-transfer-review/`:

- `desktop-upload-oracle-*`:2FAIL, test awaited POST content while actual `rawUploadFetch` uses PUT. Real upload was not replaced; only the response predicate corrected.
- `desktop-docx-escaping-oracle-*`:1PASS MD /1FAIL DOCX because DOCX-to-Markdown correctly escapes literal hyphens. Corrected exact expected escaping and strengthened MD exact-body and actual rendered semantic paragraph assertions. Production codec unchanged.
- Before presentation edits, final desktop2PASS (25.2s), mobile2PASS (21.0s):2unique compound scenarios,4viewport executions. Every run restored and asserted Ready state, original policy bits and original user grant bits, with no cleanup error. No scan flags, ACLs or permissions bypassed.

Each successful scenario executed normal create/export preview+explicit confirmation, actual artifact download, exact MD bytes / bounded OOXML contents, SQL digest/length and unchanged source, same-key opaque replay, foreign owner404 and current-module revocation403 with private draft clearing. It then used native Files upload/scan, foreign file preview404, native source picker/parser preview, Apply without persistence, explicit manual Save, same-command replay, unconfirmed409, SQL Draft/version1/type/mode/owner/provenance, exact one-page lookup and reload/body equality.

Actual12 desktop/mobile screenshots were opened and inspected. Export modal fits viewport, but its primary action flowed inline after a multiline confirmation on mobile. Desktop editor/import rail was approximately338px beside a dominant library; nested content heading wrapped awkwardly. Coordinator authorized bounded presentation remediation: exact Documents workspace class, scoped layout/preview styles, and DocumentExportDialog layout/copy only. Editor now uses the full content width with library following below, no sticky tall form; export uses spaced confirmation and separate action rows, localized labelled browser time and understandable file-generation/status copy. Request/response/capability/confirmation/replay logic unchanged. Existing transfer assertions additionally require >=95% workspace editor width, static editor positioning, >=8px action separation and44px primary target.

Before images and execution JUnit are curated under `evidence/documents-transfer-20261029/before-*`. Corrected-presentation browser reruns and visual acceptance remain NOT TESTED at this checkpoint, waiting exclusive browser writer. Standalone strict TypeScript compile of `functional/document-transfer.spec.ts` PASS exit0; full app tsc currently has concurrent unrelated test-authoring diagnostics reported to coordinator, never counted as PASS.

## Final bounded result — corrected UI and export recovery

**Scope result:** the3 authored transfer/recovery scenarios are runtime verified on the integrated worktree; this is not complete FX20/R1 acceptance. Human UI review and final delivered-revision regression/CI remain pending.

| Scenario | Desktop1366×768 | Mobile390×844 | Actual evidence |
| --- | --- | --- | --- |
| MD export → real Files scan → preview/apply → manual import/replay → reload + denial/SQL checks | PASS | PASS | corrected JUnit, exact byte/hash proofs |
| DOCX export → bounded actual OOXML inspection → real Files scan → rendered semantic conversion → manual import/replay → reload + denial/SQL checks | PASS | PASS | corrected JUnit, artifact digest/length proofs |
| Real export commit followed by HTTP408 response loss → frozen original UUID key/If-Match/body hash replay → original persisted job identity | PASS | PASS | corrected JUnit; preserved before-fix failing JUnit; frozen-state images |

Final command from repository root: `.tmp/run-nexora-browser.ps1 -Suite functional -Project desktop -Grep 'Documents (transfer|export committed HTTP408)'`:3PASS /0FAIL /0skip,25.5s. Mobile same command with `-Project mobile`:3PASS /0FAIL /0skip,21.9s. These are3unique cases /6viewport executions, not additive with the preceding baseline/retry runs. All final policy/grant restoration assertions completed without cleanup errors.

### Defect fixed

**DOC-REC-408 — Medium:** DocumentExportDialog classified HTTP408 as a definitive4xx failure and discarded its frozen export intent even when the server had committed. Real `route.fetch` produced201/SQL artifact before the first response was replaced with408; the original UI then had no retry action (actual failed desktop test,12.4s). Minimal fix excludes408 from terminal classification, retaining original preview/ETag/accepted warnings/body/key. All other authorization, validation, confirmation and error classifications are preserved. Exact replay assertions compare UUID key, original If-Match, SHA256 of the entire request body and returned job UUID; source SQL row is unchanged throughout and original job remains persisted after reload. Read-by-ID is not claimed as a complete orphan-row count.

An earlier fault-test harness assertion raced the real route response and caused teardown; that failure is preserved in ignored `desktop-408-harness-race-*`. It is not the product reproduction. The harness now awaits the actual408 response before checking retained recovery state. Exact saved FX07/10/20 policy/grant checkpoint was restored through normal SuperAdmin preview/commit and fresh reads (`desktop-408-harness-restoration.json`); no fake Ready or direct grant SQL. Separately restored the earlier interrupted Skill run's exact FX40 checkpoint via legitimate APIs; coordinator was informed and proof is ignored `artifacts/started-access-restoration-20261029.json`. These current verified restorations do not rewrite earlier failed evidence.

### Visual verification and changed files

- `src/App.tsx`: only Documents resource-layout gains `documents-workspace` for this worker's change; other coordinator changes remain untouched.
- `src/styles.css`: Documents-only fullwidth stacked editor/library, static tall editor, bounded readable preview heading; namespaced portaled export spacing/actions/checkbox alignment. Existing tokens/components, no dependency.
- `src/DocumentExportDialog.tsx`: explicit spaced warning/confirmation/action groups; readable local expiry labelled browser time, private job UUID removed from ordinary copy, clearer file labels;408 recovery above.
- `functional/document-transfer.spec.ts`: actual MD/DOCX journeys, owner/current-action negatives and SQL/byte proofs; source-backed PUT/escaped Markdown oracle corrections; responsive layout/44px/action-spacing guards and actual post-commit408 fault/replay case.

Opened corrected desktop workspace/export/import-preview/imported screenshots and mobile equivalents plus desktop/mobile frozen408 screenshots,10actual rendered images. Desktop editor is no longer a squeezed rail and content headings no longer dominate narrow boxes. Mobile export primary action now occupies its own spaced row below confirmation and fits within the normal viewport; import/editor stack coherently with no horizontal overflow. Retry state remains usable in the scrollable modal. Subjective human design acceptance is pending. Existing technical import copy may be refined later; no preservation/loss warning was removed.

Curated sanitized before/after images, final per-viewport JUnit, before-fix408 JUnit, export digest/length proofs and exact source hashes: [`evidence/documents-transfer-20261029`](../evidence/documents-transfer-20261029). `execution-manifest.json` identifies current base/worktree and environment; coordinator must reconcile to the final delivered commit and invalidate affected evidence if relevant code changes. Raw credentials, tokens, cookies and browser storage are excluded.

Standalone strict compile executed from `web/Nexora.Web`: `node node_modules/typescript/bin/tsc --noEmit --strict --skipLibCheck --target ES2022 --module ESNext --moduleResolution node --jsx react-jsx --esModuleInterop functional/document-transfer.spec.ts` → PASS exit0. An initial invocation from repository root could not resolve web-local Node type roots; rerunning from the package root resolved the invocation error. Full app tsc attempts exposed concurrent SkillRecovery/AutomationRecovery test-authoring diagnostics, reported to their owners; this worker does not count those full-app attempts PASS.

Independent reviewer `independent_finish` reviewed the scoped presentation delta and requested/asserted If-Match equality plus bounded artifact-count wording, both addressed. Independent408 logic final review is requested. Remaining gates: complete parser/adversarial/expiry/history/revocation-race matrix beyond these scenarios, Files separate three-Windows-principal ACL proof, affected final broad regression, delivered-head trace/CI, human UI/product acceptance. No new provider/network activation, migration, auth weakening, production operation or merge.
