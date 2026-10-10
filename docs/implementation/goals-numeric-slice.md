# FX16-S01/S02/S03/S04 — Goals numeric local slice

Status: implementation overlay for PR #4 under `DEC-20260909-014`. This is a
local SQL-backed slice, not a claim that the complete Goals module or Release 1
has been verified.

## Goal → requirement → acceptance → operation → code

| Outcome | Contract / acceptance | API operation | Implementation |
| --- | --- | --- | --- |
| Owner can create and browse measurable goals | `NXG-FX16-G01`; title/date validation; `FX-16-AC-002` rejects `target <= initial` | `listGoals`, `createGoal` | `IGoalService.List/Create`, `SqlGoalService`, migration `20260910_0014` |
| Owner can edit a non-terminal goal with concurrency protection | `NXG-FX16-G02`; `ETag`/`If-Match`, idempotency and owner isolation | `getGoal`, `updateGoal` | `IGoalService.Get/Update`, SQL rowversion transaction |
| Owner can record numeric progress without implicit completion | `FX-16-BR-001`, `FX-16-AC-001`; clamp only in projection, progress event is append-only | `recordGoalProgress` | `GoalTarget.CurrentValue`, `GoalProgress`, `RecordNumericProgress` |
| Owner controls explicit lifecycle | `FX-16-BR-003`; Draft→Active, Active→Completed/Abandoned, terminal→Active reopen | `transitionGoal` | `Transition`, action-specific grants and audit |

## API and data contract

- `GET /api/v1/goals` is owner-scoped and excludes `Deleted`; optional status,
  title/description query and bounded limit return progress and target count.
- `GET /api/v1/goals/{goalId}` returns the goal and its owner-scoped targets.
- `POST /api/v1/goals` creates `Draft`; an optional numeric target is created in
  the same transaction and requires `goals.target.create`.
- `PUT /api/v1/goals/{goalId}` updates title/description/date only while the
  goal is editable. The goal `ETag` is required.
- `POST /api/v1/goals/{goalId}/targets/{targetId}/progress` accepts a bounded
  decimal value and optional note. `If-Match` is the Goal `ETag`, so the whole
  detail refreshes after a progress write.
- `POST /api/v1/goals/{goalId}/transition` accepts only the explicit statuses
  supported by the state graph. It never infers completion from percentage.

`Goal`, `GoalTarget` and `GoalProgress` are SQL source-of-truth tables with
composite owner foreign keys, rowversion on mutable resources, value-kind
checks, position uniqueness and indexes for owner queries. Redis is not used as
authority.

## Security and safety boundary

- Every read and write scopes by `IdentityPrincipal.OwnerId`; request bodies
  cannot choose an owner or user.
- `FX16` module enablement and exact action grants are resolved from SQL for
  each request. Admin SELF access needs explicit `Allow`; Deny wins.
- Mutations use Serializable transactions, `ETag`/`If-Match`, durable request
  receipts and SQL audit events.
- No task state is written, no Finance amount is inferred, and no provider,
  worker, browser storage or external network call is used.
- Boolean/Tasks targets, target editing/removal/linking, archive/trash/history,
  reminders, habits, planner and support projections remain gated.

## Evidence status

This run follows the attached code-only instruction: no new tests, fixtures,
mock records or functional QA were added. The agent ran the repository static
checks recorded in the PR body and `docs/implementation/local-e2e-status.md`.
The .NET SDK, SQL Server runtime, migrations, endpoint behavior and browser QA
were not available/run in this environment; the human owner must execute and
record those checks before treating this slice as verified.
