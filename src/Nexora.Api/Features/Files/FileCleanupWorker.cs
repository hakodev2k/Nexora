using Microsoft.Data.SqlClient;
using Nexora.Application.Files;

namespace Nexora.Api.Features.Files;

/// <summary>
/// Retries only durable, owner-scoped storage cleanup intents. It has no
/// provider or public file behavior and never derives a path from an
/// unvalidated request value.
/// </summary>
public sealed class FileCleanupWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IFileCleanupService _cleanup;
    private readonly ILogger<FileCleanupWorker> _logger;

    public FileCleanupWorker(IFileCleanupService cleanup, ILogger<FileCleanupWorker> logger)
    {
        _cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        do
        {
            try
            {
                await _cleanup.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (SqlException)
            {
                _logger.LogWarning("Private file cleanup is degraded because SQL is unavailable.");
            }
            catch (IOException)
            {
                _logger.LogWarning("Private file cleanup is degraded because local storage is unavailable.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
