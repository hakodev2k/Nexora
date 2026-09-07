# Apply / Adapt / Reject behavior register

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

| Reference | Observed behavior | Decision | Nexora rationale / rejected scope |
| --- | --- | --- | --- |
| [Auth0](https://auth0.com/docs/manage-users/user-accounts/verify-emails) | Email verification can be a link sent to the supplied address. | ADAPT | Nexora requires verification before module access; no bulk verification, social login or Auth0 dependency inferred. |
| [WordPress](https://wordpress.org/documentation/article/roles-and-capabilities/) | Roles group capabilities governing administrative actions. | ADAPT | Use explicit Nexora system roles/action grants; reject access to others personal content from role alone. |
| [WordPress Plugins](https://wordpress.org/documentation/article/manage-plugins/) | Installed plugins have activation/deactivation management. | ADAPT | Installed/enabled/disabled/dependency states; reject executable upload and marketplace. |
| [Google Drive](https://support.google.com/drive/answer/2494822?hl=en) | Sharing exposes audience and permission choices with specific-recipient access. | ADAPT | Readonly PublicLink/AuthenticatedLink/RestrictedUsers with expiry; reject edit/comment and inherited Document-child sharing. |
| [Microsoft Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests) | Customer approval, limited-duration access and audit records distinguish support access. | ADAPT | Owner one-module readonly grant; keep24h default and any qualified Admin, not Microsoft duration/organization workflow. |
| [GitHub Notifications](https://docs.github.com/en/subscriptions-and-notifications/how-tos/viewing-and-triaging-notifications/managing-notifications-from-your-inbox) | Inbox supports read/unread and multi-selection triage. | ADAPT | Use owner inbox actions; reject unsubscribe/mute/automatic retention because Nexora always attempts three channels and retains until deletion. |
| [Google Drive](https://support.google.com/drive/answer/2424368?hl=en) | Upload is a distinct file-management workflow. | ADAPT | Add progress, validation and required scan/quarantine gate; Drive documentation is not evidence of Nexora scanning behavior. |
| [Notion](https://www.notion.com/help/duplicate-delete-and-restore-content) | Trash is a separate recovery surface with search/filter. | ADAPT | Keep Nexora indefinite owner Trash and cohort dependencies; reject reference retention/teamspace behavior. |
| [Notion](https://www.notion.com/help/navigate-with-the-sidebar) | Sidebar separates navigation and Trash; trashed pages are not editable before restore. | ADAPT | Collapsible module groups and module-local tree; reject workspace/teamspace switch and retention policy. |
| [GitLab](https://docs.gitlab.com/administration/backup_restore/) | System backup and restore are administrative recovery workflows. | ADAPT | Separate owner export/import from system recovery; no claim SQL/files/keys are recovered without verification. |
| [Microsoft To Do](https://support.microsoft.com/en-us/todo/welcome-to-microsoft-to-do) | Personal lists organize tasks and My Day focuses daily work. | ADAPT | Nexora Project contains every Task; keep four statuses and irreversible Project terminal lock. Reject shared lists/assignment. |
| [Linear](https://linear.app/docs/select-issues) | Selection, context command bar and keyboard reordering provide alternatives to pointer actions. | ADAPT | Task Move/Reorder commands with same validation as drag; reason-required backward moves remain Nexora-specific. |
| [Google Calendar](https://support.google.com/calendar/answer/37118?co=GENIE.Platform%3DDesktop&hl=en) | Import & Export is a settings entry with file selection and target calendar selection. | ADAPT | Nexora .ics preview/report, always ManualEvent; reject recurrence/reminder import and external sync. |
| [Todoist](https://www.todoist.com/help/todoist/features/introduction-to-reminders-9PezfU) | Custom reminders may specify exact time or relative time before a task. | ADAPT | Exactly one reminder and15min before Start preset; reject multiple/location reminders or subscription tiers. |
| [Microsoft To Do / Outlook My Day](https://support.microsoft.com/en-us/outlook/calendar/use-my-day-with-to-do-in-outlook) | My Day exposes upcoming calendar events and tasks alongside other work. | ADAPT | Planner lens pins existing Tasks without schedule mutation or copy. |
| [ClickUp Goals](https://clickup.com/features/goals) | Numeric, true/false and Task targets show goal progress. | ADAPT | Personal equal-weight targets; explicit Goal status and Nexora formula; reject teams/financial automation. |
| [Habitify](https://intercom.help/habitify-app/en/articles/9387661-record-progress-on-a-good-habit) | Daily journal offers direct complete or partial-progress logging. | ADAPT | Boolean/count check-ins with visible controls; no swipe-only action, bad-habit/challenge expansion or auto-grading scope. |
| [Toggl Track](https://support.toggl.com/en-us/article/creating-a-time-entry-wg8nug/) | Timer/manual modes and continuing a past entry create time records. | ADAPT | One owner timer, resume new entry, gross duration; reject billable/team/client workflow. |
| [Pomofocus](https://pomofocus.io/) | A focus timer alternates a work interval with a break after completion. | ADAPT | Explicit next phase and approved25/5/15 defaults; reject auto-chain, imported Task copies and subscription features. |
| [Notion](https://www.notion.com/help/writing-and-editing-basics) | Page content is built from typed editable blocks. | ADAPT | Bounded Block editor or immutable Markdown mode, explicit manual Save; reject autosave/coediting/databases/unlimited tree. |
| [Google Docs](https://support.google.com/docs/answer/190843?hl=en) | Version history lets users inspect earlier document content. | ADAPT | Manual Save produces immutable versions; Restore creates a new one; no history retention borrowed. |
| [Raindrop.io](https://help.raindrop.io/quickstart) | Saving a bookmark can assign a collection and tags. | ADAPT | Owner metadata overrides, collection refs and direct Add form; reject AI/library chat/team sharing. |
| [GitHub Gists](https://docs.github.com/en/rest/gists/gists) | Gist API represents text files and version history. | ADAPT | Escaped code/language/history/copy; no executable snippets, GitHub publishing or collaboration. |
| [Raindrop.io articles](https://help.raindrop.io/quickstart) | Saved articles/bookmarks form an organized reading collection. | ADAPT | One owner ReadingItem per source, no duplicate News body; parser failure remains degraded. |
| [Notion templates](https://www.notion.com/help/database-templates) | Templates reuse page structure and preset properties during creation. | ADAPT | Typed seed without IDs/shares/secrets; still explicit DocumentType/EditorMode; reject automatic creation and no-code databases. |
| [Linear Search](https://linear.app/docs/search) | Global search and find-in-view are distinct scopes with keyboard entry points. | ADAPT | Global command palette versus local Documents direct-location search; no comments/team scope. |
| [ClickUp Dashboards](https://clickup.com/features/dashboards) | Dashboard cards organize selectable summaries/reports. | ADAPT | Attention-focused owner widgets with independent state, not enterprise BI or AI dashboard. |
| [Actual Budget](https://actualbudget.org/docs/transactions/transfers/) | Linked transfer sides represent movement between accounts rather than unrelated income/expense. | ADAPT | Transfer form previews both sides atomically; do not import Actual budget or deletion semantics while Q-05 open. |
| [Bitwarden](https://bitwarden.com/help/managing-items/) | Vault item management separates item list/detail and archive/delete actions. | ADAPT | Masked personal-only item workflow, recent-auth reveal/copy; reject organization/shared Vault and reference retention. |
| [Feedly](https://docs.feedly.com/article/288-how-to-follow-a-feed-in-your-feedly-account) | Follow a found feed and place it in a folder/category. | ADAPT | RSS/Atom source preview/category; reject newsletter/RSS-builder/AI scope without approval. |
| [Keepa](https://keepa.com/) | Public product describes price history charts and price-drop alerts for Amazon products. | ADAPT | History/threshold/freshness visualization for approved Shopee adapter, not proof a Shopee provider exists. |
| [AnyList](https://www.anylist.com/features) | Shopping list features offer a focused item-list workflow. | ADAPT | Wishlist entry/list pattern only; Order/Seller/Warranty are Nexora requirements, not asserted AnyList capabilities. |
| [DevToys](https://devtoys.app/) | Offline utility catalog includes converters, formatters, text comparison and generators. | ADAPT | Web input/output workbench, memory-only default, explicit network warning; reject automatic clipboard reading/executable extensions. |
| [GitHub Search API](https://docs.github.com/en/rest/search/search) | Search supports qualifiers, explicit sorting and incomplete-results indication. | ADAPT | Public created-this-week sorted total-stars metric, timestamp/partial state; reject OAuth/private/write scope. |
| [n8n](https://n8n.io/features/) | Workflows expose triggers, per-step outputs and execution debugging. | ADAPT | Versioned definition/run/step details; reject code/AI/loops/unbounded graph, keep Q-07 for allowed flow and egress. |
| [GitHub Webhooks](https://docs.github.com/en/webhooks/using-webhooks/handling-webhook-deliveries) | Delivery handling is a separate event-processing workflow. | ADAPT | Signature/dedupe/status/redacted delivery view; no exposing payload secrets or assuming replay is safe. |
| [UptimeRobot](https://uptimerobot.com/) | Monitoring product distinguishes endpoint checks, response time and SSL/cron capabilities. | ADAPT | Owner monitor/incident history separate from Admin jobs; scope/budget remain Q-07/Q-08. |
| [Snipe-IT](https://snipe-it.readme.io/docs/overview) | Assets have status labels and check-in/check-out context. | ADAPT | Personal asset state/repair/loan metadata; reject team assignment and configurable enterprise deployment states. |
| [Cloudflare Registrar](https://developers.cloudflare.com/registrar/account-options/renew-domains/) | Domain renewal information distinguishes auto-renew setting and actual renewal outcome. | ADAPT | Show entered renewal metadata/freshness; reject payment/registrar control and guarantees that renewal happened. |
| [Teal](https://www.tealhq.com/tools/job-tracker) | Saved opportunities are organized by application stage with job/company detail. | ADAPT | Manual personal pipeline, exact Resume version; reject AI resume generation/browser scraping/outreach. |
| [Moodle](https://docs.moodle.org/502/en/Activity_completion) | Completion criteria can include a learner marking an activity complete. | ADAPT | Explicit owner milestones/progress and manual Course completion; reject teacher override, grading and LMS delivery. |


## Cross-system decisions

| Pattern | Classification | Reason |
| --- | --- | --- |
| Modal focus containment and return | APPLY | Use W3C APG semantics; minor visual shape delegated. |
| Shared sidebar hierarchy | ADAPT | Nexora groups personal modules; no Workspace/teamspace. |
| Reference autosave/coediting | REJECT | Documents manual Save and personal-only approved rules. |
| Reference recurring Tasks/multiple reminders | FUTURE / OUT OF SCOPE until Q-10 | One reminder and no recurring ICS import already fixed. |
| Reference shared Vault/organization | REJECT | Personal-only, no organization key-sharing model. |
| Reference financial budgeting/corrections | PROPOSED — Q-05 | Money meaning cannot be delegated from a reference UI. |
| Reference webhook/code/graph automation | PROPOSED / REJECT | Q-07 for scoped flows/egress; arbitrary executable code rejected. |
| Reference Trash retention | REJECT | Nexora approved owner Trash indefinite until explicit purge. |
| Reference paid-plan/provider features | OUT OF SCOPE unless PO approves | No pricing/provider budget/integration automatically adopted. |
