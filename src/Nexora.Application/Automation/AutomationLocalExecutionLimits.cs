namespace Nexora.Application.Automation;

/// <summary>Local Task adapter safety bounds, not a provider quota or activation grant.</summary>
public sealed record AutomationLocalExecutionLimits
{
    public int SystemConcurrency { get; }
    public int OwnerConcurrency { get; }
    public AutomationLocalExecutionLimits(int systemConcurrency = 1, int ownerConcurrency = 1)
    {
        if (systemConcurrency is < 1 or > 4 || ownerConcurrency < 1 || ownerConcurrency > systemConcurrency)
            throw new ArgumentOutOfRangeException(nameof(systemConcurrency), "Local concurrency must satisfy 1 <= owner <= system <= 4.");
        SystemConcurrency = systemConcurrency;
        OwnerConcurrency = ownerConcurrency;
    }
}

public sealed record AutomationExecutionPolicy(bool LocalExecutionEnabled, int SystemConcurrency,
    int OwnerConcurrency, int DefinitionConcurrency, int RunDeadlineSeconds, int QueueMaxAgeMinutes);
