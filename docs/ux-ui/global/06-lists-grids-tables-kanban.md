# UX-06 — Lists, Grid, Table and Kanban

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

List toolbar: h1/header actions → local search label/scope → filters/chips+clear → view/sort controls → result/selection count → content → pagination. Default25, options25/50/100, stable Id tie; no unbounded client fetch. Changing filter/query resets cursor/selection with notice; view switch retains same query and permitted selection. Count uses same safe server predicate, not loaded rows. Search typing debounce300ms delegated; Enter immediate; cancel obsolete request to prevent stale results replacing new query.

Grid cards: one primary link, secondary More button with accessible name; no nested clickable div traps. Keyboard Tab to card link/More, Enter opens. Hover reveals are also visible on focus/touch. Columns adapt to content minimum; mobile single column. Projects card exactly Title/Start/End; Documents exactly Title/Type/Tag, not extra fields copied from illustrative prompt.

Tables: native semantic table/caption/column headers; sort button announces ascending/descending; selection checkbox labels resource, header checkbox selects current page only. Bulk bar states selected count and scope. Row actions separate from navigation and do not toggle checkbox accidentally. Sticky header within labeled scrolling region; number/currency aligned and accompanied by units. Mobile priority fields from each Screen; remaining values expandable/detail, not removed. Horizontal scroll allowed for irreducible comparison/ledger with accessible region and full-detail alternative.

Kanban: four Task columns as approved with counts; rank within column; paged Load more each column and true filtered total. Drag only permitted active-Project Tasks. Pending visual ghost until server confirms; backward move opens required-reason dialog before command. Reject/conflict restores original card position and announces reason. Equivalent More → Move to status and Move before/after item; keyboard actions use same API/permissions. Column creation buttons only NotStarted/InProgress and always full Task form.

Reordering positions: client sends neighbors/expected revision, not unlimited arbitrary owner rank; server persists stable order/rebalances atomically. Another update during reorder yields reload/anchor recovery. Filtered order cannot imply moving hidden items across unrelated scopes.

Bulk destructive action: preview explicit selected IDs/revisions/count and aggregate children, never silently select all database matches. Revalidate at commit; report per-item success/blocked/failure for heterogeneous batch. Root aggregate operations atomic. Removing item returns focus to next row or list heading when empty. Empty data, no matches, provider failure, stale state and module-disabled are distinct contracts.
