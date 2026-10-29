# PR #4 — Owner-confirmed D01–D09 implementation contract index

Decision date: **2026-10-19**. All nine options are **A**, per explicit PO selection and the *visible choices presented in the conversation*. [Normative decision record](../../requirements/13-owner-decisions-20261019-pr4-blockers.md). Historical PR [review](../pr4-feature-gap-review-20261019.md) is an inventory; do not use its older inconsistent D01 A/B wording.

| Option | Contract | Owner-confirmed effect | Still blocks completion |
|---|---|---|---|
| D01=A | [01 Vault threat model/architecture](01-vault-architecture.md) | Prepare ADR only, no secret functionality | Crypto/key/recovery security review, implementation, real tests |
| D02=A | [02 Finance](02-bounded-scopes-and-providers.md#d02-finance--manually-entered-records-only-advanced-business-scope-gated) | Basic manual records | Advanced financial business decisions |
| D03=A | [02 Tasks/Reminders](02-bounded-scopes-and-providers.md#d03-tasksprojectsreminders--flat-first-slice) | Flat Tasks + one reminder | Recurrence/subtask/snooze/attachment extensions |
| D04=A | [02 Local providers](02-bounded-scopes-and-providers.md#d04-local-deterministic-simulation-zero-external-effects) | Explicit synthetic mode only | Real provider/network/paused workers |
| D05=A | [03 Sensitive projections](03-sensitive-projection.md) | Fail closed pending reviewed source field allowlists | Sensitive SHARE/SUPPORT/EMERGENCY projection release |
| D06=A | [04 Learning Plan](04-learning-plan.md) | Goals/Tasks/Courses, NOT Skill source | New typed schema/API/UX/SQL/tests/review |
| D07=A | [05 Owner revoked ShareLink](05-sharing-and-tag-replay.md#d07--owner-diagnostic-list-retains-own-explicitly-revoked-row-no-viewer-access) | Owner current-authorized inactive metadata/token-null, viewer404 | Source-authority/revocation tests and reviewer |
| D08=A | [05 Tag committed replay](05-sharing-and-tag-replay.md#d08--original-tag-create-replay-after-tag-removal--opaque-committed-ack) | Canonical original create ACK only after original authority | Legacy/changed intent/namespace/race tests |
| D09=A | [06 Draft/verification gate](06-release-verification.md) | PR stays Draft, R1 scope unchanged | Latest SHA CI, full semantic trace, independent security/DB and owner acceptance |

## Handoff order

1. Apply confirmed decisions to source/action/DB/UX doc conflicts **without granting capabilities**. Existing `DEC-20260909-014` allows contract-complete local code; new docs neither supersede its gates nor retroactively pass tests.
2. Repair current-head failing unit/SQL/browser/Windows gates. Treat old failing oracles separately from actual product security defects.
3. Implement independent local-safe feature gaps (e.g. Learning Plan typed sources) with same-TX authorization, owner isolation, receipts, migration and rollback; keep field-sensitive and provider functions unavailable until review.
4. Collect final-source SQL/API, React/browser desktop+mobile, Windows ACL, independent security/DB and requirement-by-requirement evidence.
5. Human reviewer and PO record acceptance; only then consider moving PR from Draft. **Never merge or enable real providers under this task.**

### Provenance, implementation and verification statuses

- `DECISION_CONFIRMED`: explicit user choice, documented in `docs/requirements/13-owner-decisions-20261019-pr4-blockers.md`.
- `CONTRACT_DRAFT`: precise reviewable API/DB/UX/security/acceptance specifications, including open specialists, not yet run in application.
- `CODE_PRESENT`: actual registered source implementation after later code change, to be independently checked.
- `TESTED`: exact command/run/artifact + source SHA with no skips on mandatory gates.
- `ACCEPTED`: all active ACs, security review and PO signoff. **Not established** for entire PR.
- `EXTERNAL_EXECUTION_GATED`: live provider calls/production not authorized, even if synthetic tests pass.

Documentation updates must preserve historical QA evidence (including NQA-012/013) as old observations; append forward decisions instead of rewriting the history of a previous test run. No SQL migration, UI change, provider execution, test run or merge was performed in this documentation-only task.
