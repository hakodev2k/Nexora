# Root integration and recovered delivery — 2026-10-24

Base `14c76bcb6960e4ce53369dce388a8480b1c0299a`, branch
`impl/m01-s00-scaffold`. Accountable author: root Technical Lead. Code implemented;
functional verification and acceptance pending. Latest user-supplied AGENTS is
code-only: no new tests, fixtures, application runtime, SQL or browser execution.
DEC014 and D01–D09=A apply. PR4 remains Draft.

## Delivered integration

| Scope | Actual code and invariants | Remaining gate |
|---|---|---|
| FX24 Collections | Program DI/routes; seven exact policy actions; migration0090; `/collections` and UUID detail route; sidebar under FX24 | SQL/API/UI and independent source review |
| Collection sources | Module-owned Project/Document/Bookmark/Snippet participants; current source read and owner/lifecycle before title projection; invisible IDs/counts omitted; visible reorder preserves hidden slots; delete container never deletes sources | Actual revoke/Trash/ancestor/race schedules |
| Collection recovery | Serialized immutable body/key/revision/target retained across ambiguous replies and subsequent denials; validated opaque ACK; denial clears private display; explicit stop warns it cannot undo server commit; revision reload checks current capabilities and generations | Owner lost-response/double-click/browser acceptance |
| Collection migration | Preserve existing permission statuses/grants; exact type collation; contract markers prevent silently adopting unrelated tables; additive owner/composite FK/indexes | SQL replay/crash-gap and rollback rehearsal |
| FX40 Skill merge | Native Skills UI; exact merge action/manifest0088; API/SQL/preview/history/replay integrated | Separate worker contract/report, independent review and runtime |
| FX40 Plan lifecycle | Manifest0089; archive/unarchive preview and exact prior-state recovery; typed available Goal/Task/Course source opening | Readiness stays false, actions Gated; Plan Trash/cohort contract incomplete |
| Native Courses | `/learning/courses` and UUID detail; actual metadata/progress/lifecycle/milestones; Learning tab/Plan callback; finite same-origin login return path; owner-context remount | Direct link/back/login/permission/lifecycle browser acceptance |
| Course privacy/recovery | Capabilities before reads; abort/generation checks; denied/unavailable clears private pane/editor; current-read proof before post-ACK refresh; copied original command/body/parent/child/key retained on uncertainty; ACK validation; safe explicit retry/abandon | Independent review and real lost-response/revoke/conflict cases |
| FX32 diff/color | Catalog/action/manifest0091; browser helpers/options; empty server payload current-authority seam; bounded inert output; abort/epoch/cancel and privacy cleanup for both drafts | Final integration review and functional/performance QA |

Anscombe's Collection implementation was interrupted before a final handoff. Root
recovered the shared files and completed wiring, migration safety and UI recovery.
Einstein's initial Course assignment delivered no patch before interruption; root
authored that recovery. Later independent assignments must be counted separately
from actual review reports. No grants, providers, new dependencies or lockfiles.

## Compile evidence, not functional evidence

Four independent source-review assignments completed. Root corrected CR-01,
COL-R01, R24-PT-01 and central R24-AUTH-01. The last defect placed installed
actions in historical inventory only; effective eligibility now contains exact
seven Collections, Skill merge and15shipped Toolbox keys. This does not seed
Allow, promote inactive permission status or bypass current Deny/source/module
checks. Focused source re-reviews closed these corrections at recorded hashes;
runtime acceptance remains NOT RUN. Final changed-source digest:
`3cb465a78ed8d9f001a7b4653e27371e36ba08dd8cfd135f28391ed109acacc8`.

Final relevant-source commands, exit0:

- `dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore -v minimal`
  — zero warnings/errors; no assertions executed.
- `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --no-restore -v minimal`
  — zero warnings/errors; includes all backend application layers.
- `npm run build --prefix web/Nexora.Web` — TypeScript/Vite succeeded,378 modules;
  >500kB advisory retained (main approximately1.1MB minified).

Curated logs/source hashes: `../evidence/oct24-r1/`. No runtime/performance/SQL/
responsive/a11y/E2E pass inferred. Existing manifest assertion gains only exact
filenames0088–0091; no new test or relaxed assertion/contract.

## Rollback / next ownership

Revert new endpoint/DI/policy/navigation/tool wiring and UI commands, retaining
new tables, receipts, evidence/history, permissions and audit. Never automatically
drop data or unarchive merge tombstones. Plan remains gated during rollback.
No local migration executed. Root owns integration/publication; owner owns runtime QA.

Ordinary work remains: Templates, stored GitHub Discovery, Shopping Orders/Sellers/
Warranty, Career Resume, Learning Certification/Plan Trash/progress and remaining
Toolbox families. These are implementation backlog, not approval blockers. Vault
secrets, advanced Finance/Task extensions, sensitive nonSELF allowlists, live
providers and production remain specifically gated. R1 remains incomplete.
