using Microsoft.Data.SqlClient;
using Nexora.Application.Productivity;
using Nexora.Infrastructure.Automation;
namespace Nexora.Api.Features.Automation;

public sealed class AutomationTaskWorker(SqlAutomationDefinitionService automation, IProductivityService tasks, ILogger<AutomationTaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var warnedAt=DateTimeOffset.MinValue;
        using var timer=new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while(await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { automation.ProcessNext(tasks); }
                catch(SqlException) { if(DateTimeOffset.UtcNow-warnedAt>TimeSpan.FromMinutes(1)) { logger.LogWarning("Automation local worker storage unavailable; durable work retained."); warnedAt=DateTimeOffset.UtcNow; } await Task.Delay(TimeSpan.FromSeconds(15),stoppingToken); }
                catch(InvalidOperationException) { if(DateTimeOffset.UtcNow-warnedAt>TimeSpan.FromMinutes(1)) { logger.LogWarning("Automation local worker integrity unavailable; durable work retained."); warnedAt=DateTimeOffset.UtcNow; } await Task.Delay(TimeSpan.FromSeconds(15),stoppingToken); }
            }
        }
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { }
    }
}
