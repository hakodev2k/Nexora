using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Snippets;

internal sealed class SnippetListCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/snippets/list/key/v1\0" + secret));
    private static byte[] Context(Guid owner, bool includeArchived, string? query, int limit) =>
        JsonSerializer.SerializeToUtf8Bytes(new { version = 1, owner, includeArchived, query, limit });

    public string Encode(Guid owner, bool includeArchived, string? query, int limit, DateTimeOffset updatedAt, Guid id)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(new Position(updatedAt.UtcDateTime, id));
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Context(owner, includeArchived, query, limit));
        return "sl1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Guid owner, bool includeArchived, string? query, int limit,
        out DateTime? updatedAt, out Guid? id)
    {
        updatedAt = null; id = null;
        if (token.Length is < 44 or > 1024 || !token.StartsWith("sl1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Context(owner, includeArchived, query, limit));
            var position = JsonSerializer.Deserialize<Position>(plain);
            if (position is null || position.Id == Guid.Empty || position.UpdatedAt.Kind != DateTimeKind.Utc) return false;
            updatedAt = position.UpdatedAt; id = position.Id;
            return true;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return false; }
    }

    private sealed record Position(DateTime UpdatedAt, Guid Id);
}
