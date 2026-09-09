using System.Security.Cryptography;
using Nexora.Api.Http;
using Nexora.Domain.Modules;

namespace Nexora.Api.Features.Modules;

/// <summary>
/// Development-only module policy store for the M01 Minimal API surface.
/// The final authority must be SQL-backed through the platform/module tables.
/// </summary>
public sealed class DevelopmentModuleStore
{
    private static readonly TimeSpan PreviewTtl = TimeSpan.FromMinutes(2);
    private readonly object _sync = new();
    private readonly Dictionary<Guid, ModuleRecord> _modulesById = new();
    private readonly Dictionary<string, ModuleRecord> _modulesByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PreviewRecord> _previewsByToken = new(StringComparer.Ordinal);

    public DevelopmentModuleStore()
    {
        Seed(new ModuleRecord(Guid.Parse("11111111-1111-1111-1111-111111111111"), "FX01", "Identity", ModuleRuntimeState.Ready, true, true, 1, Array.Empty<string>(), new[] { "FX02", "FX03", "FX09" }));
        Seed(new ModuleRecord(Guid.Parse("22222222-2222-2222-2222-222222222222"), "FX02", "Users / Roles / Permissions", ModuleRuntimeState.Ready, true, true, 1, new[] { "FX01" }, Array.Empty<string>()));
        Seed(new ModuleRecord(Guid.Parse("33333333-3333-3333-3333-333333333333"), "FX03", "Module Platform", ModuleRuntimeState.Ready, true, true, 1, new[] { "FX01" }, Array.Empty<string>()));
        Seed(new ModuleRecord(Guid.Parse("99999999-9999-9999-9999-999999999999"), "FX09", "Settings / App Shell", ModuleRuntimeState.Ready, true, true, 1, new[] { "FX01" }, Array.Empty<string>()));
        Seed(new ModuleRecord(Guid.Parse("30303030-3030-3030-3030-303030303030"), "FX30", "Price Tracking", ModuleRuntimeState.Paused, false, false, 1, Array.Empty<string>(), Array.Empty<string>()));
        Seed(new ModuleRecord(Guid.Parse("34343434-3434-3434-3434-343434343434"), "FX34", "Automation / Scheduler / Workflows", ModuleRuntimeState.Paused, false, false, 1, Array.Empty<string>(), Array.Empty<string>()));
        Seed(new ModuleRecord(Guid.Parse("35353535-3535-3535-3535-353535353535"), "FX35", "Integrations / Webhooks / n8n", ModuleRuntimeState.Paused, false, false, 1, Array.Empty<string>(), Array.Empty<string>()));
    }

    public ApiResult<ModulePageResponse> ListModules(int? limit)
    {
        lock (_sync)
        {
            var take = Math.Clamp(limit ?? 25, 1, 100);
            var items = _modulesByCode.Values
                .OrderBy(module => module.Code, StringComparer.OrdinalIgnoreCase)
                .Take(take)
                .Select(ToResponse)
                .ToArray();

            return ApiResult<ModulePageResponse>.Success(new ModulePageResponse(items, null));
        }
    }

    public ApiResult<ModulePolicyPreviewResponse> PreviewModule(Guid moduleId, ModulePolicyChangeRequest request)
    {
        lock (_sync)
        {
            if (!_modulesById.TryGetValue(moduleId, out var module))
            {
                return ApiResult<ModulePolicyPreviewResponse>.Failure("ResourceUnavailable", StatusCodes.Status404NotFound, "Module unavailable.");
            }

            var validation = ValidateEditableFields(request.SystemEnabled, request.RegistrationEnabled);
            if (validation is not null)
            {
                return validation;
            }

            var changes = BuildDiffs(module, request.SystemEnabled, request.RegistrationEnabled);
            var blockers = EvaluateBlockers(module, request.SystemEnabled, request.RegistrationEnabled);
            var token = NewToken();
            var preview = new PreviewRecord(
                token,
                module.Id,
                ModuleDigest(module, request.SystemEnabled, request.RegistrationEnabled),
                module.PolicyRevision,
                DateTimeOffset.UtcNow.Add(PreviewTtl));
            _previewsByToken[token] = preview;

            return ApiResult<ModulePolicyPreviewResponse>.Success(new ModulePolicyPreviewResponse(token, preview.ExpiresAt, ETag(module.PolicyRevision), changes, blockers));
        }
    }

    public ApiResult<ModuleResponse> SetModulePolicy(Guid moduleId, string? ifMatch, ModulePolicyCommitRequest request)
    {
        lock (_sync)
        {
            if (!_modulesById.TryGetValue(moduleId, out var module))
            {
                return ApiResult<ModuleResponse>.Failure("ResourceUnavailable", StatusCodes.Status404NotFound, "Module unavailable.");
            }

            if (string.IsNullOrWhiteSpace(ifMatch))
            {
                return ApiResult<ModuleResponse>.Failure("PreconditionRequired", StatusCodes.Status428PreconditionRequired, "If-Match is required.");
            }

            if (!string.Equals(ifMatch, ETag(module.PolicyRevision), StringComparison.Ordinal))
            {
                return ApiResult<ModuleResponse>.Failure("RevisionConflict", StatusCodes.Status412PreconditionFailed, "Module policy revision changed.");
            }

            var validation = ValidateEditableFields(request.SystemEnabled, request.RegistrationEnabled);
            if (validation is not null)
            {
                return validation;
            }

            if (string.IsNullOrWhiteSpace(request.PreviewToken) ||
                !_previewsByToken.TryGetValue(request.PreviewToken, out var preview) ||
                preview.ModuleId != module.Id ||
                preview.ExpiresAt <= DateTimeOffset.UtcNow ||
                preview.PolicyRevision != module.PolicyRevision ||
                !string.Equals(preview.BodyDigest, ModuleDigest(module, request.SystemEnabled, request.RegistrationEnabled), StringComparison.Ordinal))
            {
                return ApiResult<ModuleResponse>.Failure("PreviewStale", StatusCodes.Status409Conflict, "Module policy preview is stale.");
            }

            var blockers = EvaluateBlockers(module, request.SystemEnabled, request.RegistrationEnabled);
            if (blockers.Count > 0)
            {
                var blocker = blockers[0];
                return ApiResult<ModuleResponse>.Failure(blocker.Code, StatusCodes.Status409Conflict, blocker.Message);
            }

            if (request.SystemEnabled is { } systemEnabled)
            {
                module.SystemEnabled = systemEnabled;
            }

            if (request.RegistrationEnabled is { } registrationEnabled)
            {
                module.RegistrationEnabled = registrationEnabled;
            }

            module.PolicyRevision++;
            module.UpdatedAt = DateTimeOffset.UtcNow;
            _previewsByToken.Remove(request.PreviewToken);

            return ApiResult<ModuleResponse>.Success(ToResponse(module));
        }
    }

    private void Seed(ModuleRecord module)
    {
        _modulesById[module.Id] = module;
        _modulesByCode[module.Code] = module;
    }

    private static ApiResult<ModulePolicyPreviewResponse>? ValidateEditableFields(bool? systemEnabled, bool? registrationEnabled)
    {
        if (systemEnabled is null && registrationEnabled is null)
        {
            return ApiResult<ModulePolicyPreviewResponse>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, "At least one editable module policy field is required.");
        }

        return null;
    }

    private IReadOnlyList<ModulePolicyBlocker> EvaluateBlockers(ModuleRecord module, bool? systemEnabled, bool? registrationEnabled)
    {
        var blockers = new List<ModulePolicyBlocker>();
        if (systemEnabled == true && !module.SystemEnabled)
        {
            var dependencies = module.RequiredDependencies.Select(code => new ModuleDependencyStatus(code, _modulesByCode.TryGetValue(code, out var dependency) && dependency.SystemEnabled, Required: true));
            var decision = ModulePolicy.CanEnable(module.Code, module.State, dependencies);
            if (!decision.Allowed)
            {
                blockers.Add(new ModulePolicyBlocker(decision.Code, decision.Message, nameof(ModulePolicyChangeRequest.SystemEnabled)));
            }
        }

        if (systemEnabled == false && module.SystemEnabled)
        {
            var dependents = module.RequiredBy.Select(code => new ModuleDependencyStatus(code, _modulesByCode.TryGetValue(code, out var dependent) && dependent.SystemEnabled, Required: true));
            var decision = ModulePolicy.CanDisable(dependents);
            if (!decision.Allowed)
            {
                blockers.Add(new ModulePolicyBlocker(decision.Code, decision.Message, nameof(ModulePolicyChangeRequest.SystemEnabled)));
            }
        }

        var effectiveSystemEnabled = systemEnabled ?? module.SystemEnabled;
        if (registrationEnabled == true && !effectiveSystemEnabled)
        {
            blockers.Add(new ModulePolicyBlocker("DependencyUnavailable", "Registration defaults cannot be enabled while the module is disabled.", nameof(ModulePolicyChangeRequest.RegistrationEnabled)));
        }

        return blockers;
    }

    private static IReadOnlyList<ModuleChangeDiff> BuildDiffs(ModuleRecord module, bool? systemEnabled, bool? registrationEnabled)
    {
        var changes = new List<ModuleChangeDiff>();
        if (systemEnabled is not null && systemEnabled.Value != module.SystemEnabled)
        {
            changes.Add(new ModuleChangeDiff("systemEnabled", module.SystemEnabled.ToString(), systemEnabled.Value.ToString()));
        }

        if (registrationEnabled is not null && registrationEnabled.Value != module.RegistrationEnabled)
        {
            changes.Add(new ModuleChangeDiff("registrationEnabled", module.RegistrationEnabled.ToString(), registrationEnabled.Value.ToString()));
        }

        return changes;
    }

    private static ModuleResponse ToResponse(ModuleRecord module) => new(
        module.Id,
        module.Code,
        module.Name,
        module.State.ToString(),
        module.SystemEnabled,
        module.RegistrationEnabled,
        module.PolicyRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ETag(module.PolicyRevision),
        module.RequiredDependencies,
        module.RequiredBy,
        module.State == ModuleRuntimeState.Paused ? "PO_PAUSED" : null);

    private static string ModuleDigest(ModuleRecord module, bool? systemEnabled, bool? registrationEnabled) =>
        string.Join('|', module.Id, module.PolicyRevision, systemEnabled?.ToString() ?? "<same>", registrationEnabled?.ToString() ?? "<same>");

    private static string ETag(long revision) => $"\"{Convert.ToBase64String(BitConverter.GetBytes(revision))}\"";

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed record PreviewRecord(string Token, Guid ModuleId, string BodyDigest, long PolicyRevision, DateTimeOffset ExpiresAt);

    private sealed class ModuleRecord
    {
        public ModuleRecord(Guid id, string code, string name, ModuleRuntimeState state, bool systemEnabled, bool registrationEnabled, long policyRevision, IReadOnlyList<string> requiredDependencies, IReadOnlyList<string> requiredBy)
        {
            Id = id;
            Code = code;
            Name = name;
            State = state;
            SystemEnabled = systemEnabled;
            RegistrationEnabled = registrationEnabled;
            PolicyRevision = policyRevision;
            RequiredDependencies = requiredDependencies;
            RequiredBy = requiredBy;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; }
        public string Code { get; }
        public string Name { get; }
        public ModuleRuntimeState State { get; }
        public bool SystemEnabled { get; set; }
        public bool RegistrationEnabled { get; set; }
        public long PolicyRevision { get; set; }
        public IReadOnlyList<string> RequiredDependencies { get; }
        public IReadOnlyList<string> RequiredBy { get; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
