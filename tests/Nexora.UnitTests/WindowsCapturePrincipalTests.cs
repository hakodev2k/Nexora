using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;

namespace Nexora.UnitTests;

internal static class WindowsCapturePrincipalTests
{
    // A child always proves its own token; command-line SID values are expectations only.
    public static int RunProbe(string[] args)
    {
        if (!OperatingSystem.IsWindows() || args.Length != 7)
            return 2;
        try
        {
            RunProbeCore(args);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Windows principal probe failed ({ex.GetType().Name}); capture contents suppressed.");
            return 1;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void RunProbeCore(string[] args)
    {
        var mode = args[1];
        var root = Path.GetFullPath(args[2]);
        var runRoot = Directory.GetParent(root)
            ?? throw new InvalidOperationException("Synthetic run root required");
        var programData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        AssertEx.True(string.Equals(Path.GetFileName(root), "workspace", StringComparison.Ordinal) &&
            runRoot.Name.StartsWith("NexoraAcl-", StringComparison.Ordinal) &&
            Guid.TryParseExact(runRoot.Name["NexoraAcl-".Length..], "N", out _) &&
            string.Equals(runRoot.Parent?.FullName, programData, StringComparison.OrdinalIgnoreCase),
            "Probe refuses paths outside a unique ProgramData synthetic workspace");
        foreach (var directory in new[] { runRoot.FullName, root })
            AssertEx.False((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0,
                "Probe refuses a linked workspace");
        var runtimeSid = args[3];
        var operatorSid = args[4];
        var expectedSid = args[5];
        var messageId = Guid.ParseExact(args[6], "N");
        using var identity = WindowsIdentity.GetCurrent();
        AssertEx.Equal(expectedSid, identity.User?.Value, "The child must have the requested actual OS identity");
        AssertEx.False(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator),
            "Probe identities must be unprivileged standard users");
        AssertEx.False(runtimeSid == operatorSid, "A and B must be distinct");
        var captures = Path.Combine(root, "captures");
        var capturePath = Path.Combine(captures, $"{messageId:N}.json");
        LocalAccountMessageSink OpenSink() => new(captures, root,
            NullLogger<LocalAccountMessageSink>.Instance, operatorSid, runtimeSid);
        if (mode == "cleanup")
        {
            AssertEx.Equal(runtimeSid, expectedSid, "Only A cleans its own capture tree");
            // Only the fixed synthetic directory is removed; never follow a reparse point.
            if (Directory.Exists(captures))
            {
                AssertEx.False((File.GetAttributes(captures) & FileAttributes.ReparsePoint) != 0,
                    "Cleanup refuses a reparse point");
                AssertNoReparseTree(captures);
                Directory.Delete(captures, recursive: true);
            }
        }
        else if (mode == "publish")
        {
            AssertEx.Equal(runtimeSid, expectedSid, "Only A publishes");
            var sink = OpenSink();
            var fence = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var message = new LocalAccountMessage(messageId, Guid.NewGuid(), "EmailVerification",
                "acl-synthetic@example.test", "acl-synthetic-token", now, now.AddHours(1));
            AssertEx.Equal(LocalAccountMessagePublishOutcome.Prepared, sink.Publish(message, fence, () => true),
                "A must prepare a synthetic capture");
            AssertEx.True(sink.PromoteIfOwned(messageId, fence), "A must promote the capture");
            AssertPrivateAcl(captures, runtimeSid, operatorSid);
            AssertPrivateAcl(capturePath, runtimeSid, operatorSid);
        }
        else if (mode == "read")
        {
            AssertEx.True(expectedSid == runtimeSid || expectedSid == operatorSid, "Only A/B read");
            _ = OpenSink(); // Also exercises restart validation against the original A+B policy.
            var messages = LocalAccountMessageSink.ReadCaptured(captures, DateTimeOffset.UtcNow,
                operatorSid: operatorSid, runtimeSid: runtimeSid);
            AssertEx.Equal(1, messages.Count, "A/B must read exactly the existing synthetic capture");
            AssertEx.Equal(messageId, messages[0].Id, "Restart must preserve the capture identity");
            AssertPrivateAcl(captures, runtimeSid, operatorSid);
            AssertPrivateAcl(capturePath, runtimeSid, operatorSid);
        }
        else if (mode == "deny")
        {
            AssertEx.True(expectedSid != runtimeSid && expectedSid != operatorSid, "C must be distinct from A/B");
            AssertEx.Throws<UnauthorizedAccessException>(() => File.ReadAllBytes(capturePath),
                "The OS must deny C direct file reads");
            AssertEx.Throws<UnauthorizedAccessException>(() => Directory.GetFiles(captures),
                "The OS must deny C capture enumeration");
            AssertEx.Throws<UnauthorizedAccessException>(() =>
            {
                using var stream = new FileStream(capturePath, FileMode.Open, FileAccess.Write);
            }, "The OS must deny C writes");
            AssertEx.Throws<InvalidOperationException>(() => OpenSink(), "The sink must reject actual caller C");
            AssertEx.Throws<InvalidOperationException>(() => LocalAccountMessageSink.ReadCaptured(captures,
                DateTimeOffset.UtcNow, operatorSid: operatorSid, runtimeSid: runtimeSid),
                "The operator reader must reject actual caller C");
        }
        else throw new InvalidOperationException("Unknown principal probe operation");
        Console.WriteLine($"[principal] {mode}: actualSid={identity.User!.Value}; pid={Environment.ProcessId}; passed");
    }

    [SupportedOSPlatform("windows")]
    private static void AssertPrivateAcl(string path, string runtimeSid, string operatorSid)
    {
        FileSystemSecurity security = Directory.Exists(path)
            ? new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access);
        AssertEx.True(security.AreAccessRulesProtected, "Capture ACL must be protected");
        var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
        AssertEx.Equal(2, rules.Length, "Only A+B may appear in the capture ACL");
        foreach (var rule in rules)
        {
            AssertEx.False(rule.IsInherited, "Capture ACE must be explicit");
            AssertEx.Equal(AccessControlType.Allow, rule.AccessControlType, "Capture ACE must allow");
            AssertEx.Equal(FileSystemRights.FullControl, rule.FileSystemRights, "Capture rights must remain FullControl");
        }
        var sids = rules.Select(rule => rule.IdentityReference.Value).ToHashSet(StringComparer.Ordinal);
        AssertEx.True(sids.SetEquals(new[] { runtimeSid, operatorSid }), "Capture ACL must retain the original A+B set");
    }

    private static void AssertNoReparseTree(string directory)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            var attributes = File.GetAttributes(entry);
            AssertEx.False((attributes & FileAttributes.ReparsePoint) != 0, "Cleanup refuses nested reparse points");
            if ((attributes & FileAttributes.Directory) != 0) AssertNoReparseTree(entry);
        }
    }

    public static void RunHarness()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Actual Windows processes required.");
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new InvalidOperationException("BLOCKED: actual controller token is not an elevated local administrator; no ephemeral principals created.");
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName,
                   "scripts", "dev", "test-filesystem-windows-principals.ps1")))
            directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Run the filesystem gate from the repository checkout.");
        var script = Path.Combine(directory.FullName, "scripts", "dev", "test-filesystem-windows-principals.ps1");
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32", "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-File", script,
                     "-ProbeDirectory", AppContext.BaseDirectory })
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Windows principal harness.");
        // The helper bounds every child and owns cleanup. Do not kill it before its finally block.
        process.WaitForExit();
        AssertEx.Equal(0, process.ExitCode,
            "Real Windows A/B/C principal gate failed; elevated disposable Windows runner required");
    }
}
