using Nexora.Domain.Common;

namespace Nexora.Domain.Access;

public static class ActionGrantPolicy
{
    private static readonly HashSet<string> ApprovedLocalActions = new(StringComparer.Ordinal)
    {
        "identity.account.register",
        "identity.account.verify",
        "identity.account.resend",
        "identity.account.login",
        "identity.account.reset_request",
        "identity.account.reset_confirm",
        "identity.session.logout",
        "identity.session.read",
        "identity.session.revoke_session",
        "identity.session.revoke_all",
        "identity.profile.read",
        "identity.profile.update",
        "access.user.read",
        "access.user.disable",
        "access.permission.read",
        "access.change.read",
        "access.role.set",
        "access.permission.set",
        "access.entitlement.set",
        "modules.catalog.read",
        "modules.policy.enable",
        "modules.policy.disable",
        "modules.policy.defaults",
        "modules.policy.sharing",
        "notifications.dispatch.publish",
        "notifications.dispatch.deliver",
        "settings.preference.read",
        "settings.preference.update",
        "projects.view",
        "projects.create",
        "projects.update",
        "projects.delete",
        "projects.restore",
        "projects.purge",
        "tasks.view",
        "tasks.create",
        "tasks.update",
        "tasks.delete",
        "tasks.restore",
        "tasks.purge",
        "calendar.view",
        "calendar.create",
        "calendar.update",
        "calendar.cancel",
        "calendar.import",
        "calendar.export",
        "documents.library.read",
        "documents.page.read",
        "documents.page.create",
        "documents.page.save",
        "documents.page.publish",
        "documents.page.unpublish",
        "documents.page.archive",
        "documents.page.unarchive",
        "files.view",
        "files.create",
        "files.delete",
        "files.restore",
        "files.purge",
        "notifications.view",
        "notifications.update",
        "notifications.delete",
        "settings.view",
        "settings.update",
        "backup.create",
        "backup.restore",

        // The following canonical keys are retained for the current PR's
        // historical implementation inventory. They are not authority for
        // the current implementation boundary; only the explicit M01
        // AdminGrantable projection below can be assigned to Admin SELF.
        "projects.project.read",
        "projects.project.create",
        "projects.project.update",
        "projects.project.start",
        "projects.project.revert",
        "projects.project.complete",
        "projects.project.skip",
        "projects.project.trash",
        "tasks.task.read",
        "tasks.task.create",
        "tasks.task.update",
        "tasks.task.start",
        "tasks.task.complete",
        "tasks.task.skip",
        "tasks.task.revert",
        "tasks.task.trash",
        "calendar.event.read",
        "calendar.event.create",
        "calendar.event.update",
        "calendar.event.complete",
        "calendar.event.cancel",
        "notifications.inbox.read",
        "notifications.inbox.mark_read",
        "notifications.inbox.mark_unread",
        "notifications.inbox.mark_all_read",
        "notifications.inbox.delete",
        "lifecycle.trash.read",
        "lifecycle.resource.restore",
        "lifecycle.resource.purge",
        "finance.manual_category.read",
        "finance.manual_category.create",
        "finance.manual_category.update",
        "finance.manual_category.remove",
        "finance.manual_record.read",
        "finance.manual_record.create",
        "finance.manual_record.update",
        "finance.manual_summary.read",
        "bookmarks.bookmark.read",
        "bookmarks.bookmark.create",
        "bookmarks.bookmark.update",
        "bookmarks.bookmark.archive",
        "bookmarks.bookmark.unarchive",
        "snippets.snippet.read",
        "snippets.snippet.create",
        "snippets.snippet.save",
        "snippets.snippet.archive",
        "snippets.snippet.unarchive",
        "reading.queue.read",
        "reading.item.save",
        "reading.item.remove",
        "reading.item.read",
        "reading.item.unread",
        "reading.item.position",
        "organization.tag.read",
        "organization.tag.create",
        "organization.tag.rename",
        "organization.tag.remove",
        "toolbox.catalog.read",
        "toolbox.base64.run",
        "toolbox.url_codec.run",
        "toolbox.html_codec.run",
        "toolbox.hash.run",
        "toolbox.uuid.run",
        "toolbox.password.run",
        "toolbox.json.run",
        "toolbox.regex.run",
        "toolbox.xml.run",
        "toolbox.datetime.run",
        "toolbox.csv.run",
        "toolbox.jwt.run",
        "goals.goal.read",
        "goals.goal.create",
        "goals.goal.update",
        "goals.goal.start",
        "goals.goal.complete",
        "goals.goal.abandon",
        "goals.goal.reopen",
        "goals.goal.history",
        "goals.goal.archive",
        "goals.goal.unarchive",
        "goals.target.read",
        "goals.target.create",
        "goals.target.update",
        "goals.target.remove",
        "goals.target.record_progress",
        "dashboard.dashboard.read",
        "discovery.search.query",
        "discovery.favorite.read",
        "discovery.favorite.add",
        "discovery.favorite.remove",
        "discovery.favorite.reorder"
    };

    private static readonly HashSet<string> PausedPrefixes = new(StringComparer.Ordinal)
    {
        "prices.",
        "automation.",
        "integrations."
    };

    private static readonly HashSet<string> BlockedOutsideM01 = new(StringComparer.Ordinal)
    {
        "toolbox.network.http",
        "toolbox.network.dns"
    };

    // Current product authority is the exact M01 action set from main's
    // effective-status overlay. The larger inventory above is retained only
    // so old PR code can be identified during review; it must not authorize
    // a new grant outside this set.
    private static readonly HashSet<string> ApprovedM01Actions = new(StringComparer.Ordinal)
    {
        "identity.account.register",
        "identity.account.verify",
        "identity.account.resend",
        "identity.account.login",
        "identity.account.reset_request",
        "identity.account.reset_confirm",
        "identity.session.logout",
        "identity.session.read",
        "identity.session.revoke_session",
        "identity.session.revoke_all",
        "identity.profile.read",
        "identity.profile.update",
        "access.user.read",
        "access.permission.read",
        "access.change.read",
        "access.role.set",
        "access.permission.set",
        "access.entitlement.set",
        "modules.catalog.read",
        "modules.policy.enable",
        "modules.policy.disable",
        "modules.policy.defaults",
        "modules.policy.sharing",
        "notifications.dispatch.publish",
        "notifications.dispatch.deliver",
        "settings.preference.read",
        "settings.preference.update"
    };

    // This is an explicit projection of the current M01 action manifest's
    // AdminGrantable field. It is intentionally not inferred from a verb,
    // namespace or database row: SUPER/CONTROL/SYSTEM/PUBLIC actions and
    // outside-M01 actions must fail closed even if a stale SQL row exists.
    private static readonly HashSet<string> AdminGrantableActions = new(StringComparer.Ordinal)
    {
        // Exact M01 actions from main's effective action set. Do not grow this
        // list from PR-only module code until a later slice is approved.
        "access.user.read",
        "access.permission.read",
        "modules.catalog.read",
        "settings.preference.read",
        "settings.preference.update"
    };

    // Exact shipped SELF catalog eligibility; no grant, SYSTEM, provider or uninstalled action.
    private static readonly HashSet<string> ApprovedShippedSelfActions = new(StringComparer.Ordinal)
    {
        "organization.collection.read", "organization.collection.create", "organization.collection.update",
        "organization.collection.add", "organization.collection.remove", "organization.collection.reorder", "organization.collection.delete",
        "toolbox.catalog.read", "toolbox.base64.run", "toolbox.url_codec.run", "toolbox.html_codec.run",
        "toolbox.hash.run", "toolbox.uuid.run", "toolbox.password.run", "toolbox.json.run", "toolbox.regex.run",
        "toolbox.xml.run", "toolbox.datetime.run", "toolbox.csv.run", "toolbox.jwt.run",
        "toolbox.text_diff.run", "toolbox.color.run",
        "toolbox.url_parse.run", "toolbox.headers.run",
        "documents.format.export",
        "documents.format.import",
        "notifications.source.open",
        "finance.manual_category.read",
        "finance.manual_category.create",
        "finance.manual_category.update",
        "finance.manual_category.remove",
        "finance.manual_record.read",
        "finance.manual_record.create",
        "finance.manual_record.update",
        "finance.manual_summary.read",
        "notifications.inbox.read",
        "notifications.inbox.mark_read",
        "notifications.inbox.mark_unread",
        "notifications.inbox.mark_all_read",
        "notifications.inbox.delete",
        "documents.folder.read", "documents.folder.create", "documents.folder.rename", "documents.folder.trash", "documents.folder.restore", "documents.folder.purge",
        "documents.page.trash", "documents.page.restore", "documents.page.purge",
        "documents.library.read",
        "documents.page.read",
        "documents.page.create",
        "documents.page.save",
        "documents.page.publish",
        "documents.page.unpublish",
        "documents.page.archive",
        "documents.page.unarchive",
        "documents.page.history",
        "documents.page.restore_version",
        "documents.tag.read",
        "documents.tag.create",
        "documents.tag.rename",
        "documents.tag.remove",
        "organization.tag.read",
        "organization.tag.create",
        "organization.tag.rename",
        "organization.tag.remove",
        "discovery.search.query",
    };

    // Local Time/Focus SELF actions; support and SYSTEM actions are not grantable.
    private static readonly HashSet<string> ApprovedTimeAndFocusActions = new(StringComparer.Ordinal)
    {
        "time.timer.read", "time.timer.start", "time.timer.stop", "time.timer.resume",
        "time.entry.read", "time.entry.create", "time.entry.update", "time.entry.trash",
        "time.entry.restore", "time.entry.history", "time.entry.purge", "time.report.read",
        "focus.session.read", "focus.session.start", "focus.session.pause",
        "focus.session.resume", "focus.session.cancel", "focus.preference.update", "focus.session.record_time"
    };

    public static PolicyDecision CanGrantAllow(string actionKey, string? installedContract = null)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
        {
            return PolicyDecision.Deny("UnknownField", "Action key is required.");
        }

        var installedAutomationAction = installedContract == "automation-definition-v1" && ApprovedAutomationDefinitionActions.Contains(actionKey);
        var installedIntegrationAction = installedContract == "integration-connection-v1" && ApprovedIntegrationConnectionActions.Contains(actionKey);
        if (!installedAutomationAction && !installedIntegrationAction && PausedPrefixes.Any(prefix => actionKey.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Paused action cannot be granted Allow.");
        }

        if (BlockedOutsideM01.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Network toolbox action is inactive until a future PO/network decision.");
        }

        if (!ApprovedM01Actions.Contains(actionKey) && !ApprovedTimeAndFocusActions.Contains(actionKey) && !ApprovedWishlistActions.Contains(actionKey) && !ApprovedSkillActions.Contains(actionKey) && !ApprovedCourseActions.Contains(actionKey) && !ApprovedPersonalAssetActions.Contains(actionKey) && !ApprovedDigitalAssetActions.Contains(actionKey) && !ApprovedCareerActions.Contains(actionKey) && !ApprovedCalendarImportActions.Contains(actionKey) && !ApprovedCalendarExportActions.Contains(actionKey) && !ApprovedSharingActions.Contains(actionKey) && !ApprovedMonitoringActions.Contains(actionKey) && !ApprovedNewsCategoryActions.Contains(actionKey) && !ApprovedFilesActions.Contains(actionKey) && !ApprovedAutomationDefinitionActions.Contains(actionKey) && !ApprovedIntegrationConnectionActions.Contains(actionKey) && !ApprovedProductivityActions.Contains(actionKey) && !ApprovedSnippetActions.Contains(actionKey) && !ApprovedBookmarkActions.Contains(actionKey) && !ApprovedReadingActions.Contains(actionKey) && !ApprovedShippedSelfActions.Contains(actionKey) && !ApprovedProductivityLensActions.Contains(actionKey) && !ApprovedSavedSearchActions.Contains(actionKey) && !ApprovedDashboardLayoutActions.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Action is not approved for implementation or grant in the current M01 scope.");
        }

        if (!AdminGrantableActions.Contains(actionKey) && !ApprovedTimeAndFocusActions.Contains(actionKey) && !ApprovedWishlistActions.Contains(actionKey) && !ApprovedSkillActions.Contains(actionKey) && !ApprovedCourseActions.Contains(actionKey) && !ApprovedPersonalAssetActions.Contains(actionKey) && !ApprovedDigitalAssetActions.Contains(actionKey) && !ApprovedCareerActions.Contains(actionKey) && !ApprovedCalendarImportActions.Contains(actionKey) && !ApprovedCalendarExportActions.Contains(actionKey) && !ApprovedSharingActions.Contains(actionKey) && !ApprovedMonitoringActions.Contains(actionKey) && !ApprovedNewsCategoryActions.Contains(actionKey) && !ApprovedFilesActions.Contains(actionKey) && !ApprovedAutomationDefinitionActions.Contains(actionKey) && !ApprovedIntegrationConnectionActions.Contains(actionKey) && !ApprovedProductivityActions.Contains(actionKey) && !ApprovedSnippetActions.Contains(actionKey) && !ApprovedBookmarkActions.Contains(actionKey) && !ApprovedReadingActions.Contains(actionKey) && !ApprovedShippedSelfActions.Contains(actionKey) && !ApprovedProductivityLensActions.Contains(actionKey) && !ApprovedSavedSearchActions.Contains(actionKey) && !ApprovedDashboardLayoutActions.Contains(actionKey))
        {
            return PolicyDecision.Deny("ActionNotGrantable", "Action is not assignable as an Admin grant in the current action manifest.");
        }

        return PolicyDecision.Allow("GrantAllowed", "Action is approved for M01 grant mutation.");
    }

    // Exact main FX23 SELF eligibility for the shipped Bookmark-reference queue.
    // Explicit user-approved six-key proposal; no grants/provider/Support actions.
    private static readonly HashSet<string> ApprovedReadingActions = new(StringComparer.Ordinal)
    { "reading.queue.read", "reading.item.save", "reading.item.remove", "reading.item.read", "reading.item.unread", "reading.item.position" };

    // Main FX21 catalog Admin eligibility for the exact shipped local manual subset.
    // No grants are written; current SQL Allow/Deny, owner and live session proof remains mandatory.
    private static readonly HashSet<string> ApprovedBookmarkActions = new(StringComparer.Ordinal)
    { "bookmarks.bookmark.read", "bookmarks.bookmark.create", "bookmarks.bookmark.update", "bookmarks.bookmark.archive", "bookmarks.bookmark.unarchive" };

    // Exact installed text/version SELF keys implementing main FX22 Admin eligibility.
    // Eligibility alone cannot authorize access; every current SQL Allow/owner/session check remains.
    private static readonly HashSet<string> ApprovedSnippetActions = new(StringComparer.Ordinal)
    { "snippets.snippet.read", "snippets.snippet.create", "snippets.snippet.save", "snippets.snippet.archive", "snippets.snippet.unarchive" };

    // Current DEC-014 local implementation: only canonical actions with shipped Productivity handlers.
    // Eligibility does not create grants; SQL current module/action/dependency and owner checks remain required.
    private static readonly HashSet<string> ApprovedProductivityActions = new(StringComparer.Ordinal)
    {
        "projects.project.read", "projects.project.create", "projects.project.update", "projects.project.start",
        "projects.project.revert", "projects.project.complete", "projects.project.skip", "projects.project.trash",
        "tasks.task.read", "tasks.task.create", "tasks.task.update", "tasks.task.start",
        "tasks.task.complete", "tasks.task.skip", "tasks.task.revert", "tasks.task.trash",
        "tasks.task.criteria", "tasks.task.reorder", "tasks.task.set_reminder", "tasks.task.remove_reminder",
        "reminders.configuration.read", "reminders.configuration.set", "reminders.configuration.remove",
        "calendar.event.set_reminder", "calendar.event.remove_reminder",
        "calendar.event.read", "calendar.event.create", "calendar.event.update", "calendar.event.complete", "calendar.event.cancel"
    };

    private static readonly HashSet<string> ApprovedIntegrationConnectionActions = new(StringComparer.Ordinal)
    { "integrations.connection.read", "integrations.connection.create", "integrations.connection.update", "integrations.connection.disable" };
    // Only syntax preflight may use this; grant commit still requires server-derived installation evidence.
    public static bool IsInstalledContractCandidate(string actionKey) => ApprovedAutomationDefinitionActions.Contains(actionKey) || ApprovedIntegrationConnectionActions.Contains(actionKey);

    // Exact shipped local definition/manual execution subset. Installation evidence
    // is required by CanGrantAllow; worker, schedule and provider actions stay denied.
    private static readonly HashSet<string> ApprovedAutomationDefinitionActions = new(StringComparer.Ordinal)
    { "automation.definition.read", "automation.definition.create", "automation.definition.save", "automation.definition.history", "automation.definition.validate", "automation.definition.trash", "automation.definition.restore", "automation.definition.import", "automation.definition.export", "automation.definition.enable", "automation.definition.disable", "automation.run.start", "automation.run.read", "automation.run.cancel", "automation.run.retry_step", "automation.schedule.update", "automation.definition.purge" };

    // Exact reviewed private file subset; other file handlers remain inactive.
    private static readonly HashSet<string> ApprovedFilesActions = new(StringComparer.Ordinal)
    { "files.file.read", "files.file.upload", "files.file.download", "files.file.rename", "files.file.trash", "files.file.restore" };

    // Reviewed provider-specific local ICS export; no standalone Project/Task exporter.
    private static readonly HashSet<string> ApprovedCalendarExportActions = new(StringComparer.Ordinal)
    {
        "transfer.export.request", "transfer.export.read", "transfer.export.download", "calendar.ics.export",
        "projects.project.read"
    };

    // Reviewed local import and exact already-installed source prerequisites; no export/operational keys.
    private static readonly HashSet<string> ApprovedCalendarImportActions = new(StringComparer.Ordinal)
    {
        "transfer.import.preview", "transfer.import.commit", "transfer.import.read", "transfer.import.cancel",
        "calendar.ics.preview", "calendar.ics.import", "calendar.event.create", "calendar.event.read",
        "files.file.read", "files.file.download", "files.file.upload", "tasks.task.read"
    };
    private static readonly HashSet<string> ApprovedCareerActions = new(StringComparer.Ordinal)
    {
        "career.company.read", "career.company.create", "career.company.update", "career.company.merge",
        "career.job.read", "career.job.create", "career.job.update", "career.job.transition", "career.job.history", "career.job.trash", "career.job.restore", "career.job.purge",
        "career.appointment.read", "career.appointment.create_event", "career.appointment.link_event", "career.appointment.unlink_event"
    };
    private static readonly HashSet<string> ApprovedDigitalAssetActions = new(StringComparer.Ordinal)
    {
        "digital.asset.read", "digital.asset.create", "digital.asset.update", "digital.asset.cancel", "digital.asset.history", "digital.renewal.record",
        "digital.asset.archive", "digital.asset.unarchive", "digital.asset.trash", "digital.asset.restore", "digital.asset.purge"
    };
    private static readonly HashSet<string> ApprovedPersonalAssetActions = new(StringComparer.Ordinal)
    {
        "assets.asset.read", "assets.asset.create", "assets.asset.update", "assets.asset.transition", "assets.asset.history",
        "assets.asset.archive", "assets.asset.unarchive", "assets.asset.trash", "assets.asset.restore", "assets.asset.purge"
    };
    private static readonly HashSet<string> ApprovedCourseActions = new(StringComparer.Ordinal)
    {
        "learning.course.read", "learning.course.create", "learning.course.update", "learning.course.progress", "learning.course.milestone",
        "learning.course.complete", "learning.course.abandon", "learning.course.archive", "learning.course.unarchive",
        "learning.course.trash", "learning.course.restore", "learning.course.purge"
    };
    private static readonly HashSet<string> ApprovedSkillActions = new(StringComparer.Ordinal)
    {
        "learning.skill.read", "learning.skill.create", "learning.skill.update", "learning.skill.proficiency", "learning.skill.evidence", "learning.skill.merge",
        "learning.skill.archive", "learning.skill.unarchive", "learning.skill.trash", "learning.skill.restore", "learning.skill.purge",
        "learning.worklog.read", "learning.worklog.create", "learning.worklog.update", "learning.worklog.archive", "learning.worklog.unarchive", "learning.report.read",
        "learning.worklog.trash", "learning.worklog.restore", "learning.worklog.purge"
    };

    private static readonly HashSet<string> ApprovedNewsCategoryActions = new(StringComparer.Ordinal)
    {
        "news.category.read", "news.category.create", "news.category.update",
        "news.source.read", "news.source.follow", "news.source.update", "news.source.pause", "news.source.resume", "news.source.unfollow",
        "news.article.read", "news.article.mark_read", "news.article.mark_unread", "news.article.mark_all_read", "news.article.save", "news.history.read", "news.history.clear"
    };

    // Exact reviewed local configuration SELF subset; no checks/jobs/observations/support.
    private static readonly HashSet<string> ApprovedMonitoringActions = new(StringComparer.Ordinal)
    { "monitoring.monitor.read", "monitoring.monitor.create", "monitoring.monitor.update", "monitoring.monitor.pause", "monitoring.monitor.resume" };

    private static readonly HashSet<string> ApprovedSharingActions = new(StringComparer.Ordinal)
    {
        "sharing.link.read", "sharing.link.create", "sharing.link.update", "sharing.link.revoke",
        "projects.project.read", "projects.project.share", "documents.page.read", "documents.page.share"
    };

    private static readonly HashSet<string> ApprovedWishlistActions = new(StringComparer.Ordinal)
    {
        "shopping.wishlist.read", "shopping.wishlist.create", "shopping.wishlist.update",
        "shopping.wishlist.mark_purchased", "shopping.wishlist.archive", "shopping.wishlist.unarchive",
        "shopping.wishlist.trash", "shopping.wishlist.restore", "shopping.wishlist.purge"
    };

    public static bool IsApprovedForLocalAction(string actionKey) => ApprovedM01Actions.Contains(actionKey) || ApprovedTimeAndFocusActions.Contains(actionKey) || ApprovedWishlistActions.Contains(actionKey) || ApprovedSkillActions.Contains(actionKey) || ApprovedCourseActions.Contains(actionKey) || ApprovedPersonalAssetActions.Contains(actionKey) || ApprovedDigitalAssetActions.Contains(actionKey) || ApprovedCareerActions.Contains(actionKey) || ApprovedCalendarImportActions.Contains(actionKey) || ApprovedCalendarExportActions.Contains(actionKey) || ApprovedSharingActions.Contains(actionKey) || ApprovedMonitoringActions.Contains(actionKey) || ApprovedNewsCategoryActions.Contains(actionKey) || ApprovedFilesActions.Contains(actionKey) || ApprovedAutomationDefinitionActions.Contains(actionKey) || ApprovedIntegrationConnectionActions.Contains(actionKey) || ApprovedProductivityActions.Contains(actionKey) || ApprovedSnippetActions.Contains(actionKey) || ApprovedBookmarkActions.Contains(actionKey) || ApprovedReadingActions.Contains(actionKey) || ApprovedShippedSelfActions.Contains(actionKey) || ApprovedProductivityLensActions.Contains(actionKey) || ApprovedSavedSearchActions.Contains(actionKey) || ApprovedDashboardLayoutActions.Contains(actionKey);

    // Installed owner-only Planner/Goals/Habits handlers. Eligibility for an
    // explicit grant is not an Allow row or module enablement. Support, trash,
    // reminder dispatch and unshipped target operations are deliberately absent.
    private static readonly HashSet<string> ApprovedProductivityLensActions = new(StringComparer.Ordinal)
    {
        "planner.plan.read", "planner.plan.pin", "planner.plan.unpin", "planner.plan.reorder", "planner.plan.reschedule", "planner.plan.notes",
        "goals.goal.read", "goals.goal.create", "goals.goal.update", "goals.goal.start", "goals.goal.complete", "goals.goal.abandon", "goals.goal.reopen",
        "goals.goal.history", "goals.goal.archive", "goals.goal.unarchive",
        "goals.target.read", "goals.target.create", "goals.target.record_progress", "goals.target.update", "goals.target.remove",
        "habits.habit.read", "habits.streak.read", "habits.habit.create", "habits.habit.update", "habits.habit.schedule",
        "habits.habit.set_reminder", "habits.habit.pause", "habits.habit.resume", "habits.habit.archive", "habits.habit.unarchive",
        "habits.checkin.record", "habits.checkin.correct"
    };

    private static readonly HashSet<string> ApprovedSavedSearchActions = new(StringComparer.Ordinal)
    {
        "discovery.saved_search.read", "discovery.saved_search.create", "discovery.saved_search.update",
        "discovery.saved_search.delete", "discovery.saved_search.run",
        "discovery.recent.read", "discovery.recent.clear", "discovery.command.read", "discovery.command.execute"
    };

    private static readonly HashSet<string> ApprovedDashboardLayoutActions = new(StringComparer.Ordinal)
    {
        "dashboard.layout.update", "dashboard.layout.add_widget", "dashboard.layout.configure_widget",
        "dashboard.layout.remove_widget", "dashboard.layout.reorder_widget", "dashboard.widget.refresh", "dashboard.quick_create.open"
    };

    public static bool IsAdminGrantable(string actionKey) => AdminGrantableActions.Contains(actionKey) || ApprovedTimeAndFocusActions.Contains(actionKey) || ApprovedWishlistActions.Contains(actionKey) || ApprovedSkillActions.Contains(actionKey) || ApprovedCourseActions.Contains(actionKey) || ApprovedPersonalAssetActions.Contains(actionKey) || ApprovedDigitalAssetActions.Contains(actionKey) || ApprovedCareerActions.Contains(actionKey) || ApprovedCalendarImportActions.Contains(actionKey) || ApprovedCalendarExportActions.Contains(actionKey) || ApprovedSharingActions.Contains(actionKey) || ApprovedMonitoringActions.Contains(actionKey) || ApprovedNewsCategoryActions.Contains(actionKey) || ApprovedFilesActions.Contains(actionKey) || ApprovedAutomationDefinitionActions.Contains(actionKey) || ApprovedIntegrationConnectionActions.Contains(actionKey) || ApprovedProductivityActions.Contains(actionKey) || ApprovedSnippetActions.Contains(actionKey) || ApprovedBookmarkActions.Contains(actionKey) || ApprovedReadingActions.Contains(actionKey) || ApprovedShippedSelfActions.Contains(actionKey) || ApprovedProductivityLensActions.Contains(actionKey) || ApprovedSavedSearchActions.Contains(actionKey) || ApprovedDashboardLayoutActions.Contains(actionKey);
}
