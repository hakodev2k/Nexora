namespace Nexora.Domain.Automation;

/// <summary>A saved definition is configuration, never proof of execution readiness.</summary>
public static class AutomationDefinition
{
    public static bool TryTitle(string? input, out string title)
    {
        title = input?.Trim() ?? string.Empty;
        if (title.Length is < 1 or > 200 || title.Any(char.IsControl)) return false;
        for (var i = 0; i < title.Length; i++)
        {
            if (char.IsHighSurrogate(title[i]))
            { if (++i >= title.Length || !char.IsLowSurrogate(title[i])) return false; }
            else if (char.IsLowSurrogate(title[i])) return false;
        }
        return true;
    }

    public static bool CanSave(string state, bool enabled) => state is "Draft" or "Disabled" && !enabled;
}
