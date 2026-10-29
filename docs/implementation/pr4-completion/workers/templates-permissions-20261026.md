# Template → Bookmark create-only Admin browser verification — 2026-10-26

Authorization: root assigned the known create-only runtime gap under DEC014 isolated automated QA; scope only existing FX24 Bookmark seed provider and ordinary FX21 Bookmark Save. Base a0215f7 plus coordinated source. Contracts: FX24 Template read/create/instantiate and Bookmark create; NXG-SYS-02/03/13/14; UX-05/11. No provider effects, role changes, security bypass or module lifecycle promotion. Test author owns functional/templates-permissions.spec.ts; root independently read its bounded grant/restoration design. Product code unchanged by this test.

Test uses genuine Admin/SuperAdmin logins and finite legitimate preview/commit operations: only already Ready FX21/24 system/user enablement and five exact action dispositions. Admin gets Template read/create/instantiate and Bookmark create, explicit Bookmark read Deny. It creates a Template through normal UI, reloads/reads the real seed, opens preview and normal Bookmark form handoff, checks seed values, observes zero UI library GETs, saves and validates exact owner/title/URL/description in real SQL, reloads without protected list reads, checks denied direct detail. Finally restores and asserts original action effects, user module grants, system/registration flags, module states and user role. Cleanup failures preserve the original scenario error in an AggregateError and must block another shared run.

## Actual runs so far

- Initial desktop1FAIL: test incorrectly expected403 for direct denied Bookmark list, actual409. Failure retained at artifacts/templates-permissions-initial-failure/{functional-junit.xml,functional-results.json}. Finally authority restoration/assertions completed without a cleanup error. No application behavior changed.
- Read exact source: SqlBookmarkService.List/Get use ModuleAvailable(action) and map unavailable action authority to ModuleUnavailable409; helper at line435. Corrected assertions to exact409 **and exact ModuleUnavailable code**, not an arbitrary denial range. Desktop rerun **1PASS**,4.5s overall/3.4s case. Passing artifacts copied to templates-permissions-desktop-pass before another worker's shared reporter run. Tablet/mobile final-source execution remains pending exclusive writer slot.

## Proposed convention versus observed module contract

The architecture document's full context is **design only**, and its table explicitly says **Proposed status/code**. Its403 proposal is not a confirmed module contract requiring a server change. Current Bookmark List/Get use the exact409 ModuleUnavailable source contract for explicit read Deny; Discovery's separate403-vs409 mapping belongs to that module. Bookmarks manual-slice record contains no exact HTTP mapping and is a historical code-only snapshot. Record proposed403 versus observed409 as needing concrete contract confirmation, **not a verified Medium product defect**. The test proves current fail-closed behavior and zero denied UI list reads; it does not establish broader normative API acceptance or release readiness. The reviewer corrected an earlier classification that had omitted the table's explicit proposed authority.

## Source fingerprints
- `web/Nexora.Web/functional/templates-permissions.spec.ts`: `EEEE207FAD082D9F2F76DAD88F02693C90C0CCB68C1D4842F89E13AA382D2E51`
- `src/Nexora.Infrastructure/Bookmarks/SqlBookmarkService.cs`: `CCC4FD8148383E28881B39BB458CD2312E5E5C83D4B1884EAB7F152A075948FF`
- `docs/architecture/05-api-and-frontend-contracts.md`: `32DB8A8CAAF2862B8D43C4C4BE4C1A7B25D07727BBF90DBECC20CECA01A3FBEF`

## Final current implementation verification

After final shared axis restoration and initial shared-card adapters, executed the unchanged genuine-Admin scenario sequentially at desktop1366x768, tablet768x1024 and mobile390x844: **3PASS**,0failure/skip/errors. Overall runs4.0s/3.8s/3.9s (cases3.0s/2.8s/2.9s). Preserved each parsed successful run under `artifacts/templates-permissions-final/{desktop,tablet,mobile}/verified-junit.xml` and verified-results.json before another worker used the functional reporter. Exact authority restoration assertions passed in every final case. No mocked positive API response, storage/auth injection or protected library read was used. The later one-line static filter presentation correction does not change the permission/recovery code; broader final-source regression/CI remains root-owned. This supersedes the pending tablet/mobile note above, without deleting the initial oracle failure.

Observed current409 ModuleUnavailable mapping remains exact and fail-closed; architecture403 is explicitly a proposed convention rather than a confirmed defect. No complete FX24/FX21 or release acceptance follows from this bounded path.
