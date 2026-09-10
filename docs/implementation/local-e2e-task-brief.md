# Full local E2E implementation task

Owner: implementation agent `/root`. Source branch: `impl/m01-s00-scaffold`.
Starting revision: `b17537263e2d478d8a9b14258891a3c1658534e1`.
Remediation implementation commit: `d2c7c5e00c05714b9d7a54a09a38b64f0e20e33d`.
Current productivity-contract alignment commit: `e42516e2360ad2afd7d8a0144412a15086770983`.
Delivery target: existing PR #4 to `main`; commit/push/update authorized; no merge.
Authorization: current PO prompt, recorded as DEC-20260909-014.

## Plan and gates

1. Reconcile current approval and action gates before post-M01 code.
2. Finish identity/access/module foundation with SQL persistence, bootstrap,
   transactional token consumption, sessions, audit/outbox and usable UI.
3. Continue platform, productivity, knowledge, finance/Vault, remaining domain
   and simulated provider slices in dependency order. Complete missing contracts
   before each slice; preserve exact feature lifecycle semantics.
4. Add SQL integration and endpoint evidence, UI validation/build checks and
   local migration/seed/bootstrap/run/verify workflows; keep PR status current.

Initial stories: M01-S00–S11, exact acceptance and operations in
`docs/delivery/milestone-01/01-stories.md` through `06-readiness-and-evidence.md`.
Goals: NXG-SYS-01–16 plus the selected phase/module goals, to be bound to exact
source AC/actions/tests in each slice's evidence before implementation.

Architecture: feature Minimal API → application use cases → domain policies →
SQL Infrastructure; Redis optional cache. Data changes require forward migration,
transaction/owner constraints and synthetic restore/rollback evidence. Provider
adapters use local simulation; real-provider execution disabled. Independent
security/migration review remains Pending until actually performed.

## Instructions loaded

- `AGENTS.md`, `.agents/skills/nexora-engineering/SKILL.md`
- `.ai/profiles/nexora-implementation-agent.md`, `.ai/routing.json`, `.ai/verification.md`
- `.ai/roles/technical-lead/README.md`, `rules/core-rules.md`,
  `skills/work-intake-and-decomposition.md`, `workflows/feature-delivery.md`,
  `templates/task-brief.md`, `checklists/definition-of-done.md` under that role
- Documentation entry points and current approval/scope/action/goal records.
  Specialist routes and exact slice contracts are loaded before affected code.

## Initial evidence and unresolved work

The branch is `impl/m01-s00-scaffold` and remains unmerged. Local tool discovery
found dotnet, Node/npm, Docker and sqlcmd executables; exact restore/frontend/SQL
runtime readiness remains pending. This brief does not claim GitHub Actions state.
The remaining R1 modules are deliberately unimplemented. No full-slice or R1
completion is claimed.

## PR #4 blocker-remediation continuation

Requested outcome: make the existing PR #4 branch merge-ready for the approved
local implementation surface without merge, production/provider execution, real
secrets/data, paid services or external destructive actions. Current PO authority
is `DEC-20260909-014`; the current execution amendment is code-only, so the
human owner owns functional QA, test-data design and runtime verification.

Affected review gates: M01-S03/S04/S05/S06/S07/S08/S09, M01-AC03–AC09,
`NXG-SYS-03`, `NXG-SYS-04`, `NXG-SYS-05`, `NXG-SYS-12`, `NXG-SYS-14`, and the
local R1 FX16/FX20/FX21/FX22/FX23/FX24/FX25/FX26/FX27/FX32 overlays.
The implementation work covers SQL MFA reset fail-closed behavior, response-wide
CSRF rotation capture, Toolbox idempotency, module catalog gating, Admin SELF
baseline authorization, SQL readiness, request-size/idempotency hardening and
stale M01 naming/comments.

The selected Nexora engineering skill and routed baseline/architecture/backend/
frontend/security/database/verification/owner-isolation rules were loaded from
`.agents/skills/nexora-engineering/SKILL.md`, `.ai/profiles/`, `.ai/roles/`
and `.ai/routing.json`. Exact restore-based CI verification passes on workflow
runs `34501160127` / `143` and `34501462229` / `144`; remaining gates are SQL runtime/migration
verification, the documented owner-run manual QA script, and independent review.
The local API/Bootstrap Release builds and frontend build have now run
successfully; the existing unit-check step and `verify.sh` also passed in CI
run `34508111597` / `149`. The local Bash invocation was blocked before
execution by the host's WSL access policy, and SQL/runtime/owner QA plus
independent review remain `Not run` or `Pending`.

## Productivity contract-alignment continuation

The current slice binds `NXG-FX11-G01..G03`, `NXG-FX12-G01..G03` and
`NXG-FX13-G01..G03` to the source requirements in
`docs/features/11-projects.md`, `docs/features/12-tasks.md` and
`docs/features/13-calendar.md`. It adds the forward migrations
`20260910_0019_productivity_contract_alignment.sql` and
`20260910_0020_task_calendar_projection.sql`, Project/Task contract validation
and confirmation gates, server-derived Overdue, a one-way Task → Calendar
projection with direct-edit denial, and the Calendar Day/Week/Month/Agenda
selectors. Documents creation now requires an explicit type/editor choice and
the UI exposes Grid (default) and Table views.

The complete trace is in `docs/implementation/r1-requirement-traceability-matrix.md`.
SQL migration/application, SQL API integration, health failure-mode, browser
and manual QA evidence remain `Not run`; no new tests, fixtures or runtime data
were added under the current code-only amendment.

