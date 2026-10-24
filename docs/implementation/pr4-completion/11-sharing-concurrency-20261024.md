# ShareLink concurrency regression — 2026-10-24

Status: CODE_IMPLEMENTED / independent source review complete; final real SQL qualification required.

## Actual CI evidence

Branch7c5b9e3fe0deead50e18102c3af5456bb5e46193 was tested by
[CI40066173203](https://github.com/hakodev2k/Nexora/actions/runs/40066173203)
using mergea1298316b3c33a9ca01e99c6b07392c984074c90. GitHub commit metadata
confirms both have the identical tree d23a8ead359ac1106bb39497204c4d4dbe079a92.

| Layer | Actual result from CI job/log |
|---|---|
| Backend unit,120789791579 |131PASS/0FAIL/0SKIP |
| Frontend,120789791492 |201PASS/0FAIL/1existingSKIP;25files |
| Linux filesystem,120789791283 |32PASS/0FAIL/2Windows-onlySKIP |
| Windows actual principals,120789791496 |31PASS/0FAIL/3Linux-onlySKIP; required gate SUCCESS |
| Browser,120789791539 | Job SUCCESS; sanitized artifact12548954525 downloaded through connector, testcase counts not parsed here |
| SQL/API,120789791591 |289PASS/1FAIL/0SKIP;290total |

These counts are separate layers and overlapping workflows, not a whole-feature
coverage percentage. Agent baseline40066173197 also succeeded. No local functional
tests were started; this continuation inspected the existing push-triggered CI.

## SQL-SHARE-01 — controlled source deletion makes List return503

Type BUG/PERFORMANCE; severity Medium; priority P1 (regression gate); confidence
High for observed response, cause remains source-derived without a deadlock graph.

Existing case:
`SqlSharingRepairTests.Source_delete_and_sharing_source_first_reads_finish_without_a_link_source_deadlock(operation: "List")`.
On the exact tree above it expected200 with an empty item collection and observed
503 in550ms. Its unchanged schedule takes a Project XLOCK, starts a real List API
request, waits for SQL blocking evidence, updates the source to Deleted (trigger
invalidates its link), commits, and then awaits List. The failure is genuine
integration evidence; no assertion or fixture was weakened to turn it green.

The candidate query still started at ShareLink then probed source eligibility.
Even READ COMMITTED transient link locks can be held while waiting for the held
source. The deleting transaction needs the same links for invalidation. This
source-derived cycle explains the503; the retained public log does not establish
SQL1205 or contain a captured deadlock graph, so the exact SQL victim cause is not
claimed as directly measured.

## Repair and invariants

Three source files now enumerate the module-owned eligible Project/Document
relation first, then INNER LOOP JOIN ShareLink. Both advisory candidate and
anchor statements specify FORCE ORDER/LOOP JOIN. No NOLOCK, READPAST, hidden-row
threshold, second connection, unbounded application scan or weakened transaction
proof is introduced. Current owner/action/source policy still precedes selection;
the existing bounded source-before-definitive-link phase and public25+1 paging,
D07 revoked/null-token metadata and anchor revalidation remain.

SQL semantics reference: [Microsoft join hints](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-join?view=sql-server-ver17)
describes join strategy/order enforcement. [Query hints](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-query?view=sql-server-ver17)
documents that FORCE ORDER alone does not prevent role reversal; hence explicit
LOOP is retained. These documents support the intended query constraints, not
proof of an actual runtime plan or global absence of deadlocks.

The intended source-before-link access path covers the advisory phase that the
previous repair left optimizer-selected. Query hints do not establish all global
Documents ancestor/cohort lock ordering, an absolute I/O budget or a latency SLA.
Real SQL plan/locking acceptance remains necessary. No schema, grants, test
assertion, read projection or feature flag changed.

Build: `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
--no-restore --nologo -m:1 -nr:false`, exit0,0warnings/errors. This does not parse
SQL query text or prove the concurrency repair. See evidence/oct24-sharing.
Independent source review is in review-sharing-concurrency-20261024.md; no runtime
signoff is inferred from that review. The final pushed source needs its own CI.

Reviewer Mencius, actual new agent01a16c64-1b81-7bb0-b448-57170e0b6aec,
completed this bounded independent source pass with no concrete source blocker.
Only its report was written; no tests/runtime/fixtures were executed by reviewer.
The three source hashes remained frozen. Together with Mendel this makes two new
Oct24 review agents; Oct23's two new agents and resumed reviewer are separate
recorded assignments. No reset child is silently counted as successful execution.

Closure: unchanged controlled deletion case and complete290case SQL suite pass on
the new source; source/denied/D07/paging/cursor regressions retained, with no hidden
row influence. Broader concurrency/query-plan schedules remain separate gates.
Rollback is a source-only revert preserving persisted links/receipts/audit; such
a revert reintroduces this known regression and is not a release recommendation.
