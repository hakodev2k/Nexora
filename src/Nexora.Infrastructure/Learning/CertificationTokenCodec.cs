using System.Security.Cryptography;
using System.Text;

namespace Nexora.Infrastructure.Learning;

/// <summary>Private, authenticated cursor/preview envelope bound to an exact server scope.</summary>
public sealed class CertificationTokenCodec(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/certification/v1\0" + secret));
    public string Protect(string scope, string value)
    {
        var plain = Encoding.UTF8.GetBytes(value); var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Encoding.UTF8.GetBytes(scope));
        return "cert1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    public string? Unprotect(string scope, string token)
    {
        if (token.Length is < 45 or > 8192 || !token.StartsWith("cert1.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[6..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return null;
            var plain = new byte[envelope.Length - 28]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Encoding.UTF8.GetBytes(scope));
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception error) when (error is FormatException or CryptographicException) { return null; }
    }
}
