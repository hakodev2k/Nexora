namespace Nexora.Api.Features.Modules;

public sealed record ModulePageResponse(IReadOnlyList<ModuleResponse> Items, string? NextCursor);

public sealed record ModuleResponse(
    Guid Id,
    string Code,
    string Name,
    string State,
    bool SystemEnabled,
    bool RegistrationEnabled,
    string PolicyRevision,
    string ETag,
    IReadOnlyList<string> RequiredDependencies,
    IReadOnlyList<string> RequiredBy,
    string? UnavailableReason,
    bool SharingEnabled = false);

public sealed record ModulePolicyChangeRequest(bool? SystemEnabled, bool? RegistrationEnabled, bool? SharingEnabled = null);

public sealed record ModulePolicyCommitRequest(
    bool? SystemEnabled,
    bool? RegistrationEnabled,
    string PreviewToken,
    bool? SharingEnabled = null);

public sealed record ModulePolicyPreviewResponse(
    string PreviewToken,
    DateTimeOffset ExpiresAt,
    string ETag,
    IReadOnlyList<ModuleChangeDiff> Changes,
    IReadOnlyList<ModulePolicyBlocker> Blockers,
    long AffectedSharingLinks = 0,
    long AffectedUsers = 0);

public sealed record ModuleChangeDiff(string Field, string Before, string After);

public sealed record ModulePolicyBlocker(string Code, string Message, string? Field);
