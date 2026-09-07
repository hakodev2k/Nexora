# Catalog verification and authorization acceptance

Docs-only checks are not runtime security testing. Implementation cannot start until PO approval; handler tests below become backlog acceptance, not claimed executed tests.

## Structural checks executed for this document set

Unique lowercase stable keys; ≤150 chars;40 FX coverage; kinds/contexts/risk mapping; explicit prerequisite keys exist; no static prerequisite cycle; all non-worker operations have UI/context entry;197 existing screen IDs bind operations; relative Markdown links resolve; only docs files changed. Counts in README/CSV/module tables must agree. No unknown module namespace silently maps installed entitlement.

## Required runtime scenarios

| ID | Scenario | Expected |
| --- | --- | --- |
| ACT-001 | User A requests User B resource/read count/search | No payload/existence/count leak |
| ACT-002 | Admin Self keeps User role but action Deny | Denied; no role-union bypass |
| ACT-003 | Missing Admin action row; new deployed action | Denied by default |
| ACT-004 | Allow action while system/user module disabled | Unavailable; no side effect |
| ACT-005 | Admin writes role/module/action grant despite forged SuperAdmin field | Denied/audited attempt; existing authority unchanged |
| ACT-006 | Last active SuperAdmin disable/demote races | Invariant prevents losing all active SuperAdmins |
| ACT-007 | Task update sends Completed status without complete grant | Whole command denied; history/calendar unchanged |
| ACT-008 | Backward Task drag or keyboard move lacks reason | No transition; card restored; same error behavior |
| ACT-009 | Task restore version changes status/reminder without respective grants | Denied; immutable Project preserved |
| ACT-010 | Project terminal; Task create/edit/restore/timer resume | Denied; history still available by own permission |
| ACT-011 | Project complete unfinished Tasks | Confirm+reason; states unchanged; terminal readonly aggregate |
| ACT-012 | Document Save attempts Type/Editor/Folder/Parent change | Immutable validation rejection; Title remains editable Draft/Published per current FX-20 |
| ACT-013 | Document parent archive/unarchive and previously archived child | Matching cohort restored only; no unrelated child unarchive |
| ACT-014 | Calendar update targets Task projection | Denied; navigate source Task action only |
| ACT-015 | Completed/Canceled personal Event edit/reopen/delete | Denied; no Trash/lifecycle invented |
| ACT-016 | Shared Project Task detail requests private reason/history/reminder config | Safe projection only; no owner DTO |
| ACT-017 | Restricted share link forwarded to different authenticated User | Unavailable; no recipient/resource enumeration |
| ACT-018 | Owner support consent absent/revoked/expired, Admin has module grant | Denied; no ambient private read |
| ACT-019 | Support tries linked second module/file download/secret reveal | Denied even if own-module permission allows it |
| ACT-020 | Emergency reason/audit intent cannot commit | No target data; immediate three-channel intents on successful entry |
| ACT-021 | Grant revoked after preflight before commit/worker effect | Current revision denies; no stale capability override |
| ACT-022 | Job retry unknown accepted external effect | No blind replay; explicit unknown outcome |
| ACT-023 | Global Search/Trash/Command wrapper allowed but source denied | Source remains inaccessible; no count/payload/action bypass |
| ACT-024 | Template/import injects OwnerId/identity/protected status | Reject/sanitize per contract before diff; no unauthorized effect |
| ACT-025 | ICS mixed recurring/invalid/duplicate UID/valid | Only eligible valid new Scheduled events; report skipped; no reminder imported |
| ACT-026 | ICS export source permission revoked before download | Artifact access denied; no standalone Project/Task export expansion |
| ACT-027 | File Clean preview then replace/scan status changes | Current version/source/scan gate rechecked; pinned reference preserved |
| ACT-028 | Focus conversion request repeated | Exactly one authorized Time Entry; work phase only |
| ACT-029 | Save for later repeated; mark News read through queue | One reference; actual News action checked; no duplicate state |
| ACT-030 | Read Later Archive command invented from stale UI | Unknown action denied; only remove reference, no source delete |
| ACT-031 | Grant a Blocked Q operation or System action | Editor/handler rejects; cannot approve by checkbox |
| ACT-032 | Ordinary User per-action grant payload | Unsupported; module entitlement only |
| ACT-033 | Toolbox JSON run allowed, HTTP denied | Local tool available; backend HTTP denied; no input persisted by default |
| ACT-034 | Provider down vs empty query | Degraded/failure/freshness displayed; not fabricated empty success |
| ACT-035 | Three-channel notifications with blocked browser permission | In-app+Email attempts plus Push unavailable reason; not global channel opt-out |
| ACT-036 | Purge parent with pinned version/dependency | Explicit dependency preview; guard prevents irreversible invalid references |
| ACT-037 | Catalog mismatch/new key reused old semantic scope | Readiness failed; no silent grant inheritance |
| ACT-038 | Bulk contains authorized and unauthorized independent aggregates | Per-item outcome; no hidden unauthorized mutation; aggregate changes atomic |
| ACT-039 | Denied source field hidden visually but present in payload/accessibility tree/cache | Test fails; unauthorized data must not be returned/rendered |
| ACT-040 | Retry/restore supported route invoked without wrapper vs via wrapper | Same semantic target guards; no alternate entry privilege escalation |
| ACT-041 | Mutation allowed, source read Deny; command success/conflict response | Minimal authorized acknowledgement/error only; no full DTO, old values or history leak |
| ACT-042 | Read Task in terminal Project; purge individually trashed Task after Project ends | Read-only remains available; purge uses Trash/cohort/pin policy, never requires Project reopen; edit/restore still denied |

Per-row tests additionally cover source field dictionary, lifecycle graph, history/cohort, scope/Q gate and runtime actor. Not every action is mutation; read projections, LOCAL boundaries and workers need context-specific assertions rather than generic CRUD tests.
