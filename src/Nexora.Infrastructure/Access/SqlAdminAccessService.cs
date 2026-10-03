using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Access;
using Nexora.Application.Identity;
using Nexora.Domain.Access;
using Nexora.Infrastructure.Authorization;
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
    private static readonly TimeSpan PreviewTtl = TimeSpan.FromMinutes(2);
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly byte[] _previewSecret;

    public SqlAdminAccessService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        var secret = idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _receipts = new SqlRequestReceiptStore(secret);
        _previewSecret = SHA256.HashData(Encoding.UTF8.GetBytes("access-preview:" + secret));
        _capabilities = new SqlSelfCapability(_connections);
    }

    public IdentityOperationResult<AdminUserPage> ListUsers(IdentityPrincipal actor, string? query = null, int? limit = null)
    {
        if ((!IsSuperAdmin(actor) && !string.Equals(actor.Role, "Admin", StringComparison.Ordinal)) ||
            !_capabilities.IsAllowed(actor, "FX02", "access.user.read")) return Denied<AdminUserPage>();

        var take = Math.Clamp(limit ?? 25, 1, 100);
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
            ORDER BY u.[NormalizedEmail], u.[Id];
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

    public IdentityOperationResult<AdminAccessPreview> PreviewAccess(IdentityPrincipal actor, Guid userId, AdminAccessChange change)
    {
        if (!Can(actor, "access.change.read")) return Denied<AdminAccessPreview>();
        if (!HasRecentAuthentication(actor))
            return Failure<AdminAccessPreview>("RecentAuthenticationRequired", 428, "Reauthenticate before reviewing an access change.");

        var normalized = NormalizeChange(change);
        var validation = ValidateChange(normalized);
        if (validation is not null) return Failure<AdminAccessPreview>(validation.Value.Code, validation.Value.Status, validation.Value.Title);

        using var connection = _connections.Create();
        connection.Open();
        var target = ReadUser(connection, null, userId, forUpdate: false);
        if (target is null) return Missing<AdminAccessPreview>();

        var blockers = EvaluateAccessBlockers(connection, null, target, normalized);
        var changes = BuildPreviewDiffs(connection, null, target, normalized);
        if (changes.Count == 0)
            return Failure<AdminAccessPreview>("ValidationFailed", 422, "The requested access change does not alter the current state.");

        var expiresAt = DateTimeOffset.UtcNow.Add(PreviewTtl);
        var preview = new AccessPreviewPayload(
            actor.UserId,
            actor.SessionId,
            userId,
            target.ETag,
            normalized.Kind,
            DigestChange(normalized),
            ReadModulePolicyRevisions(connection, null),
            expiresAt);
        return IdentityOperationResult<AdminAccessPreview>.Success(new AdminAccessPreview(
            SignPreview(preview),
            expiresAt,
            target.ETag,
            changes,
            blockers));
    }

    public IdentityOperationResult<AdminUserAccess> SetRole(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminRoleCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.role.set")) return Denied<AdminUserAccess>();
        if (!HasRecentAuthentication(actor)) return RecentAuthRequired<AdminUserAccess>();
        var change = NormalizeChange(new AdminAccessChange("role", command.Role, Array.Empty<AdminPermissionChange>(), Array.Empty<AdminModuleGrantChange>()));
        var validation = ValidateChange(change);
        if (validation is not null) return Failure<AdminUserAccess>(validation.Value.Code, validation.Value.Status, validation.Value.Title);
        if (!TryETag(ifMatch, out var expectedVersion)) return PreconditionRequired<AdminUserAccess>();
        if (!TryReadPreview(command.PreviewToken, actor, userId, change, out var preview)) return PreviewStale<AdminUserAccess>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.role.set", idempotencyKey,
            $"user:{userId:N}|etag:{ifMatch}|change:{DigestChange(change)}|preview:{DigestToken(command.PreviewToken)}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            LockInvariant(connection, transaction);
            if (!Can(connection, transaction, actor, "access.role.set"))
            {
                transaction.Rollback();
                return Denied<AdminUserAccess>();
            }
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            if (!PreviewStillCurrent(connection, transaction, target, preview))
            {
                transaction.Rollback();
                return PreviewStale<AdminUserAccess>();
            }
            var blockers = EvaluateAccessBlockers(connection, transaction, target, change);
            if (blockers.Count > 0)
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>(blockers[0].Code, 409, blockers[0].Message);
            }

            Execute(connection, transaction, "DELETE ur FROM [identity].[UserRole] ur WHERE ur.[UserId] = @UserId;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));
            Execute(connection, transaction, "INSERT INTO [identity].[UserRole] ([UserId], [RoleId]) SELECT @UserId, [Id] FROM [identity].[Role] WHERE [Code] IN ('User', @Role);",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@Role", SqlDbType.VarChar, (object)change.Role!));
            RevokeSessions(connection, transaction, userId);
            Execute(connection, transaction, "UPDATE [identity].[User] SET [SecurityStamp] = @Stamp, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@Stamp", SqlDbType.NVarChar, (object)Convert.ToHexString(RandomNumberGenerator.GetBytes(32))),
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.role.set", traceId);

            // A self-demotion revokes the caller's sessions and removes its
            // authority to read the admin projection. Do not serialize the
            // pre-change privileged DTO into the response or receipt.
            if (userId == actor.UserId && !string.Equals(change.Role, "SuperAdmin", StringComparison.Ordinal))
            {
                CompleteReceipt(connection, transaction, receipt, "RoleUpdated", 204);
                transaction.Commit();
                return IdentityOperationResult<AdminUserAccess>.NoContent("RoleUpdated");
            }

            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            CompleteReceipt(connection, transaction, receipt, "RoleUpdated", 200, JsonSerializer.Serialize(access));
            transaction.Commit();
            return IdentityOperationResult<AdminUserAccess>.Success(access);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<AdminUserAccess>(exception);
        }
    }

    public IdentityOperationResult<AdminUserAccess> SetActionGrant(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminActionGrantCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.permission.set")) return Denied<AdminUserAccess>();
        if (!HasRecentAuthentication(actor)) return RecentAuthRequired<AdminUserAccess>();
        var change = NormalizeChange(new AdminAccessChange("permissions", null, command.Changes, Array.Empty<AdminModuleGrantChange>()));
        var validation = ValidateChange(change);
        if (validation is not null) return Failure<AdminUserAccess>(validation.Value.Code, validation.Value.Status, validation.Value.Title);
        if (!TryETag(ifMatch, out var expectedVersion)) return PreconditionRequired<AdminUserAccess>();
        if (!TryReadPreview(command.PreviewToken, actor, userId, change, out var preview)) return PreviewStale<AdminUserAccess>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.permission.set", idempotencyKey,
            $"user:{userId:N}|etag:{ifMatch}|change:{DigestChange(change)}|preview:{DigestToken(command.PreviewToken)}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            if (!Can(connection, transaction, actor, "access.permission.set"))
            {
                transaction.Rollback();
                return Denied<AdminUserAccess>();
            }
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            if (!PreviewStillCurrent(connection, transaction, target, preview))
            {
                transaction.Rollback();
                return PreviewStale<AdminUserAccess>();
            }
            var blockers = EvaluateAccessBlockers(connection, transaction, target, change);
            if (blockers.Count > 0)
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>(blockers[0].Code, 409, blockers[0].Message);
            }
            foreach (var permissionChange in change.PermissionChanges)
            {
                var permissionId = EnsurePermission(connection, transaction, permissionChange.ActionKey);
                if (permissionChange.Effect == "Unset")
                {
                    Execute(connection, transaction, "DELETE FROM [platform].[AdminPermission] WHERE [UserId] = @UserId AND [PermissionId] = @PermissionId;",
                        ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@PermissionId", SqlDbType.UniqueIdentifier, (object)permissionId));
                    continue;
                }
                Execute(connection, transaction, "MERGE [platform].[AdminPermission] AS target USING (SELECT @UserId AS [UserId], @PermissionId AS [PermissionId]) AS source ON target.[UserId] = source.[UserId] AND target.[PermissionId] = source.[PermissionId] WHEN MATCHED THEN UPDATE SET [Effect] = @Effect, [UpdatedAt] = SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT ([UserId], [PermissionId], [Effect]) VALUES (@UserId, @PermissionId, @Effect);",
                    ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@PermissionId", SqlDbType.UniqueIdentifier, (object)permissionId), ("@Effect", SqlDbType.VarChar, (object)permissionChange.Effect));
            }
            Execute(connection, transaction, "UPDATE [identity].[User] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.permission.set", traceId);
            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            CompleteReceipt(connection, transaction, receipt, "PermissionUpdated", 200, JsonSerializer.Serialize(access));
            transaction.Commit();
            return IdentityOperationResult<AdminUserAccess>.Success(access);
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<AdminUserAccess>(exception);
        }
    }

    public IdentityOperationResult<AdminUserAccess> SetModuleGrant(IdentityPrincipal actor, Guid userId, string? ifMatch, AdminModuleGrantCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!Can(actor, "access.entitlement.set")) return Denied<AdminUserAccess>();
        if (!HasRecentAuthentication(actor)) return RecentAuthRequired<AdminUserAccess>();
        var change = NormalizeChange(new AdminAccessChange("modules", null, Array.Empty<AdminPermissionChange>(), command.Changes));
        var validation = ValidateChange(change);
        if (validation is not null) return Failure<AdminUserAccess>(validation.Value.Code, validation.Value.Status, validation.Value.Title);
        if (!TryETag(ifMatch, out var expectedVersion)) return PreconditionRequired<AdminUserAccess>();
        if (!TryReadPreview(command.PreviewToken, actor, userId, change, out var preview)) return PreviewStale<AdminUserAccess>();

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<AdminUserAccess>(connection, transaction, actor, "access.entitlement.set", idempotencyKey,
            $"user:{userId:N}|etag:{ifMatch}|change:{DigestChange(change)}|preview:{DigestToken(command.PreviewToken)}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            if (!Can(connection, transaction, actor, "access.entitlement.set"))
            {
                transaction.Rollback();
                return Denied<AdminUserAccess>();
            }
            var target = ReadUser(connection, transaction, userId, forUpdate: true);
            if (target is null) { transaction.Rollback(); return Missing<AdminUserAccess>(); }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(target.ETag)))
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>("RevisionConflict", 412, "User account changed.");
            }
            if (!PreviewStillCurrent(connection, transaction, target, preview))
            {
                transaction.Rollback();
                return PreviewStale<AdminUserAccess>();
            }
            var blockers = EvaluateAccessBlockers(connection, transaction, target, change);
            if (blockers.Count > 0)
            {
                transaction.Rollback();
                return Failure<AdminUserAccess>(blockers[0].Code, 409, blockers[0].Message);
            }
            foreach (var moduleChange in change.ModuleChanges)
            {
                Execute(connection, transaction,
                    "MERGE [platform].[UserModuleGrant] AS target USING (SELECT @UserId AS [UserId], @ModuleId AS [ModuleId]) AS source ON target.[UserId] = source.[UserId] AND target.[ModuleId] = source.[ModuleId] WHEN MATCHED THEN UPDATE SET [Enabled] = @Enabled, [UpdatedAt] = SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT ([UserId], [ModuleId], [Enabled]) VALUES (@UserId, @ModuleId, @Enabled);",
                    ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@ModuleId", SqlDbType.UniqueIdentifier, (object)moduleChange.ModuleId), ("@Enabled", SqlDbType.Bit, (object)moduleChange.Enabled));
            }
            Execute(connection, transaction, "UPDATE [identity].[User] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @UserId AND [RowVersion] = @RowVersion;",
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
            WriteAudit(connection, transaction, actor, userId, "access.entitlement.set", traceId);
            var updated = ReadUser(connection, transaction, userId, forUpdate: false);
            if (updated is null) { transaction.Rollback(); return Failure<AdminUserAccess>("PersistenceFailure", 500, "User could not be loaded after update."); }
            var access = new AdminUserAccess(updated, ReadActionGrants(connection, transaction, userId), ReadModuleGrants(connection, transaction, userId));
            CompleteReceipt(connection, transaction, receipt, "ModuleGrantUpdated", 200, JsonSerializer.Serialize(access));
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
        if (!HasRecentAuthentication(actor)) return RecentAuthRequired<object?>();
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
            if (!Can(connection, transaction, actor, "access.user.disable"))
            {
                transaction.Rollback();
                return Denied<object?>();
            }
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
            CompleteReceipt(connection, transaction, receipt, "UserDisabled", 204);
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("UserDisabled");
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private static AdminAccessChange NormalizeChange(AdminAccessChange change)
    {
        var kind = change.Kind?.Trim().ToLowerInvariant() ?? string.Empty;
        var permissions = (change.PermissionChanges ?? Array.Empty<AdminPermissionChange>())
            .Select(item => new AdminPermissionChange(item.ActionKey?.Trim() ?? string.Empty, item.Effect?.Trim() ?? string.Empty))
            .OrderBy(item => item.ActionKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Effect, StringComparer.Ordinal)
            .ToArray();
        var modules = (change.ModuleChanges ?? Array.Empty<AdminModuleGrantChange>())
            .OrderBy(item => item.ModuleId)
            .ToArray();
        return kind switch
        {
            "role" => new AdminAccessChange(kind, change.Role?.Trim(), Array.Empty<AdminPermissionChange>(), Array.Empty<AdminModuleGrantChange>()),
            "permissions" => new AdminAccessChange(kind, null, permissions, Array.Empty<AdminModuleGrantChange>()),
            "modules" => new AdminAccessChange(kind, null, Array.Empty<AdminPermissionChange>(), modules),
            _ => new AdminAccessChange(kind, change.Role?.Trim(), permissions, modules)
        };
    }

    private static (string Code, int Status, string Title)? ValidateChange(AdminAccessChange change)
    {
        switch (change.Kind)
        {
            case "role":
                return change.Role is "User" or "Admin" or "SuperAdmin"
                    ? null
                    : ("ValidationFailed", 422, "Role must be User, Admin or SuperAdmin.");
            case "permissions":
                if (change.PermissionChanges.Count is < 1 or > 100)
                    return ("ValidationFailed", 422, "Provide between one and 100 permission changes.");
                if (change.PermissionChanges.GroupBy(item => item.ActionKey, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
                    return ("ValidationFailed", 422, "Permission changes must not contain duplicate action keys.");
                foreach (var item in change.PermissionChanges)
                {
                    if (string.IsNullOrWhiteSpace(item.ActionKey) || item.ActionKey.Length > 160 || item.Effect is not ("Allow" or "Deny" or "Unset"))
                        return ("ValidationFailed", 422, "Action key and effect are invalid.");
                    var policy = ActionGrantPolicy.CanGrantAllow(item.ActionKey);
                    if (!policy.Allowed) return (policy.Code, 409, policy.Message);
                }
                return null;
            case "modules":
                if (change.ModuleChanges.Count is < 1 or > 100)
                    return ("ValidationFailed", 422, "Provide between one and 100 module changes.");
                if (change.ModuleChanges.Any(item => item.ModuleId == Guid.Empty) ||
                    change.ModuleChanges.GroupBy(item => item.ModuleId).Any(group => group.Count() > 1))
                    return ("ValidationFailed", 422, "Module changes must contain unique valid module identifiers.");
                return null;
            default:
                return ("ValidationFailed", 422, "Access change kind must be role, permissions or modules.");
        }
    }

    private static IReadOnlyList<AdminChangeDiff> BuildPreviewDiffs(SqlConnection connection, SqlTransaction? transaction,
        AdminUserRecord target, AdminAccessChange change)
    {
        var diffs = new List<AdminChangeDiff>();
        switch (change.Kind)
        {
            case "role":
                if (!string.Equals(target.Role, change.Role, StringComparison.Ordinal))
                    diffs.Add(new AdminChangeDiff("role", target.Role, change.Role));
                break;
            case "permissions":
            {
                var current = ReadActionGrants(connection, transaction, target.Id)
                    .ToDictionary(item => item.ActionKey, item => item.Effect, StringComparer.OrdinalIgnoreCase);
                foreach (var item in change.PermissionChanges)
                {
                    var before = current.TryGetValue(item.ActionKey, out var value) ? value : "Unset";
                    if (!string.Equals(before, item.Effect, StringComparison.Ordinal))
                        diffs.Add(new AdminChangeDiff($"permission:{item.ActionKey}", before, item.Effect));
                }
                break;
            }
            case "modules":
            {
                var current = ReadModuleGrants(connection, transaction, target.Id)
                    .ToDictionary(item => item.ModuleId);
                foreach (var item in change.ModuleChanges)
                {
                    if (!current.TryGetValue(item.ModuleId, out var module)) continue;
                    var after = item.Enabled.ToString();
                    if (module.Enabled != item.Enabled)
                        diffs.Add(new AdminChangeDiff($"module:{module.Code}", module.Enabled.ToString(), after));
                }
                break;
            }
        }
        return diffs;
    }

    private static IReadOnlyList<AdminAccessBlocker> EvaluateAccessBlockers(SqlConnection connection, SqlTransaction? transaction,
        AdminUserRecord target, AdminAccessChange change)
    {
        var blockers = new List<AdminAccessBlocker>();
        switch (change.Kind)
        {
            case "role":
                if (target.Role == "SuperAdmin" && change.Role != "SuperAdmin" && ActiveSuperAdminCount(connection, transaction!) <= 1)
                    blockers.Add(new AdminAccessBlocker("LastSuperAdmin", "The last active SuperAdmin cannot be demoted."));
                break;
            case "permissions":
                foreach (var item in change.PermissionChanges)
                {
                    var policy = ActionGrantPolicy.CanGrantAllow(item.ActionKey);
                    if (!policy.Allowed) blockers.Add(new AdminAccessBlocker(policy.Code, policy.Message));
                }
                break;
            case "modules":
            {
                var current = ReadModuleGrants(connection, transaction, target.Id).ToDictionary(item => item.ModuleId);
                var desired = current.ToDictionary(item => item.Key, item => item.Value.Enabled);
                foreach (var item in change.ModuleChanges) desired[item.ModuleId] = item.Enabled;
                foreach (var item in change.ModuleChanges)
                {
                    if (!current.TryGetValue(item.ModuleId, out var module))
                    {
                        blockers.Add(new AdminAccessBlocker("ResourceUnavailable", "Module unavailable."));
                        continue;
                    }
                    if (!item.Enabled) continue;
                    if (module.State != "Ready" || !module.SystemEnabled)
                    {
                        blockers.Add(new AdminAccessBlocker("DecisionBlocked", "A disabled, paused or unavailable module cannot be granted."));
                        continue;
                    }
                    foreach (var dependency in ReadHardDependencies(connection, transaction, target.Id, module.ModuleId))
                    {
                        if (dependency.State != "Ready" || !dependency.SystemEnabled || !desired.GetValueOrDefault(dependency.ModuleId, dependency.Enabled))
                            blockers.Add(new AdminAccessBlocker("DependencyUnavailable", $"Required module {dependency.Code} is not enabled for this account."));
                    }
                }

                foreach (var item in change.ModuleChanges.Where(item => !item.Enabled))
                {
                    foreach (var dependent in ReadHardDependents(connection, transaction, target.Id, item.ModuleId))
                    {
                        if (desired.GetValueOrDefault(dependent.ModuleId, dependent.Enabled))
                            blockers.Add(new AdminAccessBlocker("DependencyEnabled", $"Module {dependent.Code} still requires this module and cannot remain enabled."));
                    }
                }
                break;
            }
        }
        return blockers
            .GroupBy(item => (item.Code, item.Message))
            .Select(group => group.First())
            .ToArray();
    }

    private static IReadOnlyList<ModuleDependencyState> ReadHardDependencies(SqlConnection connection, SqlTransaction? transaction,
        Guid userId, Guid moduleId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH [dependency_chain] AS
            (
                SELECT d.[DependsOnModuleId] AS [ModuleId]
                FROM [platform].[ModuleDependency] d
                WHERE d.[ModuleId] = @ModuleId AND d.[DependencyKind] = 'Hard'
                UNION ALL
                SELECT d.[DependsOnModuleId]
                FROM [dependency_chain] c
                INNER JOIN [platform].[ModuleDependency] d ON d.[ModuleId] = c.[ModuleId]
                WHERE d.[DependencyKind] = 'Hard'
            )
            SELECT DISTINCT m.[Id], m.[Code], m.[State], m.[SystemEnabled], COALESCE(g.[Enabled], CAST(0 AS bit))
            FROM [dependency_chain] c
            INNER JOIN [platform].[Module] m ON m.[Id] = c.[ModuleId]
            LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId
            OPTION (MAXRECURSION 32);
            """;
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        Add(command, "@ModuleId", SqlDbType.UniqueIdentifier, moduleId);
        using var reader = command.ExecuteReader();
        var dependencies = new List<ModuleDependencyState>();
        while (reader.Read()) dependencies.Add(new ModuleDependencyState(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetBoolean(4)));
        return dependencies;
    }

    private static IReadOnlyList<ModuleDependencyState> ReadHardDependents(SqlConnection connection, SqlTransaction? transaction,
        Guid userId, Guid moduleId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH [dependent_chain] AS
            (
                SELECT d.[ModuleId]
                FROM [platform].[ModuleDependency] d
                WHERE d.[DependsOnModuleId] = @ModuleId AND d.[DependencyKind] = 'Hard'
                UNION ALL
                SELECT d.[ModuleId]
                FROM [dependent_chain] c
                INNER JOIN [platform].[ModuleDependency] d ON d.[DependsOnModuleId] = c.[ModuleId]
                WHERE d.[DependencyKind] = 'Hard'
            )
            SELECT DISTINCT m.[Id], m.[Code], m.[State], m.[SystemEnabled], COALESCE(g.[Enabled], CAST(0 AS bit))
            FROM [dependent_chain] c
            INNER JOIN [platform].[Module] m ON m.[Id] = c.[ModuleId]
            LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId
            OPTION (MAXRECURSION 32);
            """;
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        Add(command, "@ModuleId", SqlDbType.UniqueIdentifier, moduleId);
        using var reader = command.ExecuteReader();
        var dependents = new List<ModuleDependencyState>();
        while (reader.Read()) dependents.Add(new ModuleDependencyState(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3), reader.GetBoolean(4)));
        return dependents;
    }

    private static IReadOnlyList<ModulePolicyRevision> ReadModulePolicyRevisions(SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = transaction is null
            ? "SELECT [Id], [PolicyRevision] FROM [platform].[Module] ORDER BY [Id];"
            : "SELECT [Id], [PolicyRevision] FROM [platform].[Module] WITH (UPDLOCK, HOLDLOCK) ORDER BY [Id];";
        using var reader = command.ExecuteReader();
        var revisions = new List<ModulePolicyRevision>();
        while (reader.Read()) revisions.Add(new ModulePolicyRevision(reader.GetGuid(0), reader.GetInt64(1)));
        return revisions;
    }

    private static bool PreviewStillCurrent(SqlConnection connection, SqlTransaction transaction, AdminUserRecord target, AccessPreviewPayload preview)
    {
        if (!string.Equals(target.ETag, preview.TargetETag, StringComparison.Ordinal)) return false;
        var current = ReadModulePolicyRevisions(connection, transaction);
        return current.Count == preview.ModulePolicyRevisions.Count &&
            current.Zip(preview.ModulePolicyRevisions).All(pair => pair.First.ModuleId == pair.Second.ModuleId && pair.First.PolicyRevision == pair.Second.PolicyRevision);
    }

    private string SignPreview(AccessPreviewPayload payload)
    {
        var body = Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload));
        using var hmac = new HMACSHA256(_previewSecret);
        return body + "." + Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private bool TryReadPreview(string? token, IdentityPrincipal actor, Guid targetUserId, AdminAccessChange change,
        out AccessPreviewPayload payload)
    {
        payload = default!;
        var parts = token?.Split('.') ?? Array.Empty<string>();
        if (parts.Length != 2) return false;
        try
        {
            using var hmac = new HMACSHA256(_previewSecret);
            var body = Encoding.UTF8.GetBytes(parts[0]);
            if (!CryptographicOperations.FixedTimeEquals(hmac.ComputeHash(body), FromBase64Url(parts[1]))) return false;
            payload = JsonSerializer.Deserialize<AccessPreviewPayload>(FromBase64Url(parts[0]))!;
            return payload is not null && payload.ExpiresAt > DateTimeOffset.UtcNow &&
                payload.ActorUserId == actor.UserId && payload.ActorSessionId == actor.SessionId &&
                payload.TargetUserId == targetUserId && string.Equals(payload.Kind, change.Kind, StringComparison.Ordinal) &&
                CryptographicOperations.FixedTimeEquals(Convert.FromHexString(payload.ChangeDigest), Convert.FromHexString(DigestChange(change)));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string DigestChange(AdminAccessChange change) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(change)));
    private static string DigestToken(string? token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
    private static bool HasRecentAuthentication(IdentityPrincipal actor) => actor.SessionId is not null && actor.RecentAuthenticatedAt >= DateTimeOffset.UtcNow.AddMinutes(-5);
    private static IdentityOperationResult<T> RecentAuthRequired<T>() => Failure<T>("RecentAuthenticationRequired", 428, "Reauthenticate within five minutes before changing access.");
    private static IdentityOperationResult<T> PreconditionRequired<T>() => Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> PreviewStale<T>() => Failure<T>("PreviewStale", 409, "The access change preview is stale. Review a fresh preview before committing.");

    private sealed record ModuleDependencyState(Guid ModuleId, string Code, string State, bool SystemEnabled, bool Enabled);
    private sealed record ModulePolicyRevision(Guid ModuleId, long PolicyRevision);
    private sealed record AccessPreviewPayload(
        Guid ActorUserId,
        Guid? ActorSessionId,
        Guid TargetUserId,
        string TargetETag,
        string Kind,
        string ChangeDigest,
        IReadOnlyList<ModulePolicyRevision> ModulePolicyRevisions,
        DateTimeOffset ExpiresAt);

    private bool Can(IdentityPrincipal actor, string actionKey) =>
        IsSuperAdmin(actor) && _capabilities.IsAllowed(actor, "FX02", actionKey);

    private bool Can(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string actionKey) =>
        IsSuperAdmin(actor) && _capabilities.IsAllowed(connection, transaction, actor, "FX02", actionKey);

    private static bool IsSuperAdmin(IdentityPrincipal actor) =>
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
        var lockHint = forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty;
        command.CommandText = $"""
            SELECT u.[Id], u.[Email], u.[DisplayName], u.[State], u.[EmailConfirmed],
                COALESCE((SELECT TOP (1) r.[Code] FROM [identity].[UserRole] ur INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId] WHERE ur.[UserId] = u.[Id] ORDER BY CASE r.[Code] WHEN 'SuperAdmin' THEN 3 WHEN 'Admin' THEN 2 ELSE 1 END DESC), 'User'),
                ps.[Id], ps.[State], u.[CreatedAt], u.[UpdatedAt], u.[RowVersion]
            FROM [identity].[User] u {lockHint}
            LEFT JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
            WHERE u.[Id] = @UserId;
            """;
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
        command.CommandText = "SELECT m.[Id], m.[Code], COALESCE(g.[Enabled], CAST(0 AS bit)), m.[State], m.[SystemEnabled] FROM [platform].[Module] m LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId ORDER BY m.[Code];";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        using var reader = command.ExecuteReader();
        var grants = new List<AdminModuleGrantRecord>();
        while (reader.Read()) grants.Add(new AdminModuleGrantRecord(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2), reader.GetString(3), reader.GetBoolean(4)));
        return grants;
    }

    private static Guid EnsurePermission(SqlConnection connection, SqlTransaction transaction, string actionKey)
    {
        using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText = "SELECT [Id] FROM [platform].[Permission] WITH (UPDLOCK, HOLDLOCK) WHERE [ActionKey] = @ActionKey;";
            Add(existing, "@ActionKey", SqlDbType.NVarChar, actionKey, 160);
            if (existing.ExecuteScalar() is Guid permissionId)
            {
                return permissionId;
            }
        }

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus]) OUTPUT inserted.[Id] VALUES (@ActionKey, 'Resolved');";
        Add(insert, "@ActionKey", SqlDbType.NVarChar, actionKey, 160);
        return insert.ExecuteScalar() is Guid createdPermissionId
            ? createdPermissionId
            : throw new InvalidOperationException("Permission could not be created.");
    }

    private static int ActiveSuperAdminCount(SqlConnection connection, SqlTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM [identity].[User] u INNER JOIN [identity].[UserRole] ur ON ur.[UserId] = u.[Id] INNER JOIN [identity].[Role] r ON r.[Id] = ur.[RoleId] WHERE r.[Code] = 'SuperAdmin' AND u.[State] = 'Active' AND u.[IsDeleted] = 0;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void LockInvariant(SqlConnection connection, SqlTransaction transaction) => Execute(connection, transaction, "SELECT [Id] FROM [platform].[SecurityInvariant] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = 1;", Array.Empty<(string, SqlDbType, object)>());

    private static void RevokeSessions(SqlConnection connection, SqlTransaction transaction, Guid userId) => Execute(connection, transaction, "UPDATE [identity].[Session] SET [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()) WHERE [UserId] = @UserId AND [RevokedAt] IS NULL;", ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid userId, string actionKey, string? traceId) => Execute(connection, transaction, "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'identity.User', @Target, 'Succeeded', @TraceId);", ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)userId), ("@Action", SqlDbType.NVarChar, (object)actionKey), ("@Target", SqlDbType.UniqueIdentifier, (object)userId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;

        if (claim.IsReplay)
        {
            if (claim.ResultStatusCode == 204)
                return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent");

            if (!string.IsNullOrWhiteSpace(claim.ResultJson))
            {
                try
                {
                    var value = JsonSerializer.Deserialize<T>(claim.ResultJson);
                    if (value is not null)
                        return IdentityOperationResult<T>.Success(value, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
                }
                catch (JsonException)
                {
                    // A malformed or manually repaired receipt is not a safe
                    // reason to return a current privileged projection.
                }
            }
        }

        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode,
        int? resultStatusCode = null, string? resultJson = null) => _receipts.Complete(connection, transaction, claim, resultCode, resultStatusCode, resultJson);

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
