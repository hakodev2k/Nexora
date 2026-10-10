using System.Collections.Immutable;

namespace Nexora.Application.Automation;

// Contract registration is not runtime availability or authority to dispatch.
public sealed record AutomationContribution(
    string Key, int Version, string ModuleCode,
    bool ExecutionAvailable, bool VerifiedSimulation, bool ExternalEffects);

public static class AutomationContractRegistry
{
    public static ImmutableArray<AutomationContribution> Triggers { get; } =
    [new("manual", 1, "Automation", false, false, false)];

    public static ImmutableArray<AutomationContribution> Actions { get; } =
    [new("tasks.task.create", 1, "Tasks", false, false, false)];

    public static AutomationContribution? FindTrigger(string key, int version) =>
        Triggers.FirstOrDefault(item => item.Key == key && item.Version == version);

    public static AutomationContribution? FindAction(string key, int version) =>
        Actions.FirstOrDefault(item => item.Key == key && item.Version == version);
}
