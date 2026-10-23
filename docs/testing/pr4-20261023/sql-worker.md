# PR4 SQL release coverage — 2026-10-23

Scope: implemented features at starting source `fbafbaa482b094f26f067f1697d1f9d610e5367c`. New source: `tests/Nexora.IntegrationTests/SqlReleaseCoverageTests.cs`. Twelve new SQL/HTTP cases; all business resources are created/read/mutated through the real HTTP endpoints. SQL is restricted to disposable fixture authority grants, row counts, and audit counts. No production code change, live provider, or production data.

## Case mapping

| Cases | Source AC / decision | HTTP action / assertion |
| --- | --- | --- |
| 3 deleted-create replay routes: Organization/projects, Documents/documents, Organization/documents wrapper | D08=A, `pr4-decision-contracts-20261019/05-sharing-and-tag-replay.md`; FX24/20 namespace/current authority | Create→delete→same original UUID/body twice returns original 201 with exactly opaque id/committed/detailAvailable ACK, no ETag, no resource resurrection or extra successful audit. Changed body remains IdempotencyConflict; explicitly revoked original create action and revoked current module both deny even though source row is gone. Owner+namespace+name count is zero, including any erroneous replacement ID. |
| 2 same-key concurrent creates: Organization/projects, Documents/documents | NXG-SYS-05 idempotency; D08 scope preservation | Two concurrently submitted HTTP commands with same UUID/body resolve same ID, one persisted Tag (also counted by owner+namespace+name) and one successful audit. Submission overlap does not guarantee a particular SQL lock schedule. |
| 2 owner/revision deletes: Organization/projects, Documents/documents | NXG-SYS-02 owner; NXG-SYS-03 revision | Foreign owner deletion is 404. Owner rename then deletion using stale ETag is 412; exact current GET projection survives unchanged. |
| 1 history authority matrix across progress/metadata/state/target histories | FX16 history continuation contracts; NXG-SYS-02/08; `goals.goal.history` | Actual Admin with Unset is denied; explicit Allow reads real history collections; explicit Deny blocks; restoring history Allow but revoking a required reader blocks again. Goal reader applies to metadata/state; target reader applies to progress/target. |
| 2 archive/unarchive authority cases | `goals-archive-contract-20261018.md`; FX16-BR004; SYS02/03/05 | Actual Admin Unset and Deny cannot change source. Allow archives Draft/restores stored Draft; revoke after commit blocks original receipt replay; current source and successful audit count remain unchanged. |
| 2 target Update/Remove preview/commit cases | `goals-target-management-contract-20261018.md`; SYS02/03/05/08 | Actual Admin Unset and Deny cannot preview; Allow previews without mutation. Revoke after preview blocks commit without source change. Restored Allow commits same preview; Update applies new definition, Remove excludes target, Draft remains Draft. Explicit Deny after commit blocks receipt replay without new write/audit. |

The existing `SqlTagPaginationTests.Completed_Tag_mutations_replay_without_a_second_write_and_reject_changed_payload` already covers generic direct-service postdelete replay. Added HTTP dedicated Documents/wrapper coverage tests the missing boundary rather than replacing that oracle. Existing five-action manifest eligibility unit coverage does not establish runtime SQL authorization; the new Admin matrix addresses that gap.

## Verification

- Static whitespace check of the new test source: passed (`git diff --check`).
- Local integration compile/SQL execution is infrastructure-blocked: parent restore returned NU1301 because the proxy returned HTTP 403 for Microsoft.AspNetCore.Mvc.Testing metadata. No compile or test result is inferred from this restore failure; locked dependencies remain unchanged.
- Actual compilation and real SQL execution: pending parent-run CI validation. This document does **not** certify these tests as passed from source inspection.
- Parent owns central SDK restore/build and SQL CI execution to avoid competing build output. Preserve actual command/run URL and failures in this report or the consolidated report when available.

## Remaining coverage

This bounded addition is not complete feature coverage. It does not establish every Goal prior archive state, legacy missing prior, target preview expiry/tamper/cross-owner/stale token, target history rollback/pagination, all SQL provider/content/version/import/stored-reader branches, or every implemented feature's logic/UI/E2E matrix. Those remain separate evidence gaps unless other suites supply executed evidence. D01–D09 semantics remain unchanged.
