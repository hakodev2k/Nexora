# PR4 narrow independent integration review — 2026-10-24

## Verdict and task brief

**SOURCE_REVIEW_COMPLETE / NO_BLOCKING_INTEGRATION_FINDING; one Low/P3 diagnostic regression.** This is an independent source review of the specified merge resolution, not build, test, Windows ACL, browser, release or runtime acceptance. All execution gates are **NOT_RUN_BY_THIS_REVIEWER**. Root integration owner retains responsibility for build, final regression, merge staging and GitHub publication; PR4 must remain Draft.

- Requested scope: review four integration surfaces only: `tests/Nexora.UnitTests/Program.cs`, `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs`, `scripts/dev/test-filesystem-windows-principals.ps1`, `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`; inspect `scripts/dev/test-filesystem.ps1` and directly related harness contracts. No broad feature audit.
- Authorization: current user explicitly restricts this review to code-only, no tests/runtime/new fixtures, no source edits. This supersedes earlier broad execution requests for this bounded assignment. Only this report was written.
- Branch: `impl/m01-s00-scaffold`.
- Local HEAD: `ac64d7605e767e7dc87212c32cb7f40684bc3d35`.
- MERGE_HEAD: `5be1f5424f0b1d7ceb65ced184d00e467b6f1b11`.
- Reviewed working-tree contents, not only index blobs: the three resolved files still appeared `UU` in the index at inspection. No conflict-marker lines were found in the four reviewed source surfaces. This does not mean the merge has been finalized or committed. Stage the reviewed resolutions through the integration owner; no index change was made here.
- Binding goals/contracts: `NXG-SYS-08` (capture protection), `NXG-SYS-13` (keyboard/modal UX), `NXG-SYS-14` (fresh evidence); `M01-S10 / M01-AC10`, `M01-S11 / M01-AC11` as mapped in the existing Windows handoff; D09 Windows actual A/B/C and current-source evidence gates. LOCAL probe commands are test-runner interfaces, not new application permission/action keys.
- Current PO authority read: DEC-20260909-014 and DEC-20261019-PR4 D01–D09, especially D09; `06-release-verification.md`. No production/provider or gate-policy change is approved by this review.

## Reviewed source fingerprints

SHA-256 of actual file bytes read on 2026-10-24. These bind this review to dirty merge contents; neither parent commit alone identifies that content.

| Path | SHA-256 |
| --- | --- |
| `tests/Nexora.UnitTests/Program.cs` | `7000524df79ee53adb12c49d49802129b647a49a189f8acbb103a121125e17c4` |
| `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs` | `81155d5babdcc813663f6765f4d4a86532892c2a4e0df9d51c40495736959b3b` |
| `scripts/dev/test-filesystem-windows-principals.ps1` | `a9c640f6f08b1e57df2b4de1670b3806b3e85310babcf6f9ac771c9fff26fcba` |
| `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts` | `2f8260ad92dfdaf119e829f15edd2ce65222068426f7045c8fe738a4946148cb` |
| `scripts/dev/test-filesystem.ps1` | `991300ea07c7310b5f78024237e2af7d8813608f353808953305db03b4fb3a1b` |
| `tests/Nexora.UnitTests/WindowsCaptureProcessTests.cs` | `a43515b725ba43601fe1a2515fa5a44c0ca9d224c964224f532618882457be74` |
| `tests/Nexora.UnitTests/WindowsCapturePrincipalTests.cs` | `f337154d7cd5053f75496b6e1dde7c010968fe3f2d7573d4fcfac1cd9aab970e` |
| `tests/Nexora.UnitTests/TestRunner.cs` | `5e3e29c4fea8549bd83667a2cc4aa57ae5bfe10217026cba5c5d6cf7b496d860` |
| `scripts/dev/test-filesystem-windows-principals.md` | `73b772044afb8f99ae19d7dc2a75cd33a93814b7938aba625f1ca59a9dac4128` |

## Integration conclusions

| Surface | Source-only observation | Disposition |
| --- | --- | --- |
| Canonical dispatch | `Program.cs:16–18` requires exactly two arguments and dispatches `--windows-capture-probe <config>` to `WindowsCaptureProcessTests.Probe`. Canonical `Launch` emits exactly this flag and config-path argument. Existing `--hold-capture` remains separate. | Compatible at source level. |
| Optional manual dispatch | `Program.cs:22–24` requires seven arguments for `--windows-capture-principal-probe`; PowerShell `Invoke-Probe` emits that renamed flag plus mode, workspace, runtime SID, operator SID, expected SID and message ID. `RunProbe` consumes indices1–6 and proves the child's actual token. | No remaining flag/arity collision found. Not used as the mandatory gate. |
| Required registration | `LocalAccountMessageSinkTests.cs:18` registers exactly one actual-process gate through `AddWindows(..., WindowsCaptureProcessTests.Run)`. Optional `WindowsCapturePrincipalTests.RunHarness` is not registered. | Stronger canonical gate retained; not replaced by the smaller manual helper. |
| Failure/skip propagation | `test-filesystem.ps1` passes `--require-os`, preserves native exit code across `Pop-Location`, and restores caller directory. `TestRunner` catches a Windows harness failure as failure and rejects required-platform skips. Canonical harness requires identified GitHub Windows CI and administrator setup token. | Missing prerequisites remain a failing/blocked gate, not a pass or an intentional Windows skip. Local elevation alone is insufficient for the canonical CI-only harness. |
| Canonical process proof | Existing canonical source verifies distinct real accounts, private config/nonce/SID/PID/creation-time reports, nonprivileged child/control access, native denied read/write/create/delete/DACL/owner operations, temporary/pending/final capture states, restart, injected faults and owned cleanup. Config-probe dispatch now reaches that implementation. | No protection removed by the resolution. No execution or protection guarantee inferred. |
| Metadata negative case | The local replacement of fabricated sibling RID with `BuiltinGuestsSid` remains a configured-policy mismatch check; it is not represented as an actual foreign OS principal test. Actual foreign-principal evidence remains the separately registered canonical process gate. | Does not substitute SID strings for the actual-process gate. |
| Palette early return | Remote branch retained named dialog, focused combobox, expanded state, attached listbox, dialog axe collection, screenshot, Escape, hidden dialog, restored Home URL/heading and focus outside the closed dialog. Assertions remain inside `observe`'s existing catch; failure becomes `BLOCKED_RUNTIME`, and group completeness subsequently fails on blocked/unscreened rows. | No lost Palette assertions or new swallowed-success path found. Axe violations are recorded findings, not asserted zero; this is unchanged from the remote baseline. |
| Main probes | Non-Palette routes retain visible-main detection, blocked disposition, H1/overflow/name/error checks and screenshot completeness. H1 lookup is now bounded to5s; missing H1 remains a finding. | Compatible except R24-M01 below. No claim of exhaustive visual acceptance. |
| Worker artifacts | Optional worker ID accepts only1–32 lowercase letters/hyphens; empty default preserves existing artifact root. JSON and screenshot references share the worker directory; project/persona naming remains. | Scoped path split introduces no traversal or credential disclosure found in this delta. Reusing a worker ID is not independently isolated storage; scheduling ownership still belongs to the caller. |

## Finding R24-M01 — Dashboard loading diagnostic searches truncated text

- Type: TEST_INFRA; Severity **Low**; Priority **P3**; Confidence **High**; Status **OPEN**, source-confirmed, runtime **NOT_RUN**.
- Source: `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts:193,204` in the fingerprint above.
- Before remote merge resolution: the loading diagnostic used `(await page.locator('main').innerText()).includes('Đang tải các widget')` over the full main text. The merged implementation reuses `shellText`, already truncated by `.slice(0, 1200)`.
- Static reproduction: a main landmark whose first1,200 characters are other content and whose later widget region contains `Đang tải các widget` no longer emits `POSSIBLE_UNSETTLED_DASHBOARD`. This is a deterministic data-flow deduction, not an executed browser case. Actual current Dashboard text position has not been measured.
- Expected: preserving the remote diagnostic's text coverage while bounding only text retained for reporting/placeholder classification. No new product behavior is required.
- Impact: a route can still be `AUTO_PROBED` without this diagnostic when a loading region occurs beyond the prefix. This affects evidence sensitivity, not the Windows security gate or the retained Palette assertions. Existing scanner limitations already mean `AUTO_PROBED` is not business acceptance.
- Recommended change for integration owner: retain full main text in a local variable for the loading predicate, derive a1,200-character prefix separately for short heuristic classification; avoid persisting full private page text. No source change made by reviewer.
- Closure criteria: source shows full-main loading detection, bounded/redacted persisted diagnostics, and all current blocked/screenshot/Palette checks preserved. When separately authorized, the root QA owner should exercise a loading region beyond that prefix and verify the finding is recorded. No test/fixture was created here.

## Harness documentation qualification

The Oct21 manual-helper handoff is historical and explicitly names its old baseline. Its description that an elevated Windows terminal is enough for `test-filesystem.ps1` no longer describes the newly selected canonical harness: canonical `Run` additionally requires GitHub CI identity variables. Do not interpret that handoff or its old execution counts as current-source acceptance. A small integration-owner documentation clarification distinguishing the canonical CI-only entry from the optional direct manual helper would reduce operator confusion. This is a documentation follow-up, not an observed weakening of the executable gate.

## Method, limitations and handoff

Read mandatory AGENTS, engineering skill, implementation profile, Technical Lead README/core and code-change-review; docs README, delivery README/current scope, goals README/system goals/evidence template; `.ai/routing.json`, `.ai/verification.md`; QA contract-testing/test-strategy; security-code-review; ground-truth completion README, completion-claim-policy and evidence-grounded-verification. Read the scoped files and directly related canonical/manual harness, runner, Windows handoff and Windows worker contract. Compared working source against both parent commits using read-only Git diff.

Read-only commands: `Get-Content`, scoped `rg`, `Get-FileHash -Algorithm SHA256`, and `git status`, `git rev-parse HEAD MERGE_HEAD`, `git branch --show-current`, `git diff <parent> -- <scoped paths>`. Git initially rejected sandbox ownership; a command-scoped `-c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora` permitted read-only inspection without changing global/repository configuration. A missing guessed release-document filename was corrected to the register-linked `06-release-verification.md`; a PowerShell wildcard passed to rg was corrected by explicit paths. No application command or test was run.

Executed by this reviewer: **0 builds, 0 tests, 0 browser sessions, 0 fixtures, 0 OS principals, 0 Git writes, 0 publication actions**. No screenshot/trace was created. Historical Windows CI evidence in `docs/testing/pr4-20261023/windows-worker.md` was read only as context; it is not a run on this merged working tree.

Root owner next steps: address/acknowledge R24-M01, stage resolutions, bind final merge commit to fresh permitted build/CI/runtime evidence and review requirements. No broad R1 or security signoff is granted. Any relevant source change after these hashes requires a review delta; this report does not authorize merging PR4 or bypassing release gates.
