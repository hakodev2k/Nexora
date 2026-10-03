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
        "goals.goal.read",
        "goals.goal.create",
        "goals.goal.update",
        "goals.goal.start",
        "goals.goal.complete",
        "goals.goal.abandon",
        "goals.goal.reopen",
        "goals.target.read",
        "goals.target.create",
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

    // Local Time/Focus SELF actions; support and SYSTEM actions are not grantable.
    private static readonly HashSet<string> ApprovedTimeAndFocusActions = new(StringComparer.Ordinal)
    {
        "time.timer.read", "time.timer.start", "time.timer.stop", "time.timer.resume",
        "time.entry.read", "time.entry.create", "time.entry.update", "time.entry.trash",
        "time.entry.restore", "time.entry.history", "time.entry.purge", "time.report.read",
        "focus.session.read", "focus.session.start", "focus.session.pause",
        "focus.session.resume", "focus.session.cancel", "focus.preference.update", "focus.session.record_time"
    };

    public static PolicyDecision CanGrantAllow(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
        {
            return PolicyDecision.Deny("UnknownField", "Action key is required.");
        }

        if (PausedPrefixes.Any(prefix => actionKey.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Paused action cannot be granted Allow.");
        }

        if (BlockedOutsideM01.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Network toolbox action is inactive until a future PO/network decision.");
        }

        if (!ApprovedM01Actions.Contains(actionKey) && !ApprovedTimeAndFocusActions.Contains(actionKey) && !ApprovedWishlistActions.Contains(actionKey) && !ApprovedSkillActions.Contains(actionKey) && !ApprovedCourseActions.Contains(actionKey) && !ApprovedPersonalAssetActions.Contains(actionKey) && !ApprovedDigitalAssetActions.Contains(actionKey) && !ApprovedCareerActions.Contains(actionKey) && !ApprovedCalendarImportActions.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Action is not approved for implementation or grant in the current M01 scope.");
        }

        if (!AdminGrantableActions.Contains(actionKey) && !ApprovedTimeAndFocusActions.Contains(actionKey) && !ApprovedWishlistActions.Contains(actionKey) && !ApprovedSkillActions.Contains(actionKey) && !ApprovedCourseActions.Contains(actionKey) && !ApprovedPersonalAssetActions.Contains(actionKey) && !ApprovedDigitalAssetActions.Contains(actionKey) && !ApprovedCareerActions.Contains(actionKey) && !ApprovedCalendarImportActions.Contains(actionKey))
        {
            return PolicyDecision.Deny("ActionNotGrantable", "Action is not assignable as an Admin grant in the current action manifest.");
        }

        return PolicyDecision.Allow("GrantAllowed", "Action is approved for M01 grant mutation.");
    }

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
        "career.job.read", "career.job.create", "career.job.update", "career.job.transition", "career.job.history", "career.job.trash", "career.job.restore", "career.job.purge"
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
        "learning.skill.read", "learning.skill.create", "learning.skill.update", "learning.skill.proficiency",
        "learning.skill.archive", "learning.skill.unarchive", "learning.skill.trash", "learning.skill.restore", "learning.skill.purge"
    };

    private static readonly HashSet<string> ApprovedWishlistActions = new(StringComparer.Ordinal)
    {
        "shopping.wishlist.read", "shopping.wishlist.create", "shopping.wishlist.update",
        "shopping.wishlist.mark_purchased", "shopping.wishlist.archive", "shopping.wishlist.unarchive",
        "shopping.wishlist.trash", "shopping.wishlist.restore", "shopping.wishlist.purge"
    };

    public static bool IsApprovedForLocalAction(string actionKey) => ApprovedM01Actions.Contains(actionKey) || ApprovedTimeAndFocusActions.Contains(actionKey) || ApprovedWishlistActions.Contains(actionKey) || ApprovedSkillActions.Contains(actionKey) || ApprovedCourseActions.Contains(actionKey) || ApprovedPersonalAssetActions.Contains(actionKey) || ApprovedDigitalAssetActions.Contains(actionKey) || ApprovedCareerActions.Contains(actionKey) || ApprovedCalendarImportActions.Contains(actionKey);

    public static bool IsAdminGrantable(string actionKey) => AdminGrantableActions.Contains(actionKey) || ApprovedTimeAndFocusActions.Contains(actionKey) || ApprovedWishlistActions.Contains(actionKey) || ApprovedSkillActions.Contains(actionKey) || ApprovedCourseActions.Contains(actionKey) || ApprovedPersonalAssetActions.Contains(actionKey) || ApprovedDigitalAssetActions.Contains(actionKey) || ApprovedCareerActions.Contains(actionKey) || ApprovedCalendarImportActions.Contains(actionKey);
}
