# E25 discovery state QA and broad-regression oracle reconciliation

Accountable worker: templates/UI foundation; coordinator owns runtime scheduling,
SQL operator and connector delivery. Branch `impl/m01-s00-scaffold`, base
`a0215f7b3e85d33503cff9077b41cc065240cb81` plus coordinated worktree.
Existing explicit user instruction and DEC014 automated-QA amendment authorize
isolated synthetic local QA. No product behavior, permissions, gates or providers
changed in this task. Human UI acceptance and current-head release CI remain gates.

## Contract and goals

Read current engineering startup/profile/TL rules, delivery and goal entry points,
verification rules and retained verification queue. Feature/action/UX25 sources,
module25 goals NXG-FX25-G01/G02/G03 and NXG-SYS03/08/13/14 bind to FX25-S01,
BR001/003/005/006, AC001/003. Concrete DISC3 recents and DISC4 palette contracts
are `remediation-plan-20261016/discovery-recents-contract-20261018.md` and
`discovery-palette-contract-20261018.md`. SavedSearches, RecentsScreen,
CommandPalette, their API clients, App canonical routing and SQL services were
read directly; definition persistence is `discovery.SavedQuery`, recent metadata
is `discovery.RecentItem`. Both remain owner scoped. The coordinator is adding
only literal read-only SQL operator keys for these tables through existing guards.

## Authored, not yet executed

`functional/discovery-state.spec.ts` contains three genuine browser/API/SQL cases:

| Scenario | Assertions defined | Current execution |
|---|---|---|
| Saved Search definition lifecycle | Native Save, schema1/query/owner SQL, reload/read, current results include a subsequently created source, native update, exact stale412 and unknown-version409/no write, UserB list/run/update/delete isolation, normal confirmed delete keeps source rows | NOT TESTED |
| Recents bounded clear | Canonical native source detail open records owned metadata; native clear preview; second same-owner page opens a later source; confirmation removes previewed metadata and preserves later row; explicit reload/refresh, UserB isolation, source rows unchanged | NOT TESTED |
| Palette keyboard/destructive handoff | Ctrl+K, finite current command search, ArrowDown performs no execute/clear, Enter executes only handoff, normal clear dialog cancellation preserves metadata, foreign-resource execute404 and source row unchanged | NOT TESTED |

Screenshots are defined for current Saved Search results/deletion, bounded clear,
and actual keyboard selection at each executing project's viewport. No screenshot
or runtime result for these new cases exists yet. No positive response mocks,
injected sessions or unsupported fixtures are used. Existing UserA/B genuine logins
and actual source API prerequisites are used; native UI owns the workflows under test.

Remaining coverage explicitly not implied: legacy unknown-version row UI migration
(API correctly forbids manufacturing schema99); Audit-row retention needs a suitable
read-only oracle; palette Create handoff/IME/session revocation and uncertain-response
recovery are not covered by these three cases. The new scenarios are bounded coverage,
not full E25 acceptance. No runtime launched while coordinator's broad writer runs.

## Two retained broad failures reconciled

Retained `artifacts/broad-desktop-initial-junit.xml` records News categories heading
timeout at native module entry and FN027 catalog exact-array failure. These failures
are preserved; no tests disabled or domain assertions removed.

* News now enters installed `NewsReaderScreen`; native `News views` navigation
  includes Categories and renders `NewsCategoryScreen`. Test follows that actual
  subview before its existing owner/default-name, dirty cancel, SQL, concurrent412
  recovery, paging and revoked-grant assertions. Existing approved stored-reader
  contract and source support this navigation; no new feature was implemented.
* Toolbox catalog assertions now enumerate exactly16 installed IDs, rather than
  the superseded original8. Approved pure/extra-offline/more-offline/URL-header and
  catalog-navigation contracts correspond to installed finite metadata. Additional
  assertion requires exactly5 Browser execution-mode entries. The unknown `http`
  gate404/ToolUnavailable, no outbound/no persistent canary and memory reset remain.
  No permissive subset/count-only oracle was introduced.

Both revised scenarios remain NOT TESTED after changes until scheduled execution.

## Actual check and fingerprints

Executed final strict compile only, exit0:
`tsc --noEmit --strict --skipLibCheck --target ES2022 --module ESNext --moduleResolution Bundler --esModuleInterop --types node functional/discovery-state.spec.ts functional/news-categories.spec.ts functional/toolbox.spec.ts`.
This does not prove runtime acceptance.

| File | SHA256 |
|---|---|
| functional/discovery-state.spec.ts | E7D9D0BFD0ACFC4B7D8C6B24F072EACEB32A13D295CDF8909A4759404D8ECD40 |
| functional/news-categories.spec.ts | C220FE43D203564D57F1692C024391ABF672EA4D8F3A68F57459D2A11E559A4B |
| functional/toolbox.spec.ts | 674C0610BAE1A42E46100DFF5D8701BFC9F7BF658A6BE0403FC1BFF637C8E4E8 |

Earlier final UI evidence remains saved: exactly9 synthetic PNGs under
`evidence/ui-foundation/`, per-image/source hashes in its README, actual opened
1440/768/390 populated/create/edit images and bounded3PASS evidence in
`workers/design-system-20261025.md`. These are separate executed scenarios and
must not be counted as execution of this newly authored E25 suite.

## First actual desktop execution and discovered handoff defect

Coordinator gave exclusive runtime GO. The owned helper executed5cases:3PASS,
2FAIL in29.8s. SavedSearch lifecycle PASS5.5s, revised News category case PASS4.7s,
revised Toolbox catalog PASS0.944s. Preserved reporters:
`artifacts/discovery-initial-desktop/{junit.xml,results.json}`.

Recents' response observer initially caught403 before-handler CsrfInvalid caused
by second-page antiforgery cookie rotation. Existing real API client refreshes once;
observer now ignores only that exact pre-handler code, requires final204 and at most
one refresh. No API response substitution or additional retry was introduced.
Palette option appears before its source search finishes; test now waits for actual
aria-disabled=false and asserts aria-selected=true before Enter.

Second run2cases: Recents PASS4.7s, Palette FAIL11.7s (18.6s total), preserved
`artifacts/discovery-retry-desktop/{junit.xml,results.json}`. Recents now executed
real UI→SQL preview watermark semantics, later-open persistence, owner separation
and unchanged source rows. Earlier table's NOT TESTED states are superseded only
for these explicitly executed scenarios, not all listed remaining coverage.

**E25-UI-01, Medium, AUTO-RESOLVABLE:** actual Palette Enter successfully reaches
the onHandoff callback and closes palette but leaves the canonical source URL
unchanged. App synchronously calls setPaletteOpen(false) then navigate while
CommandPalette.choose is busy; shared modal's registered busy leave guard still
exists and consumes navigation before React unmount cleanup. Needs a coordinated
post-unmount validated handoff that preserves guards/server authority. Coordinator
owns App and was notified; worker made no App edits. After source fix rerun actual
keyboard/destructive-confirmation case; no release acceptance while it fails.

Two sanitized retry screenshots copied before shared output reuse under
`artifacts/discovery-retry-desktop/screenshots/` were personally opened: Recents
bounded-clear confirmation and palette selected clear-recents command at1366x768.
Modal text/control hierarchy is readable, input and selected-option blue focus
visible. These images precede handoff, and cannot prove successful navigation.
Mobile E25 execution remains pending. Source fingerprints in authoring section
precede the synchronization edits and must be refreshed after final verification.

## Final corrected desktop and mobile verification

Coordinator implemented the fix in App; independent source review found no blocking
bounded defect. Pending handoff suppresses the palette even on `/commands`; passive
effect navigates after child modal cleanup, requires unchanged user/PersonalSpace/
role/modules scope and FX25 enabled, and uses normal navigate so other dirty-form
guards remain effective. No server authorization, target validation or grant widened.

After this fix, same final3 E25 cases ran desktop **3PASS15.5s**, mobile
**3PASS15.6s**. Three unique scenarios, six viewport executions; do not count
earlier overlapping attempts again. Saved Search5.3/5.5s, Recents4.5/4.6s,
Palette2.5/3.5s respectively. Reports/screenshots copied before shared output reuse:
`artifacts/discovery-final-desktop/` and `artifacts/discovery-final-mobile/`.
The previous Palette FAIL is retained and E25-UI-01 is **FIXED + TESTED** for this
bounded handoff, not full feature acceptance. News/Toolbox's final changed scenarios
each passed in the initial focused run; no related production source changed here.

Final actual screenshots personally opened: desktop Saved Search populated current
results, mobile same populated results, mobile Recents bounded-clear dialog, mobile
Palette keyboard-selected command. All readable at sampled widths, no horizontal
overflow (also asserted by scenarios); mobile controls wrap vertically. Raw JSON
definition remains a presentation limitation of the existing screen, not replaced
by this test-only work. Curated4 PNGs and hashes:
`evidence/discovery-20261027/README.md`. Final shared9 prior images remain separate.

Final strict compile of all3 assigned test files exit0. Final fingerprints SHA256:

| Source | Hash |
|---|---|
| functional/discovery-state.spec.ts | 44DEEA5BEC0F3A45233EEEF1AC898F5685067F60345DCF385B3A8B34CB520006 |
| src/App.tsx | A5EFFE1994F23752919DE4D816BB213DF022CAA597C05C9151933EAB0A359ABB |
| src/SavedSearches.tsx | 276B986A9002033500E13C39D36A4A401F7A84507F253B594778D6E20E86C1DA |
| src/RecentsScreen.tsx | D4FEF9753830ED6535096EA58A0AEAEF077153B4E38FD1A79C60BDDC236D4DFF |
| src/CommandPalette.tsx | 62A4FF62EEA4BB3E82A552A75ABFFF55EDA71042DEF864A1C1060473DB47BB77 |

Earlier explicitly listed uncovered scenarios remain uncovered. Full E25 acceptance,
human UI/product review and release current-head CI remain pending separate gates.
Runtime writer released to coordinator; no ongoing child browser process.
