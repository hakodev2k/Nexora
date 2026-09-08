# UX-02 — Application shell

> **Current decision amendment — 2026-09-07:** Internal-only user journeys; paused module navigation removed; Account/Vault deletion exceptions and SuperAdmin RECOVERY context override generic purge/restore patterns. Language default vi, selectable en. External research URLs in docs are evidence, not product navigation. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Desktop >=1200 CSS px: persistent global sidebar (~240expanded/~64collapsed design tokens), content header and flexible main region; module sidebar only when hierarchy helps (Documents/Vault). Optional detail context panel occupies bounded region, never obscures Save or required access banner. Dense Finance/Assets tables can use full content width; reading body bounded about68–80characters. Breakpoints are delegated layout decisions, not device detection.

Header anatomy: breadcrumb Back/root path; single h1; source/status/unsaved/access labels; one primary action; secondary buttons and named More menu. Do not render unrelated actions merely because a shared toolbar supports them. Sticky action header must not cover focused field/error or modal heading.

Global sidebar: Home/Search/Create/Notifications; grouped registered modules; utility footer Settings/Trash; current route label+indicator+aria-current, not color alone. Collapse preserves icon accessible names and tooltip/focus equivalents. Sidebar group expansion does not trigger navigation.

Tablet768–1199: compact nav rail/drawer; module tree collapsible drawer; detail overlay with explicit close/back. Mobile<768: stack navigation with Home/Search/Create/Inbox/More; More searchable groups. Module-local filters/tree appear in labeled sheet, not off-screen second permanent sidebar. Small320px viewport supported without whole-page horizontal scroll except explicitly labeled data grid region.

Support/Emergency banner belongs above content and persists in all layouts, dialogs show compact mode context. Personal page header never adopts target-user avatar as if logged in as that User. Inoperative module cannot blank shell. Content skeleton only main region while navigation remains usable, unless identity/context itself unknown; then no prior private content is shown.

Save/Discard guard applies leaving dirty forms even via sidebar, breadcrumb, browser Back and command palette. Modal focus follows UX-11; restored navigation returns scroll/filter/selection where still valid. No logo/branding redesign or CSS code delivered.
