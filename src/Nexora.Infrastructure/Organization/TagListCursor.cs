using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Organization;

internal sealed class TagListCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/tags/list/key/v2\0" + secret));
    private static byte[] Context(Guid owner, string? tagNamespace, string? query, int limit, bool documentsAllowed) =>
        JsonSerializer.SerializeToUtf8Bytes(new { version = 2, owner, tagNamespace, query, limit, documentsAllowed });

    public string Encode(Guid owner, string? tagNamespace, string? query, int limit, bool documentsAllowed, string name, Guid id)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(new Position(name, id));
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16),
            Context(owner, tagNamespace, query, limit, documentsAllowed));
        return "tl2." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Guid owner, string? tagNamespace, string? query, int limit, bool documentsAllowed,
        out string? name, out Guid? id)
    {
        name = null; id = null;
        if (token.Length is < 44 or > 1024 || !token.StartsWith("tl2.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') +
                new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded)
                return false;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain,
                Context(owner, tagNamespace, query, limit, documentsAllowed));
            var position = JsonSerializer.Deserialize<Position>(plain);
            if (position is null || position.Name is null || position.Name.Length is < 1 or > 50 || position.Id == Guid.Empty)
                return false;
            name = position.Name; id = position.Id;
            return true;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException)
        {
            return false;
        }
    }

    private sealed record Position(string Name, Guid Id);
}
