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

        if (!ApprovedM01Actions.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Action is not approved for implementation or grant in the current M01 scope.");
        }

        if (!AdminGrantableActions.Contains(actionKey))
        {
            return PolicyDecision.Deny("ActionNotGrantable", "Action is not assignable as an Admin grant in the current action manifest.");
        }

        return PolicyDecision.Allow("GrantAllowed", "Action is approved for M01 grant mutation.");
    }

    public static bool IsApprovedForLocalAction(string actionKey) => ApprovedM01Actions.Contains(actionKey);

    public static bool IsAdminGrantable(string actionKey) => AdminGrantableActions.Contains(actionKey);
}
