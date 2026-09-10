using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Access;
using Nexora.Application.Identity;
using Nexora.Domain.Access;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Access;

/// <summary>
/// SQL-backed SuperAdmin account and grant administration. Admin projections
/// never include another user's personal resources. Every mutation locks the
/// target account and the singleton security invariant before committing.
/// </summary>
public sealed class SqlAdminAccessService : IAdminAccessService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;

    public SqlAdminAccessService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<AdminUserPage> ListUsers(IdentityPrincipal actor, string? query = null, int? limit = null)
    {
        if (!Can(actor, "access.user.read")) return Denied<AdminUserPage>();

        var take = Math.Clamp(limit ?? 50, 1, 200);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit)
                u.[Id], u.[Email], u.[DisplayName], u.[State], u.[EmailConfirmed],
                COALESCE((SELECT TOP (1) r.[Code] FROM [identity].[UserRole] ur
                    INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId]
                    WHERE ur.[UserId] = u.[Id]
                    ORDER BY CASE r.[Code] WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC), 'User') AS [Role],
                ps.[Id] AS [PersonalSpaceId], ps.[State] AS [PersonalSpaceState],
                u.[CreatedAt], u.[UpdatedAt], u.[RowVersion]
            FROM [identity].[User] u
            LEFT JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
            WHERE (@Query IS NULL OR u.[NormalizedEmail] LIKE @QueryLike OR u.[DisplayName] LIKE @QueryLike)
            ORDER BY u.[CreatedAt] DESC, u.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@Query", SqlDbType.NVarChar, (object?)normalizedQuery ?? DBNull.Value, 320);
        Add(command, "@QueryLike", SqlDbType.NVarChar, normalizedQuery is null ? DBNull.Value : $"%{normalizedQuery}%", 320);
        using var reader = command.ExecuteReader();
        var users = new List<AdminUserRecord>();
        while (reader.Read()) users.Add(ReadUser(reader));
        return IdentityOperationResult<AdminUserPage>.Success(new AdminUserPage(users, null));
    }

    public IdentityOperationResult<AdminUserAccess> GetUserAccess(IdentityPrincipal actor, Guid userId)
    {
        if (!Can(actor, "access.user.read")) return Denied<AdminUserAccess>();
        using var connection = _connections.Create();
        connection.Open();
        var user = ReadUser(connection, null, userId, forUpdate: false);
        if (user is null) return Missing<AdminUserAccess>();
        return IdentityOperationResult<AdminUserAccess>.Success(new AdminUserAccess(
            user, ReadActionGrants(connection, null, userId), ReadModuleGrants(connection, null, userId)));
    }

    public IdentityOperationResult<AdminUserAccess> SetRole(IdentityPrincipal actor, Guid userId, AdminRoleCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.role.set")) return Denied<AdminUserAccess>();
        if (string.IsNullOrWhiteSpace(command.Role) || command.Role is not ("User" or "Admin" or "SuperAdmin"))
            return Failure<AdminUserAccess>("ValidationFailed", 422, "Role must be User, Admin or SuperAdmin.");
        if (!TryETag(command.IfMatch, out var expectedVersion))
            return Failure<AdminUserAccess>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.role.set", idempotencyKey,
            $"user:{userId:N}|etag:{command.IfMatch}|role:{command.Role}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            LockInvariant(connection, transaction);
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            if (target.Role == "SuperAdmin" && command.Role != "SuperAdmin" && ActiveSuperAdminCount(connection, transaction) <= 1)
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("LastSuperAdmin", 409, "The last active SuperAdmin cannot be demoted.");
            }

            Execute(connection, transaction, "DELETE ur FROM [identity].[UserRole] ur WHERE ur.[UserId] = @UserId;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));
            Execute(connection, transaction, "INSERT INTO [identity].[UserRole] ([UserId], [RoleId]) SELECT @UserId, [Id] FROM [identity].[Role] WHERE [Code] = @Role;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@Role", SqlDbType.VarChar, (object)command.Role));
            RevokeSessions(connection, transaction, userId);
            Execute(connection, transaction, "UPDATE [identity].[User] SET [SecurityStamp] = @Stamp, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@Stamp", SqlDbType.NVarChar, (object)Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.role.set", traceId);
            CompleteReceipt(connection, transaction, receipt, "RoleUpdated");
            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            transaction.Commit();
            return IdentityOperationResult<AdminUserAccess>.Success(access);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<AdminUserAccess>(exception);
        }
    }

    public IdentityOperationResult<AdminUserAccess> SetActionGrant(IdentityPrincipal actor, Guid userId, AdminActionGrantCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.permission.set")) return Denied<AdminUserAccess>();
        if (string.IsNullOrWhiteSpace(command.ActionKey) || command.ActionKey.Length > 160 ||
            command.Effect is not ("Allow" or "Deny"))
            return Failure<AdminUserAccess>("ValidationFailed", 422, "Action key and effect are invalid.");
        var decision = ActionGrantPolicy.CanGrantAllow(command.ActionKey);
        if (command.Effect == "Allow" && !decision.Allowed)
            return Failure<AdminUserAccess>(decision.Code, 409, decision.Message);
        if (!TryETag(command.IfMatch, out var expectedVersion))
            return Failure<AdminUserAccess>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.permission.set", idempotencyKey,
            $"user:{userId:N}|etag:{command.IfMatch}|action:{command.ActionKey}|effect:{command.Effect}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            var permissionId = EnsurePermission(connection, transaction, command.ActionKey);
            Execute(connection, transaction, "MERGE [platform].[AdminPermission] AS target USING (SELECT @UserId AS [UserId], @PermissionId AS [PermissionId]) AS source ON target.[UserId] = source.[UserId] AND target.[PermissionId] = source.[PermissionId] WHEN MATCHED THEN UPDATE SET [Effect] = @Effect, [UpdatedAt] = SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT ([UserId], [PermissionId], [Effect]) VALUES (@UserId, @PermissionId, @Effect);",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@PermissionId", SqlDbType.UniqueIdentifier, (object)permissionId), ("@Effect", SqlDbType.VarChar, (object)command.Effect));
            Execute(connection, transaction, "UPDATE [identity].[User] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.permission.set", traceId);
            CompleteReceipt(connection, transaction, receipt, "PermissionUpdated");
            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            transaction.Commit();
            return IdentityOperationResult<AdminUserAccess>.Success(access);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<AdminUserAccess>(exception);
        }
    }

    public IdentityOperationResult<AdminUserAccess> SetModuleGrant(IdentityPrincipal actor, Guid userId, AdminModuleGrantCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.entitlement.set")) return Denied<AdminUserAccess>();
        if (string.IsNullOrWhiteSpace(command.ModuleCode) || command.ModuleCode.Length > 64)
            return Failure<AdminUserAccess>("ValidationFailed", 422, "Module code is invalid.");
        if (!TryETag(command.IfMatch, out var expectedVersion))
            return Failure<AdminUserAccess>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.entitlement.set", idempotencyKey,
            $"user:{userId:N}|module:{command.ModuleCode}|enabled:{command.Enabled}|etag:{command.IfMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            using var module = connection.CreateCommand();
            module.Transaction = transaction;
            module.CommandText = "SELECT [Id], [State], [SystemEnabled] FROM [platform].[Module] WHERE [Code] = @Code;";
            Add(module, "@Code", SqlDbType.VarChar, command.ModuleCode);
            using var moduleReader = module.ExecuteReader();
            if (!moduleReader.Read())
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("ResourceUnavailable", 404, "Module unavailable.");
            }
            var moduleId = moduleReader.GetGuid(0);
            var moduleState = moduleReader.GetString(1);
            var systemEnabled = moduleReader.GetBoolean(2);
            if (command.Enabled && (moduleState != "Ready" || !systemEnabled))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("DecisionBlocked", 409, "A disabled, paused or unavailable module cannot be granted.");
            }
            moduleReader.Close();
            Execute(connection, transaction,
                "MERGE [platform].[UserModuleGrant] AS target USING (SELECT @UserId AS [UserId], @ModuleId AS [ModuleId]) AS source ON target.[UserId] = source.[UserId] AND target.[ModuleId] = source.[ModuleId] WHEN MATCHED THEN UPDATE SET [Enabled] = @Enabled, [UpdatedAt] = SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT ([UserId], [ModuleId], [Enabled]) VALUES (@UserId, @ModuleId, @Enabled);",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@ModuleId", SqlDbType.UniqueIdentifier, (object)moduleId), ("@Enabled", SqlDbType.Bit, (object)command.Enabled));
            Execute(connection, transaction, "UPDATE [identity].[User] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.entitlement.set", traceId);
            CompleteReceipt(connection, transaction, receipt, "ModuleGrantUpdated");
            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            transaction.Commit();
            return IdentityOperationResult<AdminUserAccess>.Success(access);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<AdminUserAccess>(exception);
        }
    }

    public IdentityOperationResult<object?> DisableUser(IdentityPrincipal actor, Guid userId, AdminDisableCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.user.disable")) return Denied<object?>();
        if (!string.Equals(command.Confirmation?.Trim(), "DISABLE", StringComparison.Ordinal))
            return Failure<object?>("ConfirmationRequired", 422, "Type DISABLE to confirm account suspension.");
        if (!TryETag(command.IfMatch, out var expectedVersion))
            return Failure<object?>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "access.user.disable", idempotencyKey,
            $"user:{userId:N}|etag:{command.IfMatch}|confirmation:DISABLE", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            LockInvariant(connection, transaction);
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<object?>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<object?>("RevisionConflict", 412, "User account changed.");
            }
            if (target.Role == "SuperAdmin" && ActiveSuperAdminCount(connection, transaction) <= 1)
            {
                transaction.Rollback();
                return Failure<object?>("LastSuperAdmin", 409, "The last active SuperAdmin cannot be disabled.");
            }
            Execute(connection, transaction, "UPDATE [identity].[User] SET [State] = 'Disabled', [SecurityStamp] = @Stamp, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@Stamp", SqlDbType.NVarChar, (object)Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            Execute(connection, transaction, "UPDATE [platform].[PersonalSpace] SET [State] = 'Suspended', [UpdatedAt] = SYSUTCDATETIME() WHERE [UserId] = @UserId;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));
            RevokeSessions(connection, transaction, userId);
            WriteAudit(connection, transaction, actor, userId, "access.user.disable", traceId);
            CompleteReceipt(connection, transaction, receipt, "UserDisabled");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private bool Can(IdentityPrincipal actor, string actionKey) =>
        string.Equals(actor.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static IdentityOperationResult<T> Denied<T>() => Failure<T>("PermissionDenied", 403, "Permission denied.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "User account unavailable.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Identity persistence is unavailable.");

    private static AdminUserRecord ReadUser(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4), reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetGuid(6), reader.IsDBNull(7) ? null : reader.GetString(7),
        ToOffset(reader.GetDateTime(8)), ToOffset(reader.GetDateTime(9)), EncodeETag(reader.GetFieldValue<byte[]>(10)));

    private static AdminUserRecord? ReadUser(SqlConnection connection, SqlTransaction? transaction, Guid userId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT u.[Id], u.[Email], u.[DisplayName], u.[State], u.[EmailConfirmed],
                COALESCE((SELECT TOP (1) r.[Code] FROM [identity].[UserRole] ur INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId] WHERE ur.[UserId] = u.[Id] ORDER BY CASE r.[Code] WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC), 'User'),
                ps.[Id], ps.[State], u.[CreatedAt], u.[UpdatedAt], u.[RowVersion]
            FROM [identity].[User] u {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : "WITH (NOLOCK)")}
            LEFT JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
            WHERE u.[Id] = @UserId;""";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    private static IReadOnlyList<AdminActionGrantRecord> ReadActionGrants(SqlConnection connection, SqlTransaction? transaction, Guid userId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT p.[ActionKey], ap.[Effect], p.[EffectiveStatus], ap.[UpdatedAt] FROM [platform].[AdminPermission] ap INNER JOIN [platform].[Permission] p ON p.[Id] = ap.[PermissionId] WHERE ap.[UserId] = @UserId ORDER BY p.[ActionKey];";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        using var reader = command.ExecuteReader();
        var grants = new List<AdminActionGrantRecord>();
        while (reader.Read()) grants.Add(new AdminActionGrantRecord(reader.GetString(0), reader.GetString(1), reader.GetString(2), ToOffset(reader.GetDateTime(3))));
        return grants;
    }

    private static IReadOnlyList<AdminModuleGrantRecord> ReadModuleGrants(SqlConnection connection, SqlTransaction? transaction, Guid userId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT m.[Code], COALESCE(g.[Enabled], 0), m.[State], m.[SystemEnabled] FROM [platform].[Module] m LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId ORDER BY m.[Code];";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        using var reader = command.ExecuteReader();
        var grants = new List<AdminModuleGrantRecord>();
        while (reader.Read()) grants.Add(new AdminModuleGrantRecord(reader.GetString(0), reader.GetBoolean(1), reader.GetString(2), reader.GetBoolean(3)));
        return grants;
    }

    private static Guid EnsurePermission(SqlConnection connection, SqlTransaction transaction, string actionKey)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            MERGE [platform].[Permission] AS target
            USING (SELECT @ActionKey AS [ActionKey]) AS source ON target.[ActionKey] = source.[ActionKey]
            WHEN NOT MATCHED THEN INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved')
            OUTPUT inserted.[Id];
            """;
        Add(command, "@ActionKey", SqlDbType.NVarChar, actionKey, 160);
        return (Guid)command.ExecuteScalar()!;
    }

    private static int ActiveSuperAdminCount(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM [identity].[User] u INNER JOIN [identity].[UserRole] ur ON ur.[UserId] = u.[Id] INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId] WHERE r.[Code] = 'SuperAdmin' AND u.[State] = 'Active' AND u.[IsDeleted] = 0;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void LockInvariant(SqlConnection connection, SqlTransaction transaction) => Execute(connection, transaction, "SELECT [Id] FROM [platform].[SecurityInvariant] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = 1;", Array.Empty<(string, SqlDbType, object)>());

    private static void RevokeSessions(SqlConnection connection, SqlTransaction transaction, Guid userId) => Execute(connection, transaction, "UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()) WHERE [UserId] = @UserId AND [RevokedAt] IS NULL;", ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid userId, string actionKey, string? traceId) => Execute(connection, transaction, "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'identity.User', @Target, 'Succeeded', @TraceId);", ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@Action", SqlDbType.NVarChar, (object)actionKey), ("@Target", SqlDbType.UniqueIdentifier, (object)userId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) => _receipts.Complete(connection, transaction, claim, resultCode);

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type, size);
        parameter.Value = value;
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Trim().StartsWith('"') || !value.Trim().EndsWith('"')) return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }
}
