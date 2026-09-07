# UX-08 — Lifecycle and destructive actions

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Vocabulary fixed: Archive; Unarchive; Move to Trash; Restore; Delete permanently. Calendar ManualEvent uses Cancel Event because delete is terminal Canceled, not Trash. Complete Project warns permanent lock; Skip Project keeps child statuses unchanged. No generic Restore button can bypass domain terminal rules.

| Dialog ID | Title / explanation | Preview / action / cancel | Failure and focus |
| --- | --- | --- | --- |
| D-TRASH | Move to Trash? Saved resource leaves active views; recoverability depends original parent | Selected root/count/children and already-independent Trash exclusions; Move to Trash / Cancel | Cancel focused for large tree; commit revalidates; failures keep preview and safe reason |
| D-PURGE | Delete permanently? This cannot be undone through Nexora | Exact roots/children/versions/files and pinned blockers; explicit typed name for aggregate/bulk; Delete permanently / Cancel | Danger styling+text, default Cancel; no undo toast; partial report if heterogeneous bulk |
| D-RESTORE | Restore these items? Original location and state will be used | Cohort members, parent state, missing/pinned dependencies; Restore / Cancel | Refresh preview if source changed; never move child to invented root as repair |
| D-ARCHIVE | Archive? Content becomes readonly, not deleted | Affected parent/children and independent archived exclusions; Archive / Cancel | Preview root gate/revision; fail without partial tree |
| D-UNARCHIVE | Unarchive? Restore previous states for this cohort | Affected members + skipped Trash/independent Archived children; Unarchive / Cancel | Child blocked while parent Archived; focus blocked explanation |
| D-REASON | Explain backward transition | From/to and required nonblank reason; Confirm transition / Cancel | First reason field focused; canceled drag remains original;409/412 preserves reason in authorized memory |
| D-PROJECT-CLOSE | Complete/Skip Project permanently? | Unfinished Tasks/count; no further edit/create/continue; reason required for Complete with unfinished; Confirm / Cancel | Default Cancel, danger irreversible lock warning even data not deleted; server parent lock |
| D-SHARE | Create readonly share link | Audience/expiry/allowlist and exact disclosed fields; Create link / Cancel | Missing sensitive projection/Q gates block; no token in diagnostics |
| D-DISABLE | Disable module? Data retained but access/work gated | Dependents/users/jobs/contributions; Disable module / Cancel | Policy revision conflict refreshes preview; no uninstall/purge |
| D-EMERGENCY | Start emergency access? User notified immediately | User/module/reason/expiry/audit disclosure; Start access / Cancel | Reason focused, recent-auth if required; audit failure no access |
| D-IMPORT-EXPORT | Review data operation | Format/scope/valid+skipped counts or exported fields; Apply/Generate / Cancel | Safe row/operation errors; no mutation on preview |
| D-UNSAVED | Unsaved changes | Source; Save changes / Discard changes / Keep editing | No close by backdrop causing silent discard; focus Keep editing |
| D-CONFLICT | This resource changed | Safe current state/version and draft differences; Reload / Reapply allowed fields / Cancel | No Force save; cleared data when authorization revoked |


All dialogs have accessible title, purpose, named primary/cancel, modal focus trap and return to invoking control or next surviving row. Busy state announces operation; cancel after irreversible dispatch is cooperative, not rollback promise. Provider/status/expiry error never implies deleted data. Notifications deletion is permanent removal from inbox only, not security audit or source deletion. General module Archives require feature support; do not apply Archive to account/module settings merely because common dialog exists.
