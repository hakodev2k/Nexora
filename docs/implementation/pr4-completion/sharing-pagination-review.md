# Sharing pagination — bounded code-only handoff

Date: 2026-10-23. Status: **CODE_IMPLEMENTED / VERIFICATION_PENDING**.

## Task brief

- Authorization: latest bounded assignment permits direct edits only to `SqlSharingService.cs`, the Project/Document sharing participants and this report. No tests, fixtures, runtime, commit or push. Other dirty work belongs to the coordinating implementation owner and is preserved.
- Branch: `impl/m01-s00-scaffold`; source HEAD: `fbafbaa482b094f26f067f1697d1f9d610e5367c`. The starting Sharing file already contained the uncommitted IR01 scan-loop repair reviewed in `independent-review.md`; this patch replaces that loop. No independent reviewer has approved this replacement.
- Goal/requirement trace: NXG-SYS-02/03/05/07/08/09, NXG-FX04-G01/G03; FX-04-BR-001/005 and FX-04-AC-001; `sharing.link.read` SELF with current `projects.project.read/share` or `documents.page.read/share`; D07 in `../pr4-decision-contracts-20261019/05-sharing-and-tag-replay.md` governs owner diagnostics, eligible pagination and revoked/null-token metadata. DEC-20260909-014 authorizes local implementation; D05/D09 gates stay in force.
- Applied skill: `.agents/skills/nexora-engineering/SKILL.md`. Read AGENTS, implementation profile, Technical Lead README/core rules, routing, verification, docs/delivery/current-scope/goals entry points, system/module goals and evidence template, current PO decisions, FX04 feature/action/UX and the independent review. Selected rules: architecture ownership/module-boundary/transaction consistency, backend authorization/security, privacy access control, identity/access, database transaction/isolation skill, security code-review skill, owner-boundary README/rules/review skill.
- Plan: move eligibility ahead of SQL TOP, bound application materialization/proofs, release advisory locks before definitive source/link locking, validate the anchor again, compile and hand off to independent review. No migration, public DTO or UI change.

## Changes

| File | Change |
| --- | --- |
| `src/Nexora.Infrastructure/Sharing/SqlSharingService.cs` | Removed the second advisory connection, scan-until-full loop and growing source-authority dictionary. Current type-wide module/read/share/owner policy is evaluated inside the existing Serializable transaction. One candidate query applies those gates and source-owned lifecycle predicates **before TOP(26)**. |
| Same file | Candidate and optional anchor reads use READ COMMITTED table hints on the same connection/transaction. Those are advisory reads, not retained definitive link authority. At most 26 candidates and one anchor are materialized; distinct sources are proved in a single sorted phase before definitive link reads. No fresh source proof is acquired after definitive link locks. |
| Same file | The anchor link is revalidated after source proof using owner, source identity, CreatedAt/Id, epoch and the exact mode/state/resource filters. Definitive candidate rows must match the selected identities and count. A source/link change observed between advisory selection and definitive proof returns the existing generic ResourceUnavailable/404 instead of returning a misleading short/empty success. There is no hidden-row-count error threshold. |
| Same file | Public `hasMore` comes only from the definitive authorized 26th row; the cursor anchors the 25th returned row. No denied source contributes a page slot or cursor. The extracted type-policy helper explicitly rejects unsupported source types. |
| `src/Nexora.Infrastructure/Productivity/SqlProjectSharedSource.cs` | Source-owned SQL eligibility fragment requires same OwnerId/ProjectId and Status != Deleted, matching existing owner-state proof. |
| `src/Nexora.Infrastructure/Documents/SqlDocumentSharedSource.cs` | Source-owned SQL eligibility fragment requires same OwnerId/PageId, Status != Deleted and the existing VisiblePageV1 container/topology visibility contract. Draft remains eligible for owner diagnostics; viewer Published/Archived rules are unchanged. |

Revoked owner diagnostics retain the exact OwnerRevoked exception, current epoch, inactive metadata and null raw token. Viewer resolution, token invalidation and mutation/receipt behavior are unchanged. SQL fragments are static source-owned constants; caller input remains parameterized.

## Bound and isolation limits

- Application-level work no longer grows by hidden-link batches. The candidate query runs once, optional advisory anchor once, definitive anchor once and definitive link collection once. Source proofs are bounded by 27 distinct resources. The existing helper commands and authority dependency queries still have their own costs.
- This is **not** a claim of constant database I/O, an absolute 27-lock ceiling or a request-wide latency SLA. SQL may inspect many physical rows to find 26 eligible rows, and may take range/page/escalated locks. Large owner datasets require execution-plan and blocking measurements; no index or schema change was authorized here.
- Source-before-link means **retained definitive** source/container locks precede retained definitive ShareLink locks. The advisory statement can take transient link/source read locks in optimizer-selected order. READ COMMITTED hints deliberately prevent these advisory data locks from surviving into the later source-proof phase; no NOLOCK/READPAST or independent-connection wait edge is introduced. This distinction requires independent SQL/security review and a controlled concurrency schedule, not an assertion that all physical SQL locks follow a prescribed order.
- Advisory candidate enumeration is READ COMMITTED inside a Serializable transaction; policy and definitive source/link authority use the transaction's normal isolation. This is not a Serializable snapshot of the entire eligible population. Concurrent inserts can enter a later refresh; no cross-request snapshot guarantee is added. Observed removal/filter/source changes fail closed; there is no scan/refill/retry loop. SQL deadlocks/timeouts remain subject to existing error handling.
- VisiblePageV1 is the same source visibility view used by current Documents container checks. Its READ COMMITTED hint applies only during advisory selection. The later source participant retains the normal HOLDLOCK/current topology proof. Review query-plan hint propagation and lock behavior on real SQL before closing IR03.

## Executed checks

```
dotnet build src/Nexora.Infrastructure/Nexora.Infrastructure.csproj --no-restore -p:BuildProjectReferences=false -p:UseSharedCompilation=false --verbosity minimal
```

Exit 0; Debug/net10.0; 0 warnings, 0 errors; reported elapsed 8.94 seconds. This compiles the current shared-tree Infrastructure source against existing referenced assemblies; it is not an integrated solution build or SQL syntax/runtime validation. Main owner handles integrated Learning/API builds.

`git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora diff --check -- <the three source files>`: exit 0, no whitespace errors. Per-command safe.directory was needed for the sandbox principal; global Git configuration was not changed.

No unit/integration/E2E/browser tests, SQL queries/migrations, fixtures, runtime environment or independent agents were executed/created in this bounded task. Build success is not IR01/IR03 runtime acceptance.

Source SHA256 fingerprints (checkout bytes, after the successful build):

| File | SHA256 |
| --- | --- |
| SqlSharingService.cs | F32149E6FAD6F5F49C1159C4348F1B0FAF7DE326AC786C6584076EA61C13FA8B |
| SqlDocumentSharedSource.cs | 27FE77C0B4F1CB34BC473E1F2A1D2F83C1AAA2A9DAA1A043256C7575D0ED79A7 |
| SqlProjectSharedSource.cs | 928E8E74E05CD967EF2DEB53031F1308CD801895A8B3480D314A907CF5A1A58A |

## Required next review / verification (not executed)

Independent DB/security reviewer must assess hints, source-before-definitive-link ordering, topology/cohort writers, type gates, anchor races and non-snapshot semantics. Under a separately authorized runtime task, verify zero versus many denied links; mixed Project/Document eligibility; 25/26/27+ eligible links beyond denied history; all filters; Draft/Archived/Trash/container visibility; owner/module/action revocation; revoked metadata/null token; no duplicate/missing IDs on stable data; anchor revoke/filter/epoch races; source lifecycle interleavings; large-history plans and lock escalation. No new test files were added here.

IR01 pagination disclosure and IR03 application scan growth have source repairs; both remain **review/verification pending**, not closed or accepted. No release signoff. Rollback is source-only: coordinating owner can revert these method/helper changes without deleting or rewriting stored links, receipts, audit or tombstones. Reverting the repair reintroduces the prior pagination/scanning risks and is not a release recommendation. No commit was made.
