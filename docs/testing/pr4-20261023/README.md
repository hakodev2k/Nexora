# PR4 implemented-feature test continuation — 2026-10-23

Accountable owner: root. User explicitly requested continued logic/E2E testing
of the features already implemented, including isolated synthetic test data.
This current instruction supersedes the historical code-only restriction for this
QA scope. AGENTS.md is preserved. No production, paid service, live provider,
merge, relaxed authorization or release acceptance is authorized.

Baseline branch impl/m01-s00-scaffold, HEADfbafbaa482b094f26f067f1697d1f9d610e5367c.
All1748repository files were retrieved and their Git blob/tree hashes checked.
Mandatory nexora-engineering/profile/routing/TechnicalLead/currentPO and QA
contracts were read. D01–D09 remain applicable. Later edits invalidate affected
verification and receive actual reruns or explicit pending status.

## Actual baseline evidence

CI39792570922 checks associated with branchfbafbaa, actual PRmerge checkout
2497194dc6672b76f65d0a83eb98fff4cf832200:
- Backend/frontend/SQLAPI/Linux PASS in their bounded suites.
- Browser artifact12433005954 downloaded and parsed:28cases,21passed,
  2failures(identitydesktop/mobile),5errors(resetmobile,auth/navigationboth).
  Failure payloads intentionally sanitized; no raw secret-bearing logs published.
- Windows34discovered/30passed/0failed/4skipped; required actual A/B/C test is
  hardcoded skipped, correctly making the required Windows gate fail.

## Workstreams and acceptance

| Owner | Work / files | Acceptance and remaining limits |
|---|---|---|
| root | PublicSharedResource main landmark;3component regressions; UIUXworkflow trigger coverage; central restore/build/test/push |Loading/unavailable token states remain accessible; stale projection cannot revive; no permission/data expansion |
| qa_browser_oct23 | Identity menu route oracles, explicit CommandPalette modal audit, safe stage diagnostics, real SQL/API browser workflows | Existing assertions preserved; UIUX9shards kept distinct from45s journeys, no lost test coverage |
| qa_sql_oct23 |12new HTTP/realSQL Tag and Goals current-authority/replay/concurrency cases | RealSQL CI execution required; compilation/source review are not pass |
| qa_windows_oct23 | Full WP-WIN real3principal/process harness if contract-complete | Genuine tokens/positive controls/native denials/restart/fault/cleanup; no partialprobe can satisfy requiredgate |
| qa_review_oct23 | Independent test/security review and expected-result provenance | Read source/hashes, detect vacuous or weakened assertions; no self-review acceptance |
| qa_coverage_oct23 | PerFX implemented-surface→existing/newtest inventory | Explicit missing states and unmapped AC; no action-count coverage percentage |

Five actual child AI workers were started in this testing run. Their final
reports record actual actions/results/blocked work individually. Root is separate.

## First findings and verification

Identity test looked for PERSONAL SPACE before opening native account details;
Logout likewise lives inside the account disclosure. Those are stale UI entry
assumptions, not evidence of authentication failure. Main E2E config accidentally
discovered the dedicated UIUXscanner under45s timeout; navigation lasted45.06s.
It belongs in the existing9area workflow with240s config, which must actually
trigger on application/API/schema changes. No assertion may be removed to hide it.

PublicSharedResourceScreen used a generic div, so invalid publicshare route had
no main landmark. Root replaced that wrapper with native main only, preserving
uniform unavailable content, source resolution and token semantics. New current
component tests:3passed/0failed/0skipped (2.91s total;364ms tests). Reviewer found
an initially vacuous stale-response test; request-start, delayed-response and act
synchronization were added before recording this final pass.

## Evidence and publication

Use actual command logs/JUnit, safe enum-only phase diagnostics, CI run/job IDs,
tested branch/merge SHA and changed-source fingerprints. Keep original failures
as failures; overlapping focused suites are not additive coverage. Synthetic real
SQL/API/browser and controlled HTTP component evidence remain distinct.
Every source popup/page not exercised stays explicitly unverified. AllR1 scope
and completion remains separate from this testing continuation.
