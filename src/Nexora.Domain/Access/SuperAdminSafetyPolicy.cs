using Nexora.Domain.Common;

namespace Nexora.Domain.Access;

public static class SuperAdminSafetyPolicy
{
    public static PolicyDecision CanRemoveOrDowngradeSuperAdmin(int activeSuperAdminCount, bool targetIsActiveSuperAdmin)
    {
        if (!targetIsActiveSuperAdmin)
        {
            return PolicyDecision.Allow("NotLastSuperAdmin", "Target is not an active SuperAdmin.");
        }

        if (activeSuperAdminCount <= 1)
        {
            return PolicyDecision.Deny("LastSuperAdmin", "The final active SuperAdmin cannot be removed, disabled, downgraded, or deleted.");
        }

        return PolicyDecision.Allow("SuperAdminRemovalAllowed", "Another active SuperAdmin remains.");
    }
}
