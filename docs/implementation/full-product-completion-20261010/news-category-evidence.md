# FX29 — News category vertical slice

Requirements main: 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3. Parent PR revision: 4eee65512b406fd1d3418ddc9f585b82911517ca. Evidence applies to the source in the commit containing this record; no source was edited after the final passing runs.

Authority: current explicit PO implementation request; DEC-20260909-014. Contract: ../pr4-missing-features-20261002/news-category-contract.md; FX29 category read/create/update; P05-NEW-001/002; fixed 25-row owner list and guarded rename/create. No feed/provider execution.

Production: SQL category persistence, atomic audit/receipt, current actor/owner/module/action checks, ETag conflicts, original idempotent ACK recovery, literal name filtering and cursor pagination. Trusted account-verification and both bootstrap transactions initialize AI News/Tech News once. Registered API GET capabilities/list/item, POST list and PUT item under /api/v1/news/categories. FX29 shell renders actual create/edit/list React UI, explicit conflict comparison, dirty cancellation and frozen uncertain-save recovery. Successful ACK decoding validates UUID and canonical rowversion ETag; malformed/missing ACK does not rotate the command key.

Migration 0046 copied unchanged from reviewed staging and appended to explicit 47-file manifest. Retained isolated journal already contained it; replay preserved core record counts. No edits to 0045 or applied checksum. FX29 activation flags remain unchanged; only three installed category actions become locally eligible, other News actions deny.

Executed on final source:
- API Release build: PASS, 0 warnings/errors.
- Web build/typecheck: PASS, existing >500kB bundle warning.
- Functional SQL operator Release build: PASS, 0 warnings/errors.
- Focused real SQL/API News: 25 PASS, 0 FAIL/SKIP.
- Full real SQL/API suite: 147 PASS, 0 FAIL/SKIP; fresh GUID-isolated databases, no in-memory substitution.
- Backend unit: 47 PASS, 0 FAIL/SKIP. Initial manifest assertion failed because new reviewed 0046 was absent from expected list; expectation now includes exact added migration, no gate removed.
- Full frontend suite: 108 PASS, 1 existing SKIP, 0 FAIL. Initial new test hook accidentally returned a mock function as teardown; corrected hook returns void, assertions retained.
- Actual API/SQL browser category journey: 5 PASS, 0 FAIL/SKIP/NotRun, across 1920/1366/768/390/320 widths. Initial run: 3 PASS, mobile login429 FAIL, 1 NotRun. Final run increased private runner login pacing to13s with unchanged server limits and passed all5. Checks include create/persistence, dirty cancellation, concurrent rename412/reapply, actual25+4 paging, no overflow and grant revocation.
- Independent source review: baseline_review; auth, immutable migration, owner initialization, SQL lock/receipt/audit and client reviewed. Confirmed lost/malformed ACK finding fixed and re-reviewed; no remaining source blocker. Reviewer did not claim independent runtime execution.
- git diff --check: PASS.

Raw TRX, browser artifacts, accounts and private runner state remain ignored/local; no credentials committed. Full browser regression beyond this affected journey: NotRun for this revision. CI results for this new commit: pending, not inferred from local runs.

Remaining FX29 scope: sources/RSS ingestion/articles/read-state/topic-watch and their lifecycle/security/UI acceptance are incomplete. This category slice does not establish whole FX29 or Release1 completion. Real-provider operations remain inactive.
