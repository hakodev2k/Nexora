# UX-10 — Responsive contracts

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| Surface | Desktop >=1200 | Tablet768–1199 | Mobile<768 |
| --- | --- | --- | --- |
| Navigation | Expanded sidebar, optional module tree | Compact rail + tree drawer | Bottom Home/Search/Create/Inbox/More, module stack |
| Grid/list | Fit content-based columns, persistent filters | Fewer columns, filter drawer if needed | One column; explicit More actions, not hover |
| Table | Full columns, header sticky in region | Column priorities + optional detail drawer | Primary identity/status/value fields; expand or detail for all others; ledger labeled horizontal region only when irreducible |
| Detail | Main body + optional right metadata panel | Panel drawer with close | One-column stack, metadata accordion, Back to list |
| Form | Bounded readable width, optional two-column independent fields | Single/two-column if labels fit | Single column; sticky Save does not cover keyboard/errors; no multicolumn datetime trap |
| Documents editor | Module tree + canvas; Markdown optional split | Tree drawer, one canvas | Page stack, child sidebar drawer, Markdown Edit/Preview tabs; manual Save visible |
| Kanban | Status columns visible with horizontal region if necessary | Scrollable columns + status overview | Status selector/one column list with counts, Move action; same Kanban state/rank, not forced data loss |
| Calendar | Selected Day/Week/Month/Agenda | Preserve chosen view, day drilldown | Default Day retained; Week/Month day list drilldown and Back; Agenda always accessible alternative |
| Workbench | Input/output split | Resizable/tabbed regions | Input/Output tabs with result/error badge and preserved memory draft |
| Charts | Chart + table toggle | Chart + table toggle | Simplified chart + always available value list/table; no tooltip-only values |
| Admin matrix | Grouped permissions table | Group accordion + detail | Module group list, action rows, before/after review; no unreachable huge spreadsheet |
| Support mode | Persistent full banner | Persistent compact banner | Persistent compact User/module/expiry/End, expanded reason panel for Emergency |


Responsive changes are presentation only: must not reset Task status, selected Calendar date/view, filters, entered form, manual Save state or access context. Orientation change keeps focused element visible. Dialog on mobile may be full-screen sheet with same title/actions/trap/return behavior. Touch target design goal44px; spacing allows safe destructive action separation. At200%zoom/320px width no whole-page sideways scrolling for prose/forms; test virtual keyboard obscuring Save/error and browser safe areas.
