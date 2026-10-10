using System.Security.Cryptography;
using System.Text;

namespace Nexora.Application.Security;

public static class IdempotencyDigest
{
    public static byte[] ComputeKeyHash(ReadOnlySpan<byte> secret, string idempotencyKey)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            throw new FormatException("Idempotency-Key must be a UUID.");
        }

        return Hmac(secret, idempotencyKey);
    }

    public static byte[] ComputeRequestDigest(ReadOnlySpan<byte> secret, string canonicalRequestWithoutSecrets)
    {
        if (string.IsNullOrWhiteSpace(canonicalRequestWithoutSecrets))
        {
            throw new ArgumentException("Canonical request digest input is required.", nameof(canonicalRequestWithoutSecrets));
        }

        return Hmac(secret, canonicalRequestWithoutSecrets);
    }

    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        CryptographicOperations.FixedTimeEquals(left, right);

    private static byte[] Hmac(ReadOnlySpan<byte> secret, string input)
    {
        var key = secret.ToArray();
        if (key.Length < 32)
        {
            throw new ArgumentException("Digest secret must contain at least 256 bits.", nameof(secret));
        }

        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
    }
}
