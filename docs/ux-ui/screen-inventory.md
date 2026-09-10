# Screen inventory — PO revision

202 screen definitions; counts include paused/historical scope, not ready screens. [Current PO decisions](../requirements/10-owner-decisions-20260907.md) and [exact actions](../action-catalog/06-screen-bindings.md). Routes are proposals.

| ID | Screen | Route | Scope |
| --- | --- | --- | --- |
| FX01-S01 | Register | `/register` | Current / per-action gate |
| FX01-S02 | Verify email | `/verify-email` | Current / per-action gate |
| FX01-S03 | Login | `/login` | Current / per-action gate |
| FX01-S04 | Forgot / Reset password | `/password/forgot; /password/reset` | Current / per-action gate |
| FX01-S05 | Profile | `/settings/profile` | Current / per-action gate |
| FX01-S06 | Security and sessions | `/settings/security` | Current / per-action gate |
| FX02-S01 | Users | `/admin/users` | Current / per-action gate |
| FX02-S02 | User detail | `/admin/users/:userId` | Current / per-action gate |
| FX02-S03 | Role assignment | `/admin/users/:userId/roles` | Current / per-action gate |
| FX02-S04 | Permission matrix | `/admin/users/:userId/permissions` | Current / per-action gate |
| FX02-S05 | Module grants | `/admin/users/:userId/modules` | Current / per-action gate |
| FX03-S01 | Module catalog | `/admin/modules` | Current / per-action gate |
| FX03-S02 | Module detail | `/admin/modules/:moduleCode` | Current / per-action gate |
| FX03-S03 | Enablement impact | `/admin/modules/:moduleCode/enablement` | Current / per-action gate |
| FX03-S04 | Registration defaults | `/admin/modules/registration-defaults` | Current / per-action gate |
| FX03-S05 | Module settings | `/admin/modules/:moduleCode/settings` | Current / per-action gate |
| FX04-S01 | Share dialog | `/sharing/new?resource=:opaqueId` | Current / per-action gate |
| FX04-S02 | Manage shares | `/settings/sharing` | Current / per-action gate |
| FX04-S03 | Shared resource | `/s/:token` | Current / per-action gate |
| FX04-S04 | Unavailable share | `/s/:token/unavailable` | Current / per-action gate |
| FX05-S01 | Grant support | `/settings/security/support/new` | Current / per-action gate |
| FX05-S02 | Security Center | `/settings/security/access` | Current / per-action gate |
| FX05-S03 | Support session | `/support/:accessSessionId/:moduleCode` | Current / per-action gate |
| FX05-S04 | Emergency entry | `/admin/emergency/new` | Current / per-action gate |
| FX05-S05 | Emergency session | `/emergency/:accessSessionId/:moduleCode` | Current / per-action gate |
| FX06-S01 | Notification inbox | `/notifications` | Current / per-action gate |
| FX06-S02 | Notification detail | `/notifications/:notificationId` | Current / per-action gate |
| FX06-S03 | Browser push permission | `/settings/notifications/push` | Current / per-action gate |
| FX07-S01 | Files library | `/files` | Current / per-action gate |
| FX07-S02 | Upload queue | `/files/uploads/:uploadId` | Current / per-action gate |
| FX07-S03 | Preview | `/files/:fileId` | Current / per-action gate |
| FX07-S04 | Attach file picker | `/files/picker?resource=:opaqueId` | Current / per-action gate |
| FX07-S05 | Replace / remove reference | `/files/:fileId/references` | Current / per-action gate |
| FX08-S01 | Trash | `/trash` | Current / per-action gate |
| FX08-S02 | Restore / purge preview | `/trash/operations/new` | Current / per-action gate |
| FX08-S03 | Resource activity | `/activity?resource=:opaqueId` | Current / per-action gate |
| FX08-S04 | Audit log | `/admin/audit` | Current / per-action gate |
| FX09-S01 | Settings home | `/settings` | Current / per-action gate |
| FX09-S02 | Preferences | `/settings/preferences` | Current / per-action gate |
| FX09-S03 | My modules | `/settings/modules` | Current / per-action gate |
| FX09-S04 | Unavailable module route | `/modules/:moduleCode/unavailable` | Current / per-action gate |
| FX10-S01 | Import | `/data/import?module=:moduleCode` | Current / per-action gate |
| FX10-S02 | Import preview | `/data/import/:operationId/preview` | Current / per-action gate |
| FX10-S03 | Export | `/data/export?module=:moduleCode` | Current / per-action gate |
| FX10-S04 | Operation status | `/data/operations/:operationId` | Current / per-action gate |
| FX10-S05 | System backup / restore | `/admin/recovery` | Current / per-action gate |
| FX11-S01 | Projects Grid / Table | `/projects` | Current / per-action gate |
| FX11-S02 | Project Create / Edit | `/projects/new; /projects/:projectId/edit` | Current / per-action gate |
| FX11-S03 | Project detail | `/projects/:projectId` | Current / per-action gate |
| FX11-S04 | Complete / Skip Project | `/projects/:projectId/close` | Current / per-action gate |
| FX11-S05 | Project history | `/projects/:projectId/history` | Current / per-action gate |
| FX12-S01 | Task Kanban | `/projects/:projectId/tasks?view=kanban` | Current / per-action gate |
| FX12-S02 | Task Table | `/projects/:projectId/tasks?view=table` | Current / per-action gate |
| FX12-S03 | Task Create / Edit | `/projects/:projectId/tasks/new; /tasks/:taskId/edit` | Current / per-action gate |
| FX12-S04 | Task detail | `/tasks/:taskId` | Current / per-action gate |
| FX12-S05 | Backward status reason | `/tasks/:taskId/transition` | Current / per-action gate |
| FX12-S06 | Task history / restore | `/tasks/:taskId/history` | Current / per-action gate |
| FX13-S01 | Day | `/calendar?view=day` | Current / per-action gate |
| FX13-S02 | Week | `/calendar?view=week` | Current / per-action gate |
| FX13-S03 | Month | `/calendar?view=month` | Current / per-action gate |
| FX13-S04 | Agenda | `/calendar?view=agenda` | Current / per-action gate |
| FX13-S05 | Manual Event form / detail | `/calendar/events/new; /calendar/events/:eventId` | Current / per-action gate |
| FX13-S06 | Task projection detail | `/calendar/tasks/:taskId` | Current / per-action gate |
| FX13-S07 | ICS Import / Export | `/calendar/data` | Current / per-action gate |
| FX14-S01 | Embedded reminder field | `/tasks/:taskId/edit#reminder; /calendar/events/:eventId#reminder` | Current / per-action gate |
| FX14-S02 | Exact reminder picker | `/reminders/picker?source=:opaqueId` | Current / per-action gate |
| FX14-S03 | Reminder delivery state | `/reminders/:reminderId` | Current / per-action gate |
| FX15-S01 | Daily planner | `/planner?view=day` | Current / per-action gate |
| FX15-S02 | Weekly planner | `/planner?view=week` | Current / per-action gate |
| FX15-S03 | Plan Task picker | `/planner/add` | Current / per-action gate |
| FX16-S01 | Goal list | `/goals` | Current / per-action gate |
| FX16-S02 | Goal create / edit | `/goals/new; /goals/:goalId/edit` | Current / per-action gate |
| FX16-S03 | Goal detail | `/goals/:goalId` | Current / per-action gate |
| FX16-S04 | Progress update | `/goals/:goalId/progress` | Current / per-action gate |
| FX17-S01 | Today habits | `/habits/today` | Current / per-action gate |
| FX17-S02 | Habit library | `/habits` | Current / per-action gate |
| FX17-S03 | Habit form | `/habits/new; /habits/:habitId/edit` | Current / per-action gate |
| FX17-S04 | Habit detail / history | `/habits/:habitId` | Current / per-action gate |
| FX18-S01 | Timer | `/time` | Current / per-action gate |
| FX18-S02 | Time entries | `/time/entries` | Current / per-action gate |
| FX18-S03 | Entry form | `/time/entries/new; /time/entries/:entryId/edit` | Current / per-action gate |
| FX18-S04 | Time reports / history | `/time/reports; /time/entries/:entryId/history` | Current / per-action gate |
| FX19-S01 | Focus setup | `/focus` | Current / per-action gate |
| FX19-S02 | Running phase | `/focus/sessions/:sessionId` | Current / per-action gate |
| FX19-S03 | Completion / conversion | `/focus/sessions/:sessionId/complete` | Current / per-action gate |
| FX19-S04 | Focus history | `/focus/history` | Current / per-action gate |
| FX20-S01 | Documents library | `/documents` | Current / per-action gate |
| FX20-S02 | Folder view | `/documents/folders/:folderId` | Current / per-action gate |
| FX20-S03 | Create Page | `/documents/new` | Current / per-action gate |
| FX20-S04 | Block editor | `/documents/pages/:pageId` | Current / per-action gate |
| FX20-S05 | Markdown editor | `/documents/pages/:pageId?editor=markdown` | Current / per-action gate |
| FX20-S06 | Page metadata / cover | `/documents/pages/:pageId/metadata` | Current / per-action gate |
| FX20-S07 | Page versions | `/documents/pages/:pageId/history` | Current / per-action gate |
| FX20-S08 | Archived pages | `/documents/archived` | Current / per-action gate |
| FX20-S09 | Archive / Unarchive tree preview | `/documents/pages/:pageId/archive` | Current / per-action gate |
| FX20-S10 | Document Trash and restore | `/trash?module=documents` | Current / per-action gate |
| FX21-S01 | Bookmark library / collection | `/bookmarks; /bookmarks/collections/:collectionId` | Current / per-action gate |
| FX21-S02 | Add / edit Bookmark | `/bookmarks/new; /bookmarks/:bookmarkId/edit` | Current / per-action gate |
| FX21-S03 | Bookmark detail | `/bookmarks/:bookmarkId` | Current / per-action gate |
| FX22-S01 | Snippet list | `/snippets` | Current / per-action gate |
| FX22-S02 | Snippet editor | `/snippets/new; /snippets/:snippetId/edit` | Current / per-action gate |
| FX22-S03 | Snippet detail / history | `/snippets/:snippetId; /snippets/:snippetId/history` | Current / per-action gate |
| FX23-S01 | Reading queue | `/read-later` | Local Bookmark-reference slice / per-action gate |
| FX23-S02 | Reader | `/read-later/:itemId` | Contract-gated; body reader not implemented |
| FX23-S03 | Unavailable reading sources | `/read-later?availability=unavailable` | Local safe snapshot projection / per-action gate |
| FX24-S01 | Tag management | `/organize/tags?namespace=:namespace` | Current / per-action gate |
| FX24-S02 | Collections | `/organize/collections` | Current / per-action gate |
| FX24-S03 | Collection detail | `/organize/collections/:collectionId` | Current / per-action gate |
| FX24-S04 | Templates | `/organize/templates` | Current / per-action gate |
| FX24-S05 | Template preview / apply | `/organize/templates/:templateId` | Current / per-action gate |
| FX25-S01 | Global Search | `/search` | Current / per-action gate |
| FX25-S02 | Command palette | `/command-palette` | Current / per-action gate |
| FX25-S03 | Favorites / recents | `/favorites; /recent` | Current / per-action gate |
| FX25-S04 | Saved searches | `/search/saved` | Current / per-action gate |
| FX26-S01 | Home dashboard | `/` | Current / per-action gate |
| FX26-S02 | Dashboard configuration | `/settings/dashboard` | Current / per-action gate |
| FX27-S01 | Finance overview | `/finance` | Historical advanced scope / Blocked |
| FX27-S02 | Accounts | `/finance/accounts` | Historical advanced scope / Blocked |
| FX27-S03 | Account form | `/finance/accounts/new; /finance/accounts/:accountId/edit` | Historical advanced scope / Blocked |
| FX27-S04 | Transactions ledger | `/finance/transactions` | Historical advanced scope / Blocked |
| FX27-S05 | Transaction / split form | `/finance/transactions/new; /finance/transactions/:transactionId` | Historical advanced scope / Blocked |
| FX27-S06 | Transfer form | `/finance/transfers/new` | Historical advanced scope / Blocked |
| FX27-S07 | Bills and payment allocation | `/finance/bills; /finance/bills/:billId` | Historical advanced scope / Blocked |
| FX27-S08 | Subscriptions / price history | `/finance/subscriptions; /finance/subscriptions/:subscriptionId` | Historical advanced scope / Blocked |
| FX27-S09 | Budgets | `/finance/budgets` | Historical advanced scope / Blocked |
| FX27-S10 | Savings and debts | `/finance/savings; /finance/debts` | Historical advanced scope / Blocked |
| FX27-S11 | Reports | `/finance/reports` | Historical advanced scope / Blocked |
| FX27-S12 | Finance CSV import / corrections | `/finance/data; /finance/transactions/:transactionId/correction` | Historical advanced scope / Blocked |
| FX28-S01 | Vault list | `/vault` | Current / per-action gate |
| FX28-S02 | Vault item detail | `/vault/items/:itemId` | Current / per-action gate |
| FX28-S03 | Vault create / edit | `/vault/items/new; /vault/items/:itemId/edit` | Current / per-action gate |
| FX28-S04 | Vault history | `/vault/items/:itemId/history` | Current / per-action gate |
| FX28-S05 | Password generator | `/vault/generator` | Current / per-action gate |
| FX28-S06 | Vault Trash / security policy | `/vault/trash; /settings/vault` | Current / per-action gate |
| FX29-S01 | Sources | `/news/sources` | Current / per-action gate |
| FX29-S02 | Follow source form | `/news/sources/new` | Current / per-action gate |
| FX29-S03 | Article list | `/news` | Current / per-action gate |
| FX29-S04 | Article reader | `/news/articles/:articleId` | Current / per-action gate |
| FX29-S05 | Topic watches / matches | `/news/watches; /news/watches/:watchId` | Current / per-action gate |
| FX30-S01 | Tracked products | `/shopping/prices` | Paused |
| FX30-S02 | Tracker create / edit | `/shopping/prices/new; /shopping/prices/:trackerId/edit` | Paused |
| FX30-S03 | Product price detail / chart | `/shopping/prices/:trackerId` | Paused |
| FX30-S04 | Alert rules | `/shopping/prices/:trackerId/alerts` | Paused |
| FX30-S05 | Provider / alert history | `/shopping/prices/:trackerId/history` | Paused |
| FX31-S01 | Wishlist | `/shopping/wishlist` | Current / per-action gate |
| FX31-S02 | Comparison | `/shopping/comparisons/:comparisonId` | Current / per-action gate |
| FX31-S03 | Orders | `/shopping/orders` | Current / per-action gate |
| FX31-S04 | Order form / detail | `/shopping/orders/new; /shopping/orders/:orderId` | Current / per-action gate |
| FX31-S05 | Sellers / merge | `/shopping/sellers; /shopping/sellers/:sellerId/merge` | Current / per-action gate |
| FX31-S06 | Warranty detail / claims | `/shopping/warranties/:warrantyId` | Current / per-action gate |
| FX31-S07 | Create Asset handoff | `/shopping/orders/:orderId/create-asset` | Current / per-action gate |
| FX32-S01 | Tool catalog | `/developer/tools` | Current / per-action gate |
| FX32-S02 | Tool workbench | `/developer/tools/:toolCode` | Current / per-action gate |
| FX32-S03 | Tool history / favorites | `/developer/tools/saved` | Current / per-action gate |
| FX32-S04 | Network request preview | `/developer/tools/:toolCode/network-preview` | Current / per-action gate |
| FX33-S01 | Discovery feed | `/developer/github` | Current / per-action gate |
| FX33-S02 | Repository detail | `/developer/github/repositories/:repositoryId` | Current / per-action gate |
| FX33-S03 | Saved queries / repositories | `/developer/github/saved` | Current / per-action gate |
| FX33-S04 | Ranking snapshots | `/developer/github/snapshots` | Current / per-action gate |
| FX34-S01 | Automations | `/automation` | Paused |
| FX34-S02 | Definition editor | `/automation/new; /automation/:definitionId/edit` | Paused |
| FX34-S03 | Definition detail / versions | `/automation/:definitionId` | Paused |
| FX34-S04 | Run history | `/automation/:definitionId/runs` | Paused |
| FX34-S05 | Run / step detail | `/automation/runs/:runId` | Paused |
| FX35-S01 | Connections | `/integrations` | Paused |
| FX35-S02 | Connection form | `/integrations/new; /integrations/:connectionId/edit` | Paused |
| FX35-S03 | Webhook detail / form | `/integrations/webhooks/:webhookId` | Paused |
| FX35-S04 | Delivery history | `/integrations/webhooks/:webhookId/deliveries` | Paused |
| FX35-S05 | Credential reference picker | `/integrations/credential-picker` | Paused |
| FX36-S01 | My monitors | `/monitoring` | Current / per-action gate |
| FX36-S02 | Monitor form | `/monitoring/new; /monitoring/:monitorId/edit` | Current / per-action gate |
| FX36-S03 | Monitor detail / incidents | `/monitoring/:monitorId` | Current / per-action gate |
| FX36-S04 | Admin jobs | `/admin/jobs` | Current / per-action gate |
| FX36-S05 | Job attempt detail | `/admin/jobs/:jobId` | Current / per-action gate |
| FX37-S01 | Asset inventory | `/assets/personal` | Current / per-action gate |
| FX37-S02 | Asset create / edit | `/assets/personal/new; /assets/personal/:assetId/edit` | Current / per-action gate |
| FX37-S03 | Asset detail | `/assets/personal/:assetId` | Current / per-action gate |
| FX37-S04 | Warranty / repair form | `/assets/personal/:assetId/service` | Current / per-action gate |
| FX37-S05 | Components / accessories | `/assets/personal/:assetId/components` | Current / per-action gate |
| FX37-S06 | Asset history / Trash | `/assets/personal/:assetId/history; /trash?module=assets` | Current / per-action gate |
| FX38-S01 | Digital asset list | `/assets/digital` | Current / per-action gate |
| FX38-S02 | Type-specific form | `/assets/digital/new; /assets/digital/:assetId/edit` | Current / per-action gate |
| FX38-S03 | Digital detail | `/assets/digital/:assetId` | Current / per-action gate |
| FX38-S04 | Renewal form / timeline | `/assets/digital/:assetId/renewals` | Current / per-action gate |
| FX39-S01 | Job pipeline | `/career/jobs` | Current / per-action gate |
| FX39-S02 | Job create / detail | `/career/jobs/new; /career/jobs/:jobId` | Current / per-action gate |
| FX39-S03 | Company directory / merge | `/career/companies; /career/companies/:companyId/merge` | Current / per-action gate |
| FX39-S04 | Linked personal Calendar events | `/career/jobs/:jobId/events` | Replaced by internal Calendar link |
| FX39-S05 | Resume library / upload | `/career/resumes` | Current / per-action gate |
| FX39-S06 | Resume version / share | `/career/resumes/:resumeId/versions/:version` | Current / per-action gate |
| FX39-S07 | Application timeline | `/career/jobs/:jobId/history` | Current / per-action gate |
| FX40-S01 | Learning overview | `/learning` | Current / per-action gate |
| FX40-S02 | Skills / evidence / merge | `/learning/skills; /learning/skills/:skillId` | Current / per-action gate |
| FX40-S03 | Course form / progress | `/learning/courses/new; /learning/courses/:courseId` | Current / per-action gate |
| FX40-S04 | Certifications / renewal | `/learning/certifications; /learning/certifications/:certificationId` | Current / per-action gate |
| FX40-S05 | Learning plans | `/learning/plans; /learning/plans/:planId` | Current / per-action gate |
| FX40-S06 | Work log form / list | `/learning/work-log; /learning/work-log/new` | Current / per-action gate |
| FX28-S07 | Owner recovery requests | `/vault/recovery` | Current / per-action gate |
| FX28-S08 | SuperAdmin recovery review | `/admin/vault-recovery/:requestId` | Current / per-action gate |
| FX27-S13 | Manual money records | `/finance/records` | Current / per-action gate |
| FX27-S14 | Manual money record form | `/finance/records/new; /finance/records/:id/edit` | Current / per-action gate |
| FX27-S15 | Manual categories | `/finance/categories` | Current / per-action gate |
