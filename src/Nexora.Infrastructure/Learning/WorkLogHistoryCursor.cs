using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexora.Infrastructure.Learning;

internal sealed class WorkLogHistoryCursor(string secret)
{
    private readonly byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes("nexora/worklog/history/v1\0" + secret));
    internal sealed record Scope(Guid Actor, Guid Owner, Guid WorkLog);

    internal string Encode(Scope scope, long sequence)
    {
        var plain = new byte[8]; BinaryPrimitives.WriteInt64BigEndian(plain, sequence);
        var envelope = new byte[36]; RandomNumberGenerator.Fill(envelope.AsSpan(0,12));
        using var aes = new AesGcm(key,16);
        aes.Encrypt(envelope.AsSpan(0,12),plain,envelope.AsSpan(28),envelope.AsSpan(12,16),JsonSerializer.SerializeToUtf8Bytes(scope));
        return "wh1." + Convert.ToBase64String(envelope).Replace('+','-').Replace('/','_');
    }

    internal long? Decode(Scope scope, string token)
    {
        if (token.Length != 52 || !token.StartsWith("wh1.",StringComparison.Ordinal)) return null;
        try
        {
            var envelope = Convert.FromBase64String(token[4..].Replace('-','+').Replace('_','/'));
            if (envelope.Length != 36 || Convert.ToBase64String(envelope).Replace('+','-').Replace('/','_') != token[4..]) return null;
            var plain = new byte[8]; using var aes = new AesGcm(key,16);
            aes.Decrypt(envelope.AsSpan(0,12),envelope.AsSpan(28),envelope.AsSpan(12,16),plain,JsonSerializer.SerializeToUtf8Bytes(scope));
            var sequence = BinaryPrimitives.ReadInt64BigEndian(plain);
            return sequence > 0 ? sequence : null;
        }
        catch (Exception error) when (error is FormatException or CryptographicException) { return null; }
    }
}
