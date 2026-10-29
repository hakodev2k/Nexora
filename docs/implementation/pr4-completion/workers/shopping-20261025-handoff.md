# FX31 manual records author handoff — 2026-10-25

Owner `/root/shopping_completion`; root owns integration, independent review, SQL
runtime, final CI and connector delivery. Initial source `75b6d9f4f0ac247680e22cc07bf26a4890071ef2`
on `impl/m01-s00-scaffold`. Current explicit user authorizes local implementation,
synthetic SQL/E2E and UIUX; DEC-014/D01–D09 provider/security restrictions remain.
Exact sources/actions/AC, rules loaded and rollback: [contract](shopping-20261025-contract.md).

## Implemented bounded workflows

- Seller read/create/update, Name/public inert URL; no personal contact/notes added
  across the unresolved P05-SEL-001 versus lower feature/DB field discrepancy.
- Order read/create/update for Draft manual records, stable line IDs, positive
  quantities, exact decimal amounts, explicit date/currency, adjustments and total.
  BigInteger scaled arithmetic detects overflow/precision loss before decimal can
  silently round; no tracked price or FX dependency. Seller attachment requires
  current same-owner readable source; retained historical label survives seller rename.
  Duplicate order reference warns with409 until explicit confirmation; no auto-unique rule.
- Warranty read/create/update, current same-owner readable purchased-line source;
  Fixed requires valid dates/end>=start, Lifetime/Unknown have no fake dates.
  Referenced lines cannot be removed through Draft editing; SQL FK and transactional
  check preserve the source. Provider/terms/contact are SELF metadata, no claims sent.
- All writes require opaque UUID key, strong rowversion on update, current live SQL
  actor/module/action authority, same-owner lock, receipt/audit/domain/registry transaction.
  Receipts store only version/owner/action/kind/id, never private record bodies.
  Replay and final commit recheck dynamic Seller/Order source authority.
- Native responsive cards/detail/forms, native line editor and source search/page
  chooser, exact-retry frozen command after408/5xx/network/invalid ACK, stale revision
  compare/reload/reapply, dirty close guard, explicit unknown-save discard warning,
  private-data clear on401/403, safe return from unavailable deep link. No external links.

The nine source actions are implemented subsets, **not complete FX31**. Merge,
order transition graph/return/history/Trash/purge, comparison, evidence/Files,
Asset handoff, Finance references, global providers and expiry notifications remain
explicit work/gates. There is no provider or Finance write. Root integrates native
routes/DI and exact installed/grantable actions; author did not edit shared root files.

## Files authored

| Layer | Paths |
|---|---|
| Application | `src/Nexora.Application/Shopping/ShoppingRecordContracts.cs` |
| SQL service | `src/Nexora.Infrastructure/Shopping/SqlShoppingRecordsService.cs` |
| API | `src/Nexora.Api/Features/Shopping/ShoppingRecordsEndpoints.cs` |
| Migration | `database/migrations/20261025_0094_shopping_manual_records.sql` |
| UI/client | `web/Nexora.Web/src/ShoppingRecordsScreen.tsx`, `shoppingRecordsApi.ts`, `shoppingRecords.css` |
| Tests | `tests/Nexora.UnitTests/ShoppingRecordRulesTests.cs`, `tests/Nexora.IntegrationTests/SqlShoppingRecordTests.cs`, `web/Nexora.Web/src/ShoppingRecordsScreen.test.tsx`, `shoppingRecordsApi.test.ts` |
| Contract/evidence | This handoff and `shopping-20261025-contract.md` |

## Actual checks and failures preserved

| Executor / command | Actual result / limits |
|---|---|
| Author: normal Release API build | Dependency compilation reached successfully; copy to live-host locked Release DLLs retried. Not recorded as a successful build; no process stopped. |
| Author: `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore --configuration Release -p:OutputPath=D:/Projects/ASP_NET_Core_Developer_2026/Nexora/.tmp/shopping-build/` | PASS,0warnings/0errors,7.53s, isolated output. |
| Author: `npx tsc -b` | PASS before added test file; later root identified unsupported Testing Library exact option in3new test queries and corrected to anchored `/^Lưu$/` without changing assertions. Root final build is separate evidence. |
| Author: `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore --configuration Release -p:OutputPath=.../.tmp/shopping-tests/ --filter FullyQualifiedName~ShoppingRecordRulesTests` | First run25pass/1fail: one newly authored negative example was mathematically representable. Corrected example to a true high-significance precision-loss case; no product arithmetic weakened. Final26pass/0fail/0skip,91ms. |
| Author: `npm run test:unit -- src/ShoppingRecordsScreen.test.tsx src/shoppingRecordsApi.test.ts --reporter=dot` | PASS17cases/2files,31s, component/mocked API only. No later shared JUnit run by author after root coordination. |
| Root report:6new real SQL/API facts | Initial5pass/1fail: test requested invalid systemEnabled=false/registrationEnabled=true policy combination. Author corrected only setup tofalse/false; same read/replay/private-projection assertions remain. Root rerun pending at author handoff; do not call this6/6PASS until root evidence exists. |
| Root report: broader regression | Root reported193unitPASS,296SQLPASS and250frontendPASS/1existing skip before final Shopping SQL correction. These are coordinator observations, not independently executed by this author, and must be bound to final root source/logs. |
| Author: owned-file `git diff --check` | PASS; no whitespace error. No commit/push performed by author. |

Six authored real SQL/API cases cover Seller owner isolation/stale/digest replay,
exact Order totals/historical seller label, cross-owner Seller/line references,
duplicate-reference confirmation, Warranty coverage/pinned-line mutation atomicity,
and module-disabled query/replay privacy. They enable/restore existing FX31 via
actual Admin preview/commit API, not fabricated grants or production data.

No browser/E2E, full responsive/visual, timing/performance or complete FX31 acceptance
was executed by this worker. Root's runtime and final independent review remain gates.
Rollback is code revert/disable new surfaces while preserving additive populated data;
no destructive down migration or permission promotion is introduced.

## Stable author source fingerprints

| File | SHA256 |
|---|---|
| ShoppingRecordContracts.cs | `287AAF543693300ED2419E7314748A74A9FF51F95ACB71B52580BFD74D88A504` |
| SqlShoppingRecordsService.cs | `5029596B1614107334C223F74DAEDFF2558FD08B1F5F73EEDD925074A9D0559B` |
| ShoppingRecordsEndpoints.cs | `C718EBACAF4FB9F629D1F3324CA19A720C15550D81C0AA963490E56E66A85A86` |
| 0094 migration | `6A734699E5D356B6B95F1383BABDA2110C52B3E1CDE5C5B7DB0203A53A46853E` |
| shoppingRecordsApi.ts | `C9C840E621F87148AAE48DA9C70E328F6AE4E42394772CBE758C9F948C34FDB8` |
| ShoppingRecordsScreen.tsx | `4389CC86BA881E0035DFDEBAAC3D10DC98F7D20E4AD4A9BCE3B2F8C3E4A832DD` |
| shoppingRecords.css | `57283FCF1BE33E98DCC3191449A4B1C56B7A7CDBB2FAB313452E2F86FB804315` |

Fingerprint reflects Windows working bytes; root Git LF normalization and later
relevant edits require corresponding updated evidence. Implemented/tested subset
is not VERIFIED RELEASE READY or a complete Shopping module.
