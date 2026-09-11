using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Support;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Support;

/// <summary>
/// Owner consent and narrowly scoped support sessions. A support actor must
/// have a resolved AdminPermission and a current, module-specific owner grant.
/// This service intentionally contains no business-data read surface.
/// </summary>
public sealed class SqlSupportService : ISupportService
{
    private static readonly HashSet<string> NonSupportableModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "FX01", "FX02", "FX03", "FX04", "FX05", "FX06", "FX07", "FX08", "FX09", "FX10", "FX27", "FX28", "FX30", "FX34", "FX35", "FX37", "FX38", "FX39", "FX40"
    };

    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlSupportService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<SupportGrantPage> ListGrants(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX05", "support.consent.read")) return ModuleUnavailable<SupportGrantPage>();
        var take = Math.Clamp(limit ?? 50, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) g.[Id], m.[Code], g.[DurationMode], g.[ExpiresAt], g.[RevokedAt],
                   g.[CreatedAt], g.[UpdatedAt], g.[RowVersion],
                   CASE WHEN g.[RevokedAt] IS NULL
                              AND (g.[ExpiresAt] IS NULL OR g.[ExpiresAt] > SYSUTCDATETIME())
                              AND m.[State] = 'Ready' AND m.[SystemEnabled] = 1 AND m.[RegistrationEnabled] = 1
                              AND EXISTS
                              (
                                  SELECT 1
                                  FROM [platform].[PersonalSpace] ownerSpace
                                  INNER JOIN [platform].[UserModuleGrant] ownerGrant
                                    ON ownerGrant.[UserId] = ownerSpace.[UserId] AND ownerGrant.[ModuleId] = g.[ModuleId]
                                  WHERE ownerSpace.[Id] = g.[OwnerId] AND ownerSpace.[State] = 'Active' AND ownerGrant.[Enabled] = 1
                              ) THEN CONVERT(bit, 1)
                        ELSE CONVERT(bit, 0) END
            FROM [security].[SupportGrant] g
            INNER JOIN [platform].[Module] m ON m.[Id] = g.[ModuleId]
            WHERE g.[OwnerId] = @OwnerId
            ORDER BY g.[UpdatedAt] DESC, g.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        var items = new List<SupportGrantRecord>();
        while (reader.Read()) items.Add(ReadGrant(reader));
        return IdentityOperationResult<SupportGrantPage>.Success(new SupportGrantPage(items, null));
    }

    public IdentityOperationResult<SupportGrantRecord> Grant(IdentityPrincipal actor, SupportGrantCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX05", "support.consent.grant")) return ModuleUnavailable<SupportGrantRecord>();
        var validation = ValidateGrant(command);
        if (validation is not null) return validation;
        var moduleCode = command.ModuleCode.Trim();
        var duration = command.DurationMode.Trim();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = duration switch
        {
            "24Hours" => now.AddHours(24),
            "Custom" => command.ExpiresAt!.Value,
            _ => (DateTimeOffset?)null
        };

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<SupportGrantRecord>(connection, transaction, actor,
            "support.consent.grant", idempotencyKey,
            $"module:{moduleCode}|duration:{duration}|expires:{expiresAt?.UtcDateTime:O}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }

        var module = ReadModule(connection, transaction, moduleCode, forUpdate: true);
        if (module is null || !module.Value.Available || NonSupportableModules.Contains(moduleCode) || ReadActionForModule(moduleCode) is null)
        {
            transaction.Rollback();
            return Failure<SupportGrantRecord>("ModuleUnavailable", 409, "The selected module is unavailable for support consent.");
        }

        var grantId = Guid.NewGuid();
        Execute(connection, transaction, """
            INSERT INTO [security].[SupportGrant]
                ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [ModuleId], [DurationMode], [ExpiresAt], [ConsentVersion])
            VALUES (@Id, @OwnerId, @Actor, @Actor, @ModuleId, @DurationMode, @ExpiresAt, 'support-v1');
            """,
            ("@Id", SqlDbType.UniqueIdentifier, (object)grantId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@ModuleId", SqlDbType.UniqueIdentifier, (object)module.Value.Id),
            ("@DurationMode", SqlDbType.VarChar, (object)duration),
            ("@ExpiresAt", SqlDbType.DateTime2, (object?)expiresAt?.UtcDateTime ?? DBNull.Value));
        WriteAudit(connection, transaction, actor, grantId, "support.consent.grant", traceId);
        var created = ReadGrant(connection, transaction, actor.OwnerId, grantId, forUpdate: false);
        if (created is null)
        {
            transaction.Rollback();
            return PersistenceFailure<SupportGrantRecord>();
        }
        CompleteReceipt(connection, transaction, receipt, "SupportConsentGranted");
        transaction.Commit();
        return IdentityOperationResult<SupportGrantRecord>.Success(created, 201, "SupportConsentGranted");
    }

    public IdentityOperationResult<object?> RevokeGrant(IdentityPrincipal actor, Guid grantId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX05", "support.consent.revoke")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "support.consent.revoke", idempotencyKey, $"grant:{grantId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var current = ReadGrant(connection, transaction, actor.OwnerId, grantId, forUpdate: true);
        if (current is null)
        {
            transaction.Rollback();
            return Missing<object?>();
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return Revision<object?>();
        }
        Execute(connection, transaction, """
            UPDATE [security].[SupportGrant]
            SET [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()), [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @Actor
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RevokedAt] IS NULL AND [RowVersion] = @RowVersion;
            UPDATE [security].[AccessSession]
            SET [EndedAt] = COALESCE([EndedAt], SYSUTCDATETIME())
            WHERE [SupportGrantId] = @Id AND [OwnerId] = @OwnerId AND [EndedAt] IS NULL;
            """,
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Id", SqlDbType.UniqueIdentifier, (object)grantId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, grantId, "support.consent.revoke", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<SupportSessionPage> ListSessions(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX05", "support.session.end")) return ModuleUnavailable<SupportSessionPage>();
        var take = Math.Clamp(limit ?? 50, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) s.[Id], s.[TargetUserId], m.[Code], s.[Mode], s.[ExpiresAt], s.[EndedAt],
                   s.[SupportGrantId], s.[Reason], s.[CreatedAt], s.[RowVersion]
            FROM [security].[AccessSession] s
            INNER JOIN [platform].[Module] m ON m.[Id] = s.[ModuleId]
            WHERE s.[OwnerId] = @OwnerId OR (s.[ActorUserId] = @ActorUserId AND s.[Mode] = 'Support')
            ORDER BY s.[CreatedAt] DESC, s.[Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId);
        using var reader = command.ExecuteReader();
        var items = new List<SupportSessionRecord>();
        while (reader.Read()) items.Add(ReadSession(reader));
        return IdentityOperationResult<SupportSessionPage>.Success(new SupportSessionPage(items, null));
    }

    public IdentityOperationResult<SupportSessionRecord> OpenSupport(IdentityPrincipal actor, SupportSessionOpenCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!IsAdmin(actor)) return Denied<SupportSessionRecord>();
        if (!HasAdminPermission(actor.UserId, "support.session.open")) return Denied<SupportSessionRecord>();
        if (command.TargetUserId == Guid.Empty || string.IsNullOrWhiteSpace(command.ModuleCode) || command.ModuleCode.Trim().Length > 64)
            return Failure<SupportSessionRecord>("ValidationFailed", 422, "Support target and module are required.");
        if (command.TargetUserId == actor.UserId)
            return Failure<SupportSessionRecord>("ValidationFailed", 422, "A support session cannot target the current administrator.");

        var moduleCode = command.ModuleCode.Trim();
        var now = DateTimeOffset.UtcNow;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<SupportSessionRecord>(connection, transaction, actor,
            "support.session.open", idempotencyKey,
            $"target:{command.TargetUserId:N}|module:{moduleCode}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }

        var module = ReadModule(connection, transaction, moduleCode, forUpdate: true);
        var sourceReadAction = ReadActionForModule(moduleCode);
        if (module is null || !module.Value.Available || NonSupportableModules.Contains(moduleCode) || sourceReadAction is null ||
            !ModuleAvailable(connection, transaction, actor, moduleCode, sourceReadAction))
        {
            transaction.Rollback();
            return Failure<SupportSessionRecord>("ModuleUnavailable", 409, "The selected module is unavailable for support.");
        }
        var target = ReadTarget(connection, transaction, command.TargetUserId, forUpdate: true);
        if (target is null)
        {
            transaction.Rollback();
            return Failure<SupportSessionRecord>("ResourceUnavailable", 404, "Support target or module unavailable.");
        }
        var grant = ReadActiveGrant(connection, transaction, target.Value.OwnerId, module.Value.Id, now, forUpdate: true);
        if (grant is null)
        {
            transaction.Rollback();
            return Failure<SupportSessionRecord>("PermissionDenied", 403, "Current owner consent is required for this module.");
        }

        var sessionId = Guid.NewGuid();
        var expiresAt = grant.Value.ExpiresAt is { } grantExpiry && grantExpiry < now.AddHours(24) ? grantExpiry : now.AddHours(24);
        var auditId = WriteAudit(connection, transaction, actor, sessionId, "support.session.open", traceId, target.Value.OwnerUserId);
        Execute(connection, transaction, """
            INSERT INTO [security].[AccessSession]
                ([Id], [OwnerId], [ActorUserId], [TargetUserId], [ModuleId], [Mode], [SupportGrantId], [ExpiresAt], [OpeningAuditId])
            VALUES (@Id, @OwnerId, @Actor, @Target, @ModuleId, 'Support', @GrantId, @ExpiresAt, @AuditId);
            """,
            ("@Id", SqlDbType.UniqueIdentifier, (object)sessionId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)target.Value.OwnerId),
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Target", SqlDbType.UniqueIdentifier, (object)command.TargetUserId),
            ("@ModuleId", SqlDbType.UniqueIdentifier, (object)module.Value.Id),
            ("@GrantId", SqlDbType.UniqueIdentifier, (object)grant.Value.Id),
            ("@ExpiresAt", SqlDbType.DateTime2, (object)expiresAt.UtcDateTime),
            ("@AuditId", SqlDbType.UniqueIdentifier, (object)auditId));
        var session = ReadSession(connection, transaction, sessionId, target.Value.OwnerId);
        if (session is null)
        {
            transaction.Rollback();
            return PersistenceFailure<SupportSessionRecord>();
        }
        CompleteReceipt(connection, transaction, receipt, "SupportSessionOpened");
        transaction.Commit();
        return IdentityOperationResult<SupportSessionRecord>.Success(session, 201, "SupportSessionOpened");
    }

    public IdentityOperationResult<SupportSessionRecord> OpenEmergency(IdentityPrincipal actor, EmergencyOpenCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!string.Equals(actor.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)) return Denied<SupportSessionRecord>();
        if (!HasAdminPermission(actor.UserId, "support.emergency.open")) return Denied<SupportSessionRecord>();
        if (command.TargetUserId == Guid.Empty || string.IsNullOrWhiteSpace(command.ModuleCode) || command.ModuleCode.Trim().Length > 64)
            return Failure<SupportSessionRecord>("ValidationFailed", 422, "Emergency target and module are required.");
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length is < 20 or > 1000)
            return Failure<SupportSessionRecord>("ValidationFailed", 422, "Emergency reason must be between 20 and 1,000 characters.");

        // Q-02 has not approved the duration/recent-auth contract. This is a
        // safe shell: no target, module or business data is read and nothing
        // is written until the owner decision is resolved.
        return Failure<SupportSessionRecord>("DecisionBlocked", 409,
            "Emergency access is blocked pending the approved duration and recent-auth decision.");
    }

    public IdentityOperationResult<object?> EndSession(IdentityPrincipal actor, Guid sessionId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX05", "support.session.end")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "support.session.end", idempotencyKey, $"session:{sessionId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var current = ReadSession(connection, transaction, sessionId, ownerId: null, forUpdate: true);
        if (current is null || (current.Value.TargetUserId != actor.UserId && !SessionActor(connection, transaction, sessionId, actor.UserId)))
        {
            transaction.Rollback();
            return Missing<object?>();
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.Value.ETag)))
        {
            transaction.Rollback();
            return Revision<object?>();
        }
        Execute(connection, transaction, "UPDATE [security].[AccessSession] SET [EndedAt] = COALESCE([EndedAt], SYSUTCDATETIME()) WHERE [Id] = @Id AND [RowVersion] = @RowVersion AND ([OwnerId] = @OwnerId OR [ActorUserId] = @ActorUserId);",
            ("@Id", SqlDbType.UniqueIdentifier, (object)sessionId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@ActorUserId", SqlDbType.UniqueIdentifier, (object)actor.UserId));
        WriteAudit(connection, transaction, actor, sessionId, "support.session.end", traceId, current.Value.TargetUserId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    private static IdentityOperationResult<SupportGrantRecord>? ValidateGrant(SupportGrantCommand command)
    {
        var duration = command.DurationMode?.Trim();
        if (string.IsNullOrWhiteSpace(command.ModuleCode) || command.ModuleCode.Trim().Length > 64 ||
            duration is not ("24Hours" or "Custom" or "UntilRevoked"))
            return Failure<SupportGrantRecord>("ValidationFailed", 422, "Support module and duration are invalid.");
        if (duration == "Custom")
        {
            if (command.ExpiresAt is not { } customExpiry)
                return Failure<SupportGrantRecord>("ValidationFailed", 422, "Custom support consent requires an expiry.");
            if (customExpiry <= DateTimeOffset.UtcNow)
                return Failure<SupportGrantRecord>("ValidationFailed", 422, "Custom support consent expiry must be in the future.");
        }
        if (duration == "UntilRevoked" && command.ExpiresAt is not null)
            return Failure<SupportGrantRecord>("ValidationFailed", 422, "UntilRevoked consent cannot include an expiry.");
        return null;
    }

    private bool HasAdminPermission(Guid userId, string actionKey)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN p.[EffectiveStatus] = 'Resolved' AND ap.[Effect] = 'Allow' THEN 1 ELSE 0 END
            FROM [platform].[Permission] p
            LEFT JOIN [platform].[AdminPermission] ap ON ap.[PermissionId] = p.[Id] AND ap.[UserId] = @UserId
            WHERE p.[ActionKey] = @ActionKey;
            """;
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        Add(command, "@ActionKey", SqlDbType.NVarChar, actionKey, 160);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static (Guid Id, bool Available)? ReadModule(SqlConnection connection, SqlTransaction transaction, string code, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [State], [SystemEnabled], [RegistrationEnabled] FROM [platform].[Module] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Code] = @Code;";
        Add(command, "@Code", SqlDbType.VarChar, code, 64);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetGuid(0), reader.GetString(1) == "Ready" && reader.GetBoolean(2) && reader.GetBoolean(3));
    }

    private static (Guid OwnerId, Guid OwnerUserId)? ReadTarget(SqlConnection connection, SqlTransaction transaction, Guid userId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT ps.[Id], u.[Id]
            FROM [identity].[User] u {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [platform].[PersonalSpace] ps ON ps.[UserId] = u.[Id]
            WHERE u.[Id] = @UserId AND u.[State] = 'Active' AND u.[IsDeleted] = 0
              AND u.[EmailConfirmed] = 1 AND ps.[State] = 'Active';
            """;
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, userId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetGuid(0), reader.GetGuid(1)) : null;
    }

    private static (Guid Id, DateTimeOffset? ExpiresAt)? ReadActiveGrant(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, Guid moduleId, DateTimeOffset now, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT g.[Id], g.[ExpiresAt]
            FROM [security].[SupportGrant] g {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            WHERE g.[OwnerId] = @OwnerId AND g.[ModuleId] = @ModuleId AND g.[RevokedAt] IS NULL
              AND (g.[ExpiresAt] IS NULL OR g.[ExpiresAt] > @Now)
              AND EXISTS (SELECT 1 FROM [platform].[UserModuleGrant] umg WHERE umg.[UserId] = (SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = g.[OwnerId]) AND umg.[ModuleId] = g.[ModuleId] AND umg.[Enabled] = 1)
            ORDER BY g.[CreatedAt] DESC;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ModuleId", SqlDbType.UniqueIdentifier, moduleId);
        Add(command, "@Now", SqlDbType.DateTime2, now.UtcDateTime);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetGuid(0), reader.IsDBNull(1) ? null : ToOffset(reader.GetDateTime(1))) : null;
    }

    private static SupportGrantRecord? ReadGrant(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid id, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT g.[Id], m.[Code], g.[DurationMode], g.[ExpiresAt], g.[RevokedAt], g.[CreatedAt], g.[UpdatedAt], g.[RowVersion],
                   CASE WHEN g.[RevokedAt] IS NULL AND (g.[ExpiresAt] IS NULL OR g.[ExpiresAt] > SYSUTCDATETIME())
                              AND m.[State] = 'Ready' AND m.[SystemEnabled] = 1 AND m.[RegistrationEnabled] = 1
                              AND EXISTS
                              (
                                  SELECT 1
                                  FROM [platform].[PersonalSpace] ownerSpace
                                  INNER JOIN [platform].[UserModuleGrant] ownerGrant
                                    ON ownerGrant.[UserId] = ownerSpace.[UserId] AND ownerGrant.[ModuleId] = g.[ModuleId]
                                  WHERE ownerSpace.[Id] = g.[OwnerId] AND ownerSpace.[State] = 'Active' AND ownerGrant.[Enabled] = 1
                              ) THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END
            FROM [security].[SupportGrant] g {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [platform].[Module] m ON m.[Id] = g.[ModuleId]
            WHERE g.[Id] = @Id AND g.[OwnerId] = @OwnerId;
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadGrant(reader) : null;
    }

    private static SupportGrantRecord ReadGrant(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : ToOffset(reader.GetDateTime(3)), reader.IsDBNull(4) ? null : ToOffset(reader.GetDateTime(4)),
        reader.GetBoolean(8), ToOffset(reader.GetDateTime(5)), ToOffset(reader.GetDateTime(6)), EncodeETag(reader.GetFieldValue<byte[]>(7)));

    private static SupportSessionRecord? ReadSession(SqlConnection connection, SqlTransaction transaction, Guid id, Guid? ownerId,
        bool forUpdate = false)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT s.[Id], s.[TargetUserId], m.[Code], s.[Mode], s.[ExpiresAt], s.[EndedAt], s.[SupportGrantId], s.[Reason], s.[CreatedAt], s.[RowVersion]
            FROM [security].[AccessSession] s {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : string.Empty)}
            INNER JOIN [platform].[Module] m ON m.[Id] = s.[ModuleId]
            WHERE s.[Id] = @Id AND (@OwnerId IS NULL OR s.[OwnerId] = @OwnerId);
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, (object?)ownerId ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadSession(reader) : null;
    }

    private static SupportSessionRecord ReadSession(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
        ToOffset(reader.GetDateTime(4)), reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), reader.IsDBNull(6) ? null : reader.GetGuid(6),
        reader.IsDBNull(7) ? null : reader.GetString(7), ToOffset(reader.GetDateTime(8)), EncodeETag(reader.GetFieldValue<byte[]>(9)));

    private static bool SessionActor(SqlConnection connection, SqlTransaction transaction, Guid sessionId, Guid actorUserId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM [security].[AccessSession] WHERE [Id] = @Id AND [ActorUserId] = @Actor;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, sessionId);
        Add(command, "@Actor", SqlDbType.UniqueIdentifier, actorUserId);
        return command.ExecuteScalar() is not null;
    }

    private static bool IsAdmin(IdentityPrincipal actor) =>
        string.Equals(actor.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(actor.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static string? ReadActionForModule(string moduleCode) => moduleCode.ToUpperInvariant() switch
    {
        "FX11" => "projects.project.read",
        "FX12" => "tasks.task.read",
        "FX13" => "calendar.event.read",
        "FX15" => "planner.plan.read",
        "FX16" => "goals.goal.read",
        "FX20" => "documents.page.read",
        "FX21" => "bookmarks.bookmark.read",
        "FX22" => "snippets.snippet.read",
        "FX23" => "reading.queue.read",
        "FX24" => "organization.tag.read",
        "FX25" => "discovery.search.query",
        "FX26" => "dashboard.dashboard.read",
        "FX32" => "toolbox.catalog.read",
        _ => null
    };

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) => _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private bool ModuleAvailable(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string moduleCode, params string[] actionKeys) => _capabilities.IsAllowed(connection, transaction, actor, moduleCode, actionKeys);

    private static Guid WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId,
        string action, string? traceId, Guid? ownerUserId = null)
    {
        var auditId = Guid.NewGuid();
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([Id], [ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Id, @Actor, @Owner, @Action, N'security.Access', @Target, 'Succeeded', @TraceId);",
            ("@Id", SqlDbType.UniqueIdentifier, (object)auditId),
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Owner", SqlDbType.UniqueIdentifier, (object?)(ownerUserId ?? actor.UserId) ?? DBNull.Value),
            ("@Action", SqlDbType.NVarChar, (object)action),
            ("@Target", SqlDbType.UniqueIdentifier, (object)targetId),
            ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));
        return auditId;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
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
        var parameter = command.Parameters.Add(name, type);
        if (size > 0) parameter.Size = size;
        parameter.Value = value;
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim().Trim('"'));
    private static bool TryDecodeETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            bytes = DecodeETag(value);
            return bytes.Length == 8;
        }
        catch (FormatException) { return false; }
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Support is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Denied<T>() => Failure<T>("PermissionDenied", 403, "Permission denied.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Support resource unavailable.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Support resource revision changed.");
    private static IdentityOperationResult<T> Precondition<T>(string? ifMatch) => Failure<T>(string.IsNullOrWhiteSpace(ifMatch) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(ifMatch) ? 428 : 412, string.IsNullOrWhiteSpace(ifMatch) ? "If-Match is required." : "If-Match is invalid.");
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "Support persistence is unavailable.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
}
