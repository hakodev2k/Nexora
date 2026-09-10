using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// Local-only delivery adapter. It records delivery metadata without writing
/// raw verification/reset tokens to logs. A real provider or an explicitly
/// approved local operator transport must be supplied for token delivery.
/// </summary>
public sealed class LocalAccountMessageSink : IAccountMessageSink
{
    private readonly ILogger<LocalAccountMessageSink> _logger;
    public LocalAccountMessageSink(ILogger<LocalAccountMessageSink> logger)
    {
        _logger = logger;
    }

    public void Publish(LocalAccountMessage message)
    {
        // The raw token is intentionally not included in this log entry. The
        // durable AccountMessageIntent/Outbox rows contain only token metadata
        // and a real delivery adapter must be wired before sending mail.
        _logger.LogInformation(
            "A local {Purpose} delivery intent was queued for the account address; token delivery requires an approved local transport and expires at {ExpiresAt:o}.",
            message.Purpose, message.ExpiresAt);
    }
}
