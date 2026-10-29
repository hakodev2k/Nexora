# D01=A — Vault threat model and crypto/recovery architecture (DESIGN ONLY)

**Decision:** DEC-20261019-PR4 D01=A. Product scope FX28; sources: `docs/features/28-vault.md` (FX28 BR/AC), `docs/action-catalog/modules/28-vault.md`, `docs/architecture/07-owner-decisions-security-and-recovery.md` ADR-PO-04, `docs/design-database/07-*.md` / `docs/design-database/17-*.md`, DEC-20260909-004/-014, POAC-004. **Do not implement secret-bearing endpoints, create cryptographic keys, expose plaintext or enable Vault grant/UI as a result of this document.** Existing absence of a registered Vault feature is expected while gated.

## Existing approved invariants

Hybrid recoverability: encrypted owner portability and request-scoped server-assisted recovery; SuperAdmin authorizes but cannot view/export/copy another owner's plaintext; owner-account and item deletion retain ciphertext, historical versions and key material, with no owner/admin purge or crypto-erasure. Support/Emergency is metadata-only/read-only and not equivalent to RECOVERY. There is no zero-knowledge promise, and absent root keys cannot be manufactured by permission grants. ADR-PO-04 currently describes a proposed server-recoverable envelope based on AES-256-GCM with nonce/tag and item/version associated context. **This is an earlier technical design, not tested implementation or final cryptographic sign-off.**

## Threat model to review (before approved implementation)

| Asset / actor | Threat | Required control / evidence |
|---|---|---|
| Owner secret and historical ciphertext | Wrong-owner IDOR, accidental cross-tenant support projection, replayed version | Server-derived `OwnerId`, resource FK, current module/action/read/lifecycle proof within transaction; negative cross-owner tests |
| Owner session/device | Stolen session, forgotten MFA, malicious browser extension | Recent-auth binding, CSRF, idle vault lock, no secret in DOM after lock, consistent error and explicit re-auth policy; MFA recovery independent |
| Operator/SuperAdmin | Ambient plaintext, impersonation, hidden recovery access | Distinct RECOVERY context, owner request + current proof + fresh privileged auth + explicit reason/time-bound approval + auditor evidence; no reveal/export on operator API |
| SQL/log/backups/cache | Plaintext or key disclosure, stale backup allowing revoked permissions | Ciphertext only at rest, no plaintext/cache/log/event/telemetry, owner/version AD, separate key provider and backup/key retention strategy |
| Tampering/replay | Modified ciphertext, nonce reuse, substituted owner/version, downgrade to legacy weak format | Authenticated encryption with unique nonces and associated context, versioned package canonicalization, explicit unsupported-format fail closed; corpus/replay/fuzz tests |
| Key loss/recovery | Lost KEK/root, partial rotation, corrupt snapshot | Recovery wraps retained separately, migration/rollback decrypt compatibility, tested full restore of keys and ciphertext; missing all usable roots is terminal unavailable |
| External/storage dependency | Key-provider outage or compromised KMS | Split trust, explicit refusal/degraded state, per-owner rate limits, monitoring without secret payload, documented provider/paid-service approval boundary |

**Specifically excluded:** operator plaintext, generic secret search, user-supplied algorithm or key IDs controlling decryption policy, account/item purge, webhooks/provider execution, auto-generating replacement root key to “restore” old ciphertext, broad public share of Vault.

## Conditional API contract — design candidate, NOT REGISTERED

Only after architecture review:
- `POST /api/v1/vault/session/unlock`: authenticated owner + step-up proof, short-lived purpose-bound unlock capability; no arbitrary caller-provided owner, no plaintext key response; module + `vault.session.unlock`.
- `POST /api/v1/vault/session/lock`: invalidates unlock and client memory; revoke on logout, owner/account/permission changes. Safe ACK only.
- `GET /api/v1/vault/items` and `GET /api/v1/vault/items/{id}`: current owner + `vault.item.read`, pagination/cursor after authority scope; default **masked metadata only**. Neither route has ambient `reveal` semantics.
- `POST/PUT /api/v1/vault/items`: versioned envelope payload, strict typed item allowlist, ID/body owner ignored/rejected, `Idempotency-Key` for create, quoted `If-Match` for update. Current write proof before commit; absent independent review, respond capability unavailable rather than accept an insecure placeholder.
- `POST /api/v1/vault/items/{id}/reveal`: separate `vault.item.reveal` + recent-auth/unlock + no-cache response; never print result in logs/errors/history. Reveal may remain unimplemented even if masked reads are approved.
- `POST /api/v1/vault/recovery/requests` (owner) → `GET/POST /api/v1/admin/vault/recovery/{id}/{review|authorize|reject}` (qualified SuperAdmin) → protected service execute; no arbitrary SuperAdmin raw decrypt endpoint. Exact role, request TTL, anti-replay and identity proof remain **OPEN architecture gates**.

Proposed response envelope: only opaque resource ID/ETag + masked metadata on authorized reads; mutations return readable minimal DTO *only with independently rechecked current read projection*, otherwise opaque committed ACK. Auth denial/wrong owner/resource unavailable must be indistinguishable; never return stored ciphertext as a generic “backup” payload. Exact error HTTP mapping must align shared app API contract in implementation review (do not invent a new global error family).

## Conditional SQL/data contract and transactional invariants

Reconcile against physical design in `docs/design-database/07-*` and `17-*`; prospective resource model:
- Versioned `VaultItem`(OwnerId, Id, kind, status, rowversion, created/updated metadata, *no plaintext*), immutable `VaultItemVersion`(OwnerId, ItemId, VersionNumber, cipher bytes, nonce, tag, algorithm/packageVersion, bound authenticated-context fingerprint, keyVersion), `VaultKeyEnvelope`(OwnerId, keyVersion, wrapPurpose, protected wrapping blob, provider key ID), `VaultRecoveryRequest`(OwnerId, RequestId, requestVersion/status/evidence/audit). Exact physical names/column constraints are **design candidates**; no migration may be applied until reconciled to source schema and reviewed.
- Composite owner/source FKs; no cascading delete of ciphertext/envelope; unique owner/item/version and nonce uniqueness under chosen key/version; append-only version history. SQL always stores protected ciphertext/wrap metadata, never private key material.
- Lock order and transactional owner proof: session/action/receipt reservation → owner/item/version/read source → key-version validity → immutable new version + audit + receipt; final policy proof before commit. Key-provider operations cannot safely be rolled back by SQL; design a durable intent/confirmation/failure recovery protocol and test crash states before implementing rotation.
- Encrypted portability export is authenticated and versioned, with same-owner/import proof and restore preview; no claim of free portability into an unavailable key provider or inaccessible older schema. No plaintext raw ZIP/JSON.
- Rollback: disable *new* formats/commands but preserve decrypt-compatible readers and old wraps until all historical values demonstrably restorable. No key deletion while any version/backup references it.

## Owner UI contract (design only)

Default locked state; item list masked, reveal explicit with recent-auth and per-item consent; copied value intentionally ephemeral; quick lock, session switch and permission revocation clear view and clipboard-sensitive state where technically possible (do not promise OS clipboard deletion). Recovery request wizard shows minimum metadata, destructive/nonrecoverable warnings, immutable audit reference; SuperAdmin approval screen cannot inspect values or ciphertext. Errors for missing key/tamper/provider-outage distinct operationally, without revealing secret/owner through public API. No active menu/button while action gated.

## Acceptance criteria before secret implementation

1. Given a wrong owner, deleted account, absent/Deny grant, disabled FX28, stale ETag, archived/Trash and revoked RECOVERY, every API fails without record existence leakage or write.
2. Identical idempotency key/body commits one immutable ciphertext version; collision/changed body/ambiguous legacy receipt denied, and retry after commit never performs second encryption/version write.
3. Fault-inject crash during key wrap, DB transaction, rotation and backup import; no orphan “success”, lost key, missing audit, or rollback into unusable ciphertext.
4. Tampered tag/nonce/AAD/version/owner fails closed; no logs/debug/errors/SQL observer contain secret; nonce uniqueness tested across processes.
5. Valid owner export/import restores exact history under reviewed key material; wrong-owner package, missing-root or incompatible version cannot be silently accepted.
6. Support/Emergency/SuperAdmin without valid RECOVERY never yields plaintext; separately check fresh approval, audit before action and owner notification intent after action.
7. Independent crypto/security/DB review approves cipher/package/KDF/wrapping, key-provider separation, retention/backup, privacy and threat model; actual real SQL/browser/restart tests on **same final SHA**. Until then: **DESIGN_PREPARED, IMPLEMENTATION_BLOCKED**.

## Open ADR review inputs — cannot be guessed

Precise cipher suite compatibility and maintained library, KDF parameters and owner credential interaction, owner vs recovery wrap layout, key-provider deployment and budget, backup custodian/rehearsal, root rotation and loss protocol, current MFA/recent-auth guarantees, exposure to the owner during reveal, audit/notification dependency, secure import threat cases, encryption format downgrade policy. Record these as `OPEN_SECURITY_DESIGN` with named reviewer rather than silently calling this document a passed ADR.
