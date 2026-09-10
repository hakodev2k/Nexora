using Nexora.Domain.Common;

namespace Nexora.Domain.Access;

public static class ActionGrantPolicy
{
    private static readonly HashSet<string> ApprovedForM01 = new(StringComparer.Ordinal)
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

        // DEC-014 permits the implemented local Release 1 self slices. These
        // canonical catalog keys are grantable for Admin SELF access; the
        // legacy aliases above remain accepted so existing local rows continue
        // to behave deterministically during the migration window.
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
        "snippets.snippet.unarchive"
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

        if (!ApprovedForM01.Contains(actionKey))
        {
            return PolicyDecision.Deny("DecisionBlocked", "Action is not approved for implementation or grant in the current M01 slice.");
        }

        return PolicyDecision.Allow("GrantAllowed", "Action is approved for M01 grant mutation.");
    }

    public static bool IsApprovedForM01(string actionKey) => ApprovedForM01.Contains(actionKey);
}
