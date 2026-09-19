using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;

namespace Nexora.UnitTests;

internal static class LocalAccountMessageSinkTests
{
    public static void Register(TestRunner runner)
    {
        runner.AddWindows("local capture retains stable configured runtime and operator ACL policy", RunWindowsAclPolicyAssertionsIfSupported);

        runner.Skip(
            "local capture verifies Windows A/B/C with actual process identities",
            "requires a configured runtime process, operator process, and foreign principal; a SID argument in one process is not evidence of this boundary.",
            "windows");

        runner.Add("local capture removes only old malformed owned JSON and keeps foreign names", () =>
        {
            WithCaptureDirectory((contentRoot, captureDirectory) =>
            {
                var sink = CreateSink(contentRoot, captureDirectory);
                var now = DateTimeOffset.UtcNow;

                var recentId = Guid.NewGuid();
                var recentFence = Guid.NewGuid();
                PrepareCapture(sink, recentId, recentFence);
                var recentPath = Path.Combine(captureDirectory, $"{recentId:N}.json");
                File.WriteAllText(recentPath, "{");
                File.SetLastWriteTimeUtc(recentPath, now.UtcDateTime.Subtract(TimeSpan.FromHours(1)));

                var oldId = Guid.NewGuid();
                var oldFence = Guid.NewGuid();
                PrepareCapture(sink, oldId, oldFence);
                var oldPath = Path.Combine(captureDirectory, $"{oldId:N}.json");
                File.WriteAllText(oldPath, "{");
                File.SetLastWriteTimeUtc(oldPath, now.UtcDateTime.Subtract(TimeSpan.FromHours(25)));

                var nonObjectId = Guid.NewGuid();
                var nonObjectFence = Guid.NewGuid();
                PrepareCapture(sink, nonObjectId, nonObjectFence);
                var nonObjectPath = Path.Combine(captureDirectory, $"{nonObjectId:N}.json");
                File.WriteAllText(nonObjectPath, "[]");
                File.SetLastWriteTimeUtc(nonObjectPath, now.UtcDateTime.Subtract(TimeSpan.FromHours(25)));

                var foreignId = Guid.NewGuid();
                var foreignFence = Guid.NewGuid();
                PrepareCapture(sink, foreignId, foreignFence);
                var ownedForeignSource = Path.Combine(captureDirectory, $"{foreignId:N}.json");
                var foreignPath = Path.Combine(captureDirectory, "operator-not-owned.json");
                File.Move(ownedForeignSource, foreignPath);
                File.SetLastWriteTimeUtc(foreignPath, now.UtcDateTime.Subtract(TimeSpan.FromHours(25)));

                var foreignMalformedPath = Path.Combine(captureDirectory, "operator-not-owned-malformed.json");
                File.WriteAllText(foreignMalformedPath, "{");
                File.SetLastWriteTimeUtc(foreignMalformedPath, now.UtcDateTime.Subtract(TimeSpan.FromHours(25)));

                var messages = LocalAccountMessageSink.ReadCaptured(captureDirectory, now);

                AssertEx.Equal(0, messages.Count, "Malformed captures must not be returned as messages");
                AssertEx.True(File.Exists(recentPath), "A recent malformed capture must remain inside its grace period");
                AssertEx.False(File.Exists(oldPath), "An old malformed owned capture must be removed");
                AssertEx.False(File.Exists(nonObjectPath), "An old non-object JSON capture must be removed");
                AssertEx.True(File.Exists(foreignPath), "A valid capture with an unowned name must not be returned or removed");
                AssertEx.True(File.Exists(foreignMalformedPath), "A malformed file with an unowned name must not be removed");
            });
        });

        runner.Add("local capture rejects filename and payload identity mismatches without deleting them", () =>
        {
            WithCaptureDirectory((contentRoot, captureDirectory) =>
            {
                var sink = CreateSink(contentRoot, captureDirectory);
                var payloadId = Guid.NewGuid();
                var payloadFence = Guid.NewGuid();
                PrepareCapture(sink, payloadId, payloadFence);
                var ownedPath = Path.Combine(captureDirectory, $"{payloadId:N}.json");
                var mismatchedId = Guid.NewGuid();
                var mismatchPath = Path.Combine(captureDirectory, $"{mismatchedId:N}.json");
                File.Move(ownedPath, mismatchPath);
                File.SetLastWriteTimeUtc(mismatchPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(25)));

                var emptyIdPath = Path.Combine(captureDirectory, $"{Guid.Empty:N}.json");
                File.WriteAllText(emptyIdPath, JsonSerializer.Serialize(CreateMessage(Guid.NewGuid())));
                File.SetLastWriteTimeUtc(emptyIdPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(25)));

                var messages = LocalAccountMessageSink.ReadCaptured(captureDirectory, DateTimeOffset.UtcNow);
                AssertEx.Equal(0, messages.Count, "Mismatched and empty-id filenames must not be returned");
                sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
                AssertEx.True(File.Exists(mismatchPath), "A valid payload under the wrong owned filename must not be deleted");
                AssertEx.True(File.Exists(emptyIdPath), "An empty-id filename must not be deleted");
            });
        });

        runner.Add("local capture preserves a matching legacy final JSON capture", () =>
        {
            WithCaptureDirectory((contentRoot, captureDirectory) =>
            {
                var message = CreateMessage(Guid.NewGuid());
                var sink = CreateSink(contentRoot, captureDirectory);
                PrepareCapture(sink, message.Id, Guid.NewGuid());
                var path = Path.Combine(captureDirectory, $"{message.Id:N}.json");
                File.WriteAllText(path, JsonSerializer.Serialize(message));

                var messages = LocalAccountMessageSink.ReadCaptured(captureDirectory, DateTimeOffset.UtcNow);
                AssertEx.Equal(1, messages.Count, "A matching legacy capture remains readable");
                AssertEx.Equal(message.Id, messages[0].Id, "The legacy capture identity is preserved");
            });
        });

        runner.Add("local capture removes malformed pending files after grace on an idle restart", () =>
        {
            WithCaptureDirectory((contentRoot, captureDirectory) =>
            {
                var sink = CreateSink(contentRoot, captureDirectory);
                var messageId = Guid.NewGuid();
                var effectFence = Guid.NewGuid();
                var outcome = sink.Publish(CreateMessage(messageId), effectFence, () => true);
                AssertEx.Equal(LocalAccountMessagePublishOutcome.Prepared, outcome, "The synthetic pending capture should be prepared");

                var pendingPath = Path.Combine(captureDirectory, $".{messageId:N}.{effectFence:N}.pending");
                File.WriteAllText(pendingPath, "{");
                var recentNow = DateTimeOffset.UtcNow;
                File.SetLastWriteTimeUtc(pendingPath, recentNow.UtcDateTime.Subtract(TimeSpan.FromHours(1)));

                var restartedSink = CreateSink(contentRoot, captureDirectory);
                var recentReconciled = restartedSink.ReconcilePending((_, _) => LocalAccountMessagePendingDisposition.Remove);
                AssertEx.Equal(0, recentReconciled, "Recent malformed pending content must wait for the grace period");
                AssertEx.True(File.Exists(pendingPath), "Recent malformed pending content must remain recoverable");

                File.SetLastWriteTimeUtc(pendingPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(25)));
                var oldReconciled = restartedSink.ReconcilePending((_, _) => LocalAccountMessagePendingDisposition.Remove);
                AssertEx.Equal(1, oldReconciled, "Old malformed pending content should be reconciled once");
                AssertEx.False(File.Exists(pendingPath), "Old malformed pending content must be removed");
            });
        });

        runner.Add("local capture disposes an actual orphan temporary file after the grace period", () =>
        {
            WithCaptureDirectory((contentRoot, captureDirectory) =>
            {
                var sink = CreateSink(contentRoot, captureDirectory);
                var messageId = Guid.NewGuid();
                var effectFence = Guid.NewGuid();
                var temporaryPath = Path.Combine(captureDirectory, $".{messageId:N}.{effectFence:N}.tmp");
                File.WriteAllText(temporaryPath, "{\"Id\":");
                SetPrivateFileMode(temporaryPath);
                File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)));
                sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
                AssertEx.False(File.Exists(temporaryPath), "An old orphan temporary file should be swept without a completed capture rename");

                var nextMessageId = Guid.NewGuid();
                var nextOutcome = sink.Publish(CreateMessage(nextMessageId), Guid.NewGuid(), () => true);
                AssertEx.Equal(LocalAccountMessagePublishOutcome.Prepared, nextOutcome, "A later message must still be deliverable after orphan cleanup");
            });
        });

        runner.AddLinux("local capture uses private Unix modes when running on Linux", RunUnixModeAssertionsIfSupported);

        runner.AddLinux("local capture isolates an unsafe temporary file from later delivery", RunUnsafeTemporaryFileAssertionsIfSupported);

        runner.AddLinux("local capture detects an active writer across processes", RunCrossProcessActiveWriterAssertion);
    }

    private static void RunWindowsAclPolicyAssertionsIfSupported()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        RunWindowsAclPolicyAssertions();
    }

    private static void RunUnixModeAssertionsIfSupported()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        RunUnixModeAssertions();
    }

    private static void RunUnsafeTemporaryFileAssertionsIfSupported()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        RunUnsafeTemporaryFileAssertions();
    }

    [SupportedOSPlatform("linux")]
    private static void RunUnsafeTemporaryFileAssertions()
    {
        WithCaptureDirectory((contentRoot, captureDirectory) =>
        {
            var sink = CreateSink(contentRoot, captureDirectory);
            var orphanPath = Path.Combine(captureDirectory, $".{Guid.NewGuid():N}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(orphanPath, "{\"Id\":");
            SetPrivateLinuxFileMode(orphanPath, includeOtherRead: true);
            File.SetLastWriteTimeUtc(orphanPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)));

            sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
            AssertEx.True(File.Exists(orphanPath), "An unsafe orphan must remain fail-closed for operator recovery");

            var nextMessageId = Guid.NewGuid();
            var nextOutcome = sink.Publish(CreateMessage(nextMessageId), Guid.NewGuid(), () => true);
            AssertEx.Equal(LocalAccountMessagePublishOutcome.Prepared, nextOutcome, "An unsafe orphan must not block later delivery");
        });
    }

    private static void WithCaptureDirectory(Action<string, string> test)
    {
        var root = Path.Combine(Path.GetTempPath(), $"nexora-r4-{Guid.NewGuid():N}");
        var contentRoot = Path.Combine(root, "content");
        var captureDirectory = Path.Combine(root, "captures");
        Directory.CreateDirectory(Path.Combine(contentRoot, "wwwroot"));

        try
        {
            test(contentRoot, captureDirectory);
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
                // The test already reports its assertion; cleanup must not
                // expose capture content or hide the original failure.
            }
        }
    }

    private static LocalAccountMessageSink CreateSink(
        string contentRoot,
        string captureDirectory,
        string? operatorSid = null,
        string? runtimeSid = null) =>
        new(
            captureDirectory,
            contentRoot,
            NullLogger<LocalAccountMessageSink>.Instance,
            operatorSid,
            runtimeSid);

    private static void PrepareCapture(LocalAccountMessageSink sink, Guid messageId, Guid effectFence)
    {
        var outcome = sink.Publish(CreateMessage(messageId), effectFence, () => true);
        AssertEx.Equal(LocalAccountMessagePublishOutcome.Prepared, outcome, "The synthetic capture should be prepared");
        AssertEx.True(sink.PromoteIfOwned(messageId, effectFence), "The synthetic capture should be promoted");
    }

    private static LocalAccountMessage CreateMessage(Guid messageId) =>
        new(
            messageId,
            Guid.NewGuid(),
            "EmailVerification",
            "synthetic-user@example.test",
            "synthetic-r4-token",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(2));

    private static void SetPrivateFileMode(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            SetPrivateLinuxFileMode(path);
        }
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("linux")]
    private static void SetPrivateLinuxFileMode(string path) =>
        SetPrivateLinuxFileMode(path, includeOtherRead: false);

    [SupportedOSPlatform("linux")]
    private static void SetPrivateLinuxFileMode(string path, bool includeOtherRead)
    {
        var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        if (includeOtherRead)
        {
            mode |= UnixFileMode.OtherRead;
        }

        File.SetUnixFileMode(path, mode);
    }

    private static void RunCrossProcessActiveWriterAssertion()
    {
        WithCaptureDirectory((contentRoot, captureDirectory) =>
        {
            var sink = CreateSink(contentRoot, captureDirectory);
            var temporaryPath = Path.Combine(captureDirectory, $".{Guid.NewGuid():N}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporaryPath, "{\"Id\":");
            SetPrivateFileMode(temporaryPath);
            File.SetLastWriteTimeUtc(temporaryPath, DateTime.UtcNow.Subtract(TimeSpan.FromHours(1)));

            using var holder = StartCaptureHolder(temporaryPath);
            sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
            AssertEx.True(File.Exists(temporaryPath), "A separate process holding the advisory lock must prevent cleanup");

            holder.StandardInput.WriteLine("release");
            holder.StandardInput.Flush();
            holder.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            AssertEx.Equal(0, holder.ExitCode, "The active-writer probe must exit cleanly");

            sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
            AssertEx.False(File.Exists(temporaryPath), "The released orphan must be cleaned on the next pass");
        });
    }

    private static Process StartCaptureHolder(string path)
    {
        var processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The test process path is unavailable.");
        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--hold-capture");
        startInfo.ArgumentList.Add(path);

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The active-writer probe could not start.");
        try
        {
            var ready = process.StandardOutput.ReadLineAsync()
                .WaitAsync(TimeSpan.FromSeconds(10))
                .GetAwaiter()
                .GetResult();
            if (!string.Equals(ready, "ready", StringComparison.Ordinal))
            {
                process.Dispose();
                throw new InvalidOperationException("The active-writer probe did not acquire its file lock.");
            }

            return process;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }

            process.Dispose();
            throw;
        }
    }

    public static int HoldCaptureProcess(string path)
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            Console.Error.WriteLine("The active-writer probe supports Windows and Linux only.");
            return 2;
        }

        return HoldCaptureProcessCore(path);
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    private static int HoldCaptureProcessCore(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            stream.Lock(0, 1);
            Console.WriteLine("ready");
            Console.Out.Flush();
            _ = Console.ReadLine();
            stream.Unlock(0, 1);
            return 0;
        }
        catch
        {
            Console.Error.WriteLine("The active-writer probe could not hold the synthetic capture file.");
            return 1;
        }
    }

    [UnsupportedOSPlatform("windows")]
    private static void RunUnixModeAssertions()
    {
        WithCaptureDirectory((contentRoot, captureDirectory) =>
        {
            var sink = CreateSink(contentRoot, captureDirectory);
            var messageId = Guid.NewGuid();
            var effectFence = Guid.NewGuid();
            PrepareCapture(sink, messageId, effectFence);
            var capturePath = Path.Combine(captureDirectory, $"{messageId:N}.json");
            var groupOrOther = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                               UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

            AssertEx.Equal(UnixFileMode.None, File.GetUnixFileMode(captureDirectory) & groupOrOther,
                "The capture directory must not grant group or other access");
            AssertEx.Equal(UnixFileMode.None, File.GetUnixFileMode(capturePath) & groupOrOther,
                "The capture file must not grant group or other access");
        });
    }

    [SupportedOSPlatform("windows")]
    private static void RunWindowsAclPolicyAssertions()
    {
        WithCaptureDirectory((contentRoot, captureDirectory) =>
        {
            var currentSid = WindowsIdentity.GetCurrent().User?.Value
                ?? throw new InvalidOperationException("The Windows test identity is unavailable.");
            var operatorSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Value;
            var sink = CreateSink(contentRoot, captureDirectory, operatorSid, currentSid);
            var messageId = Guid.NewGuid();
            var effectFence = Guid.NewGuid();
            PrepareCapture(sink, messageId, effectFence);

            var messages = LocalAccountMessageSink.ReadCaptured(
                captureDirectory,
                DateTimeOffset.UtcNow,
                operatorSid: operatorSid,
                runtimeSid: currentSid);
            AssertEx.Equal(1, messages.Count, "The configured runtime/operator policy should allow both API and CLI paths");

            var directorySecurity = new DirectoryInfo(captureDirectory).GetAccessControl(AccessControlSections.Access);
            var allowedSids = directorySecurity
                .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
                .OfType<FileSystemAccessRule>()
                .Where(rule => rule.AccessControlType == AccessControlType.Allow)
                .Select(rule => (SecurityIdentifier)rule.IdentityReference)
                .ToArray();
            AssertEx.True(allowedSids.Any(sid => sid.Value == currentSid), "The runtime identity must be in the capture ACL");
            AssertEx.True(allowedSids.Any(sid => sid.Value == operatorSid), "The operator identity must be in the capture ACL");

            var foreignSid = CreateSiblingSid(currentSid);
            AssertEx.Throws<InvalidOperationException>(
                () => LocalAccountMessageSink.ReadCaptured(
                    captureDirectory,
                    DateTimeOffset.UtcNow,
                    operatorSid: foreignSid,
                    runtimeSid: foreignSid),
                "A principal outside the trusted runtime/operator set must be rejected");

            var missingRuntimeDirectory = Path.Combine(Path.GetDirectoryName(captureDirectory)!, "missing-runtime-captures");
            AssertEx.Throws<InvalidOperationException>(
                () => CreateSink(contentRoot, missingRuntimeDirectory, operatorSid, runtimeSid: null),
                "An operator-configured Windows sink must require a stable runtime identity");

            var unsafeDirectory = Path.Combine(Path.GetDirectoryName(captureDirectory)!, "inherited-captures");
            Directory.CreateDirectory(unsafeDirectory);
            var unsafeSecurity = new DirectoryInfo(unsafeDirectory).GetAccessControl(AccessControlSections.Access);
            unsafeSecurity.SetAccessRuleProtection(isProtected: false, preserveInheritance: true);
            new DirectoryInfo(unsafeDirectory).SetAccessControl(unsafeSecurity);
            AssertEx.Throws<InvalidOperationException>(
                () => CreateSink(contentRoot, unsafeDirectory, operatorSid, currentSid),
                "An inherited ACL must be rejected");

            var sameIdentityDirectory = Path.Combine(Path.GetDirectoryName(captureDirectory)!, "same-identity-captures");
            var sameIdentitySink = CreateSink(contentRoot, sameIdentityDirectory, currentSid, currentSid);
            var sameIdentityMessageId = Guid.NewGuid();
            var sameIdentityFence = Guid.NewGuid();
            PrepareCapture(sameIdentitySink, sameIdentityMessageId, sameIdentityFence);
            var sameIdentityMessages = LocalAccountMessageSink.ReadCaptured(
                sameIdentityDirectory,
                DateTimeOffset.UtcNow,
                operatorSid: currentSid,
                runtimeSid: currentSid);
            AssertEx.Equal(1, sameIdentityMessages.Count, "The same runtime/operator identity must read its capture");
        });
    }

    [SupportedOSPlatform("windows")]
    private static string CreateSiblingSid(string sid)
    {
        var parts = sid.Split('-');
        if (parts.Length < 2 || !long.TryParse(parts[^1], out var rid) || rid == long.MaxValue)
        {
            throw new InvalidOperationException("The Windows test identity SID cannot produce a sibling SID.");
        }

        parts[^1] = (rid + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return string.Join('-', parts);
    }
}
