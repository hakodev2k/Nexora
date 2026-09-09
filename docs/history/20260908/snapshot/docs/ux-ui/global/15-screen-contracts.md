# UX-15A — Normative common screen contracts

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Every module Screen specification assigns one profile below. **Inheritance is normative**, not optional vague advice: all fields and states below apply unless the screen gives a stricter explicit override. N/A always means no corresponding capability in that screen, never invent CRUD to fill a template. Screen primary/secondary actions, data and controls come from its inventory; lifecycle matrix and context gates intersect them.

| Profile | Layout/content | Header/action placement | Selection / exit |
| --- | --- | --- | --- |
| BROWSE | Shared list/grid/table/Kanban region under search/filter toolbar; sidebar remains usable | h1 Screen title; declared primary right of header; secondary More and view controls | Select only declared bulk-capable resources; Back restores parent filter/scroll, direct deep link fallback module root |
| DETAIL | Header, lifecycle/access banner, metadata then main body; tabs only declared domains; optional context panel | Resource safe title or screen title; declared primary; More for remaining authorized actions | No bulk selection; explicit breadcrumb/Back; source links carry context |
| FORM | Bounded field groups in declared order; required/context fields first, optional/sensitive later | Screen title, immutable context summary; Save/Cancel persistent without covering fields | No data pagination except pickers; dirty Save/Discard/Keep editing; Save new→detail, edit→current detail |
| DIALOG | Modal preview/reason/form with affected-resource list and explicit footer | Accessible title/explanation; cancel plus verb-specific primary, danger if irreversible | No backdrop destructive submit; focus trap/return; Escape Cancel while not committed |
| HISTORY | Version/activity list beside readonly preview desktop; stacked list→preview mobile | History title and source link; only explicitly approved restore primary | Version selected read-only, no auto-restore; Back returns source; paged list25 default |
| EDITOR | Header/manual Save/unsaved/lifecycle; editable body or readonly canvas; metadata/sidebar optional | Title/status then Save; mode-specific toolbar; lifecycle More | Editor owns text selection only; dirty guard; no autosave; explicit focus exit |
| WORKBENCH | Declared inputs/options and results, or timer state; desktop split/mobile tabs | Tool/phase title, privacy/source badge; Run/Start/Stop as declared | No implicit data list; clear operation explicit; exit retains authorized current memory only |
| CALENDAR | Date/view toolbar; all-day band and time/date grid; overflow day list | Date range and timezone, New Event, Today/prev/next/view | Selected date not arbitrary resource selection; Back restores date/view; keyboard/Agenda alternative |
| DASHBOARD | Ordered independent widgets with each state/error/freshness and source link | Home/module heading; declared attention/create action; configuration secondary | No bulk selection; charts have table/text equivalent; Back/source returns same widget context |
| ADMIN | Grouped operational table/matrix or change preview; no owner payload hidden in DOM | Admin area heading, target metadata, permission-limited primary | Select explicit targets; before/after review; return authorized Admin landing after revoke |


## State contract applied to every Screen

| Dimension | Exact default | Exceptions / N/A |
| --- | --- | --- |
| Entry | From inventory parent/module or same-origin authorized deep link. Recheck context before loading data. | Public auth/share has public shell and no previous personal data cache. |
| Header | One h1, breadcrumb/Back, lifecycle/unsaved/context labels; primary+secondary as declared. | Dialog uses accessible modal heading rather than duplicate page h1. |
| Data | Only inventory fields and declared form payload, with safe missing-value dash/Unknown label. | Never add DB technical columns or reference-product fields to approved Card/Table. |
| Search/filter/sort/pagination | Use inventory Controls exactly;25/50/100 and stable Id only for pageable collections. | Form/dialog/editor/workbench no list controls unless picker/history explicitly listed; range Calendar bounded not offset pages. |
| No data yet | Explain purpose and offer declared authorized create/add action; if none, explain how data arrives. | Provider-fed result cannot say Create if User cannot create source. |
| No search/filter matches | State current scope/query, Clear filters or edit query; keep create only if independently allowed. | No query controls on screen → N/A. |
| Loading | Skeleton for initial data structure; labeled spinner for short action; aria-busy. | Unknown permission/context never displays previous owner/private payload beneath skeleton. |
| Fetch error | Safe error, correlation and Retry only when retryable; keep authorized last-success view labeled stale. | No successful previous result → error panel, not empty; no stack/SQL/secret. |
| Provider degraded | Per-provider error/timestamp; retry-after; partial results labeled incomplete. | No provider dependency → ordinary fetch error only, not fabricated provider-status panel. |
| Module disabled | Show Module unavailable + Back/Home, clear affected cache; data retained explanation for owner. | Auth/public root core screen not optional module; no unavailable fake business-data listing. |
| Permission denied/revoked | Clear inaccessible data/draft; safe denial + authorized exit, no hidden DOM payload. | Public share generic unavailable; do not enumerate resource existence. |
| Readonly | Lifecycle/access banner, selectable safe content, no mutation affordances; allowed history/exit retained. | Readonly is contextual, not copying all entity fields; sensitive projection still restricts. |
| Archived | Source matrix dictates inspect/Unarchive/Trash and prior state; no editor Save. | No Archive lifecycle in source → N/A; do not add Archive UI. |
| Trash | Only approved preview/Restore/Purge with original parent/pin guards; no active share/edit. | Manual Calendar Event uses Canceled, no Trash; auth/admin operational records no generic Trash. |
| Conflict | No optimistic final success; retain authorized draft, show current revision/state and safe compare/reload/reapply. | Immutable history/public readonly screen no mutation conflict; refresh source availability instead. |
| Mobile/tablet/desktop | Use UX-10 profile; first named identity then status/time/amount fields prioritized, all rest available in detail/expand. | Screens with exact list fields keep those fields; no alternate default view merely because viewport changes. |
| Keyboard | Logical Tab; Enter open/submit explicit; Escape current overlay; named action menus; drag alternatives; editor exit. | Shortcuts never active inside arbitrary text/IME unless editor owns them. |
| Accessibility | Semantic headings/labels/status text; visible focus; tables/charts equivalents; touch/zoom/reduced-motion. | No compliance claim before implemented tests. |
| Exit/back | Return to invoking screen/filter/date/scroll; deleted trigger → next row or list heading; direct-entry fallback module root. | Dirty guard before leaving; if auth revoked security clears draft/data. |


## Dialog behavior completeness

Each DIALOG Screen's Data is the affected-resource preview and explanation content; Primary/Secondary provide verb/cancel; its behavior specifies source exceptions. Global D-* dialogs in UX-08 define wording/risk/focus/retry. When a screen uses multiple operations, title/primary verb must match chosen operation; never generic OK for purge/share/close/emergency. Confirm payload includes expected source revisions, so preview cannot authorize stale destructive execution.

## Five-question review rubric

User goal = module Scope/Journey; simplest IA = inventory grouped by source lifecycle; mature reference = linked source/evidence; suitable/unsuitable behaviors = Apply/Adapt/Reject register; remaining important guess = only listed Q-gated major decisions or later technical library/visual fidelity testing, never an undisclosed workflow choice. This is document-level coverage, not usability testing or final implementation approval.
