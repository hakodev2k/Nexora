using System.Globalization;
using System.Text;
using Nexora.Application.Integrations;
namespace Nexora.Infrastructure.Integrations;

internal static class IntegrationConnectionInput
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public static bool Normalize(IntegrationConnectionCommand? input, out IntegrationConnectionCommand normalized)
    {
        normalized = null!;
        if (input is null || input.SchemaVersion != 1 || !ConnectorRegistry.SupportsConfiguration(input.ProviderKey, input.ConnectorVersion)) return false;
        var title = input.Title?.Trim() ?? "";
        if (title.Length is < 1 or > 200 || title.Any(char.IsControl)) return false;
        try { StrictUtf8.GetByteCount(title); } catch (EncoderFallbackException) { return false; }
        if (!Origin(input.Endpoint, out var origin)) return false;
        normalized = input with { Title = title, Endpoint = origin }; return true;
    }
    // Configuration storage only. A saved name is not a DNS/egress approval; no network access here.
    private static bool Origin(string? input, out string origin)
    {
        origin = ""; var raw = input?.Trim() ?? "";
        if (raw.Length is < 1 or > 2048 || raw.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || raw.Contains('\\') ||
            !Uri.TryCreate(raw, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsDefaultPort ||
            uri.HostNameType != UriHostNameType.Dns || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0) return false;
        var hostStart = raw.IndexOf("://", StringComparison.Ordinal) + 3;
        var slash = raw.IndexOf('/', hostStart);
        if (slash >= 0 && slash != raw.Length - 1) return false; // Reject paths before Uri normalization can remove dot segments.
        var authority = raw[hostStart..].TrimEnd('/');
        if (authority.Contains('%') || authority.Contains('@') || authority.Contains('[') || authority.Contains(']')) return false;
        string host;
        try { host = new IdnMapping().GetAscii(uri.DnsSafeHost).ToLowerInvariant(); } catch (ArgumentException) { return false; }
        if (host.Length > 253 || !host.Contains('.') || host.EndsWith('.') || host.All(c => char.IsDigit(c) || c == '.') ||
            new[] { ".local", ".localhost", ".internal", ".lan", ".home", ".test", ".invalid" }.Any(x => host.EndsWith(x, StringComparison.Ordinal))) return false;
        if (host.Split('.').Any(x => x.Length is < 1 or > 63 || x[0] == '-' || x[^1] == '-' || x.Any(c => c is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))) return false;
        origin = "https://" + host + "/"; return true;
    }
}
