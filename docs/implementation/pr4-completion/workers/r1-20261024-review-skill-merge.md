# Independent Skill merge source review — 2026-10-24

**Disposition: bounded source signoff. No confirmed blocking defect identified in the six reviewed file versions below.** This is an independent source review of another worker's Skill merge implementation, not functional acceptance, SQL execution evidence, security certification or FX40/R1 completion.

Reviewer: Collections worker, reassigned by the root coordinator to independent Skill merge review; not the Skill merge author. Root owns corrections/integration. Latest assignment permits only this report to be written. No product files were edited during this review. No agents were delegated; no commit/push performed.

## Authority and reviewed source

Branch `impl/m01-s00-scaffold`, baseline HEAD `14c76bcb6960e4ce53369dce388a8480b1c0299a`, PR #4 remains draft. Current DEC-20260909-014, D01–D09=A and AGENTS code-only amendment apply. Review scope: Skill API/contracts, SqlSkillService, migration0088, SkillScreen and skillApi, with read-only tracing into their authorization/receipt/evidence/UI dependencies.

Read the worker's `r1-20261024-skill-merge-contract.md` and handoff. Source binding: P07-SKL-001; FX-40-BR-003/006; FX40-S02; shared owner/current-authority/concurrency/idempotency/lifecycle/UX contracts and common acceptance gates. Goals: NXG-FX40-G01, NXG-P07-01, NXG-SYS-02/03/05/08/09/13/14. D06 does not make Skill a Learning Plan source; D05 does not authorize Skill sharing/support projections.

The baseline plus SHA256 identifies the uncommitted source reviewed; other workers are changing the shared tree. The six hashes match the Skill author's final handoff hashes:

| File | SHA256 |
| --- | --- |
| `src/Nexora.Application/Learning/SkillContracts.cs` | `72FF4CBF725F4F74DE0CF3993D4335D272F5ABC353A209C22B21113CEB447CFA` |
| `src/Nexora.Api/Features/Learning/SkillEndpoints.cs` | `A760614D99FE28E7F82D58D7979608A1F74251B24BC3DD81A3949D77BFEFD94D` |
| `src/Nexora.Infrastructure/Learning/SqlSkillService.cs` | `AC9E8E2F75E2BB9BFD3E4F32870A3F254D0944A302B4F3300602F206C778F924` |
| `database/migrations/20261024_0088_skill_merge.sql` | `977888207B418C0E388674FB996BF90C97BB549AEA632C640605A09A901ED14D` |
| `web/Nexora.Web/src/SkillScreen.tsx` | `BC6392F8243B4EFC9E7E379B159AE1872064EAAA96B2FEC68CBB09164D4FDBF5` |
| `web/Nexora.Web/src/skillApi.ts` | `2BBC76584C540B389D9AD704171964F396B9C23AF9168B5101B50C031B95FF8F` |

## Source findings and bounded signoff

| Review boundary | Confirmed source evidence | Disposition |
| --- | --- | --- |
| Current SELF authority and owner isolation | API resolves the session principal and applies the existing CSRF group. SQL queries bind server-derived OwnerId. `RequiredAllowed` requires exact `learning.skill.merge` plus `learning.skill.read`; SqlSelfCapability checks current session/security stamp/account/PersonalSpace/role, module/dependencies and resolved permissions, with Admin explicit Allow/Deny handling. Merge holds the existing same-owner PersonalSpace update lock and repeats current authority before commit. No support/share mode or caller-selected owner is introduced. | Source signoff within these paths. |
| Evidence dependency proof | `ReadMergePlan` reads both owned Skills' actual evidence, rejects types other than `learning.Course`, and calls `SqlCourseService.ReadEvidenceSource` for each distinct source. That participant checks current course.read and same-owner non-Trash Course. Preview and new commit both inspect dependencies, and commit repeats the plan after writes. This is source-provider proof, not a registry grant. Existing SQL restricts SkillEvidence to Course with unique owner/Skill/resource membership. | Source signoff for the installed Course participant only. |
| Both revisions and preview freshness | Both current Skill ETags are compared; both Skills must be distinct, Active and unmerged. HMAC preview binds actor, owner, both IDs/revisions, evidence counts/overlap and expiry. New commit rejects expired or altered preview; evidence add/remove advances the parent Skill revision. The serializable boundary prevents the checked source rows from changing during the transaction. | Source signoff; real concurrency/clock-boundary behavior not executed. |
| Original-commit replay and dependency retention | Durable receipt claim binds canonical owner/source/revisions/body and actor/action namespace. Replay requires the typed versioned actor/owner/source/target envelope, expected result code/status and a non-null CourseReadRequired proof. It rechecks merge/read and conditionally current course.read, then returns only opaque target ACK. It performs no new merge and emits no historical fields/ETags. Replay precedes preview-expiry validation so a proven commit can still be reconciled after its original preview expires. Missing/ambiguous envelopes fail closed. | Source signoff; current Course **action** is rechecked on replay, while new writes use current individual Course availability. Replay is an acknowledgement, not a new source read/write. |
| Atomicity and evidence preservation | One transaction claims receipt, appends before-snapshot history, copies only absent evidence, archives source/sets MergedIntoId, touches target, updates both Resource revisions, audits and completes receipt. Source evidence IDs/notes/creation attribution remain retained; overlapping target interpretation remains unchanged. Copied refs retain original note/creation attribution. Target metadata/level/lastUsed are not inferred or overwritten. Failed checks leave the transaction uncommitted. | Source signoff for transaction structure; rollback/fault behavior not executed. |
| Immutable source and history | Source tombstone stays Archived; Preview/Transition reject MergedIntoId, while metadata/proficiency/evidence writes require Active. History is append-only through the examined application paths and stores both pre-merge metadata snapshots/counts. Purge reference checks include merge history. No unmerge, hidden source deletion or source-history rewrite is introduced. | Source signoff; no claim of database-user tamper resistance. |
| Migration compatibility and owner constraints | Existing migration0036 supplies the composite same-owner MergedInto FK. Migration0088 replaces its NULL-only marker check with Archived/nonself semantics; history has same-owner source/target FKs, unique source, JSON/count/overlap constraints and indexes. No cascade, backfill, data drop or Allow grant. Existing lifecycle constraints remain compatible with the written Archived/PreArchiveState combination. Rollback requires disabling FX40 before old binaries because old Unarchive handlers lacked the tombstone guard, as explicitly recorded in the worker contract. | Source signoff on DDL shape; migration/upgrade/rollback not executed. |
| Native UI, uncertain retries and privacy | The target picker uses current owner Active Skill pages, excludes source and merge tombstones, and preserves the applied query for pagination. Server preview supplies exact revisions/counts/expiry before confirmation. Uncertain commit retains the preview and key, blocks new intent, and retries the same body/key; client validates opaque target ACK. Definite stale failure clears selection and reloads. 401/403 clears Skills, preview, history, target results, editor and evidence state. Private snapshots render as React text; no persistent browser storage/raw HTML/provider navigation is introduced. Archived source exposes preserved evidence/history read-only. | Source signoff; keyboard/mobile/focus and actual network/revoke races not executed. |
| Integration registration observed | Root's current `ActionGrantPolicy.cs` includes `learning.skill.merge`; `Local/M01MigrationManifest.cs` includes migration0088. Existing ISkillService and endpoint mapping carry the new methods; existing Skills screen carries the UI. Global HTTP JSON configuration rejects unmapped fields and preserves the expected `etag` wire name. | Registration presence observed read-only, not runtime activation proof. |

## Review method and limits

Read AGENTS, nexora-engineering skill, implementation profile, TL core/startup/routing and verification instructions; mandatory docs/delivery/goals/current DEC014/D01–D09 sources were loaded in this session. Added review-specific reading of `.ai/roles/technical-lead/skills/code-change-review.md`, current FX40 feature/action/UX/phase/goals and learning DB dictionary. Applied architecture/backend/frontend/security/database/owner-isolation routing rules already loaded for this session. Supporting source traced: SqlSelfCapability, SqlRequestReceiptStore, SqlSkillEvidenceService, SqlCourseEvidenceSource, migrations0036/0063, SkillEvidence, ResourceFormDialog, ActionDialog, global JSON settings, action policy and migration manifest.

Executed only read-only `Get-Content`, `rg`, `git diff`, branch/HEAD inspection and SHA256 hashing for this review. No build, lint, typecheck, tests, fixtures, runtime, SQL migration, provider calls or browser execution was performed during the review assignment. The author's build results are attributed to the author and are not independently reproduced here. Earlier Collections implementation builds do not verify Skill merge.

Root/human gates remain: integrated current-head compilation/CI, actual SQL constraint/migration/rollback and concurrent mutation behavior, allowed/denied/revoked dependency matrix, audit/receipt fault rollback, evidence-preservation acceptance and browser/accessibility QA. A relevant file change invalidates this bounded source signoff and requires review of the changed paths. This report does not accept unresolved High/Critical risk or authorize merge/deployment/provider execution.

## Central integration delta review — requested follow-up

**Initial disposition: central action-policy integration required correction (P1); now CLOSED-SOURCE by the focused re-review below.** The original finding and unfixed hashes are retained as review history. The Skill domain source signoff above remains bounded to its six hashes. Its earlier “registration presence observed” row is only a literal-presence observation and must not be read as effective-policy approval.

Scope authorized by the coordinator: central Program DI/Collection endpoint mapping, the seven Collection action keys plus Skill merge key, M01 manifest0088/0090 and the existing expected-filename assertion. Domain implementation is covered by other assigned reviewers; Collections was authored by this reviewer and is **not independently reapproved** here. This follow-up changed only this report, and ran no builds, tests, assertions, migration or runtime.

### P1 — New keys were added to historical inventory rather than effective action eligibility

Location: `src/Nexora.Domain/Access/ActionGrantPolicy.cs:152` through the adjacent Collection rows; effective checks at `:317`, `:322`, `:441`, `:470`.

All eight requested keys occur only inside `ApprovedLocalActions`:

```text
learning.skill.merge
organization.collection.read
organization.collection.create
organization.collection.update
organization.collection.add
organization.collection.remove
organization.collection.reorder
organization.collection.delete
```

`ApprovedLocalActions` is explicitly documented as historical inventory and is not consulted by `CanGrantAllow`, `IsApprovedForLocalAction` or `IsAdminGrantable`. None of the effective sets those methods union contains these eight keys. In particular, `ApprovedSkillActions` still omits merge and `ApprovedShippedSelfActions` still contains Organization Tag keys only.

Confirmed consequence by source tracing: `CanGrantAllow` returns `DecisionBlocked` for these keys at its effective eligibility check; both boolean eligibility methods return false. `SqlAdminAccessService` uses CanGrantAllow for grant changes, and `SqlSelfCapability` rejects Admin actions failing IsAdminGrantable **before** consulting even an existing SQL Allow row (`Authorization/SqlSelfCapability.cs:105`). Thus the registered Collections handlers and Skill merge are unavailable to Admin SELF despite their approved catalog context and installed Resolved SQL permissions. This finding does not claim a User/SuperAdmin bypass or test execution.

Required root correction: add these exact eight installed SELF keys to an effective set already consumed by all three eligibility paths (or a narrowly scoped new effective set wired consistently into them). Preserve explicit Allow requirements, Deny precedence, module/source prerequisites and the exclusion of Collection sharing/support/template keys. Do not make the entire historical inventory authoritative. Review the corrected policy hash before declaring central integration signed off. No product correction was made by this reviewer.

### Central portions with bounded source signoff

| Central delta | Source conclusion |
| --- | --- |
| Program DI | `Program.cs:268` registers exactly one `ICollectionService` singleton factory constructing `SqlCollectionService` with the registered SqlConnectionFactory and existing idempotency secret. The constructor's two dependencies match. The existing startup check rejects a missing/blank NEXORA_IDEMPOTENCY_SECRET; the null-forgiving annotation does not introduce a fallback key. Required Organization namespaces are imported. No new provider/worker/service lifetime dependency is introduced in this delta. |
| Endpoint mapping | `Program.cs:392` calls `app.MapCollectionEndpoints()` once, in addition to the existing Tag endpoint mapper. The extension's existing route group is `/api/v1/organization/collections`; it is distinct from the existing `/organization/tags` paths. This is mapping/DI source compatibility approval only; domain authorization and endpoint behavior are not reapproved here. |
| Explicit migration manifest | `Local/M01MigrationManifest.cs:100` lists `20261024_0088_skill_merge.sql` once, after0087; `:102` lists `20261024_0090_organization_collections.sql` once, after0089 and before0091. Both exact files exist. No wildcard or automatic directory inclusion replaces the existing explicit allowlist. Adjacent0089/0091 content is outside this review. |
| Existing expected-filename assertion | `tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs:107` and `:109` contain the identical0088/0090 spellings in the same appended order as the manifest, with the adjacent0089/0091 entries also aligned. The diff updates only the existing filename list in `Local_release_migration_manifest_contains_all_reviewed_release_files`; it adds no new test method or fixture. Assertion implementation was read, not executed; this is not a claim that the test passed. |

### Central reviewed hashes

Baseline remains `14c76bcb6960e4ce53369dce388a8480b1c0299a` on `impl/m01-s00-scaffold`. SHA256 values below identify the source versions for this follow-up, including the **unfixed policy version**:

| File | SHA256 |
| --- | --- |
| `src/Nexora.Api/Program.cs` | `03F2E1F311205595E4C18684A0DE37CFED00608D9BFE26BE7C554EE04A8848EC` |
| `src/Nexora.Domain/Access/ActionGrantPolicy.cs` | `6620192BB9DC6DEE5DC130B94D5428FB6CE737404FBA59852054FFB832A7E79B` |
| `src/Nexora.Infrastructure/Local/M01MigrationManifest.cs` | `F4E736875E5BCC1ED586A58733E5A679968B2BC69CAECA0EB48CBFCDFE083F18` |
| `tests/Nexora.UnitTests/M01PolicyAndCaptureContractTests.cs` | `0D3C15D7A6D60D909644D517A9D494A3DE03E7DB4585D8BBFD9EA9F86D5B3AF4` |

Method: read-only source/diff/line-reference searches, branch/HEAD inspection, filename existence listing and SHA256 hashing. Existing mandatory instructions/current authorization remain those recorded above. Runtime, functional tests, source-domain acceptance and current-head CI remain outside this source-only follow-up.

## Focused corrective re-review — CLOSED-SOURCE

**P1 closed at source level.** Root's corrective policy delta fixes the eight reviewed keys without making historical inventory authoritative. No new confirmed defect identified within this focused correction. This closure supersedes the initial central-policy block above; it does not imply an executed policy test or runtime permission acceptance.

Source at re-review: branch `impl/m01-s00-scaffold`, HEAD `7dc98b42eadcbdab6053bb9422363403aec9aa35` plus the uncommitted corrective policy delta. Reviewer again changed only this report. Root performs rebuilds; no build/test/typecheck/runtime/SQL or product edits were performed by the reviewer.

| Reviewed key(s) | Effective membership and all three consumers | Source disposition |
| --- | --- | --- |
| `learning.skill.merge` | Present in `ApprovedSkillActions` at current line407. `CanGrantAllow` consults that set in both eligibility predicates (lines313/318); `IsApprovedForLocalAction` (line437) and `IsAdminGrantable` (line466) also consult it. The key matches no paused/network rejection prefix, so the source control flow now permits grant eligibility. | Corrected; effective local/Admin eligibility established by inspection. |
| `organization.collection.read`, `.create`, `.update`, `.add`, `.remove`, `.reorder`, `.delete` (each with the full `organization.collection` prefix) | All seven exact keys are in `ApprovedShippedSelfActions` at current lines239–240. Both CanGrantAllow eligibility predicates and both boolean consumers consult this set. None of the keys matches a paused/network rejection prefix. | Corrected for every one of the seven keys, not just read/create. |
| Historical inventory | The original eight keys and the two newly added historical-only Toolbox rows were removed from `ApprovedLocalActions`; that inventory is still absent from the three effective consumers. | No historical-inventory bypass introduced. |

Grant eligibility remains distinct from an actual Allow or available module. Read-only tracing confirms `SqlSelfCapability` still requires current verified/live account/session/owner, Ready/system/user-enabled modules and hard dependencies, Resolved requested permissions, an explicit Admin Allow for every requested action and absence of any requested Deny. The policy delta changes no SQL grants, statuses, source/lifecycle checks, pause prefixes or Plan contract. Collections' existing source participants/current reads and mutation read prerequisites remain separate; Skill merge retains skill.read plus its dynamic Course proof and conditional replay prerequisite. No Collection share/support/template action was added by this correction.

The same diff also places the exact 15 shipped Toolbox keys (catalog plus14 tools) in ApprovedShippedSelfActions, as root reported. Their membership therefore participates in the same effective consumers; network HTTP/DNS remain in BlockedOutsideM01. This focused re-review does **not** independently approve Toolbox implementation or its source contracts; those are assigned to its domain reviewers. The additional membership does not change the eight-key closure above.

Corrected policy SHA256:

```text
src/Nexora.Domain/Access/ActionGrantPolicy.cs
FD1A7C642DC53F51AC38A2B2116C368BB425E5A551A9A65867169506EEF40A54
```

Supporting authorization file read/hash for the retained Deny/module checks:

```text
src/Nexora.Infrastructure/Authorization/SqlSelfCapability.cs
7F37A926731832DB81BA1C9B8A518B99EC121FC833C72E6BF37085AE1A48AD91
```

Verification method was source/diff inspection and SHA256 hashing only. Root's final integrated build/CI and human runtime acceptance remain separate pending evidence; any relevant policy change invalidates this exact-hash closure.
