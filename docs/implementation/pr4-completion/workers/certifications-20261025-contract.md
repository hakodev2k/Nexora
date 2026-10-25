# FX40 certification manual records — technical contract

Source revision: `75b6d9f4f0ac247680e22cc07bf26a4890071ef2`, branch `impl/m01-s00-scaffold`. Owner: learning_completion; root integrates/reviews. Current user explicitly requests completion, synthetic SQL/E2E and UIUX; this supersedes old code-only restrictions. DEC014 and D01–D09 remain binding.

Sources read: required root AGENTS/nexora-engineering/profile/Technical Lead core, routing/baseline and matching backend/database/frontend/security/owner-isolation/verification sources; current delivery, DEC014, DEC-20261019-PR4, system/module goals and task evidence template; FX40 feature, FX40 action/UX catalogs and learning.Certification/CertificationVersion design. Primary bindings: P07-CERF-001, FX40-S04, FX-40-BR-004/006, NXG-FX40-G02/G03, NXG-SYS-02/03/05/08/11/13/14.

## Bounded scope and unresolved dependencies

Implement owner-only manual certification metadata, renewal history and Archive/Unarchive, with real SQL persistence and native list/detail/forms. Exact actions: `learning.certification.read/create/update/renew/archive/unarchive`; history requires read. A renewal preserves earlier dates/metadata in immutable versions and never claims credential verification or provider issuance.

Credential IDs, protected file evidence, Course references, share/support projection, reminder configuration/expiry worker and Trash/Restore/Purge remain explicitly unavailable in this bounded contract. Existing secure credential/history encryption and source file pin/reminder revision participants do not yet provide an approved Certification vertical contract. This increment MUST NOT claim FX-40-AC-003 (one pending reminder after renewal) passed or imply notifications are scheduled. Renewals here have zero reminder intents; a future reminder participant must atomically invalidate its old source revision and dedupe current revision before activation. No accepted capability is removed from R1.

## API / data / security

- `/api/v1/learning/certifications`: GET capabilities; GET list with status (`Active`, `Expired`, `Archived`, `All`), title/issuer query<=200 and owner/filter/date-bound opaque cursor; POST create.
- `/{id}`: GET detail; PUT metadata (title/issuer/verification URL only). `/{id}/renew`: POST new issued/expiry dates plus optional reason<=2000. Dates cannot be changed by generic metadata update.
- `/{id}/history`: GET append-only versions,25/page with opaque owner/source-bound cursor. `/{id}/preview-archive` and `preview-unarchive`: POST current preview; matching transition POST requires explicit confirmation, If-Match and signed5minute actor/owner/id/operation/revision proof.
- Title/issuer trimmed1..200; URL optional<=2048, absolute http(s), no userinfo/control characters, inert display only. Issued/expiry nullable date-only; expiry>=issue when both present. Issuer date/history is entered manually, never proof of authenticity. Unknown DTO fields rejected by existing host strict JSON.
- Stored lifecycle Active/Archived; effective Expired is derived when expiry<current owner-local today. Same-day expiry remains Active through that owner-local date. Today comes from trusted Identity profile IANA zone and server TimeProvider; client never supplies authoritative timezone/date. Date display does not convert date-only values.
- SQL0095 creates learning.Certification and CertificationVersion (same-owner registry/parent FKs, rowversion, date checks, monotonically increasing history version). No credential plaintext/envelope or fictitious file/reminder row. Resource type certifications-manual-v1 is owner-private, no share/search/support. Migration inserts absent action definitions only; no grants or existing permission-status promotions.
- All reads/writes require current SELF SQL capability, owner registry consistency and active session; list filters before pagination. Mutations serialize on current owner, strong quoted rowversion, durable request digest/receipt, current action recheck before commit, audit and registry same transaction. Same request replay returns opaque id/committed ACK only after actor/owner/action proof; no stale private DTO/ETag. Same key/different command409. Missing If-Match428, stale412; current denial403 and foreign/missing404. Archived denies metadata/renew.
- Preview binds revision and operation; commit rechecks current state/authority. No cross-owner fields or ambient role bypass. Audit and receipts exclude title/issuer/URL/date snapshots; snapshots live in private module version rows only.

## UX / verification / rollback

Native `/learning/certifications[/id]` list/detail; expiry-aware statuses/filter, read-only archive, plain inert URL, explicit create/edit/renew dialogs with labelled fields, dirty navigation, immutable same-command unknown-write recovery and current-denial clearing. History is private and separately loaded. Empty/no matches/error/loading/unavailable states distinct. No misleading reminder controls.

Tests: date-boundary/validation and cursor/preview tamper tests; component create/unknown-write/denial tests; real SQL owner/ETag/replay/history/archive tests when isolated fixture available. Builds/typechecks are not these runtime tests. Root must wire and execute final-source API/SQL/browser and independently review before any acceptance.

Additive migration only, no data rewrite or delete. Rollback code disables these routes without removing retained records/history; do not automatically drop tables or rewind migration journals. No production, provider or paid action.
