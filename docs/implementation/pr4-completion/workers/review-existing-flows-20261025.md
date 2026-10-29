# Independent source review of existing-flow deltas

Reviewer: learning_completion. Base HEAD `a0215f7b3e85d33503cff9077b41cc065240cb81`, PR4 shared working tree. Read-only review, no other-author source/test edits and no new test executions during this review. Author of earlier Certification foundation and Bookmark regression tests; independence here is limited to other authors' current source deltas, not approval of this reviewer's earlier code/tests. Root separately reviews Time changes.

## Finding

**S31-R02 — Medium / AUTO-RESOLVABLE / pending author remediation.** ShoppingRecordsScreen.chooseOrder keeps the prior sourceOrder/orderLineId when starting a switch to another order and when the new fetch fails. Save is correctly disabled while sourceBusy (the initial immediate-click concern was corrected), but re-enables after503 with the old valid line still selected. Thus a failed intended switch can silently save the earlier order's product. persist also lacks a sourceBusy guard against implicit submit. Reproduce: edit warranty linked to A; choose B; B detail fails503; Save becomes available with A line. Fix: clear stale line/order at switch initiation, retain pending/error selection truthfully, and block submission until selected source resolution; add delayed/rejected source-switch regression. Existing new test waits for successful response and does not cover failure. Sent to coordinator promptly; no product edit by reviewer.

## Other bounded observations

- Shopping applied-query pagination fix binds Load more to last executed query; deduplication remains by source ID. Successful order switch clears a line absent from new order. This does not close the failed-switch gap above.
- Certification empty-history label is conditional on an actual empty history array; failed first fetch leaves history null, retains retry entry, and clears old error before retry. Added regression checks failure→empty recovery. No new finding in this delta; not full Certification review/acceptance.
- Bookmark load gates list calls on read; create-only retains create capability and no403 error, with explicit unavailable-library state. Unknown408 retains original draft/key/ETag through disabled inputs and reload/search guards; closeBlocked uses shared modal Escape/navigation/unload guards. Two-step abandonment warns that write may have committed; shared Button defaults to typebutton while Save explicitly uses typesubmit. FormField forwards ID/error/hint associations into native controls. No new source finding in these reviewed changes. Earlier authored component evidence is documented separately, not presented as independently executed here.
- Documents `functional/content.spec.ts` oracle now reopens the Archived record after tree commit, verifies readonly rendered Markdown, absent editing textarea, disabled title/Save, and retains SQL Archived→Published, unchanged version2 and reload-body assertions. This is a contract-consistent stronger oracle than checking a cleared editor for disabled textarea. Runtime desktop/tablet/mobile PASS was reported by coordinator, not executed by this reviewer.
- FunctionalTestOperator six resource names map only to literal allowlisted SQL tables; UUID remains a parameter. Existing LocalSqlTarget loopback/Development checks and exact `Nexora_Test_[0-9a-f]{32}` database guard execute before command dispatch. No new mutation, arbitrary identifier, remote target or production allowance. Generic read-resource selects all current metadata columns only inside disposable synthetic DB; do not reuse this operator as production/support/API authorization evidence.

## Reviewed SHA256

| File | Working-byte SHA256 |
|---|---|
| ShoppingRecordsScreen.tsx | 41F1662ED75E883E9AF1FEC0971E4C085A4E737C213F24A56760C3023B6AA961 |
| ShoppingRecordsScreen.test.tsx | 46445B2207D8BBA90A3AAAD7362884F1300D918C11A1CA28EF6906BD45483229 |
| CertificationScreen.tsx | A3F321AC0AC895DD6FC86C587E6D37E8AEF30281E9500C811098E4C03D4710DA |
| CertificationScreen.test.tsx | 90DF6ABD30E16B790D268C7BFB600D46E71FEA68D2DF142B3707A7E4489A5CB8 |
| App.tsx | 2F78127DBB5AB08EA8237C1811689F59760AB8CD858DBC92315B645108AA71FB |
| ui/primitives.tsx | CCC63FBD9F920E8C061437006331D8441E90395D04C55E9F91E745E7BC8CE0BE |
| ui/primitives.test.tsx | DB74410241EB71A8E01D5A274D5032EC42BF1C1813A2F18E03847E2FF36FDF66 |
| FunctionalTestOperator/Program.cs | 6014FDB266722D6012A890957858D890B01E3E1D1A781C5464D5E60308E9356F |
| functional/content.spec.ts | B41E1C18BBE67DF7C3BAF423E85D23C59DC3597DD266182FFC3E2257D1793EB9 |

## S31-R02 remediation re-review

Current Shopping screen SHA256 `51DFDAA2684CFE2319AC2EE6E027DBE19F12668A838D1304494BB72C06C1128E`, tests `8B523C4E90AC3223404678BFE2059307CD635B3464D255633887460CB7080ED6`: chooseOrder now clears the prior sourceOrder/orderLineId before awaiting the replacement fetch; persist includes sourceBusy. Failed replacement leaves the required line blank, retains unrelated warranty draft, and permits explicit retry/reselection. Author added rejected503 replacement recovery and pending-source implicit-submit assertions. Reviewed source addresses S31-R02; finding **SOURCE-RESOLVED**, focused execution/runtime evidence remains separate coordinator/author responsibility. This reviewer did not edit or execute Shopping source/tests.

No blanket module, security, SQL, UI or release acceptance. Later edits invalidate affected reviewed fingerprints.

## Shared modal scroll-lock review

Reviewed other-author `modalIsolation.ts` SHA `324F62657A4029A9FDE92FE3647B44741BB6D624D0CFFDAAC47978BB4E6C18B2` and new tests SHA `B2C5EB5E07AAED7201600DD16B513A5ADB3CFF6E2DB5C84415A077F18283FB82`. Nested document reference counting and idempotent outer release preserve lock until final portal closes, irrespective of cleanup order; prior aria-hidden/inert is retained. Tests cover shorthand priority, body padding/gutter and nested order. No new app/browser reporter tests executed by this reviewer for the shared delta; author reports11focusedPASS separately.

**MOD-R01 — Medium / AUTO-RESOLVABLE / pending author fix.** Only capturing shorthand overflow loses mixed-axis inline declarations/priorities. Confirmed in a detached about:blank Chromium CSSOM diagnostic, explicitly coordinated with root, with no app/network/SQL/shared reporter: before `overflow-x:scroll!important;overflow-y:auto`, `getPropertyValue('overflow')` and shorthand priority are empty; setProperty overflow hidden replaces axes; release removeProperty overflow leaves cssText empty. Thus original x/y behavior is lost on modal close. Root/author notified to capture/restore individual axes/priorities and add regression. No source edits by reviewer. Current app root has no scrollbar-gutter stable rule (sidebar does); scroll position/page-jump behavior beyond the authored runtime proof is not inferred from the DOM tests.

### MOD-R01 corrected-source verification

Author delivered `previousOverflow`/`restoreOverflow` capturing shorthand and each axis with original priorities. Reviewed final source SHA `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`, tests `FA2BE7BBA6D3E1C50FEFB216DBF8A673E66EAEC1472A5960B922B6165090ADA1`. Independently transpiled/imported the actual file into detached Chromium about:blank (no app/network/SQL/shared reporter) and asserted both parent-first/child-first cleanup orders with double cleanup. Root/body mixed-axis values/priorities restored exactly; nested lock retained; gutter padding equaled original7px+measured gutter; scrollY remained500. **2diagnostic cases PASS, exit0,1.75s**. This is independent source/real-CSSOM diagnostic resolution of MOD-R01, not an additional application-suite execution or full modal/mobile acceptance. Author additionally reports real Chromium before-fix FAIL/after-fix PASS and12focusedPASS; those are author evidence, not runs by this reviewer. No new blocking finding in the corrected bounded delta.
