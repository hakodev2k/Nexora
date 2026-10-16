using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Finance;

// The established AES-GCM envelope authenticates owner, resource kind,
// canonical filters and effective limit; positions never expose private text.
internal sealed class FinanceListCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/finance/list/key/v1\0" + secret));
    private static byte[] Context(Guid owner, string kind, string filters, int limit) =>
        JsonSerializer.SerializeToUtf8Bytes(new { version = 1, owner, kind, filters, limit });
    public string Encode(Guid owner, string kind, string filters, int limit, string sortKey, Guid id)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(new Position(sortKey, id));
        var envelope = new byte[28 + plain.Length]; RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Context(owner, kind, filters, limit));
        return "fl1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    public bool TryDecode(string token, Guid owner, string kind, string filters, int limit, out string? sortKey, out Guid? id)
    {
        sortKey = null; id = null;
        if (token.Length is < 44 or > 1024 || !token.StartsWith("fl1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plain = new byte[envelope.Length - 28]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Context(owner, kind, filters, limit));
            var position = JsonSerializer.Deserialize<Position>(plain);
            if (position is null || position.Id == Guid.Empty || position.SortKey is null || position.SortKey.Length is < 1 or > 100) return false;
            sortKey = position.SortKey; id = position.Id; return true;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return false; }
    }
    private sealed record Position(string SortKey, Guid Id);
}
