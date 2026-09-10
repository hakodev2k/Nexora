# Screen/action bindings — v1.1

202 screen definitions, including5new manual-finance/recovery screens. Historical/Paused rows are not active scope. Deprecated interview actions removed from live bindings; Calendar link replaces FX39-S04. Route strings are proposals. [PO authority](../requirements/10-owner-decisions-20260907.md).

| Screen / purpose | Route | Scope | Action keys |
| --- | --- | --- | --- |
| FX01-S01 — Register | `/register` | Current / per-action gate | `identity.account.register` |
| FX01-S02 — Verify email | `/verify-email` | Current / per-action gate | `identity.account.verify`, `identity.account.resend` |
| FX01-S03 — Login | `/login` | Current / per-action gate | `identity.account.login` |
| FX01-S04 — Forgot / Reset password | `/password/forgot; /password/reset` | Current / per-action gate | `identity.account.reset_request`, `identity.account.reset_confirm` |
| FX01-S05 — Profile | `/settings/profile` | Current / per-action gate | `identity.profile.read`, `identity.profile.update`, `identity.profile.change_email`, `identity.profile.change_password` |
| FX01-S06 — Security and sessions | `/settings/security` | Current / per-action gate | `identity.session.logout`, `identity.session.revoke_session`, `identity.session.revoke_all`, `identity.session.read`, `identity.profile.change_email`, `identity.profile.change_password`, `identity.mfa.enroll`, `identity.mfa.remove`, `identity.mfa.recover`, `identity.account.soft_delete` |
| FX02-S01 — Users | `/admin/users` | Current / per-action gate | `access.user.read` |
| FX02-S02 — User detail | `/admin/users/:userId` | Current / per-action gate | `access.user.read`, `access.user.disable`, `access.user.enable`, `access.user.revoke_sessions` |
| FX02-S03 — Role assignment | `/admin/users/:userId/roles` | Current / per-action gate | `access.role.set`, `access.change.read` |
| FX02-S04 — Permission matrix | `/admin/users/:userId/permissions` | Current / per-action gate | `access.permission.read`, `access.permission.set`, `access.change.read` |
| FX02-S05 — Module grants | `/admin/users/:userId/modules` | Current / per-action gate | `access.entitlement.set`, `access.change.read` |
| FX03-S01 — Module catalog | `/admin/modules` | Current / per-action gate | `modules.catalog.read` |
| FX03-S02 — Module detail | `/admin/modules/:moduleCode` | Current / per-action gate | `modules.catalog.read`, `modules.policy.enable`, `modules.policy.disable`, `modules.upgrade.read` |
| FX03-S03 — Enablement impact | `/admin/modules/:moduleCode/enablement` | Current / per-action gate | `modules.policy.enable`, `modules.policy.disable` |
| FX03-S04 — Registration defaults | `/admin/modules/registration-defaults` | Current / per-action gate | `modules.policy.defaults` |
| FX03-S05 — Module settings | `/admin/modules/:moduleCode/settings` | Current / per-action gate | `modules.policy.sharing`, `modules.policy.settings` |
| FX04-S01 — Share dialog | `/sharing/new?resource=:opaqueId` | Current / per-action gate | `sharing.link.read`, `sharing.link.create`, `sharing.link.update`, `sharing.link.copy_created` |
| FX04-S02 — Manage shares | `/settings/sharing` | Current / per-action gate | `sharing.link.read`, `sharing.link.revoke` |
| FX04-S03 — Shared resource | `/s/:token` | Current / per-action gate | `sharing.link.resolve` |
| FX04-S04 — Unavailable share | `/s/:token/unavailable` | Current / per-action gate | `sharing.link.resolve` |
| FX05-S01 — Grant support | `/settings/security/support/new` | Current / per-action gate | `support.consent.read`, `support.consent.grant` |
| FX05-S02 — Security Center | `/settings/security/access` | Current / per-action gate | `support.consent.read`, `support.consent.revoke` |
| FX05-S03 — Support session | `/support/:accessSessionId/:moduleCode` | Current / per-action gate | `support.session.open`, `support.session.end`, `files.support.read`, `projects.support.read`, `tasks.support.read`, `calendar.support.read`, `planner.support.read`, `goals.support.read`, `habits.support.read`, `time.support.read`, `focus.support.read`, `documents.support.read`, `bookmarks.support.read`, `snippets.support.read`, `reading.support.read`, `organization.support.read`, `finance.support.read`, `vault.support.read`, `news.support.read`, `prices.support.read`, `shopping.support.read`, `toolbox.support.read`, `github.support.read`, `automation.support.read`, `integrations.support.read`, `monitoring.support.read`, `assets.support.read`, `digital.support.read`, `career.support.read`, `learning.support.read` |
| FX05-S04 — Emergency entry | `/admin/emergency/new` | Current / per-action gate | `support.emergency.open` |
| FX05-S05 — Emergency session | `/emergency/:accessSessionId/:moduleCode` | Current / per-action gate | `support.emergency.end`, `files.support.read`, `projects.support.read`, `tasks.support.read`, `calendar.support.read`, `planner.support.read`, `goals.support.read`, `habits.support.read`, `time.support.read`, `focus.support.read`, `documents.support.read`, `bookmarks.support.read`, `snippets.support.read`, `reading.support.read`, `organization.support.read`, `finance.support.read`, `vault.support.read`, `news.support.read`, `prices.support.read`, `shopping.support.read`, `toolbox.support.read`, `github.support.read`, `automation.support.read`, `integrations.support.read`, `monitoring.support.read`, `assets.support.read`, `digital.support.read`, `career.support.read`, `learning.support.read` |
| FX06-S01 — Notification inbox | `/notifications` | Current / per-action gate | `notifications.inbox.read`, `notifications.inbox.mark_read`, `notifications.inbox.mark_unread`, `notifications.inbox.mark_all_read`, `notifications.inbox.delete` |
| FX06-S02 — Notification detail | `/notifications/:notificationId` | Current / per-action gate | `notifications.inbox.read`, `notifications.source.open` |
| FX06-S03 — Browser push permission | `/settings/notifications/push` | Current / per-action gate | `notifications.push.subscribe`, `notifications.push.remove` |
| FX07-S01 — Files library | `/files` | Current / per-action gate | `files.file.read`, `files.file.rename`, `files.file.trash`, `files.file.restore`, `files.file.purge` |
| FX07-S02 — Upload queue | `/files/uploads/:uploadId` | Current / per-action gate | `files.file.upload`, `files.file.cancel_upload` |
| FX07-S03 — Preview | `/files/:fileId` | Current / per-action gate | `files.file.preview`, `files.file.download` |
| FX07-S04 — Attach file picker | `/files/picker?resource=:opaqueId` | Current / per-action gate | `files.reference.attach` |
| FX07-S05 — Replace / remove reference | `/files/:fileId/references` | Current / per-action gate | `files.file.replace`, `files.reference.detach` |
| FX08-S01 — Trash | `/trash` | Current / per-action gate | `lifecycle.trash.read`, `lifecycle.resource.preview`, `lifecycle.resource.trash`, `lifecycle.resource.restore`, `lifecycle.resource.purge` |
| FX08-S02 — Restore / purge preview | `/trash/operations/new` | Current / per-action gate | `lifecycle.resource.preview`, `lifecycle.resource.restore`, `lifecycle.resource.purge` |
| FX08-S03 — Resource activity | `/activity?resource=:opaqueId` | Current / per-action gate | `lifecycle.activity.read` |
| FX08-S04 — Audit log | `/admin/audit` | Current / per-action gate | `lifecycle.audit.read` |
| FX09-S01 — Settings home | `/settings` | Current / per-action gate | `settings.preference.read` |
| FX09-S02 — Preferences | `/settings/preferences` | Current / per-action gate | `settings.preference.update` |
| FX09-S03 — My modules | `/settings/modules` | Current / per-action gate | `settings.module.read`, `settings.module.update` |
| FX09-S04 — Unavailable module route | `/modules/:moduleCode/unavailable` | Current / per-action gate | `settings.preference.read` |
| FX10-S01 — Import | `/data/import?module=:moduleCode` | Current / per-action gate | `transfer.import.preview` |
| FX10-S02 — Import preview | `/data/import/:operationId/preview` | Current / per-action gate | `transfer.import.commit`, `transfer.import.cancel` |
| FX10-S03 — Export | `/data/export?module=:moduleCode` | Current / per-action gate | `transfer.export.request` |
| FX10-S04 — Operation status | `/data/operations/:operationId` | Current / per-action gate | `transfer.import.read`, `transfer.export.read`, `transfer.export.download` |
| FX10-S05 — System backup / restore | `/admin/recovery` | Current / per-action gate | `transfer.backup.read`, `transfer.backup.request`, `transfer.restore.preview`, `transfer.restore.request` |
| FX11-S01 — Projects Grid / Table | `/projects` | Current / per-action gate | `projects.project.read`, `projects.project.create`, `projects.project.trash`, `projects.project.restore`, `projects.project.purge` |
| FX11-S02 — Project Create / Edit | `/projects/new; /projects/:projectId/edit` | Current / per-action gate | `projects.project.create`, `projects.project.update` |
| FX11-S03 — Project detail | `/projects/:projectId` | Current / per-action gate | `projects.project.read`, `projects.project.start`, `projects.project.revert`, `projects.project.share` |
| FX11-S04 — Complete / Skip Project | `/projects/:projectId/close` | Current / per-action gate | `projects.project.complete`, `projects.project.skip` |
| FX11-S05 — Project history | `/projects/:projectId/history` | Current / per-action gate | `projects.project.history` |
| FX12-S01 — Task Kanban | `/projects/:projectId/tasks?view=kanban` | Current / per-action gate | `tasks.task.read`, `tasks.task.create`, `tasks.task.start`, `tasks.task.complete`, `tasks.task.skip`, `tasks.task.revert`, `tasks.task.reorder` |
| FX12-S02 — Task Table | `/projects/:projectId/tasks?view=table` | Current / per-action gate | `tasks.task.read`, `tasks.task.update`, `tasks.task.trash` |
| FX12-S03 — Task Create / Edit | `/projects/:projectId/tasks/new; /tasks/:taskId/edit` | Current / per-action gate | `tasks.task.create`, `tasks.task.update`, `tasks.task.criteria`, `tasks.task.set_reminder`, `tasks.task.remove_reminder` |
| FX12-S04 — Task detail | `/tasks/:taskId` | Current / per-action gate | `tasks.task.read`, `tasks.task.trash`, `tasks.task.restore`, `tasks.task.purge`, `tasks.task.share` |
| FX12-S05 — Backward status reason | `/tasks/:taskId/transition` | Current / per-action gate | `tasks.task.revert` |
| FX12-S06 — Task history / restore | `/tasks/:taskId/history` | Current / per-action gate | `tasks.task.history`, `tasks.task.restore_version` |
| FX13-S01 — Day | `/calendar?view=day` | Current / per-action gate | `calendar.calendar.read` |
| FX13-S02 — Week | `/calendar?view=week` | Current / per-action gate | `calendar.calendar.read` |
| FX13-S03 — Month | `/calendar?view=month` | Current / per-action gate | `calendar.calendar.read` |
| FX13-S04 — Agenda | `/calendar?view=agenda` | Current / per-action gate | `calendar.calendar.read` |
| FX13-S05 — Manual Event form / detail | `/calendar/events/new; /calendar/events/:eventId` | Current / per-action gate | `calendar.event.read`, `calendar.event.create`, `calendar.event.update`, `calendar.event.complete`, `calendar.event.cancel`, `calendar.event.set_reminder`, `calendar.event.remove_reminder` |
| FX13-S06 — Task projection detail | `/calendar/tasks/:taskId` | Current / per-action gate | `calendar.projection.open` |
| FX13-S07 — ICS Import / Export | `/calendar/data` | Current / per-action gate | `calendar.ics.preview`, `calendar.ics.import`, `calendar.ics.export` |
| FX14-S01 — Embedded reminder field | `/tasks/:taskId/edit#reminder; /calendar/events/:eventId#reminder` | Current / per-action gate | `reminders.configuration.read`, `reminders.configuration.set`, `reminders.configuration.remove` |
| FX14-S02 — Exact reminder picker | `/reminders/picker?source=:opaqueId` | Current / per-action gate | `reminders.configuration.set` |
| FX14-S03 — Reminder delivery state | `/reminders/:reminderId` | Current / per-action gate | `reminders.configuration.read` |
| FX15-S01 — Daily planner | `/planner?view=day` | Current / per-action gate | `planner.plan.read`, `planner.plan.pin`, `planner.plan.unpin`, `planner.plan.reorder`, `planner.plan.notes` |
| FX15-S02 — Weekly planner | `/planner?view=week` | Current / per-action gate | `planner.plan.read`, `planner.plan.reschedule` |
| FX15-S03 — Plan Task picker | `/planner/add` | Current / per-action gate | `planner.plan.pin` |
| FX16-S01 — Goal list | `/goals` | Current / per-action gate | `goals.goal.read`, `goals.goal.create`, `goals.goal.archive`, `goals.goal.unarchive`, `goals.goal.trash`, `goals.goal.restore`, `goals.goal.purge` |
| FX16-S02 — Goal create / edit | `/goals/new; /goals/:goalId/edit` | Current / per-action gate | `goals.goal.create`, `goals.goal.update` |
| FX16-S03 — Goal detail | `/goals/:goalId` | Current / per-action gate | `goals.goal.start`, `goals.goal.complete`, `goals.goal.abandon`, `goals.goal.reopen`, `goals.target.read`, `goals.target.create`, `goals.target.update`, `goals.target.remove`, `goals.target.link_task`, `goals.target.unlink_task`, `goals.progress.read`, `goals.goal.history`, `goals.target.record_progress` |
| FX16-S04 — Progress update | `/goals/:goalId/progress` | Current / per-action gate | `goals.target.record_progress` |
| FX17-S01 — Today habits | `/habits/today` | Current / per-action gate | `habits.habit.read`, `habits.checkin.record`, `habits.checkin.correct` |
| FX17-S02 — Habit library | `/habits` | Current / per-action gate | `habits.habit.read`, `habits.habit.pause`, `habits.habit.resume`, `habits.habit.archive`, `habits.habit.unarchive`, `habits.habit.trash`, `habits.habit.restore`, `habits.habit.purge` |
| FX17-S03 — Habit form | `/habits/new; /habits/:habitId/edit` | Current / per-action gate | `habits.habit.create`, `habits.habit.update`, `habits.habit.schedule`, `habits.habit.set_reminder` |
| FX17-S04 — Habit detail / history | `/habits/:habitId` | Current / per-action gate | `habits.checkin.correct`, `habits.streak.read` |
| FX18-S01 — Timer | `/time` | Current / per-action gate | `time.timer.read`, `time.timer.start`, `time.timer.stop`, `time.timer.resume` |
| FX18-S02 — Time entries | `/time/entries` | Current / per-action gate | `time.entry.read`, `time.entry.trash`, `time.entry.restore`, `time.entry.purge` |
| FX18-S03 — Entry form | `/time/entries/new; /time/entries/:entryId/edit` | Current / per-action gate | `time.entry.create`, `time.entry.update` |
| FX18-S04 — Time reports / history | `/time/reports; /time/entries/:entryId/history` | Current / per-action gate | `time.entry.history`, `time.report.read` |
| FX19-S01 — Focus setup | `/focus` | Current / per-action gate | `focus.session.start`, `focus.preference.update` |
| FX19-S02 — Running phase | `/focus/sessions/:sessionId` | Current / per-action gate | `focus.session.read`, `focus.session.pause`, `focus.session.resume`, `focus.session.cancel` |
| FX19-S03 — Completion / conversion | `/focus/sessions/:sessionId/complete` | Current / per-action gate | `focus.session.start`, `focus.session.record_time` |
| FX19-S04 — Focus history | `/focus/history` | Current / per-action gate | `focus.session.read` |
| FX20-S01 — Documents library | `/documents` | Current / per-action gate | `documents.library.read`, `documents.page.read`, `documents.folder.create`, `documents.folder.rename`, `documents.page.share` |
| FX20-S02 — Folder view | `/documents/folders/:folderId` | Current / per-action gate | `documents.folder.read`, `documents.folder.create`, `documents.folder.rename`, `documents.folder.trash`, `documents.folder.restore`, `documents.folder.purge` |
| FX20-S03 — Create Page | `/documents/new` | Current / per-action gate | `documents.page.create` |
| FX20-S04 — Block editor | `/documents/pages/:pageId` | Current / per-action gate | `documents.page.read`, `documents.page.save`, `documents.page.publish`, `documents.page.unpublish`, `documents.format.import`, `documents.format.export` |
| FX20-S05 — Markdown editor | `/documents/pages/:pageId?editor=markdown` | Current / per-action gate | `documents.page.read`, `documents.page.save`, `documents.page.publish`, `documents.page.unpublish`, `documents.format.import`, `documents.format.export` |
| FX20-S06 — Page metadata / cover | `/documents/pages/:pageId/metadata` | Current / per-action gate | `documents.page.save`, `documents.tag.read`, `documents.tag.create`, `documents.tag.rename`, `documents.tag.remove` |
| FX20-S07 — Page versions | `/documents/pages/:pageId/history` | Current / per-action gate | `documents.page.history`, `documents.page.restore_version` |
| FX20-S08 — Archived pages | `/documents/archived` | Current / per-action gate | `documents.page.read`, `documents.page.unarchive`, `documents.page.trash` |
| FX20-S09 — Archive / Unarchive tree preview | `/documents/pages/:pageId/archive` | Current / per-action gate | `documents.page.archive`, `documents.page.unarchive` |
| FX20-S10 — Document Trash and restore | `/trash?module=documents` | Current / per-action gate | `documents.page.trash`, `documents.page.restore`, `documents.page.purge` |
| FX21-S01 — Bookmark library / collection | `/bookmarks; /bookmarks/collections/:collectionId` | Current / per-action gate | `bookmarks.bookmark.read`, `bookmarks.bookmark.archive`, `bookmarks.bookmark.unarchive`, `bookmarks.bookmark.trash`, `bookmarks.bookmark.restore`, `bookmarks.bookmark.purge` |
| FX21-S02 — Add / edit Bookmark | `/bookmarks/new; /bookmarks/:bookmarkId/edit` | Current / per-action gate | `bookmarks.bookmark.create`, `bookmarks.bookmark.update` |
| FX21-S03 — Bookmark detail | `/bookmarks/:bookmarkId` | Current / per-action gate | `bookmarks.bookmark.read`, `bookmarks.bookmark.refresh`, `bookmarks.bookmark.share` |
| FX22-S01 — Snippet list | `/snippets` | Current / per-action gate | `snippets.snippet.read`, `snippets.snippet.create`, `snippets.snippet.archive`, `snippets.snippet.unarchive`, `snippets.snippet.trash`, `snippets.snippet.restore`, `snippets.snippet.purge` |
| FX22-S02 — Snippet editor | `/snippets/new; /snippets/:snippetId/edit` | Current / per-action gate | `snippets.snippet.create`, `snippets.snippet.save` |
| FX22-S03 — Snippet detail / history | `/snippets/:snippetId; /snippets/:snippetId/history` | Current / per-action gate | `snippets.snippet.read`, `snippets.snippet.history`, `snippets.snippet.restore_version`, `snippets.snippet.share`, `snippets.snippet.copy`, `snippets.snippet.export` |
| FX23-S01 — Reading queue | `/read-later` | Local slice / per-action gate | `reading.queue.read`, `reading.item.save`, `reading.item.remove`, `reading.item.read`, `reading.item.unread`, `reading.item.position` |
| FX23-S02 — Reader | `/read-later/:itemId` | Contract-gated; no body reader in local slice | `reading.queue.read`, `reading.item.read`, `reading.item.unread`, `reading.item.position` |
| FX23-S03 — Unavailable reading sources | `/read-later?availability=unavailable` | Local queue projection / per-action gate | `reading.queue.read`, `reading.item.remove` |
| FX24-S01 — Tag management | `/organize/tags?namespace=:namespace` | Local Tag catalog slice / per-action gate | `organization.tag.read`, `organization.tag.create`, `organization.tag.rename`, `organization.tag.remove`; assignment remains gated |
| FX24-S02 — Collections | `/organize/collections` | Current / per-action gate | `organization.collection.read`, `organization.collection.create`, `organization.collection.update`, `organization.collection.delete` |
| FX24-S03 — Collection detail | `/organize/collections/:collectionId` | Current / per-action gate | `organization.collection.add`, `organization.collection.remove`, `organization.collection.reorder`, `organization.collection.share` |
| FX24-S04 — Templates | `/organize/templates` | Current / per-action gate | `organization.template.read`, `organization.template.create`, `organization.template.update`, `organization.template.archive`, `organization.template.unarchive`, `organization.template.trash`, `organization.template.restore`, `organization.template.purge` |
| FX24-S05 — Template preview / apply | `/organize/templates/:templateId` | Current / per-action gate | `organization.template.instantiate` |
| FX25-S01 — Global Search | `/search` | Current / per-action gate | `discovery.search.query` |
| FX25-S02 — Command palette | `/command-palette` | Current / per-action gate | `discovery.command.read`, `discovery.command.execute` |
| FX25-S03 — Favorites / recents | `/favorites; /recent` | Current / per-action gate | `discovery.favorite.read`, `discovery.favorite.add`, `discovery.favorite.remove`, `discovery.favorite.reorder`, `discovery.recent.read`, `discovery.recent.clear` |
| FX25-S04 — Saved searches | `/search/saved` | Current / per-action gate | `discovery.saved_search.read`, `discovery.saved_search.create`, `discovery.saved_search.update`, `discovery.saved_search.delete`, `discovery.saved_search.run` |
| FX26-S01 — Home dashboard | `/` | Current / per-action gate | `dashboard.dashboard.read`, `dashboard.widget.refresh`, `dashboard.quick_create.open` |
| FX26-S02 — Dashboard configuration | `/settings/dashboard` | Current / per-action gate | `dashboard.layout.update`, `dashboard.layout.add_widget`, `dashboard.layout.configure_widget`, `dashboard.layout.remove_widget`, `dashboard.layout.reorder_widget` |
| FX27-S01 — Finance overview | `/finance` | Historical advanced scope / Blocked | `finance.report.read` |
| FX27-S02 — Accounts | `/finance/accounts` | Historical advanced scope / Blocked | `finance.account.read`, `finance.account.close` |
| FX27-S03 — Account form | `/finance/accounts/new; /finance/accounts/:accountId/edit` | Historical advanced scope / Blocked | `finance.account.create`, `finance.account.update` |
| FX27-S04 — Transactions ledger | `/finance/transactions` | Historical advanced scope / Blocked | `finance.transaction.read`, `finance.transaction.void` |
| FX27-S05 — Transaction / split form | `/finance/transactions/new; /finance/transactions/:transactionId` | Historical advanced scope / Blocked | `finance.transaction.post`, `finance.transaction.correct`, `finance.transaction.split`, `finance.category.read`, `finance.category.create`, `finance.category.update`, `finance.category.merge`, `finance.category.remove` |
| FX27-S06 — Transfer form | `/finance/transfers/new` | Historical advanced scope / Blocked | `finance.transfer.post` |
| FX27-S07 — Bills and payment allocation | `/finance/bills; /finance/bills/:billId` | Historical advanced scope / Blocked | `finance.bill.read`, `finance.bill.create`, `finance.bill.update`, `finance.bill.cancel`, `finance.bill.record_payment`, `finance.bill.void_payment` |
| FX27-S08 — Subscriptions / price history | `/finance/subscriptions; /finance/subscriptions/:subscriptionId` | Historical advanced scope / Blocked | `finance.subscription.read`, `finance.subscription.create`, `finance.subscription.update`, `finance.subscription.pause`, `finance.subscription.cancel`, `finance.subscription.record_price` |
| FX27-S09 — Budgets | `/finance/budgets` | Historical advanced scope / Blocked | `finance.budget.read`, `finance.budget.create`, `finance.budget.update` |
| FX27-S10 — Savings and debts | `/finance/savings; /finance/debts` | Historical advanced scope / Blocked | `finance.savings.read`, `finance.savings.create`, `finance.savings.update`, `finance.debt.read`, `finance.debt.create`, `finance.debt.update`, `finance.debt.record_payment`, `finance.debt.adjust_interest`, `finance.savings.record_progress` |
| FX27-S11 — Reports | `/finance/reports` | Historical advanced scope / Blocked | `finance.report.read`, `finance.report.share` |
| FX27-S12 — Finance CSV import / corrections | `/finance/data; /finance/transactions/:transactionId/correction` | Historical advanced scope / Blocked | `finance.csv.preview`, `finance.csv.import`, `finance.csv.export` |
| FX28-S01 — Vault list | `/vault` | Current / per-action gate | `vault.session.unlock`, `vault.session.lock`, `vault.item.read`, `vault.folder.read`, `vault.folder.create`, `vault.folder.rename`, `vault.folder.remove`, `vault.tag.manage` |
| FX28-S02 — Vault item detail | `/vault/items/:itemId` | Current / per-action gate | `vault.item.read`, `vault.item.reveal`, `vault.item.copy`, `vault.recovery_code.mark_used` |
| FX28-S03 — Vault create / edit | `/vault/items/new; /vault/items/:itemId/edit` | Current / per-action gate | `vault.item.create`, `vault.item.update` |
| FX28-S04 — Vault history | `/vault/items/:itemId/history` | Current / per-action gate | `vault.item.history` |
| FX28-S05 — Password generator | `/vault/generator` | Current / per-action gate | `vault.generator.generate`, `vault.generator.copy` |
| FX28-S06 — Vault Trash / security policy | `/vault/trash; /settings/vault` | Current / per-action gate | `vault.session.lock`, `vault.item.trash` |
| FX29-S01 — Sources | `/news/sources` | Current / per-action gate | `news.source.read`, `news.source.pause`, `news.source.resume`, `news.source.unfollow`, `news.source.refresh`, `news.category.read`, `news.category.create`, `news.category.update`, `news.category.remove` |
| FX29-S02 — Follow source form | `/news/sources/new` | Current / per-action gate | `news.source.follow`, `news.source.update` |
| FX29-S03 — Article list | `/news` | Current / per-action gate | `news.article.read`, `news.article.mark_read`, `news.article.mark_unread`, `news.article.mark_all_read`, `news.history.read`, `news.history.clear` |
| FX29-S04 — Article reader | `/news/articles/:articleId` | Current / per-action gate | `news.article.read`, `news.article.save` |
| FX29-S05 — Topic watches / matches | `/news/watches; /news/watches/:watchId` | Current / per-action gate | `news.watch.read`, `news.watch.create`, `news.watch.update`, `news.watch.enable`, `news.watch.disable`, `news.watch.delete`, `news.match.read` |
| FX30-S01 — Tracked products | `/shopping/prices` | Paused | `prices.tracker.read`, `prices.tracker.pause`, `prices.tracker.resume`, `prices.tracker.remove` |
| FX30-S02 — Tracker create / edit | `/shopping/prices/new; /shopping/prices/:trackerId/edit` | Paused | `prices.tracker.create`, `prices.tracker.update` |
| FX30-S03 — Product price detail / chart | `/shopping/prices/:trackerId` | Paused | `prices.tracker.refresh`, `prices.observation.read` |
| FX30-S04 — Alert rules | `/shopping/prices/:trackerId/alerts` | Paused | `prices.alert.read`, `prices.alert.create`, `prices.alert.update`, `prices.alert.enable`, `prices.alert.disable`, `prices.alert.remove` |
| FX30-S05 — Provider / alert history | `/shopping/prices/:trackerId/history` | Paused | `prices.delivery.read` |
| FX31-S01 — Wishlist | `/shopping/wishlist` | Current / per-action gate | `shopping.wishlist.read`, `shopping.wishlist.create`, `shopping.wishlist.update`, `shopping.wishlist.mark_purchased`, `shopping.wishlist.archive`, `shopping.wishlist.unarchive`, `shopping.wishlist.trash`, `shopping.wishlist.restore`, `shopping.wishlist.purge` |
| FX31-S02 — Comparison | `/shopping/comparisons/:comparisonId` | Current / per-action gate | `shopping.comparison.read`, `shopping.comparison.create`, `shopping.comparison.update`, `shopping.comparison.members`, `shopping.comparison.criteria`, `shopping.comparison.remove` |
| FX31-S03 — Orders | `/shopping/orders` | Current / per-action gate | `shopping.order.read`, `shopping.order.trash`, `shopping.order.restore`, `shopping.order.purge` |
| FX31-S04 — Order form / detail | `/shopping/orders/new; /shopping/orders/:orderId` | Current / per-action gate | `shopping.order.create`, `shopping.order.update`, `shopping.order.transition`, `shopping.order.return`, `shopping.order.history`, `shopping.order.link_finance`, `shopping.order.share` |
| FX31-S05 — Sellers / merge | `/shopping/sellers; /shopping/sellers/:sellerId/merge` | Current / per-action gate | `shopping.seller.read`, `shopping.seller.create`, `shopping.seller.update`, `shopping.seller.merge` |
| FX31-S06 — Warranty detail / claims | `/shopping/warranties/:warrantyId` | Current / per-action gate | `shopping.warranty.read`, `shopping.warranty.create`, `shopping.warranty.update`, `shopping.warranty.evidence` |
| FX31-S07 — Create Asset handoff | `/shopping/orders/:orderId/create-asset` | Current / per-action gate | `shopping.purchase.create_asset` |
| FX32-S01 — Tool catalog | `/developer/tools` | Current / per-action gate | `toolbox.catalog.read` |
| FX32-S02 — Tool workbench | `/developer/tools/:toolCode` | Current / per-action gate | `toolbox.base64.run`, `toolbox.url_codec.run`, `toolbox.html_codec.run`, `toolbox.hash.run`, `toolbox.password.run`, `toolbox.uuid.run`, `toolbox.datetime.run`, `toolbox.json.run`, `toolbox.xml.run`, `toolbox.yaml.run`, `toolbox.csv.run`, `toolbox.data_convert.run`, `toolbox.regex.run`, `toolbox.text_diff.run`, `toolbox.color.run`, `toolbox.qr.run`, `toolbox.jwt.run`, `toolbox.cron.run`, `toolbox.markdown.run`, `toolbox.code_format.run`, `toolbox.url_parse.run`, `toolbox.headers.run`, `toolbox.certificate.run`, `toolbox.output.copy`, `toolbox.output.download`, `toolbox.output.save_snippet` |
| FX32-S03 — Tool history / favorites | `/developer/tools/saved` | Current / per-action gate | `toolbox.history.read`, `toolbox.history.save`, `toolbox.history.delete` |
| FX32-S04 — Network request preview | `/developer/tools/:toolCode/network-preview` | Current / per-action gate | `toolbox.network.http`, `toolbox.network.dns` |
| FX33-S01 — Discovery feed | `/developer/github` | Current / per-action gate | `github.repository.search` |
| FX33-S02 — Repository detail | `/developer/github/repositories/:repositoryId` | Current / per-action gate | `github.repository.read`, `github.repository.refresh` |
| FX33-S03 — Saved queries / repositories | `/developer/github/saved` | Current / per-action gate | `github.saved.read`, `github.saved.save`, `github.saved.notes`, `github.saved.remove`, `github.query.read`, `github.query.create`, `github.query.update`, `github.query.delete`, `github.query.run` |
| FX33-S04 — Ranking snapshots | `/developer/github/snapshots` | Current / per-action gate | `github.snapshot.read`, `github.snapshot.capture`, `github.snapshot.compare` |
| FX34-S01 — Automations | `/automation` | Paused | `automation.definition.read`, `automation.definition.create`, `automation.definition.enable`, `automation.definition.disable`, `automation.definition.trash`, `automation.definition.restore`, `automation.definition.purge` |
| FX34-S02 — Definition editor | `/automation/new; /automation/:definitionId/edit` | Paused | `automation.definition.create`, `automation.definition.save`, `automation.definition.validate`, `automation.schedule.update`, `automation.definition.import`, `automation.definition.export` |
| FX34-S03 — Definition detail / versions | `/automation/:definitionId` | Paused | `automation.definition.read`, `automation.definition.history` |
| FX34-S04 — Run history | `/automation/:definitionId/runs` | Paused | `automation.run.read` |
| FX34-S05 — Run / step detail | `/automation/runs/:runId` | Paused | `automation.run.start`, `automation.run.cancel`, `automation.run.retry_step`, `automation.run.dry_run` |
| FX35-S01 — Connections | `/integrations` | Paused | `integrations.connection.read`, `integrations.connection.disable`, `integrations.system_connection.read`, `integrations.system_connection.configure` |
| FX35-S02 — Connection form | `/integrations/new; /integrations/:connectionId/edit` | Paused | `integrations.connection.create`, `integrations.connection.update`, `integrations.connection.test`, `integrations.connection.scopes` |
| FX35-S03 — Webhook detail / form | `/integrations/webhooks/:webhookId` | Paused | `integrations.webhook.read`, `integrations.webhook.create`, `integrations.webhook.update`, `integrations.webhook.enable`, `integrations.webhook.disable`, `integrations.webhook.test`, `integrations.webhook.rotate` |
| FX35-S04 — Delivery history | `/integrations/webhooks/:webhookId/deliveries` | Paused | `integrations.delivery.read`, `integrations.delivery.retry` |
| FX35-S05 — Credential reference picker | `/integrations/credential-picker` | Paused | `integrations.connection.credential` |
| FX36-S01 — My monitors | `/monitoring` | Current / per-action gate | `monitoring.monitor.read`, `monitoring.monitor.pause`, `monitoring.monitor.resume`, `monitoring.monitor.remove` |
| FX36-S02 — Monitor form | `/monitoring/new; /monitoring/:monitorId/edit` | Current / per-action gate | `monitoring.monitor.create`, `monitoring.monitor.update` |
| FX36-S03 — Monitor detail / incidents | `/monitoring/:monitorId` | Current / per-action gate | `monitoring.monitor.check`, `monitoring.observation.read`, `monitoring.incident.read` |
| FX36-S04 — Admin jobs | `/admin/jobs` | Current / per-action gate | `monitoring.job.read` |
| FX36-S05 — Job attempt detail | `/admin/jobs/:jobId` | Current / per-action gate | `monitoring.job.read`, `monitoring.job.retry`, `monitoring.job.cancel` |
| FX37-S01 — Asset inventory | `/assets/personal` | Current / per-action gate | `assets.asset.read`, `assets.asset.create`, `assets.asset.archive`, `assets.asset.unarchive` |
| FX37-S02 — Asset create / edit | `/assets/personal/new; /assets/personal/:assetId/edit` | Current / per-action gate | `assets.asset.create`, `assets.asset.update`, `assets.purchase.update` |
| FX37-S03 — Asset detail | `/assets/personal/:assetId` | Current / per-action gate | `assets.asset.read`, `assets.asset.transition`, `assets.serial.reveal`, `assets.serial.copy`, `assets.loan.lend`, `assets.loan.return`, `assets.asset.share` |
| FX37-S04 — Warranty / repair form | `/assets/personal/:assetId/service` | Current / per-action gate | `assets.warranty.update`, `assets.warranty.set_reminder`, `assets.repair.read`, `assets.repair.create`, `assets.repair.update`, `assets.repair.remove` |
| FX37-S05 — Components / accessories | `/assets/personal/:assetId/components` | Current / per-action gate | `assets.component.read`, `assets.component.create`, `assets.component.update`, `assets.component.remove`, `assets.accessory.link`, `assets.accessory.unlink` |
| FX37-S06 — Asset history / Trash | `/assets/personal/:assetId/history; /trash?module=assets` | Current / per-action gate | `assets.asset.trash`, `assets.asset.restore`, `assets.asset.purge`, `assets.asset.history` |
| FX38-S01 — Digital asset list | `/assets/digital` | Current / per-action gate | `digital.asset.read`, `digital.asset.archive`, `digital.asset.unarchive`, `digital.asset.trash`, `digital.asset.restore`, `digital.asset.purge` |
| FX38-S02 — Type-specific form | `/assets/digital/new; /assets/digital/:assetId/edit` | Current / per-action gate | `digital.asset.create`, `digital.asset.update`, `digital.credential.reference`, `digital.certificate.parse` |
| FX38-S03 — Digital detail | `/assets/digital/:assetId` | Current / per-action gate | `digital.asset.read`, `digital.asset.cancel`, `digital.observation.inspect`, `digital.asset.set_reminder`, `digital.asset.share` |
| FX38-S04 — Renewal form / timeline | `/assets/digital/:assetId/renewals` | Current / per-action gate | `digital.asset.history`, `digital.renewal.record` |
| FX39-S01 — Job pipeline | `/career/jobs` | Current / per-action gate | `career.job.read`, `career.job.transition`, `career.job.trash`, `career.job.restore`, `career.job.purge` |
| FX39-S02 — Job create / detail | `/career/jobs/new; /career/jobs/:jobId` | Current / per-action gate | `career.job.create`, `career.job.update`, `career.application.attach_resume` |
| FX39-S03 — Company directory / merge | `/career/companies; /career/companies/:companyId/merge` | Current / per-action gate | `career.company.read`, `career.company.create`, `career.company.update`, `career.company.merge` |
| FX39-S04 — Linked personal Calendar events | `/career/jobs/:jobId/events` | Replaced by internal Calendar link | `career.appointment.read`, `career.appointment.create_event`, `career.appointment.link_event`, `career.appointment.unlink_event` |
| FX39-S05 — Resume library / upload | `/career/resumes` | Current / per-action gate | `career.resume.read`, `career.resume.create`, `career.resume.update`, `career.resume.upload_version`, `career.resume.select_document`, `career.resume.archive`, `career.resume.unarchive`, `career.resume.trash`, `career.resume.restore`, `career.resume.purge` |
| FX39-S06 — Resume version / share | `/career/resumes/:resumeId/versions/:version` | Current / per-action gate | `career.resume.download`, `career.resume.share`, `career.resume.convert` |
| FX39-S07 — Application timeline | `/career/jobs/:jobId/history` | Current / per-action gate | `career.job.history`, `career.resume.history` |
| FX40-S01 — Learning overview | `/learning` | Current / per-action gate | `learning.report.read` |
| FX40-S02 — Skills / evidence / merge | `/learning/skills; /learning/skills/:skillId` | Current / per-action gate | `learning.skill.read`, `learning.skill.create`, `learning.skill.update`, `learning.skill.proficiency`, `learning.skill.evidence`, `learning.skill.merge`, `learning.skill.archive`, `learning.skill.unarchive`, `learning.skill.trash`, `learning.skill.restore`, `learning.skill.purge` |
| FX40-S03 — Course form / progress | `/learning/courses/new; /learning/courses/:courseId` | Current / per-action gate | `learning.course.read`, `learning.course.create`, `learning.course.update`, `learning.course.progress`, `learning.course.milestone`, `learning.course.complete`, `learning.course.abandon`, `learning.course.archive`, `learning.course.unarchive`, `learning.course.trash`, `learning.course.restore`, `learning.course.purge` |
| FX40-S04 — Certifications / renewal | `/learning/certifications; /learning/certifications/:certificationId` | Current / per-action gate | `learning.certification.read`, `learning.certification.create`, `learning.certification.update`, `learning.certification.renew`, `learning.certification.evidence`, `learning.certification.set_reminder`, `learning.certification.share`, `learning.certification.archive`, `learning.certification.unarchive`, `learning.certification.trash`, `learning.certification.restore`, `learning.certification.purge` |
| FX40-S05 — Learning plans | `/learning/plans; /learning/plans/:planId` | Current / per-action gate | `learning.plan.read`, `learning.plan.create`, `learning.plan.update`, `learning.plan.link`, `learning.plan.unlink`, `learning.plan.reorder`, `learning.plan.complete`, `learning.plan.archive`, `learning.plan.unarchive`, `learning.plan.trash`, `learning.plan.restore`, `learning.plan.purge` |
| FX40-S06 — Work log form / list | `/learning/work-log; /learning/work-log/new` | Current / per-action gate | `learning.worklog.read`, `learning.worklog.create`, `learning.worklog.update`, `learning.worklog.link_time`, `learning.worklog.archive`, `learning.worklog.unarchive`, `learning.worklog.trash`, `learning.worklog.restore`, `learning.worklog.purge` |
| FX28-S07 — Owner recovery requests | `/vault/recovery` | Current / per-action gate | `vault.recovery.request`, `vault.recovery.read` |
| FX28-S08 — SuperAdmin recovery review | `/admin/vault-recovery/:requestId` | Current / per-action gate | `vault.recovery.review`, `vault.recovery.authorize`, `vault.recovery.reject` |
| FX27-S13 — Manual money records | `/finance/records` | Current / per-action gate | `finance.manual_record.read`, `finance.manual_summary.read` |
| FX27-S14 — Manual money record form | `/finance/records/new; /finance/records/:id/edit` | Current / per-action gate | `finance.manual_record.create`, `finance.manual_record.update` |
| FX27-S15 — Manual categories | `/finance/categories` | Current / per-action gate | `finance.manual_category.read`, `finance.manual_category.create`, `finance.manual_category.update`, `finance.manual_category.remove` |

Common controls (filter/sort/view/focus) do not create extra data authority. All source read/field-diff/response gates remain mandatory. Support-safe module reads are invoked only in their explicit mode; SuperAdmin Recovery is a separate mode with persistent target/request banner, no owner impersonation.
