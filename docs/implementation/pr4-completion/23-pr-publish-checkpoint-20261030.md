# PR #4 publication checkpoint — 2026-10-30

User requested publication of the current implementation increment to existing
branch `impl/m01-s00-scaffold`. Parent revision is
`a0215f7b3e85d33503cff9077b41cc065240cb81`. This checkpoint does not certify
completion of the remaining mission or human acceptance. No merge or deployment.

## Delivered changes and executed verification

| Area | Actual result | Evidence |
|---|---|---|
| Bookmark / Time recovery | Immutable retry intent, close/leave guards, precision and timezone validation; focused component and actual browser runs recorded separately | `workers/time-bookmark-qa-20261025.md` |
| Shopping / Certification | Source-picker and history fixes; actual manual-record SQL/browser workflows at desktop/tablet/mobile | `workers/existing-shopping-certification-20261025.md` |
| Shared UI | Radix Label/Slot, semantic components/tokens, responsive forms/cards/dialogs, focus and scroll isolation | `workers/design-system-20261025.md`, `workers/modal-isolation-20261026.md` |
| Documents | Full-width editor/library, readable preview and export dialog actions; real post-commit HTTP408 recovery; final desktop3 PASS/mobile3 PASS (3 unique scenarios,6 executions) | `workers/documents-transfer-20261028.md`, `evidence/documents-transfer-20261029/` |
| Skill | Reproduced2 HTTP408 failures, fixed and reran2 PASS with exact body/key/ETag | `workers/independent-final-deltas-20261029.md` |
| Automation | Reproduced4 HTTP408 failures; fixed4 predicates; final affected regression13 PASS including those4. Default-off and opt-in local execution browser suites separated; startup opts in only local workers | `workers/automation-integrations-qa-20261028.md`, `evidence/automation-recovery-20261029/` |
| Career | Real initial400 caused by blank GUID cursor; adapter omits absent cursor, adapter regression PASS | `workers/independent-final-deltas-20261029.md` |
| Backend |193 unit and311 real isolated SQL/API integration PASS at recorded source; no backend application implementation changes followed that execution | `workers/backend-regression-20261028.md` |
| Final frontend publication checks |330 unit/component PASS,1 existing gated skip,0FAIL;42 test files. `npm run build` PASS, with existing main-chunk size warning. These do not replace pending browser scenarios | Local `artifacts/frontend-final-20261030.log`, `artifacts/frontend-build-20261030.log`; summaries captured at this checkpoint |
| Search / Collections / WorkLog / Goals | Added actual workflow and negative/recovery assertions; individual executed runs and their revision limits remain in worker reports | `workers/discovery-negative-20261028.md`, `workers/review-collection-palette-operator-20261028.md`, `workers/worklog-runtime-20261026.md`, `workers/modal-cancel-audit-20261027.md` |

Do not sum overlapping test runs. A passing build is not functional acceptance.
The83 inventory dispositions comprise77 active bounded work packages and6
gated/excluded dispositions after explicit Support reclassification; they are not
83 accepted features. Inventory21 and original733-action inventory16 preserve
unstarted/deferred siblings. Historical79-active statements in earlier checkpoints
precede that correction.

## Remaining queue / release status

- Career/Skill browser reruns after adapter/oracle fixes: NOT VERIFIED; their
  original two failed desktop scenarios remain preserved. Interrupted synthetic
  FX40 access state was subsequently restored and verified through normal APIs.
- Dashboard layout/Quick Create test authored: NOT TESTED.
- Automation default-off metadata and opt-in local Task/scheduler browser cases:
  NOT TESTED. Opt-in switches do not authorize real providers.
- Full current functional/core browser regression and current-head CI: pending.
- Files separate Windows web/operator/scanner principal ACL acceptance remains
  an environment gate; same-principal upload success is insufficient.
- Support positive flows remain security-gated; generic notification worker and
  unrelated unstarted functionality are not silently initiated.
- Final human UI/product acceptance: PENDING. Overall R1: HOLD. Scoped mission:
  PARTIAL. No whole feature group newly declared RELEASE READY.

## Evidence handling

New screenshot publication was blocked by automatic approval review, which
requires explicit user approval of image contents. The images were visually
inspected and remain local at the paths referenced by worker reports; this
commit excludes new PNG files. Their hashes/manifests describe retained local
artifacts, not proof that the corresponding images were uploaded. Code, tests
and sanitized text evidence are published independently. No rejection is bypassed.

Automatic review also rejected a failed runner XML due to artifact paths and
detailed failure output. Changed XML/TRX payloads are therefore retained locally
and excluded from this publication; executed results and root causes remain in
the worker reports and sanitized JSON summaries. Historical report files already
on the branch are unchanged by this exclusion. Links to excluded runner artifacts
describe local evidence, not newly uploaded files.

Raw runner output stays local. Curated XML/TRX copies redact local identity,
machine names and absolute paths; test outcomes/assertions are preserved. CSV
artifact digests are refreshed after redaction. Historical raw-file digests in
worker records identify the original local evidence, not redacted publication
bytes. Source manifests identify executed source independently of the later
documentation-only publication commit. Later affected executable edits require
reruns; no old CI is promoted to current-head evidence.

Security/product D01–D09 remain unchanged. No real providers/data/secrets,
production deployment, force push or automatic merge is authorized or performed.
