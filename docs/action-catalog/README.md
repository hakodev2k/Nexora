# Nexora Action Catalog

2026-09-09 · Docs/action contracts are design authority, not runtime evidence. **Only M01 + backend/frontend scaffold + local scripts are approved for implementation by `DEC-20260909-001`; every non-M01 action requires a later bounded vertical-slice approval.**

## Read order before implementation

1. [Product Owner decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md)
2. [Paused / blocked / gated register](../delivery/04-paused-blocked-gate-register.md)
3. [Effective action implementation status 2026-09-09](09-effective-implementation-status-20260909.md)
4. The relevant module table under [`modules/`](modules/)
5. [Authorization](00-authorization-contract.md), [Composition](01-composition-and-field-guards.md), [Revocation/runtime](02-revocation-and-runtime.md), [Permission editor](03-permission-editor.md), [SDK](04-module-action-contract.md), [Acceptance](05-verification.md), [Screen bindings](06-screen-bindings.md), [Review](07-review-and-open-gates.md), [PO change mapping](08-owner-decision-changes.md), [CSV snapshot](catalog.csv)

## Effective status rule

The older module tables remain useful for action names, contexts, guards, dependencies and UI bindings. Their catalog v1.1 `Current scope` / `Status / gate` wording is not enough to decide implementation. Effective implementation status is normalized by [09-effective-implementation-status-20260909.md](09-effective-implementation-status-20260909.md).

Important consequences:

- Rows listed as `APPROVED_FOR_M01` may be implemented now only inside the exact M01 package and still need runtime evidence.
- Rows not listed as M01-approved default to `DESIGN_RESOLVED_NOT_APPROVED_NOW`, unless they are explicitly `PO_PAUSED`, `NETWORK_GUARD_GATED`, `SENSITIVE_PROJECTION_GATED`, `POLICY_APPROVED_IMPLEMENTATION_GATED`, `PRODUCTION_OPS_GATED` or `SUPERSEDED`.
- `Resolved delegated` means the action design is resolved; it does not mean the action is approved for implementation now.
- `Paused`, `Blocked`, `Gated` and `Superseded` are stronger than Allow/default-on/module catalog membership.
- There is no `Full R1 implementation-ready` Go state. Future work proceeds by explicitly approved vertical slices.

## Module indexes

| FX | Catalog |
| --- | --- |
| FX-01 | [Identity / Profile](modules/01-identity.md) |
| FX-02 | [Users / Roles / Permissions](modules/02-access.md) |
| FX-03 | [Module Platform](modules/03-modules.md) |
| FX-04 | [Read-only Sharing](modules/04-sharing.md) |
| FX-05 | [Support / Emergency](modules/05-support.md) |
| FX-06 | [Notifications](modules/06-notifications.md) |
| FX-07 | [Files / Attachments](modules/07-files.md) |
| FX-08 | [Trash / Activity / Audit](modules/08-lifecycle.md) |
| FX-09 | [Settings / Shell](modules/09-settings.md) |
| FX-10 | [Import / Export / Backup](modules/10-transfer.md) |
| FX-11 | [Projects](modules/11-projects.md) |
| FX-12 | [Tasks](modules/12-tasks.md) |
| FX-13 | [Calendar](modules/13-calendar.md) |
| FX-14 | [Reminders / Scheduling](modules/14-reminders.md) |
| FX-15 | [Planner](modules/15-planner.md) |
| FX-16 | [Goals](modules/16-goals.md) |
| FX-17 | [Habits](modules/17-habits.md) |
| FX-18 | [Time Tracking](modules/18-time.md) |
| FX-19 | [Pomodoro / Focus](modules/19-focus.md) |
| FX-20 | [Documents](modules/20-documents.md) |
| FX-21 | [Bookmarks](modules/21-bookmarks.md) |
| FX-22 | [Snippets](modules/22-snippets.md) |
| FX-23 | [Read Later](modules/23-reading.md) |
| FX-24 | [Tags / Collections / Templates](modules/24-organization.md) |
| FX-25 | [Search / Favorites / Command Palette](modules/25-discovery.md) |
| FX-26 | [Dashboard](modules/26-dashboard.md) |
| FX-27 | [Finance](modules/27-finance.md) |
| FX-28 | [Vault](modules/28-vault.md) |
| FX-29 | [News / Feeds](modules/29-news.md) |
| FX-30 | [Price Tracking](modules/30-prices.md) |
| FX-31 | [Shopping Records](modules/31-shopping.md) |
| FX-32 | [Developer Toolbox](modules/32-toolbox.md) |
| FX-33 | [GitHub Discovery](modules/33-github.md) |
| FX-34 | [Automation](modules/34-automation.md) |
| FX-35 | [Integrations / Webhooks / n8n](modules/35-integrations.md) |
| FX-36 | [Monitoring / Job Operations](modules/36-monitoring.md) |
| FX-37 | [Personal Assets](modules/37-assets.md) |
| FX-38 | [Digital Assets](modules/38-digital.md) |
| FX-39 | [Career / Jobs / Resumes](modules/39-career.md) |
| FX-40 | [Learning / Work Log](modules/40-learning.md) |

40 FX namespaces are not 40 mandatory assemblies/DbContexts. New action registration never auto-grants Admin access. Source Modules/owner/lifecycle/current policy and explicit operating contexts remain mandatory. Local pure-tool capability controls UI availability, not a user's ability to run an algorithm elsewhere.

## Current implementation rule

For the next implementation agent, filter the action catalog by M01 story/action lists under `docs/delivery/milestone-01` and by [effective action implementation status](09-effective-implementation-status-20260909.md). Do not implement actions solely because a module table lists them as `Resolved delegated`. Runtime availability still requires installed code, compatible migrations, health/readiness, current scope, module enablement, action authority, lifecycle checks and field projection.
