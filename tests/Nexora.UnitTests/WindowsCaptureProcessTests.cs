using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;

namespace Nexora.UnitTests;

internal static class WindowsCaptureProcessTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Actual Windows tokens are required.");
        WindowsCaptureHarness.Run();
    }

    internal static int Probe(string config)
    {
        if (!OperatingSystem.IsWindows()) return 2;
        return WindowsCaptureHarness.Probe(config);
    }
}

/// <summary>Disposable GitHub Windows runner only. No production ACL mutation or password CLI.</summary>
[SupportedOSPlatform("windows")]
internal static class WindowsCaptureHarness
{
    private const string Marker = "Nexora actual-token ACL test ";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };
    private static readonly string[] Roles = ["runtime", "operator", "foreign"];

    internal static void Run()
    {
        Require(Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" &&
                Environment.GetEnvironmentVariable("CI") == "true" &&
                Environment.GetEnvironmentVariable("RUNNER_OS") == "Windows" &&
                ulong.TryParse(Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), out _),
            "W01 blocked: an identified disposable GitHub Windows runner is required.");
        using var parentIdentity = WindowsIdentity.GetCurrent();
        var parentSid = parentIdentity.User ?? throw new InvalidOperationException("W01 parent SID unavailable.");
        Require(new WindowsPrincipal(parentIdentity).IsInRole(WindowsBuiltInRole.Administrator),
            "W01 blocked: account setup requires the disposable runner administrator.");
        var nonce = Guid.NewGuid().ToString("N");
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NexoraAcl-" + nonce);
        Require(!Directory.Exists(root) && !File.Exists(root), "W01 generated path already exists.");
        var accounts = new List<Account>();
        var completed = new List<ProbeReport>();
        var cleanupErrors = new List<string>();
        Exception? failure = null;
        try
        {
            foreach (var role in Roles) accounts.Add(Account.Create(role, nonce));
            Require(accounts.Select(a => a.Sid.Value).Distinct().Count() == 3, "W02 distinct accounts/SIDs required.");
            Directory.CreateDirectory(root);
            SetDirectoryAcl(root, [(parentSid, FileSystemRights.FullControl),
                (accounts[0].Sid, FileSystemRights.Modify),
                (accounts[1].Sid, FileSystemRights.ReadAndExecute),
                (accounts[2].Sid, FileSystemRights.ReadAndExecute)]);
            // C can execute trusted binaries and reach the control file, but has no DELETE_CHILD.
            var binary = Path.Combine(root, "bin");
            File.WriteAllText(Path.Combine(root, "owner.json"), JsonSerializer.Serialize(new { nonce, root, accounts = accounts.Select(a => new { a.Name, sid = a.Sid.Value, a.Role }) }));
            CopyTree(AppContext.BaseDirectory, binary);
            var control = "stage-control-" + nonce;
            File.WriteAllText(Path.Combine(root, "control.txt"), control, Encoding.UTF8);

            var messageId = Guid.NewGuid(); var fence = Guid.NewGuid(); var tempId = Guid.NewGuid();
            var token = "synthetic-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            foreach (var account in accounts)
            {
                Directory.CreateDirectory(Path.Combine(root, account.Role));
                SetDirectoryAcl(Path.Combine(root, account.Role), [(parentSid, FileSystemRights.FullControl), (account.Sid, FileSystemRights.FullControl)]);
            }
            Config Configuration(Account a, string scenario) => new(root, nonce, a.Role, scenario,
                a.Sid.Value, accounts[0].Sid.Value, accounts[1].Sid.Value, control,
                messageId, fence, tempId, a.Role == "foreign" ? null : token,
                Path.Combine(root, a.Role, Guid.NewGuid().ToString("N") + ".report.json"));
            ProbeReport Execute(Account a, string scenario, int seconds = 30)
            {
                var report = Launch(a, Configuration(a, scenario), binary, parentSid, TimeSpan.FromSeconds(seconds));
                completed.Add(report); return report;
            }

            // W02 real native child processes, independently compared to launched handles.
            foreach (var a in accounts) Execute(a, "identity");
            Require(completed.Take(3).Select(p => p.Pid).Distinct().Count() == 3, "W02 three actual process IDs required.");
            Execute(accounts[0], "temporary");
            Execute(accounts[1], "read-temporary");
            var before = Execute(accounts[0], "snapshot").Digest;
            Execute(accounts[2], "deny-temporary");
            Require(before == Execute(accounts[0], "snapshot").Digest, "W05 temporary state changed after foreign attempts.");
            Execute(accounts[0], "prepare");
            Execute(accounts[1], "read-pending");
            before = Execute(accounts[0], "snapshot").Digest;
            Execute(accounts[2], "deny-pending");
            Require(before == Execute(accounts[0], "snapshot").Digest, "W05 pending state changed after foreign attempts.");
            Execute(accounts[0], "promote");
            Execute(accounts[1], "read-final");
            before = Execute(accounts[0], "snapshot").Digest;
            Execute(accounts[2], "deny-final");
            Require(before == Execute(accounts[0], "snapshot").Digest, "W05 final state changed after foreign attempts.");
            // Every Execute starts a new native process. Reopen A under the same runtime SID.
            Execute(accounts[0], "restart"); Execute(accounts[1], "read-final"); Execute(accounts[2], "deny-final");
            Execute(accounts[0], "reject-insecure");
            AssertManifestMismatch(root, nonce);

            // W09 explicitly execute timeout, cancellation, crash and partial-setup cleanup branches.
            AssertFault(() => Launch(accounts[0], Configuration(accounts[0], "hang"), binary, parentSid, TimeSpan.FromMilliseconds(750)), ProbeFailureKind.Timeout);
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(750)))
                AssertFault(() => Launch(accounts[0], Configuration(accounts[0], "hang"), binary, parentSid, TimeSpan.FromSeconds(30), cancellation.Token), ProbeFailureKind.Cancelled);
            AssertFault(() => Launch(accounts[0], Configuration(accounts[0], "crash-temporary"), binary, parentSid, TimeSpan.FromSeconds(30)), ProbeFailureKind.Crash);
            Execute(accounts[0], "verify-crashed-temporary"); Execute(accounts[2], "deny-temporary"); Execute(accounts[0], "sweep-crashed-temporary");
            Account? partial = null;
            try { partial = Account.Create("partial", nonce); throw new InjectedSetupException(); }
            catch (InjectedSetupException) { }
            finally { partial?.Dispose(); }
            Require(partial is not null && !Account.Exists(partial.Name), "W09 partial setup account cleanup failed.");
            Execute(accounts[1], "cleanup-capture");
            Require(!Directory.Exists(Path.Combine(root, "capture")), "W09 operator capture cleanup did not complete.");
            Console.WriteLine("[evidence] Windows W01-W10 actual-token assertions completed; native child reports=" + completed.Count);
        }
        catch (Exception ex) { failure = ex; }
        finally
        {
            if (Directory.Exists(root))
            {
                try
                {
                    ValidateOwnedRoot(root, nonce);
                    if (accounts.Count >= 2 && Directory.Exists(Path.Combine(root, "operator")) && Directory.Exists(Path.Combine(root, "bin")))
                    {
                        // Capture allows only A/B; parent setup privilege is not used as a foreign probe or bypass.
                        var cfg = new Config(root, nonce, "operator", "cleanup-capture", accounts[1].Sid.Value,
                            accounts[0].Sid.Value, accounts[1].Sid.Value, "stage-control-" + nonce,
                            Guid.Empty, Guid.Empty, Guid.Empty, null, Path.Combine(root, "operator", Guid.NewGuid().ToString("N") + ".report.json"));
                        _ = Launch(accounts[1], cfg, Path.Combine(root, "bin"), parentSid, TimeSpan.FromSeconds(30));
                    }
                    RejectReparseTree(root);
                    Directory.Delete(root, recursive: true);
                }
                catch { cleanupErrors.Add("W09 owned path cleanup failed; private run root retained."); }
            }
            foreach (var account in accounts.AsEnumerable().Reverse())
            {
                try { account.Dispose(); }
                catch { cleanupErrors.Add("W09 exact-SID/account ownership cleanup failed."); }
            }
        }
        if (cleanupErrors.Count > 0) throw new InvalidOperationException(string.Join(" ", cleanupErrors));
        if (failure is not null) throw new InvalidOperationException("Windows actual-token ACL assertion failed: " + failure.GetType().Name + ": " + failure.Message);
    }

    private static void AssertFault(Action action, ProbeFailureKind kind)
    {
        try { action(); }
        catch (ProbeFailureException ex) when (ex.Kind == kind) { return; }
        throw new InvalidOperationException("W09 " + kind + " fault was not detected.");
    }

    private static ProbeReport Launch(Account account, Config config, string binary, SecurityIdentifier parentSid, TimeSpan deadline, CancellationToken cancellation = default)
    {
        var configPath = Path.Combine(config.Root, account.Role, Guid.NewGuid().ToString("N") + ".config.json");
        WritePrivate(configPath, JsonSerializer.Serialize(config, Json), parentSid, account.Sid);
        var host = Environment.ProcessPath ?? throw new InvalidOperationException("W01 host executable unavailable.");
        var dotnetHost = string.Equals(Path.GetFileNameWithoutExtension(host), "dotnet", StringComparison.OrdinalIgnoreCase);
        var info = new ProcessStartInfo
        {
            FileName = dotnetHost ? host : Path.Combine(binary, Path.GetFileName(host)),
            WorkingDirectory = binary, UseShellExecute = false, CreateNoWindow = true,
            UserName = account.Name, Domain = Environment.MachineName, Password = account.Password,
            LoadUserProfile = false, RedirectStandardOutput = true, RedirectStandardError = true
        };
        if (dotnetHost) info.ArgumentList.Add(Path.Combine(binary, Path.GetFileName(typeof(WindowsCaptureHarness).Assembly.Location)));
        info.ArgumentList.Add("--windows-capture-probe"); info.ArgumentList.Add(configPath);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("W02 native credentialed process did not start.");
        var pid = process.Id; var created = process.StartTime.ToUniversalTime();
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        var phase = "handshake";
        try
        {
            var startup = Stopwatch.StartNew();
            while (!File.Exists(config.Report + ".ready"))
            {
                if (process.HasExited)
                {
                    var diagnostic = stderr.GetAwaiter().GetResult().Trim();
                    var stage = SafeStage(diagnostic);
                    throw new ProbeFailureException(ProbeFailureKind.Protocol, "Native probe scenario=" + config.Scenario + " handshake stage=" + stage + " failed.");
                }
                Require(startup.Elapsed < TimeSpan.FromSeconds(30), "W02 child identity handshake missing.");
                Thread.Sleep(20);
            }
            var ready = JsonSerializer.Deserialize<ProbeReport>(File.ReadAllText(config.Report + ".ready"), Json) ?? throw new InvalidOperationException("W02 invalid ready handshake.");
            ValidateReport(ready, config, account, pid, created);
            phase = "execute";
            using var timeout = new CancellationTokenSource(deadline);
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellation);
            try { process.WaitForExitAsync(combined.Token).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { throw new ProbeFailureException(cancellation.IsCancellationRequested ? ProbeFailureKind.Cancelled : ProbeFailureKind.Timeout, "W09 owned verified child deadline/cancellation reached."); }
            if (process.ExitCode == 17 && config.Scenario == "crash-temporary") throw new ProbeFailureException(ProbeFailureKind.Crash, "W09 verified owned child crashed as injected.");
            if (process.ExitCode != 0)
            {
                var diagnostic = stderr.GetAwaiter().GetResult().Trim();
                var stage = SafeStage(diagnostic);
                throw new ProbeFailureException(ProbeFailureKind.Protocol, "Native probe scenario=" + config.Scenario + " stage=" + stage + " failed.");
            }
            phase = "report";
            Require(File.Exists(config.Report), "W02 missing native child report.");
            var report = JsonSerializer.Deserialize<ProbeReport>(File.ReadAllText(config.Report), Json) ?? throw new InvalidOperationException("W02 invalid child report.");
            ValidateReport(report, config, account, pid, created);
            Require(report.Succeeded, "W03-W08 native child assertion did not complete.");
            return report;
        }
        catch (ProbeFailureException) { throw; }
        catch (Exception ex) { throw new ProbeFailureException(ProbeFailureKind.Protocol, "Native probe scenario=" + config.Scenario + " phase=" + phase + " failed: " + ex.GetType().Name); }
        finally
        {
            // Compare live owned handle, PID and creation time before termination. Never kill by image name.
            if (!process.HasExited)
            {
                Require(process.Id == pid && process.StartTime.ToUniversalTime() == created, "W09 child ownership changed; cleanup refused.");
                process.Kill(entireProcessTree: false);
                Require(process.WaitForExit(10_000), "W09 owned child did not terminate.");
            }
            _ = stdout.GetAwaiter().GetResult(); _ = stderr.GetAwaiter().GetResult();
            File.Delete(configPath); File.Delete(config.Report + ".ready"); File.Delete(config.Report + ".ready.tmp");
        }
    }

    private static string SafeStage(string diagnostic)
    {
        string[] allowed = ["Configuration", "Identity", "Control", "Scenario", "Report", "ExplicitCreate", "ExplicitInitialize", "ExplicitRead", "ExplicitCleanup", "InheritedCreate", "InheritedInitialize", "InheritedRead", "InheritedCleanup", "UnsafeFileSetup", "UnsafeFileRead", "UnsafeFileRestore"];
        var lines = diagnostic.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return allowed.FirstOrDefault(x => lines.Contains("NEXORA_ACL_STAGE:" + x, StringComparer.Ordinal)) ?? "Unknown";
    }

    private static void ValidateReport(ProbeReport report, Config config, Account account, int pid, DateTime created)
    {
        Require(report.Nonce == config.Nonce && report.Role == config.Role && report.Scenario == config.Scenario && report.Sid == account.Sid.Value &&
                report.Pid == pid && Math.Abs((report.CreatedAt - created).TotalMilliseconds) < 100,
            "W02 child report does not match owned process handle, SID, nonce and creation time.");
        Require(!report.Admin && !report.PrivilegedBypass && report.ReachedBoundary, "W02 actual nonprivileged staging/control proof failed.");
    }

    internal static int Probe(string configPath)
    {
        var stage = "Configuration";
        try
        {
            Require(new FileInfo(configPath).Length <= 65536, "Probe config exceeds bound.");
            var c = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath), Json) ?? throw new InvalidOperationException("Invalid private config.");
            ValidateOwnedRoot(c.Root, c.Nonce);
            Require(Path.GetDirectoryName(Path.GetFullPath(configPath)) == Path.Combine(c.Root, c.Role), "Private config is outside its role directory.");
            Require(Path.GetDirectoryName(Path.GetFullPath(c.Report)) == Path.Combine(c.Root, c.Role), "Report is outside its role directory.");
            stage = "Identity";
            using var identity = WindowsIdentity.GetCurrent();
            var sid = identity.User ?? throw new InvalidOperationException("Child SID unavailable.");
            var principal = new WindowsPrincipal(identity);
            var admin = principal.IsInRole(WindowsBuiltInRole.Administrator) || principal.IsInRole(WindowsBuiltInRole.BackupOperator);
            var privileged = HasBypassPrivileges(identity);
            Require(sid.Value == c.ExpectedSid && !admin && !privileged, "Child is not the configured nonprivileged principal.");
            stage = "Control";
            Require(File.ReadAllText(Path.Combine(c.Root, "control.txt"), Encoding.UTF8) == c.Control, "C could not reach the stage control boundary.");
            using var childProcess = Process.GetCurrentProcess();
            var ready = new ProbeReport(c.Nonce, c.Role, c.Scenario, sid.Value, childProcess.Id, childProcess.StartTime.ToUniversalTime(), admin, privileged, true, false, null, []);
            File.WriteAllText(c.Report + ".ready.tmp", JsonSerializer.Serialize(ready, Json));
            File.Move(c.Report + ".ready.tmp", c.Report + ".ready");
            stage = "Scenario";
            var capture = Path.Combine(c.Root, "capture"); var temp = Path.Combine(capture, $".{c.TempId:N}.{c.Fence:N}.tmp");
            var pending = Path.Combine(capture, $".{c.MessageId:N}.{c.Fence:N}.pending"); var final = Path.Combine(capture, $"{c.MessageId:N}.json");
            string? digest = null; var checks = new List<string>();
            LocalAccountMessageSink Sink() => new(capture, c.Root, NullLogger<LocalAccountMessageSink>.Instance, c.OperatorSid, c.RuntimeSid);
            switch (c.Scenario)
            {
                case "identity": break;
                case "temporary":
                case "crash-temporary":
                {
                    var sink = Sink();
                    var create = typeof(LocalAccountMessageSink).GetMethod("CreateTemporaryCapture", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("Production temporary creation boundary missing.");
                    using var stream = (FileStream)(create.Invoke(sink, [temp]) ?? throw new InvalidOperationException("Temporary stream not created."));
                    stream.Write(Encoding.UTF8.GetBytes(c.Token!)); stream.Flush(flushToDisk: true);
                    CheckPrivatePolicy(capture, c); CheckPrivatePolicy(temp, c);
                    if (c.Scenario == "crash-temporary") Environment.Exit(17);
                    break;
                }
                case "prepare":
                {
                    var sink = Sink(); File.Delete(temp);
                    var now = DateTimeOffset.UtcNow;
                    var message = new LocalAccountMessage(c.MessageId, Guid.NewGuid(), "EmailVerification", "synthetic-acl@example.test", c.Token!, now, now.AddHours(2));
                    Require(sink.Publish(message, c.Fence, () => true) == LocalAccountMessagePublishOutcome.Prepared, "W03 Publish did not prepare.");
                    CheckPrivatePolicy(pending, c); Require(LocalAccountMessageSink.ReadCaptured(capture, operatorSid: c.OperatorSid, runtimeSid: c.RuntimeSid).Count == 0, "Pending capture was exposed as delivered.");
                    break;
                }
                case "promote":
                {
                    var sink = Sink();
                    Require(sink.ReconcilePending((id, effect) =>
                    {
                        Require(id == c.MessageId && effect == c.Fence, "W03 reconcile identity/fence mismatch.");
                        return LocalAccountMessagePendingDisposition.Promote;
                    }) == 1, "W03 reconcile did not commit the prepared capture.");
                    Require(sink.PromoteIfOwned(c.MessageId, c.Fence), "W03 committed promotion replay failed.");
                    CheckPrivatePolicy(final, c); break;
                }
                case "restart":
                    Require(Sink().Publish(new LocalAccountMessage(c.MessageId, Guid.NewGuid(), "EmailVerification", "synthetic-acl@example.test", c.Token!, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(2)), c.Fence, () => true) == LocalAccountMessagePublishOutcome.AlreadyCaptured, "W07 restart replay did not preserve captured outcome."); break;
                case "verify-crashed-temporary":
                case "read-temporary": Require(File.ReadAllText(temp) == c.Token, "W04 B did not read the actual temporary canary."); CheckPrivatePolicy(temp, c); break;
                case "read-pending": Require(File.ReadAllText(pending).Contains(c.Token!, StringComparison.Ordinal), "W04 B did not read the actual pending canary."); CheckPrivatePolicy(pending, c); break;
                case "read-final":
                {
                    var messages = LocalAccountMessageSink.ReadCaptured(capture, operatorSid: c.OperatorSid, runtimeSid: c.RuntimeSid);
                    Require(messages.Count == 1 && messages[0].Id == c.MessageId && messages[0].RawToken == c.Token, "W04 operator did not read the original final capture.");
                    CheckPrivatePolicy(final, c); break;
                }
                case "snapshot": digest = Snapshot(capture); break;
                case "deny-temporary": NativeDeny(capture, temp, sid, checks); break;
                case "deny-pending": NativeDeny(capture, pending, sid, checks); break;
                case "deny-final": NativeDeny(capture, final, sid, checks); break;
                case "reject-insecure":
                    InsecureFixtures(c, value => stage = value); break;
                case "hang":
                    Thread.Sleep(Timeout.Infinite); break;
                case "sweep-crashed-temporary":
                    File.SetLastWriteTimeUtc(temp, DateTime.UtcNow.AddMinutes(-16)); Sink().SweepExpired(DateTimeOffset.UtcNow); Require(!File.Exists(temp), "W09 restart did not dispose owned crashed temporary."); break;
                case "cleanup-capture":
                    if (Directory.Exists(capture)) { RejectReparseTree(capture); Directory.Delete(capture, true); }
                    break;
                default: throw new InvalidOperationException("Unknown allowlisted probe scenario.");
            }
            using var process = Process.GetCurrentProcess();
            stage = "Report";
            var report = new ProbeReport(c.Nonce, c.Role, c.Scenario, sid.Value, process.Id, process.StartTime.ToUniversalTime(), admin, privileged, true, true, digest, checks.ToArray());
            File.WriteAllText(c.Report, JsonSerializer.Serialize(report, Json)); return 0;
        }
        catch
        {
            Console.Error.WriteLine("NEXORA_ACL_STAGE:" + stage); return 1;
        }
    }

    private static void NativeDeny(string capture, string path, SecurityIdentifier sid, List<string> results)
    {
        void Denied(string name, Action attempt)
        {
            try { attempt(); }
            catch (UnauthorizedAccessException ex) when ((ex.HResult & 0xffff) == 5) { results.Add(name + ":AccessDenied:5"); return; }
            catch (IOException ex) when ((ex.HResult & 0xffff) == 5) { results.Add(name + ":AccessDenied:5"); return; }
            throw new InvalidOperationException("W05 native access denied was not proven for " + name + ".");
        }
        Denied("enumerate", () => _ = Directory.EnumerateFileSystemEntries(capture).ToArray());
        Denied("read", () => _ = File.ReadAllBytes(path));
        Denied("create", () => { using var s = new FileStream(Path.Combine(capture, "foreign-" + Guid.NewGuid().ToString("N")), FileMode.CreateNew, FileAccess.Write); s.WriteByte(1); });
        Denied("overwrite", () => { using var s = new FileStream(path, FileMode.Open, FileAccess.Write); s.WriteByte(1); });
        Denied("delete", () => File.Delete(path));
        Denied("dacl", () => { var acl = new FileSecurity(); acl.SetAccessRuleProtection(true, false); acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow)); new FileInfo(path).SetAccessControl(acl); });
        Denied("owner", () => { var acl = new FileSecurity(); acl.SetOwner(sid); new FileInfo(path).SetAccessControl(acl); });
        Require(results.Count == 7, "W05 all seven native attempts must execute.");
    }

    private static void CheckPrivatePolicy(string path, Config c)
    {
        FileSystemSecurity security = Directory.Exists(path)
            ? new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access)
            : new FileInfo(path).GetAccessControl(AccessControlSections.Access);
        Require(security.AreAccessRulesProtected, "W03 ACL inheritance is not protected.");
        var allowed = new HashSet<string> { c.RuntimeSid, c.OperatorSid };
        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            Require(!rule.IsInherited && rule.AccessControlType == AccessControlType.Allow && allowed.Contains(rule.IdentityReference.Value) &&
                    (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl, "W03 capture policy contains foreign/inherited/insufficient ACE.");
            allowed.Remove(rule.IdentityReference.Value);
        }
        Require(allowed.Count == 0, "W03 runtime/operator FullControl entries missing.");
    }

    private static void InsecureFixtures(Config c, Action<string> stage)
    {
        foreach (var inherited in new[] { false, true })
        {
            stage(inherited ? "InheritedCreate" : "ExplicitCreate");
            var path = Path.Combine(c.Root, inherited ? "unsafe-inherited" : "unsafe-explicit"); Directory.CreateDirectory(path);
            if (!inherited) SetDirectoryAcl(path, [(new SecurityIdentifier(c.RuntimeSid), FileSystemRights.FullControl), (new SecurityIdentifier(c.OperatorSid), FileSystemRights.FullControl), (new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), FileSystemRights.ReadAndExecute)]);
            var before = new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access);
            stage(inherited ? "InheritedInitialize" : "ExplicitInitialize");
            var rejected = false;
            try { _ = new LocalAccountMessageSink(path, c.Root, NullLogger<LocalAccountMessageSink>.Instance, c.OperatorSid, c.RuntimeSid); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected && before == new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access), "W08 insecure directory was accepted or silently repaired.");
            stage(inherited ? "InheritedRead" : "ExplicitRead");
            rejected = false;
            try { _ = LocalAccountMessageSink.ReadCaptured(path, operatorSid: c.OperatorSid, runtimeSid: c.RuntimeSid); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected && before == new DirectoryInfo(path).GetAccessControl(AccessControlSections.Access).GetSecurityDescriptorSddlForm(AccessControlSections.Access), "W08 insecure directory read was accepted or repaired.");
            // A created these fixtures and owns deletion rights even where B intentionally has read only.
            stage(inherited ? "InheritedCleanup" : "ExplicitCleanup");
            RejectReparseTree(path); Directory.Delete(path, true);
        }
        stage("UnsafeFileSetup");
        var final = Path.Combine(c.Root, "capture", $"{c.MessageId:N}.json");
        var info = new FileInfo(final);
        var original = info.GetAccessControl();
        var insecure = info.GetAccessControl();
        insecure.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), FileSystemRights.Read, AccessControlType.Allow));
        info.SetAccessControl(insecure);
        var beforeFile = Snapshot(Path.GetDirectoryName(final)!);
        stage("UnsafeFileRead");
        Require(LocalAccountMessageSink.ReadCaptured(Path.GetDirectoryName(final)!, operatorSid: c.OperatorSid, runtimeSid: c.RuntimeSid).Count == 0, "W08 insecure owned file exposed a token.");
        Require(beforeFile == Snapshot(Path.GetDirectoryName(final)!), "W08 insecure owned file was silently repaired.");
        stage("UnsafeFileRestore");
        // GetAccessControl returns an unmodified descriptor; explicitly mark the captured DACL for restoration.
        original.SetSecurityDescriptorSddlForm(original.GetSecurityDescriptorSddlForm(AccessControlSections.Access), AccessControlSections.Access);
        info.SetAccessControl(original);
        CheckPrivatePolicy(final, c);
    }

    private static void AssertManifestMismatch(string root, string nonce)
    {
        var path = Path.Combine(root, "owner.json"); var original = File.ReadAllText(path);
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(new { nonce = Guid.NewGuid().ToString("N"), root }));
            var rejected = false;
            try { ValidateOwnedRoot(root, nonce); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && Directory.Exists(root), "W09 manifest mismatch did not refuse destructive cleanup.");
        }
        finally { File.WriteAllText(path, original); }
        ValidateOwnedRoot(root, nonce);
    }

    private static string Snapshot(string path)
    {
        var entries = new List<string> { new DirectoryInfo(path).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner) };
        foreach (var file in Directory.GetFiles(path).Order(StringComparer.Ordinal)) entries.Add(Path.GetFileName(file) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) + ":" + new FileInfo(file).GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', entries))));
    }

    private static void ValidateOwnedRoot(string root, string nonce)
    {
        Require(Guid.TryParseExact(nonce, "N", out _) && Path.GetFileName(root) == "NexoraAcl-" + nonce &&
                Path.GetDirectoryName(Path.GetFullPath(root)) == Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "W09 run root ownership mismatch.");
        // The foreign process must be able to validate public ownership without
        // enumerating private A/B config or capture descendants.
        Require((File.GetAttributes(root) & FileAttributes.ReparsePoint) == 0, "W09 reparse run root rejected.");
        using var owner = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "owner.json")));
        Require(owner.RootElement.GetProperty("nonce").GetString() == nonce && owner.RootElement.GetProperty("root").GetString() == root, "W09 owner manifest mismatch; destructive cleanup refused.");
    }

    private static void RejectReparseTree(string path)
    {
        Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "W09 reparse root rejected.");
        foreach (var child in Directory.EnumerateFileSystemEntries(path))
        {
            Require((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0, "W09 reparse descendant rejected.");
            if (Directory.Exists(child)) RejectReparseTree(child);
        }
    }

    private static void SetDirectoryAcl(string path, (SecurityIdentifier Sid, FileSystemRights Rights)[] entries)
    {
        var acl = new DirectorySecurity(); acl.SetAccessRuleProtection(true, false);
        foreach (var e in entries) acl.AddAccessRule(new FileSystemAccessRule(e.Sid, e.Rights, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(path).SetAccessControl(acl);
    }

    private static void WritePrivate(string path, string content, SecurityIdentifier parent, SecurityIdentifier child)
    {
        var acl = new FileSecurity(); acl.SetAccessRuleProtection(true, false);
        foreach (var sid in new[] { parent, child }.Distinct()) acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        using var stream = new FileInfo(path).Create(FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.WriteThrough, acl);
        stream.Write(Encoding.UTF8.GetBytes(content)); stream.Flush(true);
    }

    private static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
        foreach (var directory in Directory.GetDirectories(source)) CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static bool HasBypassPrivileges(WindowsIdentity identity)
    {
        _ = GetTokenInformation(identity.AccessToken.DangerousGetHandle(), 3, IntPtr.Zero, 0, out var length);
        var data = Marshal.AllocHGlobal(length);
        try
        {
            if (!GetTokenInformation(identity.AccessToken.DangerousGetHandle(), 3, data, length, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());
            var count = Marshal.ReadInt32(data); Require(count is >= 0 and < 256, "W02 token privilege array invalid.");
            foreach (var name in new[] { "SeBackupPrivilege", "SeRestorePrivilege", "SeDebugPrivilege", "SeTakeOwnershipPrivilege" })
            {
                if (!LookupPrivilegeValue(null, name, out var target)) throw new Win32Exception(Marshal.GetLastWin32Error());
                for (var i = 0; i < count; i++) if (Marshal.ReadInt64(data, 4 + i * 12) == target) return true;
            }
            return false;
        }
        finally { Marshal.FreeHGlobal(data); }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private enum ProbeFailureKind { Protocol, Timeout, Cancelled, Crash }
    private sealed class ProbeFailureException(ProbeFailureKind kind, string message) : Exception(message) { internal ProbeFailureKind Kind { get; } = kind; }
    private sealed class InjectedSetupException : Exception;
    private sealed record Config(string Root, string Nonce, string Role, string Scenario, string ExpectedSid, string RuntimeSid, string OperatorSid, string Control, Guid MessageId, Guid Fence, Guid TempId, string? Token, string Report);
    private sealed record ProbeReport(string Nonce, string Role, string Scenario, string Sid, int Pid, DateTime CreatedAt, bool Admin, bool PrivilegedBypass, bool ReachedBoundary, bool Succeeded, string? Digest, string[] NativeChecks);

    private sealed class Account : IDisposable
    {
        internal required string Name { get; init; }
        internal required string Role { get; init; }
        internal required string Comment { get; init; }
        internal required SecurityIdentifier Sid { get; init; }
        internal required SecureString Password { get; init; }

        internal static Account Create(string role, string nonce)
        {
            var name = "nx" + role[0] + Guid.NewGuid().ToString("N")[..14];
            var password = "Nx7!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            var comment = Marker + nonce;
            var info = new UserInfo1 { Name = name, Password = password, Privilege = 1, Comment = comment, Flags = 0x10201 };
            var result = NetUserAdd(null, 1, ref info, out _);
            Require(result == 0, "W01 disposable account creation failed with native code " + result + ".");
            SecureString? secure = null;
            SecurityIdentifier? sid = null;
            try
            {
                sid = ReadOwnedSid(name, comment);
                secure = new SecureString(); foreach (var ch in password) secure.AppendChar(ch); secure.MakeReadOnly();
                return new Account { Name = name, Role = role, Comment = comment, Sid = sid, Password = secure };
            }
            catch
            {
                secure?.Dispose();
                // Query the native fresh account and exact nonce marker before rollback; never delete by prefix.
                var current = ReadOwnedSid(name, comment);
                Require(sid is null || current.Equals(sid), "W09 partial account SID changed; cleanup refused.");
                Require(NetUserDel(null, name) == 0 && !Exists(name), "W09 partial native account rollback failed.");
                throw;
            }
        }

        private static SecurityIdentifier ReadOwnedSid(string name, string comment)
        {
            var result = NetUserGetInfo(null, name, 23, out var data);
            Require(result == 0, "W09 native account identity lookup failed.");
            try
            {
                var current = Marshal.PtrToStructure<UserInfo23>(data);
                Require(current.Name == name && current.Comment == comment && current.Sid != IntPtr.Zero, "W09 native account creation marker mismatch.");
                return new SecurityIdentifier(current.Sid);
            }
            finally { if (data != IntPtr.Zero) _ = NetApiBufferFree(data); }
        }

        internal static bool Exists(string name)
        {
            var result = NetUserGetInfo(null, name, 1, out var data);
            if (data != IntPtr.Zero) _ = NetApiBufferFree(data);
            if (result == 2221) return false;
            Require(result == 0, "W09 account existence lookup failed."); return true;
        }

        public void Dispose()
        {
            try
            {
                Require(ReadOwnedSid(Name, Comment).Equals(Sid), "W09 account SID/creation marker mismatch; cleanup refused.");
                Require(NetUserDel(null, Name) == 0 && !Exists(Name), "W09 exact owned account deletion failed.");
            }
            finally { Password.Dispose(); }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct UserInfo1
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string? Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Password;
        public uint PasswordAge;
        public uint Privilege;
        [MarshalAs(UnmanagedType.LPWStr)] public string? HomeDirectory;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
        public uint Flags;
        [MarshalAs(UnmanagedType.LPWStr)] public string? ScriptPath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct UserInfo23
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string? Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string? FullName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
        public uint Flags;
        public IntPtr Sid;
    }

    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)] private static extern int NetUserAdd(string? server, int level, ref UserInfo1 info, out uint error);
    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)] private static extern int NetUserGetInfo(string? server, string user, int level, out IntPtr data);
    [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)] private static extern int NetUserDel(string? server, string user);
    [DllImport("Netapi32.dll")] private static extern int NetApiBufferFree(IntPtr data);
    [DllImport("advapi32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetTokenInformation(IntPtr token, int tokenInformationClass, IntPtr data, int length, out int returnedLength);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool LookupPrivilegeValue(string? system, string name, out long luid);
}
