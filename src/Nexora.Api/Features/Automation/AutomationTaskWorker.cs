using Microsoft.Data.SqlClient;
using Nexora.Application.Productivity;
using Nexora.Infrastructure.Automation;
namespace Nexora.Api.Features.Automation;

public sealed class AutomationTaskWorker(SqlAutomationDefinitionService automation, IProductivityService tasks, ILogger<AutomationTaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var lanesStop = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        // Fixed bounded lanes; SQL claims coordinate all hosts. Scheduler is independent of a slow Task effect.
        var lanes = Enumerable.Range(0, automation.ExecutionLimits.SystemConcurrency)
            .Select(_ => Task.Run(() => RunLane(false), lanesStop.Token)).ToList();
        lanes.Add(Task.Run(() => RunLane(true), lanesStop.Token));
        await Task.WhenAll(lanes);
        async Task RunLane(bool scheduler)
        {
            try { await Loop(scheduler, lanesStop.Token); }
            catch { lanesStop.Cancel(); throw; } // Do not leave a failed lane silently beside never-ending healthy lanes.
        }
    }
    private async Task Loop(bool scheduler, CancellationToken stoppingToken)
    {
        var warnedAt = DateTimeOffset.MinValue;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested) return;
                try { if (scheduler) automation.ProcessDueSchedule(); else automation.ProcessNext(tasks, stoppingToken); }
                catch (SqlException) { await StorageBackoff(); }
                catch (InvalidOperationException) { await StorageBackoff(); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        async Task StorageBackoff()
        {
            if (DateTimeOffset.UtcNow - warnedAt > TimeSpan.FromMinutes(1))
            { logger.LogWarning("Automation local worker storage/integrity unavailable; durable work retained."); warnedAt = DateTimeOffset.UtcNow; }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
