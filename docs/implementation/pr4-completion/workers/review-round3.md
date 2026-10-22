# Independent source review — continuation round3

Reviewer: `/root/review_round3`, independent of root/backend and frontend/knowledge/productivity authors. Baseline remote HEAD `94fac585ee4f24edf0743f92e9b8dd396c2abbd0`. Local changes below were re-read after reconstruction; no lost earlier artifact/count is reused.

## Scope and authorization

User master prompt and continuation authorize implementation, isolated synthetic QA, parallel workers and Draft-branch pushes. D01–D09=A and DEC-20260909-014 retained. Read AGENTS, nexora-engineering skill/profile/routing, Technical Lead core rules, verification/completion rules, current delivery/goals/PO register and relevant authorization/React/security review rules. No source fixes authored by this reviewer; only this report.

## Backend source review disposition

No remaining blocking finding within the five-file reviewed delta. Exact five Goals SELF eligibility keys trace to current catalog16 and migrations0077–0079. This does not create Allow rows, enable modules, authorize source access or change SQL current session/explicit Admin grant/Deny checks. Unknown target linking and SYSTEM dispatch remain denied.

Migration expected inventory was independently compared: **86 oracle names ==86 production manifest names ==86 disk files**, same ordered list. This static check does not prove migration upgrade/rollback.

D07 test uses actual ShareLinkResponse `token` field, inactive/null token owner diagnostics; independent revoked detail/viewer404 and Active-filter exclusion remain. Markdown201 assertion preserves literal body exactly and separates inert renderer proof from persistence. IR-R3 caught an initial compile error (ReadResourceAsync returns tuple, not JsonElement); root replaced it with existing AssertStringPropertyAsync helper, re-read after fix.

Linux capture harness prepends current assembly path only for dotnet host; apphost untouched. Process is not disposed before cleanup handler uses HasExited. Windows principal gate remains unchanged/pending.

Parent reports current backend unit54pass0fail0skip (evidence/unit.log and unit/unit.trx); this reviewer did not execute that command independently. SQL/API tests not run by reviewer; no SQL/Windows acceptance.

## Knowledge source review disposition

Bounded helper preserves original chunks, CRLF/final newline, limits100k aggregatecharacters/400lines per side/160k LCS cells, and uses an explicitly labelled full replacement fallback. Renderer uses React text nodes, noHTML injection, active media, external navigation or automatic restore; full original snapshots remain expandable. Selector changes clear stale comparison. Import mode change invalidates epoch/file/preview and mismatched-source provenance; callbacks preserve working draft body. No remaining scoped blocker found.

Independent actual command: `npx vitest run src/documentVersionDiff.test.ts src/DocumentVersionDiff.test.tsx --maxWorkers=1 --reporter=default` in web/Nexora.Web. **15passed,0failed,0skipped,2files**, exit0,2.95s. Log: scratch evidence/review-round3-diff.log. Covers source fidelity/bounds and inert renderer/component behavior. No browser/real SQL/fullFX20 acceptance claim. Remaining History/Import tests are author/root runs.

## Review findings awaiting final reread

- IR-R3 backend tuple/JSON compile misuse: RESOLVED, source re-read.
- Finance in-flight controls initially appeared unlocked, but shared outer disabledfieldset already locks native descendants. RETRACTED as defect; explicit nested disabled is redundant safety clarity only.
- Time validation must set visible modal error and not rely on a returned object from native onSubmit. Current source setError path observed; targeted test author-owned.
- Time/Focus exact-action revocation should clear dependent private modal state immediately after current capability response, before unrelated allowed reads finish. RESOLVED by immediate current-epoch capability cleanup; final source re-read below.

## Freshness

Any later edit to a hashed file invalidates its scoped review until re-read. Tests do not certify full R1, all733actions, all pages/popups, provider activation, migration execution or release acceptance. PR remains Draft.

| Reviewed file | SHA256 |
|---|---|
| `src/Nexora.Domain/Access/ActionGrantPolicy.cs` | `c182a4ccf4ed1b5ab1657f34495f1246cb285698eea84a133aaecb538d3cd7fa` |
| `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs` | `5d78d9ef2811fb514060b0fbbc77fb3ba196cf00a72ce4d507f43a2c16b5d781` |
| `tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs` | `1249056883162965d586f46c6b596db3edbeb9e522f53403413580905d51b918` |
| `tests/Nexora.IntegrationTests/SqlSharingRepairTests.cs` | `066dff93cb0e1b27fe54dd21e7344093f48498bb4e8d8747358d772f5d772ffa` |
| `tests/Nexora.IntegrationTests/SqlApiIntegrationTests.cs` | `af6306338b16e723ab5606bd675ae9433307ec7931504715cfb5b85772a67894` |
| `web/Nexora.Web/src/DocumentHistory.tsx` | `e32ec111b68ca1d02a826b0c99f1caee0c706385649cebe7188f159cc0a84fdd` |
| `web/Nexora.Web/src/DocumentImportPanel.tsx` | `1a84b709e5150baa0e7c29b58fb1493a065383706136a2050fd711e5aff7a1f9` |
| `web/Nexora.Web/src/DocumentVersionDiff.tsx` | `040ab50e07f9b34052d57967b5bca061a1be8594bb7eaa7232f356e447d08e42` |
| `web/Nexora.Web/src/documentVersionDiff.ts` | `5186a5bef81458812408e77c9de81ef267853275404bacf02b5b347bc3f96d7f` |

## Frontend final scoped source review

Re-read App.tsx, DashboardLayoutEditor, ResourceFormDialog and styles.css after author freeze. Distinct Dashboard sibling keys and semantic context dependencies avoid duplicate identity/reset loop; auth callback stays current via ref. Invalid layout rejects undefined and settles to unavailable/error. Finance keeps a single dialog heading and form-associated footer, with native fieldset busy protection and uncertainty locks preserved. Native account details retains keyboard/escape focus. Reflow changes target actual shrinkable children instead of hiding root overflow. No remaining blocking finding in reviewed scope; screenshots are still required for visual acceptance. Parent/author reports focused80pass0fail1existing skip/81total and buildpass, not independently run here.

| Frontend reviewed file | SHA256 |
|---|---|
| `web/Nexora.Web/src/App.tsx` | `b77019833b12ece2dc04c8de2a5384ac740faa7408dde758410b39e52295eb2e` |
| `web/Nexora.Web/src/DashboardLayoutEditor.tsx` | `e0180c0c48de8f37f54026e4494c6f9883178d6cc3bac43419a46f32a871c975` |
| `web/Nexora.Web/src/ResourceFormDialog.tsx` | `fbd6d5e265d8ed43d0e0cb9ed6594505b81ea7e06fa349791a7df5ffae3b8d36` |
| `web/Nexora.Web/src/styles.css` | `43d80da2ffb48d70a71311d600168f79de9ff21d4af88b48d30014e7089dfe56` |

## Final remediation reread

IR-R4: New account popup used undefined --surface token without fallback, allowing transparent light-theme panel. Author added `var(--surface, #fff)`, retaining explicit dark override. Corrected CSS source re-read; no blocker remains within frontend delta. Actual browser4-pass claim is author evidence, not an independently executed reviewer browser run.

IR-R5: Time/Focus capability-loss cleanup originally waited for unrelated allowed reads to settle. Author now applies current-epoch exact action/read revocation immediately after capabilities resolve, before awaiting unrelated resources. Time history/purge request stamps invalidate older completions; dependent editors/modals close under their exact action; Focus source/private dependent panels clear when read/cancel/preference/record-time rights disappear. Native Time validation sets accessible modal error. Busy controls inherit ResourceFormDialog native disabledfieldset. Non-overlap polling schedules next request only after pending poll settles. Latest production source re-read, no remaining blocking finding in scoped delta. Author's final regression run is still separate evidence.

| Productivity reviewed file | SHA256 |
|---|---|
| `web/Nexora.Web/src/FocusScreen.tsx` | `d9d1108dd92e59069f0070abecb5be80444c158ddfba7fa0bda18ce029844438` |
| `web/Nexora.Web/src/TimeTrackingScreen.tsx` | `ddabd490ef9494c850c024fa8133490dc3fbac28c4baca320771c0937e548cb9` |
| `web/Nexora.Web/src/ProductivityRecovery.test.tsx` | `a7353e5a25c4db6ba6a0567547275766d4d839379f1a611ff0e67cbd4433aa38` |

## Final four-line Dashboard CSS delta

Independently re-read the final four scoped `[aria-label="Bố cục Dashboard"]` rules: shrinkable auto-fit card columns, grid labels with gap, full-width shrinkable selects, and wrapping action groups. They affect layout only and do not change authorization, data access, request generation or security behavior. No blocking finding in this final delta. CSS hash above updated to the final bytes. Focus/Time/test hashes also match the author's final frozen values.

Root reports final integrated component suite184passed/1existing skipped, buildpass, and controlled browser widths320/768/1440pass after this CSS adjustment. These are parent/author evidence, not additional reviewer-executed tests. This remains bounded independent source review; full R1, real SQL/browser E2E, Windows principals and Product Owner acceptance remain pending.
