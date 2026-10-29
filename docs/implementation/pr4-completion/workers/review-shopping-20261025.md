# Independent Shopping manual-records review — 2026-10-25

Reviewer: /root/learning_completion, separate from /root/shopping_completion author. Root owns central integration, final source and release decision. No Shopping product, test, migration or configuration file was edited by this reviewer.

Scope: the new owner-private Seller/Order/Warranty manual subset, not all FX31. Read authoritative FX31 feature/action/UX/DB sources, current D01–D09 and [author contract](shopping-20261025-contract.md); matching backend/security/database/owner-isolation/frontend/verification rules apply. Trace P05-SEL-001/P05-ORD-001/P05-WAR-001, FX-31-BR-001/003/005/006, source AC001/003 and common owner/concurrency/retry rules.

## Outcome

**No new blocking source finding in the final reviewed bounded delta.** This is not full FX31/security/DB/production acceptance. Earlier unsupported Testing Library ByRole exact properties were observed during a global typecheck; root corrected those three Shopping test typings. Current source uses exact string/anchored-name queries and tests pass. No assertion was removed to mask behavior.

Reviewed paths: typed DTO/interface/validator, complete SQL service,0094 migration, endpoints, client acknowledgement boundary, main screen/source picker/conflict/recovery/dirty guard, feature stylesheet, unit/component tests and real SQL test source. Shared ResourceFormDialog/navigation guard behavior was also read to confirm busy/unknown outcomes block exits and expose explicit discard/reconciliation.

| Boundary | Source observations |
|---|---|
| Owner/current authority | Server resolves IdentityPrincipal; service checks SqlCurrentActor and exact SELF action; all business predicates/registry and source joins include OwnerId. PersonalSpace owner lock serializes mutations. Update composes corresponding read; Warranty writes compose Order read. No owner field from request or ambient role bypass. |
| Source references | Current same-owner readable Seller required when linking Orders. Warranty line requires same-owner current Order source. Source checks repeated for writes/replay. Foreign line IDs cannot enter a same-owner FK. Existing Order seller label is retained across later Seller renames; no live-price/provider mutation. |
| Persistence/concurrency | Serializable domain/registry/receipt/audit transaction; strong canonical quoted rowversion on updates; owner-wide write lock prevents check/write races among these writers. Removing an Order line pinned by Warranty rejects409; retained line IDs verified against that Order. No external calls inside transaction. |
| Replay/privacy | Durable request digest and actor-qualified namespace; receipt stores opaque id/owner/kind/action only. Current action/source rechecked before returning original ACK; collision409. Client validates committed/detail flags, UUID and exact expected update id; malformed/mismatched ACK remains uncertain. No private DTO replay. |
| Money/date integrity | Explicit registered uppercase currency; invariant decimal strings; BigInteger fixed-scale arithmetic rejects fractional precision loss and overflow before casting. Nonnegative shipping/tax/discount and computed total; quantity>0; no float/FX/refund/payment/Finance mutation. ISO date-only values; Fixed coverage bounds, distinct Lifetime/Unknown null dates. |
| SQL defenses | Additive schema/contract-marker checks, same-owner composite FKs and appropriate nonnull fields; no ON DELETE CASCADE, grant seeding or module/status promotion. Fixed coverage CHECK explicitly rejects missing dates; Seller label pairing uses IS NULL/IS NOT NULL predicates rather than nullable equality. |
| UI/recovery | Native labelled fields; escaped/inert strings; no external-open link or hidden provider fetch. Sequence guards prevent stale private reads;401/403 clears payload/draft. Unknown408/5xx freezes original serialized body/key, exits blocked until explicit reconciliation/discard.412 preserves draft and requires fresh compare/reapply. Post-ACK navigation waits for editor/busy clear. |
| Boundaries retained | Orders are Draft-only editable; no pretend state transitions, returns/refunds, evidence, merge, Trash/purge, Asset/Finance handoff or sensitive sharing. Seller private contact fields excluded according to higher-priority public-reference scope. |

The UI displays the server-computed total after Save; no independent live client total-preview or measured performance/WCAG/visual acceptance was established by this review. Offset paging under concurrent edits, large datasets, locks/faults and future lifecycle/source graphs still need broader acceptance.

## Independently executed checks

| Command | Actual result |
|---|---|
| dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-build --no-restore --filter FullyQualifiedName~ShoppingRecordRulesTests |26PASS/0fail/0skip;97ms; Windows .NET10; existing final built source |
| npm run test:unit -- src/ShoppingRecordsScreen.test.tsx --reporter=dot (web/Nexora.Web) |10PASS/0fail/0skip;1file;5.37s; Vitest/jsdom mocked API |

These are overlapping subsets of root regression, not36additional distinct cases. This reviewer did not start SQL/browser fixtures or execute SQL tests. Root reported full SQL296PASS, but reviewer explicitly requested reconciliation of the six new SqlShoppingRecordTests with the final suite denominator rather than assuming they ran. Test source covers opaque Seller replay/collision, actual exact Order totals/seller snapshot, foreign references, duplicate confirmation, Warranty bounds/pinned lines, module disable/current replay. Root owns real execution logs and source identity.

## Hash-bound reviewed source

| Path | SHA256 |
|---|---|
| `src/Nexora.Application/Shopping/ShoppingRecordContracts.cs` | `287AAF543693300ED2419E7314748A74A9FF51F95ACB71B52580BFD74D88A504` |
| `src/Nexora.Infrastructure/Shopping/SqlShoppingRecordsService.cs` | `5029596B1614107334C223F74DAEDFF2558FD08B1F5F73EEDD925074A9D0559B` |
| `src/Nexora.Api/Features/Shopping/ShoppingRecordsEndpoints.cs` | `C718EBACAF4FB9F629D1F3324CA19A720C15550D81C0AA963490E56E66A85A86` |
| `web/Nexora.Web/src/ShoppingRecordsScreen.tsx` | `4389CC86BA881E0035DFDEBAAC3D10DC98F7D20E4AD4A9BCE3B2F8C3E4A832DD` |
| `web/Nexora.Web/src/shoppingRecordsApi.ts` | `C9C840E621F87148AAE48DA9C70E328F6AE4E42394772CBE758C9F948C34FDB8` |
| `web/Nexora.Web/src/shoppingRecords.css` | `57283FCF1BE33E98DCC3191449A4B1C56B7A7CDBB2FAB313452E2F86FB804315` |
| `tests/Nexora.UnitTests/ShoppingRecordRulesTests.cs` | `441615EF9B949D542BBE5DEF2A4249246792C5B7FBE8CB4FB2856116FC19D378` |
| `tests/Nexora.IntegrationTests/SqlShoppingRecordTests.cs` | `2DC52570948D8BC362203CE40C82874744E28FD15F8CCEDB62D24F41F800657D` |
| `web/Nexora.Web/src/ShoppingRecordsScreen.test.tsx` | `24C6168A9930EDFB1E91A0AA49ACD72770C04C9FAB90AC428F76D1BA8DDA93C7` |
| `database/migrations/20261025_0094_shopping_manual_records.sql` | `6A734699E5D356B6B95F1383BABDA2110C52B3E1CDE5C5B7DB0203A53A46853E` |

Subsequent relevant edits invalidate this bounded review. No full-module acceptance, security risk acceptance or Release Ready statement is granted.
