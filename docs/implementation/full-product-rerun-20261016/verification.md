# Rerun evidence and remaining gates — 2026-10-16

Overall product: **INCOMPLETE**. Implemented changes below are bounded repairs; neither the action inventory nor successful smoke tests prove all business requirements complete. User's updated prompt explicitly authorizes tests and supersedes the older code-only amendment for this run.

## Implemented changes

- Finance: current transactional action/session/owner authority, protected list cursors and exact resource reads, nullable protected usage, original projection digest/committed ACK, exact currency totals, UI exact-revision recovery and immutable uncertain-command retry. Denials including HTTP409 module/dependency unavailability clear private data and drafts.
- Bookmarks/Reading: active Trash source exclusions, original opaque replies stay opaque, safe legacy receipt downgrade and strict quoted ETags. Latest remote Reading position and Search IANA/cursor logic preserved.
- Notifications: remove ambient SuperAdmin arbitrary SYSTEM publication; retain registered internal writers. Current transaction authority, independent read gate, minimal replay proofs and source-unavailable projection replace cached private source snapshots. Browser consumes committed ACK without treating it as a DTO or automatically reading protected detail. Final authority is checked after receipt writes. Full registered source resolver and inbox cursor/filter completion remain missing.
- Documents: receipt stores keyed full authorized projection proof, not body; originally opaque/legacy receipts remain ACK-only. A live Tag rename cannot change a same-key replay DTO while page ETag stays unchanged. Existing Tag snapshots/history and trusted transition actions preserved.
- Shared UI: missing module navigation entries respect runtime gates; skip-to-content and route focus; dirty navigation/unload protection; modal background isolation reference counting fixes outer/inner dialog cleanup order. A real smoke screenshot exposed the offscreen skip-link rendering defect; explicit offscreen positioning and bounding-rectangle assertions fix it.
- CI harness: isolated job-owned disposable loopback SQL, private synthetic API/browser target, ownership-checked cleanup, masked ephemeral credentials and sanitized evidence. No production/provider execution or permission grants added.

## Executed checks

| Check | Result and limits |
| --- | --- |
| .NET unit suite before final Documents digest delta | 49 passed, 0 failed/skipped; TRX retained locally. Does not certify SQL behavior. |
| Final API + integration-test project Release build | PASS, 0 warnings/errors, 7.09 seconds after fixing seven xUnit2031 assertion overloads. Tests compiled, SQL cases not executed locally. |
| FunctionalTestOperator Release build | PASS, 0 warnings/errors, 4.83 seconds. No fixture command executed locally. |
| Frontend TypeScript/Vite final product build | PASS, 81 modules; existing JavaScript chunk-size warning remains. |
| Production-build browser smoke | 4 passed: public/authenticated × desktop/mobile (320px mobile); explicit keyboard focus, dirty SPA Back, modal focus restoration, ACK privacy, enabled/disabled navigation, Documents Tag route, form validation, API503, no whole-page horizontal overflow and scoped axe assertions. Real Chromium headless shell; controlled HTTP fixtures, **not SQL/API acceptance**. Final public 2 passed in4.6s; authenticated2 passed in6.0s. |
| Screenshot inspection | All four final screenshots inspected; skip-link overlay absent, public validation and mobile ACK remain legible. These cover selected states, not all screens or visual regression completeness. |
| Full frontend unit suite | Initial run:135 passed/3 failed/1 skipped. Failures identified missing exact capability fixture and dedicated Documents Tags route; corrected without weakening assertions. Final rerun: **138 passed, 0 failed, 1 skipped**, all13 files passed; no test disabled to repair these failures. |
| Scoped source review | Finance and Bookmarks/Reading production deltas received independent source review at recorded hashes in their reports. Latest Finance409 clearing, shared modal, Notifications and Documents digest changes still require final independent review; unavailable reviewer is not a pass. |

## GitHub checkpoint and CI

First checkpoint `27b6290ea3e8b7da296c2d729d30912c6f6fdfa1` is published on PR4; main remains8782f46. Baseline38910605316 passed. Implementation38910605382: backend-unit and filesystem-linux passed; SQL/API lacked an isolated SQL target; browser lacked an isolated E2E target; frontend failed; Windows ACL evidence failed. The next checkpoint includes the runtime harness and fixture corrections. Do not call CI green without inspecting its actual final jobs.

## Remaining work and truthful disposition

- High source-confirmed authority/projection/replay gaps remain in Goals/Habits/Planner; see independent audit findings. No broad all-handler security claim.
- Finance still needs explicit default currency behavior and owner-IANA date default reconciliation; current UI's VND/browser-date defaults are not accepted completion.
- Documents hierarchy/editor/compare/restore and other current requirements; FX24 Collections/Templates/assignments; full Search providers/facets/saved queries/palette; Dashboard persisted layout; remaining News/Career/Learning/provider capabilities remain in `missing-capabilities.md`.
- Semantic per-action input/output/postcondition/AC tracing is incomplete. CSV declarations, source extraction and action rows are distinct inventories, not a certified requirement denominator. Unread and selected-only sources are labelled in the ledger. No percentage is claimed.
- Real SQL runtime, complete API E2E, Windows ACL evidence and full-product re-audit remain pending. The local environment has no usable SQL Server/Docker. Headless browser fixtures cannot substitute for those gates.
- New security-sensitive changes remain reviewable on the existing draft PR only. No merge, main write, production deployment, real secret/data or real provider action performed.

Rollback is source revert; no applied migrations or retained records changed. Original older worktree is preserved.
