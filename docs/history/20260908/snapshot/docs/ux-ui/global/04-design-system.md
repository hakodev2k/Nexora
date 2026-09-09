# UX-04 — Design system foundations and primitives

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Direction retained: calm, neutral, productivity-first, data-capable. Behavior before visual branding; no glassmorphism/decorative gradients or per-module palettes. Tokens are design proposals/resolved delegated, not CSS implementation.

| Foundation | Decision | Use |
| --- | --- | --- |
| Spacing | 4px base:4,8,12,16,24,32,48 | Consistent field/card/dialog gaps; compact tables not tiny hit targets |
| Typography | Page title, section heading, body, compact data, metadata, monospace roles | Scale proposals24/20/16/14/12 with readable line-height; zoom/reflow must work |
| Radius | Small inputs4, cards8, overlays12 design token values | Semantic consistency, not40module-specific card shapes |
| Elevation | Base surface; raised menu/drawer; modal above inert backdrop | No decorative depth; sticky header below dialog/banner layers |
| Density | Comfortable default; Compact for data tables if selected | Touch actions retain44px target design goal; readability independent of row density |
| Color | Surface/text/border/focus + info/success/warning/danger neutral semantics | Contrast verified at later visual-token selection; status always label/icon as well |
| Icons | Single consistent library + emoji only approved content visuals | Decorative icon hidden from screen reader; icon-only button accessible action name |
| Motion | Short nonessential transitions; reduced-motion removes reorder/animated charts | No animation required to understand changed state |


Reusable primitives: AppShell, SidebarGroup, ModuleNav, PageHeader, Breadcrumb, Toolbar, SearchInput, FilterBar, CardGrid, DataTable, KanbanBoard, CalendarRange, Tabs, DetailPanel, FormSection, Date/DateTimePicker, TagPicker, FileUpload, Editor, SecretField, VersionHistory, ActivityTimeline, Dialog, Alert, Toast, StatusBadge, EmptyState, Skeleton and ChartWithTable. Module extensions must declare added variant and reuse keyboard/error/state contracts, not fork an entire component.

| Semantic state family | Examples | Required non-color cue |
| --- | --- | --- |
| Neutral / ready | NotStarted, Draft, Scheduled, Active | Text label; idle/clock icon optional |
| Progress | InProgress, Running, Uploading, Scanning | Label plus determinate progress only when known |
| Success | Completed, Succeeded, Accepted delivery | Check label; delivery Accepted not read receipt |
| Stopped | Skipped, Canceled, Archived | Distinct label; Canceled Calendar strikethrough plus label |
| Warning | Overdue, Stale, Expired, Partial | Warning text plus source timestamp/reason |
| Danger / blocked | Failed, Quarantined, PermissionDenied | Error label and safe next action |
| Unavailable | Disabled module, missing provider/source, revoked access | Unavailable wording without hidden content leakage |


Canonical user-visible English spelling Canceled, with localization keys; historical source Cancelled is an alias, not new lifecycle. Archived ≠ Completed ≠ Trash. No single generic green state means paid/verified/complete across domains. Priorities P0(highest)…P3 use ordered labels not status color mapping.
