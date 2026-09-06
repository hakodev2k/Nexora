# UX-08 — Lifecycle & Destructive Actions

Canonical vocabulary:
- Archive — reversible lifecycle where supported.
- Unarchive — reverse under feature rules.
- Move to Trash — reversible deletion boundary.
- Restore — restore according to provenance/dependencies.
- Delete permanently — irreversible purge.

Do not use Delete for both Trash and purge.

Aggregate Trash previews affected children. Permanent delete uses explicit irreversible danger language.

Parent restore must not resurrect independently trashed children. UI preview follows the same deletion provenance model as server.

Terminal Projects and terminal Personal Calendar Events cannot be bypassed via generic history/version restore.