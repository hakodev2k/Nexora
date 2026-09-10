using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Trash;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Trash;

/// <summary>
/// SQL Trash provider for owner-scoped Project/Task aggregates. Restore uses
/// the recorded deletion batch rather than timestamps; purge is explicit and
/// irreversible, and Calendar rows are intentionally outside this provider.
/// </summary>
public sealed class SqlTrashService : ITrashService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;

    public SqlTrashService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<TrashPage> List(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX08")) return ModuleUnavailable<TrashPage>();
        var take = Math.Clamp(limit ?? 100, 1, 200);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus], [DeletedAt], [RestoredAt], [PurgedAt]
            FROM [platform].[TrashItem]
            WHERE [OwnerId] = @OwnerId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL
            ORDER BY [DeletedAt] DESC, [Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        var items = new List<TrashItemRecord>();
        while (reader.Read()) items.Add(Read(reader));
        return IdentityOperationResult<TrashPage>.Success(new TrashPage(items, null));
    }

    public IdentityOperationResult<TrashRestoreResult> Restore(IdentityPrincipal actor, Guid deletionBatchId,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX08")) return ModuleUnavailable<TrashRestoreResult>();
        if (deletionBatchId == Guid.Empty) return Failure<TrashRestoreResult>("ValidationFailed", 422, "Deletion batch is required.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<TrashRestoreResult>(connection, transaction, actor,
            "trash.restore", idempotencyKey, $"batch:{deletionBatchId:N}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var items = ReadBatch(connection, transaction, actor.OwnerId, deletionBatchId, forUpdate: true);
            if (items.Count == 0) { transaction.Rollback(); return Failure<TrashRestoreResult>("ResourceUnavailable", 404, "Trash batch unavailable."); }
            var project = items.FirstOrDefault(item => item.ResourceType == "Project");
            var tasks = items.Where(item => item.ResourceType == "Task").ToArray();
            var restored = 0;
            var remaining = 0;

            if (project is not null)
            {
                Execute(connection, transaction,
                    "UPDATE [productivity].[Project] SET [Status] = @Status, [DeletedAt] = NULL, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] = 'Deleted';",
                    ("@Status", SqlDbType.VarChar, (object)project.PriorStatus), ("@Id", SqlDbType.UniqueIdentifier, (object)project.ResourceId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                Execute(connection, transaction,
                    "UPDATE [platform].[TrashItem] SET [RestoredAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;",
                    ("@Id", SqlDbType.UniqueIdentifier, (object)project.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                restored++;

                // A terminal Project is a valid restored parent but cannot
                // resurrect its child tasks. Those rows stay in Trash for an
                // explicit task-level decision.
                if (project.PriorStatus is "Completed" or "Skipped")
                {
                    remaining = tasks.Length;
                }
                else
                {
                    foreach (var task in tasks)
                    {
                        Execute(connection, transaction,
                            "UPDATE [productivity].[Task] SET [Status] = @Status, [DeletedAt] = NULL, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] = 'Deleted' AND [ProjectId] = @ProjectId;",
                            ("@Status", SqlDbType.VarChar, (object)task.PriorStatus), ("@Id", SqlDbType.UniqueIdentifier, (object)task.ResourceId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@ProjectId", SqlDbType.UniqueIdentifier, (object)project.ResourceId));
                        Execute(connection, transaction,
                            "UPDATE [platform].[TrashItem] SET [RestoredAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;",
                            ("@Id", SqlDbType.UniqueIdentifier, (object)task.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                        restored++;
                    }
                }
            }
            else
            {
                foreach (var task in tasks)
                {
                    if (!ParentAllowsTaskRestore(connection, transaction, actor.OwnerId, task.ResourceId))
                    {
                        transaction.Rollback();
                        return Failure<TrashRestoreResult>("ParentUnavailable", 409, "Task cannot be restored while its Project is terminal or unavailable.");
                    }
                    Execute(connection, transaction,
                        "UPDATE [productivity].[Task] SET [Status] = @Status, [DeletedAt] = NULL, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] = 'Deleted';",
                        ("@Status", SqlDbType.VarChar, (object)task.PriorStatus), ("@Id", SqlDbType.UniqueIdentifier, (object)task.ResourceId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                    Execute(connection, transaction,
                        "UPDATE [platform].[TrashItem] SET [RestoredAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;",
                        ("@Id", SqlDbType.UniqueIdentifier, (object)task.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                    restored++;
                }
            }

            WriteAudit(connection, transaction, actor, project?.ResourceId, "trash.restore", traceId);
            CompleteReceipt(connection, transaction, receipt, "TrashRestored");
            transaction.Commit();
            return IdentityOperationResult<TrashRestoreResult>.Success(new TrashRestoreResult(deletionBatchId, restored, remaining));
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<TrashRestoreResult>(exception);
        }
    }

    public IdentityOperationResult<object?> Purge(IdentityPrincipal actor, TrashPurgeCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX08")) return ModuleUnavailable<object?>();
        if (command.DeletionBatchId == Guid.Empty) return Failure<object?>("ValidationFailed", 422, "Deletion batch is required.");
        if (!string.Equals(command.Confirmation?.Trim(), "PURGE", StringComparison.Ordinal))
            return Failure<object?>("ConfirmationRequired", 422, "Type PURGE to permanently delete this Trash batch.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "trash.purge", idempotencyKey,
            $"batch:{command.DeletionBatchId:N}|confirmation:PURGE", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        try
        {
            var items = ReadBatch(connection, transaction, actor.OwnerId, command.DeletionBatchId, forUpdate: true);
            if (items.Count == 0) { transaction.Rollback(); return Failure<object?>("ResourceUnavailable", 404, "Trash batch unavailable."); }
            if (items.Any(item => item.RestoredAt is not null))
            {
                transaction.Rollback();
                return Failure<object?>("TrashAlreadyRestored", 409, "A restored Trash item cannot be purged through this batch.");
            }
            var taskIds = items.Where(item => item.ResourceType == "Task").Select(item => item.ResourceId).ToArray();
            var projectIds = items.Where(item => item.ResourceType == "Project").Select(item => item.ResourceId).ToArray();
            foreach (var taskId in taskIds)
            {
                Execute(connection, transaction, "DELETE FROM [productivity].[TaskHistory] WHERE [TaskId] = @Id;", ("@Id", SqlDbType.UniqueIdentifier, (object)taskId));
                Execute(connection, transaction, "DELETE FROM [productivity].[Task] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] = 'Deleted';", ("@Id", SqlDbType.UniqueIdentifier, (object)taskId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
            }
            foreach (var projectId in projectIds)
            {
                Execute(connection, transaction, "DELETE FROM [productivity].[ProjectHistory] WHERE [ProjectId] = @Id;", ("@Id", SqlDbType.UniqueIdentifier, (object)projectId));
                Execute(connection, transaction, "DELETE FROM [productivity].[Project] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] = 'Deleted';", ("@Id", SqlDbType.UniqueIdentifier, (object)projectId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
            }
            WriteAudit(connection, transaction, actor, projectIds.FirstOrDefault(), "trash.purge", traceId);
            Execute(connection, transaction, "DELETE FROM [platform].[TrashItem] WHERE [OwnerId] = @OwnerId AND [DeletionBatchId] = @BatchId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;", ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@BatchId", SqlDbType.UniqueIdentifier, (object)command.DeletionBatchId));
            CompleteReceipt(connection, transaction, receipt, "TrashPurged");
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private static IReadOnlyList<TrashItemRecord> ReadBatch(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid batchId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus], [DeletedAt], [RestoredAt], [PurgedAt] FROM [platform].[TrashItem] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [OwnerId] = @OwnerId AND [DeletionBatchId] = @BatchId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL ORDER BY [ResourceType], [ResourceId];";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@BatchId", SqlDbType.UniqueIdentifier, batchId);
        using var reader = command.ExecuteReader();
        var items = new List<TrashItemRecord>();
        while (reader.Read()) items.Add(Read(reader));
        return items;
    }

    private static TrashItemRecord Read(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetGuid(3), reader.GetString(4), ToOffset(reader.GetDateTime(5)),
        reader.IsDBNull(6) ? null : ToOffset(reader.GetDateTime(6)), reader.IsDBNull(7) ? null : ToOffset(reader.GetDateTime(7)));

    private static bool ParentAllowsTaskRestore(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid taskId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN p.[Status] NOT IN ('Completed','Skipped','Deleted') THEN 1 ELSE 0 END FROM [productivity].[Task] t INNER JOIN [productivity].[Project] p ON p.[Id] = t.[ProjectId] AND p.[OwnerId] = t.[OwnerId] WHERE t.[Id] = @TaskId AND t.[OwnerId] = @OwnerId;";
        Add(command, "@TaskId", SqlDbType.UniqueIdentifier, taskId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN m.[State] = 'Ready' AND m.[SystemEnabled] = 1 AND COALESCE(g.[Enabled], 0) = 1 THEN 1 ELSE 0 END FROM [platform].[Module] m LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] = @UserId WHERE m.[Code] = @Code;";
        Add(command, "@UserId", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(command, "@Code", SqlDbType.VarChar, moduleCode);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid? targetId, string action, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'platform.TrashItem', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@Action", SqlDbType.NVarChar, (object)action), ("@Target", SqlDbType.UniqueIdentifier, (object?)targetId ?? DBNull.Value), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value)
    {
        var parameter = command.Parameters.Add(name, type);
        parameter.Value = value;
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Trash is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Trash persistence is unavailable.");
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
