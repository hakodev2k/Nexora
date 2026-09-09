# Current implementation slices and Release1 scope

2026-09-09 · **M01 + backend/frontend scaffold + local scripts are approved for implementation by `DEC-20260909-001`.** Active outside that boundary still means eligible for refinement/implementation consideration after later approval, not live or guaranteed Ready. M01 is an internal foundation slice; Phase1 and R1 are larger.

## Release rule

All modules with current requirements remain the committed R1 catalog, subject to explicit PO exclusions already recorded (such as Project/Task standalone import/export). FX30/34/35 are Paused, not canceled or moved to R2. R1 complete only when each committed capability has passing acceptance/evidence or PO explicitly revises its commitment. Completing active items alone does not close R1.

## Current approved implementation boundary

Implementation is currently approved only for:

- M01 stories S00–S11.
- Backend scaffold required for M01.
- Frontend scaffold required for M01.
- Local development scripts/runbook artifacts required for M01 evidence.
- Synthetic local fixtures, local SQL Server, optional Redis cache and captured/simulated notification adapters.

Implementation is not currently approved for:

- Production deployment, public launch, paid provider provisioning, domains, production secrets or production data.
- Full Phase1/R1 outside the M01 slice.
- Business modules not listed as part of M01.
- FX30 Price Tracking, FX34 Automation/Scheduler/Workflows or FX35 Integrations/Webhooks/n8n.
- Real OAuth/write/provider mutation/payment/executable third-party integration.

## Forty feature groups

| FX | Current feature | Current capability boundary | M01 |
| --- | --- | --- | --- |
| FX-01 | [Identity, Registration và Profile](../features/01-identity-and-profile.md) | Password/email/profile active; deleted-account policy resolved same-owner/no email reuse; TOTP recovery policy resolved with recovery codes, enrollment/recovery outside M01 | Approved subset only; exact story/action list in M01 |
| FX-02 | [Users, Roles và Action Permissions](../features/02-users-roles-and-permissions.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Approved subset only; exact story/action list in M01 |
| FX-03 | [Module Platform và Module Manager](../features/03-module-platform.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Approved subset only; exact story/action list in M01 |
| FX-04 | [Read-only Sharing Engine](../features/04-read-only-sharing.md) | Read-only sharing core; sensitive projection policy resolved as allowlist/default-hidden but module field contracts still required | Not in M01 |
| FX-05 | [Support, Emergency Access và Security Center](../features/05-support-emergency-and-security-center.md) | Consent/emergency core; Support safe metadata only and Emergency read-only/no export/copy secret | Not in M01 |
| FX-06 | [Notification Center và Delivery](../features/06-notification-center.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Approved subset only; exact story/action list in M01 |
| FX-07 | [Files, Uploads và Attachments](../features/07-files-and-attachments.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-08 | [Trash, Activity và Audit](../features/08-trash-activity-and-audit.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Approved subset only; exact story/action list in M01 |
| FX-09 | [Settings và Application Shell](../features/09-settings-and-app-shell.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Approved subset only; exact story/action list in M01 |
| FX-10 | [Import, Export và Backup/Restore](../features/10-import-export-and-backup.md) | DOCX/MD + Calendar ICS; Vault owner portability policy resolved hybrid/no-operator-plaintext, crypto/format contracts still required | Not in M01 |
| FX-11 | [Projects](../features/11-projects.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-12 | [Tasks, Kanban và Table](../features/12-tasks.md) | Flat Task core approved for first Productivity slice; recurrence/subtask/snooze/standalone reminders/Task attachments remain gated | Not in M01 |
| FX-13 | [Calendar, Personal Events và ICS](../features/13-calendar.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-14 | [Reminders và Due Scheduling](../features/14-reminders-and-scheduling.md) | Single task/event reminder core; independent extensions remain gated | Not in M01 |
| FX-15 | [Daily và Weekly Planner](../features/15-planner.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-16 | [Goals và Targets](../features/16-goals.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-17 | [Habit Tracker](../features/17-habits.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-18 | [Time Tracking](../features/18-time-tracking.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-19 | [Pomodoro và Focus](../features/19-pomodoro.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-20 | [Documents, Note và Knowledge Pages](../features/20-documents.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-21 | [Bookmarks](../features/21-bookmarks.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-22 | [Code Snippets](../features/22-snippets.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-23 | [Read Later](../features/23-read-later.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-24 | [Tags, Collections và Templates](../features/24-organization-and-templates.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-25 | [Search, Saved Search, Favorites và Command Palette](../features/25-search-favorites-and-command-palette.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-26 | [Dashboard và Widgets](../features/26-dashboard.md) | Current owner-only scope; per-action lifecycle/permission gates apply | Not in M01 |
| FX-27 | [Finance — current basic manual records](../features/27-finance.md) | Initial Finance scope = manual category/amount/explicit currency/date/optional note; advanced ledger/budget/debt/interest/FX/transfers gated | Not in M01 |
| FX-28 | [Vault](../features/28-vault.md) | Hybrid recoverable Vault policy approved; no operator plaintext; portability/key design/security evidence still required | Not in M01 |
| FX-29 | [News, RSS và Topic Watch](../features/29-news-and-feeds.md) | Internal stored reader; read-only public outbound allowed only after slice contract and SSRF/provider guards | Not in M01 |
| FX-30 | [Shopee Price Tracking](../features/30-shopee-price-tracking.md) | Paused by Product Owner; no worker/auto-enable; not moved to R2 | Not in M01 |
| FX-31 | [Wishlist, Comparison, Orders, Sellers và Warranty](../features/31-shopping-records.md) | Current owner-only scope; sensitive projection allowlists required where applicable | Not in M01 |
| FX-32 | [Developer Toolbox](../features/32-developer-toolbox.md) | Local tools active; network tools held under integration/outbound boundary | Not in M01 |
| FX-33 | [GitHub Discovery](../features/33-github-discovery.md) | Internal stored metadata; read-only public outbound allowed only after slice contract and network guards | Not in M01 |
| FX-34 | [Automation, Scheduler và Workflows](../features/34-automation-and-scheduler.md) | Paused by Product Owner; core platform jobs unaffected; not moved to R2 | Not in M01 |
| FX-35 | [Integrations, Webhooks và n8n](../features/35-integrations-webhooks-and-n8n.md) | Paused by Product Owner; foundation email/push distinct; not moved to R2 | Not in M01 |
| FX-36 | [Monitoring và Job Operations](../features/36-monitoring-and-job-operations.md) | Admin core jobs active; HTTP probing allowed only after owner target config and network guard contract | Not in M01 |
| FX-37 | [Personal Assets, Inventory và Devices](../features/37-personal-assets.md) | Current owner-only scope; sensitive projection allowlists required where applicable | Not in M01 |
| FX-38 | [Domains, Hosting, VPS, Certificates, Licenses và Services](../features/38-digital-assets.md) | Metadata management; provider inspection held under outbound/provider gates | Not in M01 |
| FX-39 | [Career, Companies, Calendar links và Resumes](../features/39-career-and-resumes.md) | Jobs/resume DOCX/MD/internal Calendar links; sensitive projection allowlists required where applicable | Not in M01 |
| FX-40 | [Skills, Courses, Certifications, Learning Plan và Work Log](../features/40-learning-and-work-log.md) | Owner learning core; sensitive projection allowlists required where applicable | Not in M01 |

## Candidate delivery order — delegated sequencing, not business scope change

M01 Identity/access/delivery foundation and scaffold → M02 remaining platform lifecycle/files/sharing/support/notifications → Productivity flat core → Knowledge/Documents/Search → Finance basic/Vault after scoped contracts → other domain slices. Price/Automation/Integrations have no scheduled execution while paused. Each later slice requires its own API/DB/UX/acceptance package and explicit implementation approval. M01 does not masquerade as a full Phase1 or a public release.

## Runtime availability algorithm

Installed + compatible release/migrations/health + active delivery scope + system enabled + user enabled + action/context + resource lifecycle + field projection. Paused or decision-blocked wins over Allow/defaults. A module not shipped in the current build is unavailable, not a placeholder with fake data. Re-enable Disabled requires validated readiness, not only a checkbox. Registry/catalog contracts remain extensible; no hard-coded forty-module ceiling.

## Controlled boundaries

No Team Workspace, no realtime collaboration, no executable marketplace, no Google OAuth, no external-open navigation. Backend read-only public outbound for News/GitHub/Monitoring is allowed only after contract and guard implementation for the relevant slice; no silent outbound launch in M01. Core Email/Web Push delivery honors existing PO notification rules and does not resume paused user Integrations. [PO implementation-readiness decisions](../requirements/11-owner-decisions-20260909-implementation-readiness.md), [major proposals](02-decision-proposals.md) and [M01 gates](milestone-01/06-readiness-and-evidence.md).
