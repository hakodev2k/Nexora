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
