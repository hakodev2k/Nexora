# FX18 uncertain-save recovery — code-only task brief

Accountable author: learning_completion; coordinator/reviewer: root (review pending).
Source: PR #4, `impl/m01-s00-scaffold`, base `75b6d9f4f0ac247680e22cc07bf26a4890071ef2` plus current shared working changes.
Authorization: latest user AGENTS execution amendment and coordinator's code-only continuation. No new tests, fixtures, browser/runtime execution or data creation. Earlier audit test authorization is superseded for this task.

## Bound contract and change

Inventory16 and canonical ledger retain FX18 IN_PROGRESS / NOT VERIFIED. Concrete AUTO-RESOLVABLE defect: TimeTrackingScreen allowed editing after an uncertain save; each edit replaced the idempotency key. A committed request with a lost response could therefore be followed by a second create. Form submissions also lacked a synchronous guard against rapid duplicate activation.

Exact actions: `time.timer.start`, `time.entry.create`, `time.entry.update`, SELF only; screens FX18-S01/FX18-S03. Sources: features/18-time-tracking.md BR001–003/005, AC001; action-catalog/modules/18-time.md; ux-ui/modules/18-time-tracking.md error/retry state and same-key retry rule; existing timeApi.ts DTO/header contracts and SqlTimeTrackingService.Mutate durable request-receipt behavior. Goals: NXG-FX18-G01/G03, NXG-SYS-05/13/14. No timer/source/business rules changed. No migration or new API.

Plan: retain an immutable command closure and original key/ETag across network/408/5xx/RequestInProgress; disable editing and ordinary close until definitive response or explicit informed abandonment; prevent duplicate synchronous submit; clear retained private command on current authorization denial. A confirmed command closes before reloading reads so a read failure cannot imply an uncertain write. Use existing ResourceFormDialog navigation/unload guard and existing visual classes. Rollback: revert this screen-only change; no persistent data/schema impact.

Loaded instructions: root AGENTS; nexora-engineering SKILL; implementation-agent profile; Technical Lead README/core; routing baseline and frontend rule/skill paths; verification; current DEC014/code-only amendment; docs/delivery/current scope; system/module18 goals and task/evidence template. No provider/Vault/Plan gate involved. No acceptance or release promotion.

## Verification responsibility

Only compile/typecheck may be run for implementation errors. Owner QA pending: lost-response retry must preserve exact body/key/ETag, duplicate submit must produce one in-flight command, known validation/conflict errors must permit correction, denial must clear private input, and close/navigation must require resolution or explicit abandonment. No new tests will be authored or run by this task. Whole-FX18 release remains NOT VERIFIED.

## Author handoff

FIXED in source: immutable submitted closure/body/key/ETag, synchronous submission latch, frozen-input uncertain state, same-command retry, two-step explicit informed abandonment, existing modal close/navigation/unload protection, and retained-command clearing on authorization denial. Confirmed write closes the form before the separate read refresh; failed refresh is not presented as an uncertain write. Only `web/Nexora.Web/src/TimeTrackingScreen.tsx` changed; no API/database/permission/central screen/style edits. No new business feature or module completion claim.

Executed in this task: `npx tsc -b --pretty false` from `web/Nexora.Web`, exit0; `git diff --check -- web/Nexora.Web/src/TimeTrackingScreen.tsx docs/implementation/pr4-completion/workers/time-recovery-20261025-code-only.md`, exit0. These detect implementation/format errors only. No unit/integration/E2E/browser/runtime checks or test-data creation performed. Source SHA256: `61BBC679ACFD8FCDC28AAEDDC956A40E54E5C1404967EF643B7B8C63E21D404D` (working-tree bytes on base SHA above). Root owns final commit/evidence binding.

Pending: owner functional QA and independent source review; all pre-existing FX18 source-link/report/history/lifecycle completeness gaps remain outside this bounded remediation. Release remains NOT VERIFIED. Root owns canonical ledger/report19 integration and GitHub delivery; this worker has not committed or pushed.
