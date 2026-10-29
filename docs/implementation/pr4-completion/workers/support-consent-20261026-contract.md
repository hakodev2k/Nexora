# Bounded existing Support owner-consent recovery contract

**Oct27 execution amendment:** root approved the technical direction, then actual fresh-SQL discovery proved FX05 remains `Blocked` by migration0018. This plan is conditional remediation, not shipped behavior or activation authority. Root explicitly retained the gate: no Support production receipt/API/App edits this increment. Positive SQL/browser consent and replay remain BLOCKED. See [actual gate execution](support-gate-20261027.md). The earlier staged-snapshot assumption below is historical; root reset delivery state and no GitHub commit had been published at this checkpoint.

Status: technical plan for root independent security review before replay changes. Author shopping_completion; coordinator/reviewer root. PR4 branch `impl/m01-s00-scaffold`; parent at intake `a0215f7b3e85d33503cff9077b41cc065240cb81` plus root's staged verified increment. Do not stage/reset/commit or change staged delivery. Current DEC014 automated-QA amendment authorizes local implementation/isolated synthetic tests. App/frontend edits are held until root's baseline functional run finishes; backend test host may use a separate newly generated SQL test DB and output directory, without restarting current runtime.

## Requirement / action / goal binding

| Exact scope | Normative sources / goals | Planned evidence |
|---|---|---|
|Owner grant list + confirmed one-module consent|PDS-SUP-001/002/003/005/006/007 (`requirements/08-workspaces-and-collaboration.md`), FX05-S01/S02; `support.consent.read/grant`; NXG-FX05-G01/G03, NXG-SYS05/08/13/14| Default24h, custom future expiry, until-revoked; explicit final any-qualified-Admin/read-only/module/duration disclosure; no request on cancel; dirty leave; SQL owner/audit/single-create proof |
|Owner revoke and existing session end recovery|PDS-SUP-004/007, FX-05-BR-001/004, FX-05-AC-002; `support.consent.revoke`, existing `support.session.end`; NXG-FX05-G01/G03| Frozen original UUID/body/ETag; rapid-submit latch; unknown-response retry; stale revision +reload; current-authority clearing; foreign-owner denial and SQL no unintended changes |
|Bounded replay correctness|Common idempotency/current authority contracts; D05 default deny, no sensitive projection; same operation namespaces| Real SQL first failing24Hours retry then PASS; one grant/audit; opaque committed proof after original actor/action/digest/owner validated; legacy/collision/denied current permission fail closed |

Read AGENTS/engineering skill/profile/Technical Lead core; docs/delivery/current scope, goals/evidence template, FX05 feature/UX/action catalog, D01–D09/D05 projection contract, Support contracts/endpoints/SQL, receipt store and current actor/capability helpers. Frontend/security/owner-isolation routing rules/skills apply. Exact sources are `docs/features/05-support-emergency-and-security-center.md`, `docs/ux-ui/modules/05-support-emergency.md`, `docs/action-catalog/modules/05-support.md`, `docs/goals/modules/05-support-emergency-and-security-center.md`.

## Proven source gaps

- Current App SupportScreen posts immediately, omitting normative final consent confirmation that **any currently qualified Admin**, rather than a selected person, may obtain the approved one-module read-only context.
- No synchronous mutation latch, retained uncertain command/key/ETag or dirty-leave recovery; optional API UUID defaults generate new keys on every call.
- Read failures401/403/409 can leave prior grants/sessions on screen. Refresh results lack epoch/unmount guards.
- `SqlSupportService.Grant` hashes `now+24h` instead of immutable request input. Unchanged default body/key changes digest on retry. This is the initial real-SQL fail-first regression; do not hide it through a new operation namespace.
- All existing Support receipt replays produce generic409, and store no strict target/owner result proof. A success response must never replay stale grant content or assume the grant is currently active.

## Proposed API/data/replay contract (root review required)

1. Keep existing operation keys, key hashing and24h receipt storage mechanism. Canonical grant input uses normalized module, duration and **requested** custom expiry; default24h uses null request expiry, while SQL resolves actual24h expiry once for the first commit. Legacy mismatched canonical receipts stay409 conflict; do not dual-write a new namespace or reexecute an unprovable legacy key.
2. Preserve first successful grant201 DTO for existing consumers. New strictly proven grant replay returns201 opaque `{id,committed:true,detailAvailable:false}` through the existing `IdentityOperationResult.Committed`/API acknowledgement pattern. No module title, expiry, current active state, ETag or original DTO in replay. UI refreshes current authorized lists separately.
3. Store only minimal versioned receipt proof atomically with existing grant/revoke/end audit: requester UserId/PersonalSpace OwnerId, operation, exact resourceId and moduleCode. Validate expected stored result code/status and schema on replay. No reason, grant DTO, raw key or personal record body stored. Existing receipts with missing/malformed proof remain409 unavailable; never inferred accepted.
4. Recheck current live actor/owner/session and original action in the existing SQL transaction before receipt/replay and before commit, using current helpers' connection/transaction overloads. Grant also retains its current supported-target/module checks; replay must not expand sensitive source access. Revoke remains possible when the **target business module** is off, while FX05/current action gates remain authoritative. Revoke/end replay returns204 only after matching strict proof/current resource ownership or actual session-actor relationship. Stale original ETag is proof input, not a new overwrite.
5. SupportSessionOpen/EmergencyOpen semantics and legacy receipt handling are outside the new replay path. No new Admin resource read/projection, active Support-mode UI, Emergency entry/recovery, duration/recent-auth policy, field allowlist, grant/status promotion or provider effect. D05/Q02/Q04 remain explicit gates.
6. Validate static input before claim; current-time future validation applies to a **new** custom grant, not a proven historical commit acknowledgement.24Hours/UntilRevoked must not accept a caller expiry that silently changes their meaning. Exact lifetime remains server-authoritative.

Necessary wiring for this contract: `SqlSupportService.cs`, focused new SQL tests; `SupportEndpoints.cs` must forward existing generic acknowledgement instead of discarding it. The API client grant return type must acknowledge DTO-or-opaque-ACK and validate response before claiming success (root may own the narrow shared api.ts edit). No schema migration. Backend changes will not restart the existing app until coordinated.

## UX/state contract

Keep owner metadata scope. One module, default24Hours, optional future custom datetime or UntilRevoked. No Admin picker. Final dialog names exact module and duration, qualified Admin audience, read-only/no export/impersonation/secrets and revocation effect. Confirmation freezes immutable body/key, and revoke/end freeze resource ID+ETag+key. No mutation until explicit confirmation. Default24h disclosure is “24 hours from server confirmation”; show returned exact server expiry after commit rather than fabricate an authoritative client-clock instant. Strict pre-commit server-exact expiry preview is not currently implemented; record that UX gap separately instead of inventing a signed-preview lifetime/business policy.

Unknown0/network/408/5xx/409 RequestInProgress retains/fixes original command, disables editing and competing mutations, and offers same-command retry or explicit informed abandonment. Definitive validation lets authorized input be corrected.401/403/409 ModuleUnavailable (and current missing resource where applicable) clears private selections/draft/data/intent;412 never silently overwrites and requires reload. Confirmed write closes/clears before independent query refresh, so refresh failure cannot imply uncertain write. Async epochs discard stale completions; guard internal leave/unload and duplicate submission.

## Security abuse cases / rollback

Test actor B cannot list/revoke/end A's resources; key collision cannot acknowledge another body/resource/owner; a previously committed but now revoked/expired grant yields only opaque commit proof, never an active-permission claim; disabled FX05 or lost current write action denies receipt replay without resource metadata. No real secrets/data or unapproved provider/session fixtures.

Rollback: revert bounded App/API/service changes; retain committed SupportGrant/audit/receipt data. Do not delete/rewind durable audit or reuse old receipt namespaces. New proof receipts remain safe409 on an older binary; unprovable historical records fail closed. Independent root review and applicable real SQL/component/browser regression must pass before delivery; human UI/product acceptance and D09 still pending.
