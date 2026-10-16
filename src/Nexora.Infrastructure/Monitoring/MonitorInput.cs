using System.Globalization;
using System.Text;
using Nexora.Application.Monitoring;

namespace Nexora.Infrastructure.Monitoring;

/// <summary>Lexical storage validation only. This class never resolves DNS or fetches a target.</summary>
public static class MonitorInput
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public static bool TryNormalize(MonitorMetadata? input, out MonitorMetadata normalized)
    {
        normalized = null!;
        if (input is null || input.SchemaVersion != 1 || input.Kind != "Http" || input.IntervalSeconds < 1 ||
            input.ExpectedStatus is < 100 or > 599 || !Text(input.Title, 200, out var title) || !Target(input.Target, out var target)) return false;
        normalized = input with { Title = title, Target = target }; return true;
    }
    private static bool Text(string? value, int maximum, out string text)
    {
        text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > maximum || text.Any(char.IsControl)) return false;
        try { StrictUtf8.GetByteCount(text); return true; } catch (EncoderFallbackException) { return false; }
    }
    public static bool Target(string? value, out string canonical)
    {
        canonical = "";
        if (!Text(value, 2048, out var raw) || raw.Contains('\\') || raw.Any(char.IsWhiteSpace) ||
            !Uri.TryCreate(raw, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") ||
            !uri.IsDefaultPort || uri.HostNameType != UriHostNameType.Dns || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return false;
        // Reject alternate/encoded authority spelling before trusting Uri's canonical host.
        var marker = raw.IndexOf("://", StringComparison.Ordinal);
        if (marker < 0) return false;
        var authorityEnd = raw.IndexOfAny(['/', '?', '#'], marker + 3);
        var authority = raw[(marker + 3)..(authorityEnd < 0 ? raw.Length : authorityEnd)];
        if (authority.Contains('%') || authority.Contains('@') || authority.Contains('[') || authority.Contains(']')) return false;
        string host;
        try { host = new IdnMapping().GetAscii(uri.DnsSafeHost).ToLowerInvariant(); } catch (ArgumentException) { return false; }
        if (host.Length > 253 || host.EndsWith('.') || !host.Contains('.') || host.All(ch => char.IsDigit(ch) || ch == '.') ||
            host == "localhost" || new[] { ".localhost", ".local", ".internal", ".lan", ".home", ".test", ".invalid" }.Any(host.EndsWith)) return false;
        var labels = host.Split('.');
        if (labels.Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
            label.Any(ch => ch is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))) return false;
        canonical = new UriBuilder(uri) { Host = host }.Uri.AbsoluteUri;
        return canonical.Length <= 2048;
    }
}
