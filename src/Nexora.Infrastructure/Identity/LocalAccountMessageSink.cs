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
    private readonly string? _operatorSid;
    private readonly ILogger<LocalAccountMessageSink> _logger;

    public LocalAccountMessageSink(
        string captureDirectory,
        string contentRootPath,
        ILogger<LocalAccountMessageSink> logger,
        string? operatorSid = null)
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

        _operatorSid = string.IsNullOrWhiteSpace(operatorSid) ? null : operatorSid.Trim();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var existed = Directory.Exists(_captureDirectory);
        Directory.CreateDirectory(_captureDirectory);
        RejectReparsePoint(_captureDirectory);
        if (existed)
        {
            // An upgrade must not silently bless a directory that was already
            // readable by another principal. The operator can repair it
            // explicitly, after which startup validates the resulting ACL.
            ValidateDirectoryPermissions(_captureDirectory, _operatorSid);
        }
        else
        {
            RestrictDirectoryPermissions(_captureDirectory, _operatorSid);
            ValidateDirectoryPermissions(_captureDirectory, _operatorSid);
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
            ValidateDirectoryPermissions(_captureDirectory, _operatorSid);
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
            try
            {
                using (var stream = new FileStream(
                           temporary,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None,
                           bufferSize: 4096,
                           options: FileOptions.WriteThrough))
                {
                    JsonSerializer.Serialize(stream, CapturedAccountMessage.From(message, effectFence));
                    stream.Flush(flushToDisk: true);
                }

                // The directory is private before the first byte is written;
                // this file-level ACL/mode also prevents later permission
                // inheritance from widening access.
                RestrictFilePermissions(temporary, _operatorSid);
                ValidateFilePermissions(temporary, _operatorSid);

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
                TryDelete(temporary);
            }
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
            ValidateFilePermissions(pending, _operatorSid);
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
        string? operatorSid = null)
    {
        if (string.IsNullOrWhiteSpace(captureDirectory) || !Directory.Exists(captureDirectory))
        {
            return Array.Empty<LocalAccountMessage>();
        }

        var directory = Path.GetFullPath(captureDirectory);
        RejectReparsePoint(directory);
        ValidateDirectoryPermissions(directory, operatorSid);
        var current = now ?? DateTimeOffset.UtcNow;
        var messages = new List<LocalAccountMessage>();
        var expired = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, operatorSid);
                var capture = ReadCapture(path);
                if (capture is null)
                {
                    if (IsOwnedCaptureFile(path) && File.GetLastWriteTimeUtc(path) <= current.UtcDateTime.Subtract(MalformedCaptureGrace))
                    {
                        expired.Add(path);
                    }

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
                // Ignore a malformed capture; it is not a valid transport message.
            }
        }

        foreach (var path in expired)
        {
            TryDeleteSilently(path);
        }

        return messages.OrderByDescending(message => message.CreatedAt).ToArray();
    }

    public void SweepExpired(DateTimeOffset now)
    {
        lock (CaptureGate)
        {
            ValidateDirectoryPermissions(_captureDirectory, _operatorSid);
            SweepExpiredCore(now);
        }
    }

    public int ReconcilePending(Func<Guid, Guid, LocalAccountMessagePendingDisposition> resolve)
    {
        ArgumentNullException.ThrowIfNull(resolve);
        var reconciled = 0;
        lock (CaptureGate)
        {
            ValidateDirectoryPermissions(_captureDirectory, _operatorSid);
            foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.pending", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    RejectReparsePoint(path);
                    ValidateFilePermissions(path, _operatorSid);
                    var capture = ReadCapture(path);
                    if (capture is null || capture.Value.DeliveryFence is not { } effectFence ||
                        capture.Value.Message.Id == Guid.Empty)
                    {
                        continue;
                    }

                    var disposition = resolve(capture.Value.Message.Id, effectFence);
                    if (disposition == LocalAccountMessagePendingDisposition.Promote)
                    {
                        reconciled += PromoteIfOwnedCore(capture.Value.Message.Id, effectFence) ? 1 : 0;
                    }
                    else if (disposition == LocalAccountMessagePendingDisposition.Remove)
                    {
                        RemoveIfOwnedCore(capture.Value.Message.Id, effectFence);
                        reconciled++;
                    }
                }
                catch (IOException)
                {
                    // Leave a file involved in another local operation for the next pass.
                }
                catch (JsonException)
                {
                    // Retention handles malformed owned files after their grace period.
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
            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, _operatorSid);
                var capture = ReadCapture(path);
                if ((capture is not null && capture.Value.Message.ExpiresAt <= now) ||
                    (capture is null && IsOwnedCaptureFile(path) &&
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
                // A malformed capture is removed only after its bounded grace period.
            }
        }

        var temporaryCutoff = now.UtcDateTime.Subtract(OrphanTemporaryGrace);
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.tmp", SearchOption.TopDirectoryOnly))
        {
            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, _operatorSid);
                if (IsOwnedTemporaryFile(path) && File.GetLastWriteTimeUtc(path) <= temporaryCutoff)
                {
                    deleteAfterClose.Add(path);
                }
            }
            catch (IOException)
            {
                // Leave a file that is still being written for the next sweep.
            }
        }

        var pendingCutoff = now.UtcDateTime.Subtract(TimeSpan.FromHours(24));
        foreach (var path in Directory.EnumerateFiles(_captureDirectory, ".*.pending", SearchOption.TopDirectoryOnly))
        {
            try
            {
                RejectReparsePoint(path);
                ValidateFilePermissions(path, _operatorSid);
                var capture = ReadCapture(path);
                if ((capture is not null && capture.Value.Message.ExpiresAt <= now) ||
                    (capture is null && IsOwnedPendingFile(path) &&
                     File.GetLastWriteTimeUtc(path) <= now.UtcDateTime.Subtract(MalformedCaptureGrace)) ||
                    File.GetLastWriteTimeUtc(path) <= pendingCutoff)
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
                // A malformed pending file is removed only after its bounded grace period.
            }
        }

        foreach (var path in deleteAfterClose.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TryDelete(path);
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

        try
        {
            ValidateFilePermissions(path, _operatorSid);
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
        try
        {
            ValidateFilePermissions(path, _operatorSid);
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

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            _logger.LogWarning("Local account-message cleanup will retry after the file handle is released.");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Local account-message cleanup could not remove an owned capture file.");
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
            // The next explicit local sweep can retry without exposing content.
        }
        catch (UnauthorizedAccessException)
        {
            // Fail closed for reading; never disclose capture content in an error.
        }
    }

    private static CaptureReadResult? ReadCapture(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.TryGetProperty("DeliveryFence", out _))
        {
            var capture = document.RootElement.Deserialize<CapturedAccountMessage>();
            return capture is null ? null : new CaptureReadResult(capture.ToMessage(), capture.DeliveryFence);
        }

        var legacy = document.RootElement.Deserialize<LocalAccountMessage>();
        return legacy is null ? null : new CaptureReadResult(legacy, null);
    }

    private static bool IsOwnedCaptureFile(string path) =>
        Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out _);

    private static bool IsOwnedTemporaryFile(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var parts = name.Split('.', StringSplitOptions.None);
        return parts.Length == 3 && parts[0].Length == 0 &&
            Guid.TryParseExact(parts[1], "N", out _) &&
            Guid.TryParseExact(parts[2], "N", out _);
    }

    private static bool IsOwnedPendingFile(string path) =>
        IsOwnedTemporaryFile(path);

    private static void RestrictDirectoryPermissions(string path, string? operatorSid)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            RemoveAccessRules(security);
            foreach (var sid in AllowedWindowsSids(operatorSid))
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

    private static void RestrictFilePermissions(string path, string? operatorSid)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(path).GetAccessControl(AccessControlSections.Access);
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            RemoveAccessRules(security);
            foreach (var sid in AllowedWindowsSids(operatorSid))
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

    private static void ValidateDirectoryPermissions(string path, string? operatorSid)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access);
            ValidateWindowsPermissions(security, operatorSid, isDirectory: true);
            return;
        }

        ValidateUnixPermissions(path, PrivateDirectoryMode);
    }

    private static void ValidateFilePermissions(string path, string? operatorSid)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(path).GetAccessControl(AccessControlSections.Access);
            ValidateWindowsPermissions(security, operatorSid, isDirectory: false);
            return;
        }

        ValidateUnixPermissions(path, PrivateFileMode);
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
        string? operatorSid,
        bool isDirectory)
    {
        if (!security.AreAccessRulesProtected)
        {
            throw new InvalidOperationException("Local account-message capture ACL inheritance is not private.");
        }

        var allowed = AllowedWindowsSids(operatorSid);
        var found = new HashSet<SecurityIdentifier>();
        foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            if (rule.IsInherited || rule.AccessControlType != AccessControlType.Allow ||
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

        _ = isDirectory;
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<SecurityIdentifier> AllowedWindowsSids(string? operatorSid)
    {
        var current = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The local account-message runtime identity is unavailable.");
        var result = new List<SecurityIdentifier> { current };
        if (!string.IsNullOrWhiteSpace(operatorSid))
        {
            try
            {
                var configured = new SecurityIdentifier(operatorSid.Trim());
                if (!result.Any(sid => sid.Equals(configured)))
                {
                    result.Add(configured);
                }
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("The local account-message operator identity is invalid.", exception);
            }
        }

        return result;
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
