# Local R1 implementation continuation — 2026-10-24

Status IN_PROGRESS. Starting branch `impl/m01-s00-scaffold`, HEAD
`14c76bcb6960e4ce53369dce388a8480b1c0299a`, clean worktree. User requests full
approved R1 implementation; prior MASTER PROMPT authorizes independent workers,
local code, review and push. Current AGENTS code-only amendment applies: no new
tests/fixtures or local runtime QA. Compile/typecheck/build only. Prior CI evidence
does not certify the new changes. Keep PR4 Draft, no merge or production/providers.

## Accountable plan and ownership

Root Technical Lead owns integration, App/Program/policy/manifest, Course navigation,
review reconciliation, builds and publication. Disjoint implementation workers:

| Worker | Scope | Reserved migration | Exit / rollback |
|---|---|---|---|
| Hegel01a16c85-5000-7663-b84c-e247b7056608 | Skill merge, immutable provenance/evidence and native UI |0088 | Contract/API/SQL/UI; preserve target/source evidence; additive schema, disable new command on rollback |
| Franklin01a16c85-508b-75a3-8ad8-3de2c6bd0d5f | Learning Plan lifecycle with exact prior state and impact preview |0089 | Current authority/revision/receipt/audit, no source mutation; readiness false retained |
| Anscombe01a16c85-5122-71f0-91d2-d88b820c82dc | Collection container and ordered source references |0090 | Owner/source-aware API/DB/UI; delete container never deletes members; no sensitive sharing |
| Kepler01a16c85-51c6-7652-b5b3-b69f7e5c2f01 | Remaining bounded text/color offline tools |0091 | Pure processing/bounded inert output/current authority, no providers/history |

Contracts precede edits. Exact source actions/AC/goals and handoff are recorded in
each worker report under workers/r1-20261024-*. Root schedules separate-author review
before publishing sensitive/cross-module/migration changes. Ordinary missing code
is work, not a blanket approval blocker. No feature/module accepted from presence.

## Root Course/source navigation contract (before implementation)

Authority: D06; P07-LRN-001/P07-CRS-001; FX40-BR001/002/006; FX40-AC002;
FX40-S03/S05 and current UX navigation. Goals NXG-FX40-G01/G02 and
NXG-SYS-02/03/08/13/14. Exact source actions remain learning.course.read and existing
Course/milestone mutation actions. Navigation issues no grant and mutates no source.

Native `/learning/courses` and `/learning/courses/:id` reuse the real Course APIs.
UUID routes, owner-context remount, current capabilities before detail/milestone
read, loading/error/unavailable distinct from empty, same-origin finite navigation
only. Plans open available Goal/Task through existing native resource resolvers
and Course through this native route; unavailable references have no open action.
Server GET remains authoritative for ownership, lifecycle and current source rights.

Detail shows real Course metadata, progress and milestones, with existing current
action/lifecycle mutation gates. Refresh/navigation/filter changes cancel or reject
older reads; authority loss clears private pane/list/drafts. A read-denied or opaque
write response cannot trigger fresh detail GET without a new current read proof.
Dirty navigation retains existing guard/confirmation; no bypass or forced handler.
Existing typed dates and exact decimal-percent conversion remain unchanged.

No new backend route/schema for Course navigation. Existing API DTO/error/ETag,
receipts and lifecycle invariants remain. Rollback is UI routing/API-client code
revert; preserve all user data and server protections. Root does not activate Plan
readiness or resolve its permission gates from earlier CI alone.

Verification: focused compile/typecheck then integrated backend/frontend builds.
Independent source/privacy/navigation review required. Functional API/SQL/browser,
all popup/viewport/accessibility and current-head CI evidence remain separate gates.

## Integrated checkpoint

CODE_IMPLEMENTED subsets: Skill merge, Plan archive/unarchive, Collections,
diff/color, native Course/source navigation and uncertain-write recovery. Four
separate-author reviews found three P2 source defects (CR-01/COL-R01/R24-PT-01)
and a P1 effective-action eligibility defect (R24-AUTH-01);
root corrected them and requested/received focused source re-review. Relevant
final compile commands/logs/source digest are in `evidence/oct24-r1` and the root
handoff. Local functional cases0 under code-only authorization. No module/R1
VERIFIED or ACCEPTED; Plan readiness remains false. Ordinary remaining work and
real approval gates remain separate in the all40 ledger and pending-risk report.
