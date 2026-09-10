using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// Durable request replay guard backed by identity.RequestReceipt. The
/// reservation is written in the same transaction as the business mutation;
/// a rolled-back mutation therefore does not leave a false success receipt.
/// Raw request secrets are never stored. Successful responses are stored only
/// when a caller supplies an explicitly safe projection, allowing a replay to
/// return the same status/body without exposing arbitrary request data.
/// </summary>
internal sealed class SqlRequestReceiptStore
{
    private static readonly TimeSpan ReceiptTtl = TimeSpan.FromHours(24);
    private readonly byte[] _secret;

    public SqlRequestReceiptStore(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("An idempotency digest secret is required.", nameof(secret));
        }

        _secret = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public ReceiptClaim TryClaim(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid? subjectId,
        string operationKey,
        string idempotencyKey,
        string canonicalRequest,
        DateTime now)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            return ReceiptClaim.Invalid;
        }

        var subjectHash = Digest(subjectId is { } id
            ? $"user:{id:N}:{operationKey}"
            : $"anonymous:{operationKey}");
        var keyHash = KeyDigest(idempotencyKey);
        var requestDigest = Digest(canonicalRequest);
        var expiresAt = now.Add(ReceiptTtl);

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO [identity].[RequestReceipt]
                ([SubjectHash], [OperationKey], [KeyHash], [RequestDigest], [State], [CreatedAt], [ExpiresAt])
            VALUES
                (@SubjectHash, @OperationKey, @KeyHash, @RequestDigest, 'Running', @CreatedAt, @ExpiresAt);
            """;
        Add(insert, "@SubjectHash", SqlDbType.Binary, subjectHash, 32);
        Add(insert, "@OperationKey", SqlDbType.NVarChar, operationKey, 150);
        Add(insert, "@KeyHash", SqlDbType.Binary, keyHash, 32);
        Add(insert, "@RequestDigest", SqlDbType.Binary, requestDigest, 32);
        Add(insert, "@CreatedAt", SqlDbType.DateTime2, now);
        Add(insert, "@ExpiresAt", SqlDbType.DateTime2, expiresAt);

        try
        {
            insert.ExecuteNonQuery();
            return new ReceiptClaim(true, false, false, subjectHash, operationKey, keyHash);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            using var existing = connection.CreateCommand();
            existing.Transaction = transaction;
            existing.CommandText = """
                SELECT TOP (1) [RequestDigest], [State], [ExpiresAt],
                       [ResultCode], [ResultStatusCode], [ResultJson]
                FROM [identity].[RequestReceipt] WITH (UPDLOCK, ROWLOCK)
                WHERE [SubjectHash] = @SubjectHash
                  AND [OperationKey] = @OperationKey
                  AND [KeyHash] = @KeyHash;
                """;
            Add(existing, "@SubjectHash", SqlDbType.Binary, subjectHash, 32);
            Add(existing, "@OperationKey", SqlDbType.NVarChar, operationKey, 150);
            Add(existing, "@KeyHash", SqlDbType.Binary, keyHash, 32);
            using var reader = existing.ExecuteReader();
            if (!reader.Read())
            {
                return ReceiptClaim.InProgress;
            }

            var storedDigest = reader.GetFieldValue<byte[]>(0);
            var state = reader.GetString(1);
            var storedExpiresAt = reader.GetDateTime(2);
            if (storedExpiresAt <= now)
            {
                reader.Close();
                using var reclaim = connection.CreateCommand();
                reclaim.Transaction = transaction;
                reclaim.CommandText = """
                    UPDATE [identity].[RequestReceipt]
                    SET [RequestDigest] = @RequestDigest, [State] = 'Running',
                        [ResultCode] = NULL, [ResultStatusCode] = NULL, [ResultJson] = NULL,
                        [CreatedAt] = @CreatedAt, [ExpiresAt] = @ExpiresAt
                    WHERE [SubjectHash] = @SubjectHash
                      AND [OperationKey] = @OperationKey
                      AND [KeyHash] = @KeyHash;
                    """;
                Add(reclaim, "@RequestDigest", SqlDbType.Binary, requestDigest, 32);
                Add(reclaim, "@CreatedAt", SqlDbType.DateTime2, now);
                Add(reclaim, "@ExpiresAt", SqlDbType.DateTime2, expiresAt);
                Add(reclaim, "@SubjectHash", SqlDbType.Binary, subjectHash, 32);
                Add(reclaim, "@OperationKey", SqlDbType.NVarChar, operationKey, 150);
                Add(reclaim, "@KeyHash", SqlDbType.Binary, keyHash, 32);
                reclaim.ExecuteNonQuery();
                return new ReceiptClaim(true, false, false, subjectHash, operationKey, keyHash);
            }

            if (!CryptographicOperations.FixedTimeEquals(storedDigest, requestDigest))
            {
                return ReceiptClaim.Conflict;
            }

            if (state != "Succeeded")
                return ReceiptClaim.InProgress;

            return new ReceiptClaim(
                false,
                true,
                false,
                subjectHash,
                operationKey,
                keyHash,
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetString(5));
        }
    }

    public void Complete(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim,
        string resultCode, int? resultStatusCode = null, string? resultJson = null)
    {
        if (!claim.IsClaimed)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE [identity].[RequestReceipt]
            SET [State] = 'Succeeded', [ResultCode] = @ResultCode,
                [ResultStatusCode] = @ResultStatusCode, [ResultJson] = @ResultJson
            WHERE [SubjectHash] = @SubjectHash
              AND [OperationKey] = @OperationKey
              AND [KeyHash] = @KeyHash
              AND [State] = 'Running';
            """;
        Add(command, "@ResultCode", SqlDbType.NVarChar, resultCode, 64);
        command.Parameters.Add("@ResultStatusCode", SqlDbType.Int).Value = (object?)resultStatusCode ?? DBNull.Value;
        Add(command, "@ResultJson", SqlDbType.NVarChar, (object?)resultJson ?? DBNull.Value, -1);
        Add(command, "@SubjectHash", SqlDbType.Binary, claim.SubjectHash, 32);
        Add(command, "@OperationKey", SqlDbType.NVarChar, claim.OperationKey, 150);
        Add(command, "@KeyHash", SqlDbType.Binary, claim.KeyHash, 32);
        command.ExecuteNonQuery();
    }

    private byte[] KeyDigest(string key) => Hmac($"key:{key}");
    private byte[] Digest(string value) => Hmac(value);

    private byte[] Hmac(string value)
    {
        using var hmac = new HMACSHA256(_secret);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is { } explicitSize
            ? command.Parameters.Add(name, type, explicitSize)
            : command.Parameters.Add(name, type);
        parameter.Value = value;
    }
}

internal readonly record struct ReceiptClaim(
    bool IsClaimed,
    bool IsReplay,
    bool IsConflict,
    byte[] SubjectHash,
    string OperationKey,
    byte[] KeyHash,
    string? ResultCode = null,
    int? ResultStatusCode = null,
    string? ResultJson = null,
    bool IsInvalid = false)
{
    public static ReceiptClaim Invalid => new(false, false, true, Array.Empty<byte>(), string.Empty, Array.Empty<byte>(), IsInvalid: true);
    public static ReceiptClaim Conflict => new(false, false, true, Array.Empty<byte>(), string.Empty, Array.Empty<byte>());
    public static ReceiptClaim InProgress => new(false, false, false, Array.Empty<byte>(), string.Empty, Array.Empty<byte>());
}
