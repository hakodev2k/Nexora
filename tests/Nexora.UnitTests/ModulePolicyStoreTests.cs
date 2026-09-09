using Nexora.Api.Features.Modules;

namespace Nexora.UnitTests;

internal static class ModulePolicyStoreTests
{
    public static void Register(TestRunner runner)
    {
        runner.Add("module store lists core and paused M01 catalog entries", () =>
        {
            var store = new DevelopmentModuleStore();
            var result = store.ListModules(limit: null);

            AssertEx.True(result.Succeeded, "Module catalog list should succeed");
            AssertEx.True(result.Value!.Items.Any(module => module.Code == "FX01" && module.SystemEnabled), "Identity module should be enabled");
            AssertEx.True(result.Value.Items.Any(module => module.Code == "FX34" && module.UnavailableReason == "PO_PAUSED"), "Automation must remain PO_PAUSED");
        });

        runner.Add("module preview blocks enabling PO-paused module", () =>
        {
            var store = new DevelopmentModuleStore();
            var priceTrackingId = Guid.Parse("30303030-3030-3030-3030-303030303030");

            var preview = store.PreviewModule(priceTrackingId, new ModulePolicyChangeRequest(SystemEnabled: true, RegistrationEnabled: null));

            AssertEx.True(preview.Succeeded, "Preview should return blockers instead of mutating state");
            AssertEx.True(preview.Value!.Blockers.Any(blocker => blocker.Code == "DecisionBlocked"), "Paused module enable should be blocked");
        });

        runner.Add("module commit requires current etag and matching preview", () =>
        {
            var store = new DevelopmentModuleStore();
            var settingsId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            var before = store.ListModules(limit: null).Value!.Items.Single(module => module.Id == settingsId);
            var preview = store.PreviewModule(settingsId, new ModulePolicyChangeRequest(SystemEnabled: null, RegistrationEnabled: false));

            var wrongEtag = store.SetModulePolicy(settingsId, "\"stale\"", new ModulePolicyCommitRequest(SystemEnabled: null, RegistrationEnabled: false, preview.Value!.PreviewToken));
            AssertEx.False(wrongEtag.Succeeded, "Commit with stale ETag should fail");
            AssertEx.Equal("RevisionConflict", wrongEtag.Code, "Expected revision conflict");

            var committed = store.SetModulePolicy(settingsId, before.ETag, new ModulePolicyCommitRequest(SystemEnabled: null, RegistrationEnabled: false, preview.Value.PreviewToken));
            AssertEx.True(committed.Succeeded, "Commit with matching preview and ETag should apply");
            AssertEx.False(committed.Value!.RegistrationEnabled, "Registration default should now be disabled");
        });
    }
}
