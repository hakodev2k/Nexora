using Microsoft.Data.SqlClient;
using Nexora.Application.Reminders;

namespace Nexora.Api.Features.Reminders;

/// <summary>
/// Local-only dispatcher shell. Provider execution is intentionally absent:
/// the SQL service writes an InApp projection and marks email/browser channels
/// unavailable or permission-limited without sending anything externally.
/// </summary>
public sealed class ReminderDispatchWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IReminderDispatchService _dispatcher;
    private readonly ILogger<ReminderDispatchWorker> _logger;

    public ReminderDispatchWorker(IReminderDispatchService dispatcher, ILogger<ReminderDispatchWorker> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await _dispatcher.DispatchDueAsync(stoppingToken);
            }
            catch (SqlException)
            {
                _logger.LogWarning("Local reminder dispatch is degraded because SQL is unavailable.");
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Local reminder dispatch is degraded because its SQL configuration is unavailable.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
