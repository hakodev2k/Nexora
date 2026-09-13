using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Trash;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Trash;

/// <summary>
/// SQL Trash provider for owner-scoped Project/Task aggregates. Restore uses
/// the recorded deletion batch rather than timestamps. Lifecycle operations
/// dispatch to the source module action and coordinate derived participants in
/// the same SQL transaction; the FX08 action is never treated as a domain grant.
/// </summary>
public sealed class SqlTrashService : ITrashService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlTrashService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<TrashPage> List(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX08", "lifecycle.trash.read")) return ModuleUnavailable<TrashPage>();
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
        if (!ModuleAvailable(actor, "FX08", "lifecycle.resource.restore")) return ModuleUnavailable<TrashRestoreResult>();
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
            var capabilityFailure = ValidateSourceLifecycleCapabilities<TrashRestoreResult>(connection, transaction, actor, items, "restore");
            if (capabilityFailure is not null) return Rollback(transaction, capabilityFailure);
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
                        RefreshTaskCalendarProjection(connection, transaction, actor.OwnerId, task.ResourceId);
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
                    RefreshTaskCalendarProjection(connection, transaction, actor.OwnerId, task.ResourceId);
                    Execute(connection, transaction,
                        "UPDATE [platform].[TrashItem] SET [RestoredAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;",
                        ("@Id", SqlDbType.UniqueIdentifier, (object)task.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
                    restored++;
                }
            }

            WriteAudit(connection, transaction, actor, project?.ResourceId, "trash.restore", traceId);
            var restoreResult = new TrashRestoreResult(deletionBatchId, restored, remaining);
            CompleteReceipt(connection, transaction, receipt, "TrashRestored", 200, JsonSerializer.Serialize(restoreResult));
            transaction.Commit();
            return IdentityOperationResult<TrashRestoreResult>.Success(restoreResult);
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
        if (!ModuleAvailable(actor, "FX08", "lifecycle.resource.purge")) return ModuleUnavailable<object?>();
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
            var items = ReadBatch(connection, transaction, actor.OwnerId, command.DeletionBatchId, forUpdate: true, includeRestored: true);
            if (items.Count == 0) { transaction.Rollback(); return Failure<object?>("ResourceUnavailable", 404, "Trash batch unavailable."); }
            if (items.Any(item => item.RestoredAt is not null))
            {
                transaction.Rollback();
                return Failure<object?>("TrashAlreadyRestored", 409, "A restored Trash item cannot be purged through this batch.");
            }
            var capabilityFailure = ValidateSourceLifecycleCapabilities<object?>(connection, transaction, actor, items, "purge");
            if (capabilityFailure is not null) return Rollback(transaction, capabilityFailure);
            var taskIds = items.Where(item => item.ResourceType == "Task").Select(item => item.ResourceId).ToArray();
            var projectIds = items.Where(item => item.ResourceType == "Project").Select(item => item.ResourceId).ToArray();
            var aggregateTaskIds = taskIds
                .Concat(projectIds.SelectMany(projectId => ReadProjectTaskIds(connection, transaction, actor.OwnerId, projectId)))
                .Distinct()
                .ToArray();
            if (HasActiveAggregateTasks(connection, transaction, actor.OwnerId, projectIds))
            {
                transaction.Rollback();
                return Failure<object?>("DependencyUnavailable", 409, "A Project with an active child Task cannot be purged.");
            }
            if (HasIndependentPurgeReferences(connection, transaction, actor.OwnerId, aggregateTaskIds, projectIds))
            {
                transaction.Rollback();
                return Failure<object?>("DependencyUnavailable", 409, "A source with an independent file reference cannot be purged.");
            }
            DeleteTrashItemsForTasks(connection, transaction, actor.OwnerId, aggregateTaskIds);
            foreach (var taskId in aggregateTaskIds)
            {
                // Calendar and Planner are derived participants. Detach them
                // explicitly before deleting the source; no blind cascade is
                // relied on for the existing NO ACTION foreign keys.
                Execute(connection, transaction, "DELETE FROM [calendar].[Event] WHERE [OwnerId] = @OwnerId AND [TaskId] = @TaskId;",
                    ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                    ("@TaskId", SqlDbType.UniqueIdentifier, (object)taskId));
                Execute(connection, transaction, "DELETE FROM [productivity].[PlannerPin] WHERE [OwnerId] = @OwnerId AND [TaskId] = @TaskId;",
                    ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                    ("@TaskId", SqlDbType.UniqueIdentifier, (object)taskId));
                Execute(connection, transaction, "DELETE FROM [calendar].[Reminder] WHERE [OwnerId] = @OwnerId AND [SourceType] = 'Task' AND [SourceId] = @TaskId;",
                    ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                    ("@TaskId", SqlDbType.UniqueIdentifier, (object)taskId));
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
            CompleteReceipt(connection, transaction, receipt, "TrashPurged", 204, null);
            transaction.Commit();
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (SqlException exception)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            return PersistenceFailure<object?>(exception);
        }
    }

    private static IReadOnlyList<TrashItemRecord> ReadBatch(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid batchId, bool forUpdate, bool includeRestored = false)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var restoredPredicate = includeRestored ? string.Empty : " AND [RestoredAt] IS NULL";
        command.CommandText = $"SELECT [Id], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus], [DeletedAt], [RestoredAt], [PurgedAt] FROM [platform].[TrashItem] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [OwnerId] = @OwnerId AND [DeletionBatchId] = @BatchId{restoredPredicate} AND [PurgedAt] IS NULL ORDER BY [ResourceType], [ResourceId];";
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

    private IdentityOperationResult<T>? ValidateSourceLifecycleCapabilities<T>(SqlConnection connection,
        SqlTransaction transaction, IdentityPrincipal actor, IReadOnlyList<TrashItemRecord> items, string operation)
    {
        var gatewayAction = operation == "restore" ? "lifecycle.resource.restore" : "lifecycle.resource.purge";
        var gatewayStatus = _capabilities.Evaluate(connection, transaction, actor, "FX08", gatewayAction);
        if (gatewayStatus == SqlCapabilityStatus.PermissionDenied)
            return Failure<T>("PermissionDenied", 403, "The lifecycle action is not allowed for the current user.");
        if (gatewayStatus != SqlCapabilityStatus.Allowed)
            return Failure<T>("ModuleUnavailable", 409, "The lifecycle module is disabled or unavailable.");

        foreach (var resourceType in items.Select(item => item.ResourceType).Distinct(StringComparer.Ordinal))
        {
            var (module, action) = resourceType switch
            {
                "Project" => ("FX11", operation == "restore" ? "projects.project.restore" : "projects.project.purge"),
                "Task" => ("FX12", operation == "restore" ? "tasks.task.restore" : "tasks.task.purge"),
                _ => (string.Empty, string.Empty)
            };
            if (string.IsNullOrEmpty(module))
                return Failure<T>("UnsupportedResourceType", 409, "The Trash batch contains an unsupported resource type.");
            var status = _capabilities.Evaluate(connection, transaction, actor, module, action);
            if (status == SqlCapabilityStatus.PermissionDenied)
                return Failure<T>("PermissionDenied", 403, $"The source action {action} is not allowed.");
            if (status != SqlCapabilityStatus.Allowed)
                return Failure<T>("ModuleUnavailable", 409, $"The source module for {resourceType} is disabled or unavailable.");
        }

        // Project purge also permanently deletes every child Task in the
        // aggregate. Require the Task source action for those derived
        // participants even if an inconsistent/older Trash batch omitted a
        // child TrashItem row. Restore does not touch omitted children.
        if (operation == "purge")
        {
            var projectIds = items.Where(item => item.ResourceType == "Project")
                .Select(item => item.ResourceId)
                .ToArray();
            if (projectIds.Any(projectId => ReadProjectTaskIds(connection, transaction, actor.OwnerId, projectId).Count > 0))
            {
                var taskStatus = _capabilities.Evaluate(connection, transaction, actor, "FX12", "tasks.task.purge");
                if (taskStatus == SqlCapabilityStatus.PermissionDenied)
                    return Failure<T>("PermissionDenied", 403, "The source action tasks.task.purge is not allowed.");
                if (taskStatus != SqlCapabilityStatus.Allowed)
                    return Failure<T>("ModuleUnavailable", 409, "The source module for Task is disabled or unavailable.");
            }
        }

        return null;
    }

    private static IReadOnlyList<Guid> ReadProjectTaskIds(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, Guid projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT [Id] FROM [productivity].[Task] WITH (UPDLOCK, ROWLOCK) WHERE [OwnerId] = @OwnerId AND [ProjectId] = @ProjectId;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        using var reader = command.ExecuteReader();
        var ids = new List<Guid>();
        while (reader.Read()) ids.Add(reader.GetGuid(0));
        return ids;
    }

    private static bool HasActiveAggregateTasks(SqlConnection connection, SqlTransaction transaction, Guid ownerId, IReadOnlyList<Guid> projectIds)
    {
        if (projectIds.Count == 0) return false;
        var parameters = new List<(string Name, SqlDbType Type, object Value)> { ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId) };
        var names = new List<string>();
        for (var index = 0; index < projectIds.Count; index++)
        {
            var name = "@Project" + index;
            names.Add(name);
            parameters.Add((name, SqlDbType.UniqueIdentifier, projectIds[index]));
        }
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM [productivity].[Task] WHERE [OwnerId] = @OwnerId AND [ProjectId] IN ({string.Join(',', names)}) AND [Status] <> 'Deleted') THEN 1 ELSE 0 END;";
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static bool HasIndependentPurgeReferences(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, IReadOnlyList<Guid> taskIds, IReadOnlyList<Guid> projectIds)
    {
        if (taskIds.Count == 0 && projectIds.Count == 0) return false;
        var parameters = new List<(string Name, SqlDbType Type, object Value)>
        {
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId)
        };
        var clauses = new List<string>();
        if (taskIds.Count > 0)
        {
            var names = new List<string>();
            for (var index = 0; index < taskIds.Count; index++)
            {
                var name = "@Task" + index;
                names.Add(name);
                parameters.Add((name, SqlDbType.UniqueIdentifier, taskIds[index]));
            }
            clauses.Add($"(referenceRow.[ResourceType] = 'Task' AND referenceRow.[ResourceId] IN ({string.Join(',', names)}))");
        }
        if (projectIds.Count > 0)
        {
            var names = new List<string>();
            for (var index = 0; index < projectIds.Count; index++)
            {
                var name = "@Project" + index;
                names.Add(name);
                parameters.Add((name, SqlDbType.UniqueIdentifier, projectIds[index]));
            }
            clauses.Add($"(referenceRow.[ResourceType] = 'Project' AND referenceRow.[ResourceId] IN ({string.Join(',', names)}))");
        }
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT CASE WHEN EXISTS
            (
                SELECT 1 FROM [files].[FileReference] referenceRow
                WHERE referenceRow.[OwnerId] = @OwnerId
                  AND ({string.Join(" OR ", clauses)})
            ) THEN 1 ELSE 0 END;
            """;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Type, parameter.Value);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static void DeleteTrashItemsForTasks(SqlConnection connection, SqlTransaction transaction,
        Guid ownerId, IReadOnlyList<Guid> taskIds)
    {
        if (taskIds.Count == 0) return;
        var parameters = new List<(string Name, SqlDbType Type, object Value)>
        {
            ("@OwnerId", SqlDbType.UniqueIdentifier, ownerId)
        };
        var names = new List<string>(taskIds.Count);
        for (var index = 0; index < taskIds.Count; index++)
        {
            var name = "@Task" + index;
            names.Add(name);
            parameters.Add((name, SqlDbType.UniqueIdentifier, taskIds[index]));
        }

        Execute(connection, transaction,
            $"DELETE FROM [platform].[TrashItem] WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'Task' AND [ResourceId] IN ({string.Join(',', names)});",
            parameters.ToArray());
    }

    private static void RefreshTaskCalendarProjection(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid taskId)
    {
        Execute(connection, transaction, """
            UPDATE eventRow
            SET [Title] = taskRow.[Title], [Description] = taskRow.[Description], [StartAt] = taskRow.[StartAt],
                [EndAt] = taskRow.[EndAt], [TimeZoneId] = timeZoneRow.[TimeZoneId],
                [Status] = CASE taskRow.[Status] WHEN 'Completed' THEN 'Completed' WHEN 'Skipped' THEN 'Canceled' ELSE 'Scheduled' END,
                [UpdatedAt] = SYSUTCDATETIME()
            FROM [calendar].[Event] eventRow
            INNER JOIN [productivity].[Task] taskRow ON taskRow.[Id] = eventRow.[TaskId] AND taskRow.[OwnerId] = eventRow.[OwnerId]
            INNER JOIN [platform].[PersonalSpace] spaceRow ON spaceRow.[Id] = taskRow.[OwnerId]
            INNER JOIN [identity].[User] timeZoneRow ON timeZoneRow.[Id] = spaceRow.[UserId]
            WHERE eventRow.[OwnerId] = @OwnerId AND eventRow.[TaskId] = @TaskId;
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [calendar].[Event]
                    ([OwnerId], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [Status], [SourceUid], [SourceKind], [TaskId])
                SELECT taskRow.[OwnerId], taskRow.[Title], taskRow.[Description], taskRow.[StartAt], taskRow.[EndAt], timeZoneRow.[TimeZoneId],
                       CASE taskRow.[Status] WHEN 'Completed' THEN 'Completed' WHEN 'Skipped' THEN 'Canceled' ELSE 'Scheduled' END,
                       NULL, 'Task', taskRow.[Id]
                FROM [productivity].[Task] taskRow
                INNER JOIN [platform].[PersonalSpace] spaceRow ON spaceRow.[Id] = taskRow.[OwnerId]
                INNER JOIN [identity].[User] timeZoneRow ON timeZoneRow.[Id] = spaceRow.[UserId]
                WHERE taskRow.[OwnerId] = @OwnerId AND taskRow.[Id] = @TaskId AND taskRow.[Status] <> 'Deleted';
            END;
            """,
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
            ("@TaskId", SqlDbType.UniqueIdentifier, (object)taskId));
    }

    private static bool ParentAllowsTaskRestore(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid taskId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN p.[Status] NOT IN ('Completed','Skipped','Deleted') THEN 1 ELSE 0 END FROM [productivity].[Task] t INNER JOIN [productivity].[Project] p ON p.[Id] = t.[ProjectId] AND p.[OwnerId] = t.[OwnerId] WHERE t.[Id] = @TaskId AND t.[OwnerId] = @OwnerId;";
        Add(command, "@TaskId", SqlDbType.UniqueIdentifier, taskId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid)
            return Failure<T>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay && claim.ResultStatusCode == 204)
            return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent");
        if (claim.IsReplay && !string.IsNullOrWhiteSpace(claim.ResultJson))
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(claim.ResultJson);
                if (value is not null)
                    return IdentityOperationResult<T>.Success(value, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "IdempotencyReplay");
            }
            catch (JsonException) { }
            catch (NotSupportedException) { }
        }
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        return Failure<T>(code, 409, "The request was already completed or is in progress.");
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode,
        int? resultStatusCode = null, string? resultJson = null) =>
        _receipts.Complete(connection, transaction, claim, resultCode, resultStatusCode, resultJson);

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid? targetId, string action, string? traceId) => Execute(connection, transaction,
        "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @Owner, @Action, N'platform.TrashItem', @Target, 'Succeeded', @TraceId);",
        ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Owner", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@Action", SqlDbType.NVarChar, (object)action), ("@Target", SqlDbType.UniqueIdentifier, (object?)targetId ?? DBNull.Value), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

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

    private static IdentityOperationResult<T> Rollback<T>(SqlTransaction transaction, IdentityOperationResult<T> result)
    {
        transaction.Rollback();
        return result;
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Trash is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> PersistenceFailure<T>(SqlException exception) => Failure<T>("PersistenceUnavailable", 503, "Trash persistence is unavailable.");
    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
