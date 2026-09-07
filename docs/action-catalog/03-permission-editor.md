# SuperAdmin permission editor — reviewable behavior

## Scope and screens

FX02-S01/S02 metadata may be visible to Admin with access.user.read. Only SuperAdmin edits roles, User module grants and Admin module/action grants. FX02-S03/S04/S05 are not ordinary Admin mutations. FX03 module policy/settings is likewise SuperAdmin; diagnostics metadata uses explicit Admin read.

Ordinary User target: Modules tab only for entitlement configuration, no per-action checkbox. Admin target: Modules plus Actions. Read-only “effective rights” summary can explain an Admin's own access but cannot self-grant. No User-content tab or impersonation button.

## Matrix row

| Column | Meaning |
| --- | --- |
| Module / feature group | Logical domain label + actual installed module binding; dependency/system state |
| Action label + stable key | E.g. “Hoàn thành Task” / tasks.task.complete; searchable by either |
| Context | Own data / operational metadata / support-safe read; never ambiguous “all data” |
| Explicit effect | Not granted / Allow / Deny; not unchecked=inherit |
| Effective result | Allowed / Denied / Module off / Blocked decision / Missing prerequisite / Wrong context |
| Impact | Risk label + source scope/lifecycle + affected screens |
| Dependencies | Exact static keys and dynamic source checks; no automatic grant |

Filter module/context/risk/effect/changed-only; keyboard-labelled cells; default no mass toggle across hidden results. “Select visible actions” previews explicit named keys, count and revision; new future actions stay denied. Non-grantable actions are explanatory rows without checkbox. Blocked actions cannot be activated even by SuperAdmin.

## Example to review

| Target | Configuration | Effective behavior |
| --- | --- | --- |
| User A | Tasks module enabled | Owner Tasks baseline; no user action-level setup |
| Admin B | Tasks enabled; tasks.task.read Allow; tasks.task.update Allow; tasks.task.complete Deny | Reads/edits allowed own Task metadata, cannot complete by drag, menu, Save status injection or restore historical completed version |
| Admin B | tasks.support.read Allow only | Still cannot read another User; also needs support.session.open, active one-module consent and safe projection |
| Admin C | monitoring.job.read Allow; monitoring.job.retry Not granted | Views redacted job metadata; Retry disabled; calling API directly denied |
| Admin D | toolbox.json.run Allow; toolbox.network.http Deny | JSON local tool available; HTTP test unavailable; unrelated algorithm use outside Nexora not controlled |

## Save flow

Edit draft → Review diff (before/after, target, concrete keys, effective access, missing dependencies, risk, affected jobs/screens) → Confirm → server reload actor/target/revision → validate SuperAdmin and gates → atomic grant/audit/invalidation commit → show new revision and effective results. Cancel discards draft. Stale revision returns Conflict with changed rows; user reviews again, no blind retry. Last active SuperAdmin demote/disable always blocked.

Dependency examples: grant tasks.task.restore_version does not add task.history/update/transition; choosing it shows incomplete workflow warning. SuperAdmin may leave partial action set for constrained workflows, but cannot label unusable action Effective Allowed when current prerequisites missing. Resource-dependent requirements labelled “checked per resource”.

No secret values or business payload in diff/audit. Role/grant changes are security notifications on all three channels under common notification policy; actual delivery state may be delayed/failed, grant commit is not falsified as notification delivery success.
