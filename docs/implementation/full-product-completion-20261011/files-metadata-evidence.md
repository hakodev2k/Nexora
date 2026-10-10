# FX07 — Current owner file metadata detail

Source main: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; PR #4 parent: `5da91ac47bb47f5c37313dbe115983ee7d7a6e61`. Accountable owner: root implementation agent. User authorizes continued local production implementation; live providers/production remain unapproved. Evidence binds to the containing source commit.

Scope: `files.file.read` SELF, metadata portion of FX07-S03; NXG-FX07-G02 and NXG-SYS-02/03/08/13/14, inherited FIL-001..006, feature FX-07-BR-002/004/006. No content access, schema/migration, permission expansion, external effect or lifecycle mutation. Rules: repository baseline and frontend routing paths in `.ai/routing.json`; mandatory independent file-boundary review performed by baseline_review.

## Production behavior

A Chi tiết action in Files library opens FileMetadataDialog and reads current metadata through existing protected GET `/api/v1/files/{fileId}` and capabilities API. List metadata is not reused as a successful detail response. The dialog displays filename, MIME, exact bytes, scan/lifecycle/revision and created/updated times, with refresh, loading, generic fetch-error and close states. Closing preserves library filters and selection.

Current read denial/401/403/404 clears detail and parent library/capabilities, then refreshes current authority. Generation checks discard late results after refresh/unmount/StrictMode cleanup. Malformed capability, identity, numeric, scan/lifecycle or timestamp responses are rejected. React escapes field values; no binary, storage key, source references, signed URL, HTML interpretation or browser persistence is introduced. Existing resource-dialog focus trap/return and Escape handling are reused; read loading may be cancelled by closing.

Production files: `web/Nexora.Web/src/FileMetadataDialog.tsx`, `web/Nexora.Web/src/App.tsx` (Files wiring only). Existing backend owner/PersonalSpace and current action checks remain authority. No API route is newly registered.

## Actual verification

- PASS: isolated publication snapshot `npm run build` (TypeScript/Vite), exit0 on final component and exact parent App plus scoped wiring. Existing bundle warning remains.
- PASS: independent source review; strict capability, timestamp types and state vocabulary findings fixed and re-reviewed; reviewer whitespace check exit0.
- NOT_RUN: new unit/API/real SQL/browser functional acceptance and full regression. Code-only execution amendment remains applicable; no new tests/fixtures were created and no functional pass is claimed.
- Prior parent CI `5da91ac`: frontend-unit/backend-unit/filesystem-linux success. SQL/API, browser and Windows ACL jobs failed their existing gates. These results do not verify the new dialog.

Implementation: IMPLEMENTED_UNVERIFIED. Functional acceptance: NOT_RUN. Security: source review passed; runtime proof pending. Provider activation: NOT_APPLICABLE. Production readiness: not established.

Remaining FX07-S03: reference count and authorized reference inspection, supported sandboxed content preview, download/detail integration and other secondary/lifecycle/deep-link actions. This metadata subset does not complete the screen, module or Release1. Runtime acceptance still requires current/denied owner responses, malformed/late responses, refresh/close and keyboard/mobile journeys. Rollback is reverting these two UI source changes; no persisted data is changed.
