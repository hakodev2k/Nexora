using Nexora.Application.News;

namespace Nexora.Infrastructure.News;

internal static class NewsCategoryInput
{
    internal static bool TryName(string? value, bool optional, out string? normalized)
    {
        normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) { normalized = null; return optional; }
        if (normalized.Length > 100) return false;
        for (var index = 0; index < normalized.Length; index++)
        {
            var current = normalized[index];
            if (char.IsControl(current)) return false;
            if (char.IsHighSurrogate(current))
            {
                if (++index >= normalized.Length || !char.IsLowSurrogate(normalized[index])) return false;
            }
            else if (char.IsLowSurrogate(current)) return false;
        }
        return true;
    }
    internal static bool TryNormalize(NewsCategoryMetadata? value, out NewsCategoryMetadata normalized)
    {
        normalized = null!;
        if (value is null || value.SchemaVersion != 1 || !TryName(value.Name, false, out var name)) return false;
        normalized = new(1, name!); return true;
    }
    internal static bool ValidEtag(string value)
    {
        if (value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
        Span<byte> bytes = stackalloc byte[8];
        return Convert.TryFromBase64String(value[1..^1], bytes, out var count) && count == 8 && '"' + Convert.ToBase64String(bytes) + '"' == value;
    }
}
