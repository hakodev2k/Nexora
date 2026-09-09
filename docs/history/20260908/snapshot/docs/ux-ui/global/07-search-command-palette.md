# UX-07 — Search and command palette

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Search global entry and Cmd/Ctrl+K palette are always discoverable through buttons. Global Search reads current enabled registered providers with owner-safe projections; local feature search never silently becomes global. Documents location query covers direct content only; a separate Search all Nexora link may navigate global search with explicit scope label.

Palette opens modal combobox, input focused; before typing show permitted commands and recent safe resource refs. Results grouped Navigation/Create/Resource actions. Arrow keys change active result without executing; Enter invokes; Escape closes; Tab follows controls but not inert background. Do not intercept browser Cmd/Ctrl+F or typed single letters globally. Shortcut help visible.

Selected destructive action launches its standard confirmation with source preview/authorization; nested modal avoided by closing palette first while retaining focus-return chain. Create Task selects active Project/full form; Create Document requires explicit Type/Mode; no private content injected into command labels.

Search response states: Pending skeleton; Results; No matches with clear query/filters; Partial provider failure with retained valid hits; All unavailable with Retry; stale hit after revoke removed before render. Counts/snippets/facets are authorized, never existence oracle. Query<=500, saved search name<=100; recents100 metadata limit. No Vault secrets, support reason or token in index/query history.

Reference global-vs-in-view distinction: [Linear Search](https://linear.app/docs/search), checked2026-09-07. Nexora keyboard choice is delegated, not a claim Linear uses exactly this binding for Search. [Combobox semantics](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) informs accessibility; screen-reader/keyboard tests still required after implementation.
