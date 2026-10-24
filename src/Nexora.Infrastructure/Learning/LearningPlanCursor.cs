using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Nexora.Infrastructure.Learning;

internal sealed class LearningPlanCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/learning-plan/seek/v1\0" + secret));
    private readonly byte[] lifecycleKey = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/learning-plan/lifecycle-preview/v1\0" + secret));
    internal sealed record Scope(Guid Actor, Guid Owner, string Action, string Filter);
    internal sealed record Boundary(DateTime UpdatedAt, Guid Id);
    internal sealed record LifecycleScope(Guid Actor, Guid Owner, Guid Plan, string Action);
    internal sealed record LifecycleProof(string ETag, string FromState, string ToState, int RetainedItems, DateTimeOffset ExpiresAt);
    internal string EncodeLifecycle(LifecycleScope scope, LifecycleProof value)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(value); var buffer = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(buffer.AsSpan(0, 12));
        using var aes = new AesGcm(lifecycleKey, 16);
        aes.Encrypt(buffer.AsSpan(0, 12), plain, buffer.AsSpan(28), buffer.AsSpan(12, 16), JsonSerializer.SerializeToUtf8Bytes(scope));
        return "lpl1." + Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    internal LifecycleProof? DecodeLifecycle(LifecycleScope scope, string? token)
    {
        if (token is null || token.Length is < 43 or > 2048 || !token.StartsWith("lpl1.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[5..];
            var buffer = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (buffer.Length < 29 || Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return null;
            using var aes = new AesGcm(lifecycleKey, 16); var plain = new byte[buffer.Length - 28];
            aes.Decrypt(buffer.AsSpan(0, 12), buffer.AsSpan(28), buffer.AsSpan(12, 16), plain, JsonSerializer.SerializeToUtf8Bytes(scope));
            return JsonSerializer.Deserialize<LifecycleProof>(plain);
        }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException) { return null; }
    }
    internal string Encode(Scope scope, Boundary value)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(value); var buffer = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(buffer.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(buffer.AsSpan(0, 12), plain, buffer.AsSpan(28), buffer.AsSpan(12, 16), JsonSerializer.SerializeToUtf8Bytes(scope));
        return "lp1." + Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    internal Boundary? Decode(Scope scope, string token)
    {
        if (token.Length is < 43 or > 1024 || !token.StartsWith("lp1.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[4..];
            var buffer = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4));
            if (buffer.Length < 29 || Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return null;
            using var aes = new AesGcm(key, 16); var plain = new byte[buffer.Length - 28];
            aes.Decrypt(buffer.AsSpan(0, 12), buffer.AsSpan(28), buffer.AsSpan(12, 16), plain, JsonSerializer.SerializeToUtf8Bytes(scope));
            var value = JsonSerializer.Deserialize<Boundary>(plain);
            return value is { Id: var id } && id != Guid.Empty ? value : null;
        }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException) { return null; }
    }
}
