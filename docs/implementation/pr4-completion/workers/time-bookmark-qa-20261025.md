# FX18 / FX21 recovery QA continuation

Author: learning_completion; coordinator: root. Source base: PR4 branch `impl/m01-s00-scaffold`, `a0215f7` plus current working tree. Authority: 2026-10-25 automated-QA amendment in current AGENTS/DEC014; supersedes this worker's earlier code-only task. No providers, real data, SQL runtime or shared browser executed by this worker. Root owns App.tsx/central integration and final commit.

Sources/rules loaded: AGENTS; nexora-engineering skill; implementation profile; Technical Lead README/core; routing baseline/frontend/verification paths; current delivery scope/DEC014; goals SYS05/11/13/14, FX18G01/G03, FX21G01/G02; feature/action/UX18 and21, Bookmark manual slice, goal evidence template. Selected contracts: FX18-S01/S03 (`time.timer.start`, `time.entry.create/update/read`, SELF), FX18-BR001–003/005 and AC001; FX21-S02 (`bookmarks.bookmark.create/update`, ETag/idempotency) and existing FX24→Bookmark draft handoff. No change to approval/security gates or committed product scope.

## Changes

- Existing immutable uncertain-save recovery now has component fault/duplicate/permission/validation/expiry/abandonment/read-refresh coverage.
- Fixed hidden authorized timer start for start-only actors; no timer/list/report query without respective read capability, no false empty timer claim.
- Current ModuleUnavailable/DependencyUnavailable/ResourceUnavailable clears stale private list/form; 401 clears and invokes authentication recovery,403 clears without authentication redirect.
- Added412 draft retention, exact-ID current revision comparison and explicit reapply with a second current ETag check. Concurrent second revision keeps Save blocked. Current read revocation clears private form.
- Fixed metadata-only edits truncating existing instants to minutes: preserve seconds/milliseconds and set datetime-local step0.001.
- Added Bookmark408 same-body/key/ETag regression and actual Shell Template→create-only Bookmark draft/submission regression. App changes delegated to root.
- Extended immutable uncertain recovery to existing Stop/Resume and Trash/Restore/Purge commands: frozen source/key/ETag, synchronous latch, dedicated recovery modal, same-request retry, two-step informed abandonment and denial clearing. Navigation/unload is blocked during in-flight direct commands and unresolved outcomes. No new feature family/API/schema.
- Added negative Bookmark abandonment coverage: canceling abandonment preserves frozen draft; confirmed informed abandonment closes and releases modal isolation without repeating mutation. Root fixed the408 enabled-cancel defect and owns those App bytes.

## Execution and remaining gates

Final focused run: `npm run test:unit -- src/TimeTrackingScreen.test.tsx src/BookmarkRecovery.test.tsx --reporter=dot` from web/Nexora.Web, **36PASS,0FAIL,0SKIP**,2files,11.38s, Vitest5.0.5. Unique cases:33TimeTracking and3Bookmark; earlier17/23/26/27/25/36-case executions are reruns/subsets and must not be added as extra unique cases. Earlier combined26-case run had25PASS/1FAIL: Bookmark408 exposed enabled Hủy on unresolved save; root wired ResourceFormDialog.closeBlocked and explicit informed abandonment. Fresh final regression above passes all3Bookmark cases.

`npx tsc -b --pretty false` passed on the final direct-command extension (exit0). `git diff --check` for the2owned production files passed. Two invocations from repository root failed ENOENT(package.json absent); corrected to web/Nexora.Web and actual focused runs above executed. No SQL/API integration, real browser E2E/responsive/a11y or whole-feature acceptance inferred from mocked boundary component results. Final human UI review, independent review/current-headCI remain pending. Existing full source-link/report/lifecycle acceptance breadth is not certified by this focused recovery patch.

Final working-file SHA256 (before root commit/newline normalization):

| File | SHA256 |
|---|---|
| TimeTrackingScreen.tsx | 27DE71851F2FBB8B3E92BE2E1CF4BDDB0E8D421082C1B6692EC69E7A2B587F5F |
| timeApi.ts | AD3245ECCB004FD6D794EBBCA247B9092AD51261DC182F8309F8808554A5B882 |
| TimeTrackingScreen.test.tsx | D4BDED94573F269A80E72382B059B3B886ADC2FEF3899FC61A020FD1F7B6E04E |
| BookmarkRecovery.test.tsx | F1FC8DD937180B4611809448F659FF919849F0BB51C48F9DAAF14E325FDF123C |

Rollback: revert these source/test changes; no schema/data migration. Status: bounded source remediation and focused component verification, **not full FX18/FX21 RELEASE READY**. Root reconciles final evidence to delivered commit.

## Scheduled real-SQL browser recovery proof (authored, NOT RUN)

Added `web/Nexora.Web/functional/time-recovery.spec.ts`:3scenarios for manual-create lost response plus metadata precision, Stop lost response, and Resume lost response. Each uses legitimate existing synthetic fixture login/approved module enable helper. Fault injection first calls `route.fetch()` against the real server/SQL commit, then aborts only the successful response. Retry executes the actual backend again; assertions compare original body/key/ETag/resource ID, owner-scoped SQL records and source/current timer state. No synthesized successful result or provider effects. Existing Stopped-only entry-list contract is respected; Running uses its separate read action. Cleanup restores module configuration and stops only the resumed timer identified by this test's committed response.

Executed compile only: `npx tsc --noEmit --target ES2022 --module ESNext --moduleResolution node --esModuleInterop --skipLibCheck --strict functional/time-recovery.spec.ts`, exit0. File SHA256 `8696333D05D9EA957B6C5B78112120C140FF3F624ABDD3E457745FCBAAB8CC6E`. **Browser scenarios not executed**: coordinator reserves runtime/permission schedule until UI agent finishes. Their3authored cases are not added to the36executed component cases. No runtime/release promotion from this code or compile result.

## Independent-review follow-up: raw precision and DST

Root review identified that millisecond-only regression missed SQL datetime2(7) precision and ambiguous/nonexistent local-time conversion. Fixed under UX05 forms/date validation and FX18AC003: a Time-only utility preserves the original raw UTC/offset string if displayed local fields are unchanged (including a known repeated-hour instant), compares instants at100ns resolution, and resolves edited values against the explicit browser IANA zone. Impossible or ambiguous edited local times are rejected with an actionable message; no guessed fold offset or new offset-selection business UI. The current browser zone is shown. Existing App date helpers were not reused or changed.

The utility enumerates nearby actual zone offsets through Intl and roundtrips every candidate to the entire local timestamp; half-hour/date-line transitions are included. Timezone interpretation depends on runtime Intl/IANA data; this is not a promise of identical historical/future timezone database versions across environments. New input precision remains the native editor's milliseconds; unchanged server values retain all7fraction digits. Known unchanged instants are preserved even when ambiguous in local wall time.

Final focused command `npm run test:unit -- src/timeLocalInput.test.ts src/TimeTrackingScreen.test.tsx src/BookmarkRecovery.test.tsx --reporter=dot`: **54PASS/0FAIL/0SKIP**,3files,9.86s;35Time component,16utility,3Bookmark unique cases. Explicit zones: NewYork/Berlin gaps/folds, LordHowe half-hour changes, Apia skipped date, Kathmandu non-hour offset, DST elapsed-hour comparison, invalid calendar/clock values, early0099year, knownfold7fractions and100ns ordering. Components add7fraction raw persistence and submillisecond known duration. These supersede earlier36-case focused coverage for the amended source; counts are not cumulative.

Initial utility run failed35cases/27render errors due this author's assumption that iso8601 Intl emitted an era part. Diagnosed actual formatToParts output, used explicit Gregorian calendar and reran the same assertions successfully; no assertion disabled. `npx tsc -b --pretty false` exit0 after final source changes. No source is promoted to release-ready from these component/pure tests.

Current SHA256: TimeScreen `207646259931BCEFE37042FC510895D1FD84732DE52EC7CA7B74272D0391E9B2`; timeLocalInput `A0D05D8E8F19DD041D7ACF2E1437B38D69C145C6DEA122CF13573FC61C848E02`; TimeScreen tests `A312AEC0633696F07471360D2A1184CC3A6FFA4D17A6C998413A835C906E04C9`; utility tests `5DABF5EFAC0160166A4170969D24FC84BD70277165328B26E69E28D4D67E55C4`. Functional test now also verifies raw actual server-generated Stop timestamps survive metadata-only edit; current functional hash `383D1B6D934093CDD2F8D3D7301DA2312225002330E9B46A01C41674F6D23091`. Three true-fault desktop cases remain NOT RUN pending coordinator runtime helper/schedule at this report update.

## First real browser run and remediation

Executed owned `.tmp/run-nexora-browser.ps1 -Suite functional -Project desktop -Grep 'FX18 lost '` against f47 isolated local SQL runtime (default sandbox denied ACL-protected runtime metadata; authorized escalation executed the existing helper). Actual result **2PASS/1FAIL**,25.9s. Manual lost-response retry/SQL/metadata proof and Resume realcommit/replay/source-retention passed. Stop realcommit/retry/SQL timestamp checks passed, then failed the declared client null expectation: real server204 produced undefined through generic apiFetch. Fixed owned timeApi.timeTimer to normalize empty204 to declared null; retained the assertion and added HTTP204/null + HTTP200/running-preservation client tests. No production/server gate or assertion was weakened.

Shared functional JSON/JUnit was concurrently overwritten by Shopping; inspection showed completion-records.spec.ts/2PASS. Those copies are explicitly renamed `artifacts/shopping-concurrent-copy-*` and are **not Time execution evidence**. Actual initial Time outcome is retained in `artifacts/time-recovery-desktop-initial-execution-summary.json`, based on tool session61843 completion; first failure context named `time-recovery-FX18-lost-St-39321-tains-one-stopped-SQL-entry-desktop/error-context.md`. Coordinator notified. Further browser runs pause until exclusive reporter window is granted.

Added UTC-range guard for edited local values resolving before0001/after9999 (Tokyo/NY boundary assertions). Latest focused4file command (prior command plus `src/timeApi.test.ts`) **57PASS/0FAIL/0SKIP**,10.29s:35componentTime,17date utility,3Bookmark,2client. Not cumulative with older runs. Latest utility SHA `D34E5864E0EBEB0360415AA10513335629BA6E3CBE8CC958B6ECF024487C7D64`; utility tests `0566F6FF87A3D109EA9C74BD5E99A83B0F97A40BE8CE808A265A62B65DDFB8A7`; timeApi `1EA4675E9B2027A11EA01C6B8C27EF620613E11990C71605E191EE348ABD1CCA`; timeApi tests `B2D18E828E914E28B468AC279BB69D5C3DE087219D0EEB676E69CFF50BD7B1BB`. Final desktop recovery rerun and tablet/mobile remain pending; initial failure is not a PASS.

## Final scheduled runtime completion

Owned f47 runtime, exclusive reporter window: **desktop3PASS9.4s; tablet3PASS9.7s; mobile3PASS9.9s**, source-consistent final capture runs. Three unique scenarios,9viewport executions. Preserved separate final JSON/JUnit per viewport before the next writer. Root and Shopping author notified window free; no browser processes left by this worker's runs.

An intermediate desktop run after204 correction had2PASS/1FAIL at manual metadata Save: repeated synthetic runs/viewports intentionally reuse the same2003interval, while the edit had reset overlap confirmation. Corrected the fixture's user interaction to explicitly check the unchanged production overlap confirmation before editing, as creation already does. Assertions for exact retry/deduplication, owner/SQL/unchanged timestamps and duration remain unchanged. Full failed run retained as `artifacts/time-recovery-desktop-before-fixture-correction-*`; initial204 failure remains separately recorded. Final9executions above passed after correction.

Curated source-bound summary and three sanitized modal-only PNG samples: [evidence/time-recovery-20261026](../evidence/time-recovery-20261026/README.md). Final functional hash `D1E2C39039147D38FAF8DF71EFDA7AF696215F9A676AA2156B1707CA8A96D543`; production Time hashes remain the latest values above. Ordinary sandbox shells began hanging during reads/copies; authorized escalated local helper/read/copy commands completed, without killing unknown processes or changing product gates. No claim of all-FX18 or all-release acceptance; root owns final commit/CI/independent gates and human UI acceptance remains pending.

Final failed-open boundary hardening: a valid stored UTC instant at year0001/9999 may display outside the native local-date range in some zones. The formatter already explains that unsupported local range; editor open now catches and displays that error instead of throwing a pageerror or inventing a replacement instant. Added component error-path regression. Latest focused **58PASS**,4files,10.42s (36Time component+17utility+3Bookmark+2client); typecheck exit0. Current TimeScreen SHA `09EB564810F577D12E16D9D1E8354356BB9A402A3686FF41BE644104CC1ED9A8`, tests `572C64F4BA96C02CEB76167B7B143B407C997F99850969C8CABA14E62E8AB18B`. This failed-open-only guard followed the9successful viewport runs, whose executed source fingerprint remains207646…; root notified to reconcile affected final regression/current-head evidence. No further production changes planned by this worker.
