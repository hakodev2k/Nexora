# Trash lifecycle local slice

Status: `SLICE_IMPLEMENTED` in PR #4; runtime acceptance remains owner verification.

## Contract trace

- Goal: `NXG-FX08-G01…G03` and cross-cutting owner/lifecycle/audit goals.
- Source: [FX-08 Trash, Activity and Audit](../features/08-trash-activity-and-audit.md),
  `TRS-001…TRS-006`, `ACT-001…003`, `AUD-001…006`, and `FX-08-AC-001…003`.
- Operations: `listTrash`, `restoreTrashBatch`, `purgeTrashBatch`.

## Implemented behavior

- Trash rows are selected by the recorded `DeletionBatchId` and the
  authenticated `PersonalSpace.Id`; no timestamp cohort inference or alternate
  owner input is accepted.
- Project deletion batches restore the parent and only children deleted in the
  same batch. A terminal restored Project leaves its child Tasks in Trash;
  standalone Task restore fails closed when the parent is terminal/unavailable.
- Purge requires the literal `PURGE` confirmation, removes Project/Task history
  before content, deletes only the requested owner batch and writes an audit
  event. Calendar events never enter this provider because their contract is
  cancel-only.
- All mutations use a durable idempotency receipt and a serializable SQL
  transaction. No test/mock/demo data or provider call is added.

## Evidence status

`npm run build --prefix web/Nexora.Web` and
`python3 scripts/dev/verify-s00.py` run on the working tree. .NET build, SQL
migration execution, functional/API/integration/E2E/browser tests and manual QA
were not run in this code-only environment.
