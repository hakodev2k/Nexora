using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Sharing;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Nexora.Infrastructure.Documents;

namespace Nexora.Infrastructure.Sharing;

/// <summary>
/// SQL authority for read-only sharing. The raw capability is returned only
/// once at creation; all later lookups use its digest, the current sharing
/// epoch, source ownership/state and an explicit viewer policy.
/// </summary>
public sealed class SqlSharingService : ISharingService
{
    private const string ProjectionVersion = "v1";
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlSharingService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<bool> CanCopyCreated(IdentityPrincipal actor, Guid shareLinkId)
    {
        if (!ModuleAvailable(actor, "FX04", "sharing.link.create") || !ModuleAvailable(actor, "FX04", "sharing.link.read"))
            return ModuleUnavailable<bool>();
        using var connection = _connections.Create(); connection.Open();
        if (!SqlCurrentActor.IsLive(connection, null, actor)) return ModuleUnavailable<bool>();
        var advisory = ReadLink(connection, null, actor.OwnerId, shareLinkId);
        if (advisory is null) return Missing<bool>();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var module = ReadSharingModule(connection, transaction, forUpdate: false);
        if (!SqlCurrentActor.IsLive(connection, transaction, actor) || module is null || !module.Value.Available ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.create") ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.read"))
            return ModuleUnavailable<bool>();
        // Preserve source-before-link lock ordering used by source lifecycle operations.
        if (!OwnerSourceAllowed(connection, transaction, actor, advisory.ResourceType, advisory.ResourceId, creating: false))
            return Missing<bool>();
        var source = ReadSource(connection, transaction, advisory.ResourceType, advisory.ResourceId, actor.OwnerId);
        if (source is null || (advisory.ResourceType == "Document" && source.Value.Status is not ("Published" or "Archived")))
            return Missing<bool>();
        var current = ReadLink(connection, transaction, actor.OwnerId, shareLinkId);
        if (current is null || !current.IsActive || current.ProjectionVersion != ProjectionVersion || current.ResourceType != advisory.ResourceType ||
            current.ResourceId != advisory.ResourceId || !LinkEpochCurrent(connection, transaction, shareLinkId, actor.OwnerId))
            return Missing<bool>();
        if (!SqlCurrentActor.IsLive(connection, transaction, actor)) return ModuleUnavailable<bool>();
        transaction.Commit();
        // Never return or reconstruct the capability token; copying remains a local operation.
        return IdentityOperationResult<bool>.Success(true);
    }

    public IdentityOperationResult<ShareLinkPage> List(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor,"FX04","sharing.link.read")) return ModuleUnavailable<ShareLinkPage>();
        var take=Math.Clamp(limit ?? 50,1,100);
        using var connection=_connections.Create(); connection.Open();
        if (!SqlCurrentActor.IsLive(connection,null,actor)) return ModuleUnavailable<ShareLinkPage>();
        var advisory=ReadListAdvisories(connection,actor.OwnerId,take);
        using var transaction=connection.BeginTransaction(IsolationLevel.Serializable);
        var module=ReadSharingModule(connection,transaction,forUpdate:false);
        if (!SqlCurrentActor.IsLive(connection,transaction,actor) || module is null || !module.Value.Available ||
            !ModuleAvailable(connection,transaction,actor,"FX04","sharing.link.read")) return ModuleUnavailable<ShareLinkPage>();
        var eligible=new Dictionary<Guid,ShareLinkAdvisory>();
        // Source locks precede every definitive link lock, matching source-delete triggers.
        foreach(var candidate in advisory.OrderBy(item=>item.ResourceType,StringComparer.Ordinal).ThenBy(item=>item.ResourceId))
            if (OwnerSourceAllowed(connection,transaction,actor,candidate.ResourceType,candidate.ResourceId,creating:false))
                eligible[candidate.Id]=candidate;
        var links=new List<ShareLinkRecord>();
        if (eligible.Count!=0)
        {
            using var command=connection.CreateCommand();command.Transaction=transaction;
            var parameters=eligible.Keys.Select((id,index)=>{var name="@Link"+index;Add(command,name,SqlDbType.UniqueIdentifier,id);return name;}).ToArray();
            command.CommandText=$"""
                SELECT sl.Id,sl.ResourceType,sl.ResourceId,sl.Mode,sl.ExpiresAt,sl.RevokedAt,
                       sl.ProjectionVersion,sl.CreatedAt,sl.UpdatedAt,sl.RowVersion,
                       CASE WHEN sl.RevokedAt IS NULL AND (sl.ExpiresAt IS NULL OR sl.ExpiresAt>SYSUTCDATETIME()) THEN CONVERT(bit,1) ELSE CONVERT(bit,0) END
                FROM [security].[ShareLink] sl JOIN [platform].[Module] m ON m.Code='FX04'
                WHERE sl.OwnerId=@Owner AND sl.IsDeleted=0 AND sl.IssuedSharingEpoch=m.SharingEpoch
                  AND sl.Id IN({string.Join(',',parameters)}) ORDER BY sl.UpdatedAt DESC,sl.Id DESC;
                """;
            Add(command,"@Owner",SqlDbType.UniqueIdentifier,actor.OwnerId);
            using var reader=command.ExecuteReader();
            while(reader.Read())
            {
                var link=ReadLink(reader);var candidate=eligible[link.Id];
                if(link.ResourceType==candidate.ResourceType && link.ResourceId==candidate.ResourceId)links.Add(link);
            }
        }
        var allowed=ReadAllowedUsers(connection,transaction,actor.OwnerId,links.Select(link=>link.Id).ToArray());
        links=links.Select(link=>link with {AllowedUserIds=allowed.TryGetValue(link.Id,out var users)?users:Array.Empty<Guid>()}).ToList();
        if (!SqlCurrentActor.IsLive(connection,transaction,actor)) return ModuleUnavailable<ShareLinkPage>();
        transaction.Commit();return IdentityOperationResult<ShareLinkPage>.Success(new(links,null));
    }

    private sealed record ShareLinkAdvisory(Guid Id,string ResourceType,Guid ResourceId);
    private static List<ShareLinkAdvisory> ReadListAdvisories(SqlConnection connection,Guid owner,int take)
    {
        using var command=connection.CreateCommand();
        command.CommandText="SELECT TOP(@Limit) Id,ResourceType,ResourceId FROM [security].[ShareLink] WHERE OwnerId=@Owner AND IsDeleted=0 ORDER BY UpdatedAt DESC,Id DESC;";
        Add(command,"@Owner",SqlDbType.UniqueIdentifier,owner);Add(command,"@Limit",SqlDbType.Int,take);
        var items=new List<ShareLinkAdvisory>();using var reader=command.ExecuteReader();
        while(reader.Read())items.Add(new(reader.GetGuid(0),reader.GetString(1),reader.GetGuid(2)));
        return items;
    }

    public IdentityOperationResult<ShareLinkRecord> Create(IdentityPrincipal actor, ShareLinkCreateCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX04", "sharing.link.create")) return ModuleUnavailable<ShareLinkRecord>();
        var validation = ValidateCreate(command);
        if (validation is not null) return validation;

        var resourceType = command.ResourceType.Trim();
        var mode = command.Mode.Trim();
        var allowedUsers = (command.AllowedUserIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? expiresAt = command.NoExpiry ? null : command.ExpiresAt ?? now.AddDays(7);
        var expiryDescriptor = command.NoExpiry ? "no-expiry" : command.ExpiresAt is null ? "default-7d" : command.ExpiresAt.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var policyBoundary=ReadSharingModule(connection,transaction,forUpdate:true);
        if (policyBoundary is null || !policyBoundary.Value.Available)
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }

        if (!SqlCurrentActor.IsLive(connection, transaction, actor) ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.create") ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.read"))
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }
        if (!OwnerSourceAllowed(connection, transaction, actor, resourceType, command.ResourceId, creating:true))
        { transaction.Rollback(); return Missing<ShareLinkRecord>(); }
        var receiptFailure = CheckReceipt<ShareLinkRecord>(connection, transaction, actor,
            "sharing.link.create", idempotencyKey,
            $"resource:{resourceType}|id:{command.ResourceId:N}|mode:{mode}|expiry:{expiryDescriptor}|users:{string.Join(',', allowedUsers.OrderBy(id => id).Select(id => id.ToString("N")))}",
            out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }

        var module = ReadSharingModule(connection, transaction, forUpdate: true);
        if (module is null || !module.Value.Available)
        {
            transaction.Rollback();
            return ModuleUnavailable<ShareLinkRecord>();
        }

        var source = ReadSource(connection, transaction, resourceType, command.ResourceId, actor.OwnerId);
        if (source is null || source.Value.Status == "Deleted" ||
            (resourceType == "Document" && source.Value.Status != "Published"))
        {
            transaction.Rollback();
            return IdentityOperationResult<ShareLinkRecord>.Failure("ResourceUnavailable", 404, "The resource is unavailable for sharing.");
        }

        if (!ValidateAllowedUsers(connection, transaction, allowedUsers, mode))
        {
            transaction.Rollback();
            return IdentityOperationResult<ShareLinkRecord>.Failure("ValidationFailed", 422, "The restricted audience is invalid.");
        }

        var linkId = Guid.NewGuid();
        var rawToken = CreateToken();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO [security].[ShareLink]
                ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [ResourceType], [ResourceId],
                 [TokenHash], [Mode], [ExpiresAt], [ProjectionVersion], [IssuedSharingEpoch])
            VALUES
                (@Id, @OwnerId, @CreatedBy, @UpdatedBy, @ResourceType, @ResourceId,
                 @TokenHash, @Mode, @ExpiresAt, @ProjectionVersion, @SharingEpoch);
            """;
        Add(insert, "@Id", SqlDbType.UniqueIdentifier, linkId);
        Add(insert, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(insert, "@CreatedBy", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(insert, "@UpdatedBy", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(insert, "@ResourceType", SqlDbType.VarChar, resourceType, 32);
        Add(insert, "@ResourceId", SqlDbType.UniqueIdentifier, command.ResourceId);
        Add(insert, "@TokenHash", SqlDbType.Binary, HashToken(rawToken), 32);
        Add(insert, "@Mode", SqlDbType.VarChar, mode, 32);
        Add(insert, "@ExpiresAt", SqlDbType.DateTime2, (object?)expiresAt?.UtcDateTime ?? DBNull.Value);
        Add(insert, "@ProjectionVersion", SqlDbType.VarChar, ProjectionVersion, 32);
        Add(insert, "@SharingEpoch", SqlDbType.BigInt, module.Value.Epoch);
        insert.ExecuteNonQuery();

        InsertAllowedUsers(connection, transaction, actor.OwnerId, linkId, allowedUsers);
        WriteAudit(connection, transaction, actor, linkId, "sharing.link.create", traceId);
        var created = ReadLink(connection, transaction, actor.OwnerId, linkId, includeDeleted: false);
        if (created is null)
        {
            transaction.Rollback();
            return PersistenceFailure<ShareLinkRecord>();
        }
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }
        CompleteReceipt(connection, transaction, receipt, "ShareLinkCreated");
        transaction.Commit();
        return IdentityOperationResult<ShareLinkRecord>.Success(created with { RawToken = rawToken }, 201, "ShareLinkCreated");
    }

    public IdentityOperationResult<ShareLinkRecord> Update(IdentityPrincipal actor, Guid shareLinkId, string? ifMatch,
        ShareLinkUpdateCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX04", "sharing.link.update")) return ModuleUnavailable<ShareLinkRecord>();
        var validation = ValidateUpdate(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<ShareLinkRecord>(ifMatch);

        var mode = command.Mode.Trim();
        var allowedUsers = (command.AllowedUserIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        DateTimeOffset? expiresAt = command.NoExpiry ? null : command.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var expiryDescriptor = command.NoExpiry ? "no-expiry" : command.ExpiresAt is null ? "default-7d" : command.ExpiresAt.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        using var connection = _connections.Create();
        connection.Open();
        if (!SqlCurrentActor.IsLive(connection,null,actor)) return ModuleUnavailable<ShareLinkRecord>();
        var advisory=ReadLink(connection,null,actor.OwnerId,shareLinkId,forUpdate:false);
        if (advisory is null) return Missing<ShareLinkRecord>();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var policyBoundary=ReadSharingModule(connection,transaction,forUpdate:true);
        if (policyBoundary is null || !policyBoundary.Value.Available)
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }

        if (!SqlCurrentActor.IsLive(connection, transaction, actor) ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.update") ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.read"))
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }
        if (!OwnerSourceAllowed(connection,transaction,actor,advisory.ResourceType,advisory.ResourceId,creating:false))
        { transaction.Rollback(); return Missing<ShareLinkRecord>(); }
        var authorizedLink=ReadLink(connection,transaction,actor.OwnerId,shareLinkId,forUpdate:true);
        if (authorizedLink is null || authorizedLink.ResourceType!=advisory.ResourceType || authorizedLink.ResourceId!=advisory.ResourceId ||
            !LinkEpochCurrent(connection,transaction,shareLinkId,actor.OwnerId))
        { transaction.Rollback(); return Missing<ShareLinkRecord>(); }
        var receiptFailure = CheckReceipt<ShareLinkRecord>(connection, transaction, actor,
            "sharing.link.update", idempotencyKey,
            $"id:{shareLinkId:N}|etag:{ifMatch}|mode:{mode}|expiry:{expiryDescriptor}|users:{string.Join(',', allowedUsers.OrderBy(id => id).Select(id => id.ToString("N")))}",
            out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }

        var current = ReadLink(connection, transaction, actor.OwnerId, shareLinkId, forUpdate: true, includeDeleted: false);
        if (current is null)
        {
            transaction.Rollback();
            return Missing<ShareLinkRecord>();
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return Revision<ShareLinkRecord>();
        }
        if (!ValidateAllowedUsers(connection, transaction, allowedUsers, mode))
        {
            transaction.Rollback();
            return IdentityOperationResult<ShareLinkRecord>.Failure("ValidationFailed", 422, "The restricted audience is invalid.");
        }

        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [security].[ShareLink]
            SET [Mode] = @Mode, [ExpiresAt] = @ExpiresAt, [UpdatedByUserId] = @UpdatedBy,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [IsDeleted] = 0 AND [RowVersion] = @RowVersion;
            """;
        Add(update, "@Mode", SqlDbType.VarChar, mode, 32);
        Add(update, "@ExpiresAt", SqlDbType.DateTime2, (object?)expiresAt?.UtcDateTime ?? DBNull.Value);
        Add(update, "@UpdatedBy", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, shareLinkId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return Revision<ShareLinkRecord>();
        }

        Execute(connection, transaction, "DELETE FROM [security].[ShareAllowedUser] WHERE [OwnerId] = @OwnerId AND [ShareLinkId] = @LinkId;",
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@LinkId", SqlDbType.UniqueIdentifier, (object)shareLinkId));
        InsertAllowedUsers(connection, transaction, actor.OwnerId, shareLinkId, mode == "RestrictedUsers" ? allowedUsers : Array.Empty<Guid>());
        WriteAudit(connection, transaction, actor, shareLinkId, "sharing.link.update", traceId);
        var updated = ReadLink(connection, transaction, actor.OwnerId, shareLinkId, includeDeleted: false);
        if (updated is null)
        {
            transaction.Rollback();
            return PersistenceFailure<ShareLinkRecord>();
        }
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
        { transaction.Rollback(); return ModuleUnavailable<ShareLinkRecord>(); }
        CompleteReceipt(connection, transaction, receipt, "ShareLinkUpdated");
        transaction.Commit();
        return IdentityOperationResult<ShareLinkRecord>.Success(updated);
    }

    public IdentityOperationResult<object?> Revoke(IdentityPrincipal actor, Guid shareLinkId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX04", "sharing.link.revoke")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        if (!SqlCurrentActor.IsLive(connection,null,actor)) return ModuleUnavailable<object?>();
        var advisory=ReadLink(connection,null,actor.OwnerId,shareLinkId,forUpdate:false);
        if (advisory is null) return Missing<object?>();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var policyBoundary=ReadSharingModule(connection,transaction,forUpdate:true);
        if (policyBoundary is null || !policyBoundary.Value.Available)
        { transaction.Rollback(); return ModuleUnavailable<object?>(); }

        if (!SqlCurrentActor.IsLive(connection, transaction, actor) ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.revoke") ||
            !ModuleAvailable(connection, transaction, actor, "FX04", "sharing.link.read"))
        { transaction.Rollback(); return ModuleUnavailable<object?>(); }
        if (!OwnerSourceAllowed(connection,transaction,actor,advisory.ResourceType,advisory.ResourceId,creating:false))
        { transaction.Rollback(); return Missing<object?>(); }
        var authorizedLink=ReadLink(connection,transaction,actor.OwnerId,shareLinkId,forUpdate:true);
        if (authorizedLink is null || authorizedLink.ResourceType!=advisory.ResourceType || authorizedLink.ResourceId!=advisory.ResourceId ||
            !LinkEpochCurrent(connection,transaction,shareLinkId,actor.OwnerId))
        { transaction.Rollback(); return Missing<object?>(); }
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "sharing.link.revoke", idempotencyKey, $"id:{shareLinkId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var current = ReadLink(connection, transaction, actor.OwnerId, shareLinkId, forUpdate: true, includeDeleted: false);
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
            UPDATE [security].[ShareLink]
            SET [IsDeleted] = 1, [RevokedAt] = COALESCE([RevokedAt], SYSUTCDATETIME()),
                [InvalidatedAt] = COALESCE([InvalidatedAt], SYSUTCDATETIME()),
                [InvalidationReason] = 'OwnerRevoked', [UpdatedAt] = SYSUTCDATETIME(),
                [UpdatedByUserId] = @Actor
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [IsDeleted] = 0 AND [RowVersion] = @RowVersion;
            """,
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Id", SqlDbType.UniqueIdentifier, (object)shareLinkId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, shareLinkId, "sharing.link.revoke", traceId);
        if (!SqlCurrentActor.IsLive(connection, transaction, actor))
        { transaction.Rollback(); return ModuleUnavailable<object?>(); }
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<SharedResource> Resolve(string token, IdentityPrincipal? viewer)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
            return SharedUnavailable();

        var digest = HashToken(token);
        using var connection = _connections.Create();
        connection.Open();
        var now=DateTimeOffset.UtcNow;
        var advisory=ReadResolvableLink(connection,null,digest,now);
        if (advisory is null) return SharedUnavailable();
        using var transaction=connection.BeginTransaction(IsolationLevel.Serializable);
        var policyBoundary=ReadSharingModule(connection,transaction,forUpdate:false);
        if (policyBoundary is null || !policyBoundary.Value.Available) return SharedUnavailable();
        var sourceState=ReadSource(connection,transaction,advisory.Value.ResourceType,advisory.Value.ResourceId,advisory.Value.OwnerId);
        if (sourceState is null || sourceState.Value.Status=="Deleted") return SharedUnavailable();
        var link=ReadResolvableLink(connection,transaction,digest,DateTimeOffset.UtcNow);
        if (link is null || link.Value.Id!=advisory.Value.Id || link.Value.OwnerId!=advisory.Value.OwnerId ||
            link.Value.ResourceType!=advisory.Value.ResourceType || link.Value.ResourceId!=advisory.Value.ResourceId) return SharedUnavailable();
        if (link is null || !string.Equals(link.Value.ProjectionVersion, ProjectionVersion, StringComparison.Ordinal))
            return SharedUnavailable();

        if (link.Value.Mode is "AuthenticatedLink" or "RestrictedUsers")
        {
            if (viewer is null || !SqlCurrentActor.IsLive(connection, transaction, viewer) || !ViewerIsEligible(connection, transaction, viewer.UserId, link.Value.OwnerId,
                    link.Value.Mode == "RestrictedUsers" ? link.Value.Id : null))
                return SharedUnavailable();
        }

        var sourceModule = link.Value.ResourceType == "Project" ? "FX11" : "FX20";
        if (!SourceModuleAvailable(connection, transaction, link.Value.OwnerId, sourceModule) ||
            !SqlSharedLinkAuthority.SourceActionsAllowed(connection, transaction, link.Value.OwnerId, sourceModule))
            return SharedUnavailable();

        var sourceContext = new SharedLinkSourceContext(link.Value.OwnerId, link.Value.ResourceId);
        var project = link.Value.ResourceType == "Project"
            ? SqlProjectSharedSource.ReadProjection(connection, transaction, sourceContext, now) : null;
        var document = link.Value.ResourceType == "Document"
            ? SqlDocumentSharedSource.ReadProjection(connection, transaction, sourceContext) : null;
        if (project is null && document is null) return SharedUnavailable();
        // Recheck time-based expiry/session boundaries after projection materialization.
        if (ReadResolvableLink(connection, transaction, digest, DateTimeOffset.UtcNow) is null ||
            (link.Value.Mode != "PublicLink" && (viewer is null || !SqlCurrentActor.IsLive(connection, transaction, viewer))))
            return SharedUnavailable();
        transaction.Commit();
        return IdentityOperationResult<SharedResource>.Success(new SharedResource(
            link.Value.ResourceType, link.Value.ResourceId, link.Value.Mode, link.Value.ExpiresAt,
            link.Value.ProjectionVersion, project, document));
    }

    private static IdentityOperationResult<ShareLinkRecord>? ValidateCreate(ShareLinkCreateCommand command)
    {
        if (command.ResourceId == Guid.Empty || command.ResourceType?.Trim() is not ("Project" or "Document") ||
            command.Mode?.Trim() is not ("PublicLink" or "AuthenticatedLink" or "RestrictedUsers"))
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "The share resource or mode is invalid.");
        if (command.ExpiresAt is { } expires && expires <= DateTimeOffset.UtcNow)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "Share expiry must be in the future.");
        if (command.NoExpiry && command.ExpiresAt is not null)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "NoExpiry cannot include an expiry.");
        var users = command.AllowedUserIds ?? Array.Empty<Guid>();
        if (users.Any(id => id == Guid.Empty) || users.Distinct().Count() > 100)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "The share audience is invalid.");
        var mode = command.Mode?.Trim();
        if (mode == "RestrictedUsers" && users.Count == 0)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "RestrictedUsers requires at least one verified user.");
        if (mode != "RestrictedUsers" && users.Count > 0)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "An audience is only valid for RestrictedUsers.");
        return null;
    }

    private static IdentityOperationResult<ShareLinkRecord>? ValidateUpdate(ShareLinkUpdateCommand command)
    {
        if (command.Mode?.Trim() is not ("PublicLink" or "AuthenticatedLink" or "RestrictedUsers"))
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "The share mode is invalid.");
        if (command.ExpiresAt is { } expires && expires <= DateTimeOffset.UtcNow)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "Share expiry must be in the future.");
        if (command.NoExpiry && command.ExpiresAt is not null)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "NoExpiry cannot include an expiry.");
        var users = command.AllowedUserIds ?? Array.Empty<Guid>();
        if (users.Any(id => id == Guid.Empty) || users.Distinct().Count() > 100)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "The share audience is invalid.");
        var mode = command.Mode?.Trim();
        if (mode == "RestrictedUsers" && users.Count == 0)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "RestrictedUsers requires at least one verified user.");
        if (mode != "RestrictedUsers" && users.Count > 0)
            return Failure<ShareLinkRecord>("ValidationFailed", 422, "An audience is only valid for RestrictedUsers.");
        return null;
    }

    private static bool ValidateAllowedUsers(SqlConnection connection, SqlTransaction transaction, Guid[] users, string mode)
    {
        if (mode != "RestrictedUsers") return users.Length == 0;
        if (users.Length == 0 || users.Any(id => id == Guid.Empty)) return false;
        var names = new List<string>();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        for (var index = 0; index < users.Length; index++)
        {
            var name = "@User" + index;
            names.Add(name);
            Add(command, name, SqlDbType.UniqueIdentifier, users[index]);
        }
        command.CommandText = $"SELECT COUNT_BIG(1) FROM [identity].[User] WHERE [Id] IN ({string.Join(',', names)}) AND [State] = 'Active' AND [IsDeleted] = 0 AND [EmailConfirmed] = 1;";
        return Convert.ToInt64(command.ExecuteScalar() ?? 0L) == users.Length;
    }

    private static void InsertAllowedUsers(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid linkId, IEnumerable<Guid> users)
    {
        foreach (var userId in users)
        {
            Execute(connection, transaction,
                "INSERT INTO [security].[ShareAllowedUser] ([OwnerId], [ShareLinkId], [UserId]) VALUES (@OwnerId, @LinkId, @UserId);",
                ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
                ("@LinkId", SqlDbType.UniqueIdentifier, (object)linkId),
                ("@UserId", SqlDbType.UniqueIdentifier, (object)userId));
        }
    }

    private static ShareLinkRecord? ReadLink(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid id,
        bool forUpdate = false, bool includeDeleted = false)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT sl.[Id], sl.[ResourceType], sl.[ResourceId], sl.[Mode], sl.[ExpiresAt], sl.[RevokedAt],
                   sl.[ProjectionVersion], sl.[CreatedAt], sl.[UpdatedAt], sl.[RowVersion],
                   CASE WHEN sl.[RevokedAt] IS NULL AND (sl.[ExpiresAt] IS NULL OR sl.[ExpiresAt] > SYSUTCDATETIME())
                              AND sl.[IssuedSharingEpoch] = m.[SharingEpoch] THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END
            FROM [security].[ShareLink] sl WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")})
            INNER JOIN [platform].[Module] m ON m.[Code] = 'FX04'
            WHERE sl.[Id] = @Id AND sl.[OwnerId] = @OwnerId {(includeDeleted ? string.Empty : "AND sl.[IsDeleted] = 0")};
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var link = ReadLink(reader);
        reader.Close();
        var allowed = ReadAllowedUsers(connection, transaction, ownerId, new[] { id });
        return link with { AllowedUserIds = allowed.TryGetValue(id, out var users) ? users : Array.Empty<Guid>() };
    }

    private static ShareLinkRecord ReadLink(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3),
            reader.IsDBNull(4) ? null : ToOffset(reader.GetDateTime(4)),
            reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), reader.GetString(6),
            reader.GetBoolean(10), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)),
            EncodeETag(reader.GetFieldValue<byte[]>(9)), Array.Empty<Guid>());

    private static Dictionary<Guid, IReadOnlyList<Guid>> ReadAllowedUsers(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid[] linkIds)
    {
        var values = new Dictionary<Guid, IReadOnlyList<Guid>>();
        if (linkIds.Length == 0) return values;
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var names = new List<string>();
        for (var index = 0; index < linkIds.Length; index++)
        {
            var name = "@Link" + index;
            names.Add(name);
            Add(command, name, SqlDbType.UniqueIdentifier, linkIds[index]);
        }
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        command.CommandText = $"SELECT [ShareLinkId], [UserId] FROM [security].[ShareAllowedUser] WHERE [OwnerId] = @OwnerId AND [ShareLinkId] IN ({string.Join(',', names)}) ORDER BY [ShareLinkId], [UserId];";
        using var reader = command.ExecuteReader();
        var grouped = new Dictionary<Guid, List<Guid>>();
        while (reader.Read())
        {
            var linkId = reader.GetGuid(0);
            if (!grouped.TryGetValue(linkId, out var list)) grouped[linkId] = list = new List<Guid>();
            list.Add(reader.GetGuid(1));
        }
        foreach (var pair in grouped) values[pair.Key] = pair.Value;
        return values;
    }

    private static (Guid Id, Guid OwnerId, string ResourceType, Guid ResourceId, string Mode,
        DateTimeOffset? ExpiresAt, string ProjectionVersion)? ReadResolvableLink(SqlConnection connection, SqlTransaction? transaction, byte[] digest, DateTimeOffset now)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (1) sl.[Id], sl.[OwnerId], sl.[ResourceType], sl.[ResourceId], sl.[Mode],
                   sl.[ExpiresAt], sl.[ProjectionVersion]
            FROM [security].[ShareLink] sl
            INNER JOIN [platform].[Module] m ON m.[Code] = 'FX04'
            INNER JOIN [platform].[PersonalSpace] ps ON ps.[Id] = sl.[OwnerId] AND ps.[State] = 'Active'
            INNER JOIN [identity].[User] ownerUser ON ownerUser.[Id] = ps.[UserId]
            INNER JOIN [platform].[UserModuleGrant] sharingGrant
              ON sharingGrant.[ModuleId] = m.[Id] AND sharingGrant.[UserId] = ownerUser.[Id] AND sharingGrant.[Enabled] = 1
            WHERE sl.[TokenHash] = @TokenHash AND sl.[IsDeleted] = 0
              AND sl.[RevokedAt] IS NULL
              AND (sl.[ExpiresAt] IS NULL OR sl.[ExpiresAt] > @Now)
              AND sl.[IssuedSharingEpoch] = m.[SharingEpoch]
              AND m.[State] = 'Ready' AND m.[SystemEnabled] = 1 AND m.[SharingEnabled] = 1
              AND ownerUser.[State] = 'Active' AND ownerUser.[IsDeleted] = 0;
            """;
        Add(command, "@TokenHash", SqlDbType.Binary, digest, 32);
        Add(command, "@Now", SqlDbType.DateTime2, now.UtcDateTime);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetGuid(3), reader.GetString(4),
            reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), reader.GetString(6));
    }

    private static bool ViewerIsEligible(SqlConnection connection, SqlTransaction transaction, Guid viewerId, Guid ownerId, Guid? restrictedLinkId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = restrictedLinkId is null
            ? "SELECT 1 FROM [identity].[User] WHERE [Id] = @UserId AND [State] = 'Active' AND [IsDeleted] = 0 AND [EmailConfirmed] = 1;"
            : "SELECT 1 FROM [identity].[User] u INNER JOIN [security].[ShareAllowedUser] allowRow ON allowRow.[UserId] = u.[Id] WHERE u.[Id] = @UserId AND u.[State] = 'Active' AND u.[IsDeleted] = 0 AND u.[EmailConfirmed] = 1 AND allowRow.[OwnerId] = @OwnerId AND allowRow.[ShareLinkId] = @LinkId;";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, viewerId);
        if (restrictedLinkId is not null)
        {
            Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
            Add(command, "@LinkId", SqlDbType.UniqueIdentifier, restrictedLinkId.Value);
        }
        return command.ExecuteScalar() is not null;
    }

    private static bool SourceModuleAvailable(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, string moduleCode)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT CASE WHEN m.[State] = 'Ready' AND m.[SystemEnabled] = 1 AND m.[SharingEnabled] = 1
                              AND ownerSpace.[State] = 'Active'
                              AND ownerUser.[State] = 'Active' AND ownerUser.[IsDeleted] = 0
                              AND COALESCE(grantRow.[Enabled], 0) = 1
                        THEN 1 ELSE 0 END
            FROM [platform].[Module] m
            INNER JOIN [platform].[PersonalSpace] ownerSpace ON ownerSpace.[Id] = @OwnerId
            INNER JOIN [identity].[User] ownerUser ON ownerUser.[Id] = ownerSpace.[UserId]
            LEFT JOIN [platform].[UserModuleGrant] grantRow
              ON grantRow.[ModuleId] = m.[Id] AND grantRow.[UserId] = ownerSpace.[UserId]
            WHERE m.[Code] = @ModuleCode;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ModuleCode", SqlDbType.VarChar, moduleCode, 64);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static (Guid OwnerId, string Status)? ReadSource(SqlConnection connection, SqlTransaction? transaction,
        string resourceType, Guid resourceId, Guid ownerId) => resourceType == "Project"
        ? SqlProjectSharedSource.ReadState(connection, transaction, ownerId, resourceId)
        : SqlDocumentSharedSource.ReadState(connection, transaction, ownerId, resourceId);

    private (bool Available, long Epoch)? ReadSharingModule(SqlConnection connection, SqlTransaction? transaction, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [State], [SystemEnabled], [SharingEnabled], [SharingEpoch] FROM [platform].[Module] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "HOLDLOCK")}) WHERE [Code] = 'FX04';";
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetString(0) == "Ready" && reader.GetBoolean(1) && reader.GetBoolean(2), reader.GetInt64(3));
    }

    private bool OwnerSourceAllowed(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string type, Guid id, bool creating)
    {
        var module=type=="Project"?"FX11":"FX20";
        var prefix=type=="Project"?"projects.project":"documents.page";
        if (!ModuleAvailable(connection,transaction,actor,module,prefix+".read") ||
            !ModuleAvailable(connection,transaction,actor,module,prefix+".share") ||
            !SourceModuleAvailable(connection,transaction,actor.OwnerId,module)) return false;
        var sharing=ReadSharingModule(connection,transaction,forUpdate:false);
        if (sharing is null || !sharing.Value.Available) return false;
        var source=ReadSource(connection,transaction,type,id,actor.OwnerId);
        return source is not null && source.Value.Status!="Deleted" &&
            (!creating || type!="Document" || source.Value.Status=="Published");
    }

    private static bool LinkEpochCurrent(SqlConnection connection, SqlTransaction transaction, Guid id, Guid owner)
    {
        using var command=connection.CreateCommand(); command.Transaction=transaction;
        command.CommandText="SELECT 1 FROM [security].[ShareLink] l JOIN [platform].[Module] m ON m.Code='FX04' WHERE l.Id=@Id AND l.OwnerId=@Owner AND l.IsDeleted=0 AND l.IssuedSharingEpoch=m.SharingEpoch;";
        Add(command,"@Id",SqlDbType.UniqueIdentifier,id); Add(command,"@Owner",SqlDbType.UniqueIdentifier,owner);
        return command.ExecuteScalar() is not null;
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private bool ModuleAvailable(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(connection, transaction, actor, moduleCode, actionKeys);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId) =>
        Execute(connection, transaction,
            "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @OwnerUser, @Action, N'security.ShareLink', @Target, 'Succeeded', @TraceId);",
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@OwnerUser", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Action", SqlDbType.NVarChar, (object)action),
            ("@Target", SqlDbType.UniqueIdentifier, (object)targetId),
            ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

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

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static string CreateToken() => Base64Url(RandomNumberGenerator.GetBytes(32));
    private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
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

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type);
        if (size > 0) parameter.Size = size;
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Sharing is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "Share link unavailable.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "Share link revision changed.");
    private static IdentityOperationResult<T> Precondition<T>(string? ifMatch) => Failure<T>(string.IsNullOrWhiteSpace(ifMatch) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(ifMatch) ? 428 : 412, string.IsNullOrWhiteSpace(ifMatch) ? "If-Match is required." : "If-Match is invalid.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "Sharing persistence is unavailable.");
    private static IdentityOperationResult<SharedResource> SharedUnavailable() => Failure<SharedResource>("ResourceUnavailable", 404, "Shared resource unavailable.");
}
