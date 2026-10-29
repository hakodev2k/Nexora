# DEC-20261019-PR4 — Product Owner selections for PR #4 D01–D09

Date: 2026-10-19 (Asia/Bangkok). **Decision status: OWNER CONFIRMED** for the stated Option A boundaries only. Source: Product Owner's explicit chat instruction: "D01=A, D02=A, D03=A, D04=A, D05=A, D06=A, D07=A, D08=A, D09=A". Applies to existing draft PR #4 branch `impl/m01-s00-scaffold`; initial review head `7a1f157621d7dc5b734f79ec8caf5e0dec71f039`, base `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. This record is documentation, not runtime or security acceptance.

**Important interpretation:** The Option A labels below are those rendered to the owner in the *latest interactive selection control*, not the inconsistent D01 A/B labels written in the earlier review draft. Specifically **D01=A means threat model and architecture ADR only, no Vault secret implementation**. No implicit substitution with the draft's previous D01=A encryption-implementation option is permitted.

## Authority and precedence

Preserve `DEC-20260909-004` (hybrid Vault recoverability/no operator plaintext), `-005` (sensitive projection allowlist), `-006` (manual Finance), `-007` (flat Tasks/one reminder), `-008` (bounded read-only outbound scope), `-009` (FX30/34/35 real effects paused), `-010` (Local Stable precedes production), `-014` (approved local contract-complete implementation, synthetic QA permitted under later explicit QA request). Existing `DEC-20260907-Q03/Q04` deny reuse of permanently invalidated share tokens and Vault purge/crypto-erasure. The earlier code-only execution restriction is superseded only by the 2026-10-25 execution amendment in DEC-014: agents own authorized isolated automated QA/runtime verification and the human retains final UI/product acceptance. The D01–D09 dispositions below are unchanged; this decision record itself supplies no executed tests or provider approval.

## Selected dispositions (all Option A)

| ID | Confirmed Option A | Effective scope | Implementation/gate state |
|---|---|---|---|
| D01 | Prepare Vault threat model, versioned crypto/recovery ADR and review evidence **before** secret-bearing functionality | FX28 | `APPROVED_FOR_DOCS_ONLY`. Crypto selection, package interoperability, root/key management and independent specialist sign-off **remain OPEN**; no secret store/reveal/recovery endpoints enabled |
| D02 | Current Finance = manually entered Category/Amount/explicit Currency/owner-local Date/optional Note | FX27 | `RECONFIRMED_EXISTING_POLICY` (DEC006). Account, ledger, budget, debt, interest, FX, transfer and advanced deletion still `BUSINESS_GATED`; not deleted from committed requirement ledger |
| D03 | Flat Task belonging to one Project, maximum one approved reminder; no automatic extensions | FX11–14 | `RECONFIRMED_EXISTING_POLICY` (DEC007). Subtasks, recurrence, snooze, standalone reminders and Task attachments `GATED` |
| D04 | Local deterministic/synthetic-only provider adapters; no external calls/side effects now | FX29/30/33/34/35/36 | `RECONFIRMED/EXECUTION_RESTRICTED` (DEC008/009/014). Scope-specific read-only News/GitHub/Monitoring policy stays eligible for **future approved guarded contract**, but current Option A forbids activation in this work |
| D05 | Deny all **not explicitly approved** sensitive Share/Support/Emergency fields; only owner SELF flows remain independent | FX04/05 and sensitive source FX | `RECONFIRMED_EXISTING_POLICY` (DEC005). Field-level source matrices and owner preview still `SECURITY_REVIEW_GATED` |
| D06 | Learning Plan references **Goals, Tasks, Courses only**; no Skill as PlanItem reference | FX40 | `OWNER_CONFIRMED_SOURCE_RECONCILIATION`. Current feature FX40 workflow agrees; old action-label and DB field prose conflict. Reconcile exact `learning.plan.link` contract and migration before enabling; Skill evidence/Skill merge remain separate and not automatically canceled |
| D07 | Owner's diagnostic list **may** show own revoked ShareLink metadata, marked inactive with null token. Active-only view excludes it | FX04 | `OWNER_CONFIRMED_SEMANTICS` (reconciles NQA-012). Viewer/token resolution returns safe 404 permanently after revoke, owner/source delete or epoch invalidation. No resurrected token on restore |
| D08 | Exact previously committed Tag-create replay returns **opaque ACK only** after original owner/action/digest/current original-write authority are proven | FX24/20 | `OWNER_CONFIRMED_SEMANTICS` (reconciles NQA-013). No Tag DTO, no second create, no stale content; unprovable legacy receipts/collisions/denied current authority fail closed, not assumed accepted |
| D09 | Keep PR #4 draft; retain committed R1 scope and mandatory independent review/green current-head CI/traceability gates | R1/PR #4 | `RECONFIRMED_EXISTING_RELEASE_POLICY` (DEC010/014). No merge, provider activation, production go-live or blanket coverage percentages |

## Decision effects and residual blockers

- **Not new permissions:** This record cannot issue module grants, unpause a worker, enable arbitrary SYSTEM publishers, expose Vault plaintext, or approve paid/production providers. The already-approved local code slices continue without re-requesting approval when the precise contracts are complete.
- **D01:** Approves producing/reviewing ADR only, not declaring the architecture security-approved. A separate reviewed crypto selection, package/backup/key owner, local test proof and deployment boundary are required before encrypted Vault writes/reveal. Loss of all valid root/wrap material is not bypassed with a SuperAdmin grant.
- **D02/D03:** These are sequencing and technical boundaries, **not** an undocumented removal of active R1 product requirements. Later inclusion or formal exclusion requires traceable PO change control; the existing advanced gates remain visible.
- **D04:** Simulation must be conspicuously labeled, avoid outbound network activity and never be represented as delivered real external notifications or provider freshness. Independent trusted local notification transports remain a separate platform responsibility.
- **D05:** The default-deny rule is implementable now. Individual sensitive-field *allowlists* are still to be reviewed; this approval is not blanket authorization to share any field.
- **D06:** Goals/Tasks/Courses is now the owner-confirmed target set, but schema/UX/action/owner participants need an aligned vertical contract before code; Skill entities may still be managed in the Skills feature.
- **D07/D08:** Owner-confirmed runtime semantics supersede conflicting historical test oracles **only after** the existing normative security policy and current implementation are checked. Keep negative authorization, current resource lifecycle, hash-only tokens, strict ETags, transaction and receipt checks.
- **D09:** Do not mark any feature `COMPLETED` merely because docs are `CONTRACTED` or a subset test previously passed; all current-head gates remain.

## Contract/handoff map

| Selected decision | Binding implementation document | Reviewer and exit gate |
|---|---|---|
| D01 | [Vault architecture and threat model](../implementation/pr4-decision-contracts-20261019/01-vault-architecture.md) | Security/crypto specialist; ADR OPEN; no secret handlers |
| D02/D03/D04 | [Finance, Tasks, external execution boundaries](../implementation/pr4-decision-contracts-20261019/02-bounded-scopes-and-providers.md) | Owner + domain/backend QA; protected provider effects remain disabled |
| D05 | [Sensitive projection policy](../implementation/pr4-decision-contracts-20261019/03-sensitive-projection.md) | Source owners/security; per-field allowlists unapproved |
| D06 | [Learning Plan source contract](../implementation/pr4-decision-contracts-20261019/04-learning-plan.md) | FX40/Goals/Tasks/Courses + DB/security reviewer |
| D07/D08 | [Sharing/Tag compatibility contract](../implementation/pr4-decision-contracts-20261019/05-sharing-and-tag-replay.md) | Sharing/Tags backend security reviewer + SQL/API/UX tests |
| D09 | [Release and acceptance gate](../implementation/pr4-decision-contracts-20261019/06-release-verification.md) | Independent QA/security/DB + Product Owner |

**DoD for documentation:** Every affected slice must be traceable from current requirements/BR/AC/action to contract/API/data/UI/allowed-and-denied scenarios, explicit implementation owner, rollback and reviewer; unresolved security/design inputs stay `GATED`. Executed CI, app behavior, migrations and UAT remain **NOT RUN by this decision-record task**.

## Record integrity

The Product Owner explicitly chose all nine A options. Do not rewrite this decision as consent to Option B. If the branch has changed since the recorded review, repeat source and CI verification on the new SHA before changing implementation status. This decision record does not authorize merge, real providers, production, paid service use or destructive user data operations.
