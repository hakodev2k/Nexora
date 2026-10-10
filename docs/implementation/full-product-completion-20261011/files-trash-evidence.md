# FX07 — Reference-safe Move to Trash

Main: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; PR4 parent: `11dcb5bc79555d8854f80253a6b5be22f152a7b2`. Accountable owner: root; independent reviewer: baseline_review. Current user request authorizes local implementation and relevant verification (STEP4); no real provider, production, paid service or purge authorization is inferred. Existing PR4 remains the publication target.

Scope: `files.file.trash` SELF plus required `files.file.read`, FX07-S01 and D-TRASH. Sources: feature07 lifecycle/unreferenced file, FX-07-BR-001/002/003/006, FX-07-AC-001/003, FIL-001..006; UX07 sections15/16 and UX-08. Goals NXG-FX07-G01/G02/G03, NXG-SYS-02/03/05/08/09/13/14. Rules: Nexora baseline, backend authorization/security/idempotency/rest-api, SQL transaction concurrency, frontend browser-security/accessibility/form-validation.

## Production contract and implementation

GET `/api/v1/files/{fileId}/trash-preview` uses current authenticated live actor, PersonalSpace and separate read AND trash checks inside Serializable. It returns safe FileRecord plus CanTrash/BlockCode. Only Clean/Active is eligible; FileReference and reviewed retention participant are checked. Missing retention fails closed. Referenced/pinned/retained files are blocked; no source names, foreign metadata, storage keys or automatic detach are exposed. The permitted aggregate is exactly one unreferenced file, without children or binary purge.

POST `/api/v1/files/{fileId}/trash` requires current authority, nonzero UUID Idempotency-Key and If-Match. It locks the owner file, checks lifecycle/scan/revision and references/retention again, then atomically creates one TrashItem/deletion cohort, moves lifecycle to Trash, writes audit and succeeds the SQL request receipt. Live authority is checked immediately before commit. The receipt canonical input binds owner/file/ETag. Same-request replay rechecks current authority and returns204 without repeating effect/audit/cohort, even after a later restore; ACK is a historical command result, not a claim about current lifecycle. The UI reloads current state rather than removing data optimistically. Key TTL is inherited24h; no new retention policy is invented.

File content metadata reads use HOLDLOCK instead of NOLOCK to prevent dirty lifecycle authorization. Download still requires current owner/action and Clean/Active per request. Binary and immutable references are retained; no cleanup job or purge is introduced.

React FileTrashDialog loads actual preview, supports cancel/confirmed command/current reload after412, shows blocked dependency/lifecycle and serializes dispatch. Network, malformed success status,5xx or in-progress outcomes freeze file/ETag/key; retry reuses them. Reload/close/navigation cannot silently create a fresh uncertain command. The optional expectedStatus204 contract in apiFetch validates actual HTTP status while preserving existing CSRF/error handling and all other callers. Only204 is an ACK. Generation fencing, escaped text, current-denial clearing and existing accessible dialog behavior are reused. Trash is disabled while an upload selection or rename is active.

Production source: Application/Files/FileServiceContracts.cs; Infrastructure/Files/SqlFileService.cs; Infrastructure/Authorization/SqlSelfCapability.cs; Domain/Access/ActionGrantPolicy.cs; Api/Features/Files/FileEndpoints.cs; web src/FileTrashDialog.tsx, App.tsx and api.ts. Existing permission0021, schema, indexes, DI, TrashItem, RequestReceipt and audit are reused. No migration is added or applied checksum changed; no broader Files handler is activated.

## Actual evidence and limits

- PASS final isolated API Release build:0 warnings/errors.
- PASS final isolated TypeScript/Vite build; existing bundle warning remains.
- PASS backend unit47/47; no skips.
- PASS frontend119;1 existing skip. Initial isolated runner lacked the existing Vitest config, causing environment failures; copying the unchanged config resolved this. No expectation was weakened.
- PASS final real SQL/API focused Files test:1/1. It verifies upload/current authorized rename, foreign owner404, unavailable restore/purge/inline preview, trash preview, zero-UUID422, same-key two204 replies, exactly one cohort, Trash metadata, denied content and no storage cleanup. The obsolete dormant-trash/rename assertions were replaced with positive and negative assertions for the approved implemented operations.
- PASS browser final revision:5/5 across1920/1366/768/390/320 after restarting the owned isolated runtime with the final HOLDLOCK backend, including real upload/scan, cancel, post-commit lost ACK and same-request retry, Active removal/Trash listing and denied content. Two earlier harness failures were corrected without changing expected404: Node APIRequest did not forward the browser Secure cookie over the loopback proxy; then the Fetch status property was incorrectly called.
- PASS independent source review and whitespace check. Reviewer found actual ACK-status gap; fixed and re-reviewed. HOLDLOCK dependency repair reviewed separately.
- Migration replay47 existing migrations passed with retained core counts unchanged. No retained database was dropped/reseeded. One Windows PowerShell5 helper attempt failed before startup; supported runtime invocation succeeded. A build attempt while the owned API was running failed on locked DLL before tests; only owned isolated processes were stopped, then final SQL/build passed.
- NOT_RUN full all-module SQL/browser regression, explicit reference/history-pin races, audit rollback, permission revocation/concurrency and replay after a real restore. Current restore capability remains inactive and cannot supply that workflow evidence.
- CI/runtime Windows ACL and externally configured CI SQL/browser gates are not claimed passed.

Implementation: selected Trash workflow implemented. Functional acceptance: partial, bounded assertions above; full action AC remains unverified. Security: independent source review and owner negative runtime assertions passed; concurrency proof remains pending. Provider activation: NOT_APPLICABLE. Production readiness: not established. Release1/FX07 are incomplete.

Next product slice: restore with trusted deletion-cohort preview/current authority/idempotency, followed by reference/replace/scan work. Generic global Trash integration, content preview and remaining Files actions remain gaps. Rollback removes exact trash action from capability/grant subset and reverts UI/service changes; persisted Trash cohorts/binaries must remain intact for a reviewed restore path. No automatic data reversal or production action is authorized.
