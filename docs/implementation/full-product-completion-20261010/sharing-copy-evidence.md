# FX04 — Copy newly created share URL

Requirements main8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3; implementation parent aed84dda3d3651aa511665049bc410c9d87797db. Evidence binds to final source in the commit containing this record.

Scope: sharing.link.copy_created (LOCAL/SELF), prerequisite sharing.link.create and existing owner read/source authority; FX04-S01, FX04 BR002/005, NXG-FX04-G01/G02/G03. Current explicit PO implementation authorization and DEC014. No sensitive projection extension, external provider execution or token recovery introduced.

Previously creation UI exposed token/open-link only. Now CreatedShareLink displays the complete current-origin URL and performs a current SQL-backed capability query immediately before clipboard write. GET /api/v1/sharing/links/{id}/copy-capability returns only allowed:true. Rechecks live actor, owner, create AND read separately, hard dependencies, sharing policy/epoch, source read/share/state, current projectionv1, link expiry/revoke. Existing Document link copy permits Published/Archived and rejects Draft. Query neither accepts nor returns the token; copying stays local from authorized creation-result memory.

Creation result clears on reload, matching revoke, auth/source/module denial and screen unmount. Management-list records strip token. Parent flight/generation fencing blocks overlapping actions and late response revival; child fencing prevents clipboard write after result removal. Clipboard denial reports safe failure and preserves readonly manual selection, without rendering browser diagnostics or writing browser storage.

Production paths: src/Nexora.Application/Sharing/SharingServiceContracts.cs; src/Nexora.Infrastructure/Sharing/SqlSharingService.cs; src/Nexora.Api/Features/Sharing/SharingEndpoints.cs; web/Nexora.Web/src/CreatedShareLink.tsx; web/Nexora.Web/src/App.tsx. No migration needed.

Final executed evidence:
- API Release build/test compilation PASS,0warnings/errors; Web build/typecheck PASS (existing bundle size warning).
- Focused sharing real SQL/API21 PASS,0FAIL/SKIP. Includes Admin absent/Deny prerequisite, cross-owner, token-free response, revoked link, Draft/Archived/version and source-delete lock race.
- Full SQL/API regression150 PASS,0FAIL/SKIP,GUID-isolated databases; raw TRX local ignored.
- Backend unit47 PASS,0FAIL/SKIP.
- Full frontend114 PASS,1existingSKIP,0FAIL. Copy component6cases: authority, clipboard failure,401/403/404 clear and unmount fence.
- Real browser/API/SQL sharing journey5 PASS,0FAIL/SKIP/NotRun on1920/1366/768/390/320 viewports. Creates via actual form, copies via real clipboard, validates current-origin URL, reload clears result; existing public Task projection/live-content, policy impact/cancel and permanent nonrevival checks retained.
- Retained47migration replay preserved core counts; no data reseeding, migrations or checksums edited.
- Independent baseline_review found AND-prerequisite, late-response/token-list retention and Draft/projection gaps; all fixed and re-reviewed with no remaining scoped blocker. Reviewer did not execute runtime tests.
- Initial verification caught a test actor setup using User instead of Admin for AdminPermission semantics and a TypeScript stale-handler return; corrected actor/status expectation to existing documented409 ModuleUnavailable contract and production returnfalse. No valid gate removed.
- git diff --check PASS. Full browser regression beyond affected5journeys and new-commit CI pending/NotRun, not claimed pass.

Remaining FX04: owner-list25paging/filter/sort, update management UI, exact pre-create disclosure preview and creation lost-ACK outcome workflow remain incomplete. This slice does not establish complete FX04 or Release1 acceptance. Tokens, clipboard contents, raw browser artifacts, local credentials and private runtime files are excluded from commit.
