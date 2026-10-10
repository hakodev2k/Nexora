using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Nexora.Infrastructure.Files;

internal sealed class FileListCursor
{
    private readonly byte[] key;
    public FileListCursor(string secret) => key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/files/list/key/v1\0" + secret));
    private static byte[] Context(Guid owner, string filters) => Encoding.UTF8.GetBytes($"nexora/files/list/v1|{owner:N}|{filters}");
    public string Encode(Guid owner, string filters, DateTime updatedAt, Guid id)
    {
        var plain = new byte[24]; BinaryPrimitives.WriteInt64LittleEndian(plain, updatedAt.Ticks); id.TryWriteBytes(plain.AsSpan(8));
        var envelope = new byte[52]; RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28, 24), envelope.AsSpan(12, 16), Context(owner, filters));
        return "fl1." + Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    public bool TryDecode(string token, Guid owner, string filters, out DateTime updatedAt, out Guid id)
    {
        updatedAt = default; id = default;
        if (token.Length != 74 || !token.StartsWith("fl1.", StringComparison.Ordinal)) return false;
        try
        {
            var encoded = token[4..]; var envelope = Convert.FromBase64String(encoded.Replace('-', '+').Replace('_', '/') + "==");
            if (envelope.Length != 52 || Convert.ToBase64String(envelope).TrimEnd('=').Replace('+', '-').Replace('/', '_') != encoded) return false;
            var plain = new byte[24]; using var aes = new AesGcm(key, 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28, 24), envelope.AsSpan(12, 16), plain, Context(owner, filters));
            updatedAt = new DateTime(BinaryPrimitives.ReadInt64LittleEndian(plain), DateTimeKind.Utc); id = new Guid(plain.AsSpan(8));
            return id != Guid.Empty;
        }
        catch (Exception error) when (error is FormatException or CryptographicException or ArgumentOutOfRangeException) { return false; }
    }
}
