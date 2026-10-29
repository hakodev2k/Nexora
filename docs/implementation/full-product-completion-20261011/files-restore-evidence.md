# FX07 — Cohort-bound Restore

Main: 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3. PR4 parent: ca003bd6c85805d1cf477d1a2c038c0016b63d6d. Scope: files.file.restore SELF with files.file.read; FX07-S01, FX-07-BR-001/002/003/006, lifecycle and UX07/UX08 Restore contract. Goals NXG-FX07-G01/G02/G03 and owner isolation, durable effects, concurrency and audit system goals. Existing local implementation and relevant verification authorization; no purge/provider/production permission.

## Actual behavior

GET /api/v1/files/{fileId}/restore-preview uses current read AND restore authority and owner scope, resolves exactly one current trusted deletion cohort, checks Clean/Trash and owner-bound retained binary availability/length. Invalid storage paths fail closed without raw errors. No binary fabrication.

POST /api/v1/files/{fileId}/restore requires nonzero UUID Idempotency-Key, If-Match and exact deletionBatchId. Serializable owner locking and current authority guard atomic lifecycle Active, cohort RestoredAt, audit and SQL receipt. Receipt input binds owner/file/cohort/ETag. Replay rechecks authority and returns current metadata without repeating the effect; an old receipt cannot revive a newer Trash cohort. No binary deletion or rewrite.

FileRestoreDialog is reachable from the real Files Trash filter. It loads actual preview, confirms the trusted cohort, handles denial/412, freezes uncertain file/cohort/ETag/key, and retries that exact command. Only HTTP200 with matching file ID, canonical 8-byte rowversion and valid current lifecycle is acknowledged. Current list is reloaded; no optimistic fabrication. Existing permission0021/schema/DI are reused, no new migration or checksum change. Purge remains inactive.

Production files: src/Nexora.Application/Files/FileServiceContracts.cs; src/Nexora.Infrastructure/Files/SqlFileService.cs; src/Nexora.Api/Features/Files/FileEndpoints.cs; src/Nexora.Infrastructure/Authorization/SqlSelfCapability.cs; src/Nexora.Domain/Access/ActionGrantPolicy.cs; web/Nexora.Web/src/FileRestoreDialog.tsx, App.tsx and api.ts.

## Revision-bound evidence

- PASS isolated publication API Release build: 0 warning/error.
- PASS publication TypeScript/Vite build; pre-existing bundle-size warning.
- PASS backend unit47/47, no skip. Initial isolated unit invocation found no copied test project; copied unchanged project and reran successfully.
- PASS frontend119, one existing skip.
- PASS real SQL focused Files workflow1/1 after storage hardening: foreign owner404, wrong cohort404, stale ETag412, successful restore/replay, original binary download, later Trash and old Restore replay preserving Trash, exactly one restored and one current cohort, no cleanup.
- PASS browser5/5 across1920/1366/768/390/320: real upload/scan/Trash and Restore, both real commits with lost ACK, exact-key/ETag/body retry, current Trash/Active list, original retained binary.
- PASS independent source review baseline_review: owner-bound invalid-path handling and canonical ACK hardening confirmed; no remaining source blocker. No runtime claimed by reviewer.
- PASS 47 existing migrations replay; retained core counts unchanged. No retained database dropped/reseeded.
- NOT_RUN full SQL/browser all-module regression, missing-binary corruption fixture, mid-command revocation/concurrency and audit rollback fault injection. CI environment/Windows filesystem gates not claimed passed. Release1 and FX07 overall completeness not claimed.

Task owner root. Publish only selected Restore source/test/evidence against current PR4 head; preserve pending Sharing edits and private runtime files. Next missing Files slice: reference management/replacement, subject to its exact source and retention contracts.
