using Nexora.Application.Identity;

namespace Nexora.Application.DeveloperTools;

public sealed record ToolboxTool(
    string Code,
    string Name,
    string Category,
    string Description,
    string ExecutionMode,
    string ActionKey,
    bool AcceptsOptions);

public sealed record ToolboxCatalog(IReadOnlyList<ToolboxTool> Items);

public sealed record ToolboxRunCommand(
    string ToolCode,
    string Input,
    IReadOnlyDictionary<string, string>? Options = null);

public sealed record ToolboxRunResult(
    string ToolCode,
    string Output,
    string? Warning,
    string? ErrorPath,
    int DurationMilliseconds);

public interface IToolboxService
{
    IdentityOperationResult<ToolboxCatalog> Catalog(IdentityPrincipal actor);

    IdentityOperationResult<ToolboxRunResult> Run(
        IdentityPrincipal actor,
        ToolboxRunCommand command);
}
