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
        DateTime now,
        string? anonymousSessionBinding = null)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            return ReceiptClaim.Invalid;
        }

        if (subjectId is null && !IsValidAnonymousBinding(anonymousSessionBinding))
        {
            return ReceiptClaim.Invalid;
        }

        // Authenticated receipts used the operation-qualified namespace before
        // the current branch made SubjectHash operation-independent. Keep that
        // format as the write target and dual-read the current format through
        // the full 24-hour TTL. Anonymous receipts intentionally stay in their
        // signed-session namespace; the old shared anonymous namespace is not
        // reintroduced as a compatibility path.
        var subjectHashes = SubjectHashes(subjectId, operationKey, anonymousSessionBinding);
        var subjectHash = subjectHashes[0];
        var keyHash = KeyDigest(idempotencyKey);
        var requestDigest = Digest(canonicalRequest);
        var expiresAt = now.Add(ReceiptTtl);

        var existingReceipts = ReadExisting(connection, transaction, subjectHashes, operationKey, keyHash);
        if (existingReceipts.Count > 0)
        {
            return ResolveExisting(
                connection,
                transaction,
                existingReceipts,
                subjectHash,
                operationKey,
                keyHash,
                requestDigest,
                now,
                expiresAt);
        }

        try
        {
            Insert(connection, transaction, subjectHash, operationKey, keyHash, requestDigest, now, expiresAt);
            return new ReceiptClaim(true, false, false, subjectHash, operationKey, keyHash);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            // A concurrent claim may have won after the dual-read. Read both
            // namespaces again before deciding whether this request is a
            // replay, conflict or in-progress operation.
            existingReceipts = ReadExisting(connection, transaction, subjectHashes, operationKey, keyHash);
            return existingReceipts.Count == 0
                ? ReceiptClaim.InProgress
                : ResolveExisting(
                    connection,
                    transaction,
                    existingReceipts,
                    subjectHash,
                    operationKey,
                    keyHash,
                    requestDigest,
                    now,
                    expiresAt);
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

    /// <summary>
    /// Produces a keyed, non-reversible request component for a credential
    /// field. It lets same-key/different-password requests conflict without
    /// putting the password in the receipt canonical input or SQL.
    /// </summary>
    public string DigestSensitive(string value) => Convert.ToHexString(Hmac("sensitive:" + value));

    private void Insert(
        SqlConnection connection,
        SqlTransaction transaction,
        byte[] subjectHash,
        string operationKey,
        byte[] keyHash,
        byte[] requestDigest,
        DateTime now,
        DateTime expiresAt)
    {
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
        insert.ExecuteNonQuery();
    }

    private IReadOnlyList<StoredReceipt> ReadExisting(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<byte[]> subjectHashes,
        string operationKey,
        byte[] keyHash)
    {
        var parameters = new string[subjectHashes.Count];
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        for (var index = 0; index < subjectHashes.Count; index++)
        {
            parameters[index] = "@SubjectHash" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Add(command, parameters[index], SqlDbType.Binary, subjectHashes[index], 32);
        }

        command.CommandText = $"""
            SELECT [SubjectHash], [RequestDigest], [State], [ExpiresAt],
                   [ResultCode], [ResultStatusCode], [ResultJson]
            FROM [identity].[RequestReceipt] WITH (UPDLOCK, ROWLOCK)
            WHERE [SubjectHash] IN ({string.Join(", ", parameters)})
              AND [OperationKey] = @OperationKey
              AND [KeyHash] = @KeyHash
            ORDER BY CASE WHEN [SubjectHash] = {parameters[0]} THEN 0 ELSE 1 END;
            """;
        Add(command, "@OperationKey", SqlDbType.NVarChar, operationKey, 150);
        Add(command, "@KeyHash", SqlDbType.Binary, keyHash, 32);

        using var reader = command.ExecuteReader();
        var receipts = new List<StoredReceipt>();
        while (reader.Read())
        {
            receipts.Add(new StoredReceipt(
                reader.GetFieldValue<byte[]>(0),
                reader.GetFieldValue<byte[]>(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetInt32(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }

        return receipts;
    }

    private ReceiptClaim ResolveExisting(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<StoredReceipt> existingReceipts,
        byte[] primarySubjectHash,
        string operationKey,
        byte[] keyHash,
        byte[] requestDigest,
        DateTime now,
        DateTime expiresAt)
    {
        var active = existingReceipts.Where(receipt => receipt.ExpiresAt > now).ToArray();
        if (active.Length > 0)
        {
            if (active.Any(receipt => !CryptographicOperations.FixedTimeEquals(receipt.RequestDigest, requestDigest)))
            {
                return ReceiptClaim.Conflict;
            }

            var succeeded = active.FirstOrDefault(receipt => receipt.State == "Succeeded");
            if (succeeded is null)
            {
                return ReceiptClaim.InProgress;
            }

            return new ReceiptClaim(
                false,
                true,
                false,
                succeeded.SubjectHash,
                operationKey,
                keyHash,
                succeeded.ResultCode,
                succeeded.ResultStatusCode,
                succeeded.ResultJson);
        }

        // All rows are beyond the unchanged TTL. Reuse the operation-qualified
        // row when present so the next request writes the legacy-compatible
        // format; otherwise reclaim the current-format row in place.
        var reclaim = existingReceipts.FirstOrDefault(receipt =>
            CryptographicOperations.FixedTimeEquals(receipt.SubjectHash, primarySubjectHash))
            ?? existingReceipts[0];
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE [identity].[RequestReceipt]
            SET [RequestDigest] = @RequestDigest, [State] = 'Running',
                [ResultCode] = NULL, [ResultStatusCode] = NULL, [ResultJson] = NULL,
                [CreatedAt] = @CreatedAt, [ExpiresAt] = @ExpiresAt
            WHERE [SubjectHash] = @SubjectHash
              AND [OperationKey] = @OperationKey
              AND [KeyHash] = @KeyHash
              AND [ExpiresAt] <= @Now;
            """;
        Add(command, "@RequestDigest", SqlDbType.Binary, requestDigest, 32);
        Add(command, "@CreatedAt", SqlDbType.DateTime2, now);
        Add(command, "@ExpiresAt", SqlDbType.DateTime2, expiresAt);
        Add(command, "@SubjectHash", SqlDbType.Binary, reclaim.SubjectHash, 32);
        Add(command, "@OperationKey", SqlDbType.NVarChar, operationKey, 150);
        Add(command, "@KeyHash", SqlDbType.Binary, keyHash, 32);
        Add(command, "@Now", SqlDbType.DateTime2, now);
        return command.ExecuteNonQuery() == 1
            ? new ReceiptClaim(true, false, false, reclaim.SubjectHash, operationKey, keyHash)
            : ReceiptClaim.InProgress;
    }

    private byte[][] SubjectHashes(Guid? subjectId, string operationKey, string? anonymousSessionBinding) => subjectId is { } id
        ? [
            Digest($"user:{id:N}:{operationKey}"),
            Digest($"user:{id:N}")
        ]
        : [Digest($"anonymous-session:{anonymousSessionBinding}")];

    private static bool IsValidAnonymousBinding(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        var separator = value.IndexOf('.');
        if (separator != 43 || separator != value.LastIndexOf('.') || value.Length != 87)
        {
            return false;
        }

        return value[..separator].All(IsBase64UrlCharacter) &&
            value[(separator + 1)..].All(IsBase64UrlCharacter);
    }

    private static bool IsBase64UrlCharacter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '=';

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

    private sealed record StoredReceipt(
        byte[] SubjectHash,
        byte[] RequestDigest,
        string State,
        DateTime ExpiresAt,
        string? ResultCode,
        int? ResultStatusCode,
        string? ResultJson);
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
