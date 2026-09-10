namespace Nexora.Api.Features.DeveloperTools;

public sealed record ToolboxRunRequest(
    string ToolCode,
    string Input,
    Dictionary<string, string>? Options);

public sealed record ToolboxToolResponse(
    string Code,
    string Name,
    string Category,
    string Description,
    string ExecutionMode,
    string ActionKey,
    bool AcceptsOptions);

public sealed record ToolboxCatalogResponse(IReadOnlyList<ToolboxToolResponse> Items);

public sealed record ToolboxRunResponse(
    string ToolCode,
    string Output,
    string? Warning,
    string? ErrorPath,
    int DurationMilliseconds);
