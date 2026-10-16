using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Documents;

internal sealed class DocumentHistoryCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/documents/history/key/v1\0" + secret));
    internal sealed record Context(Guid Owner, Guid Page, int Limit);

    public string Encode(Context context, long version)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(version);
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), JsonSerializer.SerializeToUtf8Bytes(context));
        return "dh1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Context context, out long version)
    {
        version = 0;
        if (token.Length is < 43 or > 256 || !token.StartsWith("dh1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, JsonSerializer.SerializeToUtf8Bytes(context));
            version = JsonSerializer.Deserialize<long>(plain);
            return version > 0;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return false; }
    }
}
