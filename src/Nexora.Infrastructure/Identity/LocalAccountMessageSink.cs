using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// Local captured transport for synthetic M01 accounts. It writes one
/// expiring message per file outside the web root, never logs the token and is
/// idempotent by message id. Reading the capture is an explicit local operator
/// CLI operation; there is no HTTP/mailbox endpoint.
/// </summary>
public sealed class LocalAccountMessageSink : IAccountMessageSink
{
    private readonly string _captureDirectory;
    private readonly ILogger<LocalAccountMessageSink> _logger;

    public LocalAccountMessageSink(string captureDirectory, string contentRootPath, ILogger<LocalAccountMessageSink> logger)
    {
        if (string.IsNullOrWhiteSpace(captureDirectory))
        {
            throw new ArgumentException("A local account-message capture directory is required.", nameof(captureDirectory));
        }

        _captureDirectory = Path.GetFullPath(captureDirectory);
        var webRoot = Path.GetFullPath(Path.Combine(contentRootPath, "wwwroot"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var webRootPrefix = webRoot + Path.DirectorySeparatorChar;
        if (string.Equals(_captureDirectory, webRoot, comparison) ||
            _captureDirectory.StartsWith(webRootPrefix, comparison) ||
            _captureDirectory.StartsWith(webRoot + Path.AltDirectorySeparatorChar, comparison))
        {
            throw new ArgumentException("Local account-message capture must be outside the web root.", nameof(captureDirectory));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Directory.CreateDirectory(_captureDirectory);
        RestrictDirectoryPermissions(_captureDirectory);
    }

    public void Publish(LocalAccountMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var now = DateTimeOffset.UtcNow;
        if (message.ExpiresAt <= now)
        {
            throw new InvalidOperationException("The account-message token has expired.");
        }

        PurgeExpired(now);
        var destination = MessagePath(message.Id);
        if (File.Exists(destination))
        {
            return;
        }

        var temporary = Path.Combine(_captureDirectory, $".{message.Id:N}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       bufferSize: 4096, options: FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, message);
                stream.Flush(flushToDisk: true);
            }

            RestrictFilePermissions(temporary);
            try
            {
                File.Move(temporary, destination);
            }
            catch (IOException) when (File.Exists(destination))
            {
                // Another local worker won the idempotent publish race.
            }

            _logger.LogDebug("Captured local account-message {MessageId} for purpose {Purpose}.", message.Id, message.Purpose);
        }
        finally
        {
            TryDelete(temporary);
        }
    }

    /// <summary>
    /// Explicit local operator read path. This method is not registered in the
    /// API and returns only non-expired captures from the configured directory.
    /// </summary>
    public static IReadOnlyList<LocalAccountMessage> ReadCaptured(string captureDirectory, DateTimeOffset? now = null)
    {
        if (string.IsNullOrWhiteSpace(captureDirectory) || !Directory.Exists(captureDirectory))
        {
            return Array.Empty<LocalAccountMessage>();
        }

        var current = now ?? DateTimeOffset.UtcNow;
        var messages = new List<LocalAccountMessage>();
        foreach (var path in Directory.EnumerateFiles(Path.GetFullPath(captureDirectory), "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var message = JsonSerializer.Deserialize<LocalAccountMessage>(stream);
                if (message is not null && message.ExpiresAt > current)
                {
                    messages.Add(message);
                }
            }
            catch (IOException)
            {
                // A concurrent publish may still be moving the file.
            }
            catch (JsonException)
            {
                // Ignore a malformed capture; it is not a valid transport message.
            }
        }

        return messages.OrderByDescending(message => message.CreatedAt).ToArray();
    }

    private void PurgeExpired(DateTimeOffset now)
    {
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var message = JsonSerializer.Deserialize<LocalAccountMessage>(stream);
                if (message is not null && message.ExpiresAt <= now)
                {
                    TryDelete(path);
                }
            }
            catch (IOException)
            {
                // Leave files involved in another local operation for the next sweep.
            }
            catch (JsonException)
            {
                // A malformed file is not deleted by the token-retention sweep.
            }
        }
    }

    private string MessagePath(Guid messageId) => Path.Combine(_captureDirectory, $"{messageId:N}.json");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // The bounded worker will retry the surrounding delivery operation.
        }
        catch (UnauthorizedAccessException)
        {
            // The bounded worker will retry the surrounding delivery operation.
        }
    }

    private static void RestrictDirectoryPermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void RestrictFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
