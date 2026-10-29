using Microsoft.Data.SqlClient;
using Nexora.Application.Focus;

namespace Nexora.Api.Features.Focus;

public sealed class FocusPhaseWorker(IFocusPhaseFinisher finisher, ILogger<FocusPhaseWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try { await finisher.FinishDueAsync(stoppingToken); }
                catch (Exception e) when (e is SqlException or TimeoutException)
                { logger.LogWarning("Focus phase persistence unavailable; effects remain uncommitted."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
