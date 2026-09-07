# Composition, protected fields và chống bypass

## Command ≠ button ≠ permission

Một form Save có thể yêu cầu nhiều action. Nhận generic payload không được grant toàn bộ writable columns. Handler computes semantic before/after diff after validation/normalization, kiểm tra đúng các actions bên dưới, rồi commit atomic. If missing one → deny whole aggregate command, no partial hidden mutation. Separate bulk aggregate semantics are declared below.

| Surface / diff | Canonical requirement | Invariant |
| --- | --- | --- |
| Task Title/Description/Priority/Tags/Start/End | tasks.task.update; organization tag provider for catalog changes | Project immutable; time warning confirmed; Calendar projection updated internally, no calendar.event.update grant required |
| Task Status | tasks.task.start / complete / skip / revert by exact graph | Revert reason; parent active; no Completed→Skipped direct; every transition versioned |
| Task rank / AC / reminder | tasks.task.reorder / criteria / set_reminder / remove_reminder | Drag across columns adds transition; tick-all does not complete; reminder revision invalidates old job |
| Task historical restore | tasks.task.restore_version + task.history + every semantic changed-field action | Current Project active; immutable Project preserved; historical status/reminder cannot bypass current gate; create new full version |
| Project metadata vs terminal state | projects.project.update vs start/revert/complete/skip | Terminal never reopened by Update/restore; unfinished completion reason; terminal lock propagates, Task statuses unchanged |
| Document Title/body/Tag/Icon/Cover | documents.page.save; File upload/reference for new cover | Title editable Draft/Published theo current FX-20 delegated rule; Type/Editor/Folder/Parent immutable; one Tag, Icon XOR Cover; manual Save only |
| Document publish/archive/version restore | page.publish/unpublish/archive/unarchive/restore_version separately | Restore requires history+save and field-diff guards; cannot restore old identity/topology/lifecycle via content version; child archive cohort handled by aggregate |
| Event metadata vs state/reminder | calendar.event.update vs complete/cancel/set_reminder/remove_reminder | Only Scheduled editable; no Task projection editing, no Event reopen/Trash |
| Financial posted values | finance.transaction.correct / void / split, transfer.post | No generic balance Update; Q-05 blocks unresolved semantics |
| Vault payload vs disclosure | vault.item.update vs reveal/copy; recent-auth/key proof | Read metadata does not reveal; no bulk export/reveal in support |
| Goal/Habit progress vs definition | goals.target.record_progress; habits.checkin.record/correct; habits.habit.schedule | Derived Task progress not manually writable; historical habit schedule preserved |
| Timer / Focus | time.timer.* vs time.entry.update; focus.session.* | No elapsed/duration/status tamper via generic preferences; Focus conversion requires time.entry.create |
| Domain lifecycle / renewals | respective transition/complete/cancel/renew/record action, never plain Update | Same guards for custom forms, imports and automation |
| Module/role/action grants | access.entitlement.set / role.set / permission.set; modules.policy.* | SuperAdmin only; no writable roles/grants in profile or module-settings payload |

## Dynamic composition matrix

| Wrapper | Additional source/target contract |
| --- | --- |
| Sharing create/update/revoke | sharing.link operation AND source resource.share; verify exact live/pinned projection; no global all-resource share |
| Share resolve | LINK context with current link/source gates; never owner read/history; embedded files only approved share-file projection, not owner files.download |
| File picker/upload/replace/attach | files operation AND source attachment/cover write; Clean scan; active reference/pin constraints; upload permission not attach permission |
| Global Trash / bulk restore/purge | lifecycle.resource wrapper AND source trash/restore/purge per aggregate/cohort; file deletion cannot delete owner source |
| Search/Command/Dashboard/Favorites | wrapper read and current source read on each contribution; command execution additionally actual target action |
| Calendar Task event | source tasks.task.read; opening Task requires source read; mutations always Task command; Calendar does not supply edit rights |
| ICS import | calendar.ics.import + calendar.event.create + transfer.import.commit if called through shared import surface; no recurrence/VALARM; per-event validation report |
| ICS export | calendar.ics.export + current source read for chosen kinds + transfer.export workflow when used; explicit approved Task projection exception, not Task standalone export |
| Template instantiate | organization.template.instantiate + target create + sensitive/protected field actions; sanitized seed cannot copy identity/history/link tokens |
| Read Later News state | reading.item.read/unread + news.article.mark_read/mark_unread; do not write News state through queue repository |
| Shopping purchase→Asset | shopping.purchase.create_asset + assets.asset.create; preview/save and dedupe source key; no Finance mutation |
| Resume version | career.resume.upload_version/select_document + files.upload or documents.page.history/read as relevant; exact version pin; no implicit latest |
| Automation/operations retry | wrapper run/retry AND current original owner/source/target contracts; allowed operator is not owner of payload or new grant provider |

Prerequisite permission is not automatically granted when selecting a parent action. Editor must display missing dependency and exact affected screens. Some dependencies are resource-specific and evaluated at runtime, not satisfiable by a static global grant.

## Atomicity and multi-resource operations

- Single aggregate state change, field edit and dependent projection/outbox intents commit atomically by declared architecture boundary. Parent Project close/Document archive cohort authorizes aggregate once, computes children and validates ownership; it is not permission to arbitrary child edits.
- Bulk across independent aggregates uses bounded IDs + preview revisions + per-item result; reject unauthorized items without side effects, never silently broaden selection beyond preview. Overall UI reports partial success explicitly. User must confirm preview semantics. This common UX does not change source aggregate atomicity.
- Import validates each candidate, reports invalid/duplicates; commit only authorized valid candidates. ICS source rules remain authoritative. No import source arbitrary OwnerId/status grants.
- Unrecognized/protected payload fields fail validation; ignore-only behavior must not leave user thinking a denied change saved. Conflict returns current safe diff, no blind retry overwrite.

## No-action list (R1)

No project.reopen; task.move_project; calendar.task.update; calendar.event.share/reopen/purge; document.change_editor/change_type/change_parent/change_folder/autosave; workspace/assignment/coedit; vault.public_share; github.star/fork/write; arbitrary plugin.upload/execute; Task/Project standalone import/export; notification.channel_opt_out. Recurrence/subtask/snooze remain Q-10, not hidden executable permissions.
