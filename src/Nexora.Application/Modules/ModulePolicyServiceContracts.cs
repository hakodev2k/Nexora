using Nexora.Application.Identity;

namespace Nexora.Application.Modules;

public sealed record ModulePolicyRecord(
    Guid Id,
    string Code,
    string Name,
    string State,
    bool SystemEnabled,
    bool RegistrationEnabled,
    long PolicyRevision,
    string ETag,
    IReadOnlyList<string> RequiredDependencies,
    IReadOnlyList<string> RequiredBy,
    string? UnavailableReason);

public sealed record ModulePolicyPage(IReadOnlyList<ModulePolicyRecord> Items, string? NextCursor);

public sealed record ModulePolicyChange(bool? SystemEnabled, bool? RegistrationEnabled);

public sealed record ModulePolicyDiff(string Field, string Before, string After);

public sealed record ModulePolicyBlocker(string Code, string Message, string? Field);

public sealed record ModulePolicyPreview(
    string PreviewToken,
    DateTimeOffset ExpiresAt,
    string ETag,
    IReadOnlyList<ModulePolicyDiff> Changes,
    IReadOnlyList<ModulePolicyBlocker> Blockers);

public sealed record ModulePolicyCommit(ModulePolicyChange Change, string PreviewToken);

public interface IModulePolicyService
{
    IdentityOperationResult<ModulePolicyPage> List(int? limit, IdentityPrincipal actor);
    IdentityOperationResult<ModulePolicyPreview> Preview(Guid moduleId, ModulePolicyChange change, IdentityPrincipal actor);
    IdentityOperationResult<ModulePolicyRecord> Commit(Guid moduleId, string? ifMatch, ModulePolicyCommit command, IdentityPrincipal actor, string? idempotencyKey = null, string? traceId = null);
}
