# UX-05 — Forms and validation

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Form order: context/immutable location → required identifying fields → required time/amount → optional content/metadata → sensitive/options disclosure → sticky or end-of-form Save/Cancel. Required marker explained once; optional fields explicitly labeled where needed. Initial focus first editable required field; editor content focus only after Title/context known. Single primary Save, no competing bottom/top labels with different effects.

Validation: native input hints plus shared schema client validation; server authoritative. Before submit avoid yelling errors on untouched blank form. On blur show relevant field error; on submit validate all, error summary links fields and focus first invalid. Keep entered values on422/network/conflict; never auto-truncate. Datetime zone shown; Start/End relation validates pair; impossible/ambiguous local time not guessed. Numeric amount shows unit/currency and exact precision.

Explicit Save starts Pending and disables duplicate submission, not Cancel navigation without explanation. Same retry uses idempotency key; success updates source revision/version and unsaved indicator only after response. Document distinct Save even unchanged creates version, same command retry does not. No autosave indicator/typing-save animation. Tag/cover metadata edits on Document remain part of pending Page Save.

Dirty-leave dialog: title Unsaved changes; explain source and that changes not saved; actions Save changes, Discard changes, Keep editing. Discard never deletes saved resource. Session expiry: reauthenticate with safe return; only current authorized nonsensitive draft may remain memory, secret drafts not persisted. Revoked access clears protected data and draft; security overrides convenience of preserving typed content.

Conflict: state changed since loaded; compare server current values safely with local draft, Reload or Reapply permitted fields on refreshed revision. No force overwrite option. Terminal Project/Archived Page blocks reapply, explains lifecycle. Version restore uses normal command and immutable-field validation.

Pickers: combobox has label/current value/clear only if optional; manual option selection, arrows/Enter/Escape, current focus visible. Inline Tag creation allowed only feature cardinality/catalog permits. File picker shows scan/ownership eligibility and preserves form on failure. All required UI fields are in module form specifications; DB NOT NULL technical fields such as OwnerId/rowversion are never user inputs.
