# E34/E35 installed local behavior QA

Accountable worker templates/UI foundation; root owns environment mode, API startup,
SQL read-only operator additions and delivery. Existing user instruction and DEC014
automated-QA amendment authorize ordinary isolated synthetic local QA. Source base
a0215f7 plus coordinated working tree, PR4 branch impl/m01-s00-scaffold. No live
provider, Vault, credential, OAuth, public webhook, payment or production activation.

## Source and authority reconciliation

Read actual Automation/Integration service/API/client/native UI, migrations0047,
0049/0051/0052/0054 schema and subsequent contract chain. Current local contracts:

* `full-product-completion-20261010/fx34-definition-contract.md`, persistence,
  lifecycle and manual-run contracts: ordered trusted manual/tasks.task.create,
  typed literal inputs, immutable owner-private versions, own SQL Task effects only.
* `full-product-completion-20261011/fx34-schedule-configuration-evidence.md`,
  durable-scheduler, cron-interval and local-capacity/retry-policy evidence: real
  immutable configuration, SQL occurrence uniqueness, local worker/authority/leases.
* `full-product-completion-20261011/fx35-connection-configuration-evidence.md`:
  four installed inactive read/create/update/confirmed-disable actions only, n8n
  internal configuration manifest, no provider operations or credential binding.
* Feature/action/UX34/35 and goal modules34/35: NXG-FX34-G01/G02/G03,
  NXG-FX35-G01/G02/G03 and NXG-SYS03/05/06/08/11/13/14. Bind to FX34-S01–S05,
  P06-AUT001/002/003/004/005/006/007/009/013/014, AC001/003 where applicable;
  bounded FX35 connection configuration, not all21 catalog actions.

Historical readiness file claiming all20 missing and goal paused wording precede
installed local implementation/DEC014. Likewise old code-only/human-executes-QA
sentences are historical, superseded by current amendment. They do not erase real
provider/security boundaries. No undocumented capability is promoted.

Manual-run contract line3: "Local SQL Task effects only" and the operator flag
"defaults false, separately from module/current account/action grants". Durable
scheduler evidence line11 requires BOTH flags, schedule owner opt-in, enabled Active
definition and originating current session. Therefore bootstrap flags=false are
ordinary local configuration, AUTO-RESOLVABLE under existing authorized QA, not an
independent product SECURITY-GATED blocker. Root reviews and controls an owned
restart/mode change; tests never change these flags. FX35 provider activation stays
unapproved by its concrete manifest and D04/D05/D06/D07/D09/G3 contract.

Current code safety bounds `AutomationLocalExecutionLimits.cs`: default system1/
owner1 lanes, deadline120s, max4 attempts, retrybase5/max60 with bounded deterministic
jitter; later policy supersedes historical fixed5/20/60 wording. Worker timer1s;
SQL claims/leases and Task service own actual effects. Configuration-only and opted-in
execution are separate runs; no skipped positive test disguised as denial coverage.

## Authored cases, execution pending

`functional/local-configuration.spec.ts` contains four cases, each uses genuine
SuperAdmin preview/commit only for already Ready module policy/user grant. It
preserves registration flag, creates no roles/action grants, restores original
module policy/user dispositions in finally and asserts complete access snapshots.

| Mode | Scenario | Defined runtime/SQL proof | Status |
|---|---|---|---|
| Flags off | E34 configuration | Native Workflow create, valid Project picker, v1→v2 immutable history, owner/current pointer SQL, exact403 ModuleUnavailable Enable denial, no Tasks, native disabled UTC schedule preview/save + SQL, empty run history, Trash cohort→restore remains Disabled | NOT TESTED |
| Provider inactive | E35 connection | Native lexical bad-origin422 recovery, valid Disabled create, native edit/reload, confirmed disable invalidates revision; owner/Unconfigured/nullVault/nullLastTestAt/empty scopes SQL, registry no live operations, no browser requests to stored origins | NOT TESTED |
| LocalTask flag on | E34 manual execution | Native create/Enable/Run; eventual Succeeded pinnedv1 run/step; exactly one Task in real project/SQL, native run history, disable finally; no schedule/provider activation | NOT TESTED |
| Both local flags on | E34 finite scheduler | Native UTC Interval1min with Start now+35s and End Start+30s (one occurrence only), owner opt-in, Enable, exact due/OccurrenceQueued row projection, pinned Succeeded run and exactly one real Task, native history; finally Disable + schedule opt-out asserted | NOT TESTED |

Task inputs use isolated synthetic Project2028-01-01..10 and TaskJan03..04 browser
local, safely within bounds; scheduled due time is current explicit UTC and does
not infer arbitrary clock/trigger interpolation. No secrets/provider references.
The helper only modifies installed module policy/grant through real Admin contracts;
owned positive workflows always disable in finally before authority restoration.

Requested root-owned read-only SQL keys: AutomationDefinition→automation.Definition,
IntegrationConnection→automation.Connection; AutomationSchedule joins current
ScheduleVersion by owner/definition/current version and filters DefinitionId=@UUID.
It must use this real PK, not an invented Id column. Static legacy fixture command
guard from E25 remains untouched by this workstream.

Executed final strict compile only: exit0, with current test SHA256
`E8AEA70FE167544D794A765A8A740E0677FDF5D9D4AAD14975F6E14CAC197BF8`.
No runtime launched. Root independent mode review and exclusive writer GO pending.
Source/build cannot prove runtime acceptance, owner isolation, all retries, schedule
DST/restart/concurrency, provider integration or full feature completion. Those
unexecuted requirements remain explicit, not covered by these bounded four cases.

## 2026-10-29 separated environment modes

Worker automation_finish continues the same bounded scope on base a0215f7 plus
coordinated worktree. Root owns SQL read-only mappings, runtime identity and
exclusive browser scheduling. New owned runtime pointer is ignored/private; no
secrets or account storage are copied into this report. Documents holds the
browser writer at this checkpoint.

The four cases now have disjoint execution suites:

- `functional/local-configuration.spec.ts`: two default-off configuration cases.
- `automation-functional/local-execution.spec.ts`: two local Task/scheduler opt-in
  cases under `automation-functional.config.ts`, with separate JSON/JUnit reports.
- `functional/local-configuration-helpers.ts`: shared legitimate policy/grant
  setup and exact-state restoration, plus sanitized pre-mutation checkpoint
  containing only synthetic IDs and boolean dispositions.

No scenario assertion was removed or skipped to accommodate mode differences.
`start-isolated-browser.ps1` accepts explicit `-EnableLocalTaskExecution` and
`-EnableLocalScheduling`; both remain false by default. Scheduling without the
Task opt-in fails before any resource creation. The existing exact owned GUID
DB/SQL allowlist/process guards, environment restoration, private ACLs and provider
restrictions are unchanged. Root approved this bounded script ownership; independent
review remains pending before the opt-in environment is started.

Actual checks:

- An initial `tsc -p tsconfig.app.json` failed because that config does not exist;
  corrected to the real repository configuration, not recorded as a PASS.
- Explicit strict TypeScript compile of both specs/shared imports/config with
  ES2022/ESNext/Bundler/node types: exit0, rerun after checkpoint addition exit0.
- PowerShell parser for startup script: no errors.
- Scheduling-only startup invocation: expected fail-closed guard observed before
  resource creation; PASS for this guard only.
- Scoped git diff --check: exit0 (line-ending normalization warning only).
- Initial Playwright discovery lacked required environment variables and correctly
  refused E2E_BLOCKED. Rerun using verified owned runtime metadata: exit0,
  default suite exactly2 cases and opt-in suite exactly2 disjoint cases.

Discovery/compile/parser checks are not functional test executions. All four
runtime scenarios remain NOT TESTED at this checkpoint. No provider was contacted,
no new product feature activated, no human acceptance or Release Ready claimed.

## Reproduced existing Automation HTTP408 defect

Before any production fix, added `src/AutomationRecovery.test.tsx` with four
component regressions against the actual installed components. All4 executed and
failed: definition save, Enable command, Trash lifecycle command and Schedule save
all omit408 from uncertain-result classification, discarding the original pending
command instead of exposing exact retry. The tests preserve submitted arguments,
require identical body/key/ETag/source on retry and frozen editable fields where
applicable. These component mocks prove UI state handling only, not SQL/E2E effects.

Actual initial run: Vitest exit1,4failed/0passed,6.53s; retained
`web/Nexora.Web/artifacts/automation-recovery-initial.log` and
`automation-recovery-initial-junit.xml` (copied from the configured reporter output).
Root was notified; proposed bounded correction is to classify HTTP408 with other
uncertain outcomes in the four components. No source fix applied at this checkpoint;
no validation, permission or assertion changes proposed.

## Corrected HTTP408 handling and actual regression

Root authorized the four production classification changes after the failing
regressions. Each existing uncertain-result condition now includes408. No other
command construction, authority, payload validation, lifecycle or idempotency
behavior changed. The same four component cases pass, including exact command
argument equality across retry and frozen existing form intent.

Final typed test correction removed unsupported Testing Library `exact` option;
string accessible-name matching remains exact. Full source `tsc --noEmit` and
explicit strict compile for both browser suites/config then exit0. `npm run build`
exit0; main chunk1,189.12kB (gzip307.95kB) still produces the pre-existing chunk
size warning. Build is not counted as functional verification.

Final isolated-reporter regression:13cases PASS/0FAIL across4Automation,6shared
ResourceFormDialog and3modalIsolation cases,3.96s. There is an existing React act
warning in the shared dialog test; no test was skipped. A prior concurrent run
console also passed13, but another7case suite overwrote its shared XML before
copy. That contaminated copy is not used as13case evidence; unique ignored
config/reporter rerun supplied the authoritative current report.

Canonical sanitized evidence: `evidence/automation-recovery-20261029/` contains
original4FAIL JUnit, corrected13PASS JUnit and exact source SHA256 manifest.
Results belong to basea0215f7 plus those source hashes; coordinator must reconcile
them to delivered commit. The4cases are included in13, not additive to it.
Independent review is requested through root. Browser SQL user journeys remain
pending in their disjoint OFF/ON suites and are not inferred from component tests.
