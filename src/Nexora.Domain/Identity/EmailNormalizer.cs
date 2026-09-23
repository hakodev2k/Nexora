using System.Globalization;

namespace Nexora.Domain.Identity;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        if (email is null)
        {
            throw new ArgumentNullException(nameof(email));
        }

        var trimmed = email.Trim();
        var at = trimmed.LastIndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1 || trimmed.IndexOf('@') != at)
        {
            throw new FormatException("Email must contain exactly one local part and one domain.");
        }

        var localPart = trimmed[..at];
        var domainPart = trimmed[(at + 1)..];
        if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
        {
            throw new FormatException("Email local part and domain are required.");
        }

        var idn = new IdnMapping();
        var asciiDomain = idn.GetAscii(domainPart).ToLowerInvariant();
        return string.Concat(localPart.ToLowerInvariant(), "@", asciiDomain);
    }
}
