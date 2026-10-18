using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Discovery;

internal sealed record RecentSeek(int Version, Guid UserId, Guid OwnerId, string? Type, DateTime LastOpenedAt, string Revision, Guid Id);
internal sealed record RecentClearScope(int Version, Guid UserId, Guid OwnerId, string Revision, DateTimeOffset ExpiresAt);
internal sealed class RecentTokens(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    private byte[] Sign(string purpose, string payload) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes("discovery.recents." + purpose + ".v1:" + payload));
    internal string Encode<T>(string purpose, T value)
    {
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value));
        return payload + "." + Convert.ToBase64String(Sign(purpose, payload));
    }
    internal T? Decode<T>(string purpose, string token) where T : class
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4096) return null;
        var parts = token.Split('.'); if (parts.Length != 2) return null;
        try
        {
            var signature = Convert.FromBase64String(parts[1]);
            if (signature.Length != 32 || !CryptographicOperations.FixedTimeEquals(signature, Sign(purpose, parts[0]))) return null;
            return JsonSerializer.Deserialize<T>(Convert.FromBase64String(parts[0]));
        }
        catch (Exception error) when (error is FormatException or JsonException) { return null; }
    }
    internal static byte[]? Revision(string? value)
    {
        try { var bytes = Convert.FromBase64String(value ?? ""); return bytes.Length == 8 && Convert.ToBase64String(bytes) == value ? bytes : null; }
        catch (FormatException) { return null; }
    }
}
