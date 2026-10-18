using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Reading;

internal sealed class ReadingListCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/reading/list/key/v1\0" + secret));
    private static byte[] Context(Guid owner, string? state, int limit, string? sourceType) => sourceType is null
        ? JsonSerializer.SerializeToUtf8Bytes(new { version = 1, owner, state, limit })
        : JsonSerializer.SerializeToUtf8Bytes(new { version = 1, owner, state, limit, sourceType });

    public string Encode(Guid owner, string? state, int limit, DateTimeOffset savedAt, Guid id, string? sourceType = null)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(new Position(savedAt.UtcDateTime, id));
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Context(owner, state, limit, sourceType));
        return "rl1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Guid owner, string? state, int limit,
        out DateTime? savedAt, out Guid? id, string? sourceType = null)
    {
        savedAt = null; id = null;
        if (token.Length is < 44 or > 1024 || !token.StartsWith("rl1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Context(owner, state, limit, sourceType));
            var position = JsonSerializer.Deserialize<Position>(plain);
            if (position is null || position.Id == Guid.Empty || position.SavedAt.Kind != DateTimeKind.Utc) return false;
            savedAt = position.SavedAt; id = position.Id;
            return true;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return false; }
    }

    private sealed record Position(DateTime SavedAt, Guid Id);
}
