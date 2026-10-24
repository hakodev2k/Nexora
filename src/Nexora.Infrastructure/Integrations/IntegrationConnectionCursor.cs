using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Nexora.Infrastructure.Integrations;

internal sealed class IntegrationConnectionCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/integrations/connections/v1\0" + secret));
    private sealed record Boundary(string Title, Guid Id, long Expires);
    private static byte[] Context(Guid owner, string filters) => Encoding.UTF8.GetBytes($"connection-page-v1|{owner:N}|{filters}");
    public string Encode(Guid owner, string filters, string title, Guid id)
    {
        var plain = JsonSerializer.SerializeToUtf8Bytes(new Boundary(title, id, DateTime.UtcNow.AddHours(24).Ticks));
        var envelope = new byte[28 + plain.Length]; RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Context(owner, filters));
        return Convert.ToBase64String(envelope);
    }
    public bool Decode(string? token, Guid owner, string filters, out string title, out Guid id)
    {
        title = ""; id = default;
        if (token is null || token.Length is < 40 or > 2400) return false;
        try
        {
            var envelope = Convert.FromBase64String(token); if (envelope.Length <= 28 || Convert.ToBase64String(envelope) != token) return false;
            var plain = new byte[envelope.Length - 28]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Context(owner, filters));
            var boundary = JsonSerializer.Deserialize<Boundary>(plain);
            if (boundary is null || boundary.Title.Length is < 1 or > 200 || boundary.Id == Guid.Empty || boundary.Expires <= DateTime.UtcNow.Ticks) return false;
            title = boundary.Title; id = boundary.Id; return true;
        }
        catch (Exception e) when (e is CryptographicException or JsonException or FormatException or ArgumentException) { return false; }
    }
}
