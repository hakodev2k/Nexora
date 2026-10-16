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
| .NET unit suite at final server sources | 49 passed, 0 failed/skipped; final TRX retained locally. Separate executable Linux-required suite:32 passed,0failed,2Windows-only skips. Does not certify SQL behavior. |
| Final API + integration-test project Release build | PASS, 0 warnings/errors, 7.09 seconds after fixing seven xUnit2031 assertion overloads. Tests compiled, SQL cases not executed locally. |
| FunctionalTestOperator Release build | PASS, 0 warnings/errors, 4.83 seconds. No fixture command executed locally. |
| Frontend TypeScript/Vite final product build | PASS, 81 modules; existing JavaScript chunk-size warning remains. |
| Production-build browser smoke | 4 passed: public/authenticated × desktop/mobile (320px mobile); explicit keyboard focus, dirty SPA Back, modal focus restoration, ACK privacy, enabled/disabled navigation, Documents Tag route, form validation, API503, no whole-page horizontal overflow and scoped axe assertions. Real Chromium headless shell; controlled HTTP fixtures, **not SQL/API acceptance**. Final public 2 passed in4.6s; authenticated2 passed in6.0s. |
| Screenshot inspection | All four final screenshots inspected; skip-link overlay absent, public validation and mobile ACK remain legible. These cover selected states, not all screens or visual regression completeness. |
| Full frontend unit suite | Initial run:135 passed/3 failed/1 skipped. Failures identified missing exact capability fixture and dedicated Documents Tags route; corrected without weakening assertions. Final rerun: **141 passed, 0 failed, 1 skipped**, all14 files passed after adding three Notification ACK/denial privacy cases; no test disabled to repair these failures. |
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

## Follow-up on actual checkpoint2f76ed3 CI

Baseline38911747631 passed. Implementation38911747578 frontend-unit, backend-unit and filesystem-linux passed. SQL container readiness succeeded and real SQL/API tests started. Browser target seed succeeded but API readiness failed: source diagnosis found the harness generated a48-byte local-message envelope key while its constructor requires exactly32 decoded bytes. The follow-up generates32 bytes for that encryption key, retaining48-byte signing secrets and all masks/private paths. No product cryptography weakened. Windows job ran30 assertions with0failures but4skips; its required actual A/B/C process-identity case is still a permanent explicit skip, so the gate correctly fails. No Windows acceptance claim.

Notifications additional UI regression verifies single/batch ACK clears private rows/count/selection with no automatic detail read and does not present an unavailable projection as an empty successful Inbox. HTTP409 current-module denial clears cached content.

## Real SQL run and bounded repairs

Run38911747578 at2f76ed3 executed **219 real SQL/API tests:200 passed,19 failed,0skipped**. This supersedes the earlier "no real SQL run" state for CI only; no local SQL run is claimed. Failures were traced rather than skipped:

- Current Admin eligibility omitted31 exact shipped SELF keys (Finance8, Inbox5, Documents library/page/history9, Documents Tag4, organization Tag wrapper4, Search1); current catalog independently confirms AdminGrantable=true and resolved/implemented status. Added an explicit set to both eligibility and allow-grant validation. No actual grant, broad prefix, provider/SYSTEM/source-open/restore-version/assignment or module-policy change. These source changes require final independent review.
- Composite/Tag tests lacked the required live SessionId: fixtures now use their real synthetic session, preserving production current-session checks.
- File restore replay now correctly expects minimal committed ACK when a later Trash cohort exists, plus an explicit current metadata read verifying actual Trash state. Retained-file denial expects the precise DependencyUnavailable contract.
- News upgrade test now targets the exact0046 boundary, rather than assuming0046 remains the final migration forever. Whole-manifest upgrade remains covered by fixture setup.
- Search source fixture no longer asks the Document API to accept executable script markup; inert bold markup preserves the escaping assertion and now checks an actually accepted source.
- Revoked owner ShareLink management was incorrectly filtered out by IsDeleted even though Revoke sets that bit. Only owner-management list/advisory reads now retain OwnerRevoked metadata under unchanged current owner/source/action/epoch gates. Viewer resolution remains unchanged and revoked links stay unusable. Existing real-SQL test checks the exact revoked row and stale/foreign cursors.

Repairs are not certified until the subsequent real SQL run passes. Overall product remains INCOMPLETE; Windows A/B/C real-principal test and independent review remain pending.

## Latest local verification

At29a3f97, .NET unit49passed/0failed/0skipped; Release integration-test project0warnings/0errors. Frontend141passed/0failed/1skipped across14files; skipped test is the existing R2-10 post-M01 DST disambiguation case, not a newly disabled test. Final frontend TypeScript/Vite build passed. Current production-build public smoke2passed in4.8s; expanded authenticated desktop/mobile smoke2passed in9.7s, now additionally verifying Notifications single committed ACK clears private content, does not auto-read and does not claim empty Inbox. These remain controlled HTTP fixture checks, not backend acceptance.

## Real CI at29a3f97

Implementation38913206603: browser job **PASS**,10tests/0failures/0errors/0skips in61.28s. This includes four real SQL-backed registration/verification/reset journeys across desktop/mobile, two controlled bootstrap failures and four controlled UI smokes. SQL/API readiness and owned process cleanup passed; exact sanitized JUnit is retained in evidence/ci-browser-29a3f97-junit.xml. No claim that all ten tests are real backend business acceptance.

SQL/API: **216passed/3failed/0skipped of219**. The16 previously failing cases passed after bounded repairs. Remaining test expectations were traced against installed contracts: retained clean File rename is installed and succeeds (trash retention remains denied); owner-revoked ShareLink metadata remains visible but inactive/token-null while viewer remains404; completed removed-Tag create replay returns original committed ACK without a second write. Tests now assert those exact outcomes and retained privacy/lifecycle checks; no test skipped or production behavior weakened. The next SQL run must confirm all219. Windows actual-principal gate remains failed.
