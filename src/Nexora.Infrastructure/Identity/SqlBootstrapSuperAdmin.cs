using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Domain.Identity;

namespace Nexora.Infrastructure.Identity;

public sealed class SqlBootstrapSuperAdmin(string connectionString) : IBootstrapSuperAdmin
{
    public async Task<BootstrapOutcome> ExecuteAsync(SqlBootstrapSuperAdminCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.PasswordHash);
        if (command.PasswordHash.Length > 1024)
            throw new ArgumentException("Password hash exceeds its storage contract.", nameof(command));
        if (command.DisplayName.Trim().Length is < 1 or > 100)
            throw new ArgumentException("Display name must contain 1–100 characters.", nameof(command));
        var email = command.Email.Trim();
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email || email.Length > 320)
            throw new ArgumentException("Email is invalid.", nameof(command));
        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(command.TimeZoneId, out _))
            throw new ArgumentException("A known IANA timezone is required.", nameof(command));

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await using var guard = new SqlCommand("""
            SELECT CASE WHEN BootstrapCompletedAt IS NULL THEN 0 ELSE 1 END
            FROM [platform].[SecurityInvariant] WITH (UPDLOCK, HOLDLOCK) WHERE Id = 1;
            """, connection, transaction);
        var closed = await guard.ExecuteScalarAsync(cancellationToken);
        if (closed is null) throw new InvalidOperationException("Security invariant is missing; apply migrations first.");
        if ((int)closed == 1) return BootstrapOutcome.AlreadyBootstrapped;

        await using var existing = new SqlCommand("""
            SELECT COUNT(*) FROM [identity].[UserRole] ur
            JOIN [identity].[Role] r ON r.Id = ur.RoleId WHERE r.Code = 'SuperAdmin';
            """, connection, transaction);
        if ((int)(await existing.ExecuteScalarAsync(cancellationToken))! > 0)
            return BootstrapOutcome.AlreadyBootstrapped;

        await using var insert = new SqlCommand("""
            IF (SELECT COUNT(*) FROM [identity].[Role] WHERE Code IN ('User','SuperAdmin')) <> 2
                THROW 51001, 'Required roles are missing.', 1;
            INSERT INTO [identity].[User]
                (Id, Email, NormalizedEmail, PasswordHash, SecurityStamp, State, EmailConfirmed, VerifiedAt, DisplayName, TimeZoneId, Locale)
            VALUES (@user, @email, @normalized, @hash, @stamp, 'Active', 1, SYSUTCDATETIME(), @name, @zone, 'vi');
            INSERT INTO [platform].[PersonalSpace] (Id, UserId) VALUES (@owner, @user);
            INSERT INTO [identity].[UserRole] (UserId, RoleId)
                SELECT @user, Id FROM [identity].[Role] WHERE Code IN ('User', 'SuperAdmin');
            INSERT INTO [security].[AuditEvent] (ActorUserId, OwnerUserId, ActionKey, TargetType, TargetId, Result)
                VALUES (@user, @user, N'identity.bootstrap.completed', N'User', @user, 'Succeeded');
            UPDATE [platform].[SecurityInvariant]
                SET BootstrapCompletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = 1;
            """, connection, transaction);
        insert.Parameters.Add("@user", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
        insert.Parameters.Add("@owner", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
        insert.Parameters.Add("@email", SqlDbType.NVarChar, 320).Value = email;
        insert.Parameters.Add("@normalized", SqlDbType.NVarChar, 320).Value = EmailNormalizer.Normalize(email);
        insert.Parameters.Add("@hash", SqlDbType.NVarChar, 1024).Value = command.PasswordHash;
        insert.Parameters.Add("@stamp", SqlDbType.NVarChar, 128).Value = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        insert.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = command.DisplayName.Trim();
        insert.Parameters.Add("@zone", SqlDbType.NVarChar, 128).Value = command.TimeZoneId;
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return BootstrapOutcome.Created;
    }
}
