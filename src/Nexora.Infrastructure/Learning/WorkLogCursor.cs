using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Learning;

internal sealed class WorkLogCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/worklog/list/v2\0" + secret));
    internal sealed record Scope(Guid Actor, Guid Owner, string Status, string? Query, DateOnly? From, DateOnly? To, string? Category);
    internal sealed record Boundary(DateOnly RecordedOn, Guid Id);
    internal string Encode(Scope scope, Boundary value)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(value); var envelope = new byte[28 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0,12));
        using var aes = new AesGcm(key,16);
        aes.Encrypt(envelope.AsSpan(0,12), plain, envelope.AsSpan(28), envelope.AsSpan(12,16), JsonSerializer.SerializeToUtf8Bytes(scope));
        return "wl2." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+','-').Replace('/','_');
    }
    internal Boundary? Decode(Scope scope, string token)
    {
        if (token.Length is < 43 or > 1024 || !token.StartsWith("wl2.", StringComparison.Ordinal)) return null;
        try
        {
            var encoded = token[4..];
            var envelope = Convert.FromBase64String(encoded.Replace('-','+').Replace('_','/') + new string('=', (4-encoded.Length%4)%4));
            if (envelope.Length<29 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+','-').Replace('/','_')!=encoded) return null;
            using var aes = new AesGcm(key,16); var plain = new byte[envelope.Length-28];
            aes.Decrypt(envelope.AsSpan(0,12), envelope.AsSpan(28), envelope.AsSpan(12,16), plain, JsonSerializer.SerializeToUtf8Bytes(scope));
            var result = JsonSerializer.Deserialize<Boundary>(plain);
            return result is { Id: var id } && id!=Guid.Empty ? result : null;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or JsonException) { return null; }
    }
}
