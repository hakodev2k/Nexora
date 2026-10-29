# Existing Shopping / Certification workflow corrections — 2026-10-25

Author: shopping_completion; coordinator: root; independent reviewer requested: templates_completion. Scope originates in [report21](../21-existing-feature-completion-scope-20261025.md), E31b/c/d and E40f. Existing PR4 branch `impl/m01-s00-scaffold`, starting `a0215f7b3e85d33503cff9077b41cc065240cb81` plus shared local changes. Current user AGENTS replacement and DEC014 automated-QA amendment authorize this implementation/component regression work; earlier code-only stop is superseded. No root App/global styles/API/SQL/permission changes or shared browser/SQL runtime launch by this worker.

Sources: report21 startup/authority; exact [Shopping contract](shopping-20261025-contract.md) P05 manual Draft Order/purchase-line Warranty metadata and nine registered actions; [Certification contract](certifications-20261025-contract.md) P07-CERF-001/FX40-S04/BR004/006, six installed actions; existing UX form source-picker/current source association, loading/error/retry/empty-state rules. Goals NXG-SYS05/08/13/14, NXG-FX31-G01/G02/G03 and NXG-FX40-G02/G03. No new Warranty reminder/evidence, Order transition, Seller merge or Certification extension is started. D01–D09 unchanged.

## Concrete defects and changes

| ID | Severity / user reproduction | Root cause / correction | Verification |
|---|---|---|---|
| ECS-S31-01 | Medium: edit Warranty already linked to Line A; search/select a different Order B; product control still silently retains Line A while Order B is displayed | Source-order fetch only replaced displayed order; old orderLineId and fallback option survived. Final correction clears the previous order/line association as soon as a different Order is chosen; preserves other draft metadata, and requires explicit product selection after a successful read. On fetch failure, stale Line A cannot be silently submitted. Current request epoch still governs completion. Clearing Order clears its line. | New component regression initially FAIL at retained oldLineId, then PASS: new product is required, no write before selection, submitted body contains chosen newLineId. Additional failed503 switch→explicit rechoose test covers recovery. No server authorization was weakened. |
| ECS-S31-02 | Medium: search sources with query A/page1, edit input to query B, click More; request formerly jumped to query B/page2 and appended to query A results | Pagination used mutable input with prior page counter. Track successful query separately; hide/guard More until query matches, and a fresh search always starts new query at page1. Submitted query snapshot binds response state. | New component regression initially FAIL because More remained available; final PASS checks page1 new query and absence of page2 new-query request. |
| ECS-C40-01 | Low: successfully loaded empty Certification history produced a bare heading, indistinguishable from incomplete content | Add explicit empty-history status after successful empty response; existing failed request continues showing error/retry instead of empty. | New failure→retry→empty regression initially FAIL (missing status), then PASS; two requests, error gone only after success. |
| S31-R02 | Medium, independent review: form submit handler could submit prior line while a new Order request was pending despite ordinary Save button being disabled | Add sourceBusy check inside persist; chooseOrder also refuses in-flight writes. Previous association is cleared at selection start as above, so failed switch cannot revive it. | Expanded deferred-response regression reproduced an actual write before fix (1FAIL/11PASS); final passes direct submit denial and rechoose recovery. |

Only files changed by these corrections:

- `web/Nexora.Web/src/ShoppingRecordsScreen.tsx`
- `web/Nexora.Web/src/ShoppingRecordsScreen.test.tsx`
- `web/Nexora.Web/src/CertificationScreen.tsx`
- `web/Nexora.Web/src/CertificationScreen.test.tsx`

Public form labels and API body/key/ETag contracts are unchanged. Global UI worker owns shared Radix primitives/tokens/styles; no unnecessary primitive migration performed here. Root can continue actual Order/Warranty browser flows with existing accessible labels: `Tìm mã đơn hoặc người bán`, `Tìm để chọn`, `Chọn đơn mua sắm`, `Sản phẩm được bảo hành`, `Thời hạn` and `Lưu`.

## Actual execution / evidence

Environment: local Windows Node/Vitest5.0.5 + jsdom; component/client tests mock transport. They are **not browser E2E, API/DB integration or proof of SQL isolation**. No external provider or real user data.

| Execution | Actual result / evidence |
|---|---|
| Before-fix Shopping component run with new regressions |10PASS/2FAIL,6.11s; retained old line and changed-query More assertions failed. Failure evidence retained in command result. |
| Before-fix Certification component run with new regression |8PASS/1FAIL,6.79s; explicit empty status absent after successful recovery. |
| After Shopping corrections focused screen run |12PASS/0FAIL,5.74s. |
| Earlier four-file focused run |30PASS/0FAIL/0SKIP,24.72s, session28516 exit0; precedes independent reviewer follow-up correction. Not final source evidence. |
| Final `npx vitest run src/ShoppingRecordsScreen.test.tsx src/CertificationScreen.test.tsx src/shoppingRecordsApi.test.ts src/certificationApi.test.ts --reporter=dot` | **4files/31cases PASS,0FAIL/0SKIP,5.88s**, chunk e8bb0d exit0. Includes existing cases and four new regressions; do not sum earlier reruns into feature coverage. |
| `npx tsc -b --pretty false` after final correction | PASS exit0, session51085. Implementation-error detection only. |
| `git diff --check -- <four files>` | PASS exit0; LF/CRLF normalization advisories are not test failures. |

An earlier combined-run process handle became unavailable during the incoming instruction update; its completion is not claimed. The fresh final4file/30case run above is the recorded result.

Final screen SHA256 identities: Shopping `51DFDAA2684CFE2319AC2EE6E027DBE19F12668A838D1304494BB72C06C1128E`; Certification `A3F321AC0AC895DD6FC86C587E6D37E8AEF30281E9500C811098E4C03D4710DA`. Shopping regression file `8B523C4E90AC3223404678BFE2059307CD635B3464D255633887460CB7080ED6`. These identify working-tree bytes, not an immutable delivered commit; root owns final GitHub commit/reconciliation/current-head CI.

## Handoff / remaining gates

The three author issues and independent-review S31-R02 are FIXED and COMPONENT-TESTED. Independent source reinspection requested. New `functional/completion-records.spec.ts` authored for real normal-form Seller/DraftOrder/Warranty plus Certification renewal/history/Archive/Unarchive, read-only SQL checks and persisted screenshots; not executed at this handoff pending coordinator permission-changing runtime scheduling. Actual browser results will be appended separately. Human UI/product acceptance and D09 remain pending. No whole Shopping/Certification module or release is declared complete/Ready. No new Critical/High issue established here; release-wide residual count remains unknown.

Independent closure: templates_completion re-inspected final Shopping SHA256 `51DFDAA2684CFE2319AC2EE6E027DBE19F12668A838D1304494BB72C06C1128E`, confirmed immediate reference clearing/flight and sourceBusy guards/preserved other fields/rechoose recovery, and established no additional blocking finding in this bounded review. See [review-shopping-ui](review-shopping-ui-20261025.md). Reviewer performed source review only; author component execution is separate.

Rollback is reverting only these bounded screen/test changes; no persistent schema/data or server contract migration. Preserve unrelated concurrent work. Final inventory remains [report21](../21-existing-feature-completion-scope-20261025.md), not all733 catalog actions promoted into this mission.

## Actual UI → API → SQL continuation (2026-10-26 handoff)

Coordinator scheduled the existing guarded f47 local runtime and rebuilt read-only operator with Seller/PurchaseOrder/OrderLine/Warranty/Certification allowlist. Normal UserA login and real SuperAdmin module-policy/user-module preview+commit enable only already Ready FX31/FX40. No SQL grant edits, capability readiness changes, mocked positive responses or provider calls. Each final case re-reads Admin policy/registration/grant state after cleanup and asserts exact restoration to its starting snapshot.

Executed through `.tmp/run-nexora-browser.ps1 -Suite functional -Project <project> -Grep 'Existing records'` with approved runtime access. Root's mutable worktree label is `a0215f7b3e85d33503cff9077b41cc065240cb81-existing-feature-worktree`, not final commit proof.

| Actual run / viewport | Result | Persistent evidence relative to web/Nexora.Web/artifacts/existing-records-review/ |
|---|---|---|
| desktop, explicitly1440×900 |2PASS/0FAIL/0SKIP,30.9s | `1440x900/desktop-final-junit.xml` + `desktop-final-results.json`; images `records-40fd2176-*`, `certification-9804f2c7-*` |
| tablet768×1024 |2PASS/0FAIL/0SKIP,12.1s | `768x1024/tablet-final-junit.xml` + JSON; images `records-3a6097c7-*`, `certification-2c2a37a3-*` |
| mobile390×844 |2PASS/0FAIL/0SKIP,13.4s | `390x844/mobile-final-junit.xml` + JSON; images `records-dba7d7e8-*`, `certification-6ca128cc-*` |

**Six passing browser cases across three viewports**, not six independently completed feature families. Shopping case exercises Seller form create/edit/reload; Draft Order source selection and exact total `2 × 100.12345678 + 10 + 1 − 0.24691356 = 211 VND`, stale concurrent update→preserved draft→explicit reapply, duplicate-reference409→explicit confirmation; Warranty create/edit and selecting another Order clears old line before explicit new selection. API exact decimal strings and authoritative SQL rows/owner/source IDs/notes are asserted. Certification case exercises form create→expired detail→renew→reload→immutable history→Archive read-only→Unarchive→retained renewal dates, plus SQL owner/state. All use actual UI/API/SQL, without positive-path route fulfillment.

Earlier failures are preserved: first desktop Shopping240s timeout + Certification PASS; diagnostic Shopping18.2s exact wrapped-select label timeout; later Shopping15.1s wrapped-textarea locator mismatch + Certification PASS. Accessible DOM snapshot showed exact combobox/textbox names while label text extraction included option/textarea content. Fix was role-based precise control selection; no source validation, permission or meaningful assertion was weakened. Initial full-timeout cleanup was not proved and its teardown error is retained; later bounded runs verify their own exact starting/ending state. Do not sum failed/rerun case counts into final coverage.

Actual visual inspection opened ten images: desktop Order form/edited Warranty/Certification history; tablet Order form/Warranty order-switch/Certification create/history; mobile Order form/edited Warranty/Certification history. No page-horizontal-overflow assertion failed. **New concrete Medium ECS-S31-03:** tablet detail title collapsed into a letter-per-line column because card direct flex children competed. Source correction wraps selected detail content in one card body. New browser title-height check and final viewport re-capture remain pending at this checkpoint. Corrected Shopping screen SHA256 `53515CA3E62C9519B41F06A62DB65CB30E753FC38ECB0D8C5E6552CC359B01C9`; four focused files31PASS after wrapper,59.72s; app and standalone functional-spec typecheck PASS. Prior Shopping screenshots are pre-wrapper evidence.

Full-page screenshots with fixed modals do not establish actual visible viewport/action reachability. Enhanced capture code now additionally records fullPage:false dialog top/actions, verifies reachable Save/Cancel, focused Cancel with rendered outline/shadow and background scrollY under wheel outside the modal. These new checks are **not yet executed** here; shared modalIsolation belongs to UI worker, and any discovered body-scroll defect must be fixed there. No full a11y or human UI acceptance inferred.

Initial three-image Certification curation was provisional. It is superseded by final source-frozen curation below; exact final file hashes are in its manifest. Original raw captures remain under their recorded synthetic prefixes. No credentials/account-menu contents are visible.

## Final owned-scope handoff A–G

### A — Executive summary

| Metric | Exact result / limit |
|---|---|
| Existing workflow families owned |4: Seller, DraftOrder, purchase-line Warranty, Certification metadata/renewal/lifecycle;15 registered exact actions. Not four completed whole FX modules. |
| Source corrections |4 author issues (association, picker query/page, empty history, responsive card layout) plus reviewer S31-R02 submit guard; FIXED in source. |
| Final component/client regression |4files/31PASS/0FAIL/0SKIP,5.93s after final list wrapper. Mocked transport; not API/DB or browser. |
| Final actual browser matrix |6PASS/0FAIL/0SKIP: two composite cases × three viewports, current source; no summing reruns. |
| Real SQL/API execution |Normal browser transactions and read-only SQL assertions in those6cases; no separate new integration-suite run by this worker. Existing backend/SQL suites belong to root's final regression. |
| Build/typecheck |App `tsc -b` and standalone functional-spec `tsc --noEmit …` final exit0, session76264; implementation-error checks only. |
| Release Ready / human accepted |0 newly certified whole features; HUMAN UI REVIEW PENDING, complete per-AC coverage/current-head CI integration remains pending. |
| Critical/High residual |None newly established within reviewed bounded corrections; release-wide count not assessed by this worker. |
| Verdict |Bounded FIXED + AUTOMATED SCENARIOS VERIFIED; whole-feature/PR release remains HOLD, no full acceptance claim. |

### B — Exact action status within owned workflows

All rows have declared parent IN_PROGRESS. `Present` is verified source/runtime presence within the bounded author contracts, not a new full-AC completion assertion. All release statuses remain NOT VERIFIED / human pending. Suffixes below are actual canonical action IDs.

| Scope | Exact action | Verified implementation / evidence | Missing acceptance |
|---|---|---|---|
|E31b|shopping.seller.read|Present; list/search/detail/current SQL-owner read in real browser| Complete query/paging/denied browser matrix |
|E31b|shopping.seller.create|Present; normal create, opaque ACK and authoritative SQL row| Full validation/boundary/denial matrix |
|E31b|shopping.seller.update|Present; UI edit/reload and SQL title verified| All stale/replay/access variants |
|E31c|shopping.order.read|Present; API exact strings/detail/current source and UI list| Full query/paging/large dataset matrix |
|E31c|shopping.order.create|Present; UI source selection/lines/exact total, duplicate409→explicit confirmation, SQL| All amount/reference negative cases across layers |
|E31c|shopping.order.update|Present; real concurrent write→412/preserved draft→explicit reapply, SQL notes| Complete source-loss/replay/authority matrix |
|E31d|shopping.warranty.read|Present; list/detail/reload and SQL| Full filters/current-source denied cases |
|E31d|shopping.warranty.create|Present; real line picker/Fixed coverage/SQL line-owner relation| Full Fixed/Lifetime/Unknown/invalid date browser variants |
|E31d|shopping.warranty.update|Present; switch Order clears prior line, explicit new line persisted; failed503/deferred submit covered in component regression| Real network-fault/revocation browser coverage beyond current scenario |
|E40f|learning.certification.read|Present; expired/active detail, renewal history and reload| Full filters/paging/access variants |
|E40f|learning.certification.create|Present; UI normal create and SQL owner| Complete negative/boundary matrix |
|E40f|learning.certification.update|Present source; forbidden-update component clearing exercised| Positive metadata-update browser scenario NOT TESTED in this worker matrix |
|E40f|learning.certification.renew|Present; actual form, immutable prior dates/history/reload| Full stale/repeated/revoked/invalid date scenarios |
|E40f|learning.certification.archive|Present; signed preview/confirm, SQL Archived, read-only UI| Complete preview expiry/stale/replay paths |
|E40f|learning.certification.unarchive|Present; preview/confirm, SQL Active, renewed dates preserved| Complete prior-state/stale/denial paths |

### C — Detailed quality matrix

| Family | Backend/API/SQL | Frontend + validation/recovery | Unit/integration | E2E + UI | Security | Release |
|---|---|---|---|---|---|---|
|Seller|Selected real operations PASS|Selected UI create/edit PASS; complete negative coverage PARTIAL|31 shared component/client cases; separate backend/SQL suites not rerun here|3 viewport composite flows PASS; current image review|Owner/source assertions and legitimate authority setup PASS for selected flows; full matrix PARTIAL|NOT VERIFIED / human pending|
|DraftOrder|Exact strings/total211/owner/seller/line SQL and current revision PASS|412 draft reapply + duplicate409 confirm PASS; remaining edges PARTIAL|Same shared suites, not counted again|3 viewport flows + short total single-rendered-line regression PASS|Server revision/current source path observed; exhaustive isolation/permissions PARTIAL|NOT VERIFIED / human pending|
|Warranty|Current persisted line-owner relation and update PASS|Stale association/deferred/failed switch regression PASS; actual switch PASS|Same shared suites|3 viewport flows, fixed detail title and visible modal actions PASS|Current SQL owner proof; broader grants/replay PARTIAL|NOT VERIFIED / human pending|
|Certification|Selected create/renew/history/archive/unarchive PASS|Unknown-save/denial component paths PASS; empty/error state regression PASS|Same shared suites; positive metadata update runtime NOT TESTED here|3 viewport flows PASS; history visual review; metadata edit positive omitted|Current owner SQL/state and readonly lifecycle PASS for selected flows; full matrix PARTIAL|NOT VERIFIED / human pending|

### D — Remaining work and blocker disposition

| Priority | Scope | Work / classification | Disposition |
|---|---|---|---|
|Medium fixed|E31b/c/d|ECS-S31-03/04 card direct-flex children squeezed title/amount to letter columns|Single body wrappers fix detail and list; actual line/title regression + visual review PASS at final3viewports. No global CSS edit by author. |
|Medium fixed|E31d|ECS-S31-01 + S31-R02 stale line / in-flight submit / failed switch|Immediate reference clear, preserved other draft fields, sourceBusy/flight guard; component and actual switch proof. Independent source closure recorded. |
|Medium fixed|E31c/d|ECS-S31-02 source query mixed with old page|Applied-query snapshot guard and new-query page1 regression PASS. |
|Low fixed|E40f|ECS-C40-01 absent explicit successful empty-history state|Error→retry→empty regression PASS; no failed-read-as-empty behavior. |
|Remaining AUTO-RESOLVABLE QA|All15 actions|Complete exact BR/AC→scenario matrix beyond listed selected cases; positive Certification metadata update, broader denied/source-loss/fault/stale/large-data combinations|Do not infer these from6green composite cases. Coordinator's broader regression/CI and future bounded QA must close actual gaps. |
|Mandatory gate|All owned|Independent final integration review, final delivered SHA/current-head CI, explicit human UI/product acceptance|Pending coordinator/owner; no bypass or fabricated acceptance. |
|OUT this mission|FX31 siblings|Seller merge/private fields; comparison; Order transitions/returns/history/Trash/purge; Warranty evidence/reminders/Asset/Finance integrations|Unstarted/contract-gated as report21; not secretly implemented, retired or deferred to R2. |
|OUT this mission|FX40 siblings|Certification Files/encryption/automatic reminder/Trash/course links; gated Plan readiness|No new jobs/providers/grants; D01–D09 retained. |

### E — Final implementation/source identity

Owned changes are the four screen/component files listed above plus new `web/Nexora.Web/functional/completion-records.spec.ts`, report21+CSV and this report/evidence. No server/API/database/migration/permission change by this mission's worker. Shared scroll-lock was implemented/tested by the UI worker, not claimed as this author's fix.

Final SHA256: Shopping screen `7947895EE432245093411AD9AC14ABEE3B02098580C1E4443B1581B587A03072`; Certification `A3F321AC0AC895DD6FC86C587E6D37E8AEF30281E9500C811098E4C03D4710DA`; functional spec `3A3990BFB21D697FD47B2AC537EB6B1964A37EC5C3B15719A005E1C4FF58EC55`. Observed shared modal source `ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099`. Root owns final commit/diff/connector delivery; this worker did not commit/push.

### F — Final QA execution and curated evidence

| Case / source-frozen viewport | Actual result | Persistent report / screenshot prefixes |
|---|---|---|
|Shopping + Certification,1440×900|2PASS/0FAIL/0SKIP,16.2s| `1440x900/desktop-verified-junit.xml` + JSON; `records-cca82085`, `certification-cf6b905b` |
|Shopping + Certification,768×1024|2PASS/0FAIL/0SKIP,14.2s| `768x1024/tablet-verified-junit.xml` + JSON; `records-7c723fb5`, `certification-4e92b054` |
|Shopping + Certification,390×844|2PASS/0FAIL/0SKIP,13.1s| `390x844/mobile-verified-junit.xml` + JSON; `records-d15d409d`, `certification-9468fa56` |

All three final JUnit files were copied immediately and parsed:2tests,0failures,0errors,0skipped each. Final cases asserted restoration of starting module system/registration/user grant snapshots. Shared functional reporter was released to UI worker after copying; no concurrent further runner by this author.

Viewport proofs POSTFIX shared scroll-lock: Save/Cancel reached in scrollable dialog, actual Shift+Tab→Tab Cancel focus and outline/shadow, page wheel unchanged on nontrivial scrollable backgrounds. Curated metrics: desktop0/max897→0; tablet1311/max1311→1311; mobile1981/max1981→1981. Ten final PNGs were opened in this worker's final inspection: desktop Order top/actions + Cert history; tablet Warranty top/actions/detail + Cert history; mobile Warranty top/actions + Cert history. They show readable cards, clean responsive form fields, visible bottom controls/focus; no new blocking visual defect established in these views. This is not all screens/all states or human acceptance.

Final [curated evidence](../evidence/existing-records-20261026/README.md) includes10PNG +3proofJSON +3passing JUnit,16files/688707bytes (excluding README/manifest); [manifest](../evidence/existing-records-20261026/manifest.csv) has exact SHA256/bytes/raw source. These supersede provisional curation hashes. Prior failed/oracle diagnostics remain local: full timeout, exact wrapped-label/textarea selection, programmatic-focus versus true keyboard modality (2fail), and Range counting three inline text fragments instead of distinct visual rows (1fail). No dropped assertions: the final keyboard-ring and one-row currency assertions still execute. Corrected locators/metrics match actual accessible/rendered DOM; no production hardcoding or permission weakening.

Chromium viewport tests do not prove physical mobile/touch/Safari/Firefox/all WCAG. Actual browser fault injection/positive metadata edit coverage limitations remain as above. Root's final broad tests/current-head CI can add evidence but do not retroactively turn omitted scenarios into PASS.

### G — Decision

The authored defects are FIXED; the explicitly listed31component/client cases and6real UI/API/SQL composite cases are PASS on identified working-tree bytes. No Critical/High defect was newly established by this bounded review. These claims are independently checkable via source, tests, preserved failures and curated evidence.

**No whole FX31/FX40 or PR4 RELEASE READY claim.** Full per-AC coverage and mandatory integration/current-head CI/human gates remain. Unstarted siblings stay outside this execution mission, not silently deferred from committed R1. No merge/deploy/provider activation, real data, secrets or external effects occurred. Final owned source is frozen; future relevant changes invalidate affected evidence and require targeted re-verification.
