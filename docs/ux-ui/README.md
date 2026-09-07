# Nexora UX/UI specification system

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Đã review và bổ sung theo global architecture trước, sau đó từng module: **40 feature specifications, 197 Screen IDs**. Mỗi module có24 sections, inventory, form/validation, action matrix, dialogs, error/degraded/readonly, mobile/keyboard, evidence và Q gates. Common screen contract là normative để không tạo40 design systems riêng.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## Start here

- [Principles](global/00-product-ux-principles.md), [IA](global/01-information-architecture.md), [Shell](global/02-application-shell.md), [Navigation](global/03-navigation.md).
- [Design system](global/04-design-system.md), [Forms](global/05-forms-and-validation.md), [Lists/Grid/Table/Kanban](global/06-lists-grids-tables-kanban.md), [Search](global/07-search-command-palette.md).
- [Lifecycle/dialogs](global/08-lifecycle-destructive-actions.md), [Notifications](global/09-notifications-feedback.md), [Responsive](global/10-responsive-design.md), [Accessibility](global/11-accessibility.md).
- [Sensitive UX](global/12-security-sensitive-ux.md), [Support/Emergency](global/13-admin-support-emergency.md), [Cross-module](global/14-cross-module-interactions.md), [Normative Screen profiles/states](global/15-screen-contracts.md).
- [Full screen inventory](screen-inventory.md), [References](references/product-reference-register.md), [Behavior decisions](references/behavior-pattern-register.md), [Consistency review](audits/cross-module-consistency.md).

## Modules

| Feature | Specification | Screens |
| --- | --- | --- |
| FX-01 | [identity-profile](modules/01-identity-profile.md) | 6 |
| FX-02 | [users-roles-permissions](modules/02-users-roles-permissions.md) | 5 |
| FX-03 | [module-platform](modules/03-module-platform.md) | 5 |
| FX-04 | [sharing](modules/04-sharing.md) | 4 |
| FX-05 | [support-emergency](modules/05-support-emergency.md) | 5 |
| FX-06 | [notifications](modules/06-notifications.md) | 3 |
| FX-07 | [files](modules/07-files.md) | 5 |
| FX-08 | [trash-activity-audit](modules/08-trash-activity-audit.md) | 4 |
| FX-09 | [settings-app-shell](modules/09-settings-app-shell.md) | 4 |
| FX-10 | [import-export-backup](modules/10-import-export-backup.md) | 5 |
| FX-11 | [projects](modules/11-projects.md) | 5 |
| FX-12 | [tasks](modules/12-tasks.md) | 6 |
| FX-13 | [calendar](modules/13-calendar.md) | 7 |
| FX-14 | [reminders](modules/14-reminders.md) | 3 |
| FX-15 | [planner](modules/15-planner.md) | 3 |
| FX-16 | [goals](modules/16-goals.md) | 4 |
| FX-17 | [habits](modules/17-habits.md) | 4 |
| FX-18 | [time-tracking](modules/18-time-tracking.md) | 4 |
| FX-19 | [pomodoro-focus](modules/19-pomodoro-focus.md) | 4 |
| FX-20 | [documents](modules/20-documents.md) | 10 |
| FX-21 | [bookmarks](modules/21-bookmarks.md) | 3 |
| FX-22 | [snippets](modules/22-snippets.md) | 3 |
| FX-23 | [read-later](modules/23-read-later.md) | 3 |
| FX-24 | [organization-tags-templates](modules/24-organization-tags-templates.md) | 5 |
| FX-25 | [search-favorites-command-palette](modules/25-search-favorites-command-palette.md) | 4 |
| FX-26 | [dashboard](modules/26-dashboard.md) | 2 |
| FX-27 | [finance](modules/27-finance.md) | 12 |
| FX-28 | [vault](modules/28-vault.md) | 6 |
| FX-29 | [news-feeds](modules/29-news-feeds.md) | 5 |
| FX-30 | [price-tracking](modules/30-price-tracking.md) | 5 |
| FX-31 | [shopping-records](modules/31-shopping-records.md) | 7 |
| FX-32 | [developer-toolbox](modules/32-developer-toolbox.md) | 4 |
| FX-33 | [github-discovery](modules/33-github-discovery.md) | 4 |
| FX-34 | [automation](modules/34-automation.md) | 5 |
| FX-35 | [integrations-webhooks](modules/35-integrations-webhooks.md) | 5 |
| FX-36 | [monitoring-jobs](modules/36-monitoring-jobs.md) | 5 |
| FX-37 | [personal-assets](modules/37-personal-assets.md) | 6 |
| FX-38 | [digital-assets](modules/38-digital-assets.md) | 4 |
| FX-39 | [career-jobs-resume](modules/39-career-jobs-resume.md) | 7 |
| FX-40 | [learning](modules/40-learning.md) | 6 |


## Status / next gate

Documentation-level review completed subject to explicit findings/decisions, not final visual mockups/usability tests or implementation approval. [Open questions](decisions/ux-open-questions.md) and [cross-layer gates](../design-review/03-decision-impact.md) remain. [Database](../design-database/README.md) and [Architecture](../architecture/README.md) trace data/command boundaries. No application code, CSS/JSX, DB/migrations or package installation in this phase.
