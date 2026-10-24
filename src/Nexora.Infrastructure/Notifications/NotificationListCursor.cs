using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Notifications;

internal sealed class NotificationListCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/notifications/cursor/v1\0" + secret));
    private sealed record Position(int Version, string Context, long CreatedTicks, Guid Id);
    private static string EncodeBytes(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public string Encode(string context, DateTimeOffset created, Guid id)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new Position(1, context, created.UtcTicks, id));
        return EncodeBytes(payload) + "." + EncodeBytes(HMACSHA256.HashData(key, payload));
    }
    public bool TryDecode(string value, string context, out DateTime? created, out Guid? id)
    {
        created = null; id = null;
        if (value.Length is < 20 or > 2048) return false;
        static byte[] Decode(string text) => Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/') + new string('=', (4 - text.Length % 4) % 4));
        try
        {
            var parts = value.Split('.'); if (parts.Length != 2) return false;
            var payload = Decode(parts[0]); var signature = Decode(parts[1]);
            if (EncodeBytes(payload) != parts[0] || EncodeBytes(signature) != parts[1]
                || !CryptographicOperations.FixedTimeEquals(signature, HMACSHA256.HashData(key, payload))) return false;
            var position = JsonSerializer.Deserialize<Position>(payload);
            if (position is null || position.Version != 1 || position.Context != context || position.Id == Guid.Empty
                || position.CreatedTicks < DateTime.MinValue.Ticks || position.CreatedTicks > DateTime.MaxValue.Ticks) return false;
            created = new DateTime(position.CreatedTicks, DateTimeKind.Utc); id = position.Id; return true;
        }
        catch (Exception error) when (error is FormatException or JsonException or ArgumentException) { return false; }
    }
}
