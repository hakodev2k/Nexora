# FX32 catalog and workbench routing — contract before code

Base8eb5aaff; local R1 DEC014, D01–D09 and current code-only AGENTS. Root owns
App navigation/catalog; Kepler separately owns URL/header helper/authority files
and0092. No tests/fixtures/runtime/providers or dependency changes.

Sources: FX32-S01/S02 UX catalog/workbench, P06-TBX-001/003/004/007/008,
FX-32-BR-001 and shared UX-05/15; goals NXG-FX32-G01/G03, NXG-SYS-03/08/13/14.
Existing exact toolbox.catalog.read and tool run actions remain authority.

Use `/developer/tools/:toolCode` for installed safe code identifiers. Finite known
installed codes may be login return targets; unknown codes show an unavailable
workbench, never silently run a different tool or navigate externally. Route
contains only a public tool code, never input/output/options/tokens. App remounts
private workbench on owner/profile/module/tool context change. Direct login and
expired-session return must preserve the selected tool via existing safe origin
rules. Existing module-root entry remains compatible with the local workbench.

Catalog read uses real SQL-authorized API; expose real returned metadata only.
Filter tool name/purpose/category in memory, deterministic name order, clear
filters/no-result distinct from loading/error/unavailable. Current catalog fits
under25 entries; no pagination/history/favorite persistence is invented.

Tool selection/menu/back uses the existing draft/flight guard. A confirmed switch
clears both drafts/options/output before native navigation; defer callback until
that render releases guards to avoid recursive discard prompts. Recoverable Run
error retains authorized drafts; known denial clears them; stale async work cannot
repopulate another route/context. UI is not grant authority. No new API/SQL for
navigation. Rollback: code revert, no user-data migration or destructive cleanup.

Acceptance remains owner-run: direct/reloaded tool URL, unknown and denied code,
login/back/forward, no input in URL/storage, both-draft Stay/Discard/cancel/revoke,
search/category/clear/mobile/keyboard. Build/typecheck only in this code-only run;
independent focused source review before publication, runtime/CI separate.
