# Independent bounded delta review — 2026-10-29

Reviewer: `independent_finish`; accountable coordinator: root. User authorizes parallel independent review and isolated automated QA under current DEC014 amendment. Source branch `impl/m01-s00-scaffold`, base `a0215f7b3e85d33503cff9077b41cc065240cb81` plus working files fingerprinted below. No production edits by this reviewer. New regression test authored only after coordinator assigned that disjoint file; root retains production remediation ownership.

Loaded AGENTS, engineering skill, implementation profile, Technical Lead core rules, routing frontend/security/verification/owner-isolation instructions, goal evidence template and preceding bounded review/modal/recovery reports. Review binds existing FX18-S01/S03, FX18-BR001–003/005/AC001, FX21-S02 and FX24 Bookmark handoff, UX08/11 modal/dirty guards, FX25 command execute/handoff, and existing FX40 evidence/merge. Goals SYS05/11/13/14, FX18G01/G03, FX21G01/G02 remain the prior authoritative trace; this report adds no product requirement or scope approval. D01–D09, Files three-principal proof, final current-head CI, complete AC coverage and explicit human UI acceptance remain independent gates.

## Findings and source conclusions

| ID / severity | Surface | Finding / disposition |
|---|---|---|
| SK-REV-01 / Medium | `SkillEvidence.tsx:56`, `SkillScreen.tsx:110` | HTTP408 is incorrectly classified as definitive. Evidence drops immutable pending closure and enables editable note; merge discards preview and retry UI. A timed-out write may have committed. Reproduced with actual component interactions and MSW HTTP408 through real adapters: **2FAIL**. Root notified; production remediation pending at this checkpoint. |
| QA-REV-02 / Medium | `functional/started-flow-access.ts` | Sequential restoration stops at first rejected grant/policy operation and skips remaining restoration attempts. Synthetic prestate checkpoint exists, but not sufficient for best-effort cleanup. Root owns remediation; no false restoration claim. |
| No blocking finding | Palette `App.tsx` scoped handoff | Pending handoff temporarily unmounts Palette, releasing its busy guard before ordinary guarded navigation. Current user/PersonalSpace/role/module scope must still match and Search must remain available. Other dirty guards are retained. No permission bypass. This source conclusion is consistent with earlier independent review; reviewer ran no Palette browser scenario. |
| No blocking finding | Bookmark408/create-only | Immutable original key/body/ETag retained; dialog close blocked until explicit two-step informed abandon. Create-only capability path avoids library read and keeps draft opener available. Component scenarios pass; does not certify all Bookmark acceptance. |
| No blocking finding | Time immutable commands/date boundary | Saved immutable closures, synchronous latch, exact-ID/ETag comparison before explicit reapply; exact original SQL fractional ticks preserved for unchanged form times. Edited local values reject gaps/folds and UTC outside SQL range. Failed-open out-of-local-range exception is caught. Focused tests pass; no browser/SQL proof by this reviewer. |
| No blocking finding | `modalIsolation.ts` | Refcount per Document, idempotent release, root/body overflow axes+priorities and prior padding restored. No auth/focus guard weakened. Reviewed final mixed-axis correction already independently probed by prior reviewer; this reviewer executed jsdom regression only. |
| No blocking finding | WorkLog / GoalTargetManager | Redundant internal editable Cancel removed; shared footer controls busy/dirty/discard and explicit Continue preserves fields. Four current regression cases pass. |
| No blocking finding | Vite watch ignore | Excludes generated QA output directories only; source/API proxy/security configuration unchanged. Source fix addresses recorded locked Chromium partial-download watch crash. No claim this reviewer reproduced original Windows watcher crash. |
| No blocking finding | New read-only operator mappings | Static kind allowlist and UUID parameter retained after exact disposable local DB guard. Schedule resolves current version through `(OwnerId,DefinitionId,VersionNumber)` join; Connection explicit metadata omits Vault reference, credentials and auth material. Privileged test SQL is evidence inspection, never server authorization proof. |

Career appointment / Skill evidence+merge browser specs reviewed as meaningful bounded normal UI paths with real API/SQL owner assertions, duplicate/foreign-owner rejection and preserved source checks. They do not exercise uncertain Skill outcomes; new component regression supplies that missing fault layer. They were not run by this reviewer and remain coordinator-owned. Shared helper cleanup finding applies to both.

## Actual verification

Environment: local Windows, Node24.17, installed Vitest5.0.5/jsdom, synthetic MSW fixtures; no SQL/browser/live provider execution in this review.

1. From `web/Nexora.Web`: `node node_modules/vitest/vitest.mjs run src/TimeTrackingScreen.test.tsx src/timeLocalInput.test.ts src/BookmarkRecovery.test.tsx src/GoalTargetManager.test.tsx src/WorkLogScreen.test.tsx src/modalIsolation.test.ts src/ResourceFormDialog.test.tsx` — **69PASS**,7files,0fail/skip,56.87s; preserved `web/Nexora.Web/artifacts/independent-focused-20261029-junit.xml`. Existing async `act` warning retained. These are unit/component checks, not E2E, no additive total with earlier reruns.
2. New `src/SkillRecovery.test.tsx`: `node node_modules/vitest/vitest.mjs run src/SkillRecovery.test.tsx` — **2FAIL**,4.00s before remediation. Exact failures: note textarea not disabled after408; merge retry action absent after408. Preserved `web/Nexora.Web/artifacts/skill-http408-before-20261029-junit.xml`. Both fixtures send HTTP408 via real API adapter, then expect immutable exact-body/key/ETag retry; no production or assertion weakening. Correction/rerun pending.

## Fingerprints at review

| File | SHA256 |
|---|---|
| App.tsx | A5EFFE1994F23752919DE4D816BB213DF022CAA597C05C9151933EAB0A359ABB |
| TimeTrackingScreen.tsx | DA1D5AA2AEA56D9C688D0D5AAFCE3E28653BA8F408B4326FACDBD6C44AA8D618 |
| timeLocalInput.ts | D34E5864E0EBEB0360415AA10513335629BA6E3CBE8CC958B6ECF024487C7D64 |
| modalIsolation.ts | ECE837490D35F7A74587516E85B3743750323C0EEAFBADAAAB13D4A136562099 |
| GoalTargetManager.tsx | 702858CEB253468E6B9F3E9737BFB7A48D3FE4AE8971C46E9D37235BC6407530 |
| WorkLogScreen.tsx | 70AFE60628B8D4C7BAE3CFF1A3086033F7CA89D5D458E278EE2A3E9A1E28A932 |
| vite.config.ts | F169F78837E5A4F02DC5664B47389F29C901B9302B16DF857D802EC2319E1388 |
| SkillEvidence.tsx before correction | 44D90BD70235CEA66A1F2462FF791DCEC4B41955AE7BB351667B9681C0F95498 |
| SkillScreen.tsx before correction | BC6392F8243B4EFC9E7E379B159AE1872064EAAA96B2FEC68CBB09164D4FDBF5 |
| SkillRecovery.test.tsx | 472B8BEA3CCEC085B710D9F5E0C0088F54CD9903E979176949F82232C63D9A20 |
| FunctionalTestOperator/Program.cs | 72BC0BE2A878971EBD2F03751423BE6616B333181B8CAF78356B1AD1503D1D16 |

No Critical/High defect found in this bounded source pass. Medium findings above require correction and execution before their workflows are automated-QA verified. This is not whole-feature, whole-R1 or human acceptance certification. Coordinator must reconcile hashes to delivered revision and rerun any affected checks after further relevant edits.

## Remediation and additional independent pass

Root added only `status === 408` to the two Skill uncertain-outcome branches. Reinspection confirms existing immutable evidence closure / merge preview+key and permission checks remain intact. **SK-REV-01 resolved in source and focused component execution:**2PASS,2.69s; `artifacts/skill-http408-final-20261029-junit.xml`. Evidence source SHA256 `287EBE2EEA2F800CCA16EAEB318B228EDBFBD75C9F4F5CE85616E9C925A77D0F`; SkillScreen `DFFC0D4D1A99C39C7E1C0926EEC231B80C8DCE35018840950D24A7FB2D65D795`.

Root changed shared `started-flow-access.ts` cleanup to independently attempt every grant restoration, policy restoration, final state assertion and context close, retain checkpoint unless all assertions pass and aggregate setup+cleanup failures. **QA-REV-02 resolved by independent source reinspection**, hash `743025D92FC08E6094BD92634787E70897951B966A8CBF4AFCCADB6EC0FCD772`; actual interrupted-run restoration is never inferred from this code change.

Root's local startup switches default false; scheduling requires explicit local Task execution opt-in before any environment/SQL mutation. Existing exact local instance, unique test DB, SID/ACL, ownership and generated secrets remain. PowerShell parser PASS; actual scheduling-only invocation refused with the precise guard before side effects PASS. Hash `2E615C57E845C86377C6F41FF45DF576F428346337D1D78D0E1CA5D778E35C05`. These two flags do not alter real-provider configuration; no broad security finding in the bounded change. This check does not execute positive worker/scheduler scenarios.

Automation author's four analogous HTTP408 predicate changes independently inspected in AutomationScreen/ExecutionDialog/LifecycleDialog/ScheduleDialog: they preserve pending body/source/ETag/cohort/key and existing ACK validation/current-authority checks. New four component tests assert frozen exact adapter arguments; not SQL/provider proof. Reviewer executed `node node_modules/vitest/vitest.mjs run src/careerApi.test.ts src/SkillRecovery.test.tsx src/AutomationRecovery.test.tsx`: **7PASS**,3files,0fail/skip,21.35s, preserved `artifacts/independent-recovery-final-20261029-junit.xml`. Repeats/subsets of earlier Skill execution are not additional unique cases.

## Career/Skill browser initial execution — failures retained

Executed exclusive default-mode isolated SQL/browser desktop on owned run `nexora-e2e-audit-cf6948078be746e7a431fb2fecba5f47`, via `.tmp/run-nexora-browser.ps1 -Suite functional -Project desktop -Grep 'Career appointments|Skill evidence/merge'`: **2FAIL**. Preserved `artifacts/career-skill-review/desktop-initial-junit.xml` and `desktop-initial-results.json`.

* **CAR-REV-03 / Medium, confirmed production integration defect:** native Job/links reload returns400 before reading empty links. `careerApi.ts` always appended `?cursor=` while actual `AppointmentEndpoints.cs:14` requires nullable UUID cursor; empty string is not omitted. Root corrected adapter to omit blank query, retaining real UUID continuation and backend validation. New `careerApi.test.ts` first/default blank/continuation boundary assertions PASS in run above. Browser rerun remains pending.
* Skill browser locator oracle: wrapped label combines descendant option text, so exact `getByLabel('Trạng thái Course')` never matches; no evidence of a product failure there. Corrected only selectors to anchored role/name for Course status, Course and Note; also added15s action timeout to fail before full test timeout and allow cleanup. The initial180s timeout closed the test context before grant restoration; actual restoration remains **NOT VERIFIED**, coordinator notified with safe synthetic checkpoint `artifacts/started-access-1793291353755-e219f504-0d3a-47fd-a1be-642f5b18087f.json`. No assertion deleted/weakened. Career wrapped select selectors corrected in the same way before rerun.

The new Skill test initially used Testing Library's unsupported `exact` option on one role query; strict tsc caught it. Removed that option (string role names already match exactly) and preserved assertions. Subsequent compilation was blocked by the independent Automation test author's same option; author notified and corrected. A complete final compiler execution remains coordinator-owned until explicitly rerun.

Final reviewer `node node_modules/typescript/bin/tsc -b --pretty false` completed **PASS exit0** after both test authors corrected unsupported role-query options. This is a compiler check, not functional acceptance.

## Documents presentation review (before final browser rerun)

Independently inspected DocumentExportDialog full source, exact App `documents-workspace` addition, scoped CSS and transfer third-case diff at Documents author's request. Bounded presentation delta has no blocking source finding: Documents-only full-width stacked canvas/static editor;75ch preview and heading scale; portal content grid, wrapped actions, aligned confirmations; friendly browser-local time and status copy. Preview warnings, exact acceptance checkboxes/confirmation, capability reads, target revision, immutable serialization and normal dirty guard remain. Final actual desktop/mobile screenshots must still be opened and inspected; this report does not self-approve visual quality.

Third export408 test uses actual `route.fetch` SQL commit followed by a synthetic dropped response semantics; frozen key/body hash/receipt ID plus unchanged saved source is meaningful recovery evidence. Asked author to additionally assert original If-Match. Read-by-ID length1 plus same replay ID is receipt identity proof, not a total count of all possible artifact rows; reporting must stay bounded. Existing408 clear-intent bug is not silently treated as fixed by the presentation change; author owns real failure/reproduction and correction, pending rerun.
