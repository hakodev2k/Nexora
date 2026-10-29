# Shared modal background scroll isolation — 2026-10-26

Task: root assigned source/UX review and a bounded scroll-lock fix while Shopping worker prepares actual wheel evidence. Current approval DEC014 automated QA; UX-08/11 modal isolation, UX-001/002 and NXG-SYS-13/14. Source baseline a0215f7 plus coordinated worktree. No auth/busy/dirty-leave/focus-trap changes. Existing inert background alone does not lock document scrolling. No claim an original runtime wheel failure was reproduced; source gap and required modal isolation motivated this implementation, then real post-fix wheel verification must prove behavior.

Changes: modalIsolation.ts adds per-document reference-counted root/body overflow lock and scrollbar-gutter padding compensation. Each lock snapshots/restores only affected inline properties and their priorities, preserving unrelated styles. Nested portals retain the lock until final release in either cleanup order; existing released guard prevents duplicate release. Existing inert/aria-hidden reference counting retained, using layer.ownerDocument. No global wheel-event cancellation, body fixed-position trick, focus override or irreversible side effect.

New modalIsolation.test.ts covers pre-existing overflow/padding/priority restoration, scrollbar compensation and both nested release orders/idempotency with original background attributes preserved. Focused actual command included new2 tests, existing6 ResourceFormDialog and3 SharedResourceAccessibility tests: **11PASS**,0failure,2.50s on Vitest5.0.5. Existing async-act warning retained. Configured JUnit reporter ignored attempted CLI output override and wrote artifacts/vitest-junit.xml; verified11-case XML copied to artifacts/modal-isolation-junit.xml to preserve this run. No functional reporter used or permission fixture executed by this check.

Actual Shopping wheel/viewport/focus proof is assigned to a different worker and must be reported separately. Safari/cross-browser rubber-band behavior is not verified by jsdom or Chromium-only runs. Independent review of this shared behavior remains required. Curated UI images captured before this scroll-lock delta remain an earlier rendering checkpoint until affected captures are refreshed; no automatic release acceptance.

## Source hashes
- `web/Nexora.Web/src/modalIsolation.ts`: `324F62657A4029A9FDE92FE3647B44741BB6D624D0CFFDAAC47978BB4E6C18B2`
- `web/Nexora.Web/src/modalIsolation.test.ts`: `B2C5EB5E07AAED7201600DD16B513A5ADB3CFF6E2DB5C84415A077F18283FB82`
- `web/Nexora.Web/src/ResourceFormDialog.tsx`: `006A6220944A2D0C0C3E000C0195E29943BCF29880D2EEE91DBCCE5CA5ECF58A`

## Independent MOD-R01 finding and final correction

Learning reviewer identified genuine CSSOM loss of mixed axis values/priorities when shorthand overflow cannot serialize both. Added the mixed-axis jsdom regression; it passed even before correction because jsdom's CSSOM differs here, so it cannot prove browser correctness. Added actual Chromium integration against the real isolation module in a detached synthetic document (no live-page styling/auth mutation): **before fix1FAIL** with all axis values/priorities lost, preserved `artifacts/uiux-audit/modal-axis-before/`; **after fix1PASS**,1.1s, `modal-axis-final/`.

Final snapshot captures shorthand and each overflow-x/y value+priority for root and body, restores each axis separately, preserving combined semantics and non-expanding DOM implementations. Existing nested idempotent release/gutter/background attrs retained. Final focused new3+existing6dialog+3accessibility **12PASS**,2.65s; same async-act warning. Requested independent Learning source/probe re-review of the correction; pending its actual response. Source frozen after this correction; do not call an author self-review independent acceptance.

Final fingerprints:
- `web/Nexora.Web/src/modalIsolation.ts`: `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`
- `web/Nexora.Web/src/modalIsolation.test.ts`: `FA2BE7BBA6D3E1C50FEFB216DBF8A673E66EAEC1472A5960B922B6165090ADA1`
- `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`: `AF9EFCE3061EA330A2E69E37AF4D6C0625F9016BEAC2AF3459657FAAAB1F8FA6`

Independent Learning reviewer re-ran actual-source detached Chromium probe after correction: both nested cleanup orders, mixed root/body axis priorities, duplicate release, gutter padding and scrollY500 preservation passed; reviewer reported MOD-R01 resolved at SHA256 `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`. Reference workers/review-existing-flows-20261025.md (independent author). Shopping actual tablet post-fix wheel proof separately reports Order479/max611→479, Warranty456/max456→456 and conflict1293/max1293→1293; those are reviewer/other-worker executions, not this author's runtime run.
