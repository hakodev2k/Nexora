# Independent Templates source review — 2026-10-25

Reviewer `/root/shopping_completion`, separate from Templates author. No Template
product/test edits by this reviewer. Root owns integration/release decision.
Initial source75b6d9f plus current uncommitted Templates delta. Read exact FX24
feature/phase/archive semantics, action/UX and author contract; source:
TemplateServiceContracts, TemplateEndpoints, SqlTemplateService, migration0093,
templateApi, TemplatesScreen and pure/SQL/component tests. No reviewer execution
of SQL/browser/test suites; previously authored Shopping tests are unrelated.

## Finding T24-R01 — Medium / DB integrity

`CK_Template_State` in migration0093 Trash branch checks
`PreTrashStatus IN('Active','Archived')` but not `PreTrashStatus IS NOT NULL`.
SQL CHECK accepts UNKNOWN, so a Trash row with NULL previous state and nonnull
deletion batch can satisfy the constraint even though exact-prior-state restore
requires that state. Normal service writes populate it and restore fails closed
when absent; this is a concrete schema-invariant gap, not demonstrated unauthorized
access. Required correction: explicit IS NOT NULL in Trash branch, plus a real SQL
negative constraint assertion. Sent to root; author/root correction and rerun pending
at this review snapshot. Do not mark full DB acceptance passed from this source read.

## Bounded review observations

- Owner predicates precede list/pagination and all record reads/writes; live SQL
  actor/module/action/read prerequisites and final checks are present. No source
  IDs, ownership, share/secrets or generic executable seed are accepted.
- Archive is hidden from Active browsing while remaining editable/usable, consistent
  with P03-ORG-005. Trash denies edit/use; native restore recovers exact prior state;
  purge only accepts Trash/current ETag. Global Trash/provider integration is not claimed.
- Typed Bookmark schema-v1 draft handoff checks current template/read/instantiate
  and target create authority and exact ETag. Draft GET performs no target insert,
  receipt mutation or Audit write. Target Save uses normal Bookmark create validation;
  template edits have no live binding to created resources.
- Receipts bind owner/action/key/body digest, persist opaque proof only and verify
  status/code/IDs/current original-write authority before ACK replay. Purge replay
  does not recreate or reveal prior seed; new source DTO is not stored in receipt.
- Client intent freezes serialized body/key across unknown writes, validates matching
  committed ACK, preserves stale draft pending explicit current-version comparison,
  and clears private data on current denial. UI forms are inert native controls;
  URLs are text/seed, no external fetch. Root Bookmark-form callback wiring requires
  its own end-to-end evidence beyond this reviewed library subset.
- Migration is additive, marks exact schema, inserts only absent existing action
  definitions; no grants/status promotion or module activation. Rollback retains
  populated data; no autonomous down/drop operation.

Other than T24-R01, no new blocking source finding was established in this bounded
delta. This is not whole-FX24/security/database certification. SQL constraint/upgrades,
permission races, actual target handoff cancellation/idempotency, mobile/keyboard and
global provider integration remain tied to actual root test evidence and scope.

## Source fingerprints at review

| Path | SHA256 |
|---|---|
| SqlTemplateService.cs | `B09BDFAB06A05055AD295286EF93553759F2330D74B0DF08002DC49753B6F586` |
| migration0093 | `E6656E8E3876301F74F5E864C5C2E4809449E03F8DC804BDAC6D5AC1938A62E9` |
| TemplatesScreen.tsx | `49C6742AA6308FC42B7BD2A9003408F4D859134C12354FD65FFB85085E7A526E` |
| templateApi.ts | `854A20CC40C346F269FF4EC1D980E6A7137B2B19523B8FD5C5205D78A48AE6A4` |

Later relevant changes invalidate this fingerprint snapshot. Corrected constraint
must be re-reviewed; this report intentionally does not convert a pending fix into PASS.

## T24-R01 re-review

Root corrected the Trash branch to explicit `PreTrashStatus IS NOT NULL` and added
`Trash_constraint_rejects_missing_prior_state_instead_of_accepting_SQL_unknown`.
Reviewer re-read both: the direct synthetic SQL mutation must throw constraint547
and the record must remain Active; no assertion or lifecycle rule was weakened.
Corrected0093 SHA256:
`1524BE3CAC2355D8E452C84BED8845CFCC6FF5207C3213A39AF03C9C28205B37`.
Finding is **SOURCE FIX RE-REVIEWED**; actual fresh-database SQL execution is root-owned
and must be recorded from its real run before calling runtime verification complete.
No other new blocking source finding in this re-review. No product edit by reviewer.
