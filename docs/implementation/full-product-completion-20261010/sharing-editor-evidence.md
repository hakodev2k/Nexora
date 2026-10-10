# FX04 — Sharing settings editor and update recovery

Requirements main8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3; parent a0de198b4b5c3021c537ff93afbdf2667206afa2. Evidence binds final source in this commit. Authority: explicit PO implementation request, DEC014; sharing.link.read/update, FX04-S01/S02, FX04 BR001/002/005 and NXG-FX04-G01/G02/G03.

Production functionality: registered owner GET /api/v1/sharing/links/{id} returns currently authorized metadata/ETag and no token. Existing PATCH update is connected to actual management-row editor for PublicLink/AuthenticatedLink/RestrictedUsers, verified UUID allowlist, explicit UTC expiry/default7days/no-expiry. Source reference immutable; no user enumeration. Fresh current metadata before editing, dirty-cancel confirmation and unload/navigation protection, explicit412 comparison/reapply with a fresh version check, current authority denial clears editor/private creation result.

Unknown or malformed successful ACK freezes exact body/ETag/idempotency key for deliberate retry. Canonical8-byte ETag and fixed resource are checked. Exact committed update replay now returns authorized current metadata, rather than historical snapshot, without repeating SQL effects/audit; current account/action/read/source/policy/epoch checks still run first. Static validation remains before receipt; future-expiry check applies only to fresh mutation so a committed expired request can recover without extending/reactivating its link. React.StrictMode and async continuations are generation-fenced.

Production: SharingServiceContracts.cs, SqlSharingService.cs, SharingEndpoints.cs, ShareLinkEditor.tsx, App.tsx and api.ts. No migration or token-storage change.

Final verification:
- API compilation through Release realSQL test build PASS; frontend build/typecheck PASS (existing bundle size warning).
- Focused sharing SQL/API23 PASS,0FAIL/SKIP, including owner item isolation/no-token; exact replay after later edit/current metadata; no duplicate audit; mismatch key/payload409; absent current update authority deny; committed-expiry replay stays expired, fresh past expiry422/public404.
- Full realSQL/API152 PASS,0FAIL/SKIP; fresh isolated GUID databases; rawTRX localignored.
- Backend unit47 PASS,0FAIL/SKIP.
- Frontend119 PASS,1existingSKIP,0FAIL; editor5focused cases incl exact uncertain retry, repeated-version conflict, authority denial, missingACK and awaited StrictMode lateGET.
- Actual browser/API/SQL sharing journey5 PASS,0FAIL/SKIP/NotRun at1920/1366/768/390/320: create/copy/reload, edit dirty-cancel/noSQLmutation, save audience, concurrent update412/compare/reapply and retained sharing projection/policy/nonrevival scenarios. Initial run1FAIL/4NotRun due select/options nested-label lookup; fixed production explicitlabel htmlFor/id and reran unchanged browser assertions.
- Independent baseline_review identified missing StrictMode generation fence and future-expiry-before-replay defect; both fixed/re-reviewed, no remaining source blocker. Runtime tests executed by root, not reviewer.
- git diff --check PASS. Retained47migration replay preserves core counts; no reseed. Full browser regression beyond affected5journeys and this newcommit CI remain pending/NotRun.

Remaining FX04: required25-row filtered owner-list, exact pre-create disclosure preview and creation lost-ACK outcome workflow. Creation token remains hash-only inSQL; update recovery does not reconstruct or rotate it. Whole FX04/Release1 completion is not claimed.
