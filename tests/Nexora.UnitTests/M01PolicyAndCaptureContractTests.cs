using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Application.Identity;
using Nexora.Domain.Access;
using Nexora.Domain.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;
using Xunit;

namespace Nexora.UnitTests;

public sealed class M01PolicyAndCaptureContractTests
{
    [Fact]
    public void Local_release_migration_manifest_contains_all_reviewed_release_files()
    {
        Assert.Equal(
            [
                "20260909_0001_m01_identity_platform.sql",
                "20260909_0002_bootstrap_closure.sql",
                "20260910_0002_r1_catalog_and_productivity.sql",
                "20260910_0003_productivity_lifecycle.sql",
                "20260910_0004_notifications_inbox.sql",
                "20260910_0005_preferences.sql",
                "20260910_0006_documents_pages.sql",
                "20260910_0007_action_catalog_alignment.sql",
                "20260910_0008_finance_manual_records.sql",
                "20260910_0009_bookmarks_manual.sql",
                "20260910_0010_snippets_manual.sql",
                "20260910_0011_reading_queue_bookmarks.sql",
                "20260910_0012_organization_tags.sql",
                "20260910_0013_developer_toolbox_pure.sql",
                "20260910_0014_goals_numeric.sql",
                "20260910_0015_dashboard_attention.sql",
                "20260910_0016_global_search_source_query.sql",
                "20260910_0017_favorites_refs.sql",
                "20260910_0018_local_runtime_catalog_gate.sql",
                "20260910_0019_productivity_contract_alignment.sql",
                "20260910_0020_task_calendar_projection.sql",
                "20260911_0021_core_sharing_support_files.sql",
                "20260911_0022_reminders_scheduling.sql",
                "20260911_0023_planner_habits.sql",
                "20260912_0024_planner_habits_owner_integrity.sql",
                "20260913_0025_review_hardening.sql",
                "20260913_0026_identity_local_delivery.sql",
                "20260922_0027_sanitize_session_device_labels.sql",
                "20260928_0028_bootstrap_ready_module_grants.sql",
                "20260929_0029_habit_trash_consistency.sql",
            "20260930_0030_access_preview_action.sql",
            "20261001_0031_trash_source_permissions.sql",
            "20261001_0032_time_tracking_local.sql",
            "20261001_0033_focus_local.sql"
            ],
            M01MigrationManifest.RequiredFileNames);
        Assert.Contains("20260910_0002_r1_catalog_and_productivity.sql", M01MigrationManifest.RequiredFileNames);
        Assert.Contains("20260911_0023_planner_habits.sql", M01MigrationManifest.RequiredFileNames);
    }

    [Theory]
    [InlineData(UserState.PendingVerification, false, false, false, false, "EmailVerificationRequired")]
    [InlineData(UserState.Active, true, false, false, false, "LoginAllowed")]
    [InlineData(UserState.Disabled, true, false, true, false, "AccountUnavailable")]
    [InlineData(UserState.Deleted, true, true, false, false, "AccountUnavailable")]
    [InlineData(UserState.Active, true, false, false, true, "MfaUnavailable")]
    public void Login_policy_preserves_account_lifecycle_and_MFA_boundaries(
        UserState state,
        bool emailConfirmed,
        bool isDeleted,
        bool isDisabled,
        bool hasEnabledMfa,
        string expectedCode)
    {
        var user = new UserLoginSnapshot(
            Guid.NewGuid(),
            state,
            emailConfirmed,
            isDeleted,
            isDisabled,
            hasEnabledMfa,
            "synthetic-security-stamp");

        var decision = AccountAccessPolicy.CanLogin(user);

        Assert.Equal(expectedCode, decision.Code);
        Assert.Equal(expectedCode == "LoginAllowed", decision.Allowed);
    }

    [Fact]
    public void Admin_grant_mutation_uses_the_explicit_admin_grantable_manifest()
    {
        Assert.True(ActionGrantPolicy.CanGrantAllow("settings.preference.update").Allowed);
        Assert.Equal("ActionNotGrantable", ActionGrantPolicy.CanGrantAllow("identity.account.register").Code);
        Assert.Equal("ActionNotGrantable", ActionGrantPolicy.CanGrantAllow("access.role.set").Code);
        Assert.False(ActionGrantPolicy.IsAdminGrantable("notifications.dispatch.deliver"));
    }

    [Fact]
    public void Profile_and_registration_policy_reject_privileged_fields_and_invalid_preferences()
    {
        Assert.True(ProfilePatchPolicy.ValidatePatchFields(new[] { "displayName", "locale" }).Allowed);
        Assert.Equal("UnknownField", ProfilePatchPolicy.ValidatePatchFields(new[] { "ownerId" }).Code);
        Assert.Equal("LocaleUnsupported", RegistrationPolicy.ValidateLocale("fr").Code);
        Assert.Equal("TimeZoneRequired", RegistrationPolicy.ValidateTimeZoneId(" ").Code);
        Assert.True(RegistrationPolicy.ValidateTimeZoneId("America/New_York").Allowed);
        Assert.True(RegistrationPolicy.ValidateTimeZoneId("Etc/UTC").Allowed);
        Assert.True(RegistrationPolicy.ValidateTimeZoneId(" Asia/Ho_Chi_Minh ").Allowed);
        Assert.Equal("TimeZoneInvalid", RegistrationPolicy.ValidateTimeZoneId("Invalid/TimeZone").Code);
        Assert.Equal("TimeZoneInvalid", RegistrationPolicy.ValidateTimeZoneId("Pacific Standard Time").Code);
    }

    [Fact]
    public void Foreign_and_mismatched_final_JSON_are_neither_read_nor_deleted()
    {
        using var sandbox = new CaptureSandbox();
        var foreign = sandbox.CreateMessage();
        sandbox.PrepareAndPromote(foreign, Guid.NewGuid());
        var foreignPath = Path.Combine(sandbox.CaptureDirectory, "operator-export.json");
        File.Move(sandbox.FinalPath(foreign.Id), foreignPath);
        File.SetLastWriteTimeUtc(foreignPath, DateTime.UtcNow.AddDays(-2));

        var mismatched = sandbox.CreateMessage();
        sandbox.PrepareAndPromote(mismatched, Guid.NewGuid());
        var mismatchPath = sandbox.FinalPath(Guid.NewGuid());
        File.Move(sandbox.FinalPath(mismatched.Id), mismatchPath);
        File.SetLastWriteTimeUtc(mismatchPath, DateTime.UtcNow.AddDays(-2));

        var captures = LocalAccountMessageSink.ReadCaptured(sandbox.CaptureDirectory, DateTimeOffset.UtcNow);
        sandbox.Sink.SweepExpired(DateTimeOffset.UtcNow.AddDays(2));

        Assert.DoesNotContain(captures, item => item.Id == foreign.Id || item.Id == mismatched.Id);
        Assert.True(File.Exists(foreignPath));
        Assert.True(File.Exists(mismatchPath));
    }

    [Fact]
    public void Only_an_old_owned_malformed_final_capture_is_cleaned_after_grace()
    {
        using var sandbox = new CaptureSandbox();
        var owned = sandbox.CreateMessage();
        sandbox.PrepareAndPromote(owned, Guid.NewGuid());
        var ownedPath = sandbox.FinalPath(owned.Id);
        File.WriteAllText(ownedPath, "{");
        File.SetLastWriteTimeUtc(ownedPath, DateTime.UtcNow.AddDays(-2));

        var foreignPath = Path.Combine(sandbox.CaptureDirectory, "operator-malformed.json");
        File.WriteAllText(foreignPath, "{");
        File.SetLastWriteTimeUtc(foreignPath, DateTime.UtcNow.AddDays(-2));

        _ = LocalAccountMessageSink.ReadCaptured(sandbox.CaptureDirectory, DateTimeOffset.UtcNow);

        Assert.False(File.Exists(ownedPath));
        Assert.True(File.Exists(foreignPath));
    }

    [Fact]
    public void An_old_private_partial_temporary_capture_does_not_block_a_later_delivery()
    {
        using var sandbox = new CaptureSandbox();
        var orphanPath = Path.Combine(
            sandbox.CaptureDirectory,
            "." + Guid.NewGuid().ToString("N") + "." + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(orphanPath, "{\"Id\":");
        MakePrivateIfLinux(orphanPath);
        File.SetLastWriteTimeUtc(orphanPath, DateTime.UtcNow.AddHours(-1));

        sandbox.Sink.SweepExpired(DateTimeOffset.UtcNow.AddHours(1));
        var next = sandbox.CreateMessage();
        var outcome = sandbox.Sink.Publish(next, Guid.NewGuid(), () => true);

        Assert.False(File.Exists(orphanPath));
        Assert.Equal(LocalAccountMessagePublishOutcome.Prepared, outcome);
    }


    [Fact]
    public void Existing_partial_temporary_file_is_not_deleted_by_a_failed_new_create()
    {
        using var sandbox = new CaptureSandbox();
        var message = sandbox.CreateMessage();
        var fence = Guid.NewGuid();
        var existingPath = Path.Combine(
            sandbox.CaptureDirectory,
            "." + message.Id.ToString("N") + "." + fence.ToString("N") + ".tmp");
        File.WriteAllText(existingPath, "{\"Id\":");

        Assert.Throws<IOException>(() => sandbox.Sink.Publish(message, fence, () => true));
        Assert.True(File.Exists(existingPath));

        var next = sandbox.CreateMessage();
        var outcome = sandbox.Sink.Publish(next, Guid.NewGuid(), () => true);
        Assert.Equal(LocalAccountMessagePublishOutcome.Prepared, outcome);
    }
    private static void MakePrivateIfLinux(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            MakePrivateLinux(path);
        }
    }

    [SupportedOSPlatform("linux")]
    private static void MakePrivateLinux(string path) =>
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

    private sealed class CaptureSandbox : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "nexora-m01-contract-" + Guid.NewGuid().ToString("N"));

        public CaptureSandbox()
        {
            ContentRoot = Path.Combine(_root, "content");
            CaptureDirectory = Path.Combine(_root, "captures");
            Directory.CreateDirectory(Path.Combine(ContentRoot, "wwwroot"));
            Sink = new LocalAccountMessageSink(
                CaptureDirectory,
                ContentRoot,
                NullLogger<LocalAccountMessageSink>.Instance);
        }

        public string ContentRoot { get; }

        public string CaptureDirectory { get; }

        public LocalAccountMessageSink Sink { get; }

        public LocalAccountMessage CreateMessage() => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EmailVerification",
            "synthetic-user@example.invalid",
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1));

        public string FinalPath(Guid messageId) =>
            Path.Combine(CaptureDirectory, messageId.ToString("N") + ".json");

        public void PrepareAndPromote(LocalAccountMessage message, Guid fence)
        {
            Assert.Equal(LocalAccountMessagePublishOutcome.Prepared, Sink.Publish(message, fence, () => true));
            Assert.True(Sink.PromoteIfOwned(message.Id, fence));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root))
                {
                    Directory.Delete(_root, recursive: true);
                }
            }
            catch
            {
                // Test cleanup never exposes capture contents or masks the assertion outcome.
            }
        }
    }
}
