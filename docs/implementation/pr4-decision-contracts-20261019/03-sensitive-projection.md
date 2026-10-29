# D05=A — Sensitive field projections are denied by default

Source of authority: DEC-20260909-005 and DEC-20261019-PR4 D05=A; FX04 Sharing/FX05 Support/Emergency, `docs/action-catalog/00-authorization-contract.md`, FX27/28/31/37/38/39/40 features, `docs/design-database/15-field-classification.md`, `docs/features/04-read-only-sharing.md` and `docs/features/05-support-emergency-and-security-center.md`. **This contract is executable for default-denial, redaction, and review workflow; it is NOT blanket approval to expose sensitive fields.**

## Roles/contexts are different principals, not serialization modes

| Context | Eligible operation | Hard boundary |
|---|---|---|
| SELF | Current owner-authenticated read/mutation with precise module.action and current source role | May read original private fields only when exact action permits, not by association with a permitted ShareLink or admin menu |
| LINK / SHARE | Viewer with current valid unrevoked token and audience/expiry/source-approved projection | Read-only approved subset. Default deny sensitive fields; owner explicit field selection, preview, source share action and revision proof mandatory before any field is eligible |
| SUPPORT | Authorized Admin, active owner consent for exactly one module, limited safe diagnostics | Never reveal business payload, personal notes, credentials, Vault fields, raw files or link tokens; no mutation/export/secret copy |
| EMERGENCY | Fresh qualified SuperAdmin with request-bound documented reason/timebox, durable audit before access | Read-only safe metadata; no secret reveal/export/copy, impersonation, cross-module inherited access or bypassed source classification |
| RECOVERY | Separate Vault request-specific authorization (D01) | No operator secret plaintext; status-only recovery workflow after independent ADR review, not an extension of SUPPORT/EMERGENCY |

## Source projection registry (deny-until-approved)

Each resource module must own a typed registry entry, not generic JSON serialization/reflection: `resourceType`, `moduleCode`, `schemaVersion`, exact readable fields with classification (`PublicSafe | OwnerPrivate | Sensitive | Secret`), source action set and lifecycle, allowed context (`SELF/SHARE/SUPPORT/EMERGENCY`), current resource revision and optional file/version pin policy. For `LINK`, no entry ⇒ unavailable; a missing field/unknown schema/version ⇒ omit or fail closed as dictated by contract, never pass through DTO field.

| Source families | Explicitly denied for SHARE/SUPPORT without individually reviewed field contract | Current path that may continue |
|---|---|---|
| Finance | Amount, currency with personal context, category/note/date/detail/financial totals, account/ledger metadata; never expose from raw `FinanceRecord` DTO | `finance.manual_record.*` own SELF flows |
| Personal/Digital Assets | Serial numbers, license keys, domain/hosting identifiers when private, invoices, locations, cost, vendor credentials, renewal secrets | Current owner metadata and management |
| Career | Job applications, salary, contacts, interview feedback, resume versions/files and full private profile; public resume requires separate exact pinned projection | Current own Companies/Jobs/Appointments |
| Learning | Skill evidence private details, employer/project notes, course progress, certification credential ID, WorkLog notes/minutes/source details | Current own Skills/Courses/WorkLog; plan under D06 |
| Shopping | Order/payment/shipping fields, sellers' sensitive contacts, warranties/invoices/ownership links | Current own Wishlist/approved list |
| Documents/Files | Draft/Trash/nonpublished text, hidden version content, quarantined files, owner-private tags/attachment metadata, unapproved historical snapshots | Approved Published safe Documents/Project projections still governed by current FX04 contract, not automatically restricted by this sensitive-source gate |
| Vault | **All ciphertext/plaintext/keys/recovery data**, except a separately reviewed non-secret diagnostic enum with no owner existence leakage | D01 docs-only; no Vault share/reveal at all |

**No values in this table are an allowlist**. Support metadata like coarse category, availability, error code, and sanitized timing are candidates only; exact attributes require a source-specific signed projection matrix, positive/negative test cases and owner preview where SHARE is requested. Existing Project/Published-Document shares keep their pre-existing exact approved provider contracts and permissions; do not disable compliant non-sensitive views as collateral damage.

## API/security request contract

Existing FX04 endpoints include `GET /api/v1/sharing/preview`, `GET /api/v1/sharing/links`, `POST/PATCH/DELETE /api/v1/sharing/links...` and `GET /api/v1/sharing/resolve/{token}`. The *preview* for sensitive sources must be a current proof, not a grant: proposed typed payload `{resourceType, resourceId, sourceRevision, fields:[{id,classification,selectable,reason}], allowedScopes, expiresAt?}`. It must not include values when the viewer/owner is not currently entitled to them. Unknown resource/field returns safe unavailable; newly approved fields require an explicit opt-in form and current policy revision at create/update. **Do not add such fields or enable sensitive source providers until per-source contract approved.**

Share creation/resolve sequence: authenticated owner/module/share capability → same-owner source and eligible lifecycle → source-specific allowlist+field selection+files/version rights and sharing epoch → hash-only token identity + fixed expiry/mode/audience → atomic persisted link/audit/receipt; resolver rechecks token, current owner/source/module/epoch, projection fields and file authority in one consistency boundary. Any revocation, missing provider or source deletion yields safe 404/Unavailable without titles/counts/field names. Previously issued token never revived by restore/re-enable.

Support/emergency sequence: validated admin role + user consent or qualified reason/fresh step-up → audit intent **before** access → current module and source-safe diagnostic provider + redaction applied before count/pagination → bounded response with no raw source DTO, no copy/export; audit completion and notification intent. Denial cannot accidentally return previously cached private data.

Mutation responses: current read entitlement is separate from write entitlement. Do not serialize a full source DTO on a write-only grant, including idempotent replay. Use fixed opaque resource/action/revision acknowledgement when current read proof unavailable.

## Proposed SQL/data metadata, not migration authorization

- Per-source typed projection definitions in application code (reviewed static manifest or versioned whitelist) rather than owner-editable permissions that bypass feature policy. SQL link stores minimal `ProjectionVersion`, explicit selected field IDs (allowlisted), source revision/epoch and owned reference/receipt, not copied sensitive values. Projection metadata must be revalidated on each read; stale schema/version disables link rather than broadens fields.
- Composite OwnerId/resource FK where applicable; `ShareLink.TokenHash` only, no token plaintext storage; file version pins require clean scan/result and current rights. Unique/paging/cursor filters applied **after current authorization**, before any count exposure. Immutable audit stores safe field IDs/diff/status, not values.
- Rollback keeps token epoch/revocation tombstones, prevents re-enabling old fields/links and preserves proofs of explicit owner opt-in. Never loosen a default-deny projection during downgrade.

## UX acceptance

Owner sees locked “Sharing unavailable for this type until projection reviewed”; eligible resources show field classification preview, opt-in unchecked for sensitive fields, exact audience/expiry and impact confirmation. Unsupported fields are not rendered as selectable. Share viewers see only permitted fields and a noindex, no-referrer, no external auto-open view; restricted audience/wrong owner/expired/revoked token gets uniform unavailable state. Support/Emergency screens display why a field is not eligible and never show copy/download for protected data. Clear displayed values/caches on revoke/session/module/source changes and failed protected refresh; never imply “empty success” on a denied read.

## Required negative/positive tests before enabling each source provider

1. Absent manifest, unknown source type/field, unsupported schema version and unapproved classification cannot create or resolve a sensitive share.
2. Owner only sees fields they are entitled to read, preview is never a second channel to reveal payload; canceled/dirty preview does not write.
3. Valid approved field selection yields only that whitelist, for all LINK modes. Probe JSON keys/values, nested objects, stringified source DTOs, file/content/title and count side channels.
4. Wrong-owner, revoked/expired/disabled source, trash/archived/draft policy, epoch bump, audience mismatch, module/action Deny and session invalidation all fail without stale projection.
5. User revokes mid-list, mid-file download, mid-workflow; current authority checked at effect/read boundary, no cached/stale title or source body.
6. Support and Emergency cannot bypass per-module scope; no Vault/Finance raw details, credential key, owner history, account privileges, copy/export or unauthorized work.
7. Security reviewer approves per-source field classifications and response/pin/file policy; actual isolated SQL/browser tests and independent signoff on final source SHA.

**State:** `DEFAULT_DENY_CONTRACT_READY`; `SENSITIVE_FIELD_ALLOWLISTS_NOT_APPROVED`. Different source families may be implemented independently; an unapproved sensitive projection blocks only that consumer, not normal SELF CRUD.
