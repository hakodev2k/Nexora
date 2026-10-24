using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Nexora.Infrastructure.Automation;

internal sealed class AutomationDefinitionCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/automation/definitions/v1\0" + secret));
    private static byte[] Context(Guid owner, string query) => Encoding.UTF8.GetBytes($"automation-definition-page-v1|{owner:N}|{query}");
    public string Encode(Guid owner, string query, DateTime at, Guid id)
    {
        var plain = new byte[32]; BinaryPrimitives.WriteInt64LittleEndian(plain, at.Ticks);
        id.TryWriteBytes(plain.AsSpan(8)); BinaryPrimitives.WriteInt64LittleEndian(plain.AsSpan(24), DateTime.UtcNow.AddHours(24).Ticks);
        var envelope = new byte[60]; RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16), Context(owner, query));
        return Convert.ToBase64String(envelope);
    }
    public bool TryDecode(string token, Guid owner, string query, out DateTime at, out Guid id)
    {
        at = default; id = default; if (token.Length != 80) return false;
        try
        {
            var envelope = Convert.FromBase64String(token); if (envelope.Length != 60 || Convert.ToBase64String(envelope) != token) return false;
            var plain = new byte[32]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, Context(owner, query));
            at = new(BinaryPrimitives.ReadInt64LittleEndian(plain), DateTimeKind.Utc); id = new(plain.AsSpan(8, 16));
            return id != Guid.Empty && BinaryPrimitives.ReadInt64LittleEndian(plain.AsSpan(24)) > DateTime.UtcNow.Ticks;
        }
        catch (Exception e) when (e is FormatException or CryptographicException or ArgumentOutOfRangeException) { return false; }
    }
}
