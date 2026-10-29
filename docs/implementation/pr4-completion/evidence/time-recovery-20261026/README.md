# FX18 real-commit recovery evidence — 2026-10-26

Scope: existing manual-create/edit, Stop and Resume recovery only. Base revision `a0215f7b3e85d33503cff9077b41cc065240cb81` plus fingerprinted worktree below; coordinator must reconcile to delivered commit. Runner: approved `.tmp/run-nexora-browser.ps1`, owned f47 local runtime, genuine synthetic-user login, normal module-grant preview/commit and isolated SQL operator. No production/provider effects, injected session or positive mock response.

For each first request, Playwright `route.fetch()` executes the actual backend/SQL commit, then aborts only its response. Retry again reaches the backend and returns its actual response. Assertions retain original body/key/ETag/ID and verify persisted SQL state/owner. No assertion was disabled.

| Scenario / requirement binding | Desktop1366×768 | Tablet768×1024 | Mobile390×844 | Actual assertions |
|---|---|---|---|---|
| TT-R01 manual create/edit — time.entry.create/update; FX18-S03; shared idempotency, UX05 | PASS | PASS | PASS | Frozen form/cancel/Escape; exact retry body/key/ID; exactly one named stopped entry; SQL owner; metadata edit retains UTC instants/duration |
| TT-R02 Stop — time.timer.stop; FX18-BR002/AC001 | PASS | PASS | PASS | Original ETag/key replay; SQL EndAt unchanged; no running timer; actual server-generated timestamps retain full precision through metadata edit |
| TT-R03 Resume — time.timer.resume; FX18-AC002 | PASS | PASS | PASS | Exact source/body/key/ID replay; one new current Running timer; original stopped SQL source unchanged; only own resumed timer stopped during cleanup |

Final commands: helper `-Suite functional -Project desktop|tablet|mobile -Grep 'FX18 lost '`, sequential exclusive reporter window. Desktop **3PASS9.4s**, tablet **3PASS9.7s**, mobile **3PASS9.9s**. **3unique scenario definitions / 9viewport executions**. Existing repeated executions are not additional unique cases.

Final reports retained locally as `web/Nexora.Web/artifacts/time-recovery-{desktop,tablet,mobile}-final-results.json` and corresponding `-junit.xml`. Private runtime credentials/storage/raw logs are not copied here. The earlier shared-reporter race copied Shopping output; it is explicitly excluded from Time evidence. Initial failed runs are preserved/reported in the [worker handoff](../../workers/time-bookmark-qa-20261025.md).

## Sanitized visual samples

These are actual modal-only screenshots from final runs, containing synthetic fixture text only. Desktop manual sample shows the top of the scrollable form, not the entire form; successful retry/save used the real scrollable controls. They do not establish complete visual/a11y acceptance.

![Desktop manual save with retained frozen input](desktop-manual-pending.png)

![Tablet Stop response recovery](tablet-stop-pending.png)

![Mobile Resume response recovery](mobile-resume-pending.png)

## Source and complementary QA

| Source | SHA256 of executed bytes |
|---|---|
| functional/time-recovery.spec.ts | D1E2C39039147D38FAF8DF71EFDA7AF696215F9A676AA2156B1707CA8A96D543 |
| src/TimeTrackingScreen.tsx | 207646259931BCEFE37042FC510895D1FD84732DE52EC7CA7B74272D0391E9B2 |
| src/timeLocalInput.ts | D34E5864E0EBEB0360415AA10513335629BA6E3CBE8CC958B6ECF024487C7D64 |
| src/timeApi.ts | 1EA4675E9B2027A11EA01C6B8C27EF620613E11990C71605E191EE348ABD1CCA |

Focused unit/component/client tests: **57PASS**,4files,10.29s:35Time component,17explicit-zone date cases,3Bookmark component,2HTTP client. These are separate layers and do not multiply feature coverage. Final frontend typecheck passed. Explicit-zone pure tests cover DST gap/fold/half-hour/date-line transitions, known raw7fraction instants,100ns ordering and UTC year1..9999 boundaries; browser scenarios prove actual SQL/runtime recovery, not every timezone/history rule.

Open: whole-FX18 source-linked workflows/full permission/lifecycle coverage, broad regression/CI, independent review and final human UI/product acceptance remain separate gates. Later relevant source edits invalidate affected evidence. **No whole-feature or release-ready verdict is granted here.**

Post-run boundary-only hardening: editor open now displays an actionable conversion error when an otherwise valid stored UTC boundary instant cannot be represented in native local-year1..9999 controls, instead of throwing. Current TimeScreen SHA `09EB564810F577D12E16D9D1E8354356BB9A402A3686FF41BE644104CC1ED9A8`; latest focused4file suite **58PASS10.42s**, final typecheck exit0. The9executions above retain their exact executed fingerprint207646…; root owns final current-head reconciliation. Normal successful date conversion, persisted command and recovery code did not change in this guard, but these old run records are not relabeled as executions of the new bytes.
