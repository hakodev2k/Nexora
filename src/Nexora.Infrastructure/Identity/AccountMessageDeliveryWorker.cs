using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Identity;

/// <summary>
/// Local-only delivery worker for the durable pre-activation account-message
/// intent. Claims use READ COMMITTED plus READPAST/UPDLOCK, while a short
/// effect window and the lease fence protect the local side effect from a
/// stale worker. A terminal attempt is explicitly failed after lease recovery.
/// The worker never logs or exposes the decrypted token.
/// </summary>
public sealed class AccountMessageDeliveryWorker : BackgroundService
{
    private const int MaxAttempts = 8;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan EffectLeaseDuration = TimeSpan.FromSeconds(15);
    private readonly SqlConnectionFactory _connections;
    private readonly LocalAccountMessageEnvelopeProtector _protector;
    private readonly IAccountMessageEffectSink _effectSink;
    private readonly ILogger<AccountMessageDeliveryWorker> _logger;

    public AccountMessageDeliveryWorker(
        SqlConnectionFactory connections,
        LocalAccountMessageEnvelopeProtector protector,
        IAccountMessageEffectSink effectSink,
        ILogger<AccountMessageDeliveryWorker> logger)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _effectSink = effectSink ?? throw new ArgumentNullException(nameof(effectSink));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                for (var index = 0; index < 16 && !stoppingToken.IsCancellationRequested; index++)
                {
                    if (!ProcessOne(stoppingToken))
                    {
                        break;
                    }

                    processed = true;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (SqlException)
            {
                _logger.LogWarning("Local account-message delivery is waiting for SQL availability.");
            }
            catch (Exception)
            {
                _logger.LogError("Local account-message delivery encountered a bounded processing failure.");
            }

            await Task.Delay(processed ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private bool ProcessOne(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        // Capture retention is deliberately independent of SQL traffic. This
        // also recovers orphaned temporary files while the app is otherwise
        // idle, without opening or logging their contents.
        _effectSink.SweepExpired(new DateTimeOffset(now, TimeSpan.Zero));
        DeliveryAttempt? attempt;
        var recovered = false;

        using (var connection = _connections.Create())
        {
            connection.Open();
            recovered = _effectSink.ReconcilePending(ResolvePending) > 0;
            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            recovered |= RecoverExhausted(connection, transaction, now) > 0;
            attempt = Claim(connection, transaction, now);
            transaction.Commit();
        }

        if (attempt is null)
        {
            return recovered;
        }

        ProcessAttempt(attempt, now);
        return true;
    }

    private static int RecoverExhausted(SqlConnection connection, SqlTransaction transaction, DateTime now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE [identity].[AccountMessageIntent]
            SET [State] = 'Failed', [LastErrorCode] = 'RetryLimitReached',
                [DeliveryLeaseId] = NULL, [DeliveryLeaseUntil] = NULL,
                [DeliveryEnvelope] = NULL, [UpdatedAt] = @Now
            WHERE [State] IN ('Pending','RetryScheduled')
              AND [Attempts] >= @MaxAttempts
              AND ([DeliveryLeaseUntil] IS NULL OR [DeliveryLeaseUntil] <= @Now);
            """;
        Add(command, "@Now", SqlDbType.DateTime2, now);
        Add(command, "@MaxAttempts", SqlDbType.Int, MaxAttempts);
        return command.ExecuteNonQuery();
    }

    private static DeliveryAttempt? Claim(SqlConnection connection, SqlTransaction transaction, DateTime now)
    {
        var leaseId = Guid.NewGuid();
        var leaseUntil = now.Add(LeaseDuration);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ;WITH candidate AS
            (
                SELECT TOP (1) [Id]
                FROM [identity].[AccountMessageIntent] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                WHERE [State] IN ('Pending','RetryScheduled')
                  AND [NotBeforeAt] <= @Now
                  AND ([DeliveryLeaseId] IS NULL OR [DeliveryLeaseUntil] <= @Now)
                  AND [Attempts] < @MaxAttempts
                ORDER BY [NotBeforeAt], [Id]
            )
            UPDATE intent
            SET [Attempts] = [Attempts] + 1,
                [DeliveryLeaseId] = @LeaseId,
                [DeliveryLeaseUntil] = @LeaseUntil,
                [UpdatedAt] = @Now
            OUTPUT inserted.[Id], inserted.[DeliveryEnvelope], inserted.[Attempts],
                   inserted.[UserId], inserted.[Purpose]
            FROM [identity].[AccountMessageIntent] AS intent
            INNER JOIN candidate ON candidate.[Id] = intent.[Id];
            """;
        Add(command, "@Now", SqlDbType.DateTime2, now);
        Add(command, "@LeaseId", SqlDbType.UniqueIdentifier, leaseId);
        Add(command, "@LeaseUntil", SqlDbType.DateTime2, leaseUntil);
        Add(command, "@MaxAttempts", SqlDbType.Int, MaxAttempts);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new DeliveryAttempt(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetFieldValue<byte[]>(1),
            reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetGuid(3),
            reader.GetString(4),
            leaseId);
    }

    private void ProcessAttempt(DeliveryAttempt attempt, DateTime claimedAt)
    {
        if (attempt.Envelope is null)
        {
            CompleteFailure(attempt, "MissingDeliveryEnvelope");
            return;
        }

        try
        {
            var message = _protector.Unprotect(attempt.Envelope);
            if (message.Id != attempt.Id || message.UserId != attempt.UserId ||
                !string.Equals(message.Purpose, attempt.Purpose, StringComparison.Ordinal))
            {
                CompleteFailure(attempt, "EnvelopeBindingMismatch");
                return;
            }

            if (message.ExpiresAt.UtcDateTime <= claimedAt)
            {
                CompleteNotApplicable(attempt, "TokenExpired");
                return;
            }

            // The envelope is durable so a restart can retry it, but the
            // token/account lifecycle remains authoritative in SQL. A resend,
            // successful verification/reset, expiry, disable or delete must
            // invalidate an already-claimed delivery before the local effect.
            if (!IsCurrentTokenUsable(attempt, DateTime.UtcNow))
            {
                CompleteNotApplicable(attempt, "TokenConsumedOrUnavailable");
                return;
            }

            // A token may cross its expiry boundary between the first check
            // and the local capture. Re-check immediately before the effect so
            // an expired message is terminally disposed rather than retried.
            if (message.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                CompleteNotApplicable(attempt, "TokenExpired");
                return;
            }

            // Reserve a short effect window in SQL. The update is the
            // linearization point for current authority and lease ownership;
            // no SQL transaction is held across file I/O.
            if (!AcquireEffect(attempt, DateTime.UtcNow))
            {
                if (!IsCurrentTokenUsable(attempt, DateTime.UtcNow))
                {
                    CompleteNotApplicable(attempt, "TokenConsumedOrUnavailable");
                }

                return;
            }

            var outcome = _effectSink.Publish(
                message,
                attempt.LeaseId,
                () => IsCurrentEffect(attempt, DateTime.UtcNow));
            if (outcome == LocalAccountMessagePublishOutcome.AuthorityLost)
            {
                if (!IsCurrentTokenUsable(attempt, DateTime.UtcNow))
                {
                    CompleteNotApplicable(attempt, "TokenConsumedOrUnavailable");
                }

                return;
            }

            var completed = CompleteSuccess(attempt);
            if (outcome == LocalAccountMessagePublishOutcome.Prepared)
            {
                if (completed)
                {
                    // SQL Delivered is committed before the capture becomes
                    // visible to the local operator CLI. If the promotion
                    // fails, the next reconciliation pass can finish it.
                    _effectSink.PromoteIfOwned(attempt.Id, attempt.LeaseId);
                }
                else
                {
                    // A competing/recovered worker may have won after the
                    // prepare. Delete only a pending capture carrying this
                    // exact fence.
                    _effectSink.RemoveIfOwned(attempt.Id, attempt.LeaseId);
                }
            }
        }
        catch (InvalidDataException)
        {
            CompleteFailure(attempt, "EnvelopeInvalid");
        }
        catch (CryptographicException)
        {
            CompleteFailure(attempt, "EnvelopeInvalid");
        }
        catch (IOException)
        {
            ScheduleRetryOrFail(attempt, "LocalCaptureUnavailable");
        }
        catch (UnauthorizedAccessException)
        {
            ScheduleRetryOrFail(attempt, "LocalCaptureUnavailable");
        }
        catch (InvalidOperationException)
        {
            ScheduleRetryOrFail(attempt, "LocalDeliveryUnavailable");
        }
    }

    private bool IsCurrentTokenUsable(DeliveryAttempt attempt, DateTime now)
    {
        if (attempt.UserId is null)
        {
            return false;
        }

        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM [identity].[AccountMessageIntent] intent
                INNER JOIN [identity].[OneTimeToken] token
                    ON token.[Id] = intent.[Id]
                   AND token.[UserId] = intent.[UserId]
                   AND token.[Purpose] = intent.[Purpose]
                INNER JOIN [identity].[User] account
                    ON account.[Id] = intent.[UserId]
                LEFT JOIN [platform].[PersonalSpace] space
                    ON space.[UserId] = account.[Id]
                WHERE intent.[Id] = @Id
                  AND intent.[UserId] = @UserId
                  AND token.[ConsumedAt] IS NULL
                  AND token.[ExpiresAt] > @Now
                  AND account.[IsDeleted] = 0
                  AND
                  (
                      (intent.[Purpose] = 'EmailVerification' AND account.[State] = 'PendingVerification')
                      OR (intent.[Purpose] = 'PasswordReset'
                          AND account.[State] = 'Active'
                          AND space.[State] = 'Active')
                  )
            ) THEN 1 ELSE 0 END;
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, attempt.Id);
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, attempt.UserId.Value);
        Add(command, "@Now", SqlDbType.DateTime2, now);
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private LocalAccountMessagePendingDisposition ResolvePending(Guid messageId, Guid effectFence)
    {
        var now = DateTime.UtcNow;
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT [State], [DeliveryLeaseId], [DeliveryLeaseUntil]
            FROM [identity].[AccountMessageIntent]
            WHERE [Id] = @Id;
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, messageId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return LocalAccountMessagePendingDisposition.Remove;
        }

        var state = reader.GetString(0);
        var storedFence = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1);
        var leaseUntil = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
        if (state == "Delivered" && storedFence == effectFence)
        {
            return LocalAccountMessagePendingDisposition.Promote;
        }

        return state is "Pending" or "RetryScheduled" &&
               storedFence == effectFence && leaseUntil > now
            ? LocalAccountMessagePendingDisposition.Keep
            : LocalAccountMessagePendingDisposition.Remove;
    }

    private bool AcquireEffect(DeliveryAttempt attempt, DateTime now)
    {
        var effectUntil = now.Add(EffectLeaseDuration);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE intent
            SET [DeliveryLeaseUntil] = @EffectUntil, [UpdatedAt] = @Now
            FROM [identity].[AccountMessageIntent] AS intent
            WHERE intent.[Id] = @Id
              AND intent.[DeliveryLeaseId] = @LeaseId
              AND intent.[DeliveryLeaseUntil] > @Now
              AND intent.[State] IN ('Pending','RetryScheduled')
              AND EXISTS
              (
                  SELECT 1
                  FROM [identity].[OneTimeToken] token
                  INNER JOIN [identity].[User] account ON account.[Id] = intent.[UserId]
                  LEFT JOIN [platform].[PersonalSpace] space ON space.[UserId] = account.[Id]
                  WHERE token.[Id] = intent.[Id]
                    AND token.[UserId] = intent.[UserId]
                    AND token.[Purpose] = intent.[Purpose]
                    AND token.[ConsumedAt] IS NULL
                    AND token.[ExpiresAt] > @Now
                    AND account.[IsDeleted] = 0
                    AND
                    (
                        (intent.[Purpose] = 'EmailVerification' AND account.[State] = 'PendingVerification')
                        OR (intent.[Purpose] = 'PasswordReset'
                            AND account.[State] = 'Active'
                            AND space.[State] = 'Active')
                    )
              );
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, attempt.Id);
        Add(command, "@LeaseId", SqlDbType.UniqueIdentifier, attempt.LeaseId);
        Add(command, "@Now", SqlDbType.DateTime2, now);
        Add(command, "@EffectUntil", SqlDbType.DateTime2, effectUntil);
        var acquired = command.ExecuteNonQuery() == 1;
        transaction.Commit();
        return acquired;
    }

    private bool IsCurrentEffect(DeliveryAttempt attempt, DateTime now)
    {
        if (attempt.UserId is null)
        {
            return false;
        }

        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM [identity].[AccountMessageIntent] intent
                INNER JOIN [identity].[OneTimeToken] token
                    ON token.[Id] = intent.[Id]
                   AND token.[UserId] = intent.[UserId]
                   AND token.[Purpose] = intent.[Purpose]
                INNER JOIN [identity].[User] account ON account.[Id] = intent.[UserId]
                LEFT JOIN [platform].[PersonalSpace] space ON space.[UserId] = account.[Id]
                WHERE intent.[Id] = @Id
                  AND intent.[UserId] = @UserId
                  AND intent.[DeliveryLeaseId] = @LeaseId
                  AND intent.[DeliveryLeaseUntil] > @Now
                  AND intent.[State] IN ('Pending','RetryScheduled')
                  AND token.[ConsumedAt] IS NULL
                  AND token.[ExpiresAt] > @Now
                  AND account.[IsDeleted] = 0
                  AND
                  (
                      (intent.[Purpose] = 'EmailVerification' AND account.[State] = 'PendingVerification')
                      OR (intent.[Purpose] = 'PasswordReset'
                          AND account.[State] = 'Active'
                          AND space.[State] = 'Active')
                  )
            ) THEN 1 ELSE 0 END;
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, attempt.Id);
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, attempt.UserId.Value);
        Add(command, "@LeaseId", SqlDbType.UniqueIdentifier, attempt.LeaseId);
        Add(command, "@Now", SqlDbType.DateTime2, now);
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private bool CompleteSuccess(DeliveryAttempt attempt) =>
        UpdateState(attempt, "Delivered", null, null, clearEnvelope: true, preserveLease: true);

    private bool CompleteNotApplicable(DeliveryAttempt attempt, string errorCode) =>
        UpdateState(attempt, "NotApplicable", errorCode, null, clearEnvelope: true);

    private bool CompleteFailure(DeliveryAttempt attempt, string errorCode) =>
        UpdateState(attempt, "Failed", errorCode, null, clearEnvelope: true);

    private void ScheduleRetryOrFail(DeliveryAttempt attempt, string errorCode)
    {
        if (attempt.Attempts >= MaxAttempts)
        {
            CompleteFailure(attempt, "RetryLimitReached");
            return;
        }

        var delaySeconds = Math.Min(300, Math.Pow(2, Math.Max(0, attempt.Attempts - 1)));
        UpdateState(attempt, "RetryScheduled", errorCode, DateTime.UtcNow.AddSeconds(delaySeconds), clearEnvelope: false);
    }

    private bool UpdateState(
        DeliveryAttempt attempt,
        string state,
        string? errorCode,
        DateTime? notBeforeAt,
        bool clearEnvelope,
        bool preserveLease = false)
    {
        var now = DateTime.UtcNow;
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE [identity].[AccountMessageIntent]
            SET [State] = @State, [LastErrorCode] = @ErrorCode,
                [NotBeforeAt] = COALESCE(@NotBeforeAt, [NotBeforeAt]),
                [DeliveryLeaseId] = {(preserveLease ? "[DeliveryLeaseId]" : "NULL")}, [DeliveryLeaseUntil] = NULL,
                [DeliveryEnvelope] = {(clearEnvelope ? "NULL" : "[DeliveryEnvelope]")},
                [UpdatedAt] = @UpdatedAt
            WHERE [Id] = @Id AND [DeliveryLeaseId] = @LeaseId
              AND [DeliveryLeaseUntil] > @UpdatedAt
              AND [State] IN ('Pending','RetryScheduled');
            """;
        Add(command, "@State", SqlDbType.VarChar, state, 32);
        Add(command, "@ErrorCode", SqlDbType.NVarChar, (object?)errorCode ?? DBNull.Value, 64);
        Add(command, "@NotBeforeAt", SqlDbType.DateTime2, (object?)notBeforeAt ?? DBNull.Value);
        Add(command, "@UpdatedAt", SqlDbType.DateTime2, now);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, attempt.Id);
        Add(command, "@LeaseId", SqlDbType.UniqueIdentifier, attempt.LeaseId);
        return command.ExecuteNonQuery() == 1;
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = size > 0 ? command.Parameters.Add(name, type, size) : command.Parameters.Add(name, type);
        parameter.Value = value;
    }

    private sealed record DeliveryAttempt(Guid Id, byte[]? Envelope, int Attempts, Guid? UserId, string Purpose, Guid LeaseId);
}
