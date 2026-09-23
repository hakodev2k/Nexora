using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
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
public sealed class LocalAccountMessageSink : IAccountMessageSink, IAccountMessageEffectSink
{
    private static readonly object CaptureGate = new();
    private static readonly TimeSpan OrphanTemporaryGrace = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MalformedCaptureGrace = TimeSpan.FromHours(24);
    private static readonly UnixFileMode PrivateDirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private static readonly UnixFileMode PrivateFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private readonly string _captureDirectory;
    private readonly IReadOnlyList<SecurityIdentifier>? _allowedWindowsSids;
    private readonly ILogger<LocalAccountMessageSink> _logger;

    public LocalAccountMessageSink(
        string captureDirectory,
        string contentRootPath,
        ILogger<LocalAccountMessageSink> logger,
        string? operatorSid = null,
        string? runtimeSid = null)
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

        _allowedWindowsSids = CreateWindowsAclPolicy(
            runtimeSid,
            operatorSid,
            requireConfiguredRuntime: !string.IsNullOrWhiteSpace(operatorSid));
        ValidateCurrentWindowsIdentity(_allowedWindowsSids);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var existed = Directory.Exists(_captureDirectory);
        Directory.CreateDirectory(_captureDirectory);
        RejectReparsePoint(_captureDirectory);
        if (existed)
        {
            // An upgrade must not silently bless a directory that was already
            // readable by another principal. The operator can repair it
            // explicitly, after which startup validates the resulting ACL.
            ValidateDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
        }
        else
        {
            RestrictDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
            ValidateDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
        }
    }

    /// <summary>
    /// The unfenced compatibility path is intentionally unavailable. Durable
    /// delivery must enter through the effect contract below so a caller cannot
    /// turn a stale SQL envelope into a local capture without an authority
    /// check.
    /// </summary>
    public void Publish(LocalAccountMessage message) =>
        throw new InvalidOperationException("Local account-message delivery requires a current effect fence.");

    public LocalAccountMessagePublishOutcome Publish(
        LocalAccountMessage message,
        Guid effectFence,
        Func<bool> isCurrent)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(isCurrent);
        if (effectFence == Guid.Empty)
        {
            throw new ArgumentException("A non-empty local delivery effect fence is required.", nameof(effectFence));
        }

        var now = DateTimeOffset.UtcNow;
        if (message.ExpiresAt <= now)
        {
            throw new InvalidOperationException("The account-message token has expired.");
        }

        if (!isCurrent())
        {
            return LocalAccountMessagePublishOutcome.AuthorityLost;
        }

        lock (CaptureGate)
        {
            ValidateDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
            SweepExpiredCore(now);
            var destination = MessagePath(message.Id);
            if (File.Exists(destination))
            {
                var existing = ReadCapture(destination);
                if (existing is { Message.Id: var existingId } && existingId == message.Id)
                {
                    return isCurrent()
                        ? LocalAccountMessagePublishOutcome.AlreadyCaptured
                        : LocalAccountMessagePublishOutcome.AuthorityLost;
                }

                throw new InvalidDataException("The local account-message capture is invalid.");
            }

            var pending = PendingPath(message.Id, effectFence);
            if (File.Exists(pending))
            {
                var existing = ReadCapture(pending);
                if (existing is { Message.Id: var existingId } && existingId == message.Id &&
                    existing.Value.DeliveryFence == effectFence)
                {
                    return isCurrent()
                        ? LocalAccountMessagePublishOutcome.Prepared
                        : LocalAccountMessagePublishOutcome.AuthorityLost;
                }

                throw new InvalidDataException("The local account-message pending capture is invalid.");
            }

            var temporary = Path.Combine(_captureDirectory, $".{message.Id:N}.{effectFence:N}.tmp");
            var createdTemporary = false;
            try
            {
                using (var stream = CreateTemporaryCapture(temporary))
                {
                    createdTemporary = true;
                    var locked = false;
                    try
                    {
                        // FileShare.None protects this process/Windows. The
                        // advisory lock also lets a Linux cleanup process
                        // distinguish an active writer from an orphan.
                        LockCaptureStream(stream);
                        locked = true;
                        JsonSerializer.Serialize(stream, CapturedAccountMessage.From(message, effectFence));
                        stream.Flush(flushToDisk: true);
                    }
                    finally
                    {
                        if (locked)
                        {
                            UnlockCaptureStream(stream);
                        }
                    }
                }

                // Recheck immediately before the atomic move into the private,
                // non-readable pending area. The operator CLI only reads JSON.
                if (!isCurrent())
                {
                    return LocalAccountMessagePublishOutcome.AuthorityLost;
                }

                try
                {
                    File.Move(temporary, pending);
                }
                catch (IOException) when (File.Exists(pending))
                {
                    var existing = ReadCapture(pending);
                    if (existing is { Message.Id: var existingId } && existingId == message.Id &&
                        existing.Value.DeliveryFence == effectFence)
                    {
                        return isCurrent()
                            ? LocalAccountMessagePublishOutcome.Prepared
                            : LocalAccountMessagePublishOutcome.AuthorityLost;
                    }

                    throw new InvalidDataException("The local account-message pending capture is invalid.");
                }

                // A lease can expire while the filesystem operation is in
                // progress. Remove only a pending capture carrying this exact
                // fence; a later worker's capture is never treated as ours.
                if (!isCurrent())
                {
                    RemoveIfOwnedCore(message.Id, effectFence);
                    return LocalAccountMessagePublishOutcome.AuthorityLost;
                }

                _logger.LogDebug("Prepared local account-message {MessageId} for purpose {Purpose}.", message.Id, message.Purpose);
                return LocalAccountMessagePublishOutcome.Prepared;
            }
            finally
            {
                if (createdTemporary)
                {
                    TryDelete(temporary);
                }
            }
        }
    }

    private FileStream CreateTemporaryCapture(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsTemporaryCapture(path);
        }

        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            BufferSize = 4096,
            Options = FileOptions.WriteThrough
        };

        // FileStreamOptions applies the mode during open, before any bytes can
        // exist. This closes the umask-022 window on Linux. Windows creation is
        // also safe in the already validated private directory; the explicit
        // ACL is applied before the first write below.
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = PrivateFileMode;
        }

        var stream = new FileStream(path, options);
        try
        {
            RestrictFilePermissions(path, _allowedWindowsSids);
            ValidateFilePermissions(path, _allowedWindowsSids);
            return stream;
        }
        catch
        {
            stream.Dispose();
            TryDelete(path);
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private FileStream CreateWindowsTemporaryCapture(string path)
    {
        var security = CreatePrivateWindowsFileSecurity(_allowedWindowsSids);
        var stream = new FileInfo(path).Create(
            FileMode.CreateNew,
            FileSystemRights.FullControl,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.WriteThrough,
            security);
        try
        {
            // The ACL is supplied to Create rather than set after opening.
            // A process crash between creation and the first write therefore
            // cannot leave a plaintext temporary capture with inherited ACLs.
            ValidateFilePermissions(path, _allowedWindowsSids);
            return stream;
        }
        catch
        {
            stream.Dispose();
            TryDelete(path);
            throw;
        }
    }

    public bool PromoteIfOwned(Guid messageId, Guid effectFence)
    {
        if (messageId == Guid.Empty || effectFence == Guid.Empty)
        {
            return false;
        }

        lock (CaptureGate)
        {
            return PromoteIfOwnedCore(messageId, effectFence);
        }
    }

    public void RemoveIfOwned(Guid messageId, Guid effectFence)
    {
        if (messageId == Guid.Empty || effectFence == Guid.Empty)
        {
            return;
        }

        lock (CaptureGate)
        {
            RemoveIfOwnedCore(messageId, effectFence);
        }
    }

    private bool PromoteIfOwnedCore(Guid messageId, Guid effectFence)
    {
        var pending = PendingPath(messageId, effectFence);
        var destination = MessagePath(messageId);
        if (!File.Exists(pending))
        {
            return File.Exists(destination) && IsCaptureForFence(destination, messageId, effectFence) is true;
        }

        CaptureReadResult? capture;
        try
        {
            ValidateFilePermissions(pending, _allowedWindowsSids);
            capture = ReadCapture(pending);
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            TryDelete(pending);
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        if (capture is not { Message.Id: var pendingId } || pendingId != messageId ||
            capture.Value.DeliveryFence != effectFence)
        {
            return false;
        }

        if (File.Exists(destination))
        {
            if (IsCaptureForFence(destination, messageId, effectFence) is true)
            {
                TryDelete(pending);
                return true;
            }

            return false;
        }

        try
        {
            File.Move(pending, destination);
            return true;
        }
        catch (IOException) when (File.Exists(destination))
        {
            if (IsCaptureForFence(destination, messageId, effectFence) is true)
            {
                TryDelete(pending);
                return true;
            }

            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Explicit local operator read path. This method is not registered in the
    /// API and returns only non-expired captures from the configured directory.
    /// </summary>
    public static IReadOnlyList<LocalAccountMessage> ReadCaptured(
        string captureDirectory,
        DateTimeOffset? now = null,
        string? operatorSid = null,
        string? runtimeSid = null)
    {
        if (string.IsNullOrWhiteSpace(captureDirectory) || !Directory.Exists(captureDirectory))
        {
            return Array.Empty<LocalAccountMessage>();
        }

        var directory = Path.GetFullPath(captureDirectory);
        RejectReparsePoint(directory);
        var allowedWindowsSids = CreateWindowsAclPolicy(
            runtimeSid,
            operatorSid,
            requireConfiguredRuntime: !string.IsNullOrWhiteSpace(operatorSid));
        ValidateCurrentWindowsIdentity(allowedWindowsSids);
        ValidateDirectoryPermissions(directory, allowedWindowsSids);
        var current = now ?? DateTimeOffset.UtcNow;
        var messages = new List<LocalAccountMessage>();
        var expired = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            // The filename is the first ownership boundary. Do not parse,
            // validate, inspect timestamps, or delete a JSON file merely
            // because its content happens to look like a capture.
            if (!TryGetOwnedCaptureIdentity(path, out var fileMessageId))
            {
                continue;
            }

            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, allowedWindowsSids);
                var capture = ReadCapture(path);
                if (capture is null)
                {
                    if (File.GetLastWriteTimeUtc(path) <= current.UtcDateTime.Subtract(MalformedCaptureGrace))
                    {
                        expired.Add(path);
                    }

                    continue;
                }

                if (capture.Value.Message.Id != fileMessageId)
                {
                    // A valid JSON payload with the wrong identity is not a
                    // capture and is intentionally left for operator review.
                    Console.Error.WriteLine("Local account-message capture identity mismatch; file was not returned or removed.");
                    continue;
                }

                if (capture.Value.Message.ExpiresAt > current)
                {
                    messages.Add(capture.Value.Message);
                }
                else
                {
                    expired.Add(path);
                }
            }
            catch (IOException)
            {
                // A concurrent publish may still be moving the file.
            }
            catch (JsonException)
            {
                // A parse failure has no content-based ownership proof. The
                // private directory plus the exact capture filename and a
                // bounded age are the only cleanup authority available.
                if (IsOlderThan(path, current.UtcDateTime.Subtract(MalformedCaptureGrace)) &&
                    IsFileReadyForCleanup(path))
                {
                    expired.Add(path);
                }
            }
            catch (InvalidOperationException)
            {
                // A single unsafe/reparse/unsupported file is not allowed to
                // abort the operator read or retention pass.
                Console.Error.WriteLine("Local account-message capture permission or ownership validation failed; file was skipped.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Local account-message capture permission validation failed; file was skipped.");
            }
        }

        foreach (var path in expired)
        {
            // A completed capture normally has no writer, but a file that was
            // interrupted or altered outside this process must still pass the
            // same cross-process readiness check as malformed/temporary data.
            // The helper releases its handle before Delete, which keeps the
            // Windows delete boundary valid.
            if (IsFileReadyForCleanup(path))
            {
                TryDeleteSilently(path);
            }
        }

        return messages.OrderByDescending(message => message.CreatedAt).ToArray();
    }

    public void SweepExpired(DateTimeOffset now)
    {
        lock (CaptureGate)
        {
            ValidateDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
            SweepExpiredCore(now);
        }
    }

    public int ReconcilePending(Func<Guid, Guid, LocalAccountMessagePendingDisposition> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        var reconciled = 0;
        var now = DateTimeOffset.UtcNow;
        lock (CaptureGate)
        {
            ValidateDirectoryPermissions(_captureDirectory, _allowedWindowsSids);
            foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.pending", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    RejectReparsePoint(path);
                    ValidateFilePermissions(path, _allowedWindowsSids);
                    if (!TryGetOwnedPendingIdentity(path, out var fileMessageId, out var fileEffectFence))
                    {
                        continue;
                    }

                    var capture = ReadCapture(path);
                    if (capture is null || capture.Value.DeliveryFence is not { } effectFence ||
                        capture.Value.Message.Id == Guid.Empty ||
                        capture.Value.Message.Id != fileMessageId ||
                        effectFence != fileEffectFence)
                    {
                        continue;
                    }

                    var disposition = resolve(fileMessageId, fileEffectFence);
                    if (disposition == LocalAccountMessagePendingDisposition.Promote)
                    {
                        reconciled += PromoteIfOwnedCore(fileMessageId, fileEffectFence) ? 1 : 0;
                    }
                    else if (disposition == LocalAccountMessagePendingDisposition.Remove)
                    {
                        RemoveIfOwnedCore(fileMessageId, fileEffectFence);
                        reconciled++;
                    }
                }
                catch (IOException)
                {
                    // Leave a file involved in another local operation for the next pass.
                }
                catch (JsonException)
                {
                    if (IsOlderThan(path, now.UtcDateTime.Subtract(MalformedCaptureGrace)) &&
                        IsFileReadyForCleanup(path))
                    {
                        if (TryDelete(path))
                        {
                            reconciled++;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    _logger.LogWarning("Local account-message pending capture validation failed; file was skipped.");
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Local account-message pending capture permission validation failed; file was skipped.");
                }
            }
        }

        return reconciled;
    }

    private void SweepExpiredCore(DateTimeOffset now)
    {
        var deleteAfterClose = new List<string>();
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            // Never inspect or remove a final capture whose filename is not
            // owned by this adapter. Content cannot establish ownership.
            if (!TryGetOwnedCaptureIdentity(path, out var fileMessageId))
            {
                continue;
            }

            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, _allowedWindowsSids);
                var capture = ReadCapture(path);
                if (capture is not null && capture.Value.Message.Id != fileMessageId)
                {
                    _logger.LogWarning("Local account-message capture identity mismatch; file was left for operator review.");
                    continue;
                }

                if ((capture is not null && capture.Value.Message.ExpiresAt <= now) ||
                    (capture is null &&
                     File.GetLastWriteTimeUtc(path) <= now.UtcDateTime.Subtract(MalformedCaptureGrace)))
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (IOException)
            {
                // Leave files involved in another local operation for the next sweep.
            }
            catch (JsonException)
            {
                if (IsOlderThan(path, now.UtcDateTime.Subtract(MalformedCaptureGrace)) &&
                    IsFileReadyForCleanup(path))
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Local account-message capture permission or ownership validation failed; file was skipped.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Local account-message capture permission validation failed; file was skipped.");
            }
        }

        var temporaryCutoff = now.UtcDateTime.Subtract(OrphanTemporaryGrace);
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.tmp", SearchOption.TopDirectoryOnly))
        {
            try
            {
                RejectReparsePoint(path);
                if (!IsOwnedTemporaryFile(path))
                {
                    continue;
                }

                // A legacy Windows crash can leave an ACL that prevents the
                // advisory-lock probe. Validate it separately so an old,
                // exact adapter temporary does not prevent a later delivery
                // from reaching its own claim/effect path. This branch never
                // reads its contents and File.Delete remains blocked by an
                // active writer that omitted FileShare.Delete.
                if (OperatingSystem.IsWindows() && IsOlderThan(path, temporaryCutoff))
                {
                    try
                    {
                        ValidateTemporaryFilePermissions(path, _allowedWindowsSids);
                    }
                    catch (InvalidOperationException)
                    {
                        if (!TryDisposeUntrustedTemporaryOrphan(path, temporaryCutoff))
                        {
                            _logger.LogWarning("Local account-message temporary capture validation failed; the owned candidate remains quarantined for operator recovery.");
                        }

                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (!TryDisposeUntrustedTemporaryOrphan(path, temporaryCutoff))
                        {
                            _logger.LogWarning("Local account-message temporary capture permission validation failed; the owned candidate remains quarantined for operator recovery.");
                        }

                        continue;
                    }
                }

                // Do not let an invalid legacy file ACL poison delivery. An
                // adapter-owned temporary candidate must first be stale and
                // demonstrably inactive; only then can a failed ACL check
                // take the bounded terminal-recovery path in the catch block.
                if (!IsOlderThan(path, temporaryCutoff) || !IsFileReadyForCleanup(path))
                {
                    continue;
                }

                // A process can die between CreateNew and the file-level ACL
                // call. On Windows the already-validated private directory
                // makes its inherited ACL safe; on Unix UnixCreateMode makes
                // the file private at creation. Any other ACL/mode is rejected
                // per file and cannot stop delivery of later messages.
                ValidateTemporaryFilePermissions(path, _allowedWindowsSids);
                if (
                    IsOlderThan(path, temporaryCutoff) &&
                    IsFileReadyForCleanup(path))
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (IOException)
            {
                // Leave a file that is still being written for the next sweep.
            }
            catch (InvalidOperationException)
            {
                if (TryDisposeUntrustedTemporaryOrphan(path, temporaryCutoff))
                {
                    continue;
                }

                _logger.LogWarning("Local account-message temporary capture permission validation failed; file was skipped.");
            }
            catch (UnauthorizedAccessException)
            {
                if (TryDisposeUntrustedTemporaryOrphan(path, temporaryCutoff))
                {
                    continue;
                }

                _logger.LogWarning("Local account-message temporary capture permission validation failed; file was skipped.");
            }
        }

        var pendingCutoff = now.UtcDateTime.Subtract(TimeSpan.FromHours(24));
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.pending", SearchOption.TopDirectoryOnly))
        {
            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, _allowedWindowsSids);
                if (!TryGetOwnedPendingIdentity(path, out var fileMessageId, out var fileEffectFence))
                {
                    continue;
                }

                var capture = ReadCapture(path);
                var captureBelongsToPath = capture is
                    {
                        Message.Id: var storedMessageId,
                        DeliveryFence: var storedEffectFence
                    } &&
                    storedMessageId == fileMessageId &&
                    storedEffectFence == fileEffectFence;
                if (!captureBelongsToPath)
                {
                    _logger.LogWarning("Local account-message pending capture identity mismatch; file was left for operator review.");
                    continue;
                }

                if ((capture!.Value.Message.ExpiresAt <= now ||
                     IsOlderThan(path, pendingCutoff)) &&
                    IsFileReadyForCleanup(path))
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (IOException)
            {
                // Leave a pending file for reconciliation or the next sweep.
            }
            catch (JsonException)
            {
                if (IsOwnedPendingFile(path) &&
                    IsOlderThan(path, now.UtcDateTime.Subtract(MalformedCaptureGrace)) &&
                    IsFileReadyForCleanup(path))
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning("Local account-message pending capture permission or ownership validation failed; file was skipped.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Local account-message pending capture permission validation failed; file was skipped.");
            }
        }

        foreach (var path in deleteAfterClose.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            // Recheck immediately before deletion. Earlier parsing has closed
            // its handle, but another process can begin writing while a sweep
            // is progressing; do not delete an active owned capture.
            if (IsFileReadyForCleanup(path))
            {
                TryDelete(path);
            }
        }
    }

    private void RemoveIfOwnedCore(Guid messageId, Guid effectFence)
    {
        RemovePathIfOwned(PendingPath(messageId, effectFence), messageId, effectFence);
        RemovePathIfOwned(MessagePath(messageId), messageId, effectFence);
    }

    private string MessagePath(Guid messageId) => Path.Combine(_captureDirectory, $"{messageId:N}.json");

    private string PendingPath(Guid messageId, Guid effectFence) =>
        Path.Combine(_captureDirectory, $".{messageId:N}.{effectFence:N}.pending");

    private void RemovePathIfOwned(string path, Guid messageId, Guid effectFence)
    {
        if (!File.Exists(path))
        {
            return;
        }

        if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            (!TryGetOwnedCaptureIdentity(path, out var fileMessageId) || fileMessageId != messageId))
        {
            return;
        }

        try
        {
            ValidateFilePermissions(path, _allowedWindowsSids);
            var capture = ReadCapture(path);
            if (capture is { Message.Id: var storedId, DeliveryFence: var storedFence } &&
                storedId == messageId && storedFence == effectFence)
            {
                // ReadCapture has closed its handle before this delete.
                TryDelete(path);
            }
        }
        catch (IOException)
        {
            // The next bounded sweep/retry can remove an owned stale capture.
        }
        catch (JsonException)
        {
            // Never delete an unbound file through an ownership cleanup path.
        }
    }

    private bool IsCaptureForFence(string path, Guid messageId, Guid effectFence)
    {
        if (!TryGetOwnedCaptureIdentity(path, out var fileMessageId) || fileMessageId != messageId)
        {
            return false;
        }

        try
        {
            ValidateFilePermissions(path, _allowedWindowsSids);
            var capture = ReadCapture(path);
            return capture is { Message.Id: var storedId, DeliveryFence: var storedFence } &&
                storedId == messageId && storedFence == effectFence;
        }
        catch (IOException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
        catch (IOException)
        {
            _logger.LogWarning("Local account-message cleanup will retry after the file handle is released.");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Local account-message cleanup could not remove an owned capture file.");
            return false;
        }
    }

    private bool TryDisposeUntrustedTemporaryOrphan(string path, DateTime cutoff)
    {
        // Unix mode validation is authoritative. A stale temporary with an
        // unsafe mode must remain quarantined for operator recovery rather
        // than being deleted merely because its adapter-shaped file name is
        // old. The bounded recovery path below exists only for legacy Windows
        // ACLs where the validated private parent provides the safe boundary.
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            // This deliberately never reads or parses an untrusted file. The
            // only deletion authority is the already validated private parent
            // directory plus the exact adapter temporary name, non-reparse
            // target, orphan age and inactive-writer check. Foreign names,
            // pending/final JSON and active writers remain untouched.
            RejectReparsePoint(path);
            if (!IsOwnedTemporaryFile(path) || !IsOlderThan(path, cutoff))
            {
                return false;
            }

            if (OperatingSystem.IsLinux() && !IsFileReadyForCleanup(path))
            {
                return false;
            }

            // On Windows File.Delete fails while a writer omitted FileShare.Delete.
            // That is the cross-process active-writer boundary when the unsafe
            // ACL prevents opening the old orphan for an advisory lock.
            if (!OperatingSystem.IsWindows() && !IsFileReadyForCleanup(path))
            {
                return false;
            }

            if (!TryDelete(path))
            {
                return false;
            }

            _logger.LogWarning("Disposed a stale untrusted local account-message temporary capture candidate after bounded orphan recovery.");
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void TryDeleteSilently(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("Local account-message cleanup will retry after the file handle is released.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Local account-message cleanup could not remove an owned capture file.");
        }
    }

    private static bool IsOlderThan(string path, DateTime cutoff)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path) <= cutoff;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsFileReadyForCleanup(string path)
    {
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: 1,
                options: FileOptions.SequentialScan);
            var locked = false;
            try
            {
                LockCaptureStream(stream);
                locked = true;
                return true;
            }
            finally
            {
                if (locked)
                {
                    UnlockCaptureStream(stream);
                }
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static CaptureReadResult? ReadCapture(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A local account-message capture must contain a JSON object.");
        }

        if (document.RootElement.TryGetProperty("DeliveryFence", out _))
        {
            var capture = document.RootElement.Deserialize<CapturedAccountMessage>();
            return capture is null ? null : new CaptureReadResult(capture.ToMessage(), capture.DeliveryFence);
        }

        var legacy = document.RootElement.Deserialize<LocalAccountMessage>();
        return legacy is null ? null : new CaptureReadResult(legacy, null);
    }

    private static void LockCaptureStream(FileStream stream)
    {
        if (OperatingSystem.IsWindows())
        {
            LockCaptureStreamCore(stream);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            LockCaptureStreamCore(stream);
            return;
        }

        throw new PlatformNotSupportedException("Local account-message capture supports Windows and Linux file locking only.");
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    private static void LockCaptureStreamCore(FileStream stream) => stream.Lock(0, 1);

    private static void UnlockCaptureStream(FileStream stream)
    {
        if (OperatingSystem.IsWindows())
        {
            UnlockCaptureStreamCore(stream);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            UnlockCaptureStreamCore(stream);
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    private static void UnlockCaptureStreamCore(FileStream stream) => stream.Unlock(0, 1);

    private static bool IsOwnedCaptureFile(string path) =>
        TryGetOwnedCaptureIdentity(path, out _);

    private static bool TryGetOwnedCaptureIdentity(string path, out Guid messageId)
    {
        messageId = Guid.Empty;
        return Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out messageId) &&
            messageId != Guid.Empty;
    }

    private static bool IsOwnedTemporaryFile(string path)
    {
        return TryGetOwnedPendingIdentity(path, out _, out _);
    }

    private static bool TryGetOwnedPendingIdentity(
        string path,
        out Guid messageId,
        out Guid effectFence)
    {
        messageId = Guid.Empty;
        effectFence = Guid.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var parts = name.Split('.', StringSplitOptions.None);
        return parts.Length == 3 && parts[0].Length == 0 &&
            Guid.TryParseExact(parts[1], "N", out messageId) &&
            Guid.TryParseExact(parts[2], "N", out effectFence) &&
            messageId != Guid.Empty &&
            effectFence != Guid.Empty;
    }

    private static bool IsOwnedPendingFile(string path) =>
        IsOwnedTemporaryFile(path);

    private static void RestrictDirectoryPermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (OperatingSystem.IsWindows())
        {
            if (allowedWindowsSids is null)
            {
                throw new InvalidOperationException("The local account-message ACL policy is unavailable.");
            }

            var security = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            RemoveAccessRules(security);
            foreach (var sid in allowedWindowsSids)
            {
                security.AddAccessRule(new FileSystemAccessRule(
                    sid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow));
            }

            new DirectoryInfo(path).SetAccessControl(security);
            return;
        }

        File.SetUnixFileMode(path, PrivateDirectoryMode);
    }

    private static void RestrictFilePermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (OperatingSystem.IsWindows())
        {
            if (allowedWindowsSids is null)
            {
                throw new InvalidOperationException("The local account-message ACL policy is unavailable.");
            }

            var security = new FileInfo(path).GetAccessControl(AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            RemoveAccessRules(security);
            foreach (var sid in allowedWindowsSids)
            {
                security.AddAccessRule(new FileSystemAccessRule(
                    sid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow));
            }

            new FileInfo(path).SetAccessControl(security);
            return;
        }

        File.SetUnixFileMode(path, PrivateFileMode);
    }

    private static void ValidateDirectoryPermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
            ValidateWindowsPermissions(security, allowedWindowsSids);
            return;
        }

        ValidateUnixPermissions(path, PrivateDirectoryMode);
    }

    private static void ValidateFilePermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(path).GetAccessControl(AccessControlSections.Access);
            ValidateWindowsPermissions(security, allowedWindowsSids);
            return;
        }

        ValidateUnixPermissions(path, PrivateFileMode);
    }

    private static void ValidateTemporaryFilePermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (OperatingSystem.IsWindows())
        {
            // New temporaries have a protected ACL at CreateNew time. An
            // inherited legacy ACL is not a trusted temporary capture.
            ValidateFilePermissions(path, allowedWindowsSids);
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            ValidateFilePermissions(path, allowedWindowsSids);
            return;
        }

        ValidateTemporaryWindowsFilePermissions(path, allowedWindowsSids);
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateTemporaryWindowsFilePermissions(
        string path,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        var security = new FileInfo(path).GetAccessControl(AccessControlSections.Access);
        // A crash can happen after CreateNew but before SetAccessControl. The
        // parent directory was already validated as private, so an ACL made up
        // solely of inherited full-control entries for the configured
        // identities is safe for this temporary state. Any other inheritance or
        // ACE remains fail-closed.
        ValidateWindowsPermissions(security, allowedWindowsSids, allowSafeInheritedFile: true);
    }

    [UnsupportedOSPlatform("windows")]
    private static void ValidateUnixPermissions(string path, UnixFileMode expectedOwnerMode)
    {
        try
        {
            var mode = File.GetUnixFileMode(path);
            var groupOrOther = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                               UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
            if ((mode & groupOrOther) != 0 || (mode & expectedOwnerMode) != expectedOwnerMode)
            {
                throw new InvalidOperationException("Local account-message capture permissions are not private.");
            }
        }
        catch (PlatformNotSupportedException exception)
        {
            throw new InvalidOperationException("Local account-message capture permissions cannot be validated.", exception);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateWindowsPermissions(
        FileSystemSecurity security,
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids,
        bool allowSafeInheritedFile = false)
    {
        var allowed = allowedWindowsSids
            ?? throw new InvalidOperationException("The local account-message ACL policy is unavailable.");
        if (!security.AreAccessRulesProtected && !allowSafeInheritedFile)
        {
            throw new InvalidOperationException("Local account-message capture ACL inheritance is not private.");
        }

        var found = new HashSet<SecurityIdentifier>();
        foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            if ((!security.AreAccessRulesProtected && !rule.IsInherited) ||
                (security.AreAccessRulesProtected && rule.IsInherited) ||
                rule.AccessControlType != AccessControlType.Allow ||
                rule.IdentityReference is not SecurityIdentifier sid || !allowed.Any(allowedSid => allowedSid.Equals(sid)) ||
                (rule.FileSystemRights & FileSystemRights.FullControl) != FileSystemRights.FullControl)
            {
                throw new InvalidOperationException("Local account-message capture ACL is not private.");
            }

            found.Add(sid);
        }

        if (allowed.Any(sid => !found.Contains(sid)))
        {
            throw new InvalidOperationException("Local account-message capture ACL is not private.");
        }

    }

    [SupportedOSPlatform("windows")]
    private static FileSecurity CreatePrivateWindowsFileSecurity(
        IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        var allowed = allowedWindowsSids
            ?? throw new InvalidOperationException("The local account-message ACL policy is unavailable.");
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        foreach (var sid in allowed)
        {
            security.AddAccessRule(new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Allow));
        }

        return security;
    }

    private static IReadOnlyList<SecurityIdentifier>? CreateWindowsAclPolicy(
        string? runtimeSid,
        string? operatorSid,
        bool requireConfiguredRuntime)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return CreateWindowsAclPolicyCore(runtimeSid, operatorSid, requireConfiguredRuntime);
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<SecurityIdentifier> CreateWindowsAclPolicyCore(
        string? runtimeSid,
        string? operatorSid,
        bool requireConfiguredRuntime)
    {
        var runtime = ParseWindowsSid(runtimeSid, "runtime");
        if (runtime is null)
        {
            if (requireConfiguredRuntime)
            {
                throw new InvalidOperationException(
                    "A configured local account-message runtime identity is required when an operator identity is configured.");
            }

            runtime = WindowsIdentity.GetCurrent().User
                ?? throw new InvalidOperationException("The local account-message runtime identity is unavailable.");
        }

        var result = new List<SecurityIdentifier> { runtime };
        var operatorIdentity = ParseWindowsSid(operatorSid, "operator");
        if (operatorIdentity is not null && !result.Any(sid => sid.Equals(operatorIdentity)))
        {
            result.Add(operatorIdentity);
        }

        return result;
    }

    [SupportedOSPlatform("windows")]
    private static SecurityIdentifier? ParseWindowsSid(string? sidValue, string name)
    {
        if (string.IsNullOrWhiteSpace(sidValue))
        {
            return null;
        }

        try
        {
            return new SecurityIdentifier(sidValue.Trim());
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException($"The local account-message {name} identity is invalid.", exception);
        }
    }

    private static void ValidateCurrentWindowsIdentity(IReadOnlyList<SecurityIdentifier>? allowedWindowsSids)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        ValidateCurrentWindowsIdentityCore(
            allowedWindowsSids
            ?? throw new InvalidOperationException("The local account-message ACL policy is unavailable."));
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateCurrentWindowsIdentityCore(IReadOnlyList<SecurityIdentifier> allowedWindowsSids)
    {
        var current = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The local account-message caller identity is unavailable.");
        if (!allowedWindowsSids.Any(sid => sid.Equals(current)))
        {
            throw new InvalidOperationException(
                "The local account-message caller is not an authorized runtime or operator identity.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void RemoveAccessRules(FileSystemSecurity security)
    {
        foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            security.RemoveAccessRuleSpecific(rule);
        }
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Local account-message capture cannot use a reparse point.");
        }
    }

    // Keep the durable capture flat so an older local CLI can still deserialize
    // the original LocalAccountMessage fields and ignore the additive fence.
    // New code uses the fence for promotion/reconciliation; legacy captures
    // intentionally remain readable but are never claimed as fenced ownership.
    private sealed record CapturedAccountMessage(
        Guid Id,
        Guid? UserId,
        string Purpose,
        string Email,
        string RawToken,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt,
        Guid DeliveryFence)
    {
        public static CapturedAccountMessage From(LocalAccountMessage message, Guid deliveryFence) =>
            new(message.Id, message.UserId, message.Purpose, message.Email, message.RawToken,
                message.CreatedAt, message.ExpiresAt, deliveryFence);

        public LocalAccountMessage ToMessage() =>
            new(Id, UserId, Purpose, Email, RawToken, CreatedAt, ExpiresAt);
    }

    private readonly record struct CaptureReadResult(LocalAccountMessage Message, Guid? DeliveryFence);
}
