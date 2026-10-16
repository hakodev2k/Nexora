# Productivity contract-alignment slice

Status: `Partial / runtime Not run` on PR #4. CI run `34508111597` / `149`
passed the existing boundary/build/unit/frontend checks; SQL/API/browser runtime
remains `Not run`. This note records the bounded
Projects, Tasks and Calendar changes; it does not claim FX11–FX13 acceptance.

## Source binding

| Goal | Source requirements / AC | Action/API contract | DB | Code/UI | Evidence |
|---|---|---|---|---|---|
| `NXG-FX11-G01..G03` | `docs/features/11-projects.md`, `FX-11-BR-001..006`, `FX-11-AC-001..004` | `projects.project.*`; `/api/v1/projects`, `/api/v1/projects/{id}/transition` | `productivity.Project`, `ProjectHistory`; migration `20260910_0019_productivity_contract_alignment.sql` widens the legacy `[Name]` title mapping and description storage | `SqlProductivityService`, `ProductivityEndpoints`, `App.tsx` | Local API/Bootstrap/frontend builds and CI run `149` pass; SQL/API/browser `Not run` |
| `NXG-FX12-G01..G03` | `docs/features/12-tasks.md`, `FX-12-BR-001..006`, `FX-12-AC-001..004` | `tasks.task.*`; `/api/v1/tasks`, `/api/v1/tasks/{id}/transition` | `productivity.Task`, `TaskHistory`; `Task.IsOverdue` is a response projection; migration `20260910_0020_task_calendar_projection.sql` adds the projection key | `SqlProductivityService`, `ProductivityEndpoints`, `App.tsx` | Local API/Bootstrap/frontend builds and CI run `149` pass; SQL lifecycle/isolation `Not run` |
| `NXG-FX13-G01..G03` | `docs/features/13-calendar.md`, `FX-13-BR-001..006`, `FX-13-AC-001..004` | `calendar.event.*`; `/api/v1/calendar/events*` | `calendar.Event`; migration `20260910_0020_task_calendar_projection.sql` adds owner/task unique projection | `SqlProductivityService`, `ProductivityEndpoints`, `App.tsx` | Local API/Bootstrap/frontend builds and CI run `149` pass; DST/ICS/SQL/browser `Not run` |

## Implemented behavior in this bounded slice

- Project API keeps the existing `name` wire field as an explicitly documented
  compatibility alias for the product `Title` concept. New writes enforce
  Title 1–200, non-blank Description up to 20,000 characters, required
  Start/End and `End > Start`; Project list ordering is Title A–Z.
- Completed/Skipped Projects reject ordinary updates and require an explicit
  confirmation on terminal transition. Completing a Project with all Tasks
  already terminal still requires that owner confirmation; unfinished Tasks
  still require a reason for Complete. Task time-bound changes return a
  warning and do not mutate until the owner confirms.
- Task writes enforce Title 1–200, start state, immutable ProjectId and active
  Project checks. Out-of-Project time windows return a confirmation warning;
  the response exposes a server-computed `isOverdue` projection.
- Task create/update/delete synchronizes one Calendar projection by `(OwnerId,
  TaskId)` in the same SQL transaction when Calendar read capability is
  available. Calendar API update/transition/cancel rejects Task projections;
  Task remains the authority. The React Calendar has Day (default), Week,
  Month and Agenda view selectors and disables direct mutation for projections.
- Manual Calendar Events require Title, Description, Start and End. Completed
  and Canceled events remain read-only; delete remains Cancel.

## Explicitly still gated

Task version restore, Project/Task warning acceptance API evidence, full
Calendar all-day local-date storage, DST boundary verification, ICS
import/export, reminders, Project/Task advanced history restore and complete
aggregate UI behavior remain `Partial` or `Not run`. No test or runtime claim
is inferred from the build.

