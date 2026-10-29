# Independent source review — Sharing advisory concurrency — 2026-10-24

**Disposition: no blocking source defect identified in the three-file patch. Proceed to regression CI while keeping runtime acceptance pending.** This is a narrow independent source review, not SQL execution, deadlock confirmation, release approval or broad security sign-off. PR #4 must stay Draft; Learning Plan readiness stays false.

## Task brief and reviewed baseline

- Authorization: explicit request for independent review of the frozen Sharing query patch; write only this report. No product-source changes, commit, tests, fixtures, build or runtime execution by this reviewer.
- Accountable implementation owner: requesting root implementer. Reviewer: this independent review execution, separate from the patch author.
- Repository/worktree: `D:/Projects/ASP_NET_Core_Developer_2026/Nexora`, branch `impl/m01-s00-scaffold`.
- Observed HEAD: `7c5b9e3fe0deead50e18102c3af5456bb5e46193`; committed tree `d23a8ead359ac1106bb39497204c4d4dbe079a92`.
- At intake, Git reported exactly the three modified source files below. The review applies to their working-copy bytes, **not just committed HEAD**.
- The requester identifies CI merge `a129831` as the same source tree and CI run `40066173203` as the failing run. That merge object is unavailable locally; tree equivalence and CI results are requester-supplied evidence, not independently verified here.

| Frozen source file | SHA256 of working-copy bytes |
| --- | --- |
| `src/Nexora.Infrastructure/Sharing/SqlSharingService.cs` | `63CB079DE74EF79E25E465117DA9212C805EB0BB93012752C636E4B761083646` |
| `src/Nexora.Infrastructure/Productivity/SqlProjectSharedSource.cs` | `4A247E0F5F0A90C4AE4B689D5B7C66A101367ACDB752FA0E41FF0E9B9DFD8D5F` |
| `src/Nexora.Infrastructure/Documents/SqlDocumentSharedSource.cs` | `B62C835C6691B37091FCA98073D6E11AC31068C4F064B84B66B1F19958453EC4` |

## Authority and acceptance trace

Read startup: `AGENTS.md`, `.agents/skills/nexora-engineering/SKILL.md`, `.ai/profiles/nexora-implementation-agent.md`, `.ai/roles/technical-lead/README.md`, its `rules/core-rules.md`, `.ai/routing.json`, `.ai/verification.md`, documentation/delivery/current-scope READMEs, goals README/system/FX04 goals and task/evidence template. Applied technical-lead code-change-review skill, database routing rules/skills, security routing rules/skills, owner-isolation control/rules/review skill, and backend authorization rules. No agent-configuration change or gate script execution.

Binding product sources: `docs/features/04-read-only-sharing.md`, `docs/action-catalog/modules/04-sharing.md`, current D01–D09 decision register and `pr4-decision-contracts-20261019/05-sharing-and-tag-replay.md`. Older goals prose saying local implementation is unapproved is superseded by the current PO approval; it does not alter this review-only task.

| Goal / contract | Reviewed operation and invariant | Source disposition |
| --- | --- | --- |
| `NXG-SYS-02`, `NXG-SYS-03`, `NXG-SYS-05` | `sharing.link.read` in SELF context: owner scope, current source/module/action authority, source-before-definitive-link transaction proofs | Retained in the inspected path; runtime NOT_RUN here |
| D07=A; `FX-04-BR-001`, `FX-04-BR-005`; `NXG-FX04-G03` | Current-authorized owner may inspect own explicitly revoked inactive metadata; no old token or source-deleted/stale-epoch link revival | Retained in the inspected path; runtime NOT_RUN here |
| `FX-04-AC-001`, `NXG-SYS-07`, `NXG-SYS-08` | Revoked/denied viewer capability remains unavailable; no private fields or denied-row-derived continuation | Query patch does not change resolution or add fields; runtime NOT_RUN here |
| D07 pagination contract | Filter before page limit; signed owner/epoch/filter-bound cursor; stable `CreatedAt DESC, Id DESC` | Retained structurally; runtime NOT_RUN here |

No new product decision, permission grant, sensitive-field allowlist, provider activation or migration is introduced by the patch.

## Failure evidence: observation versus hypothesis

Requester-supplied CI outcome: SQL **289 pass / 1 fail** in run [40066173203](https://github.com/hakodev2k/Nexora/actions/runs/40066173203). The failing case is `Source_delete_and_sharing_source_first_reads_finish_without_a_link_source_deadlock("List")`: expected HTTP 200, actual 503 under controlled Project XLOCK/delete interleaving, approximately 550 ms.

The test source at `SqlSharingRepairTests.cs:473` holds a real Project XLOCK, waits until a sharing request is blocked by that SQL session, then marks the Project deleted in the holding transaction. Its deletion trigger invalidates ShareLink rows before commit. The List result must settle to HTTP 200 with an empty collection, followed by permanent viewer refusal.

**Not captured:** SQL error 1205, victim selection, an Extended Events deadlock graph, or the failing execution plan. The test name is not diagnostic proof. `Program.cs` maps both `SqlException` and `TimeoutException` to 503, so HTTP 503 alone cannot establish a deadlock. The approximately 550 ms observation also does not identify the SQL exception number.

**Source-derived likely cause:** the previous link-led advisory query could read/temporarily lock a ShareLink while probing its blocked Project; the Project holder's invalidation trigger then needs the ShareLink. This is a plausible link→source versus source→link cycle, not a graph-confirmed historical root cause.

The root implementer subsequently reports integration build success with zero warnings/errors. That is supplied compile evidence, not a reviewer-run command and not SQL runtime proof.

## Query and hint review

At `SqlSharingService.cs:196–230`, both anchor and candidate queries now share this shape:

1. Build an identity-only eligible-source relation: owner-scoped nondeleted Projects, plus owner-scoped visible nondeleted Documents, each gated by its transactionally computed type permission.
2. Use that relation on the left of `INNER LOOP JOIN security.ShareLink`, joining **OwnerId + ResourceType + ResourceId**.
3. Apply existing link lifecycle, current SharingEpoch and exact list filters.
4. Put `OPTION(FORCE ORDER, LOOP JOIN)` on the outer executable SELECT in both paths; candidates additionally use `TOP(26)` and stable descending seek ordering.

This addresses the identified query-level inversion: for the intended nested-loop plan, the eligible source is the outer input and ShareLink is probed only after a source row is obtained. With the single Project already X-locked by the controlled test, the advisory reader should wait at that source before probing its associated ShareLink. The deleting transaction can then invalidate links and commit; resumed eligibility should exclude the deleted source. This is the correct source-level direction for the known schedule.

The change covers the cursor-anchor query too; fixing only candidates would leave the same query shape reachable during continuation. Later bounded source proofs and definitive link/anchor checks remain in their existing order.

**Static SQL assessment:** no concrete syntax, alias, column-shape, parameter or hint-placement defect found. Both UNION ALL branches return compatible resource-type/OwnerId/Id columns. Source aliases are scoped correctly in the nested derived tables. Hint placement is outside the CTE definition and after ordering where applicable. All query values remain typed parameters; interpolated fragments are trusted source constants. This is source inspection, not SQL Server parse/compile execution. The hinted plan still needs engine validation, including possible plan-generation failure.

The hints specify join strategy/order; they are not a universal lock-order proof. They do not establish actual index access, view expansion, prefetch/spool behavior, lock granularity or escalation. Source-driven iteration over multiple resources is not equivalent to acquiring every source lock before any advisory link access. Therefore this review does **not** assert freedom from every deadlock or from every multi-source/container interleaving.

## Denied-source pagination, D07 and cursor review

- Both source branches constrain `OwnerId=@Owner`; typed link equality prevents a Project/Document ID collision or foreign link from matching. `OwnerSourceTypeAllowed` remains based on current source read/share rights and source-module availability inside the same Serializable transaction.
- Project eligibility still excludes Deleted. Document eligibility still uses the source-owned `VisiblePageV1` topology/trash predicate. Draft owner diagnostics remain intentionally eligible; viewer Published/Archived rules are not broadened.
- Eligibility is still relationally applied before `TOP(26)`. The patch does not restore the denied-candidate scan loop, hidden-row budget, additional connection or growing authority cache. `hasMore` and the next anchor remain derived from definitive authorized link results.
- The exact D07 exception remains `(IsDeleted=0 OR (RevokedAt IS NOT NULL AND InvalidationReason='OwnerRevoked'))` plus current epoch/source proof. It does not admit SourceDeleted invalidations. Active/Expired/Revoked filters are unchanged.
- List projection is unchanged; `ShareLinkRecord.RawToken` defaults to null. No token reconstruction or viewer-resolution edit occurs in these three files.
- Cursor decoding remains bound to owner, epoch and exact filters. Advisory anchor lookup now uses the same source-first relation and filters as candidates; definitive anchor revalidation still follows source proofs. Denied/deleted/foreign/stale-epoch anchors cannot become valid merely because a link ID exists.
- Stable tie-break, fixed page size, count agreement and concurrent-change fail-closed behavior are retained. This patch introduces no new rule for snapshots across separate page requests.

## Findings and handoff

**Confirmed blocking source findings: 0. Required corrective source patch from this review: none.** Do not roll back to link-led EXISTS probes, remove source filtering, suppress 503, weaken isolation or change the failing assertion merely to obtain green CI.

The remaining gate is concrete verification, owned by the root implementer/SQL CI runner:

| Pending gate | Required evidence / closure criterion |
| --- | --- |
| Known controlled schedule | Rerun the existing five-case `Source_delete_and_sharing_source_first_reads_finish_without_a_link_source_deadlock` theory on the exact pushed patch. List: 200 + empty items; deletion commits, link invalidated, old viewer token 404. Preserve every attempt, not only successes. |
| Plans for both query paths | Capture actual candidate and cursor-anchor plans in disposable SQL, including Project-only, Document-only and mixed eligible-source paths. Confirm source outer input precedes corresponding ShareLink probes. Check for hint-related compilation failures and expensive broad scans. |
| Privacy and compatibility regression | Existing `Denied_sources_cannot_determine_public_pagination_or_validate_a_cursor_anchor` and `Owner_links_page_25_with_confidential_scoped_cursor_and_exact_state_filters`: denied rows do not create continuation; invalid anchors fail closed; 25+ authorized rows page correctly; D07 inactive/null-token diagnostics and revoked viewer 404 remain intact. |
| Remaining SQL regression | Rerun the affected/full SQL gate on the final patch revision; retain current-head command, counts and artifact reference. Earlier 289/1 is not evidence for this dirty patch. |
| If failure persists | Preserve server correlation/error number and blocking/lock evidence; obtain a deadlock graph if a deadlock actually occurs. Patch the demonstrated remaining access-order mechanism; do not infer it solely from response status. |

No speculative architecture replacement or additional product patch is requested. Passing compilation permits proceeding to CI, not closing the concurrency acceptance gate. Keep the existing failure open as **source remediation reviewed / runtime verification pending** until corresponding evidence exists.

## Reviewer execution and integrity

Executed only repository reads/searches, local read-only Git inspection/diff and SHA256 file hashing, plus creation of this report. Initial Git inspection hit ownership protection; subsequent commands used a command-local `-c safe.directory=<this workspace>` override. No global Git configuration was written. The local CI merge object lookup failed and is recorded above rather than assumed verified.

Tests/build/runtime/SQL execution, data creation and provider calls by this reviewer: **0**. Source writes and commits by this reviewer: **0**. This report is the sole authorized deliverable. Source fingerprints must be checked again if the author changes the frozen patch before publication.

## Follow-up — bounded stale-candidate restart — 2026-10-24

**Disposition: no concrete blocking source defect identified in the bounded restart patch. Proceed to regression CI; acceptance remains pending.** This addendum supersedes no historical result above and adds no runtime passing claim.

### Baseline and supplied evidence

- Observed local HEAD: `64c1161c964e1af304a6fe84eecd724de3d87b0d`.
- Only tracked working-copy diff at review: `SqlSharingService.cs`, 22 insertions / 4 deletions. Its frozen SHA256 is `9328807D401FE276A420E95268212AA56AC49EBC7F16F3B356F181B3BCD3623A`.
- Project and Document helper hashes remain exactly those in the initial review. The source-driven query and explicit LOOP/order hints are unchanged.
- Requester-supplied run [40066918835](https://github.com/hakodev2k/Nexora/actions/runs/40066918835), source `64c1161`: **289 pass / 1 fail**; the same controlled List case now returns **404 rather than 503**, while its expected result remains 200 with an empty collection. The reviewer did not retrieve or execute this run.
- The changed outcome is consistent with the existing fail-closed stale-candidate path. It does not independently prove removal of every deadlock schedule or identify a historical SQL exception number.

### Inspected control flow

At `SqlSharingService.cs:88–101`, public `List` captures one UTC cutoff and calls `ListOnce`; it makes exactly one further call only when the first call explicitly reports `candidateChanged`. The second result is returned directly, including failure. There is no recursion, retry loop, sleep, exception retry or synthetic-success branch.

Each `ListOnce` creates its own connection and Serializable transaction. C# using-declaration cleanup completes on method exit before control returns to the wrapper; an unsuccessful attempt disposes its reader/transaction/connection before the second attempt starts. No locks or mutable candidate/DTO collections are carried into that attempt.

Both attempts repeat input normalization/validation, current sharing capability, live-actor checks, module state and epoch reads, signed cursor decoding, transaction-local current rights, type authority, source eligibility, definitive source/link proofs, allowed-user projection and final actor/action checks. The retry does not reuse the first transaction's grant/source proof. The shared cutoff fixes Active/Expired classification across this single logical request; it does not pin authority or create a cross-request snapshot guarantee.

### Retry classification and fail-closed behavior

| Condition | Inspected behavior | Review result |
| --- | --- | --- |
| Non-anchor candidate's source proof fails | `candidateChanged=true`, then Missing; wrapper may perform a full fresh read | Appropriate bounded restart; source denial/deletion is not converted directly to success |
| Failed source is also the cursor anchor's typed source | `candidateChanged=false`, Missing | No retry past a known invalid source anchor, including when several links share that source |
| Definitive candidate typed identity/CreatedAt differs, or definitive count differs | `candidateChanged=true`, Missing | Discards the entire attempt rather than dropping individual candidates or emitting a misleading partial page |
| Advisory anchor invalid, cursor decode fails, or `ListAnchorCurrent` fails | Missing with flag left false | Existing invalid-anchor 404 retained; no invented continuation |
| Validation, actor/module denial, epoch mismatch or final authority denial | Original failure with flag false | Retry is not a general authorization/error bypass |
| Second attempt encounters another stale candidate | Its Missing result is returned; second flag is ignored | At most two attempts; no fake empty result, refill scan or unbounded retry |
| SQL/timeout exception | No new catch in wrapper | Not silently hidden as a successful page |

A failed non-anchor proof may be encountered before a concurrently invalid anchor is examined, because proofs are ordered by typed source. Such an attempt can restart, but the next attempt revalidates the original cursor and anchor; it does not advance beyond them or manufacture validity. This is not a confirmed authorization defect. The no-retry rule applies when anchor invalidity has actually been established.

For the controlled single deletion, the intended source path is coherent: first attempt selects a source/link which becomes unavailable before definitive proof; the non-anchor failure requests one restart; the second attempt observes committed deletion/invalidation and obtains no eligible links. Only that normally completed, fully authorized second read can produce 200/empty. If deletion was already visible during the first advisory read, the first attempt can instead complete normally. Neither path special-cases the test or returns an empty page merely because proof failed.

Denied-source filtering remains before TOP(26), and nextCursor still comes only from definitive authorized rows. D07's OwnerRevoked-only diagnostic exception, null-token list projection, owner/epoch/filter cursor binding and viewer refusal remain unchanged. The wrapper introduces no mutation, audit write, receipt operation, permission grant, source-helper change or weakened isolation.

### Handoff and remaining evidence

**Confirmed blocking source findings: 0. Required source patch from this addendum: none.** Source review supports publishing this bounded read-restart change for regression; it does not close the failing case.

Root/SQL CI should rerun the existing controlled five-operation theory and Sharing privacy/paging/D07 regressions on the exact final patch. Required List outcome remains 200/empty after the controlled committed single delete, with invalidated link and permanent viewer 404. Retain checks that an invalid/deleted/denied cursor anchor still returns 404. Exercise or inspect evidence for a second candidate change yielding failure after two attempts, and a current authority change between attempts being re-evaluated. Query-plan/locking verification from the original review remains pending; a new 404 or 503 must be diagnosed without weakening those assertions.

This follow-up used only read-only Git diff/revision inspection, source reads/searches and file hashes, followed by appending this report. Reviewer tests, build, runtime, fixtures, source edits and commits: **0**. Keep PR Draft and Learning Plan readiness false. The report covers this exact frozen source patch only.
