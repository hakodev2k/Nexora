# Template library author handoff — 2026-10-25

Owner `/root/templates_completion`; coordinator/root owns integration, independent acceptance and GitHub. Source base `75b6d9f4f0ac247680e22cc07bf26a4890071ef2`; current worktree includes other coordinated agents' changes. User expressly authorizes remaining local implementation, synthetic SQL/E2E and UIUX. No provider, production, secret-bearing Vault, grant seeding or merge authorization inferred.

## Delivered bounded vertical

Read [exact contract](templates-20261025-contract.md) before implementation: P03-ORG-004/005, FX24-S04/S05, BR002/003, AC001/002; NXG-FX24-G02/G03 and SYS02/03/05/08/13/14. Exact installed actions are organization.template.read/create/update/archive/unarchive/trash/restore/purge/instantiate, subject to root-reviewed central installation.

Implemented owner-scoped SQL Template library with typed Bookmark seed provider only; native browse/search/paging, preview/editor, immutable unknown-write recovery, conflict comparison, private-denial clearing, native Archive/Trash/restore/purge and read-only normal-form draft handoff. Archive hides an otherwise available template; unlike FX40's explicit read-only Archive rule, it does not block Template edits/use (P03-ORG-005). Trash preserves exact previous Active/Archived state and deletion batch; restore recovers it. There is no live binding or target-table mutation. URLs remain inert, optional until target Save; scheme/userinfo/length validation and strict typed JSON reject owner/source/share/editor fields.

Root integration seams: `ITemplateService` → `SqlTemplateService(SqlConnectionFactory,string,TimeProvider)`; `MapTemplateEndpoints()`; migration0093; installed action manifest; `TemplatesScreen({onAuthLost,onUseBookmarkTemplate})`. Callback receives only {title,url,description} and opens normal Bookmark create. Root reports these seams and the callback are now wired; this worker did not edit shared App/Program/policy/manifest/styles.

## Review correction T24-R01

Separate Shopping reviewer found SQL CHECK three-valued logic permitted Trash with NULL PreTrashStatus. Accepted Medium finding: migration0093 now explicitly requires PreTrashStatus IS NOT NULL in the Trash branch. Added owner-scoped real SQL negative UPDATE test expecting constraint error547 and unchanged Active record. Original lifecycle SQL test continues to verify restore of Archived state. No application endpoint accepted the invalid fields; DB integrity still must reject them. Final SQL execution of the added negative case remains coordinator evidence until actually run.

## Actual verification

| Command / boundary | Actual result |
|---|---|
| Initial Release API build and Release focused xUnit | BLOCKED/failed build: live API held Release DLLs; MSB3027/MSB3021 after retries. No process stopped and no test asserted PASS. |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~BookmarkTemplateRulesTests` | 11PASS/0fail/0skip;215ms. Pure validation/JSON boundary only. |
| Initial `npm run test:unit -- src/TemplatesScreen.test.tsx --reporter=dot` |11PASS,7.71s; historical pre-Archive additional case. |
| Same focused native screen after Archive scenario |12PASS,64.33s under shared machine load. |
| `npm run test:unit -- src/TemplatesScreen.test.tsx src/templateApi.test.ts --reporter=dot` |14PASS/0fail,2files,32.19s; includes above12, not additive. Mocked API component/client boundary, not E2E. |
| Integration-project Debug build after T24-R01 |PASS,0warnings/0errors,7.08s. Build is not real SQL test execution. |
| Initial scoped whole-project TypeScript check |PASS before other agents' files arrived. Later check found unsupported by-role exact options in Certification/Shopping tests; notified owners without editing their files. Root subsequently reports whole frontend250PASS+1skip/buildPASS. |
| Real SQL/API/browser by this worker |NOT RUN. Root separately reports original3Template lifecycle/authority cases PASS, wholeSQL296PASS; new fourth constraint case requires fresh coordinator result. |

Meaningful cases cover optional seed draft versus target-required fields, unsafe schemes/URL userinfo, field boundaries, unknown identity/share/editor JSON, same frozen body/key after408/5xx, current denial, dirty discard cancellation, exact revision/provider handoff, stale ACK, archived access and explicit conflict comparison. SQL cases bind owner isolation, ETag, digest/replay/opaque ACK, exact lifecycle/restore, target module disable, no live mutation of a Bookmark created through the normal target service, and CHECK UNKNOWN rejection.

## Remaining scope and release limit

Only Bookmark provider is installed. Task, Documents, Snippet/other seed providers, capture-from-source, system templates, global Trash/Search/Collections participation and complete instantiate acceptance remain ordinary contract/evidence work; Task/Document requirements are not declared N/A or removed from R1. Full SQL/API/browser/mobile/keyboard/failure/race acceptance and independent migration/security review remain required. Root owns current-source execution/commit linkage. No whole Templates/FX24/R1 Release Ready claim.

Rollback: disable new surface/revert code, preserve additive table/user records until an explicitly reviewed data-preserving rollback. Do not DROP tables or rewrite existing grants/journals.

## Final authored source fingerprints (worktree bytes)

| File | SHA256 |
|---|---|
| `src/Nexora.Application/Organization/TemplateServiceContracts.cs` | `04CD28DA83904248156FD67548408C7595F33065808C5CDEFDE5C163F9AB4032` |
| `src/Nexora.Infrastructure/Organization/SqlTemplateService.cs` | `B09BDFAB06A05055AD295286EF93553759F2330D74B0DF08002DC49753B6F586` |
| `src/Nexora.Api/Features/Organization/TemplateEndpoints.cs` | `B2C8D2875A2D5BC63D3E7F82188975B22F080F3E672C69CCDE13B28BF8356B0F` |
| `database/migrations/20261025_0093_organization_templates.sql` | `1524BE3CAC2355D8E452C84BED8845CFCC6FF5207C3213A39AF03C9C28205B37` |
| `web/Nexora.Web/src/templateApi.ts` | `854A20CC40C346F269FF4EC1D980E6A7137B2B19523B8FD5C5205D78A48AE6A4` |
| `web/Nexora.Web/src/TemplatesScreen.tsx` | `49C6742AA6308FC42B7BD2A9003408F4D859134C12354FD65FFB85085E7A526E` |
| `web/Nexora.Web/src/templates.css` | `8AF32E94F2C6833888BAE457A3A81C8ADBDC240054BFDFAF5DA15929D85A1FED` |
| `tests/Nexora.UnitTests/BookmarkTemplateRulesTests.cs` | `9D21BA991F713FEA28B5CA3CFD0A107D0A8B8EB51F45DD25578310502602C365` |
| `tests/Nexora.IntegrationTests/SqlTemplateTests.cs` | `1E51A1A5125DDE5E17EB4B63B206EE2504ACB9E268D88A0231F16C3CCCE88C66` |
| `web/Nexora.Web/src/TemplatesScreen.test.tsx` | `4FD5C35604BB1E7D695C63FB1C4BCF8EC9C2C66CA89698DBF2B6CF4F8E07F6DF` |
| `web/Nexora.Web/src/templateApi.test.ts` | `90FCE83CFA14F3C65B6EC1A954F04578D0A968008076B66153918F65C23C65C5` |

No product changes after these hashes were recorded in this handoff. Root updates them if review requires a later correction. No commit/push performed by this worker.
