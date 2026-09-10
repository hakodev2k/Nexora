using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Domain.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// SQL Server identity implementation for the modular monolith. SQL is the
/// authority for users, sessions, one-time tokens, personal spaces, grants,
/// audit events and durable delivery intents. Raw session/token values only
/// live in the response or an explicitly supplied local-safe message sink.
/// </summary>
public sealed class SqlIdentityService : IIdentityService
{
    private static readonly TimeSpan VerificationTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResetTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ResendThrottle = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan IdleTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromHours(12);

    // Deliberately invalid, fixed-cost input for a missing user. It contains
    // no usable secret and prevents a fast path from disclosing account state.
    private const string DummyPasswordHash =
        "PBKDF2-HMAC-SHA512$220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly SqlConnectionFactory _connections;
    private readonly IAccountMessageSink _messageSink;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly Pbkdf2PasswordHasher _passwords = new();

    public SqlIdentityService(SqlConnectionFactory connections, IAccountMessageSink? messageSink = null, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _messageSink = messageSink ?? NullAccountMessageSink.Instance;
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<IdentityAccepted> Register(RegistrationCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        var validation = RegistrationCommandPolicy.Validate(command);
        if (!validation.IsValid || validation.Draft is null)
        {
            return Invalid<IdentityAccepted>(validation.Issues);
        }

        var draft = validation.Draft;
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var rawToken = NewToken();
        var now = UtcNow();
        var expiresAt = now.Add(VerificationTtl);
        var securityStamp = NewStamp();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<IdentityAccepted>(connection, transaction, null,
            "identity.account.register", idempotencyKey,
            $"email:{draft.NormalizedEmail}|display:{draft.DisplayName}|timezone:{draft.TimeZoneId}|locale:{draft.Locale}",
            now, out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            using (var commandText = CreateCommand(connection, transaction, @"
INSERT INTO [identity].[User]
    ([Id], [Email], [NormalizedEmail], [PasswordHash], [SecurityStamp], [State],
     [EmailConfirmed], [IsDeleted], [DisplayName], [TimeZoneId], [Locale])
VALUES
    (@userId, @email, @normalizedEmail, @passwordHash, @securityStamp,
     'PendingVerification', 0, 0, @displayName, @timeZoneId, @locale);"))
            {
                Add(commandText, "@userId", SqlDbType.UniqueIdentifier, userId);
                Add(commandText, "@email", SqlDbType.NVarChar, draft.OriginalEmail, 320);
                Add(commandText, "@normalizedEmail", SqlDbType.NVarChar, draft.NormalizedEmail, 320);
                Add(commandText, "@passwordHash", SqlDbType.NVarChar, _passwords.Hash(command.Password), 1024);
                Add(commandText, "@securityStamp", SqlDbType.NVarChar, securityStamp, 128);
                Add(commandText, "@displayName", SqlDbType.NVarChar, draft.DisplayName, 100);
                Add(commandText, "@timeZoneId", SqlDbType.NVarChar, draft.TimeZoneId, 128);
                Add(commandText, "@locale", SqlDbType.VarChar, draft.Locale, 8);
                commandText.ExecuteNonQuery();
            }

            ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[UserRole] ([UserId], [RoleId])
SELECT @userId, [Id] FROM [identity].[Role] WHERE [Code] = 'User';",
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId));

            using (var tokenCommand = CreateCommand(connection, transaction, @"
INSERT INTO [identity].[OneTimeToken]
    ([Id], [UserId], [Purpose], [TokenHash], [EmailSnapshot], [ExpiresAt])
VALUES
    (@tokenId, @userId, 'EmailVerification', @tokenHash, @email, @expiresAt);"))
            {
                Add(tokenCommand, "@tokenId", SqlDbType.UniqueIdentifier, tokenId);
                Add(tokenCommand, "@userId", SqlDbType.UniqueIdentifier, userId);
                Add(tokenCommand, "@tokenHash", SqlDbType.Binary, HashToken(rawToken), 32);
                Add(tokenCommand, "@email", SqlDbType.NVarChar, draft.OriginalEmail, 320);
                Add(tokenCommand, "@expiresAt", SqlDbType.DateTime2, expiresAt.UtcDateTime);
                tokenCommand.ExecuteNonQuery();
            }

            InsertAccountMessageIntent(connection, transaction, tokenId, userId, draft.NormalizedEmail, "EmailVerification", now);
            InsertOutbox(connection, transaction, userId, $"identity.email-verification:{tokenId:N}",
                "Identity.EmailVerificationRequested", new { tokenId, purpose = "EmailVerification" }, now);
            InsertAudit(connection, transaction, null, null, "identity.account.register", "User", userId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "Accepted");
            transaction.Commit();
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            RollbackQuietly(transaction);
            // Registration and resend/reset intentionally use an anti-enumeration
            // response. A duplicate normalized email has no visible distinction.
            return Accepted();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityAccepted>(exception);
        }

        PublishMessage(new LocalAccountMessage(tokenId, userId, "EmailVerification", draft.OriginalEmail,
            rawToken, now, expiresAt));
        return Accepted();
    }

    public IdentityOperationResult<IdentityVerification> Verify(string token, string? idempotencyKey = null, string? traceId = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Failure<IdentityVerification>("ValidationFailed", 422, "Token is required.");
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<IdentityVerification>(connection, transaction, null,
            "identity.account.verify", idempotencyKey,
            $"token:{Convert.ToHexString(HashToken(token))}", UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            VerificationRow? row;
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1)
    t.[Id] AS [TokenId], t.[UserId], t.[ExpiresAt], t.[ConsumedAt],
    u.[Email], u.[DisplayName], u.[TimeZoneId], u.[Locale], u.[State],
    u.[EmailConfirmed], u.[IsDeleted], u.[VerifiedAt], u.[RowVersion],
    ps.[Id] AS [PersonalSpaceId]
FROM [identity].[OneTimeToken] AS t WITH (UPDLOCK, ROWLOCK)
INNER JOIN [identity].[User] AS u WITH (UPDLOCK, ROWLOCK) ON u.[Id] = t.[UserId]
LEFT JOIN [platform].[PersonalSpace] AS ps WITH (UPDLOCK, ROWLOCK) ON ps.[UserId] = u.[Id]
WHERE t.[TokenHash] = @tokenHash AND t.[Purpose] = 'EmailVerification';"))
            {
                Add(command, "@tokenHash", SqlDbType.Binary, HashToken(token), 32);
                using var reader = command.ExecuteReader();
                row = reader.Read() ? ReadVerificationRow(reader) : null;
            }

            if (row is null || row.ConsumedAt is not null || row.ExpiresAt <= UtcNow() ||
                row.IsDeleted || row.State != UserState.PendingVerification.ToString() || row.EmailConfirmed)
            {
                RollbackQuietly(transaction);
                return Failure<IdentityVerification>("TokenUnavailable", 410, "Token unavailable.");
            }

            var now = UtcNow();
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[OneTimeToken]
SET [ConsumedAt] = @now
WHERE [Id] = @tokenId AND [ConsumedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@tokenId", SqlDbType.UniqueIdentifier, (object)row.TokenId));

            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[User]
SET [State] = 'Active', [EmailConfirmed] = 1, [VerifiedAt] = @now,
    [UpdatedAt] = @now
WHERE [Id] = @userId AND [State] = 'PendingVerification' AND [EmailConfirmed] = 0;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)row.UserId));

            ExecuteNonQuery(connection, transaction, @"
IF NOT EXISTS (SELECT 1 FROM [platform].[PersonalSpace] WHERE [UserId] = @userId)
BEGIN
    INSERT INTO [platform].[PersonalSpace] ([UserId], [State], [CreatedAt], [UpdatedAt])
    VALUES (@userId, 'Active', @now, @now);
END;",
                ("@userId", SqlDbType.UniqueIdentifier, (object)row.UserId),
                ("@now", SqlDbType.DateTime2, (object)now));

            GrantReadyModules(connection, transaction, row.UserId, now);
            InsertAudit(connection, transaction, null, null, "identity.account.verify", "User", row.UserId, "Succeeded", null, traceId, now);
            var profile = LoadProfile(connection, transaction, row.UserId);
            CompleteReceipt(connection, transaction, receipt, "Verified");
            transaction.Commit();
            return IdentityOperationResult<IdentityVerification>.Success(
                new IdentityVerification("Verified", "EmailVerified", profile));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityVerification>(exception);
        }
    }

    public IdentityOperationResult<IdentityAccepted> ResendVerification(string email, string? idempotencyKey = null, string? traceId = null)
    {
        var normalized = TryNormalizeEmail(email);
        if (normalized is null)
        {
            return Accepted();
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<IdentityAccepted>(connection, transaction, null,
            "identity.account.resend", idempotencyKey,
            $"email:{normalized}", UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        Guid userId;
        string originalEmail;
        try
        {
            using (var lookup = CreateCommand(connection, transaction, @"
SELECT TOP (1) [Id], [Email]
FROM [identity].[User] WITH (UPDLOCK, ROWLOCK)
WHERE [NormalizedEmail] = @normalizedEmail AND [State] = 'PendingVerification' AND [IsDeleted] = 0;"))
            {
                Add(lookup, "@normalizedEmail", SqlDbType.NVarChar, normalized, 320);
                using var reader = lookup.ExecuteReader();
                if (!reader.Read())
                {
                    CompleteReceipt(connection, transaction, receipt, "Accepted");
                    transaction.Commit();
                    return Accepted();
                }

                userId = reader.GetGuid(0);
                originalEmail = reader.GetString(1);
            }

            using (var throttle = CreateCommand(connection, transaction, @"
SELECT TOP (1) [CreatedAt]
FROM [identity].[AccountMessageIntent]
WHERE [UserId] = @userId AND [Purpose] = 'EmailVerification'
ORDER BY [CreatedAt] DESC;"))
            {
                Add(throttle, "@userId", SqlDbType.UniqueIdentifier, userId);
                var latest = throttle.ExecuteScalar();
                if (latest is DateTime latestCreated && latestCreated >= UtcNow().Subtract(ResendThrottle))
                {
                    CompleteReceipt(connection, transaction, receipt, "Accepted");
                    transaction.Commit();
                    return Accepted();
                }
            }

            var now = UtcNow();
            var tokenId = Guid.NewGuid();
            var rawToken = NewToken();
            var expiresAt = now.Add(VerificationTtl);
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[OneTimeToken]
SET [ConsumedAt] = @now
WHERE [UserId] = @userId AND [Purpose] = 'EmailVerification' AND [ConsumedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId));
            InsertToken(connection, transaction, tokenId, userId, "EmailVerification", rawToken, originalEmail, expiresAt);
            InsertAccountMessageIntent(connection, transaction, tokenId, userId, normalized, "EmailVerification", now);
            InsertOutbox(connection, transaction, userId, $"identity.email-verification:{tokenId:N}",
                "Identity.EmailVerificationRequested", new { tokenId, purpose = "EmailVerification" }, now);
            InsertAudit(connection, transaction, null, null, "identity.account.resend", "User", userId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "Accepted");
            transaction.Commit();
            PublishMessage(new LocalAccountMessage(tokenId, userId, "EmailVerification", originalEmail, rawToken, now, expiresAt));
            return Accepted();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityAccepted>(exception);
        }
    }

    public IdentityOperationResult<IdentityLoginIssue> Login(string email, string password, string? deviceLabel = null,
        string? idempotencyKey = null, string? traceId = null)
    {
        var normalized = TryNormalizeEmail(email);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<IdentityLoginIssue>(connection, transaction, null,
            "identity.account.login", idempotencyKey,
            $"email:{normalized ?? "invalid"}", UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            LoginRow? user = null;
            if (normalized is not null)
            {
                using var lookup = CreateCommand(connection, transaction, @"
SELECT TOP (1)
    u.[Id], u.[Email], u.[PasswordHash], u.[SecurityStamp], u.[State],
    u.[EmailConfirmed], u.[IsDeleted], u.[DisplayName], u.[TimeZoneId],
    u.[Locale], ps.[Id] AS [PersonalSpaceId], u.[RowVersion],
    CASE WHEN EXISTS
      (SELECT 1 FROM [identity].[MfaCredential] m WHERE m.[UserId] = u.[Id] AND m.[State] = 'Enabled')
      THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [HasEnabledMfa]
FROM [identity].[User] u
LEFT JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
WHERE u.[NormalizedEmail] = @normalizedEmail;");
                Add(lookup, "@normalizedEmail", SqlDbType.NVarChar, normalized, 320);
                using var reader = lookup.ExecuteReader();
                user = reader.Read() ? ReadLoginRow(reader) : null;
            }

            var passwordMatches = user is not null && _passwords.Verify(password, user.PasswordHash);
            if (!passwordMatches)
            {
                _ = _passwords.Verify(password, DummyPasswordHash);
                RollbackQuietly(transaction);
                return Failure<IdentityLoginIssue>("InvalidCredentials", 401, "Invalid credentials.");
            }

            var decision = AccountAccessPolicy.CanLogin(user!.ToSnapshot());
            if (!decision.Allowed)
            {
                RollbackQuietly(transaction);
                var status = decision.Code == "InvalidCredentials" ? 401 : 403;
                return Failure<IdentityLoginIssue>(decision.Code, status, decision.Message);
            }

            // An active account must have an active PersonalSpace before a
            // session can be issued. Pending verification is handled above;
            // this guard fails closed for a suspended/missing space.
            if (user.PersonalSpaceId is null)
            {
                RollbackQuietly(transaction);
                return Failure<IdentityLoginIssue>("AccountUnavailable", 403, "The personal space is not currently available.");
            }

            var now = UtcNow();
            var rawHandle = NewToken();
            var sessionId = Guid.NewGuid();
            var absoluteExpiresAt = now.Add(AbsoluteTtl);
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[Session]
    ([Id], [UserId], [HandleHash], [DeviceLabel], [SecurityStamp], [CreatedAt],
     [LastSeenAt], [IdleExpiresAt], [AbsoluteExpiresAt], [RecentAuthenticatedAt])
VALUES
    (@sessionId, @userId, @handleHash, @deviceLabel, @securityStamp, @now,
     @now, @idleExpiresAt, @absoluteExpiresAt, @now);",
                ("@sessionId", SqlDbType.UniqueIdentifier, (object)sessionId),
                ("@userId", SqlDbType.UniqueIdentifier, (object)user.Id),
                ("@handleHash", SqlDbType.Binary, (object)HashSession(rawHandle), 32),
                ("@deviceLabel", SqlDbType.NVarChar, (object)NormalizeDeviceLabel(deviceLabel), 160),
                ("@securityStamp", SqlDbType.NVarChar, (object)user.SecurityStamp, 128),
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@idleExpiresAt", SqlDbType.DateTime2, (object)now.Add(IdleTtl)),
                ("@absoluteExpiresAt", SqlDbType.DateTime2, (object)absoluteExpiresAt));

            InsertAudit(connection, transaction, user.Id, user.PersonalSpaceId, "identity.account.login", "Session", sessionId, "Succeeded", null, traceId, now);
            var profile = LoadProfile(connection, transaction, user.Id);
            CompleteReceipt(connection, transaction, receipt, "LoginSucceeded");
            transaction.Commit();
            return IdentityOperationResult<IdentityLoginIssue>.Success(new IdentityLoginIssue(profile, rawHandle, absoluteExpiresAt));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityLoginIssue>(exception);
        }
    }

    public IdentityOperationResult<object?> Logout(string? rawSessionHandle, string? idempotencyKey = null, string? traceId = null)
    {
        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            var receiptFailure = CheckReceipt<object?>(connection, transaction, null,
                "identity.session.logout", idempotencyKey,
                $"handle:{(string.IsNullOrWhiteSpace(rawSessionHandle) ? "missing" : Convert.ToHexString(HashSession(rawSessionHandle!)))}",
                UtcNow(), out var receipt);
            if (receiptFailure is not null)
            {
                RollbackQuietly(transaction);
                return receiptFailure;
            }

            var now = UtcNow();
            using var command = CreateCommand(connection, transaction, @"
UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], @now)
WHERE [HandleHash] = @handleHash;");
            Add(command, "@now", SqlDbType.DateTime2, now);
            Add(command, "@handleHash", SqlDbType.Binary, string.IsNullOrWhiteSpace(rawSessionHandle) ? DBNull.Value : HashSession(rawSessionHandle!), 32);
            command.ExecuteNonQuery();
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<object?> Reauthenticate(string? rawSessionHandle, string password, string? idempotencyKey = null, string? traceId = null)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, null,
            "identity.session.reauthenticate", idempotencyKey,
            $"handle:{(string.IsNullOrWhiteSpace(rawSessionHandle) ? "missing" : Convert.ToHexString(HashSession(rawSessionHandle!)))}|passwordLength:{password?.Length ?? 0}",
            UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null || auth.Session is null)
            {
                RollbackQuietly(transaction);
                return Failure<object?>(auth.Code, auth.StatusCode, auth.Title);
            }

            if (!_passwords.Verify(password, auth.User.PasswordHash))
            {
                RollbackQuietly(transaction);
                return Failure<object?>("InvalidCredentials", 401, "Invalid credentials.");
            }

            var now = UtcNow();
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[Session] SET [RecentAuthenticatedAt] = @now WHERE [Id] = @sessionId;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@sessionId", SqlDbType.UniqueIdentifier, (object)auth.Session.SessionId));
            InsertAudit(connection, transaction, auth.User.UserId, auth.User.PersonalSpaceId, "identity.session.reauthenticate", "Session", auth.Session.SessionId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<IdentityAccepted> RequestPasswordReset(string email, string? idempotencyKey = null, string? traceId = null)
    {
        var normalized = TryNormalizeEmail(email);
        if (normalized is null)
        {
            return Accepted();
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<IdentityAccepted>(connection, transaction, null,
            "identity.account.reset_request", idempotencyKey,
            $"email:{normalized}", UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            Guid userId;
            string originalEmail;
            using (var lookup = CreateCommand(connection, transaction, @"
SELECT TOP (1) [Id], [Email]
FROM [identity].[User] WITH (UPDLOCK, ROWLOCK)
WHERE [NormalizedEmail] = @normalizedEmail AND [IsDeleted] = 0;")
            )
            {
                Add(lookup, "@normalizedEmail", SqlDbType.NVarChar, normalized, 320);
                using var reader = lookup.ExecuteReader();
                if (!reader.Read())
                {
                    CompleteReceipt(connection, transaction, receipt, "Accepted");
                    transaction.Commit();
                    return Accepted();
                }

                userId = reader.GetGuid(0);
                originalEmail = reader.GetString(1);
            }

            var now = UtcNow();
            var tokenId = Guid.NewGuid();
            var rawToken = NewToken();
            var expiresAt = now.Add(ResetTtl);
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[OneTimeToken]
SET [ConsumedAt] = @now
WHERE [UserId] = @userId AND [Purpose] = 'PasswordReset' AND [ConsumedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId));
            InsertToken(connection, transaction, tokenId, userId, "PasswordReset", rawToken, originalEmail, expiresAt);
            InsertAccountMessageIntent(connection, transaction, tokenId, userId, normalized, "PasswordReset", now);
            InsertOutbox(connection, transaction, userId, $"identity.password-reset:{tokenId:N}",
                "Identity.PasswordResetRequested", new { tokenId, purpose = "PasswordReset" }, now);
            InsertAudit(connection, transaction, null, null, "identity.account.reset_request", "User", userId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "Accepted");
            transaction.Commit();
            PublishMessage(new LocalAccountMessage(tokenId, userId, "PasswordReset", originalEmail, rawToken, now, expiresAt));
            return Accepted();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityAccepted>(exception);
        }
    }

    public IdentityOperationResult<object?> ConfirmPasswordReset(string token, string newPassword, string? idempotencyKey = null, string? traceId = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Failure<object?>("ValidationFailed", 422, "Token is required.");
        }

        var passwordDecision = PasswordPolicy.Validate(newPassword);
        if (!passwordDecision.Allowed)
        {
            return Failure<object?>("ValidationFailed", 422, passwordDecision.Message);
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, null,
            "identity.account.reset_confirm", idempotencyKey,
            $"token:{Convert.ToHexString(HashToken(token))}|passwordLength:{newPassword?.Length ?? 0}", UtcNow(), out var receipt);
        if (receiptFailure is not null)
        {
            RollbackQuietly(transaction);
            return receiptFailure;
        }
        try
        {
            ResetRow? row;
            using (var lookup = CreateCommand(connection, transaction, @"
SELECT TOP (1)
    t.[Id] AS [TokenId], t.[UserId], t.[ExpiresAt], t.[ConsumedAt],
    u.[PasswordHash], u.[SecurityStamp], u.[State], u.[EmailConfirmed],
    u.[IsDeleted], ps.[Id] AS [PersonalSpaceId]
FROM [identity].[OneTimeToken] t WITH (UPDLOCK, ROWLOCK)
INNER JOIN [identity].[User] u WITH (UPDLOCK, ROWLOCK) ON u.[Id] = t.[UserId]
LEFT JOIN [platform].[PersonalSpace] ps WITH (UPDLOCK, ROWLOCK) ON ps.[UserId] = u.[Id]
WHERE t.[TokenHash] = @tokenHash AND t.[Purpose] = 'PasswordReset';"))
            {
                Add(lookup, "@tokenHash", SqlDbType.Binary, HashToken(token), 32);
                using var reader = lookup.ExecuteReader();
                row = reader.Read() ? ReadResetRow(reader) : null;
            }

            if (row is null || row.ConsumedAt is not null || row.ExpiresAt <= UtcNow())
            {
                RollbackQuietly(transaction);
                return Failure<object?>("TokenUnavailable", 410, "Token unavailable.");
            }

            var decision = AccountAccessPolicy.CanConfirmPasswordReset(row.ToSnapshot());
            if (!decision.Allowed)
            {
                RollbackQuietly(transaction);
                var status = decision.Code == "MfaRecoveryRequired" ? 409 : 403;
                return Failure<object?>(decision.Code, status, decision.Message);
            }

            var now = UtcNow();
            var newStamp = NewStamp();
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[OneTimeToken] SET [ConsumedAt] = @now
WHERE [Id] = @tokenId AND [ConsumedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@tokenId", SqlDbType.UniqueIdentifier, (object)row.TokenId));
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[User]
SET [PasswordHash] = @passwordHash, [SecurityStamp] = @securityStamp, [UpdatedAt] = @now
WHERE [Id] = @userId AND [IsDeleted] = 0;",
                ("@passwordHash", SqlDbType.NVarChar, (object)_passwords.Hash(newPassword), 1024),
                ("@securityStamp", SqlDbType.NVarChar, (object)newStamp, 128),
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)row.UserId));
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], @now)
WHERE [UserId] = @userId AND [RevokedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)row.UserId));
            InsertOutbox(connection, transaction, row.UserId, $"identity.password-reset.completed:{row.TokenId:N}",
                "Identity.PasswordResetCompleted", new { row.TokenId }, now);
            InsertSecurityNotification(connection, transaction, row.UserId, $"identity.password-reset.completed:{row.TokenId:N}", now);
            InsertAudit(connection, transaction, null, row.PersonalSpaceId, "identity.account.reset_confirm", "User", row.UserId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<IdentityProfileRead> GetMe(string? rawSessionHandle)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null)
            {
                RollbackQuietly(transaction);
                return Failure<IdentityProfileRead>(auth.Code, auth.StatusCode, auth.Title);
            }

            var profile = LoadProfile(connection, transaction, auth.User.UserId);
            transaction.Commit();
            return IdentityOperationResult<IdentityProfileRead>.Success(new IdentityProfileRead(profile, ETag(auth.User.RowVersion)));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityProfileRead>(exception);
        }
    }

    public IdentityOperationResult<IdentityProfileRead> UpdateMe(string? rawSessionHandle, string? ifMatch,
        ProfilePatchRequest command, string? idempotencyKey = null, string? traceId = null)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return Failure<IdentityProfileRead>("PreconditionRequired", 428, "If-Match is required.");
        }

        var fields = new List<string>();
        if (command.DisplayName is not null) fields.Add("displayName");
        if (command.TimeZoneId is not null) fields.Add("timeZoneId");
        if (command.Locale is not null) fields.Add("locale");
        var fieldDecision = ProfilePatchPolicy.ValidatePatchFields(fields);
        if (!fieldDecision.Allowed)
        {
            return Failure<IdentityProfileRead>(fieldDecision.Code, 422, fieldDecision.Message);
        }

        if (command.DisplayName is { } displayName &&
            (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > RegistrationPolicy.DisplayNameMaxLength))
        {
            return Failure<IdentityProfileRead>("ValidationFailed", 422, "displayName is invalid.");
        }

        if (command.TimeZoneId is { } timeZoneId)
        {
            var decision = RegistrationPolicy.ValidateTimeZoneId(timeZoneId);
            if (!decision.Allowed)
            {
                return Failure<IdentityProfileRead>("ValidationFailed", 422, decision.Message);
            }
        }

        if (command.Locale is { } locale)
        {
            var decision = RegistrationPolicy.ValidateLocale(locale);
            if (!decision.Allowed)
            {
                return Failure<IdentityProfileRead>("ValidationFailed", 422, decision.Message);
            }
        }

        if (!TryReadETag(ifMatch, out var expectedRowVersion))
        {
            return Failure<IdentityProfileRead>("RevisionConflict", 412, "Resource revision changed.");
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        ReceiptClaim receipt = default;
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null)
            {
                RollbackQuietly(transaction);
                return Failure<IdentityProfileRead>(auth.Code, auth.StatusCode, auth.Title);
            }

            var receiptFailure = CheckReceipt<IdentityProfileRead>(connection, transaction, auth.User.UserId,
                "identity.profile.update", idempotencyKey,
                $"etag:{ifMatch}|display:{command.DisplayName}|timezone:{command.TimeZoneId}|locale:{command.Locale}",
                UtcNow(), out receipt);
            if (receiptFailure is not null)
            {
                RollbackQuietly(transaction);
                return receiptFailure;
            }

            if (!CryptographicOperations.FixedTimeEquals(auth.User.RowVersion, expectedRowVersion))
            {
                RollbackQuietly(transaction);
                return Failure<IdentityProfileRead>("RevisionConflict", 412, "Resource revision changed.");
            }

            var assignments = new List<string>();
            if (command.DisplayName is not null) assignments.Add("[DisplayName] = @displayName");
            if (command.TimeZoneId is not null) assignments.Add("[TimeZoneId] = @timeZoneId");
            if (command.Locale is not null) assignments.Add("[Locale] = @locale");
            assignments.Add("[UpdatedAt] = @now");
            using (var update = CreateCommand(connection, transaction, $"UPDATE [identity].[User] SET {string.Join(", ", assignments)} WHERE [Id] = @userId AND [RowVersion] = @rowVersion;"))
            {
                if (command.DisplayName is not null) Add(update, "@displayName", SqlDbType.NVarChar, command.DisplayName.Trim(), 100);
                if (command.TimeZoneId is not null) Add(update, "@timeZoneId", SqlDbType.NVarChar, command.TimeZoneId.Trim(), 128);
                if (command.Locale is not null) Add(update, "@locale", SqlDbType.VarChar, command.Locale, 8);
                Add(update, "@now", SqlDbType.DateTime2, UtcNow());
                Add(update, "@userId", SqlDbType.UniqueIdentifier, auth.User.UserId);
                Add(update, "@rowVersion", SqlDbType.Binary, expectedRowVersion, 8);
                if (update.ExecuteNonQuery() == 0)
                {
                    RollbackQuietly(transaction);
                    return Failure<IdentityProfileRead>("RevisionConflict", 412, "Resource revision changed.");
                }
            }

            var now = UtcNow();
            InsertAudit(connection, transaction, auth.User.UserId, auth.User.PersonalSpaceId, "identity.profile.update", "User", auth.User.UserId, "Succeeded",
                JsonSerializer.Serialize(new { fields }), traceId, now);
            var profile = LoadProfile(connection, transaction, auth.User.UserId);
            var rowVersion = LoadRowVersion(connection, transaction, auth.User.UserId);
            CompleteReceipt(connection, transaction, receipt, "ProfileUpdated");
            transaction.Commit();
            return IdentityOperationResult<IdentityProfileRead>.Success(new IdentityProfileRead(profile, ETag(rowVersion)));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityProfileRead>(exception);
        }
    }

    public IdentityOperationResult<IdentitySessionPage> ListSessions(string? rawSessionHandle)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null || auth.Session is null)
            {
                RollbackQuietly(transaction);
                return Failure<IdentitySessionPage>(auth.Code, auth.StatusCode, auth.Title);
            }

            var now = UtcNow();
            var sessions = new List<IdentitySession>();
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (25) [Id], [DeviceLabel], [CreatedAt], [LastSeenAt], [AbsoluteExpiresAt]
FROM [identity].[Session]
WHERE [UserId] = @userId AND [RevokedAt] IS NULL
  AND [AbsoluteExpiresAt] > @now AND [IdleExpiresAt] > @now
ORDER BY [CreatedAt] DESC, [Id];"))
            {
                Add(command, "@userId", SqlDbType.UniqueIdentifier, auth.User.UserId);
                Add(command, "@now", SqlDbType.DateTime2, now);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    sessions.Add(new IdentitySession(reader.GetGuid(0), reader.GetString(1),
                        ToOffset(reader.GetDateTime(2)), ToOffset(reader.GetDateTime(3)), ToOffset(reader.GetDateTime(4)),
                        reader.GetGuid(0) == auth.Session.SessionId));
                }
            }

            transaction.Commit();
            return IdentityOperationResult<IdentitySessionPage>.Success(new IdentitySessionPage(sessions, null));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentitySessionPage>(exception);
        }
    }

    public IdentityOperationResult<object?> RevokeSession(string? rawSessionHandle, Guid sessionId, string? idempotencyKey = null, string? traceId = null)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null)
            {
                RollbackQuietly(transaction);
                return Failure<object?>(auth.Code, auth.StatusCode, auth.Title);
            }

            var receiptFailure = CheckReceipt<object?>(connection, transaction, auth.User.UserId,
                "identity.session.revoke_session", idempotencyKey,
                $"session:{sessionId:N}", UtcNow(), out var receipt);
            if (receiptFailure is not null)
            {
                RollbackQuietly(transaction);
                return receiptFailure;
            }

            var now = UtcNow();
            using var command = CreateCommand(connection, transaction, @"
UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], @now)
WHERE [Id] = @sessionId AND [UserId] = @userId;");
            Add(command, "@now", SqlDbType.DateTime2, now);
            Add(command, "@sessionId", SqlDbType.UniqueIdentifier, sessionId);
            Add(command, "@userId", SqlDbType.UniqueIdentifier, auth.User.UserId);
            if (command.ExecuteNonQuery() == 0)
            {
                RollbackQuietly(transaction);
                return Failure<object?>("ResourceUnavailable", 404, "Session unavailable.");
            }

            InsertAudit(connection, transaction, auth.User.UserId, auth.User.PersonalSpaceId, "identity.session.revoke_session", "Session", sessionId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<object?> RevokeAll(string? rawSessionHandle, string? idempotencyKey = null, string? traceId = null)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null)
            {
                RollbackQuietly(transaction);
                return Failure<object?>(auth.Code, auth.StatusCode, auth.Title);
            }

            var receiptFailure = CheckReceipt<object?>(connection, transaction, auth.User.UserId,
                "identity.session.revoke_all", idempotencyKey,
                "all-sessions", UtcNow(), out var receipt);
            if (receiptFailure is not null)
            {
                RollbackQuietly(transaction);
                return receiptFailure;
            }

            var now = UtcNow();
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], @now)
WHERE [UserId] = @userId AND [RevokedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)auth.User.UserId));
            InsertAudit(connection, transaction, auth.User.UserId, auth.User.PersonalSpaceId, "identity.session.revoke_all", "User", auth.User.UserId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<object?> SoftDelete(string? rawSessionHandle, string confirmation, string password, string? idempotencyKey = null, string? traceId = null)
    {
        if (!string.Equals(confirmation?.Trim(), "DELETE", StringComparison.Ordinal))
        {
            return Failure<object?>("DeletionConfirmationRequired", 422, "Type DELETE to confirm account deletion.");
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null || auth.Session is null)
            {
                RollbackQuietly(transaction);
                return Failure<object?>(auth.Code, auth.StatusCode, auth.Title);
            }

            var receiptFailure = CheckReceipt<object?>(connection, transaction, auth.User.UserId,
                "identity.account.soft_delete", idempotencyKey,
                $"confirmation:{confirmation}|passwordLength:{password?.Length ?? 0}", UtcNow(), out var receipt);
            if (receiptFailure is not null)
            {
                RollbackQuietly(transaction);
                return receiptFailure;
            }

            var now = UtcNow();
            if (auth.Session.RecentAuthenticatedAt is null || auth.Session.RecentAuthenticatedAt.Value < now.Subtract(TimeSpan.FromMinutes(5)))
            {
                RollbackQuietly(transaction);
                return Failure<object?>("RecentAuthenticationRequired", 428, "Reauthenticate within five minutes before deleting the account.");
            }

            if (!_passwords.Verify(password, auth.User.PasswordHash))
            {
                RollbackQuietly(transaction);
                return Failure<object?>("InvalidCredentials", 401, "Invalid credentials.");
            }

            using (var guard = CreateCommand(connection, transaction, "SELECT [Id] FROM [platform].[SecurityInvariant] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = 1;"))
            {
                _ = guard.ExecuteScalar();
            }

            if (string.Equals(auth.User.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                using var count = CreateCommand(connection, transaction, @"
SELECT COUNT_BIG(1)
FROM [identity].[UserRole] ur
INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
INNER JOIN [identity].[User] u ON u.[Id] = ur.[UserId]
WHERE r.[Code] = 'SuperAdmin' AND u.[State] = 'Active' AND u.[IsDeleted] = 0;");
                if (Convert.ToInt64(count.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) <= 1)
                {
                    RollbackQuietly(transaction);
                    return Failure<object?>("LastSuperAdmin", 409, "The last active SuperAdmin cannot be deleted.");
                }
            }

            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[User]
SET [State] = 'Deleted', [IsDeleted] = 1, [DeletedAt] = @now,
    [DeletedByUserId] = [Id], [UpdatedAt] = @now
WHERE [Id] = @userId AND [State] = 'Active' AND [IsDeleted] = 0;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)auth.User.UserId));
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], @now)
WHERE [UserId] = @userId AND [RevokedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@userId", SqlDbType.UniqueIdentifier, (object)auth.User.UserId));
            InsertOutbox(connection, transaction, auth.User.UserId, $"identity.account.soft-delete:{auth.User.UserId:N}",
                "Identity.AccountSoftDeleted", new { userId = auth.User.UserId }, now);
            InsertAudit(connection, transaction, auth.User.UserId, auth.User.PersonalSpaceId, "identity.account.soft_delete", "User", auth.User.UserId, "Succeeded", null, traceId, now);
            CompleteReceipt(connection, transaction, receipt, "NoContent");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<IdentityPrincipal> GetPrincipal(string? rawSessionHandle)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var auth = AuthenticateSession(connection, transaction, rawSessionHandle, true);
            if (!auth.Succeeded || auth.User is null || auth.User.PersonalSpaceId is null)
            {
                RollbackQuietly(transaction);
                return Failure<IdentityPrincipal>(auth.Code, auth.StatusCode, auth.Title);
            }

            transaction.Commit();
            return IdentityOperationResult<IdentityPrincipal>.Success(new IdentityPrincipal(
                auth.User.UserId, auth.User.PersonalSpaceId.Value, auth.User.Role, auth.User.RecentAuthenticatedAt ?? UtcNow()));
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<IdentityPrincipal>(exception);
        }
    }

    public Task<IdentityOperationResult<IdentityPrincipal>> AuthenticateAsync(string? rawSessionHandle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetPrincipal(rawSessionHandle));
    }

    public IdentityOperationResult<BootstrapSuperAdminResult> BootstrapSuperAdmin(BootstrapSuperAdminCommand command, string? traceId = null)
    {
        var registration = RegistrationCommandPolicy.Validate(new RegistrationCommand(
            command.Email, command.Password, command.TimeZoneId, command.DisplayName, command.Locale));
        if (!registration.IsValid || registration.Draft is null)
        {
            return Invalid<BootstrapSuperAdminResult>(registration.Issues);
        }

        var draft = registration.Draft;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (var guard = CreateCommand(connection, transaction, @"
SELECT [Id] FROM [platform].[SecurityInvariant] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = 1;"))
            {
                if (guard.ExecuteScalar() is null)
                {
                    RollbackQuietly(transaction);
                    return Failure<BootstrapSuperAdminResult>("BootstrapUnavailable", 503, "Security invariant is unavailable.");
                }
            }

            // Bootstrap is a one-way local operation. The additive closure
            // migration persists the marker independently of the role rows so
            // deleting/altering a later account can never reopen the gate.
            using (var closure = CreateCommand(connection, transaction, @"
SELECT [BootstrapCompletedAt] FROM [platform].[SecurityInvariant] WHERE [Id] = 1;"))
            {
                var completedAt = closure.ExecuteScalar();
                if (completedAt is DateTime)
                {
                    RollbackQuietly(transaction);
                    return Failure<BootstrapSuperAdminResult>("BootstrapAlreadyCompleted", 409, "SuperAdmin bootstrap has already completed.");
                }
            }

            using (var existing = CreateCommand(connection, transaction, @"
SELECT COUNT_BIG(1)
FROM [identity].[UserRole] ur
INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
INNER JOIN [identity].[User] u ON u.[Id] = ur.[UserId]
WHERE r.[Code] = 'SuperAdmin' AND u.[State] = 'Active' AND u.[IsDeleted] = 0;"))
            {
                if (Convert.ToInt64(existing.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0)
                {
                    RollbackQuietly(transaction);
                    return Failure<BootstrapSuperAdminResult>("BootstrapAlreadyCompleted", 409, "SuperAdmin bootstrap has already completed.");
                }
            }

            var userId = Guid.NewGuid();
            var spaceId = Guid.NewGuid();
            var now = UtcNow();
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[User]
    ([Id], [Email], [NormalizedEmail], [PasswordHash], [SecurityStamp], [State],
     [EmailConfirmed], [VerifiedAt], [IsDeleted], [DisplayName], [TimeZoneId], [Locale])
VALUES
    (@userId, @email, @normalizedEmail, @passwordHash, @securityStamp, 'Active',
     1, @now, 0, @displayName, @timeZoneId, @locale);",
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
                ("@email", SqlDbType.NVarChar, (object)draft.OriginalEmail, 320),
                ("@normalizedEmail", SqlDbType.NVarChar, (object)draft.NormalizedEmail, 320),
                ("@passwordHash", SqlDbType.NVarChar, (object)_passwords.Hash(command.Password), 1024),
                ("@securityStamp", SqlDbType.NVarChar, (object)NewStamp(), 128),
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@displayName", SqlDbType.NVarChar, (object)draft.DisplayName, 100),
                ("@timeZoneId", SqlDbType.NVarChar, (object)draft.TimeZoneId, 128),
                ("@locale", SqlDbType.VarChar, (object)draft.Locale, 8));
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO [platform].[PersonalSpace] ([Id], [UserId], [State], [CreatedAt], [UpdatedAt])
VALUES (@spaceId, @userId, 'Active', @now, @now);",
                ("@spaceId", SqlDbType.UniqueIdentifier, (object)spaceId),
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
                ("@now", SqlDbType.DateTime2, (object)now));
            ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[UserRole] ([UserId], [RoleId])
SELECT @userId, [Id] FROM [identity].[Role] WHERE [Code] = 'SuperAdmin';",
                ("@userId", SqlDbType.UniqueIdentifier, (object)userId));
            GrantReadyModules(connection, transaction, userId, now);
            InsertAudit(connection, transaction, userId, spaceId, "identity.superadmin.bootstrap", "User", userId, "Succeeded", null, traceId, now);
            ExecuteNonQuery(connection, transaction, @"
UPDATE [platform].[SecurityInvariant]
SET [BootstrapCompletedAt] = @now, [UpdatedAt] = @now
WHERE [Id] = 1 AND [BootstrapCompletedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now));
            transaction.Commit();
            return IdentityOperationResult<BootstrapSuperAdminResult>.Success(new BootstrapSuperAdminResult(userId, spaceId), 201, "Created");
        }
        catch (SqlException exception) when (IsUniqueViolation(exception))
        {
            RollbackQuietly(transaction);
            return Failure<BootstrapSuperAdminResult>("BootstrapAlreadyCompleted", 409, "SuperAdmin bootstrap has already completed.");
        }
        catch (SqlException exception)
        {
            RollbackQuietly(transaction);
            return PersistenceFailure<BootstrapSuperAdminResult>(exception);
        }
    }

    private IdentityProfile LoadProfile(SqlConnection connection, SqlTransaction transaction, Guid userId)
    {
        ProfileRow row;
        using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1)
    u.[Id], u.[Email], u.[DisplayName], u.[TimeZoneId], u.[Locale], u.[State],
    u.[IsDeleted], ps.[Id] AS [PersonalSpaceId], u.[RowVersion],
    COALESCE((SELECT TOP (1) r.[Code] FROM [identity].[UserRole] ur
              INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
              WHERE ur.[UserId] = u.[Id]
              ORDER BY CASE r.[Code] WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC), 'User') AS [Role]
FROM [identity].[User] u
LEFT JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
WHERE u.[Id] = @userId;"))
        {
            Add(command, "@userId", SqlDbType.UniqueIdentifier, userId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException("Authenticated identity was not found.");
            }

            row = ReadProfileRow(reader);
        }

        var modules = new List<IdentityModuleProjection>();
        using (var command = CreateCommand(connection, transaction, @"
SELECT m.[Code],
       CAST(CASE WHEN m.[SystemEnabled] = 1 AND m.[State] = 'Ready'
                       AND ISNULL(g.[Enabled], 0) = 1 THEN 1 ELSE 0 END AS bit) AS [Enabled],
       CASE WHEN m.[SystemEnabled] = 0 THEN 'SystemDisabled'
            WHEN m.[State] <> 'Ready' THEN m.[State]
            WHEN ISNULL(g.[Enabled], 0) = 0 THEN 'NotGranted'
            ELSE NULL END AS [UnavailableReason]
FROM [platform].[Module] m
LEFT JOIN [platform].[UserModuleGrant] g
  ON g.[ModuleId] = m.[Id] AND g.[UserId] = @userId
ORDER BY m.[Code];"))
        {
            Add(command, "@userId", SqlDbType.UniqueIdentifier, userId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                modules.Add(new IdentityModuleProjection(reader.GetString(0), reader.GetBoolean(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }
        }

        return new IdentityProfile(row.Id, row.Email, row.DisplayName, row.TimeZoneId, row.Locale,
            row.State, row.PersonalSpaceId, modules, row.Role);
    }

    private static byte[] LoadRowVersion(SqlConnection connection, SqlTransaction transaction, Guid userId)
    {
        using var command = CreateCommand(connection, transaction, @"
SELECT [RowVersion] FROM [identity].[User] WHERE [Id] = @userId;");
        Add(command, "@userId", SqlDbType.UniqueIdentifier, userId);
        var value = command.ExecuteScalar();
        return value as byte[] ?? throw new InvalidOperationException("Updated identity revision was not returned.");
    }

    private static AuthResult AuthenticateSession(SqlConnection connection, SqlTransaction transaction,
        string? rawSessionHandle, bool touch)
    {
        if (string.IsNullOrWhiteSpace(rawSessionHandle))
        {
            return AuthResult.Fail("AuthenticationRequired", 401, "Authentication required.");
        }

        AuthRow? row;
        using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1)
    s.[Id] AS [SessionId], s.[UserId], s.[SecurityStamp] AS [SessionSecurityStamp],
    s.[RecentAuthenticatedAt], s.[RevokedAt], s.[IdleExpiresAt], s.[AbsoluteExpiresAt],
    u.[Email], u.[PasswordHash], u.[SecurityStamp], u.[State], u.[EmailConfirmed],
    u.[IsDeleted], ps.[Id] AS [PersonalSpaceId],
    CASE WHEN EXISTS
      (SELECT 1 FROM [identity].[MfaCredential] m WHERE m.[UserId] = u.[Id] AND m.[State] = 'Enabled')
      THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS [HasEnabledMfa],
    COALESCE((SELECT TOP (1) r.[Code] FROM [identity].[UserRole] ur
              INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
              WHERE ur.[UserId] = u.[Id]
              ORDER BY CASE r.[Code] WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC), 'User') AS [Role]
FROM [identity].[Session] s
INNER JOIN [identity].[User] u ON u.[Id] = s.[UserId]
INNER JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id] AND ps.[State] = 'Active'
WHERE s.[HandleHash] = @handleHash;"))
        {
            Add(command, "@handleHash", SqlDbType.Binary, HashSession(rawSessionHandle), 32);
            using var reader = command.ExecuteReader();
            row = reader.Read() ? ReadAuthRow(reader) : null;
        }

        if (row is null)
        {
            return AuthResult.Fail("AuthenticationRequired", 401, "Authentication required.");
        }

        var now = UtcNow();
        if (row.RevokedAt is not null || row.AbsoluteExpiresAt <= now || row.IdleExpiresAt <= now ||
            !string.Equals(row.SessionSecurityStamp, row.SecurityStamp, StringComparison.Ordinal))
        {
            return AuthResult.Fail("AuthenticationRequired", 401, "Authentication required.");
        }

        var decision = AccountAccessPolicy.CanLogin(row.ToSnapshot());
        if (!decision.Allowed)
        {
            return AuthResult.Fail(decision.Code, 403, decision.Message);
        }

        if (touch)
        {
            ExecuteNonQuery(connection, transaction, @"
UPDATE [identity].[Session]
SET [LastSeenAt] = @now, [IdleExpiresAt] = @idleExpiresAt
WHERE [Id] = @sessionId AND [RevokedAt] IS NULL;",
                ("@now", SqlDbType.DateTime2, (object)now),
                ("@idleExpiresAt", SqlDbType.DateTime2, (object)now.Add(IdleTtl)),
                ("@sessionId", SqlDbType.UniqueIdentifier, (object)row.SessionId));
        }

        return AuthResult.Ok(row);
    }

    private static VerificationRow ReadVerificationRow(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("TokenId")),
        reader.GetGuid(reader.GetOrdinal("UserId")),
        reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
        reader.IsDBNull(reader.GetOrdinal("ConsumedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConsumedAt")),
        reader.GetString(reader.GetOrdinal("Email")),
        reader.GetString(reader.GetOrdinal("DisplayName")),
        reader.GetString(reader.GetOrdinal("TimeZoneId")),
        reader.GetString(reader.GetOrdinal("Locale")),
        reader.GetString(reader.GetOrdinal("State")),
        reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
        reader.IsDBNull(reader.GetOrdinal("VerifiedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("VerifiedAt")),
        reader.GetFieldValue<byte[]>(reader.GetOrdinal("RowVersion")),
        reader.IsDBNull(reader.GetOrdinal("PersonalSpaceId")) ? null : reader.GetGuid(reader.GetOrdinal("PersonalSpaceId")));

    private static LoginRow ReadLoginRow(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("Id")),
        reader.GetString(reader.GetOrdinal("Email")),
        reader.GetString(reader.GetOrdinal("PasswordHash")),
        reader.GetString(reader.GetOrdinal("SecurityStamp")),
        reader.GetString(reader.GetOrdinal("State")),
        reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
        reader.GetString(reader.GetOrdinal("DisplayName")),
        reader.GetString(reader.GetOrdinal("TimeZoneId")),
        reader.GetString(reader.GetOrdinal("Locale")),
        reader.IsDBNull(reader.GetOrdinal("PersonalSpaceId")) ? null : reader.GetGuid(reader.GetOrdinal("PersonalSpaceId")),
        reader.GetFieldValue<byte[]>(reader.GetOrdinal("RowVersion")),
        reader.GetBoolean(reader.GetOrdinal("HasEnabledMfa")));

    private static ResetRow ReadResetRow(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("TokenId")),
        reader.GetGuid(reader.GetOrdinal("UserId")),
        reader.GetDateTime(reader.GetOrdinal("ExpiresAt")),
        reader.IsDBNull(reader.GetOrdinal("ConsumedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ConsumedAt")),
        reader.GetString(reader.GetOrdinal("PasswordHash")),
        reader.GetString(reader.GetOrdinal("SecurityStamp")),
        reader.GetString(reader.GetOrdinal("State")),
        reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
        reader.IsDBNull(reader.GetOrdinal("PersonalSpaceId")) ? null : reader.GetGuid(reader.GetOrdinal("PersonalSpaceId")));

    private static ProfileRow ReadProfileRow(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("Id")),
        reader.GetString(reader.GetOrdinal("Email")),
        reader.GetString(reader.GetOrdinal("DisplayName")),
        reader.GetString(reader.GetOrdinal("TimeZoneId")),
        reader.GetString(reader.GetOrdinal("Locale")),
        reader.GetString(reader.GetOrdinal("State")),
        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
        reader.IsDBNull(reader.GetOrdinal("PersonalSpaceId")) ? null : reader.GetGuid(reader.GetOrdinal("PersonalSpaceId")),
        reader.GetFieldValue<byte[]>(reader.GetOrdinal("RowVersion")),
        reader.GetString(reader.GetOrdinal("Role")));

    private static AuthRow ReadAuthRow(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("SessionId")),
        reader.GetGuid(reader.GetOrdinal("UserId")),
        reader.GetString(reader.GetOrdinal("SessionSecurityStamp")),
        reader.IsDBNull(reader.GetOrdinal("RecentAuthenticatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("RecentAuthenticatedAt")),
        reader.IsDBNull(reader.GetOrdinal("RevokedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("RevokedAt")),
        reader.GetDateTime(reader.GetOrdinal("IdleExpiresAt")),
        reader.GetDateTime(reader.GetOrdinal("AbsoluteExpiresAt")),
        reader.GetString(reader.GetOrdinal("Email")),
        reader.GetString(reader.GetOrdinal("PasswordHash")),
        reader.GetString(reader.GetOrdinal("SecurityStamp")),
        reader.GetString(reader.GetOrdinal("State")),
        reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
        reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
        reader.IsDBNull(reader.GetOrdinal("PersonalSpaceId")) ? null : reader.GetGuid(reader.GetOrdinal("PersonalSpaceId")),
        reader.GetBoolean(reader.GetOrdinal("HasEnabledMfa")),
        reader.GetString(reader.GetOrdinal("Role")));

    private static void InsertToken(SqlConnection connection, SqlTransaction transaction, Guid tokenId, Guid userId,
        string purpose, string rawToken, string email, DateTime expiresAt)
    {
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[OneTimeToken]
    ([Id], [UserId], [Purpose], [TokenHash], [EmailSnapshot], [ExpiresAt])
VALUES
    (@tokenId, @userId, @purpose, @tokenHash, @email, @expiresAt);",
            ("@tokenId", SqlDbType.UniqueIdentifier, (object)tokenId),
            ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
            ("@purpose", SqlDbType.VarChar, (object)purpose, 32),
            ("@tokenHash", SqlDbType.Binary, (object)HashToken(rawToken), 32),
            ("@email", SqlDbType.NVarChar, (object)email, 320),
            ("@expiresAt", SqlDbType.DateTime2, (object)expiresAt));
    }

    private static void InsertAccountMessageIntent(SqlConnection connection, SqlTransaction transaction, Guid tokenId,
        Guid userId, string normalizedEmail, string purpose, DateTime now)
    {
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [identity].[AccountMessageIntent]
    ([Id], [UserId], [NormalizedEmailHash], [Purpose], [State], [NotBeforeAt], [CreatedAt], [UpdatedAt])
VALUES
    (@id, @userId, @emailHash, @purpose, 'Pending', @now, @now, @now);",
            ("@id", SqlDbType.UniqueIdentifier, (object)tokenId),
            ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
            ("@emailHash", SqlDbType.Binary, (object)HashText(normalizedEmail), 32),
            ("@purpose", SqlDbType.VarChar, (object)purpose, 64),
            ("@now", SqlDbType.DateTime2, (object)now));
    }

    private static void InsertOutbox(SqlConnection connection, SqlTransaction transaction, Guid? ownerUserId,
        string logicalKey, string kind, object payload, DateTime now)
    {
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [operations].[Outbox]
    ([OwnerUserId], [LogicalKey], [Kind], [PayloadJson], [State], [CreatedAt], [NotBeforeAt])
VALUES
    (@ownerUserId, @logicalKey, @kind, @payloadJson, 'Pending', @now, @now);",
            ("@ownerUserId", SqlDbType.UniqueIdentifier, (object?)ownerUserId ?? DBNull.Value),
            ("@logicalKey", SqlDbType.NVarChar, (object)logicalKey, 200),
            ("@kind", SqlDbType.NVarChar, (object)kind, 100),
            ("@payloadJson", SqlDbType.NVarChar, (object)JsonSerializer.Serialize(payload)),
            ("@now", SqlDbType.DateTime2, (object)now));
    }

    private static void InsertSecurityNotification(SqlConnection connection, SqlTransaction transaction, Guid userId,
        string logicalKey, DateTime now)
    {
        var notificationId = Guid.NewGuid();
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [notifications].[Notification]
    ([Id], [OwnerUserId], [LogicalKey], [Kind], [Title], [Body], [CreatedAt])
VALUES
    (@id, @userId, @logicalKey, 'Security.PasswordReset', N'Password changed',
     N'Your Nexora password was changed. Existing sessions were signed out.', @now);",
            ("@id", SqlDbType.UniqueIdentifier, (object)notificationId),
            ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
            ("@logicalKey", SqlDbType.NVarChar, (object)logicalKey, 200),
            ("@now", SqlDbType.DateTime2, (object)now));
        // Schedule all channels in one transaction. Browser Push is explicitly
        // unavailable until a local subscription/provider is configured; that
        // state must not be represented as a false delivery success.
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [notifications].[Delivery]
    ([NotificationId], [Channel], [State], [LastErrorCode], [CreatedAt], [UpdatedAt])
VALUES
    (@notificationId, 'InApp', 'Pending', NULL, @now, @now),
    (@notificationId, 'Email', 'Pending', NULL, @now, @now),
    (@notificationId, 'BrowserPush', 'PermissionUnavailable', 'PushPermissionUnavailable', @now, @now);",
            ("@notificationId", SqlDbType.UniqueIdentifier, (object)notificationId),
            ("@now", SqlDbType.DateTime2, (object)now));
        InsertOutbox(connection, transaction, userId, $"notification.dispatch:{logicalKey}",
            "Notifications.DispatchRequested", new { notificationId, channels = new[] { "InApp", "Email", "BrowserPush" } }, now);
    }

    private static void GrantReadyModules(SqlConnection connection, SqlTransaction transaction, Guid userId, DateTime now)
    {
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [platform].[UserModuleGrant] ([UserId], [ModuleId], [Enabled], [CreatedAt], [UpdatedAt])
SELECT @userId, m.[Id], CAST(1 AS bit), @now, @now
FROM [platform].[Module] m
WHERE m.[SystemEnabled] = 1 AND m.[RegistrationEnabled] = 1 AND m.[State] = 'Ready'
  AND NOT EXISTS
      (SELECT 1 FROM [platform].[UserModuleGrant] g WHERE g.[UserId] = @userId AND g.[ModuleId] = m.[Id]);",
            ("@userId", SqlDbType.UniqueIdentifier, (object)userId),
            ("@now", SqlDbType.DateTime2, (object)now));
    }

    private static void InsertAudit(SqlConnection connection, SqlTransaction transaction, Guid? actorUserId,
        Guid? ownerUserId, string actionKey, string targetType, Guid? targetId, string result,
        string? redactedDiffJson, string? traceId, DateTime now)
    {
        ExecuteNonQuery(connection, transaction, @"
INSERT INTO [security].[AuditEvent]
    ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [RedactedDiffJson], [TraceId], [CreatedAt])
VALUES
    (@actorUserId, @ownerUserId, @actionKey, @targetType, @targetId, @result, @redactedDiffJson, @traceId, @now);",
            ("@actorUserId", SqlDbType.UniqueIdentifier, (object?)actorUserId ?? DBNull.Value),
            ("@ownerUserId", SqlDbType.UniqueIdentifier, (object?)ownerUserId ?? DBNull.Value),
            ("@actionKey", SqlDbType.NVarChar, (object)actionKey, 160),
            ("@targetType", SqlDbType.NVarChar, (object)targetType, 100),
            ("@targetId", SqlDbType.UniqueIdentifier, (object?)targetId ?? DBNull.Value),
            ("@result", SqlDbType.VarChar, (object)result, 32),
            ("@redactedDiffJson", SqlDbType.NVarChar, (object?)redactedDiffJson ?? DBNull.Value),
            ("@traceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 128),
            ("@now", SqlDbType.DateTime2, (object)now));
    }

    private void PublishMessage(LocalAccountMessage message)
    {
        try
        {
            _messageSink.Publish(message);
        }
        catch
        {
            // The durable intent/outbox is already committed. A provider failure
            // must be retried by the delivery worker and must not leak a token or
            // turn a successful registration into an ambiguous server error.
        }
    }

    private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction? transaction, string text)
    {
        var command = connection.CreateCommand();
        command.CommandText = text;
        command.CommandType = CommandType.Text;
        command.Transaction = transaction;
        command.CommandTimeout = 30;
        return command;
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object? value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type);
        if (size > 0)
        {
            parameter.Size = size;
        }

        parameter.Value = value ?? DBNull.Value;
    }

    private static void ExecuteNonQuery(SqlConnection connection, SqlTransaction? transaction, string text,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, text);
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Type, parameter.Value);
        }

        command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqlConnection connection, SqlTransaction? transaction, string text,
        params (string Name, SqlDbType Type, object Value, int Size)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, text);
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Type, parameter.Value, parameter.Size);
        }

        command.ExecuteNonQuery();
    }

    private static IdentityOperationResult<T> Invalid<T>(IReadOnlyList<Nexora.Domain.Common.PolicyDecision> issues)
    {
        var first = issues.FirstOrDefault();
        return Failure<T>(first?.Code ?? "ValidationFailed", 422, first?.Message ?? "Request is invalid.");
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int statusCode, string title) =>
        IdentityOperationResult<T>.Failure(code, statusCode, title);

    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) =>
        Failure<T>("PersistenceUnavailable", 503, "Identity persistence is unavailable.");

    private static IdentityOperationResult<IdentityAccepted> Accepted() =>
        IdentityOperationResult<IdentityAccepted>.Success(new IdentityAccepted("Accepted", "CheckEmailIfEligible"), 202, "Accepted");

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        Guid? subjectId, string operationKey, string? idempotencyKey, string canonicalRequest, DateTime now,
        out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        claim = _receipts.TryClaim(connection, transaction, subjectId, operationKey, idempotencyKey!,
            canonicalRequest, now);
        if (claim.IsClaimed)
        {
            return null;
        }

        return claim.IsConflict
            ? Failure<T>("IdempotencyConflict", 409, "The same Idempotency-Key was already used with a different request.")
            : claim.IsReplay
                ? Failure<T>("IdempotencyReplay", 409, "The request was already completed; use a new Idempotency-Key for a new mutation.")
                : Failure<T>("RequestInProgress", 409, "An identical request is already in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static string? TryNormalizeEmail(string? email)
    {
        try
        {
            return email is null ? null : EmailNormalizer.Normalize(email);
        }
        catch (Exception) when (email is not null)
        {
            return null;
        }
    }

    private static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string NormalizeDeviceLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Local browser session";
        }

        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, 160)];
    }

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static byte[] HashSession(string rawHandle) => HashTextBytes(rawHandle);

    private static byte[] HashToken(string rawToken) => HashTextBytes(rawToken);

    private static byte[] HashText(string value) => HashTextBytes(value);

    private static byte[] HashTextBytes(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static DateTime UtcNow() => DateTime.UtcNow;

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string ETag(byte[] rowVersion) => $"\"{Convert.ToBase64String(rowVersion)}\"";

    private static bool TryReadETag(string value, out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();
        if (value.Length < 2 || value[0] != '"' || value[^1] != '"')
        {
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value[1..^1]);
            return rowVersion.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsUniqueViolation(SqlException exception) =>
        exception.Number is 2601 or 2627;

    private static void RollbackQuietly(SqlTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch (InvalidOperationException)
        {
            // The transaction may already have been completed by the provider.
        }
    }

    private sealed record VerificationRow(Guid TokenId, Guid UserId, DateTime ExpiresAt, DateTime? ConsumedAt,
        string Email, string DisplayName, string TimeZoneId, string Locale, string State, bool EmailConfirmed,
        bool IsDeleted, DateTime? VerifiedAt, byte[] RowVersion, Guid? PersonalSpaceId);

    private sealed record LoginRow(Guid Id, string Email, string PasswordHash, string SecurityStamp, string State,
        bool EmailConfirmed, bool IsDeleted, string DisplayName, string TimeZoneId, string Locale,
        Guid? PersonalSpaceId, byte[] RowVersion, bool HasEnabledMfa)
    {
        public UserLoginSnapshot ToSnapshot() => new(Id, ParseState(State), EmailConfirmed, IsDeleted,
            ParseState(State) == UserState.Disabled, HasEnabledMfa, SecurityStamp);
    }

    private sealed record ResetRow(Guid TokenId, Guid UserId, DateTime ExpiresAt, DateTime? ConsumedAt,
        string PasswordHash, string SecurityStamp, string State, bool EmailConfirmed, bool IsDeleted,
        Guid? PersonalSpaceId)
    {
        public UserLoginSnapshot ToSnapshot() => new(UserId, ParseState(State), EmailConfirmed, IsDeleted,
            ParseState(State) == UserState.Disabled, false, SecurityStamp);
    }

    private sealed record ProfileRow(Guid Id, string Email, string DisplayName, string TimeZoneId, string Locale,
        string State, bool IsDeleted, Guid? PersonalSpaceId, byte[] RowVersion, string Role);

    private sealed record AuthRow(Guid SessionId, Guid UserId, string SessionSecurityStamp,
        DateTime? RecentAuthenticatedAt, DateTime? RevokedAt, DateTime IdleExpiresAt, DateTime AbsoluteExpiresAt,
        string Email, string PasswordHash, string SecurityStamp, string State, bool EmailConfirmed, bool IsDeleted,
        Guid? PersonalSpaceId, bool HasEnabledMfa, string Role)
    {
        public UserLoginSnapshot ToSnapshot() => new(UserId, ParseState(State), EmailConfirmed, IsDeleted,
            ParseState(State) == UserState.Disabled, HasEnabledMfa, SecurityStamp);
    }

    private sealed record AuthResult(bool Succeeded, AuthRow? User, AuthRow? Session, string Code, int StatusCode, string Title)
    {
        public static AuthResult Ok(AuthRow row) => new(true, row, row, "Ok", 200, "Ok");
        public static AuthResult Fail(string code, int statusCode, string title) => new(false, null, null, code, statusCode, title);
    }

    private sealed class NullAccountMessageSink : IAccountMessageSink
    {
        public static NullAccountMessageSink Instance { get; } = new();

        public void Publish(LocalAccountMessage message)
        {
        }
    }

    private static UserState ParseState(string state) =>
        Enum.TryParse<UserState>(state, ignoreCase: false, out var parsed) ? parsed : UserState.Disabled;
}
