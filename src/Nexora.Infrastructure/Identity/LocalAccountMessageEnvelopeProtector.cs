using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// Protects the short-lived local account-message delivery envelope. SQL gets
/// only authenticated ciphertext; the local worker is the only code path that
/// turns it back into a transport message. The key is supplied by local
/// configuration and is deliberately separate from the idempotency secret.
/// </summary>
public sealed class LocalAccountMessageEnvelopeProtector
{
    private const byte CurrentVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MaxHeaderSize = 1024;
    private const int MaxEnvelopeSize = 128 * 1024;
    private readonly byte[] _key;

    public LocalAccountMessageEnvelopeProtector(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new ArgumentException("A local delivery encryption key is required.", nameof(base64Key));
        }

        try
        {
            var normalized = base64Key.Trim().Replace('-', '+').Replace('_', '/');
            normalized += new string('=', (4 - normalized.Length % 4) % 4);
            _key = Convert.FromBase64String(normalized);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The local delivery encryption key must be base64 encoded.", nameof(base64Key), exception);
        }

        if (_key.Length != 32)
        {
            throw new ArgumentException("The local delivery encryption key must decode to 32 bytes.", nameof(base64Key));
        }
    }

    /// <summary>
    /// Creates a process-compatible key from a configured secret with domain
    /// separation. This fallback keeps command-line bootstrap construction
    /// usable; the API must provide NEXORA_LOCAL_MESSAGE_KEY explicitly so a
    /// restart can recover pending delivery envelopes.
    /// </summary>
    public static LocalAccountMessageEnvelopeProtector FromSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("Nexora.LocalAccountMessageEnvelope:" + secret));
        return new LocalAccountMessageEnvelopeProtector(Convert.ToBase64String(key));
    }

    public byte[] Protect(LocalAccountMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var header = JsonSerializer.SerializeToUtf8Bytes(new EnvelopeHeader(
            message.Id, message.UserId, message.Purpose, message.ExpiresAt.UtcDateTime.Ticks));
        if (header.Length > MaxHeaderSize)
        {
            throw new InvalidOperationException("The local delivery envelope header is too large.");
        }

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(message);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];
        using (var aes = new AesGcm(_key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData(header));
        }

        var envelope = new byte[1 + sizeof(int) + header.Length + NonceSize + TagSize + ciphertext.Length];
        envelope[0] = CurrentVersion;
        BinaryPrimitives.WriteInt32LittleEndian(envelope.AsSpan(1, sizeof(int)), header.Length);
        header.CopyTo(envelope.AsSpan(1 + sizeof(int)));
        var offset = 1 + sizeof(int) + header.Length;
        nonce.CopyTo(envelope.AsSpan(offset));
        offset += NonceSize;
        tag.CopyTo(envelope.AsSpan(offset));
        offset += TagSize;
        ciphertext.CopyTo(envelope.AsSpan(offset));
        return envelope;
    }

    public LocalAccountMessage Unprotect(byte[] envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Length < 1 + sizeof(int) + NonceSize + TagSize || envelope.Length > MaxEnvelopeSize ||
            envelope[0] != CurrentVersion)
        {
            throw new InvalidDataException("The local delivery envelope is invalid.");
        }

        var headerLength = BinaryPrimitives.ReadInt32LittleEndian(envelope.AsSpan(1, sizeof(int)));
        var headerOffset = 1 + sizeof(int);
        if (headerLength is <= 0 or > MaxHeaderSize || headerOffset + headerLength + NonceSize + TagSize > envelope.Length)
        {
            throw new InvalidDataException("The local delivery envelope header is invalid.");
        }

        EnvelopeHeader header;
        try
        {
            header = JsonSerializer.Deserialize<EnvelopeHeader>(envelope.AsSpan(headerOffset, headerLength))
                ?? throw new InvalidDataException("The local delivery envelope header is missing.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The local delivery envelope header is invalid.", exception);
        }

        var offset = headerOffset + headerLength;
        var nonce = envelope.AsSpan(offset, NonceSize);
        offset += NonceSize;
        var tag = envelope.AsSpan(offset, TagSize);
        offset += TagSize;
        var ciphertext = envelope.AsSpan(offset);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData(envelope.AsSpan(headerOffset, headerLength)));
            var message = JsonSerializer.Deserialize<LocalAccountMessage>(plaintext)
                ?? throw new InvalidDataException("The local delivery envelope message is missing.");
            if (message.Id != header.Id || message.UserId != header.UserId ||
                !string.Equals(message.Purpose, header.Purpose, StringComparison.Ordinal) ||
                message.ExpiresAt.UtcDateTime.Ticks != header.ExpiresAtTicks)
            {
                throw new InvalidDataException("The local delivery envelope binding is invalid.");
            }

            return message;
        }
        catch (CryptographicException exception)
        {
            throw new InvalidDataException("The local delivery envelope could not be authenticated.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The local delivery envelope message is invalid.", exception);
        }
    }

    private static byte[] AssociatedData(ReadOnlySpan<byte> header) =>
        SHA256.HashData(header.ToArray());

    private sealed record EnvelopeHeader(Guid Id, Guid? UserId, string Purpose, long ExpiresAtTicks);
}
