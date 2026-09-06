# UX-06 — Collection Views

Shared toolbar order: title/context → primary create → scoped search → filters → sort → view switch → bulk actions after selection.

Default pagination baseline: 25; options 25/50/100 where appropriate.

Grid: visual scanning, e.g. Projects/Documents. Cards show decision-driving metadata only.

Table: Finance/Assets/Admin/runs. Sticky header, numeric alignment, sortable headers when supported, row menu, keyboard navigation, responsive column priority.

Kanban:
- only when columns represent real state/grouping;
- drag is convenience, never sole control;
- reverse transitions can require reason before commit;
- rejected move returns card to authoritative position.

Empty states distinguish no data, no search result, no filtered result, unavailable, denied, provider unavailable, fetch failed and stale/degraded.