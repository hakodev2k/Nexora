# FX07 — Owner Files library paging and filters

Authoritative main: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; PR #4 parent `265e9f37125119cbdf885b8b45c1910674510349`. Evidence is bound to the containing implementation commit.

Scope: `files.file.read`, FX07-S01, Files UX sections12/15, inherited `FX-07-BR-001/002/004`, `FIL-001..006`, NXG-FX07-G01/G02/G03. No new permission, schema change, migration, fixture, file content access, network/provider effect or lifecycle mutation.

## Implemented production workflow

GET `/api/v1/files` accepts cursor/query/mediaType/scanState/lifecycle. The SQL owner query implements literal filename search, MIME and scan filters and explicit Active/Trash metadata selection. It reads TOP26 in UpdatedAt DESC/Id DESC order and returns25 with continuation. Existing numeric limits1..100 remain syntactically accepted; effective page size is25. Invalid filters/limits are422, invalid/stale/cross-owner/filter-mismatched cursors404. Purged records are excluded.

The74-character versioned AES-GCM cursor encrypts timestamp/ID under a domain-separated runtime key with owner and canonical normalized-filter JSON as authenticated context. Random nonce/tag, canonical encoding and bounded decoding prevent tampering. SQL verifies the exact owner/filter/timestamp anchor in the current Serializable transaction. Module/action and live actor authority are checked before and after materialization. Secret rotation/restart may invalidate old cursors; refreshing restarts safely. Pagination is a current keyset traversal, not a snapshot guarantee under concurrent edits.

The React Files library applies real server filters, appends subsequent pages without duplicate IDs, resets continuation on query/reload/rename, shows authorized UpdatedAt, and separates loading/fetch-error/empty selection. Filter/upload/reload controls are blocked while a rename dialog is open. Denied data clears immediately; continuation is hidden after fetch errors. Trash has metadata only: this batch does not enable restore/download/rename from Trash.

Production: `src/Nexora.Application/Files/FileServiceContracts.cs`, `src/Nexora.Infrastructure/Files/SqlFileService.cs`, `src/Nexora.Infrastructure/Files/FileListCursor.cs`, `src/Nexora.Api/Features/Files/FileEndpoints.cs`, `web/Nexora.Web/src/FileListFilters.tsx`, `web/Nexora.Web/src/App.tsx` (FilesScreen/import only), `web/Nexora.Web/src/api.ts` (listFiles only). Existing owner/lifecycle/UpdatedAt index is reused.

## Validation and limits

- PASS: API Release build on working source and isolated publication snapshot;0 warnings/errors.
- PASS: frontend TypeScript/Vite build on the working source and isolated publication snapshot. The first isolated snapshot build failed because an unrelated uncommitted FX04 component remained in the copied build directory; excluding that transient copy corrected the publication scope. Existing bundle-size warning remains.
- PASS: independent SQL/security/source review after fixing cursor reset/error continuation and missing UpdatedAt UI findings; diff whitespace check passed.
- NOT_RUN: new unit, real SQL/API integration, browser acceptance and full regression for this slice. Testing-scope clarification following automatic code-only review rejection is still pending. No passing functional evidence or SQL replay/concurrency acceptance is claimed.

Implementation: IMPLEMENTED_UNVERIFIED. Functional acceptance: NOT_RUN. Security: source review passed; runtime proof pending. Real-provider activation: NOT_APPLICABLE. Production readiness: not established.

Remaining acceptance: real SQL25/26/51-row pages and same-timestamp GUID ties, owner/grant revocation, tamper/filter/cursor movement and secret rotation, literal special-character search, current Trash metadata, concurrent rename/reload, browser paging/filter/error/denied states and affected regression. Remaining Files replacement/reference/lifecycle/scan-worker work is outside this batch.

## Follow-up production regression repair

Existing frontend CI at PR HEAD `0bfec60348eb6c0a48564cea6c5dfbf47e9795ed` reported118 passed,1 failed,1 skipped. The valid Files loading/empty-state test failed because the default empty library displayed the filtered-empty message. FilesScreen now distinguishes default empty (`Chưa có file`), filtered empty, denied, loading and fetch-error states. No test was edited or added. Isolated publication TypeScript/Vite build passed; rerun CI results are pending at publication. This repair does not establish SQL/browser functional acceptance.
