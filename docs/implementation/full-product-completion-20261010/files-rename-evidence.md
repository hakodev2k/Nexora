# FX07 — Owner file rename implementation

Source authority: main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; implementation parent PR #4 `223c3b0f5e5865aecd085c0ca7f1b94f3b61a31e`.

Scope: `files.file.rename`, FX07-S01, inherited `FX-07-BR-001/002/004`, `FIL-001..006`, NXG-FX07-G01/G02/G03. This evidence belongs to its containing implementation commit. No provider operation, production deployment, merge or binary replacement.

## Production behavior

- Files library exposes Rename only for the reviewed action capability and Clean Active files. The dialog reads current owner metadata, validates filename1..255 and unchanged extension, sends the existing PATCH `/api/v1/files/{fileId}` with ETag and UUID Idempotency-Key, and updates displayed metadata from the authorized response.
- SQL rechecks live actor, PersonalSpace owner, separate read AND rename grants, Clean Active lifecycle and extension inside Serializable before claiming a receipt. Rename changes label only; binary, revision and references remain intact. Audit and receipt commit atomically; missing UUID key is422. Existing receipt replay returns409 and the UI reads current committed metadata rather than repeating the mutation.
- Metadata reads use HOLDLOCK instead of NOLOCK. Unknown transport, malformed ACK and all5xx preserve the exact name/ETag/key for deliberate retry. Revision412 requires comparison and a fresh GET before explicit reapply. Generation fences handle StrictMode/unmount; dirty navigation/close guards protect drafts. Known authority denial clears the parent file projection immediately.
- Exact allowlists, Admin grant eligibility and capability projection add only `files.file.rename`; other dormant Files actions remain inactive. The permission is already registered by immutable migration0021; no schema migration or runtime/data mutation was needed.

Production files: `src/Nexora.Infrastructure/Files/SqlFileService.cs`, `src/Nexora.Infrastructure/Authorization/SqlSelfCapability.cs`, `src/Nexora.Domain/Access/ActionGrantPolicy.cs`, `web/Nexora.Web/src/FileRenameDialog.tsx`, `web/Nexora.Web/src/App.tsx` (FilesScreen/import only).

## Actual validation

- PASS: API Release build,0 warnings/0 errors; repeated on an isolated source snapshot excluding unrelated uncommitted FX04.
- PASS: frontend TypeScript/Vite production build on published-parent App plus only FilesScreen/import and the final rename dialog. Existing bundle-size warning remains.
- PASS: independent source/security review after correcting exact action activation, dirty metadata read and5xx unknown-outcome findings. This is source review, not runtime security verification.
- NOT_RUN: new unit, SQL/API integration, browser acceptance and full regression for rename. Automatic approval review rejected adding preview tests under the older AGENTS code-only amendment; clarification of the latest implementation-task testing instruction is pending. No passing functional evidence is claimed for rename.
- No tests were added/changed for rename; no synthetic fixtures or private runtime state are committed.

## Coverage limits

Implementation: IMPLEMENTED_UNVERIFIED. Functional acceptance: NOT_RUN. Security: reviewed source; runtime/concurrency verification pending. Real-provider activation: NOT_APPLICABLE. Production readiness: not established.

Required next evidence: real SQL authorization/owner isolation, missing/stale ETag, denied rename/read, quarantine/Trash, changed extension, audit rollback, replay and concurrency; browser fresh read, dirty cancel,412 comparison, lost ACK and permission revocation on approved viewports. Other Files lifecycle, references, replacement and scan-worker capabilities are outside this batch and remain incomplete.

Uncommitted FX04 pagination/filter and disclosure preview are preserved locally; this commit intentionally publishes only the independent FX07 source change, without withdrawing those local changes.
