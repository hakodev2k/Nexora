using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Discovery;

/// <summary>
/// SQL-backed owner-scoped favorites for FX25-S03. A favorite stores only a
/// typed source reference. The source owner, lifecycle and current capability
/// are rechecked while the mutation transaction is still open, so this table
/// never becomes a second authority for source access or metadata.
/// </summary>
public sealed class SqlFavoriteService : IFavoriteService
{
    private const int MaxLimit = 100;
    private const decimal MaxRank = 1_000_000_000m;

    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlFavoriteService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<FavoritePage> List(IdentityPrincipal actor, string? resourceType = null,
        int? limit = null, string? cursor = null)
    {
        var normalizedType = NormalizeResourceType(resourceType);
        if (!string.IsNullOrWhiteSpace(resourceType) && normalizedType is null)
            return Failure<FavoritePage>("ValidationFailed", 422, "Favorite resource type is invalid.");

        var take = Math.Clamp(limit ?? MaxLimit, 1, MaxLimit);
        var cursorValue = ParseCursor(cursor, out var cursorValid);
        if (!cursorValid)
            return Failure<FavoritePage>("ValidationFailed", 422, "Favorite cursor is invalid.");

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.read"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoritePage>();
            }

            var rows = ReadRows(connection, transaction, actor.OwnerId, normalizedType, take + 1, cursorValue);
            var hasMore = rows.Count > take;
            if (hasMore)
                rows = rows.Take(take).ToList();
            var items = rows.Select(row => ToRecord(connection, transaction, actor, row)).ToArray();
            var nextCursor = hasMore && rows.Count > 0 ? EncodeCursor(rows[^1]) : null;
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.read"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoritePage>();
            }
            transaction.Commit();
            return IdentityOperationResult<FavoritePage>.Success(new FavoritePage(items, nextCursor));
        }
        catch (SqlException exception)
        {
            return PersistenceFailure<FavoritePage>(exception);
        }
    }

    public IdentityOperationResult<FavoriteRecord> Add(IdentityPrincipal actor, FavoriteReferenceCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (command is null)
            return Failure<FavoriteRecord>("ValidationFailed", 422, "Favorite resource type and resource id are required.");

        var normalizedType = NormalizeResourceType(command.ResourceType);
        if (normalizedType is null || command.ResourceId == Guid.Empty)
            return Failure<FavoriteRecord>("ValidationFailed", 422, "Favorite resource type and resource id are required.");

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.add"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoriteRecord>();
            }

            var receiptFailure = CheckReceipt<FavoriteRecord>(connection, transaction, actor, "discovery.favorite.add", idempotencyKey,
                $"resource:{normalizedType}|id:{command.ResourceId:N}", out var receipt);
            if (receiptFailure is not null)
            {
                transaction.Rollback();
                return receiptFailure;
            }
            if (receipt.IsReplay)
            {
                var replay = ReplayFavoriteRecord(connection, transaction, actor, receipt);
                transaction.Rollback();
                return replay;
            }

            var source = ResolveSource(connection, transaction, actor, normalizedType, command.ResourceId);
            if (source is null)
            {
                transaction.Rollback();
                return Missing<FavoriteRecord>();
            }
            // Keep the capability decision in the same serializable transaction
            // immediately before the reference write. The capability query also
            // locks the module/grant rows until commit.
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.add"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoriteRecord>();
            }

            var favoriteId = Guid.NewGuid();
            Execute(connection, transaction, """
                INSERT INTO [discovery].[Favorite]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [ResourceType], [ResourceId], [Rank])
                SELECT @Id, @OwnerId, @UserId, @UserId, @ResourceType, @ResourceId,
                       CONVERT(decimal(28,8), CASE WHEN COALESCE(MAX([Rank]), -1) >= @MaxRank
                                                   THEN @MaxRank
                                                   ELSE COALESCE(MAX([Rank]), -1) + 1 END)
                FROM [discovery].[Favorite] WITH (UPDLOCK, HOLDLOCK)
                WHERE [OwnerId] = @OwnerId;
                """,
                ("@Id", SqlDbType.UniqueIdentifier, favoriteId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@ResourceType", SqlDbType.VarChar, normalizedType, 32),
                ("@ResourceId", SqlDbType.UniqueIdentifier, command.ResourceId, null),
                ("@MaxRank", SqlDbType.Decimal, MaxRank, null));

            var created = ReadFavorite(connection, transaction, actor.OwnerId, favoriteId, forUpdate: false);
            if (created is null)
            {
                transaction.Rollback();
                return Failure<FavoriteRecord>("PersistenceFailure", 500, "Favorite could not be loaded after creation.");
            }

            var record = ToRecord(source, created);
            WriteAudit(connection, transaction, actor, favoriteId, "discovery.favorite.add", traceId);
            CompleteReceipt(connection, transaction, receipt, "FavoriteAdded", 201, JsonSerializer.Serialize(record));
            transaction.Commit();
            return IdentityOperationResult<FavoriteRecord>.Success(record, 201, "FavoriteAdded");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Failure<FavoriteRecord>("FavoriteDuplicate", 409, "This resource is already a favorite.");
        }
        catch (SqlException exception)
        {
            return PersistenceFailure<FavoriteRecord>(exception);
        }
    }

    public IdentityOperationResult<object?> Remove(IdentityPrincipal actor, Guid favoriteId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<object?>();

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.remove"))
            {
                transaction.Rollback();
                return ModuleUnavailable<object?>();
            }

            var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "discovery.favorite.remove", idempotencyKey,
                $"favorite:{favoriteId:N}|etag:{ifMatch}", out var receipt);
            if (receiptFailure is not null)
            {
                transaction.Rollback();
                return receiptFailure;
            }
            if (receipt.IsReplay)
            {
                transaction.Rollback();
                return Replay<object?>(receipt);
            }

            var current = ReadFavorite(connection, transaction, actor.OwnerId, favoriteId, forUpdate: true);
            if (current is null)
            {
                transaction.Rollback();
                return Missing<object?>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<object?>();
            }

            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.remove"))
            {
                transaction.Rollback();
                return ModuleUnavailable<object?>();
            }
            var affected = Execute(connection, transaction, """
                DELETE FROM [discovery].[Favorite]
                WHERE [OwnerId] = @OwnerId AND [Id] = @Id AND [RowVersion] = @RowVersion;
                """,
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@Id", SqlDbType.UniqueIdentifier, favoriteId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            if (affected != 1)
            {
                transaction.Rollback();
                return Revision<object?>();
            }

            WriteAudit(connection, transaction, actor, favoriteId, "discovery.favorite.remove", traceId);
            CompleteReceipt(connection, transaction, receipt, "FavoriteRemoved", 204, null);
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent("FavoriteRemoved");
        }
        catch (SqlException exception)
        {
            return PersistenceFailure<object?>(exception);
        }
    }

    public IdentityOperationResult<FavoriteRecord> Reorder(IdentityPrincipal actor, Guid favoriteId, string? ifMatch,
        FavoriteRankCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (command is null || command.Rank is not { } rank)
            return Failure<FavoriteRecord>("ValidationFailed", 422, "Favorite rank is required.");
        if (rank < 0 || rank > MaxRank || decimal.Round(rank, 8) != rank)
            return Failure<FavoriteRecord>("ValidationFailed", 422, "Favorite rank must be between 0 and 1000000000 with at most 8 decimal places.");
        if (!TryETag(ifMatch, out var expectedVersion))
            return Precondition<FavoriteRecord>();

        try
        {
            using var connection = _connections.Create();
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.reorder"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoriteRecord>();
            }

            var receiptFailure = CheckReceipt<FavoriteRecord>(connection, transaction, actor, "discovery.favorite.reorder", idempotencyKey,
                $"favorite:{favoriteId:N}|etag:{ifMatch}|rank:{rank.ToString(CultureInfo.InvariantCulture)}", out var receipt);
            if (receiptFailure is not null)
            {
                transaction.Rollback();
                return receiptFailure;
            }
            if (receipt.IsReplay)
            {
                var replay = ReplayFavoriteRecord(connection, transaction, actor, receipt);
                transaction.Rollback();
                return replay;
            }

            var current = ReadFavorite(connection, transaction, actor.OwnerId, favoriteId, forUpdate: true);
            if (current is null)
            {
                transaction.Rollback();
                return Missing<FavoriteRecord>();
            }
            if (!CryptographicOperations.FixedTimeEquals(expectedVersion, DecodeETag(current.ETag)))
            {
                transaction.Rollback();
                return Revision<FavoriteRecord>();
            }

            // Reordering is a source-facing mutation: do not mutate a stale
            // reference whose current source read capability/lifecycle is no
            // longer available. Removal remains the explicit cleanup path.
            var sourceBeforeUpdate = ResolveSource(connection, transaction, actor,
                current.ResourceType, current.ResourceId);
            if (sourceBeforeUpdate is null)
            {
                transaction.Rollback();
                return Missing<FavoriteRecord>();
            }

            var affected = Execute(connection, transaction, """
                UPDATE [discovery].[Favorite]
                SET [Rank] = @Rank, [UpdatedByUserId] = @UserId, [UpdatedAt] = SYSUTCDATETIME()
                WHERE [OwnerId] = @OwnerId AND [Id] = @Id AND [RowVersion] = @RowVersion;
                """,
                ("@Rank", SqlDbType.Decimal, rank, null),
                ("@UserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
                ("@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
                ("@Id", SqlDbType.UniqueIdentifier, favoriteId, null),
                ("@RowVersion", SqlDbType.Binary, expectedVersion, 8));
            if (affected != 1)
            {
                transaction.Rollback();
                return Revision<FavoriteRecord>();
            }
            var updated = ReadFavorite(connection, transaction, actor.OwnerId, favoriteId, forUpdate: false);
            if (updated is null)
            {
                transaction.Rollback();
                return Failure<FavoriteRecord>("PersistenceFailure", 500, "Favorite could not be loaded after reordering.");
            }

            var source = ResolveSource(connection, transaction, actor, updated.ResourceType, updated.ResourceId);
            if (source is null)
            {
                transaction.Rollback();
                return Missing<FavoriteRecord>();
            }
            var record = ToRecord(source, updated);
            if (!ModuleAvailable(connection, transaction, actor, "FX25", "discovery.favorite.reorder"))
            {
                transaction.Rollback();
                return ModuleUnavailable<FavoriteRecord>();
            }
            WriteAudit(connection, transaction, actor, favoriteId, "discovery.favorite.reorder", traceId);
            CompleteReceipt(connection, transaction, receipt, "FavoriteReordered", 200, JsonSerializer.Serialize(record));
            transaction.Commit();
            return IdentityOperationResult<FavoriteRecord>.Success(record);
        }
        catch (SqlException exception)
        {
            return PersistenceFailure<FavoriteRecord>(exception);
        }
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private bool ModuleAvailable(SqlConnection connection, SqlTransaction? transaction, IdentityPrincipal actor,
        string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(connection, transaction, actor, moduleCode, actionKeys);

    private FavoriteRecord ToRecord(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, FavoriteRow row) => ToRecord(ResolveSource(connection, transaction, actor, row.ResourceType, row.ResourceId), row);

    private static FavoriteRecord ToRecord(SourceProjection? source, FavoriteRow row) => new(
        row.Id,
        row.ResourceType,
        row.ResourceId,
        source is null ? "Unavailable" : "Available",
        source?.Title,
        source?.Status,
        source?.UpdatedAt,
        source?.Route,
        row.Rank,
        row.CreatedAt,
        row.UpdatedAt,
        row.ETag);

    private SourceProjection? ResolveSource(SqlConnection connection, SqlTransaction? transaction,
        IdentityPrincipal actor, string resourceType, Guid resourceId)
    {
        (string Module, string Action, string Sql)? capability = resourceType switch
        {
            "Project" => ("FX11", "projects.project.read", """
                SELECT p.[Name], p.[Status], p.[UpdatedAt]
                FROM [productivity].[Project] p
                WHERE p.[OwnerId] = @OwnerId AND p.[Id] = @ResourceId
                  AND p.[Status] NOT IN ('Deleted','Archived')
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Project'
                        AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Task" => ("FX12", "tasks.task.read", """
                SELECT t.[Title], t.[Status], t.[UpdatedAt]
                FROM [productivity].[Task] t
                WHERE t.[OwnerId] = @OwnerId AND t.[Id] = @ResourceId
                  AND t.[Status] <> 'Deleted'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = t.[OwnerId] AND tr.[ResourceType] = 'Task'
                        AND tr.[ResourceId] = t.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Event" => ("FX13", "calendar.event.read", """
                SELECT e.[Title], e.[Status], e.[UpdatedAt]
                FROM [calendar].[Event] e
                WHERE e.[OwnerId] = @OwnerId AND e.[Id] = @ResourceId
                  AND e.[Status] <> 'Deleted'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = e.[OwnerId] AND tr.[ResourceType] = 'Event'
                        AND tr.[ResourceId] = e.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Document" => ("FX20", "documents.page.read", """
                SELECT p.[Title], p.[Status], p.[UpdatedAt]
                FROM [documents].[Page] p
                WHERE p.[OwnerId] = @OwnerId AND p.[Id] = @ResourceId
                  AND p.[DeletedAt] IS NULL AND p.[Status] <> 'Archived'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = p.[OwnerId] AND tr.[ResourceType] = 'Document'
                        AND tr.[ResourceId] = p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Bookmark" => ("FX21", "bookmarks.bookmark.read", """
                SELECT b.[Title], b.[Status], b.[UpdatedAt]
                FROM [knowledge].[Bookmark] b
                WHERE b.[OwnerId] = @OwnerId AND b.[Id] = @ResourceId
                  AND b.[Status] <> 'Archived'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = b.[OwnerId] AND tr.[ResourceType] = 'Bookmark'
                        AND tr.[ResourceId] = b.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Snippet" => ("FX22", "snippets.snippet.read", """
                SELECT v.[Title], s.[Status], s.[UpdatedAt]
                FROM [knowledge].[Snippet] s
                INNER JOIN [knowledge].[SnippetVersion] v
                    ON v.[OwnerId] = s.[OwnerId] AND v.[SnippetId] = s.[Id]
                   AND v.[VersionNumber] = s.[CurrentVersion]
                WHERE s.[OwnerId] = @OwnerId AND s.[Id] = @ResourceId
                  AND s.[Status] <> 'Archived'
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = s.[OwnerId] AND tr.[ResourceType] = 'Snippet'
                        AND tr.[ResourceId] = s.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            "Goal" => ("FX16", "goals.goal.read", """
                SELECT g.[Title], g.[Status], g.[UpdatedAt]
                FROM [productivity].[Goal] g
                WHERE g.[OwnerId] = @OwnerId AND g.[Id] = @ResourceId
                  AND g.[Status] NOT IN ('Deleted','Archived')
                  AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr
                      WHERE tr.[OwnerId] = g.[OwnerId] AND tr.[ResourceType] = 'Goal'
                        AND tr.[ResourceId] = g.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
                """),
            _ => default
        };
        if (capability is null || !ModuleAvailable(connection, transaction, actor, capability.Value.Module, capability.Value.Action))
            return null;

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = capability.Value.Sql;
        command.CommandTimeout = 3;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@ResourceId", SqlDbType.UniqueIdentifier, resourceId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        var title = reader.IsDBNull(0) ? null : reader.GetString(0);
        var status = reader.IsDBNull(1) ? null : reader.GetString(1);
        var updatedAt = reader.IsDBNull(2) ? null : ToOffset(reader.GetDateTime(2));
        reader.Close();
        // Re-read the source capability before returning a projection. When a
        // mutation is in flight, the transactional lock prevents a revoke or
        // disable from committing around this source read.
        if (!ModuleAvailable(connection, transaction, actor, capability.Value.Module, capability.Value.Action))
            return null;
        return new SourceProjection(title, status, updatedAt, RouteFor(resourceType));
    }

    private static string RouteFor(string resourceType) => resourceType switch
    {
        "Project" => "/modules/FX11",
        "Task" => "/modules/FX12",
        "Event" => "/modules/FX13",
        "Document" => "/modules/FX20",
        "Bookmark" => "/bookmarks",
        "Snippet" => "/snippets",
        "Goal" => "/goals",
        _ => "/"
    };

    private static string? NormalizeResourceType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value.Trim().ToLowerInvariant() switch
        {
            "project" or "projects" => "Project",
            "task" or "tasks" => "Task",
            "event" or "events" or "calendar" => "Event",
            "document" or "documents" or "note" or "knowledge" => "Document",
            "bookmark" or "bookmarks" => "Bookmark",
            "snippet" or "snippets" => "Snippet",
            "goal" or "goals" => "Goal",
            _ => null
        };
    }

    private static List<FavoriteRow> ReadRows(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, string? resourceType, int limit, FavoriteCursor? cursor)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) f.[Id], f.[ResourceType], f.[ResourceId], f.[Rank],
                   f.[CreatedAt], f.[UpdatedAt], f.[RowVersion]
            FROM [discovery].[Favorite] f
            WHERE f.[OwnerId] = @OwnerId
              AND (@ResourceType IS NULL OR f.[ResourceType] = @ResourceType)
              AND (@HasCursor = 0 OR f.[Rank] > @CursorRank
                   OR (f.[Rank] = @CursorRank AND (f.[CreatedAt] > @CursorCreatedAt
                   OR (f.[CreatedAt] = @CursorCreatedAt AND f.[Id] > @CursorId))))
            ORDER BY f.[Rank], f.[CreatedAt], f.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, limit);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ResourceType", SqlDbType.VarChar, (object?)resourceType ?? DBNull.Value, 32);
        Add(command, "@HasCursor", SqlDbType.Bit, cursor is not null);
        Add(command, "@CursorRank", SqlDbType.Decimal, (object?)cursor?.Rank ?? DBNull.Value);
        Add(command, "@CursorCreatedAt", SqlDbType.DateTime2, (object?)cursor?.CreatedAt.UtcDateTime ?? DBNull.Value);
        Add(command, "@CursorId", SqlDbType.UniqueIdentifier, (object?)cursor?.Id ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var rows = new List<FavoriteRow>();
        while (reader.Read())
            rows.Add(ReadRow(reader));
        return rows;
    }

    private static FavoriteRow? ReadFavorite(SqlConnection connection, SqlTransaction? transaction,
        Guid ownerId, Guid favoriteId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT f.[Id], f.[ResourceType], f.[ResourceId], f.[Rank],
                   f.[CreatedAt], f.[UpdatedAt], f.[RowVersion]
            FROM [discovery].[Favorite] f WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")})
            WHERE f.[OwnerId] = @OwnerId AND f.[Id] = @Id;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@Id", SqlDbType.UniqueIdentifier, favoriteId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadRow(reader) : null;
    }

    private static FavoriteRow ReadRow(SqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetGuid(2),
        reader.GetDecimal(3),
        ToOffset(reader.GetDateTime(4)),
        ToOffset(reader.GetDateTime(5)),
        EncodeETag(reader.GetFieldValue<byte[]>(6)));

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest,
        out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed || claim.IsReplay)
            return null;
        var code = claim.IsInvalid ? "IdempotencyInvalid" : claim.IsConflict ? "IdempotencyConflict" : "RequestInProgress";
        return Failure<T>(code, claim.IsInvalid ? 422 : 409, "The idempotency key is invalid or the request is already completed/in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim,
        string resultCode, int statusCode, string? resultJson) =>
        _receipts.Complete(connection, transaction, claim, resultCode, statusCode, resultJson);

    private static IdentityOperationResult<T> Replay<T>(ReceiptClaim claim)
    {
        if (claim.ResultStatusCode == 204)
            return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent");
        if (string.IsNullOrWhiteSpace(claim.ResultJson))
            return Failure<T>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");
        try
        {
            var value = JsonSerializer.Deserialize<T>(claim.ResultJson);
            return value is null
                ? Failure<T>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.")
                : IdentityOperationResult<T>.Success(value, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        }
        catch (JsonException)
        {
            return Failure<T>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");
        }
    }

    private IdentityOperationResult<FavoriteRecord> ReplayFavoriteRecord(
        SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, ReceiptClaim claim)
    {
        if (string.IsNullOrWhiteSpace(claim.ResultJson))
            return Failure<FavoriteRecord>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");

        FavoriteRecord? stored;
        try
        {
            stored = JsonSerializer.Deserialize<FavoriteRecord>(claim.ResultJson);
        }
        catch (JsonException)
        {
            return Failure<FavoriteRecord>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");
        }

        if (stored is null || stored.Id == Guid.Empty || stored.ResourceId == Guid.Empty)
            return Failure<FavoriteRecord>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");

        // A receipt is not a bypass around current source authority. Re-read
        // the owner-scoped favorite and source while the transaction still
        // holds its capability locks; unavailable sources are redacted by the
        // normal projection boundary instead of returning stale stored text.
        var current = ReadFavorite(connection, transaction, actor.OwnerId, stored.Id, forUpdate: true);
        if (current is null)
            return Failure<FavoriteRecord>("IdempotencyReplayUnavailable", 409, "The idempotent response is unavailable; retry with a new key.");
        var source = ResolveSource(connection, transaction, actor, current.ResourceType, current.ResourceId);
        var record = ToRecord(source, current);
        return IdentityOperationResult<FavoriteRecord>.Success(
            record, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor,
        Guid targetId, string actionKey, string? traceId) => Execute(connection, transaction, """
            INSERT INTO [security].[AuditEvent]
                ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@ActorUserId, @OwnerUserId, @ActionKey, N'discovery.Favorite', @TargetId, 'Succeeded', @TraceId);
            """,
        ("@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId, null),
        ("@OwnerUserId", SqlDbType.UniqueIdentifier, actor.OwnerId, null),
        ("@ActionKey", SqlDbType.NVarChar, actionKey, 160),
        ("@TargetId", SqlDbType.UniqueIdentifier, targetId, null),
        ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 200));

    private static int Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value, int? Size)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
            Add(command, parameter.Name, parameter.Type, parameter.Value, parameter.Size);
        return command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int? size = null)
    {
        var parameter = size is null ? command.Parameters.Add(name, type) : command.Parameters.Add(name, type, size.Value);
        if (type == SqlDbType.Decimal)
        {
            parameter.Precision = 28;
            parameter.Scale = 8;
        }
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> ModuleUnavailable<T>() =>
        Failure<T>("ModuleUnavailable", 409, "Favorites are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() =>
        Failure<T>("ResourceUnavailable", 404, "Favorite source is unavailable.");
    private static IdentityOperationResult<T> Precondition<T>() =>
        Failure<T>("PreconditionRequired", 428, "If-Match is required and must be a quoted rowversion.");
    private static IdentityOperationResult<T> Revision<T>() =>
        Failure<T>("RevisionConflict", 412, "Favorite revision changed.");
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) =>
        Failure<T>("PersistenceUnavailable", 503, "Favorite persistence is unavailable.");

    private static string EncodeETag(byte[] value) => $"\"{Convert.ToBase64String(value)}\"";
    private static byte[] DecodeETag(string value) => Convert.FromBase64String(value.Trim()[1..^1]);
    private static bool TryETag(string? value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed == "*" || trimmed.Length < 3 || trimmed[0] != '"' || trimmed[^1] != '"')
                return false;
            bytes = DecodeETag(trimmed);
            return bytes.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string EncodeCursor(FavoriteRow row) => Convert.ToBase64String(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new FavoriteCursor(row.Rank, row.CreatedAt, row.Id))));

    private static FavoriteCursor? ParseCursor(string? value, out bool valid)
    {
        valid = true;
        if (string.IsNullOrWhiteSpace(value))
            return null;
        try
        {
            var cursor = JsonSerializer.Deserialize<FavoriteCursor>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
            if (cursor is null || cursor.Id == Guid.Empty || cursor.Rank < 0 || cursor.Rank > MaxRank)
            {
                valid = false;
                return null;
            }
            return cursor;
        }
        catch (FormatException)
        {
            valid = false;
            return null;
        }
        catch (JsonException)
        {
            valid = false;
            return null;
        }
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record SourceProjection(string? Title, string? Status, DateTimeOffset? UpdatedAt, string Route);
    private sealed record FavoriteCursor(decimal Rank, DateTimeOffset CreatedAt, Guid Id);
    private sealed record FavoriteRow(Guid Id, string ResourceType, Guid ResourceId, decimal Rank,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
}
