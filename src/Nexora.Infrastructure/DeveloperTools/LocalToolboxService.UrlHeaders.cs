namespace Nexora.Infrastructure.DeveloperTools;

public sealed partial class LocalToolboxService
{
    // Central Run must resolve current SQL SELF authority before this seam.
    // Actual URL/header text belongs only in browser memory.
    private static (string Output, string? Warning, string? ErrorPath) RunUrlHeadersAuthority(
        string code, string input, IReadOnlyDictionary<string, string>? options)
    {
        if (code is not ("url-parse" or "headers"))
            throw new ToolFailureException("ToolUnavailable", "Tool is unavailable.");
        if (input.Length != 0 || options is { Count: > 0 })
            throw new ToolFailureException("ValidationFailed", "Browser authority checks require empty input and options.");
        return (string.Empty, "Browser-only parsing; no fetch, navigation, credential verification or persistence.", null);
    }
}
