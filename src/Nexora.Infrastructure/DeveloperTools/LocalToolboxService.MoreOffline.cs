namespace Nexora.Infrastructure.DeveloperTools;

public sealed partial class LocalToolboxService
{
    // The normal Run method must perform SQL capability checks before this seam.
    // Text/color inputs stay in browser memory, like the JWT decoder.
    private static (string Output, string? Warning, string? ErrorPath) RunMoreOffline(
        string code, string input, IReadOnlyDictionary<string, string>? options)
    {
        if (code is not ("text-diff" or "color"))
            throw new ToolFailureException("ToolUnavailable", "Tool is unavailable.");
        if (input.Length != 0 || options is { Count: > 0 })
            throw new ToolFailureException("ValidationFailed", "Browser tool authority checks require empty input and options.");
        return (string.Empty, "Browser-only processing; input and output are not transmitted or persisted.", null);
    }
}
