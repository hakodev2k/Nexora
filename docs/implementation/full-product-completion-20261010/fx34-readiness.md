# FX34 — source readiness audit

Requirements revision: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Product source reviewed: local `40f5345406ecc716281550909a7b6b78077fca1c` plus three baseline fixes published as `f8001aade70d5ee6aa0beebe38b6e67295544ac4`; fixes do not change FX34. Reviewer `/root/baseline_review` inspected local source, not the remote f8001aa object. Root checked source references. No functional/security execution for FX34; current continuation is code-only.

Source contracts: `docs/features/34-automation-and-scheduler.md`, `docs/action-catalog/modules/34-automation.md`, `docs/ux-ui/modules/34-automation.md`, `docs/requirements/phases/phase-06-developer-and-automation.md`, `docs/design-database/09-automation-monitoring.md`. Goals NXG-FX34-G01…G03 and NXG-SYS-02/03/05/06/08/11/14/16.

## Exact action assessment

All rows below have implementation status **MISSING**, simulated verification **NotRun**, security acceptance **NotRun**. Current handler/API/store/UI/test evidence: none found. Referenced database entities and screen IDs are specification bindings, not installed tables or working screens. Engineering resumed; external effects remain **PROVIDER_INACTIVE** and G3/G4 unapproved. Functional acceptance requires the source AC and common owner/grant/lifecycle/concurrency contracts, not only the listed phase IDs.

| Action key | Requirement / acceptance binding | Expected data / UX | Contract readiness |
| --- | --- | --- | --- |
| automation.definition.read | P06-AUT-001/006; common query gates | Definition; FX34-S01/S03 | Safe metadata contract can proceed; exact DTO/query/errors pending |
| automation.definition.create | P06-AUT-001/002 | Definition/DefinitionVersion; FX34-S01/S02 | Draft metadata can proceed; executable body D03 |
| automation.definition.save | P06-AUT-002/006/013 | Immutable DefinitionVersion; FX34-S02 | Version/transaction contract pending; body D03 |
| automation.definition.validate | P06-AUT-001/002/014 | Version/references; FX34-S02 | D03 registry/topology/mappings |
| automation.definition.enable | P06-AUT-002/013/014 | Definition/Schedule; FX34-S01 | D03; no implicit external authority |
| automation.definition.disable | P06-AUT-006/013; BR004 | Definition/Run/Schedule; FX34-S01 | Queued→Skipped, stop before next running effect already defined; transaction/lease contract pending |
| automation.definition.trash | P06-AUT-006/013; shared Trash | Definition/TrashBatch; FX34-S01 | Dependency ordering/queued-run disposition contract pending |
| automation.definition.restore | P06-AUT-006/013; shared Trash | Definition/TrashBatch; FX34-S01 | Restore activation/revalidation contract pending; no automatic enable |
| automation.definition.purge | P06-AUT-006/013; shared Trash | Definition/versions/schedules; FX34-S01 | Dependency ordering/impact contract pending; independent D08 run/audit retention |
| automation.definition.history | P06-AUT-006/009 | DefinitionVersion; FX34-S03 | Safe projection/pagination contract can proceed |
| automation.run.read | P06-AUT-007/009 | Run/StepRun; FX34-S04 | Safe states/projection/25-row paging contract can proceed; retention D08 |
| automation.run.start | P06-AUT-004/005/007/014; AC001/002 | Run/StepRun; FX34-S05 | D03; immutable version, authority/lease/idempotency contract pending |
| automation.run.cancel | P06-AUT-006/007; BR005 | Run/StepRun; FX34-S05 | Cancellation boundary/lease contract pending; never claim undo |
| automation.run.retry_step | P06-AUT-006/008/010; AC002/004 | Run/StepRun/outbox; FX34-S05 | D03 action-specific retry/reconcile; never replay ambiguous mutation blindly |
| automation.run.dry_run | P06-AUT-011; AC003 | Run/StepRun; FX34-S05 | D03 and verified simulation implementation per action |
| automation.schedule.update | P06-AUT-003/004/008; BR002 | Schedule; FX34-S02 | Engineering cron/interval/preview/recovery contract pending; trigger keys D03 |
| automation.definition.import | P06-AUT-012; BR006 | Draft DefinitionVersion; FX34-S02 | D03 versioned schema; rebind refs, no secrets |
| automation.definition.export | P06-AUT-012; BR006 | DefinitionVersion; FX34-S02 | D03 schema; no secret values or ambient authority |
| automation.step.dispatch | P06-AUT-005/008/010/014; AC001…004 | StepRun/outbox; trusted SYSTEM worker | D03; current authority check and provider-operation G3 for external effect |
| automation.support.read | P06-AUT-009; Support common contract | Explicit safe metadata; FX05-S03/S05 | Field allowlist and module-scoped expiring grant contract pending |

Five product screens FX34-S01…S05 are **MISSING**; UI loading/empty/denied/conflict/responsive acceptance NotRun. Shared Support screens exist elsewhere but do not implement the FX34 projection.

## Existing source is denial plumbing

`src/Nexora.Domain/Modules/ModulePolicy.cs` contains paused-module denial; `src/Nexora.Domain/Access/ActionGrantPolicy.cs` contains `automation.` denial. Catalog seeds `20260910_0002_r1_catalog_and_productivity.sql` and `20260910_0018_local_runtime_catalog_gate.sql`, shell labels and paused-enable checks do not implement any of the20 actions. No Automation handler/store/schema migration/worker/registry/scheduler was found. Do not remove default-deny controls to make coverage appear implemented.

## Delegated versus unresolved policy

Already defined by BR002/003: one run/definition by default, busy queue latest, missed schedule skip, explicit catch-up, nonexistent DST time skip/repeated occurrence once; transient at most3 retries/backoff, permission/validation failures no retry. These are not new PO decisions to request.

D03 remains open for executable trigger/action keys, mappings and topology. Acyclic≤20-step linear/conditional design is a proposal. Timeout30s is explicitly proposed, not an approved default. Exact cron dialect, jitter, leases and transition mechanics require a bounded engineering contract. D08 retention is independent of business Trash. Provider projections/auth/budgets/activation remain D05/D06/D07/D09 plus provider-specific G3.

Next safe slice: finish metadata/version-history API/DB/UX/security contract without choosing executable D03 policy; then implement while preserving module default-disabled state. Existing schema numbers0045 and retained applied0046 remain immutable; no new migration number reserved here. Independent migration/authorization review is required before publication. Runtime acceptance remains human QA under current amendment.
