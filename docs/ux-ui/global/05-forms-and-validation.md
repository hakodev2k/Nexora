# UX-05 — Forms & Validation

Default:
- explicit Save;
- clear required/optional labels;
- trim required strings; blank is invalid;
- field-level errors adjacent to fields;
- server validation authoritative;
- failed requests preserve input;
- dirty forms warn on leave/cancel.

Save states: `Idle → Dirty → Saving → Saved | Validation | Conflict | Network error`.

Stale revision:
1. keep local draft;
2. explain changed elsewhere;
3. Reload latest;
4. Compare when justified;
5. reapply/save on fresh revision if lifecycle permits;
6. never silently overwrite.

Session expiry preserves safe non-sensitive input; never persist Vault secrets in browser persistent storage.

Immutable fields become visibly read-only with explanation.