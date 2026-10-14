using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Discovery;

internal sealed class SearchCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/search/page/key/v1\0" + secret));
    internal sealed record Position(int Score, DateTime UpdatedAt, Guid Id, string ResourceType);
    internal sealed record Context(Guid Owner, string Query, string? ResourceType, DateOnly? From,
        DateOnly? To, bool IncludeArchived, int Limit);

    public string Encode(Context context, Position position)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(position);
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16),
            JsonSerializer.SerializeToUtf8Bytes(context));
        return "sg1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Context context, out Position? position)
    {
        position = null;
        if (token.Length is < 44 or > 2048 || !token.StartsWith("sg1.", StringComparison.Ordinal)) return false;
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
                JsonSerializer.SerializeToUtf8Bytes(context));
            var value = JsonSerializer.Deserialize<Position>(plain);
            if (value is null || value.Score is < 0 or > 2 || value.Id == Guid.Empty ||
                value.UpdatedAt.Kind != DateTimeKind.Utc ||
                value.ResourceType is not ("Project" or "Task" or "Event" or "Document" or "Bookmark" or "Snippet" or "Goal"))
                return false;
            position = value;
            return true;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return false; }
    }
}
