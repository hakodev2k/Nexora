using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Nexora.Application.Automation;

/// <summary>Local Task adapter safety bounds, not a provider quota or activation grant.</summary>
public sealed record AutomationLocalExecutionLimits
{
    public int SystemConcurrency { get; }
    public int OwnerConcurrency { get; }
    public int RunDeadlineSeconds { get; }
    public int MaxStepAttempts { get; }
    public int RetryBaseSeconds { get; }
    public int RetryMaxSeconds { get; }
    public AutomationLocalExecutionLimits(int systemConcurrency = 1, int ownerConcurrency = 1,
        int runDeadlineSeconds = 120, int maxStepAttempts = 4, int retryBaseSeconds = 5, int retryMaxSeconds = 60)
    {
        if (systemConcurrency is < 1 or > 4 || ownerConcurrency < 1 || ownerConcurrency > systemConcurrency)
            throw new ArgumentOutOfRangeException(nameof(systemConcurrency), "Local concurrency must satisfy 1 <= owner <= system <= 4.");
        SystemConcurrency = systemConcurrency;
        OwnerConcurrency = ownerConcurrency;
        if (runDeadlineSeconds is < 30 or > 120 || maxStepAttempts is < 1 or > 4 ||
            retryBaseSeconds is < 1 or > 30 || retryMaxSeconds < retryBaseSeconds || retryMaxSeconds > 60)
            throw new ArgumentOutOfRangeException(nameof(runDeadlineSeconds), "Local deadline30..120s, attempts1..4, retry base1..30s and base<=max<=60s required.");
        RunDeadlineSeconds = runDeadlineSeconds;
        MaxStepAttempts = maxStepAttempts;
        RetryBaseSeconds = retryBaseSeconds;
        RetryMaxSeconds = retryMaxSeconds;
    }
    public int RetryDelaySeconds(Guid commandKey, int failedAttempt)
    {
        if (failedAttempt is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(failedAttempt));
        var ceiling = Math.Min(RetryMaxSeconds, RetryBaseSeconds * (1 << (failedAttempt - 1)));
        var floor = Math.Max(1, ceiling / 2);
        Span<byte> input = stackalloc byte[20]; commandKey.TryWriteBytes(input);
        BinaryPrimitives.WriteInt32LittleEndian(input[16..], failedAttempt);
        // Stable bounded jitter by logical effect and attempt; restart does not select a fresh delay.
        var sample = BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(input));
        return floor + (int)(sample % (uint)(ceiling - floor + 1));
    }
}

public sealed record AutomationExecutionPolicy(bool LocalExecutionEnabled, int SystemConcurrency,
    int OwnerConcurrency, int DefinitionConcurrency, int RunDeadlineSeconds, int QueueMaxAgeMinutes,
    int MaxStepAttempts, int RetryBaseSeconds, int RetryMaxSeconds);
