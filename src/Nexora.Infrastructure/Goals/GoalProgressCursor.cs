using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Goals;

internal sealed class GoalProgressCursor(string secret, string purpose = "progress-history")
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/goals/" + purpose + "/v1\0" + secret));
    internal sealed record Context(Guid User, Guid Owner, Guid Goal, int Limit = 25);
    internal sealed record Boundary(DateTime RecordedAt, Guid Id);

    internal string Encode(Context context, Boundary boundary)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(boundary);
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), JsonSerializer.SerializeToUtf8Bytes(context));
        return "gp1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal Boundary? Decode(Context context, string token)
    {
        if (token.Length is < 43 or > 1024 || !token.StartsWith("gp1.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return null;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, JsonSerializer.SerializeToUtf8Bytes(context));
            var boundary = JsonSerializer.Deserialize<Boundary>(plain);
            return boundary is { Id: var id, RecordedAt: var at } && id != Guid.Empty && at != default ? boundary : null;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return null; }
    }
}
