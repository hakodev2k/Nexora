# Certification manual records author handoff — 2026-10-25

Owner: /root/learning_completion; coordinator/root handles integration, GitHub, independent review and final runtime evidence. Starting branch impl/m01-s00-scaffold, HEAD75b6d9f4f0ac247680e22cc07bf26a4890071ef2. Current user expressly permits local code/tests/synthetic SQL/E2E/UIUX. D01–D09 unchanged. No code-only restriction is used to avoid the current QA request.

[Pre-code contract](certifications-20261025-contract.md) records current requirements/actions/goals, data/API/UX/security, rollback and acceptance. Source binding: P07-CERF-001, FX40-S04, FX-40-BR-004/006, NXG-FX40-G02/G03, SYS02/03/05/08/11/13/14. Relevant mandatory startup/routing instructions were loaded; no agent configuration changed.

## Actual implementation

- Typed manual Certification metadata (title, issuer, optional issued/expiry dates and inert HTTP(S) verification URL); date-order/range validation. Generic metadata update cannot bypass the separate renewal action.
- SQL owner-scoped records, same-owner registry FKs, rowversion concurrency, append-only immutable safe history on create/update/renew/archive/unarchive. History versions travel as decimal strings, avoiding JavaScript bigint precision loss.
- Current SELF capability/session recheck and owner lock in one serializable transaction with registry/history/audit/durable receipt. Original same-body/key replay returns opaque actor/owner/action-proven ACK, no stale private DTO/ETag; changed payload409, missing428/stale412, current denial403, foreign404.
- Native list/detail/forms with owner-local server-derived expiry, explicit Expired/Archived states,25/page owner/filter/date-bound encrypted cursor. Nullable expiries sort last even against real9999-12-31 dates. History private and25/page.
- Five-minute authenticated lifecycle preview binds actor/owner/resource/operation/revision. Already-committed receipt recovery precedes preview expiry validation so expired previews cannot break original committed ACK recovery.
- UI empty/no-match/loading/error/unavailable states, labeled native forms, date-only dd/MM/yyyy display, archive read-only, current-denial clearing, immutable unknown-write body/key retry, deliberate discard warning, dirty navigation and blocked flight exits. Client rejects malformed/wrong-resource ACK before treating a write as known committed.

Additive0095 creates no demo records, grants or capability-status promotions. Root central DI, endpoint/migration manifest, action eligibility and native routes are separate integration edits, not authored by this worker. No Plan readiness change.

## Exact integration seams

Program: register ICertificationService as SqlCertificationService(SqlConnectionFactory,idempotencySecret,TimeProvider); call MapCertificationEndpoints(). Six installed actions: learning.certification.read/create/update/renew/archive/unarchive. Existing Deny/module/owner/action gates remain authoritative. Route /learning/certifications[/UUID]; CertificationScreen props onAuthLost, optional certificationId/onOpenCertification/onBackToCertifications. Root reported these central seams integrated.

## Actual checks and limits

| Check | Actual result / source |
|---|---|
| dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore | PASS,0warnings/errors,9.85s initial source; subsequent focused tests rebuild latest backend |
| dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore --filter FullyQualifiedName~CertificationContractTests |11PASS/0fail/0skip; latest rerun559ms after nullable-expiry boundary correction |
| npm run test:unit -- src/CertificationScreen.test.tsx src/certificationApi.test.ts --reporter=dot |10PASS/0fail/0skip,2files; latest42.65s under parallel load. Mocked API component/client proof only. |
| dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --no-restore |PASS,0warnings/errors,2.71s; not SQL execution |
| npm run build |Initial full TypeScript/Vite PASS,379modules, retained >500kB bundle advisory; module was not yet App-wired at initial build, so no claim of native route execution |
| Later global tsc |Found own unsupported Testing Library ByRole exact flag and3same flags in Shopping tests. Own flag removed; root corrected Shopping3flags and subsequently reported integrated buildPASS. This failed typing check is retained, not relabeled PASS. |
| Real SQL / final integration |This worker did not launch SQL. Root reported three Certification SQL/API cases PASS on isolated local SQL2019, full SQL296PASS and unit193PASS/frontend250PASS+1skip. Those are coordinator reports requiring root final logs/source attribution, not independently executed by this author. |
| Browser/visual/a11y/concurrency/renewal worker |NOT RUN by this author; root is performing native E2E/UIUX separately. No screenshot or visual acceptance claim here. |

Four real SQL/API scenarios are authored in SqlCertificationTests: retained renewal history+same-key replay+stale ETag; foreign/current-module denial; revision-bound archive preview+archived renewal denial; SQL rejection of Archived with missing prior state. Local fake/in-memory SQL was not substituted.

Independent review C40-R01 identified SQL three-valued CHECK behavior: Archived plus NULL PreArchiveState could evaluate UNKNOWN and be accepted. Corrected0095 to require explicit IS NOT NULL in the Archived branch and added owner-scoped negative SQL assertion expecting error547 and unchanged Active row. No service exploit was observed. Integration-project rebuild after correction passed0warnings/errors,4.83s. The earlier root-reported3SQLPASS is historical pre-correction evidence; final migration/negative assertion requires a fresh isolated database. Root/reviewer notified; no old successful run is substituted.

## Explicit remaining committed scope

Credential-ID encrypted storage/reveal/history, Clean file evidence/pins, Course references, reminder rules/durable expiry/revision invalidation, sensitive share/support, Trash/Restore/Purge and wider FX40 acceptance remain unimplemented in this bounded slice. There are zero reminder effects here, never fake Scheduled/Sent. FX-40-AC-003 (one pending reminder after renew) is **NOT VERIFIED/NOT IMPLEMENTED by this slice**. No full Certification/FX40/R1-complete or Release Ready claim. Root independent security/DB review and final-source SQL/browser evidence still govern acceptance.

Rollback code without deleting new data/history; no autonomous down migration/drop. No production/provider/paid action, no commit/push by this worker.

## Reviewed authored source hashes

| Path | SHA256 |
|---|---|
| `src/Nexora.Application/Learning/CertificationContracts.cs` | `698C550285D947CAC5C38D11537C2E6AD181E7EB7FF28AD0691BEC887CBE8DEF` |
| `src/Nexora.Infrastructure/Learning/SqlCertificationService.cs` | `72FB4B0FA2C08EF9ED5C171DB4EABF82001D68914AD42FC67A0E7AF7C773E732` |
| `src/Nexora.Infrastructure/Learning/CertificationTokenCodec.cs` | `C2EDE257A1CAB2631BB7584A5419C10FE578B5EA54AF5E12B9C133684FDF8811` |
| `src/Nexora.Api/Features/Learning/CertificationEndpoints.cs` | `35117A5C1CEABF7B2160C612264BCFEC2C680EB6B186AD31F334E450AC966092` |
| `database/migrations/20261025_0095_certifications_manual.sql` | `83BBD549EEA6177C2DF1F9E4A0C850005D9F36EFCE5359D0AA346E68DB81842F` |
| `web/Nexora.Web/src/CertificationScreen.tsx` | `26CF38A5CAB0857422410ED3FA04BA94B39F1A3EDBE353D22586F2233CDB7E45` |
| `web/Nexora.Web/src/certificationApi.ts` | `3D40708B210ED86F45BBB7663547FED28F1A99907ED0AF91FBEE0A9D01605E59` |
| `web/Nexora.Web/src/CertificationScreen.test.tsx` | `F5B801F64FCE411D8F412834C6C31594F33268D52C5F8858F4DFE723C53C307C` |
| `web/Nexora.Web/src/certificationApi.test.ts` | `E37CCF8F277D8964833CDFC7F6FFD136A92192558C0762E64828E6A497F3196F` |
| `tests/Nexora.UnitTests/CertificationContractTests.cs` | `1F28397FEF1A7ACF9F95D0B64BCFB91DEB342DD3CFC14B4CD6B0092DFA1965AA` |
| `tests/Nexora.IntegrationTests/SqlCertificationTests.cs` | `C7E6D51990C6FBB84E788A4A33B529939CC4C975CA91C2F7CD4C42F50E5EF4BE` |
