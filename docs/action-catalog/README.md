# Nexora Action Catalog v1.1

2026-09-09 · Updated after [Product Owner decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md). Docs/action contracts are design authority, not runtime evidence. **Only M01 + backend/frontend scaffold + local scripts are approved for implementation by `DEC-20260909-001`; all other action contracts require a later bounded approval.**

733 registered/documented contracts across40 FX scopes, including retained historical keys; 202 documented Screen IDs including paused/conditional screens. Status counts from catalog v1.1 remain: Resolved delegated **584**; Blocked **75**; Superseded **11**; Paused **63**. “Resolved delegated” describes action design, not a guarantee every dependent resource/proof exists or that every action is approved for implementation now.

**User được cấp module; Admin được cấp module/action; SuperAdmin đổi grants.** Paused/Blocked/Superseded is stronger than Allow. No new User action-level grants. 641 action classes are theoretically Admin-grantable; 520 are in current resolved scope before resource/context checks. `DEC-20260909-001` narrows what may be implemented now to the M01 action/story set.

[PO change mapping](08-owner-decision-changes.md) · [Decision status](../features/90-open-decisions.md) · [Authorization](00-authorization-contract.md) · [Composition](01-composition-and-field-guards.md) · [Revoke](02-revocation-and-runtime.md) · [Permission editor](03-permission-editor.md) · [SDK](04-module-action-contract.md) · [Acceptance](05-verification.md) · [Screen bindings](06-screen-bindings.md) · [Review](07-review-and-open-gates.md) · [CSV](catalog.csv)

| FX | Catalog | Total | Resolved | Blocked | Paused | Superseded |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| FX-01 | [Identity / Profile](modules/01-identity.md) | 19 | 15 | 3 | 0 | 1 |
| FX-02 | [Users / Roles / Permissions](modules/02-access.md) | 9 | 9 | 0 | 0 | 0 |
| FX-03 | [Module Platform](modules/03-modules.md) | 10 | 10 | 0 | 0 | 0 |
| FX-04 | [Read-only Sharing](modules/04-sharing.md) | 6 | 6 | 0 | 0 | 0 |
| FX-05 | [Support / Emergency](modules/05-support.md) | 7 | 7 | 0 | 0 | 0 |
| FX-06 | [Notifications](modules/06-notifications.md) | 10 | 10 | 0 | 0 | 0 |
| FX-07 | [Files / Attachments](modules/07-files.md) | 14 | 14 | 0 | 0 | 0 |
| FX-08 | [Trash / Activity / Audit](modules/08-lifecycle.md) | 8 | 8 | 0 | 0 | 0 |
| FX-09 | [Settings / Shell](modules/09-settings.md) | 4 | 4 | 0 | 0 | 0 |
| FX-10 | [Import / Export / Backup](modules/10-transfer.md) | 13 | 7 | 6 | 0 | 0 |
| FX-11 | [Projects](modules/11-projects.md) | 13 | 13 | 0 | 0 | 0 |
| FX-12 | [Tasks](modules/12-tasks.md) | 18 | 18 | 0 | 0 | 0 |
| FX-13 | [Calendar](modules/13-calendar.md) | 13 | 13 | 0 | 0 | 0 |
| FX-14 | [Reminders / Scheduling](modules/14-reminders.md) | 6 | 6 | 0 | 0 | 0 |
| FX-15 | [Planner](modules/15-planner.md) | 7 | 7 | 0 | 0 | 0 |
| FX-16 | [Goals](modules/16-goals.md) | 22 | 22 | 0 | 0 | 0 |
| FX-17 | [Habits](modules/17-habits.md) | 16 | 16 | 0 | 0 | 0 |
| FX-18 | [Time Tracking](modules/18-time.md) | 13 | 13 | 0 | 0 | 0 |
| FX-19 | [Pomodoro / Focus](modules/19-focus.md) | 9 | 9 | 0 | 0 | 0 |
| FX-20 | [Documents](modules/20-documents.md) | 27 | 27 | 0 | 0 | 0 |
| FX-21 | [Bookmarks](modules/21-bookmarks.md) | 12 | 10 | 1 | 0 | 1 |
| FX-22 | [Snippets](modules/22-snippets.md) | 14 | 14 | 0 | 0 | 0 |
| FX-23 | [Read Later](modules/23-reading.md) | 7 | 7 | 0 | 0 | 0 |
| FX-24 | [Tags / Collections / Templates](modules/24-organization.md) | 23 | 22 | 1 | 0 | 0 |
| FX-25 | [Search / Favorites / Command Palette](modules/25-discovery.md) | 15 | 15 | 0 | 0 | 0 |
| FX-26 | [Dashboard](modules/26-dashboard.md) | 8 | 8 | 0 | 0 | 0 |
| FX-27 | [Finance](modules/27-finance.md) | 53 | 8 | 45 | 0 | 0 |
| FX-28 | [Vault](modules/28-vault.md) | 28 | 24 | 1 | 0 | 3 |
| FX-29 | [News / Feeds](modules/29-news.md) | 28 | 26 | 2 | 0 | 0 |
| FX-30 | [Price Tracking](modules/30-prices.md) | 18 | 0 | 0 | 18 | 0 |
| FX-31 | [Shopping Records](modules/31-shopping.md) | 36 | 34 | 2 | 0 | 0 |
| FX-32 | [Developer Toolbox](modules/32-toolbox.md) | 33 | 31 | 0 | 2 | 0 |
| FX-33 | [GitHub Discovery](modules/33-github.md) | 17 | 12 | 5 | 0 | 0 |
| FX-34 | [Automation](modules/34-automation.md) | 20 | 0 | 0 | 20 | 0 |
| FX-35 | [Integrations / Webhooks / n8n](modules/35-integrations.md) | 21 | 0 | 0 | 21 | 0 |
| FX-36 | [Monitoring / Job Operations](modules/36-monitoring.md) | 14 | 12 | 0 | 2 | 0 |
| FX-37 | [Personal Assets](modules/37-assets.md) | 29 | 27 | 2 | 0 | 0 |
| FX-38 | [Digital Assets](modules/38-digital.md) | 17 | 13 | 4 | 0 | 0 |
| FX-39 | [Career / Jobs / Resumes](modules/39-career.md) | 38 | 31 | 1 | 0 | 6 |
| FX-40 | [Learning / Work Log](modules/40-learning.md) | 58 | 56 | 2 | 0 | 0 |

40 FX namespaces are not40 mandatory assemblies/DbContexts. New action registration never auto-grants Admin access. Source Modules/owner/lifecycle/current policy and explicit operating contexts remain mandatory. Local pure-tool capability controls UI availability, not a user's ability to run an algorithm elsewhere.

## Current implementation rule

For the next implementation agent, the action catalog must be filtered by M01 story/action lists under `docs/delivery/milestone-01`. Do not implement actions solely because this catalog lists them as Resolved delegated. Paused/Blocked/Superseded remains stronger than Allow/default-on. Runtime availability still requires installed code, compatible migrations, health/readiness, current scope, module enablement, action authority, lifecycle checks and field projection.
