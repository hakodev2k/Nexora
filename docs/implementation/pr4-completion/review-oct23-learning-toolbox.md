# Independent source review — Learning Plans and offline Toolbox — 2026-10-23

## Scope, authorization and baseline

Review owner: current read-only Codex reviewer, separate from the Learning/Toolbox implementation authors and main integration workstream. This review spawned no additional agents. Sharing pagination is deliberately excluded: its assigned worker owns that review.

Latest task authorization is **read/build only**, with no product edits, tests, fixtures, migration execution, browser interaction, database access, runtime startup or grant activation. The sole deliverable written by this reviewer is this report. Earlier worker test claims in `LearningPlanImplementation.md` are historical author evidence, not executions or acceptance by this reviewer.

- Repository: local `D:/Projects/ASP_NET_Core_Developer_2026/Nexora`, expected Nexora source and branch present.
- HEAD: `fbafbaa482b094f26f067f1697d1f9d610e5367c`.
- Branch: `impl/m01-s00-scaffold`.
- Working tree: dirty, with staged, unstaged and untracked implementation changes. Review applies to the actual on-disk files identified below, **not the committed HEAD alone**.
- Snapshot recorded: `2026-10-23T00:29:26+07:00`, Windows/PowerShell, Asia/Saigon.
- Included the main agent's latest `/learning/plans/:id` route, `planId` capabilities/detail effect, owner-key remount, dirty-guarded `onSelectPlan`, and optional `onAuthLost` callback.
- `new LearningPlanReadiness(false)` remains intentional in `Program.cs:309–310`. No activation or grant change is recommended by this report. Learning findings below are latent defects in the prepared vertical, blocked from ordinary execution by this gate.

Plan performed: load instructions and normative decisions → trace DTO/API/service/SQL/source participants → trace React intent/route/privacy state → inspect Toolbox parser/input/network/storage boundaries → record concrete findings with source pointers. No runtime claim is made.

Authority: DEC-20260909-014 and DEC-20261019-PR4 D01–D09, particularly D04 (no live providers), D06 (Goal/Task/Course only) and D09 (Draft/review gates). Source trace: `P07-LRN-001`, `FX-40-BR-001/002/006`, `FX-40-AC-002`, `FX40-S05`; `FX-32-BR-001/002/003/005`, `FX-32-AC-001/002/003`, `FX32-S01/S02`. Goals: `NXG-SYS-02/03/05/08/13/14`, `NXG-FX40-G01/G02`, `NXG-FX32-G01/G02/G03`.

Exact actions reviewed: `learning.plan.read/create/update/link/unlink/reorder/complete`; `toolbox.xml.run`, `toolbox.datetime.run`, `toolbox.csv.run`, `toolbox.jwt.run`, and their shared catalog/run authorization paths. No unimplemented lifecycle or provider action is treated as verified.

## Verdict

**CHANGES REQUESTED — two concrete source findings, both Medium/P2.** One prevents the new post-create detail handoff; one retains a Toolbox workbench after a known authority denial. Neither is a demonstrated cross-owner leak or database corruption. No Critical/High exploitable defect was established in this read-only pass. This is not security acceptance, SQL acceptance, functional verification or permission to enable Learning Plans.

## R23-LT-01 — Committed Plan creation cannot navigate to its new detail route

- Type: BUG; severity Medium; priority P2; confidence High; status OPEN.
- Primary location: `web/Nexora.Web/src/LearningPlanScreen.tsx:75–101`, `LearningPlanOwnerScreen.send()`; blocking guard at line 70.
- Integrated call path: `web/Nexora.Web/src/App.tsx:1485` (`onSelectPlan`), `App.navigate()` at lines 6982–6997 and `web/Nexora.Web/src/navigationGuard.ts:8–10`.
- Contract: D06 React flow Create → Detail; worker contract promises valid ACK is final and separately authorized detail GET. The new route must preserve that handoff without weakening uncertain-intent navigation protection.
- Preconditions: a separately reviewed/enabled Learning environment; own actor with Plan create/read; native Shell callback supplied. The shipped default-false gate prevents executing this scenario today and must stay false.

Source-derived reproduction (not executed):

1. Open `/learning/plans`, choose Tạo kế hoạch, submit title `Chuẩn bị phỏng vấn .NET`.
2. Let POST commit and return a valid opaque ACK for a new Plan ID.
3. Let the subsequent capabilities request authorize `learning.plan.read`.
4. `send()` reaches `onSelectPlan(ack.id)` while `flight.current` is still `true`; the assignment to false occurs only in `finally` after the callback returns.
5. Shell calls `navigate('learningPlans', undefined, false, ack.id)`. Since the URL changes, `attemptDirtyLeave()` executes. The Plan guard returns true for `flight.current` without calling the continuation, so navigation returns early. The still-busy form guard may also intercept the transition.

Expected: the one committed Plan is shown at `/learning/plans/<ack.id>` through a currently authorized GET; no duplicate POST and no phantom uncertainty.

Actual implied by the synchronous call chain: URL does not change and no detail GET is started by this branch. `setPage(null)` and `setDetail(null)` already ran at line 82, leaving the cleared view with the “Đang kiểm tra quyền đọc hiện tại” notice. The `else await open(ack.id)` fallback is not taken because the callback exists. Returning from `finally` does not retry navigation. Workaround: reload the list and choose the persisted Plan. Creating from an existing detail route has the same issue when ACK ID differs from `planId`.

Root cause: post-commit navigation is performed before write-flight and modal busy state have settled, through the same guard that correctly blocks leaving an unresolved mutation.

Recommended fix: queue an ACK-confirmed destination, complete pending/flight/busy/dialog state, then perform the normal guarded navigation after those states have settled (for example a post-render effect with the committed destination). Ensure the closed modal guard is removed too; changing only `flight.current` immediately before the callback can still leave the modal's busy guard active. Do not globally bypass dirty guards, replace navigation indiscriminately, or treat a successful ACK as uncertain because navigation failed.

Acceptance for the main/owner verification workstream: create from both list and existing detail; each returns exactly one committed Plan and navigates to its own detail; Back/Forward work; failed/uncertain writes still block navigation and retain the original request/key; denial after ACK never causes a second mutation. No test was authored or run here.

## R23-LT-02 — Catalog denial leaves prior Toolbox input and output visible

- Type: SECURITY / privacy-state handling; severity Medium; priority P2; confidence High; status OPEN.
- Primary location: `web/Nexora.Web/src/App.tsx:5415–5430`, `DeveloperToolsScreen.loadCatalog()`.
- Related locations: run-path clearing at lines 5484–5488; workbench rendering at lines 5522 onward; server `LocalToolboxService.Catalog()` and `ModuleUnavailable<T>()`.
- Contract: `docs/implementation/toolbox-extra-offline-contract.md`, UX/integration: clear protected input/output on auth loss or module denial. `NXG-SYS-08/13` and browser privacy rules require a known denial not to leave private stale state displayed.
- Preconditions: a previously loaded Toolbox catalog/workbench and locally entered synthetic token or text; permission/module subsequently revoked while the shell still has its prior profile projection. Existing reload button is reachable without a new profile fetch.

Source-derived reproduction (not executed):

1. Open `/developer/tools`, choose JWT, enter a synthetic compact token and decode it; input and decoded claims are held only in component state.
2. In the separate authorized administration context, disable that user's FX32 module, or revoke an Admin's catalog-read permission. This report performed neither action.
3. Back in the existing Toolbox screen, click Tải lại.
4. Real catalog service returns `409 ModuleUnavailable` when the current SQL capability check fails. A 403 boundary denial follows the same client failure path.
5. The catch assigns only `error` and handles 401 via `onAuthLost`. It clears neither `tools`, `input`, `output`, nor `copied`, and does not invalidate `toolEpoch`.

Expected: known catalog auth/module denial immediately clears private workbench values and removes the stale actionable catalog; controlled retry may rediscover current capabilities. Server enforcement remains mandatory.

Actual implied by rendering: loading briefly hides the workbench; `finally` sets loading false and the old nonempty `tools` array renders it again with the original input/output and Copy output control. The run-path has explicit denial clearing, but catalog reload bypasses it. This does **not** prove a remote user can read another owner's token or that any token was sent to the server; it is retention/redisplay after a known denial in the existing browser session. The catch branch predates parts of this extension but is reused by the new JWT surface and contradicts its explicit privacy contract.

Recommended fix: centralize authority-loss cleanup and use it in both catalog and run catch paths for actual auth/permission/module denial codes/statuses. Invalidate outstanding epochs, clear private input/output/copy state and stale tools, and keep unavailable state until successful catalog refresh. Keep recoverable parsing failures distinct so ordinary invalid input remains editable. Ensure late catalog/run responses cannot repopulate state after a denial or owner/context change; do not change server capability policy to work around UI state.

Acceptance for the main/owner verification workstream: after populated JWT and XML workbenches, catalog denial removes input/output/copy and stale actions; reopening/reloading while denied does not restore values; a recoverable 422 parser failure preserves input; token content remains absent from the JWT authority-check request. No fixture or test was created or executed here.

## Security/DB/API paths inspected without an additional confirmed finding

These are source observations, not passing runtime cases:

- `Program.cs` registers the actual service and endpoint map with readiness false. Migration 0086 inserts missing Plan actions as Gated, does not resolve/promote existing permissions or grant users access. Missing Plan Admin allowlist activation is not reported as a defect while intentionally gated. Manifest orders 0086 before 0087.
- Plan DTOs reject unmapped fields; source kinds are exactly Goal/Task/Course. Plan mutations use server principal OwnerId, owner-bound SQL, Serializable transaction, owner write lock, strong quoted rowversion checks, UUID request keys, receipt/audit persistence and opaque ACK. Existing receipt-store/Sharing changes were not independently re-reviewed in this assignment.
- Source SQL is selected through fixed module-owned participants, never client SQL. Goal active Trash is excluded; Task and parent deletion/Trash are excluded; Course eligibility excludes Archived/Trash/Purged payload states. Source read capability precedes source-title access. Missing source projections retain only Plan-owned reference/target metadata and mask title/state/percent; no inferred completion/progress formula was introduced.
- 0086 includes same-owner Plan/Resource and PlanItem/Plan/Resource foreign keys, allowed-kind check, unique typed source and unique per-plan position. Reorder uses disjoint temporary positions before final compact order; stale Plan ETags are checked while the root update lock is held. No source deletion cascade is introduced.
- Cursor AES-GCM binds actor, owner, action and filter with a purpose-derived key; decoding rejects malformed/noncanonical ciphertext. This inspection is not cryptographic specialist approval or SQL pagination/concurrency proof.
- The new detail route validates UUID shape, shares the owner-context remount, and loads capabilities before the route-driven detail GET with abort/generation checks. Optional 401 callback is present. The inspected owner key uses profile ID/PersonalSpace/role/modules; no claim is made that it represents every possible session/policy epoch.
- XML reader prohibits DTD and external resolution, checks depth/node bounds before materialization, bounds encoded output, and uses numeric-only XML error positions. Mixed-content indent suppression is present. CSV has explicit width/row/cell bounds and displays formula-like values as JSON text. Epoch conversion requires explicit units and integer/range checks.
- JWT path calls `runDeveloperTool('jwt', '', {})`, not with token content; decoding is browser-only, canonical base64url/fatal UTF-8/object/depth/node bounded, with `signatureVerified:false`. Display is escaped React `<pre>`. No token persistence, fetch in decoder, raw HTML sink or provider activation was found in these paths.
- 0087 adds only missing four offline permission definitions; existing Blocked/Paused/Gated statuses and user grants are preserved. This is not permission to activate any other tool/provider.

## Evidence and limits

Executed tools: repository `Get-Content`, `rg`, `Get-FileHash`, `Get-Date` and read-only Git status/revision/branch commands. Initial Git reads hit dubious ownership under the restricted runner; retried with **per-command** `-c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora`. No global Git config or repository permissions were changed.

Builds: **NOT RUN** (optional for this bounded static review). Tests: **NOT RUN** by explicit latest instruction. Runtime/browser/SQL/migrations/fixtures/grants: **NOT RUN / NOT CREATED / NOT ACTIVATED**. No reproduction rate, screenshot, trace, CI URL, test count or functional pass is invented. Reproduction sequences above are precise future validation instructions derived from source.

Mandatory startup read: `AGENTS.md`, `.agents/skills/nexora-engineering/SKILL.md`, `.ai/profiles/nexora-implementation-agent.md`, `.ai/roles/technical-lead/README.md`, its `rules/core-rules.md`, `.ai/routing.json`, `.ai/verification.md`, docs README/delivery/current-scope/goals README/system/module32/module40/evidence template; current PO decision records and D06 vertical contract; current feature/action/UX/DB and authored implementation handoffs. Specialist reading used architecture ownership/module/transaction rules; backend authorization/security/migration/input-validation/idempotency/REST/data-access rules and skills; database migrations and transaction/isolation skills; React browser-security/accessibility/design-system/form-validation/component/frontend-security guidance; security/privacy/identity/secrets/threat-model guidance and owner-boundary control README/rules/review skill. Routing test guidance does not override the latest no-test instruction.

Reviewed file SHA-256 snapshot (raw on-disk bytes, including working-tree changes):

| Path | SHA-256 |
|---|---|
| `web/Nexora.Web/src/LearningPlanScreen.tsx` | `155f2b74504f71cf687e91cf4cd25fde0d0ad40b8dec88ebcd3f2e4a82272966` |
| `web/Nexora.Web/src/LearningPlanApi.ts` | `fdb1275fd9271cb72eda2173e687dd264a52b119e7e03adac395887eda6e76c6` |
| `web/Nexora.Web/src/App.tsx` | `ab59859ad62df2b3c106698cd1db818066af75fbe35b926a1e223cc23ea0c621` |
| `src/Nexora.Api/Program.cs` | `994819d53a137abd33ec8c3173bee4297fa983f23f6bdd62a5c9c5fe1d03cce4` |
| `src/Nexora.Api/Features/Learning/LearningPlanEndpoints.cs` | `1e470500bb956c72a44752cef1ddd7d46ab7a9492b1f959f87c5e9c8a743a341` |
| `src/Nexora.Application/Learning/LearningPlanContracts.cs` | `0d72d4dd3315a55a85bb6345aff57bd0a92e228a9e8baa68493aab9113084f05` |
| `src/Nexora.Application/Learning/LearningPlanImplementation.md` | `c4d10ef6ef69f4876ca1651b7363a88bd495376ea0a6ef68612935640477bfe3` |
| `src/Nexora.Infrastructure/Learning/SqlLearningPlanService.cs` | `b4d6ffe796797bf8a7aeb1d7448e23bbc1621e5acc49bd83453e385545d50d3f` |
| `src/Nexora.Infrastructure/Learning/LearningPlanCursor.cs` | `f63e41df0d211649b70882e8f363b6bb2f367e27e2a941ab0581657b0268d1aa` |
| `src/Nexora.Infrastructure/Learning/SqlLearningPlanSource.cs` | `ee557db55371e887506103bd0c44714727b2dfe604dc8535fe4390372ad2e2e8` |
| `src/Nexora.Infrastructure/Learning/SqlLearningPlanCourseSource.cs` | `10dd7d3eb032da0b7f687bf635bdb5178259061448ce9f5a73b66b1e6d6c3f5c` |
| `src/Nexora.Infrastructure/Goals/SqlLearningPlanGoalSource.cs` | `b46a7b5722231a3e71123c052efd552b4f0bf60e47afe9042c727c199f05d318` |
| `src/Nexora.Infrastructure/Productivity/SqlLearningPlanTaskSource.cs` | `6ba60b907fd67afdaa988bdc01c525a7fce3c89b27233b82200206e01edc03a2` |
| `database/migrations/20261021_0086_learning_plans.sql` | `48829fc7bf4bb70668f3a3829693d903fc60a96c60d9554448715ed8954f0454` |
| `database/migrations/20261021_0087_toolbox_extra_offline.sql` | `52f66f0b595f0c62d81f6060ea29ece113a9f65594bff7a5f062f4c74b2a88cb` |
| `src/Nexora.Infrastructure/DeveloperTools/LocalToolboxService.cs` | `7390ab86b07d599ed2382bc5484ab09c2c3647b54cb60ed4be03592621203130` |
| `src/Nexora.Infrastructure/DeveloperTools/LocalToolboxService.Offline.cs` | `7e10f20972e103bf990acb3a855e81ceaab049811adbd1ccf7b90d1a20af7e44` |
| `web/Nexora.Web/src/ToolboxBrowserJwt.ts` | `e8fb41f44a4749dfaefe371a709f8d3dd7c0df9f0773c38b43c72b99641ae30f` |
| `web/Nexora.Web/src/ToolboxExtraOptions.tsx` | `85c96aded9f29b0ee252b208fab25fd0bdd7426a8139d40f17514484029cc29a` |

Handoff: main integration owner should fix R23-LT-01 and R23-LT-02, request focused source re-review, and preserve Learning false readiness until the separate approved source/DB/security and real runtime acceptance gates are satisfied. No approval, merge, activation or feature acceptance is supplied here.

## Focused remediation re-review — 2026-10-23 00:33–00:35 +07:00

This addendum supersedes the two OPEN dispositions and CHANGES REQUESTED verdict **only for the two reviewed source defects**. Original observations and hashes above remain historical evidence. User froze the source and requested only focused re-review plus this append; no product source edits, builds, tests, fixtures, runtime/browser/SQL execution or grant activation were performed. Mandatory startup/skill/profile/core rules were refreshed; the previously read product/security contracts still govern this pass. HEAD remains `fbafbaa482b094f26f067f1697d1f9d610e5367c`; these fixes remain working-tree changes.

| Finding | Current disposition | Evidence |
|---|---|---|
| R23-LT-01 | **REMEDIATED_IN_SOURCE — focused static re-review closed; runtime verification pending** | `LearningPlanScreen.tsx:23,73–78,90–109`; modal deletion/guard cleanup traced below |
| R23-LT-02 | **REMEDIATED_IN_SOURCE — focused static re-review closed; runtime verification pending** | `App.tsx:5415–5443,5491–5504`; catalog/run epoch and clearing traced below |

### R23-LT-01: committed navigation and passive modal unmount

The original synchronous `onSelectPlan(ack.id)` call inside the still-active write flight is gone. `send()` now queues `setCommittedDestination(ack.id)` only after a valid ACK and positive current read capability. The destination effect at lines 73–78 requires a destination, `busy === false`, `dialog.open === false`, `flight.current === false`, empty `pendingRef.current`, and an actual navigation callback. `finally` resets the flight ref and busy state; the queued destination is cleared before invoking the ordinary Shell callback. The 401/403/404 authority/source-unavailable branch at line 33 clears the queued destination as well. An uncertain/noncommitted attempt never queues it.

Specific passive-unmount check requested by main:

1. `ResourceFormDialog.tsx:23` returns `null` when `dialog.finish()` has committed `open=false`; this **deletes `OpenFormDialog`**, rather than merely hiding its portal/DOM.
2. Its guard was registered by a `useEffect` at lines 42–46. That effect returns the cleanup supplied by `registerDirtyLeaveGuard`; `navigationGuard.ts:6` synchronously removes the guard token from the map.
3. Read the actual installed `react-dom` package metadata: version **19.3.0**. In its development source, `react-dom-client.development.js:20130` calls `commitPassiveUnmountOnFiber(finishedWork)` before `commitPassiveMountOnFiber(...)` at line 20134. `recursivelyTraversePassiveUnmountEffects` processes the deleted subtree and `commitPassiveUnmountEffectsInsideOfDeletedTree_begin` invokes its passive hook cleanup. The production source has the same unmount-before-mount sequence at `react-dom-client.production.js:13250–13251`.
4. Therefore the deleted modal's passive guard cleanup executes before the surviving Plan component's destination `useEffect` setup in that passive flush. If the modal was already removed in an earlier commit while capabilities were pending, its cleanup precedes navigation by an earlier flush instead. The old modal busy/dirty ref cannot remain registered solely because its last render was busy.
5. The Plan's own persistent guard remains registered but evaluates false at this point because both flight and pending refs have settled. Shell still calls normal `attemptDirtyLeave`; no global bypass or assertion weakening was introduced.

The modal's focus-return microtask is separate from guard removal: guard removal is synchronous during passive cleanup. No claim is made that keyboard/focus behavior has been exercised in a browser. The original source cause is closed without requiring a timeout or force-navigation workaround. Runtime create-from-list/create-from-detail, uncertainty and Back/Forward checks remain pending under the original acceptance criteria.

### R23-LT-02: shared authority cleanup and late-response rejection

`DeveloperToolsScreen.clearToolAuthority()` at lines 5415–5419 increments the shared `toolEpoch` and clears tools, input, regex pattern, extra options, output, copied state, busy and loading. Both catalog and run catches check that their captured epoch is still current, then call this function for 401/403/409; 401 still delegates to the Shell auth-loss callback. A catalog denial can no longer return to the old nonempty workbench with its previous JWT/text/output.

`loadCatalog()` now increments/captures that same epoch before the request; success, catch and finally all check it. Denial cleanup advances the epoch again, so its old finally cannot restore stale loading state; cleanup itself explicitly sets loading/busy false. An older catalog response or older run completion cannot repopulate tools/output after a later cleanup. Empty/malformed catalog items also use the clearing path. If a refreshed catalog excludes the currently selected tool, its fallback selection resets input/pattern/output/copy and uses the new tool's default extra options. Ordinary recoverable parser failures remain outside the authority-clear statuses and retain editable input. JWT still calls the payload-free server authority boundary.

These changes address the exact catalog-denial redisplay finding. This is not a blanket assertion about every possible clipboard interaction, owner/session transition, or Toolbox UX behavior; no new concrete defect was identified in the focused remediation paths. The original runtime privacy/negative-input acceptance cases remain pending.

### New raw-byte SHA-256 snapshot

Hashes were read twice during this pass; the frozen application files matched both times. Node module files below were **read as local implementation evidence only**, not changed, imported or executed; they are not proposed committed artifacts.

| Path | SHA-256 |
|---|---|
| `web/Nexora.Web/src/LearningPlanScreen.tsx` | `74d213e40f223cdd59513de229d35c5c3c23b3be2471cd754373cf732acd6196` |
| `web/Nexora.Web/src/App.tsx` | `170f1a249b742573a65d0381dc7e7d82228616f350e195b709211b3e070337c5` |
| `web/Nexora.Web/src/ResourceFormDialog.tsx` | `b0f6dd3530cebfe121c7ef480b079692b394007ebdb6fe86e671e738ce5c7883` |
| `web/Nexora.Web/src/navigationGuard.ts` | `28b2d3a375900230ea322ce47f1956407e92133935ed742ee1e9806a9ed2d651` |
| `src/Nexora.Api/Program.cs` | `994819d53a137abd33ec8c3173bee4297fa983f23f6bdd62a5c9c5fe1d03cce4` |
| `web/Nexora.Web/node_modules/react-dom/cjs/react-dom-client.development.js` | `c10b5e5ad37ca22c88b58bf4ada9a3b655ab79934c0b5cc6bad26f42c774584f` |
| `web/Nexora.Web/node_modules/react-dom/cjs/react-dom-client.production.js` | `8a8147c1cebdb19d82fd11bdb76391be9b20d00edf67cc8e8b30f44e850ce693` |

Final focused disposition: **no remaining source objection to these two remediations**. Learning readiness remains false, with no grant activation. Broader security/DB/runtime acceptance and the two findings' actual functional retest are still pending; this addendum grants no feature acceptance or release approval.
