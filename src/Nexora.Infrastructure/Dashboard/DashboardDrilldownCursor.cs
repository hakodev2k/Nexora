using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Dashboard;

internal sealed record DashboardDrilldownScope(int Version, Guid UserId, Guid OwnerId, string Key,
    DateTimeOffset CapturedAt, string TimeZoneId, bool IncludeTaskEvents, int Offset);

internal sealed class DashboardDrilldownCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    private byte[] Sign(string payload) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes("dashboard.drilldown.v1:" + payload));
    internal string Encode(DashboardDrilldownScope value)
    {
        var payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value));
        return payload + "." + Convert.ToBase64String(Sign(payload));
    }
    internal DashboardDrilldownScope? Decode(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4096) return null;
        var parts = token.Split('.');
        if (parts.Length != 2) return null;
        try
        {
            var signature = Convert.FromBase64String(parts[1]);
            if (signature.Length != 32 || !CryptographicOperations.FixedTimeEquals(signature, Sign(parts[0]))) return null;
            var value = JsonSerializer.Deserialize<DashboardDrilldownScope>(Convert.FromBase64String(parts[0]));
            return value is { Version: 1, Offset: >= 0 and <= int.MaxValue - 25 }
                && value.Key is "tasks-due" or "calendar-today" or "documents-recent" or "notifications-unread"
                && !string.IsNullOrEmpty(value.TimeZoneId) && value.TimeZoneId.Length <= 100 ? value : null;
        }
        catch (Exception error) when (error is FormatException or JsonException) { return null; }
    }
}
