using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Nexora.Infrastructure.Sharing;

/// <summary>Confidential continuation boundary, scoped to current owner, epoch and filters.</summary>
internal sealed class SharingListCursor
{
    private readonly byte[] key;
    public SharingListCursor(string secret) => key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/sharing/list/key/v1\0" + secret));

    public string Encode(Guid owner, long epoch, string filters, DateTime createdAt, Guid id)
    {
        var plaintext = new byte[24]; BinaryPrimitives.WriteInt64LittleEndian(plaintext, createdAt.Ticks); id.TryWriteBytes(plaintext.AsSpan(8));
        var envelope = new byte[52]; RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plaintext, envelope.AsSpan(28, 24), envelope.AsSpan(12, 16), Context(owner, epoch, filters));
        return "sl1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public bool TryDecode(string token, Guid owner, long epoch, string filters, out DateTime createdAt, out Guid id)
    {
        createdAt = default; id = default;
        if (token.Length != 74 || !token.StartsWith("sl1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..]; var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + "==");
            if (envelope.Length != 52 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plaintext = new byte[24]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28, 24), envelope.AsSpan(12, 16), plaintext, Context(owner, epoch, filters));
            createdAt = new DateTime(BinaryPrimitives.ReadInt64LittleEndian(plaintext), DateTimeKind.Utc); id = new Guid(plaintext.AsSpan(8));
            return id != Guid.Empty;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or ArgumentOutOfRangeException) { return false; }
    }

    private static byte[] Context(Guid owner, long epoch, string filters) => Encoding.UTF8.GetBytes($"nexora/sharing/list/v1|{owner:N}|{epoch}|{filters}");
}
