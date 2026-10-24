# Independent R1 review — Plan lifecycle and offline Toolbox

2026-10-24. Reviewer: this independent review session; no implementation authorship.
Disposition: **no confirmed source blocker in the completed assigned worker files**.
Final root integration and remediation were subsequently reviewed: **R24-PT-01
is closed in source; no remaining confirmed finding in the assigned scope. See
the final remediation addendum below.** This is bounded static
source/security/DB review, not functional acceptance, SQL verification or R1 approval.

## Task brief

| Field | Record |
|---|---|
| Authorization | Explicit independent review of completed Learning Plan archive/unarchive and typed source callback; Toolbox text diff/color helpers/options, C# authority seam and migrations0089/0091. Write only this report; no product edits, tests, fixtures, runtime, agents, commit or push. |
| Source | `impl/m01-s00-scaffold`, HEAD `14c76bcb6960e4ce53369dce388a8480b1c0299a`, plus shared uncommitted work. Assigned executable snapshots below identify the reviewed bytes. |
| Accountable integration owner | Root Technical Lead. Other workers' dirty changes were preserved. Skill merge, Collections and Course implementation are outside this assignment. |
| Authority | AGENTS code-only amendment; DEC-20260909-014; current DEC-20261019-PR4 D01–D09=A. D06 restricts PlanItem sources to Goal/Task/Course; D05 denies unapproved sensitive projections; D09 preserves draft/review/current-head CI gates. No grant or activation is authorized here. |
| Bounded plan | Trace lifecycle endpoint → principal/current capability → preview/receipt → owner/revision/state → SQL/registry/audit commit; inspect callback privacy and uncertainty; trace browser authority seam → bounded diff/color → inert output; inspect additive migration/rollback effects. |
| Readiness | `LearningPlanReadiness(false)` remains explicit in Program.cs; DTO default is also false. This is intentional and must remain. Migration0089 inserts absent actions as Gated. |
| Gates | Root has not notified this session that final App/catalog/policy/manifest wiring is complete/frozen. Owner runtime/SQL/browser QA and final-source CI remain separate, unexecuted gates. |

## Source and goal bindings

Sources read include current docs README, delivery README/current scope, goals
README/system/phase/module32/module40/evidence template, current DEC014 and D01–D09,
D06 `pr4-decision-contracts-20261019/04-learning-plan.md`, feature32/40,
action-catalog32/40, UX32/40, relevant phase06/07 requirements, existing0086 schema,
the completed lifecycle/offline Toolbox worker reports, and
`docs/implementation/toolbox-more-offline-contract-20261024.md`.
Earlier implementation/review reports were context, not current execution approval
or independent runtime evidence. The older Plan implementation report's QA allowance
does not override this session's explicit no-tests/no-runtime instruction.

| Goals | Source requirement / acceptance | Exact selected action / surface | Reviewed source evidence | Executed acceptance |
|---|---|---|---|---|
| NXG-FX40-G02; NXG-SYS-02/03/05/09 | P07-LRN-001; FX-40-BR-006; FX40-S05; D06 | `learning.plan.archive`, `learning.plan.unarchive`; preview requires intended action + `learning.plan.read` | Service PreviewLifecycle/Lifecycle/Write; lifecycle token codec; endpoints;0089 | NOT RUN |
| NXG-FX40-G01/G02; NXG-SYS-02/08/13 | FX-40-BR-002; source preservation related to FX-40-AC-002; FX40-S05 | `learning.plan.read`; typed Goal/Task/Course open handoff | LearningPlanScreen.openSource; source participant protocol and current projection | NOT RUN; destination integration pending |
| NXG-FX32-G01/G03; NXG-P06-01; NXG-SYS-03/08 | P06-DAT-004; P06-TBX-001/002/003/004/007/008; FX-32-BR-001; relevant shared bounds/security of FX-32-AC-001/003 | `toolbox.text_diff.run`, `toolbox.color.run`; FX32-S02 | Browser helper/options; RunMoreOffline;0091 | NOT RUN |
| NXG-FX32-G02; NXG-SYS-13/14 | FX32 declared color/alpha/contrast semantics; D09 | Color conversion/palette; truthful processing and release status | parseColor/fromHsl/describeColor/convertColor; warning strings; report limitations | NOT RUN |

AC002 in FX40 specifically concerns Course deletion preserving linked Task/Goal;
this lifecycle review checks the related no-source-mutation invariant, and does
not claim to certify the entire Course deletion scenario. Likewise XXE, JWT and
network redirect acceptance are not established by reviewing diff/color.

## Concrete review result

No new confirmed correctness, authorization, privacy, replay or SQL-constraint
defect was found in the completed worker files identified below. No hypothetical
test gap is promoted to a source defect. The following are source observations,
with the exact boundaries of this review.

### Plan lifecycle, replay and source callback

- `SqlLearningPlanService.cs:164`, `PreviewLifecycle`: current intended action and
  plan.read are required before root lookup; strict quoted revision must match;
  preview contains transition/count, not source titles. It uses a Serializable
  transaction, bounded SQL lock timeout and a five-minute authenticated proof.
- `LearningPlanCursor.cs:14/22`, `EncodeLifecycle/DecodeLifecycle`: separate
  purpose-derived AES-GCM key; actor/PersonalSpace/Plan/action authenticated as
  scope; ETag/from/to/count/expiry authenticated as payload. Noncanonical base64url,
  wrong prefix, oversized or unauthentic ciphertext rejects. This is source review
  of the preview protocol, not a blanket crypto/key-management approval.
- `SqlLearningPlanService.cs:228`, `Write`: principal OwnerId/UserId are checked
  against PersonalSpace under UPDLOCK/HOLDLOCK. Current exact write authority
  precedes receipt claim. Digest-bound committed replay is handled at245–254,
  before the fresh ETag/state/preview path. It verifies saved actor, owner, Plan,
  action, status, source and result code, checks current original authority, and
  returns opaque ACK without a second business update or audit. Thus preview
  expiration or a later archive/unarchive cycle does not itself defeat a provable
  committed retry. Receipt-store retention and broad compatibility semantics are
  inherited dependencies, not newly certified here.
- `Lifecycle` at193 and `LifecycleDestination` at214: fresh commit validates
  confirmation, bound preview, expiry, current revision/state and retained item
  count. Archive saves actual Planned/Active/Completed; unarchive accepts only the
  actual saved prior state and clears it. No guessed fallback state exists.
  Neither transition invokes source writes or changes PlanItems/order/TargetOn.
- `Write` at268–279 synchronizes root/registry availability and revisions, writes
  safe from/to audit metadata and completes the receipt in the same transaction;
  final current authority/registry checks precede commit. The post-update read
  used for audit occurs after registry synchronization, so Archived registry
  matching does not hide the new state from that read.
- `LearningPlanEndpoints.cs`, `MapLearningPlanEndpoints/Run`: existing CSRF group
  applies to both POSTs; original If-Match/key reach the service; preview carries
  an ETag header; committed responses use the opaque acknowledgement branch.
- `LearningPlanScreen.tsx:86/141/213`, `send/previewLifecycle/lifecycle dialog`:
  original serialized request/key/revision/token are frozen before send; uncertain
  outcome keeps original retry intent. A successful write ACK is final even if
  subsequent read fails. Confirmation describes retained references and no source
  transition. Denied/unavailable responses clear private detail/choices/lifecycle.
  Expired or stale preview requires closing/reloading/reviewing current impact;
  the server never accepts a replacement revision silently.
- `LearningPlanScreen.tsx:150`, `openSource`: old source titles are cleared while
  rereading the current Plan/source projection. Only a still-present item with
  available projection/title reaches the typed callback. Generation/alive checks
  reject obsolete reads. The callback carries kind/id only. Destination authority
  is still the root/native resolver's responsibility, not granted by the callback.

### SQL changes

- `20261024_0089_learning_plan_lifecycle.sql`: requires0086 contract marker,
  PreArchiveState, enabled/trusted named archive constraint, valid current prior
  states, and exact Plan registry contract. It inserts only absent archive keys
  as Gated; existing permission status/grants/registry capabilities remain intact.
  Lifecycle marker and inserts commit together, supporting crash-gap rerun.
- The underlying0086 `CK_LearningPlan_Archive` explicitly requires nonnull valid
  prior state when Archived and null otherwise; it avoids SQL UNKNOWN accepting
  a null Archived prior state. PlanItem already has owner-composite FKs to Plan
  and Resource, Goal/Task/Course kind constraint, unique position and unique typed
  source per Plan.0089 adds no destructive DDL, backfill or deletion cascade.
- `20261024_0091_toolbox_more_offline.sql`: two exact missing permission
  definitions only, with UPDLOCK/HOLDLOCK existence probes in one transaction.
  Existing Gated/Paused/Blocked statuses are not promoted, no AdminPermission or
  module grant is created. Actual SQL locking/rerun behavior was not executed.
- Rollback for both slices is code/wiring disable/revert while retaining
  permissions, Plan/items, audit and receipts. No destructive down SQL is needed
  or approved. Keep Plan readiness false throughout mixed-version rollback.

### Offline Toolbox boundaries

- `LocalToolboxService.MoreOffline.cs:7`, `RunMoreOffline`: fixed two-code allowlist,
  empty input/options required; returns empty authority-check output. Existing
  `LocalToolboxService.Run` checks current SQL SELF capability before this seam.
  The partial does not process, store or log text/color payloads.
- `ToolboxMoreOffline.ts:175`, `runMoreOfflineBrowser`: snapshots options before
  awaiting a zero-argument rejecting authority callback; checks abort before and
  after that await and before returning. No fetch/storage/clipboard/HTML execution
  sink exists in the helper. Integration must pass only empty server payloads and
  must not display the empty server authority response as a calculated result.
- `diffText` at41: UTF-8 bound128 KiB per side, malformed-surrogate rejection,
  explicit line/scalar and newline modes, token/cell bounds before Uint16 table
  allocation, cooperative yields/abort checks every16,384 cells. Traceback uses
  deterministic deletion-first ties and preserves grouped literal operations.
  Terminal newlines are retained by tokenization; normalized mode is explicit.
  Bounds establish finite work/memory, not a measured wall-clock latency claim.
- `parseColor` at96 and `convertColor` at157: finite bounded numeric syntax,
  explicit HEX/RGB/HSL/alpha grammar, exact option allowlists, opaque background
  requirement and fixed palette sizes. CSS URLs/variables/code are not accepted.
- `describeColor` at142: composites alpha onto explicit opaque background before
  sRGB luminance, applies AA/AAA thresholds to the unrounded ratio, and separately
  rounds displayed values. Warnings disclose channel/alpha quantization, duplicate
  palette possibilities and absence of accessibility certification.
- `ToolboxMoreOfflineOptions.tsx`: controlled labeled inputs, declared limits and
  compare modes; Cancel is supplied by the screen. Helper errors are fixed safe
  messages; JSON output is bounded to2 MiB. Rendering, draft cleanup, Cancel notice,
  dirty protection and output-copy behavior require the final App integration pass.

## Remaining sign-offs and bounded completion blockers

| Item | Concrete outstanding work / next owner |
|---|---|
| Final root source integration — PENDING | Root must notify final wiring/freeze, then reviewer rechecks App.tsx DeveloperToolsScreen selection/defaults, controlled second input/options mounting, empty-payload run routing, Cancel/clear/auth/unmount epoch handling, inert output, and native typed Plan navigation; also exact catalog/switch/policy keys and ordered manifest0089/0091. Root files were changing during inspection and are not signed off by this report. |
| Plan activation — BLOCKED intentionally | Keep Program readiness false and existing gated permissions. Source-owner/DB/security and owner acceptance gates must be reconciled before any separately reviewed activation. This report is not that activation decision. |
| SQL operational acceptance — NOT RUN | Owner: upgrade/replay/crash-gap and rollback/lock/concurrency evidence for actual SQL Server, plus atomic lifecycle/registry/audit/receipt and all three exact prior states. Static constraint inspection is complete for assigned changes; SQL execution is not. |
| Functional/browser/privacy acceptance — NOT RUN | Human owner: preview expiry/staleness/revoke, uncertain replay after expiry/later transitions, unavailable-source opening, diff limits/cancel/newline/scalar cases, color alpha/contrast boundary behavior and keyboard/mobile/focus. No tests/fixtures were created here. |
| Current-source compile/CI and PR state — PENDING root | Author builds recorded in worker handoffs are author implementation-error checks, not independent runtime evidence. Root owns final relevant-source builds/green current-head CI, traceability and draft PR4; no merge or release approval. |
| Plan R1 remainder — OPEN, outside this completed lifecycle increment | Plan-specific Trash/cohort/prior-state/restore/purge contract and implementation remain missing as recorded by lifecycle worker; progress-provider enrichment and remaining declared list filters remain visible. Do not label the entire Plan/FX40 complete from archive/unarchive alone. |
| Toolbox R1 remainder — OPEN, outside two new tools | Preserve worker's explicit inventory of unimplemented YAML/conversion/date/cron/Markdown/format/QR/URL/header/certificate families and output/history/favorites/support work. Network actions remain paused. Diff/color source review does not close FX32 or R1. |

At inspection, root additions had begun appearing in App, catalog, policy and
manifest, but the App options/rendering was still being integrated. That partial
state is deliberately tracked as a pending integration gate, not reported as a
confirmed defect in a completed helper or as a final root-wiring verdict.

## Evidence and limits

Executed: read-only `Get-Content`, `rg`, `Get-FileHash`, `Get-Date`, Git
status/branch/revision/scoped diff; `apply_patch` to this report only. Initial Git
reads failed with dubious ownership; retried successfully with command-local
`-c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora`. No global Git
configuration changed. A receipt-store read initially used the wrong directory;
`rg --files` located `Identity/SqlRequestReceiptStore.cs`, which was then read.
No quota error occurred during this resumed pass.

Build/typecheck/lint: **NOT RUN by reviewer**. Tests, fixtures/mock/demo records,
runtime/API/browser/SQL/migrations/provider calls: **NOT RUN / NOT CREATED**.
No test-count, reproduction, runtime success, CI result or release claim is inferred.
No agents started; no product code, other reports, grants or Git state changed.

Instructions loaded: root AGENTS; `.agents/skills/nexora-engineering/SKILL.md`;
implementation profile; TL README/core rules; `.ai/routing.json` and verification;
architecture ownership/module/transaction rules and modular-monolith/boundary
skills; backend authorization/background/cache/EF/migration/security/testing rules
and authentication/cache/data-access/idempotency/input-validation/REST/testing
skills; frontend accessibility/browser-security/design-system/forms/testing rules
and accessibility/component/security/testing skills; privacy/security identity,
data/secrets/threat rules and secrets/security-review/threat-model skills;
database migration/concurrency skills; owner-isolation control README/rules/review
skill. Test guidance is subordinate to the explicit current code-only restriction.

Raw-byte SHA256 snapshot of assigned executable files, read2026-10-24 around17:20
local (+07:00). Relevant edits invalidate this bounded review for changed paths.

| File | SHA256 |
|---|---|
| src/Nexora.Application/Learning/LearningPlanContracts.cs | `258B1E1414381104225B80DB9FF0DA572B07048CB192DE7A9C92FED0DF898826` |
| src/Nexora.Api/Features/Learning/LearningPlanEndpoints.cs | `6B3257E10F9FEBAE3D6F739E296C8307725651EFE168C716C0DFA8901B2FECEB` |
| src/Nexora.Infrastructure/Learning/SqlLearningPlanService.cs | `49A6DA159E9E3397C529965DDBB4919402AE29C7AE58B34FA1DFA38BBAF109B9` |
| src/Nexora.Infrastructure/Learning/LearningPlanCursor.cs | `62FBCDF52689FB2F5584E311B5AD781E56D99925B4DFC28AA581726B1E786A61` |
| web/Nexora.Web/src/LearningPlanApi.ts | `6964913CF797D9A3AE8816D2F95BA5A811407743C3EE946F1BDD2A308E19312A` |
| web/Nexora.Web/src/LearningPlanScreen.tsx | `639F89B529D72458C110F8CCA36101B0215462221449C2A2C2E12814ADB2DFCF` |
| database/migrations/20261024_0089_learning_plan_lifecycle.sql | `260C6541AC5BF117A5653F11AF59D651ADB7F8DF708A38DEF78DF4C41A72B68E` |
| web/Nexora.Web/src/ToolboxMoreOffline.ts | `9B3F48E07F9631DA2D810368A8397EB289F5B19B79236FCB59E98B0E6E757A62` |
| web/Nexora.Web/src/ToolboxMoreOfflineOptions.tsx | `DE371AD62BE05CD32E322C28057B11CD5097D555A8339D1CE79F832FA7ED22B7` |
| src/Nexora.Infrastructure/DeveloperTools/LocalToolboxService.MoreOffline.cs | `8978A8A7F9D452DA5CF31C6AD3BB454BA291FA358D7C5187EA6F875206F07C03` |
| database/migrations/20261024_0091_toolbox_more_offline.sql | `4130ED93F8214D874511FBE20F93948C52B95560EFF13DE08B015E432DD26FF7` |

## Final root integration review — 2026-10-24 17:35 +07:00

User explicitly notified completed App/catalog/policy/manifest wiring and an
author build pass, and requested this bounded follow-up. HEAD/branch remain
`14c76bcb6960e4ce53369dce388a8480b1c0299a` / `impl/m01-s00-scaffold`, dirty shared
tree. Authorization, requirement/action/goal bindings and previously loaded rules
above continue to apply. Startup files and current scope were refreshed. Root
integration is now inspected, superseding its earlier NOT YET REVIEWED status.
The author's reported build is not an independently executed check in this review.

**Disposition: CHANGES REQUESTED for R24-PT-01 (P2 UX). No additional confirmed
security, payload-egress, Plan source-navigation or manifest defect was found in
the selected integration paths.** No product code was edited. Readiness stays false.

### R24-PT-01 — Leaving diff/color silently discards private workbench drafts

- Severity Medium / priority P2; confidence High; status OPEN.
- Location: `web/Nexora.Web/src/App.tsx:5411`, `DeveloperToolsScreen`, specifically
  second-input state at5415, JSX option/draft callbacks5578–5588 and Clear at5589.
  The entire component5411–5599 registers neither `registerDirtyLeaveGuard` nor
  a draft-sensitive `beforeunload` handler.
- Navigation evidence: Shell `navigate()` at7036–7052 consults only
  `attemptDirtyLeave()` before committing navigation. `navigationGuard.ts:8`
  consults only explicitly registered guards; it does not inspect form fields.
  Browser Back uses the same guard registry at7060–7072. Other forms' guards are
  component-local; they do not protect this workbench.
- Contract: FX32-S02 Exit/return in
  `docs/ux-ui/modules/32-developer-toolbox.md` explicitly gives dirty form guard
  precedence; its common UX-05 reference defines Keep editing/Discard semantics.
  The offline worker handoff also requires inclusion of the second input in the
  integrating screen's dirty/reset flow. Relevant goals: NXG-SYS-13,
  NXG-FX32-G03; memory-only behavior must remain intact.

Source-derived reproduction, **not executed**: open Toolbox, select Text diff,
enter content in After (Before may remain empty), then click Home/another module
or use browser Back. No workbench guard can intercept; navigation unmounts the
component and loses After and all other draft state. A color value/options draft
has the same path. Reload/close likewise has no draft-sensitive unload warning.
This is a concrete loss-of-input issue, not a claim of cross-owner disclosure.
The generic workbench omission predates this increment, but the newly integrated
second input/color controls inherit it. The explicit Clear confirmation at5589
protects only Clear, not these exit paths.

Requested root fix: register a component-scoped leave guard covering either input
and meaningful option changes, with Keep editing and explicit Discard continuation;
add the matching beforeunload protection. Handle this pure tool as unsaved local
work, without inventing server Save/history. Preserve security cleanup on known
authority loss and owner change; never persist the draft to fix navigation. On
confirmed discard, abort/invalidate any pending run before proceeding. Keep the
existing explicit Clear confirmation. Re-review the focused source after the fix.

Owner acceptance, still NOT RUN: After-only draft and color-option draft survive
Keep editing; explicit Discard leaves/clears; Back/reload are protected; empty
default workbench does not warn; authority revocation clears private values;
no local/session storage or input-bearing network request is introduced.

### Integration paths inspected without another confirmed defect

- `App.tsx:1500`, Plan `onOpenSource`: Course maps only to native learningCourses;
  typed Goal/Task maps to existing resource route, using ID rather than title/URL.
  It invokes normal `navigate`, with no dirty-guard bypass or external navigation.
  `pathForLocation`391–394 encodes IDs; native detail parse347–351 validates UUIDs.
  Source Plan screen still refreshes source availability before this callback.
- `App.tsx:1466`, resource destination is remounted by owner/context/type/ID;
  `ResourceScreen`3080/3105 fetches Task/Goal through their real API clients,
  clears resource on errors, and ignores cancelled reads. Course destination1501
  uses its owner/context/ID key. Its entry `load` checks current Course capability
  before detail/milestone reads with abort/generation checks. Inspection was only
  enough to establish this destination boundary, not a review of Course mutations.
- `App.tsx:5522–5528` routes diff/color into the browser helper with the exact
  empty `runDeveloperTool(selectedCode, '', {})` callback and AbortSignal.
  `api.ts:1954`, `runDeveloperTool`, serializes only those arguments. Actual
  before/after/options stay in the browser helper; only its calculated result is
  displayed, not the empty server authority response.
- `App.tsx:5435/5454/5575` uses correct defaults on selection/catalog fallback,
  resets both inputs/output/copy, and invalidates epochs on tool changes.
  Controlled second-input/options UI is mounted5578–5581; primary labels/limits
  at5588 distinguish Before128 KiB and color256 characters. Input/options edits
  clear old output/error/copy state. Recoverable helper failures retain inputs.
- `App.tsx:5581`, Cancel aborts, advances epoch, clears flight/busy/output/error/copy
  and announces cancellation while preserving drafts. Run checks epoch and abort
  before publishing; stale finally cannot reset a newer run. Unmount5420 aborts
  and invalidates. Catalog/run known authority denial5438–5464/5530–5535 clears
  both inputs/options/output/tools, while ordinary helper422 remains recoverable.
- `App.tsx:5552`, explicit Clear removes both inputs and restores defaults;
  output5592–5593 remains escaped React pre text and Copy is user initiated.
  These controls do not remedy the separate navigation finding above.
- `LocalToolboxService.cs:41–42/58/89`, catalog has Browser mode and exact action
  keys for the two tools; Run's current SQL action check precedes ExecutePure's
  fixed RunMoreOffline dispatch. The partial still rejects input/options payloads.
- **CORRECTED by R24-PT-02 below:** the earlier snapshot placed
  `toolbox.text_diff.run` and `toolbox.color.run` in historical `ApprovedLocalActions`,
  not the effective shipped SELF set. My original assertion of effective
  eligibility was wrong. The later policy correction and its exact evidence are
  independently reviewed in the permission addendum below. Plan gating and
  network exclusion remain required.
- `M01MigrationManifest.cs:101/103` includes exact0089 and0091 names in sequence
  0088/0089/0090/0091 after0087. Both SQL files exist and their hashes still match
  the reviewed worker snapshots. The Local project's existing migration wildcard
  copies SQL files; no new embedding convention is needed. No migration ran here.
- `Program.cs:312` still explicitly constructs `LearningPlanReadiness(false)`.
  This review gives no permission to change it or promote Plan permissions.

### Final evidence and remaining gates

Only read-only source/Git/hash inspection and this report update were performed.
No build, test, fixture, runtime/browser/API/SQL/provider execution, agents,
commit/push or product edit. A PowerShell wildcard directory search initially
failed and was replaced with `rg ... src/Nexora.Infrastructure/Access -g '*.cs'`;
no application check was attempted by that search. No quota failure occurred.

The root integration review gate is now completed with the single OPEN finding
above; the completed worker-file observations remain unchanged. Owner SQL and
functional/browser acceptance, final-head CI, activation and whole-R1 sign-offs
remain pending exactly as described earlier. Root owns R24-PT-01 remediation;
the reviewer owns focused re-review, not implementation.

Integration hashes read twice during this pass and unchanged:

| File | SHA256 |
|---|---|
| web/Nexora.Web/src/App.tsx | `61F1CCE88BD930263A65EC944C168047C074FF75F89DAC4822943AD50470E5A4` |
| src/Nexora.Infrastructure/DeveloperTools/LocalToolboxService.cs | `E431A676B2F51292DE44840A1B350DC669D8BEE6A588CD985CFB5612943413B1` |
| src/Nexora.Domain/Access/ActionGrantPolicy.cs | `6620192BB9DC6DEE5DC130B94D5428FB6CE737404FBA59852054FFB832A7E79B` |
| src/Nexora.Infrastructure/Local/M01MigrationManifest.cs | `F4E736875E5BCC1ED586A58733E5A679968B2BC69CAECA0EB48CBFCDFE083F18` |
| src/Nexora.Api/Program.cs | `03F2E1F311205595E4C18684A0DE37CFED00608D9BFE26BE7C554EE04A8848EC` |

## Focused remediation re-review — 2026-10-24 17:41 +07:00

User notified guarded switching/navigation, common discard dialog, beforeunload,
atomic busy guard, option/pattern cleanup and truthful processing labels, and
requested review of latest App bytes with product edits frozen. Same HEAD14c76bcb,
branch, authorization and no-tests/runtime constraints apply. Only this report
was edited. This addendum supersedes the OPEN/CHANGES REQUESTED disposition for
R24-PT-01 above; earlier evidence remains historical.

**R24-PT-01: REMEDIATED_IN_SOURCE — focused independent static review closed.
No remaining confirmed finding in the assigned Plan/Toolbox integration scope.**
Functional acceptance remains NOT RUN; this is not activation/release approval.

Source evidence in `web/Nexora.Web/src/App.tsx`:

- `hasWorkbenchDraft`5438–5440 includes primary input, second input, pattern,
  output, ordinary control changes and extra options versus selected-tool defaults.
  After-only input and changed color background/palette options therefore enter
  the guard. Empty default diff/color workbenches do not enter it.
- Registered guard5441–5445 checks the synchronous `toolFlight.current` first,
  blocking an active run even before a busy-state render. Otherwise a draft
  captures the normal Shell navigation continuation in `leaveTool`; a clean
  workbench allows navigation. Effect cleanup unregisters the guard on unmount.
- beforeunload5446–5453 checks both draft and flight, warns appropriately, and
  removes its listener during cleanup. It does not persist either input.
- `selectTool`5577–5585 refuses active-flight switches and prompts for existing
  drafts. Confirmed switch aborts/advances epoch, resets both inputs, pattern,
  output/error/copy, selected-tool options and ordinary controls. Merely selecting
  another tool no longer destroys the current draft before confirmation.
- `ResourceFormDialog`5625–5629 supplies Keep editing and explicit Discard.
  Keep editing/Cancel clears only the pending continuation. Discard captures that
  continuation, closes the modal, clears the workbench (which aborts/invalidates
  async work), then invokes the captured continuation. For Shell navigation this
  is the already-authorized `commitNavigation` continuation, so it does not
  re-enter the still-rendered dirty guard and reopen the same prompt. For tool
  selection it executes the captured target switch after clearing.
- Shared `ResourceFormDialog.tsx` uses its existing named portal, isolation and
  focus handling. This leave dialog passes `dirty=false`, because it is already
  the explicit discard confirmation; a second nested dirty-discard prompt is not
  introduced. Buttons and modal closing are disabled while busy. No claim is
  made that focus/keyboard behavior was exercised in a browser.
- Known authority loss5455–5459 clears private data and `leaveTool`; security
  cleanup takes precedence over preserving drafts or an obsolete continuation.
  Existing abort/epoch checks still prevent old runs repopulating output.
- Processing label5604 now states browser-only/no content upload for Browser tools,
  and temporary server processing/no content storage for other tools, replacing
  the raw action-key label. Diff/color still use the empty-payload callback at5541.
  No route, permission, persistence or provider expansion was introduced here.

Plan callback1500 remains the typed native mapping already reviewed. Program312
still has `LearningPlanReadiness(false)`. Catalog, ActionGrantPolicy, migration
manifest and Program hashes match the preceding integration snapshot.

Evidence: read-only source/hash/revision inspection; no builds, tests, fixtures,
runtime/browser/API/SQL calls, agents, product edits or Git mutation. App's first
read had hash `DD87F0109F7C754E90E22B78CEE9ECC70133E8346A86D84DF3A6A7E042BEE24C`;
a subsequent hash differed, so the affected guard/switch/dialog/labels and Plan
callback were read again from the latest bytes. Final inspected App hash is
`04FEE05592A2D8435E2A6D6F3385481ADA7FBD8E2655FF58CA49009E1232E716`.
The relevant remediation code matched across those reads. This sign-off binds to
the final hash, not the first read or an unqualified frozen-tree assertion.

Supporting source hashes:

| File | SHA256 |
|---|---|
| web/Nexora.Web/src/ResourceFormDialog.tsx | `B0F6DD3530CEBFE121C7EF480B079692B394007EBDB6FE86E671E738CE5C7883` |
| web/Nexora.Web/src/navigationGuard.ts | `28B2D3A375900230EA322CE47F1956407E92133935ED742EE1E9806A9ED2D651` |

Remaining sign-offs are owner functional/browser/SQL verification, final-source
CI and any separately authorized activation/release decision. Original broader
R1 capability gaps remain visible; this focused closure does not remove scope.

### Superseding R24-PT-01 confirmation — 2026-10-24 17:43 +07:00

At the user's explicit re-review request, reread current App guard, authority
cleanup, switch, modal and processing-label evidence. HEAD remains14c76bcb.
**Superseding disposition: REMEDIATED_IN_SOURCE / CLOSED for R24-PT-01;
no new confirmed defect in this focused correction.** This confirms the preceding
closure and supersedes any earlier OPEN wording, without claiming runtime acceptance.

Specifically,5438–5440 includes both inputs/pattern/output/options;5442 blocks
navigation using the synchronous busy ref;5446–5453 protects beforeunload;
5577–5585 guards controlled selection and resets pattern/state/defaults only on
confirmed continuation;5625–5628 offers Stay versus explicit Drop/continue and
clears/aborts before continuing. After `clearToolAuthority`5455–5459, tools are
empty, flight is false and pending modal is cleared, so `tools.length > 0` makes
draft protection false even if an ordinary option retains a nondefault value.
Processing label5604 distinguishes Browser/server and contains no raw action key.
Program312 still keeps Plan readiness false.

Latest App SHA256, identical before and after this source read:
`04FEE05592A2D8435E2A6D6F3385481ADA7FBD8E2655FF58CA49009E1232E716`.
Only the report was updated; no product edits, tests, builds, fixtures or runtime.
One report patch initially failed context matching and made no change; corrected
patch appended this confirmation. Remaining owner QA/CI/activation gates stand.

## Toolbox permission correction — 2026-10-24 17:48 +07:00

User requested independent review of the Toolbox-only permission delta and
correction of the prior report. Current source is branch `impl/m01-s00-scaffold`,
HEAD **`7dc98b42eadcbdab6053bb9422363403aec9aa35`**, with the policy correction
still dirty. This supersedes the earlier14c source label for this pass. Skill and
Collection corrections were visible in the shared diff but are outside this
review; no independent sign-off on those modules is supplied here.

### R24-PT-02 — Historical inventory did not authorize shipped Toolbox actions

Priority P1, confidence High; historical defect **REMEDIATED_IN_SOURCE** in the
latest policy. My earlier integration claim at the policy bullet was incorrect
and is explicitly corrected above. Previous no-finding summaries do not certify
the historical policy's effective Toolbox eligibility.

Concrete source defect: the old `ActionGrantPolicy.ApprovedLocalActions` contained
the Toolbox catalog/run keys, but was only historical inventory. `CanGrantAllow`,
`IsAdminGrantable` and `IsApprovedForLocalAction` did not consult that set. Before
the fix the keys were absent from their effective sets. Consequently grant
validation rejected these otherwise shipped/approved actions, and
`SqlSelfCapability.EvaluateCore`105–108 rejected Admin SELF catalog/run access via
`IsAdminGrantable`, even with an existing SQL Allow. This affected the catalog
and all14 shipped tool runs, not only the two newest tools. This is an availability/
authorization-contract defect, not evidence of excess access or runtime reproduction.
No blanket claim is made that this pre-SQL check denied User/SuperAdmin SELF:
the observed eligibility precheck is specifically conditional on Admin role.

Source authority: current `docs/action-catalog/modules/32-toolbox.md` declares
catalog and these run actions Admin-grantable **when active**; the earlier
`docs/implementation/toolbox-extra-offline-contract.md` explicitly requires the
four extra keys in the effective Admin SELF allowlist. New diff/color handoff
requires the same exact-key integration. Bindings: FX32-S01/S02,
P06-TBX-008, NXG-SYS-03/14 and NXG-FX32-G03. Eligibility never grants execution
without the remaining current authority checks.

Remediation independently traced in latest `ActionGrantPolicy.cs`:

- `ApprovedShippedSelfActions`237–244 now contains exactly these15 Toolbox keys:
  `toolbox.catalog.read`, `toolbox.base64.run`, `toolbox.url_codec.run`,
  `toolbox.html_codec.run`, `toolbox.hash.run`, `toolbox.uuid.run`,
  `toolbox.password.run`, `toolbox.json.run`, `toolbox.regex.run`,
  `toolbox.xml.run`, `toolbox.datetime.run`, `toolbox.csv.run`, `toolbox.jwt.run`,
  `toolbox.text_diff.run`, `toolbox.color.run`.
- `CanGrantAllow`313/318 consumes the set at both implementation-eligibility and
  Admin-grantability gates. `IsApprovedForLocalAction`437 and
  `IsAdminGrantable`466 also consume it. This closes the historical/effective-set
  mismatch; merely finding a string in the file was not sufficient evidence.
- Read-only source extraction/comparison found15 distinct effective Toolbox
  keys matching `LocalToolboxService.Tools`14 action keys plus catalog.read,
  with exact equality. This was source inventory comparison, not execution of
  policy methods, a unit test, or runtime authorization proof.
- Explicit network HTTP/DNS blocked set182–183 and earlier denial in
  `CanGrantAllow` remain. No namespace wildcard is added. YAML/conversion/cron/
  Markdown/code-format/QR/URL-parse/header/certificate/history/output/support
  actions are absent from the effective Toolbox subset. Existing historical
  duplicate entries for older shipped tools do not confer separate authority.
- No `learning.plan.*` key is added; Program312 still constructs readiness false.
  The policy-only diff creates no grant or module enablement and changes no SQL
  EffectiveStatus. `SqlSelfCapability` still requires Resolved permission rows,
  live current session/owner/module/dependencies, and Admin explicit Allow with
  Deny precedence. A nonresolved row remains denied despite static eligibility.

**Bounded disposition:** R24-PT-02 remediated in source; no remaining confirmed
defect in this exact Toolbox permission correction. R24-PT-01 remains closed in
source. This is not whole-policy/module acceptance or permission activation.

Latest policy SHA256, read twice unchanged:
`FD1A7C642DC53F51AC38A2B2116C368BB425E5A551A9A65867169506EEF40A54`.
Reviewed commands were read-only Get-Content/rg/Get-FileHash/Get-Date and Git
branch/revision/status/diff/show, plus an in-memory source-key comparison. Only
this report was edited. No tests, builds, runtime/SQL/provider execution, fixtures,
agents, product edits, grants/status changes, commit or push. Owner functional
authorization/SQL acceptance and current-head CI remain pending, not passed.

### Final Toolbox effective-key disposition for publication preparation

Latest follow-up HEAD: `e9c93a55ceb0863800f7247b907b5afb9df86a04`. Policy bytes still
match reviewed SHA256
`FD1A7C642DC53F51AC38A2B2116C368BB425E5A551A9A65867169506EEF40A54`.
**Bounded independent source review complete: no confirmed blocker remains in
the exact15-key Toolbox eligibility correction. R24-PT-02 is closed in source.**
All prior claims that historical `ApprovedLocalActions` provided effective
Toolbox eligibility are superseded by the correction above. R24-PT-01 remains
closed in source. Program still explicitly retains Plan readiness false.

Root reports unit/integration compilation with0 warnings and frontend build pass;
these are attributed author build results, not independently executed tests or
runtime proof. This follow-up only reread report/hash/revision/readiness and
updated this report. No product edits, tests, builds, runtime, fixtures, commits
or pushes by reviewer. Functional SQL/browser acceptance and final-head CI remain
separate release gates; this disposition does not authorize merge or activation.
