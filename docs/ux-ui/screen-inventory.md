# Cross-module screen inventory

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

197 concrete Screen IDs across40feature specifications. Routes proposed; same component may implement several surfaces. Screen profiles defined in [UX-15A](global/15-screen-contracts.md).

| Screen ID | Module | Screen | Route proposal | Profile | Primary action |
| --- | --- | --- | --- | --- | --- |
| FX01-S01 | [FX-01](modules/01-identity-profile.md) | Register | /register | FORM | Create account |
| FX01-S02 | [FX-01](modules/01-identity-profile.md) | Verify email | /verify-email | DETAIL | Verify / Resend email |
| FX01-S03 | [FX-01](modules/01-identity-profile.md) | Login | /login | FORM | Log in |
| FX01-S04 | [FX-01](modules/01-identity-profile.md) | Forgot / Reset password | /password/forgot; /password/reset | FORM | Send reset link / Reset password |
| FX01-S05 | [FX-01](modules/01-identity-profile.md) | Profile | /settings/profile | FORM | Save profile |
| FX01-S06 | [FX-01](modules/01-identity-profile.md) | Security and sessions | /settings/security | BROWSE | Review sessions |
| FX02-S01 | [FX-02](modules/02-users-roles-permissions.md) | Users | /admin/users | BROWSE | Open User |
| FX02-S02 | [FX-02](modules/02-users-roles-permissions.md) | User detail | /admin/users/:userId | DETAIL | Manage grants |
| FX02-S03 | [FX-02](modules/02-users-roles-permissions.md) | Role assignment | /admin/users/:userId/roles | FORM | Review change |
| FX02-S04 | [FX-02](modules/02-users-roles-permissions.md) | Permission matrix | /admin/users/:userId/permissions | ADMIN | Review permissions |
| FX02-S05 | [FX-02](modules/02-users-roles-permissions.md) | Module grants | /admin/users/:userId/modules | ADMIN | Review module changes |
| FX03-S01 | [FX-03](modules/03-module-platform.md) | Module catalog | /admin/modules | BROWSE | Open module |
| FX03-S02 | [FX-03](modules/03-module-platform.md) | Module detail | /admin/modules/:moduleCode | DETAIL | Enable / Disable |
| FX03-S03 | [FX-03](modules/03-module-platform.md) | Enablement impact | /admin/modules/:moduleCode/enablement | DIALOG | Confirm enable / Disable module |
| FX03-S04 | [FX-03](modules/03-module-platform.md) | Registration defaults | /admin/modules/registration-defaults | ADMIN | Save defaults |
| FX03-S05 | [FX-03](modules/03-module-platform.md) | Module settings | /admin/modules/:moduleCode/settings | FORM | Save settings |
| FX04-S01 | [FX-04](modules/04-sharing.md) | Share dialog | /sharing/new?resource=:opaqueId | DIALOG | Create link |
| FX04-S02 | [FX-04](modules/04-sharing.md) | Manage shares | /settings/sharing | BROWSE | Open share settings |
| FX04-S03 | [FX-04](modules/04-sharing.md) | Shared resource | /s/:token | DETAIL | Read content |
| FX04-S04 | [FX-04](modules/04-sharing.md) | Unavailable share | /s/:token/unavailable | DETAIL | Sign in / Go Home |
| FX05-S01 | [FX-05](modules/05-support-emergency.md) | Grant support | /settings/security/support/new | FORM | Grant access |
| FX05-S02 | [FX-05](modules/05-support-emergency.md) | Security Center | /settings/security/access | BROWSE | Grant support |
| FX05-S03 | [FX-05](modules/05-support-emergency.md) | Support session | /support/:accessSessionId/:moduleCode | DETAIL | End Session |
| FX05-S04 | [FX-05](modules/05-support-emergency.md) | Emergency entry | /admin/emergency/new | FORM | Start emergency access |
| FX05-S05 | [FX-05](modules/05-support-emergency.md) | Emergency session | /emergency/:accessSessionId/:moduleCode | DETAIL | End Session |
| FX06-S01 | [FX-06](modules/06-notifications.md) | Notification inbox | /notifications | BROWSE | Open notification |
| FX06-S02 | [FX-06](modules/06-notifications.md) | Notification detail | /notifications/:notificationId | DETAIL | Open source |
| FX06-S03 | [FX-06](modules/06-notifications.md) | Browser push permission | /settings/notifications/push | DETAIL | Enable browser notifications |
| FX07-S01 | [FX-07](modules/07-files.md) | Files library | /files | BROWSE | Upload files |
| FX07-S02 | [FX-07](modules/07-files.md) | Upload queue | /files/uploads/:uploadId | DETAIL | Open ready file |
| FX07-S03 | [FX-07](modules/07-files.md) | Preview | /files/:fileId | DETAIL | Download |
| FX07-S04 | [FX-07](modules/07-files.md) | Attach file picker | /files/picker?resource=:opaqueId | DIALOG | Attach selected |
| FX07-S05 | [FX-07](modules/07-files.md) | Replace / remove reference | /files/:fileId/references | DIALOG | Confirm replacement / Detach |
| FX08-S01 | [FX-08](modules/08-trash-activity-audit.md) | Trash | /trash | BROWSE | Preview restore |
| FX08-S02 | [FX-08](modules/08-trash-activity-audit.md) | Restore / purge preview | /trash/operations/new | DIALOG | Restore / Delete permanently |
| FX08-S03 | [FX-08](modules/08-trash-activity-audit.md) | Resource activity | /activity?resource=:opaqueId | HISTORY | Open relevant version |
| FX08-S04 | [FX-08](modules/08-trash-activity-audit.md) | Audit log | /admin/audit | BROWSE | Inspect audit event |
| FX09-S01 | [FX-09](modules/09-settings-app-shell.md) | Settings home | /settings | BROWSE | Open section |
| FX09-S02 | [FX-09](modules/09-settings-app-shell.md) | Preferences | /settings/preferences | FORM | Save preferences |
| FX09-S03 | [FX-09](modules/09-settings-app-shell.md) | My modules | /settings/modules | BROWSE | Open module settings |
| FX09-S04 | [FX-09](modules/09-settings-app-shell.md) | Unavailable module route | /modules/:moduleCode/unavailable | DETAIL | Go Home |
| FX10-S01 | [FX-10](modules/10-import-export-backup.md) | Import | /data/import?module=:moduleCode | FORM | Validate file |
| FX10-S02 | [FX-10](modules/10-import-export-backup.md) | Import preview | /data/import/:operationId/preview | BROWSE | Import valid rows |
| FX10-S03 | [FX-10](modules/10-import-export-backup.md) | Export | /data/export?module=:moduleCode | FORM | Generate export |
| FX10-S04 | [FX-10](modules/10-import-export-backup.md) | Operation status | /data/operations/:operationId | DETAIL | Download result / View report |
| FX10-S05 | [FX-10](modules/10-import-export-backup.md) | System backup / restore | /admin/recovery | ADMIN | Review recovery plan |
| FX11-S01 | [FX-11](modules/11-projects.md) | Projects Grid / Table | /projects | BROWSE | New Project |
| FX11-S02 | [FX-11](modules/11-projects.md) | Project Create / Edit | /projects/new; /projects/:projectId/edit | FORM | Save Project |
| FX11-S03 | [FX-11](modules/11-projects.md) | Project detail | /projects/:projectId | DETAIL | Add Task while active |
| FX11-S04 | [FX-11](modules/11-projects.md) | Complete / Skip Project | /projects/:projectId/close | DIALOG | Complete Project / Skip Project |
| FX11-S05 | [FX-11](modules/11-projects.md) | Project history | /projects/:projectId/history | HISTORY | Inspect version |
| FX12-S01 | [FX-12](modules/12-tasks.md) | Task Kanban | /projects/:projectId/tasks?view=kanban | BROWSE | Add Task in NotStarted/InProgress |
| FX12-S02 | [FX-12](modules/12-tasks.md) | Task Table | /projects/:projectId/tasks?view=table | BROWSE | Add Task |
| FX12-S03 | [FX-12](modules/12-tasks.md) | Task Create / Edit | /projects/:projectId/tasks/new; /tasks/:taskId/edit | FORM | Save Task |
| FX12-S04 | [FX-12](modules/12-tasks.md) | Task detail | /tasks/:taskId | DETAIL | Edit while parent active |
| FX12-S05 | [FX-12](modules/12-tasks.md) | Backward status reason | /tasks/:taskId/transition | DIALOG | Confirm transition |
| FX12-S06 | [FX-12](modules/12-tasks.md) | Task history / restore | /tasks/:taskId/history | HISTORY | Restore selected version |
| FX13-S01 | [FX-13](modules/13-calendar.md) | Day | /calendar?view=day | CALENDAR | New Event |
| FX13-S02 | [FX-13](modules/13-calendar.md) | Week | /calendar?view=week | CALENDAR | New Event |
| FX13-S03 | [FX-13](modules/13-calendar.md) | Month | /calendar?view=month | CALENDAR | New Event |
| FX13-S04 | [FX-13](modules/13-calendar.md) | Agenda | /calendar?view=agenda | BROWSE | New Event |
| FX13-S05 | [FX-13](modules/13-calendar.md) | Manual Event form / detail | /calendar/events/new; /calendar/events/:eventId | FORM | Save / Complete when Scheduled |
| FX13-S06 | [FX-13](modules/13-calendar.md) | Task projection detail | /calendar/tasks/:taskId | DETAIL | Open Task |
| FX13-S07 | [FX-13](modules/13-calendar.md) | ICS Import / Export | /calendar/data | FORM | Validate import / Generate export |
| FX14-S01 | [FX-14](modules/14-reminders.md) | Embedded reminder field | /tasks/:taskId/edit#reminder; /calendar/events/:eventId#reminder | FORM | Apply within source Save |
| FX14-S02 | [FX-14](modules/14-reminders.md) | Exact reminder picker | /reminders/picker?source=:opaqueId | DIALOG | Use this time |
| FX14-S03 | [FX-14](modules/14-reminders.md) | Reminder delivery state | /reminders/:reminderId | DETAIL | Open source |
| FX15-S01 | [FX-15](modules/15-planner.md) | Daily planner | /planner?view=day | BROWSE | Add existing Task |
| FX15-S02 | [FX-15](modules/15-planner.md) | Weekly planner | /planner?view=week | BROWSE | Plan Task on day |
| FX15-S03 | [FX-15](modules/15-planner.md) | Plan Task picker | /planner/add | DIALOG | Add to plan |
| FX16-S01 | [FX-16](modules/16-goals.md) | Goal list | /goals | BROWSE | New Goal |
| FX16-S02 | [FX-16](modules/16-goals.md) | Goal create / edit | /goals/new; /goals/:goalId/edit | FORM | Save Goal |
| FX16-S03 | [FX-16](modules/16-goals.md) | Goal detail | /goals/:goalId | DETAIL | Update progress |
| FX16-S04 | [FX-16](modules/16-goals.md) | Progress update | /goals/:goalId/progress | DIALOG | Record progress |
| FX17-S01 | [FX-17](modules/17-habits.md) | Today habits | /habits/today | BROWSE | Record progress |
| FX17-S02 | [FX-17](modules/17-habits.md) | Habit library | /habits | BROWSE | New Habit |
| FX17-S03 | [FX-17](modules/17-habits.md) | Habit form | /habits/new; /habits/:habitId/edit | FORM | Save Habit |
| FX17-S04 | [FX-17](modules/17-habits.md) | Habit detail / history | /habits/:habitId | DETAIL | Record selected-day progress |
| FX18-S01 | [FX-18](modules/18-time-tracking.md) | Timer | /time | WORKBENCH | Start / Stop |
| FX18-S02 | [FX-18](modules/18-time-tracking.md) | Time entries | /time/entries | BROWSE | Manual entry |
| FX18-S03 | [FX-18](modules/18-time-tracking.md) | Entry form | /time/entries/new; /time/entries/:entryId/edit | FORM | Save entry |
| FX18-S04 | [FX-18](modules/18-time-tracking.md) | Time reports / history | /time/reports; /time/entries/:entryId/history | HISTORY | Inspect details |
| FX19-S01 | [FX-19](modules/19-pomodoro-focus.md) | Focus setup | /focus | FORM | Start Focus |
| FX19-S02 | [FX-19](modules/19-pomodoro-focus.md) | Running phase | /focus/sessions/:sessionId | WORKBENCH | Pause / Resume |
| FX19-S03 | [FX-19](modules/19-pomodoro-focus.md) | Completion / conversion | /focus/sessions/:sessionId/complete | DETAIL | Start next phase |
| FX19-S04 | [FX-19](modules/19-pomodoro-focus.md) | Focus history | /focus/history | BROWSE | Inspect session |
| FX20-S01 | [FX-20](modules/20-documents.md) | Documents library | /documents | BROWSE | Create Page |
| FX20-S02 | [FX-20](modules/20-documents.md) | Folder view | /documents/folders/:folderId | BROWSE | Create Page here |
| FX20-S03 | [FX-20](modules/20-documents.md) | Create Page | /documents/new | FORM | Create Page and Save version1 |
| FX20-S04 | [FX-20](modules/20-documents.md) | Block editor | /documents/pages/:pageId | EDITOR | Save |
| FX20-S05 | [FX-20](modules/20-documents.md) | Markdown editor | /documents/pages/:pageId?editor=markdown | EDITOR | Save |
| FX20-S06 | [FX-20](modules/20-documents.md) | Page metadata / cover | /documents/pages/:pageId/metadata | FORM | Apply to draft then Save Page |
| FX20-S07 | [FX-20](modules/20-documents.md) | Page versions | /documents/pages/:pageId/history | HISTORY | Restore as new version |
| FX20-S08 | [FX-20](modules/20-documents.md) | Archived pages | /documents/archived | BROWSE | Open readonly page |
| FX20-S09 | [FX-20](modules/20-documents.md) | Archive / Unarchive tree preview | /documents/pages/:pageId/archive | DIALOG | Archive / Unarchive |
| FX20-S10 | [FX-20](modules/20-documents.md) | Document Trash and restore | /trash?module=documents | BROWSE | Preview restore |
| FX21-S01 | [FX-21](modules/21-bookmarks.md) | Bookmark library / collection | /bookmarks; /bookmarks/collections/:collectionId | BROWSE | Add Bookmark |
| FX21-S02 | [FX-21](modules/21-bookmarks.md) | Add / edit Bookmark | /bookmarks/new; /bookmarks/:bookmarkId/edit | FORM | Save Bookmark |
| FX21-S03 | [FX-21](modules/21-bookmarks.md) | Bookmark detail | /bookmarks/:bookmarkId | DETAIL | Open original |
| FX22-S01 | [FX-22](modules/22-snippets.md) | Snippet list | /snippets | BROWSE | New Snippet |
| FX22-S02 | [FX-22](modules/22-snippets.md) | Snippet editor | /snippets/new; /snippets/:snippetId/edit | EDITOR | Save |
| FX22-S03 | [FX-22](modules/22-snippets.md) | Snippet detail / history | /snippets/:snippetId; /snippets/:snippetId/history | HISTORY | Copy code / Restore selected version |
| FX23-S01 | [FX-23](modules/23-read-later.md) | Reading queue | /read-later | BROWSE | Open reader |
| FX23-S02 | [FX-23](modules/23-read-later.md) | Reader | /read-later/:itemId | DETAIL | Mark Read |
| FX23-S03 | [FX-23](modules/23-read-later.md) | Archived reading | /read-later/archived | BROWSE | Unarchive selected |
| FX24-S01 | [FX-24](modules/24-organization-tags-templates.md) | Tag management | /organize/tags?namespace=:namespace | BROWSE | New Tag |
| FX24-S02 | [FX-24](modules/24-organization-tags-templates.md) | Collections | /organize/collections | BROWSE | New Collection |
| FX24-S03 | [FX-24](modules/24-organization-tags-templates.md) | Collection detail | /organize/collections/:collectionId | DETAIL | Add existing resources |
| FX24-S04 | [FX-24](modules/24-organization-tags-templates.md) | Templates | /organize/templates | BROWSE | New Template |
| FX24-S05 | [FX-24](modules/24-organization-tags-templates.md) | Template preview / apply | /organize/templates/:templateId | DETAIL | Use Template |
| FX25-S01 | [FX-25](modules/25-search-favorites-command-palette.md) | Global Search | /search | BROWSE | Open result |
| FX25-S02 | [FX-25](modules/25-search-favorites-command-palette.md) | Command palette | /command-palette | DIALOG | Run selected command / Open result |
| FX25-S03 | [FX-25](modules/25-search-favorites-command-palette.md) | Favorites / recents | /favorites; /recent | BROWSE | Open resource |
| FX25-S04 | [FX-25](modules/25-search-favorites-command-palette.md) | Saved searches | /search/saved | BROWSE | Run saved search |
| FX26-S01 | [FX-26](modules/26-dashboard.md) | Home dashboard | / | DASHBOARD | Open attention item |
| FX26-S02 | [FX-26](modules/26-dashboard.md) | Dashboard configuration | /settings/dashboard | FORM | Save layout |
| FX27-S01 | [FX-27](modules/27-finance.md) | Finance overview | /finance | DASHBOARD | Record transaction |
| FX27-S02 | [FX-27](modules/27-finance.md) | Accounts | /finance/accounts | BROWSE | New Account |
| FX27-S03 | [FX-27](modules/27-finance.md) | Account form | /finance/accounts/new; /finance/accounts/:accountId/edit | FORM | Save Account |
| FX27-S04 | [FX-27](modules/27-finance.md) | Transactions ledger | /finance/transactions | BROWSE | New transaction |
| FX27-S05 | [FX-27](modules/27-finance.md) | Transaction / split form | /finance/transactions/new; /finance/transactions/:transactionId | FORM | Review and post — Q-05 |
| FX27-S06 | [FX-27](modules/27-finance.md) | Transfer form | /finance/transfers/new | FORM | Confirm transfer — Q-05 |
| FX27-S07 | [FX-27](modules/27-finance.md) | Bills and payment allocation | /finance/bills; /finance/bills/:billId | DETAIL | Record / link payment |
| FX27-S08 | [FX-27](modules/27-finance.md) | Subscriptions / price history | /finance/subscriptions; /finance/subscriptions/:subscriptionId | DETAIL | Record price/renewal change |
| FX27-S09 | [FX-27](modules/27-finance.md) | Budgets | /finance/budgets | FORM | Save budget — Q-05 |
| FX27-S10 | [FX-27](modules/27-finance.md) | Savings and debts | /finance/savings; /finance/debts | DETAIL | Record progress / payment — Q-05 |
| FX27-S11 | [FX-27](modules/27-finance.md) | Reports | /finance/reports | DASHBOARD | Inspect contributing transactions |
| FX27-S12 | [FX-27](modules/27-finance.md) | Finance CSV import / corrections | /finance/data; /finance/transactions/:transactionId/correction | DIALOG | Apply validated import / correction — Q-05 |
| FX28-S01 | [FX-28](modules/28-vault.md) | Vault list | /vault | BROWSE | New Vault item |
| FX28-S02 | [FX-28](modules/28-vault.md) | Vault item detail | /vault/items/:itemId | DETAIL | Copy selected secret after authorization |
| FX28-S03 | [FX-28](modules/28-vault.md) | Vault create / edit | /vault/items/new; /vault/items/:itemId/edit | FORM | Save encrypted item |
| FX28-S04 | [FX-28](modules/28-vault.md) | Vault history | /vault/items/:itemId/history | HISTORY | Restore as new encrypted version |
| FX28-S05 | [FX-28](modules/28-vault.md) | Password generator | /vault/generator | WORKBENCH | Generate |
| FX28-S06 | [FX-28](modules/28-vault.md) | Vault Trash / security policy | /vault/trash; /settings/vault | BROWSE | Preview Restore |
| FX29-S01 | [FX-29](modules/29-news-feeds.md) | Sources | /news/sources | BROWSE | Follow feed |
| FX29-S02 | [FX-29](modules/29-news-feeds.md) | Follow source form | /news/sources/new | FORM | Follow source |
| FX29-S03 | [FX-29](modules/29-news-feeds.md) | Article list | /news | BROWSE | Open reader |
| FX29-S04 | [FX-29](modules/29-news-feeds.md) | Article reader | /news/articles/:articleId | DETAIL | Mark read / Open original |
| FX29-S05 | [FX-29](modules/29-news-feeds.md) | Topic watches / matches | /news/watches; /news/watches/:watchId | FORM | Save watch |
| FX30-S01 | [FX-30](modules/30-price-tracking.md) | Tracked products | /shopping/prices | BROWSE | Track product |
| FX30-S02 | [FX-30](modules/30-price-tracking.md) | Tracker create / edit | /shopping/prices/new; /shopping/prices/:trackerId/edit | FORM | Save tracker |
| FX30-S03 | [FX-30](modules/30-price-tracking.md) | Product price detail / chart | /shopping/prices/:trackerId | DETAIL | Manage alerts |
| FX30-S04 | [FX-30](modules/30-price-tracking.md) | Alert rules | /shopping/prices/:trackerId/alerts | FORM | Save alert |
| FX30-S05 | [FX-30](modules/30-price-tracking.md) | Provider / alert history | /shopping/prices/:trackerId/history | BROWSE | Inspect evidence |
| FX31-S01 | [FX-31](modules/31-shopping-records.md) | Wishlist | /shopping/wishlist | BROWSE | Add wishlist item |
| FX31-S02 | [FX-31](modules/31-shopping-records.md) | Comparison | /shopping/comparisons/:comparisonId | DETAIL | Add/remove comparison item |
| FX31-S03 | [FX-31](modules/31-shopping-records.md) | Orders | /shopping/orders | BROWSE | New Order |
| FX31-S04 | [FX-31](modules/31-shopping-records.md) | Order form / detail | /shopping/orders/new; /shopping/orders/:orderId | FORM | Save Order |
| FX31-S05 | [FX-31](modules/31-shopping-records.md) | Sellers / merge | /shopping/sellers; /shopping/sellers/:sellerId/merge | BROWSE | New Seller / Review merge |
| FX31-S06 | [FX-31](modules/31-shopping-records.md) | Warranty detail / claims | /shopping/warranties/:warrantyId | FORM | Save warranty |
| FX31-S07 | [FX-31](modules/31-shopping-records.md) | Create Asset handoff | /shopping/orders/:orderId/create-asset | DIALOG | Open Asset draft |
| FX32-S01 | [FX-32](modules/32-developer-toolbox.md) | Tool catalog | /developer/tools | BROWSE | Open tool |
| FX32-S02 | [FX-32](modules/32-developer-toolbox.md) | Tool workbench | /developer/tools/:toolCode | WORKBENCH | Run tool |
| FX32-S03 | [FX-32](modules/32-developer-toolbox.md) | Tool history / favorites | /developer/tools/saved | BROWSE | Open tool |
| FX32-S04 | [FX-32](modules/32-developer-toolbox.md) | Network request preview | /developer/tools/:toolCode/network-preview | DIALOG | Run approved request |
| FX33-S01 | [FX-33](modules/33-github-discovery.md) | Discovery feed | /developer/github | BROWSE | Open repository |
| FX33-S02 | [FX-33](modules/33-github-discovery.md) | Repository detail | /developer/github/repositories/:repositoryId | DETAIL | Open on GitHub |
| FX33-S03 | [FX-33](modules/33-github-discovery.md) | Saved queries / repositories | /developer/github/saved | BROWSE | Run query / Open repository |
| FX33-S04 | [FX-33](modules/33-github-discovery.md) | Ranking snapshots | /developer/github/snapshots | BROWSE | Compare compatible snapshots |
| FX34-S01 | [FX-34](modules/34-automation.md) | Automations | /automation | BROWSE | New automation |
| FX34-S02 | [FX-34](modules/34-automation.md) | Definition editor | /automation/new; /automation/:definitionId/edit | FORM | Validate and Save version |
| FX34-S03 | [FX-34](modules/34-automation.md) | Definition detail / versions | /automation/:definitionId | DETAIL | Enable / Run if approved Ready |
| FX34-S04 | [FX-34](modules/34-automation.md) | Run history | /automation/:definitionId/runs | BROWSE | Inspect run |
| FX34-S05 | [FX-34](modules/34-automation.md) | Run / step detail | /automation/runs/:runId | DETAIL | Retry eligible failed work |
| FX35-S01 | [FX-35](modules/35-integrations-webhooks.md) | Connections | /integrations | BROWSE | New connection |
| FX35-S02 | [FX-35](modules/35-integrations-webhooks.md) | Connection form | /integrations/new; /integrations/:connectionId/edit | FORM | Save connection |
| FX35-S03 | [FX-35](modules/35-integrations-webhooks.md) | Webhook detail / form | /integrations/webhooks/:webhookId | FORM | Save webhook |
| FX35-S04 | [FX-35](modules/35-integrations-webhooks.md) | Delivery history | /integrations/webhooks/:webhookId/deliveries | BROWSE | Inspect delivery |
| FX35-S05 | [FX-35](modules/35-integrations-webhooks.md) | Credential reference picker | /integrations/credential-picker | DIALOG | Use reference |
| FX36-S01 | [FX-36](modules/36-monitoring-jobs.md) | My monitors | /monitoring | BROWSE | New monitor |
| FX36-S02 | [FX-36](modules/36-monitoring-jobs.md) | Monitor form | /monitoring/new; /monitoring/:monitorId/edit | FORM | Save monitor |
| FX36-S03 | [FX-36](modules/36-monitoring-jobs.md) | Monitor detail / incidents | /monitoring/:monitorId | DETAIL | Inspect incident |
| FX36-S04 | [FX-36](modules/36-monitoring-jobs.md) | Admin jobs | /admin/jobs | ADMIN | Inspect job |
| FX36-S05 | [FX-36](modules/36-monitoring-jobs.md) | Job attempt detail | /admin/jobs/:jobId | DETAIL | Retry safe job |
| FX37-S01 | [FX-37](modules/37-personal-assets.md) | Asset inventory | /assets/personal | BROWSE | New Asset |
| FX37-S02 | [FX-37](modules/37-personal-assets.md) | Asset create / edit | /assets/personal/new; /assets/personal/:assetId/edit | FORM | Save Asset |
| FX37-S03 | [FX-37](modules/37-personal-assets.md) | Asset detail | /assets/personal/:assetId | DETAIL | Edit / Record state |
| FX37-S04 | [FX-37](modules/37-personal-assets.md) | Warranty / repair form | /assets/personal/:assetId/service | FORM | Save service record |
| FX37-S05 | [FX-37](modules/37-personal-assets.md) | Components / accessories | /assets/personal/:assetId/components | BROWSE | Add component / Link Asset |
| FX37-S06 | [FX-37](modules/37-personal-assets.md) | Asset history / Trash | /assets/personal/:assetId/history; /trash?module=assets | HISTORY | Inspect version / Preview restore |
| FX38-S01 | [FX-38](modules/38-digital-assets.md) | Digital asset list | /assets/digital | BROWSE | New Digital Asset |
| FX38-S02 | [FX-38](modules/38-digital-assets.md) | Type-specific form | /assets/digital/new; /assets/digital/:assetId/edit | FORM | Save metadata |
| FX38-S03 | [FX-38](modules/38-digital-assets.md) | Digital detail | /assets/digital/:assetId | DETAIL | Record renewal |
| FX38-S04 | [FX-38](modules/38-digital-assets.md) | Renewal form / timeline | /assets/digital/:assetId/renewals | FORM | Record renewal |
| FX39-S01 | [FX-39](modules/39-career-jobs-resume.md) | Job pipeline | /career/jobs | BROWSE | Add Job |
| FX39-S02 | [FX-39](modules/39-career-jobs-resume.md) | Job create / detail | /career/jobs/new; /career/jobs/:jobId | FORM | Save Job |
| FX39-S03 | [FX-39](modules/39-career-jobs-resume.md) | Company directory / merge | /career/companies; /career/companies/:companyId/merge | BROWSE | New Company / Review merge |
| FX39-S04 | [FX-39](modules/39-career-jobs-resume.md) | Interview form / detail | /career/interviews/:interviewId | FORM | Save / Complete / Cancel Interview |
| FX39-S05 | [FX-39](modules/39-career-jobs-resume.md) | Resume library / upload | /career/resumes | BROWSE | New Resume / Upload version |
| FX39-S06 | [FX-39](modules/39-career-jobs-resume.md) | Resume version / share | /career/resumes/:resumeId/versions/:version | DETAIL | Share this version if approved |
| FX39-S07 | [FX-39](modules/39-career-jobs-resume.md) | Application timeline | /career/jobs/:jobId/history | HISTORY | Inspect evidence |
| FX40-S01 | [FX-40](modules/40-learning.md) | Learning overview | /learning | DASHBOARD | Add Course / WorkLog |
| FX40-S02 | [FX-40](modules/40-learning.md) | Skills / evidence / merge | /learning/skills; /learning/skills/:skillId | FORM | Save Skill / Record evidence |
| FX40-S03 | [FX-40](modules/40-learning.md) | Course form / progress | /learning/courses/new; /learning/courses/:courseId | FORM | Save / Record progress |
| FX40-S04 | [FX-40](modules/40-learning.md) | Certifications / renewal | /learning/certifications; /learning/certifications/:certificationId | FORM | Save certification / Record renewal |
| FX40-S05 | [FX-40](modules/40-learning.md) | Learning plans | /learning/plans; /learning/plans/:planId | DETAIL | Add existing resource |
| FX40-S06 | [FX-40](modules/40-learning.md) | Work log form / list | /learning/work-log; /learning/work-log/new | FORM | Save log |
