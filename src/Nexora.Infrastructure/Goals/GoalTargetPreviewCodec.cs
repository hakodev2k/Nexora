using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Goals;

internal sealed class GoalTargetPreviewCodec(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/goals/target-change/v1\0" + secret));
    internal sealed record Context(Guid User, Guid Owner, Guid Goal, Guid Target, string Mode);
    internal sealed record Payload(string GoalETag, string TargetETag, string Kind, string? Title,
        decimal? InitialValue, decimal? TargetValue, DateTimeOffset ExpiresAt);

    internal string Encode(Context context, Payload value)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(value);
        var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), JsonSerializer.SerializeToUtf8Bytes(context));
        return "gt1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal Payload? Decode(Context context, string token)
    {
        if (token.Length is < 43 or > 4096 || !token.StartsWith("gt1.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (envelope.Length < 29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return null;
            var plain = new byte[envelope.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, JsonSerializer.SerializeToUtf8Bytes(context));
            return JsonSerializer.Deserialize<Payload>(plain);
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return null; }
    }
}
