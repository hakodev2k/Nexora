using Nexora.Domain.Common;

namespace Nexora.Domain.Modules;

public sealed record ModuleDependencyStatus(string ModuleCode, bool SystemEnabled, bool Required);

public static class ModulePolicy
{
    private static readonly HashSet<string> PoPausedModuleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FX30",
        "FX34",
        "Automation",
        "FX35",
        "PriceTracking",
        "Integrations"
    };

    public static PolicyDecision CanEnable(
        string moduleCode,
        ModuleRuntimeState runtimeState,
        IEnumerable<ModuleDependencyStatus> dependencies,
        string? installedContract = null)
    {
        var installedLocalSubset =
            ((moduleCode.Equals("FX34", StringComparison.OrdinalIgnoreCase) || moduleCode.Equals("Automation", StringComparison.OrdinalIgnoreCase)) && installedContract == "automation-definition-v1") ||
            ((moduleCode.Equals("FX35", StringComparison.OrdinalIgnoreCase) || moduleCode.Equals("Integrations", StringComparison.OrdinalIgnoreCase)) && installedContract == "integration-connection-v1");
        if (PoPausedModuleCodes.Contains(moduleCode) && !installedLocalSubset)
        {
            return PolicyDecision.Deny("DecisionBlocked", "Product Owner-paused modules cannot be enabled by M01 policy.");
        }

        return runtimeState switch
        {
            ModuleRuntimeState.Ready or ModuleRuntimeState.Disabled => ValidateDependencies(dependencies),
            ModuleRuntimeState.Paused => PolicyDecision.Deny("DecisionBlocked", "Paused module cannot be enabled."),
            ModuleRuntimeState.Uninstalled => PolicyDecision.Deny("DependencyUnavailable", "Uninstalled module cannot be enabled."),
            ModuleRuntimeState.MigrationFailed => PolicyDecision.Deny("DependencyUnavailable", "Migration-failed module cannot be enabled."),
            ModuleRuntimeState.Blocked => PolicyDecision.Deny("DecisionBlocked", "Blocked module cannot be enabled."),
            _ => PolicyDecision.Deny("DependencyUnavailable", "Unknown module state cannot be enabled.")
        };
    }

    public static PolicyDecision CanDisable(IEnumerable<ModuleDependencyStatus> dependents)
    {
        var enabledDependent = dependents.FirstOrDefault(dependent => dependent.Required && dependent.SystemEnabled);
        return enabledDependent is null
            ? PolicyDecision.Allow("DisableAllowed", "No enabled hard dependent blocks disablement.")
            : PolicyDecision.Deny("DependencyEnabled", $"Enabled hard dependent '{enabledDependent.ModuleCode}' blocks disablement.");
    }

    private static PolicyDecision ValidateDependencies(IEnumerable<ModuleDependencyStatus> dependencies)
    {
        var missing = dependencies.FirstOrDefault(dependency => dependency.Required && !dependency.SystemEnabled);
        return missing is null
            ? PolicyDecision.Allow("EnableAllowed", "Module can be enabled after dependency validation.")
            : PolicyDecision.Deny("DependencyUnavailable", $"Required dependency '{missing.ModuleCode}' is unavailable.");
    }
}
