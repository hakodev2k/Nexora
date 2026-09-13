using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;
using Nexora.Application.Reminders;
using Nexora.Domain.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Productivity;

/// <summary>
/// SQL authority for the first personal Productivity slice. Every query carries
/// the authenticated PersonalSpace owner and every write uses rowversion
/// compare-and-swap. No request can choose an owner or move a Task between
/// Projects.
/// </summary>
public sealed class SqlProductivityService : IProductivityService
{
    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;

    public SqlProductivityService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections;
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<ProjectPage> ListProjects(IdentityPrincipal actor, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX11", "projects.project.read", "projects.view")) return ModuleUnavailable<ProjectPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        ProjectListCursor? position = null;
        if (cursor is not null && (!TryDecodeCursor(cursor, out position) || position is null || position.Scope != "projects"))
            return Failure<ProjectPage>("InvalidCursor", 422, "The Project cursor is invalid or does not match this query.");
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Name], [Description], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [Notes]
            FROM [productivity].[Project]
            WHERE [OwnerId] = @OwnerId AND [Status] <> 'Deleted'
              AND (@HasCursor = 0 OR [Name] > @CursorName OR ([Name] = @CursorName AND [Id] > @CursorId))
            ORDER BY [Name] ASC, [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@HasCursor", SqlDbType.Bit, cursor is not null);
        Add(command, "@CursorName", SqlDbType.NVarChar, (object?)position?.Name ?? DBNull.Value, 200);
        Add(command, "@CursorId", SqlDbType.UniqueIdentifier, (object?)position?.Id ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var items = new List<ProjectRecord>();
        while (reader.Read()) items.Add(ReadProject(reader));
        var hasMore = items.Count > take;
        if (hasMore) items.RemoveAt(items.Count - 1);
        var nextCursor = hasMore && items.Count > 0
            ? EncodeCursor(new ProjectListCursor("projects", items[^1].Name, items[^1].Id))
            : null;
        return IdentityOperationResult<ProjectPage>.Success(new ProjectPage(items, nextCursor));
    }

    public IdentityOperationResult<ProjectRecord> GetProject(IdentityPrincipal actor, Guid projectId)
    {
        if (!ModuleAvailable(actor, "FX11", "projects.project.read", "projects.view")) return ModuleUnavailable<ProjectRecord>();
        if (projectId == Guid.Empty) return Failure<ProjectRecord>("ResourceUnavailable", 404, "Project unavailable.");
        using var connection = _connections.Create();
        connection.Open();
        var project = ReadProject(connection, null, actor.OwnerId, projectId, forUpdate: false);
        return project is null || project.Status == "Deleted"
            ? Failure<ProjectRecord>("ResourceUnavailable", 404, "Project unavailable.")
            : IdentityOperationResult<ProjectRecord>.Success(project);
    }

    public IdentityOperationResult<ProjectRecord> CreateProject(IdentityPrincipal actor, ProjectCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11", "projects.project.create", "projects.create")) return ModuleUnavailable<ProjectRecord>();
        var validation = ValidateProject(command);
        if (validation is not null) return validation;
        var id = Guid.NewGuid();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<ProjectRecord>(connection, transaction, actor,
            "productivity.project.create",
            idempotencyKey,
            CanonicalProject(command),
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO [productivity].[Project] ([Id], [OwnerId], [Name], [Description], [StartAt], [EndAt], [Priority], [TagsJson], [Notes], [Status])
            VALUES (@Id, @OwnerId, @Name, @Description, @StartAt, @EndAt, @Priority, @TagsJson, @Notes, 'NotStarted');
            """;
        Add(insert, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(insert, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(insert, "@Name", SqlDbType.NVarChar, command.Name.Trim(), 200);
        Add(insert, "@Description", SqlDbType.NVarChar, command.Description!.Trim(), 20000);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(insert, "@Priority", SqlDbType.VarChar, command.Priority.Trim(), 2);
        Add(insert, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(insert, "@Notes", SqlDbType.NVarChar, (object?)TrimOrNull(command.Notes, 20000) ?? DBNull.Value, -1);
        insert.ExecuteNonQuery();
        var result = ReadProject(connection, transaction, actor.OwnerId, id, forUpdate: false);
        if (result is not null) InsertProjectHistory(connection, transaction, actor.OwnerId, result, null);
        WriteAudit(connection, transaction, actor, id, "productivity.project.create", traceId);
        CompleteReceipt(connection, transaction, receipt, "ProjectCreated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<ProjectRecord>.Failure("PersistenceFailure", 500, "Project could not be loaded after creation.")
            : IdentityOperationResult<ProjectRecord>.Success(result);
    }

    public IdentityOperationResult<ProjectRecord> UpdateProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, ProjectCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11", "projects.project.update", "projects.update")) return ModuleUnavailable<ProjectRecord>();
        var validation = ValidateProject(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<ProjectRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<ProjectRecord>(connection, transaction, actor,
            "productivity.project.update",
            idempotencyKey,
            CanonicalProject(command, projectId, ifMatch),
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadProject(connection, transaction, actor.OwnerId, projectId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("ResourceUnavailable", 404, "Project unavailable.");
        }
        if (current.Status is "Completed" or "Skipped")
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("ProjectTerminal", 409, "Completed or skipped Projects are permanently read-only.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("RevisionConflict", 412, "Project revision changed.");
        }
        if (!command.ConfirmTaskBounds && HasTasksOutsideProjectBounds(connection, transaction, actor.OwnerId, projectId, command.StartAt!.Value, command.EndAt!.Value))
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("ProjectTaskTimeWarning", 409, "One or more Tasks fall outside the new Project time bounds. Confirm to save without moving them.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [productivity].[Project]
            SET [Name] = @Name, [Description] = @Description, [StartAt] = @StartAt, [EndAt] = @EndAt,
                [Priority] = @Priority, [TagsJson] = @TagsJson, [Notes] = @Notes, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] <> 'Deleted';
            """;
        Add(update, "@Name", SqlDbType.NVarChar, command.Name.Trim(), 200);
        Add(update, "@Description", SqlDbType.NVarChar, command.Description!.Trim(), 20000);
        Add(update, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(update, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(update, "@Priority", SqlDbType.VarChar, command.Priority.Trim(), 2);
        Add(update, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(update, "@Notes", SqlDbType.NVarChar, (object?)TrimOrNull(command.Notes, 20000) ?? DBNull.Value, -1);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, projectId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("RevisionConflict", 412, "Project revision changed.");
        }
        var result = ReadProject(connection, transaction, actor.OwnerId, projectId, forUpdate: false);
        if (result is not null) InsertProjectHistory(connection, transaction, actor.OwnerId, result, command.TransitionReason);
        WriteAudit(connection, transaction, actor, projectId, "productivity.project.update", traceId);
        CompleteReceipt(connection, transaction, receipt, "ProjectUpdated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<ProjectRecord>.Failure("PersistenceFailure", 500, "Project could not be loaded after update.")
            : IdentityOperationResult<ProjectRecord>.Success(result);
    }

    public IdentityOperationResult<ProjectRecord> TransitionProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null, bool confirmed = false)
    {
        if (!ModuleAvailable(actor, "FX11", ProjectTransitionAction(status))) return ModuleUnavailable<ProjectRecord>();
        if (status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project status is invalid.");
        if (!TryDecodeETag(ifMatch, out var expectedVersion))
            return Precondition<ProjectRecord>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<ProjectRecord>(connection, transaction, actor, "productivity.project.transition",
            idempotencyKey, $"project:{projectId:N}|etag:{ifMatch}|status:{status}|reason:{reason?.Trim()}|confirmed:{confirmed}", out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }

        var current = ReadProject(connection, transaction, actor.OwnerId, projectId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return MissingProject();
        }

        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return Revision<ProjectRecord>("Project");
        }

        if (current.Status is "Completed" or "Skipped" || !IsAllowedStatusTransition(current.Status, status, reason))
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("ProjectStateTransitionInvalid", 409, "The requested Project status transition is not supported.");
        }

        var hasOpenTasks = HasOpenTasks(connection, transaction, actor.OwnerId, projectId);
        var hasTasks = HasTasks(connection, transaction, actor.OwnerId, projectId);
        if (status is "Completed" or "Skipped" && !confirmed)
        {
            transaction.Rollback();
            var code = status == "Completed" && hasTasks && !hasOpenTasks
                ? "ProjectCompletionConfirmationRequired"
                : "ProjectConfirmationRequired";
            return IdentityOperationResult<ProjectRecord>.Failure(code, 409, "Confirm that the Project will become permanently read-only.");
        }

        if (status == "Completed" && string.IsNullOrWhiteSpace(reason) && hasOpenTasks)
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("TransitionReasonRequired", 422, "A reason is required when closing a Project with unfinished Tasks.");
        }

        Execute(connection, transaction,
            "UPDATE [productivity].[Project] SET [Status] = @Status, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
            ("@Status", SqlDbType.VarChar, (object)status),
            ("@Id", SqlDbType.UniqueIdentifier, (object)projectId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        InsertProjectHistory(connection, transaction, actor.OwnerId, current with { Status = status }, reason);
        WriteAudit(connection, transaction, actor, projectId, "productivity.project.transition", traceId);
        var updated = ReadProject(connection, transaction, actor.OwnerId, projectId, forUpdate: false);
        CompleteReceipt(connection, transaction, receipt, "ProjectTransitioned", 200, updated is null ? null : JsonSerializer.Serialize(updated));
        transaction.Commit();
        return updated is null
            ? Failure<ProjectRecord>("PersistenceFailure", 500, "Project could not be loaded after transition.")
            : IdentityOperationResult<ProjectRecord>.Success(updated);
    }

    public IdentityOperationResult<object?> DeleteProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11", "projects.project.trash", "projects.delete")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "productivity.project.delete",
            idempotencyKey,
            $"project:{projectId:N}|etag:{ifMatch}",
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadProject(connection, transaction, actor.OwnerId, projectId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("ResourceUnavailable", 404, "Project unavailable.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("RevisionConflict", 412, "Project revision changed.");
        }
        var deletionBatchId = Guid.NewGuid();
        Execute(connection, transaction,
            "INSERT INTO [platform].[TrashItem] ([OwnerId], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus]) SELECT @OwnerId, 'Project', [Id], @Batch, [Status] FROM [productivity].[Project] WHERE [Id] = @ProjectId AND [OwnerId] = @OwnerId AND [Status] <> 'Deleted';",
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@ProjectId", SqlDbType.UniqueIdentifier, (object)projectId),
            ("@Batch", SqlDbType.UniqueIdentifier, (object)deletionBatchId));
        Execute(connection, transaction,
            "INSERT INTO [platform].[TrashItem] ([OwnerId], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus]) SELECT @OwnerId, 'Task', [Id], @Batch, [Status] FROM [productivity].[Task] WHERE [ProjectId] = @ProjectId AND [OwnerId] = @OwnerId AND [Status] <> 'Deleted';",
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@ProjectId", SqlDbType.UniqueIdentifier, (object)projectId),
            ("@Batch", SqlDbType.UniqueIdentifier, (object)deletionBatchId));
        Execute(connection, transaction,
            "UPDATE [productivity].[Task] SET [Status] = 'Deleted', [DeletedAt] = SYSUTCDATETIME(), [UpdatedAt] = SYSUTCDATETIME() WHERE [ProjectId] = @ProjectId AND [OwnerId] = @OwnerId AND [Status] <> 'Deleted';",
            ("@ProjectId", SqlDbType.UniqueIdentifier, (object)projectId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
        Execute(connection, transaction,
            "UPDATE [productivity].[Project] SET [Status] = 'Deleted', [DeletedAt] = SYSUTCDATETIME(), [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
            ("@Id", SqlDbType.UniqueIdentifier, (object)projectId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        InsertProjectHistory(connection, transaction, actor.OwnerId, current with { Status = "Deleted" }, "Trash");
        WriteAudit(connection, transaction, actor, projectId, "productivity.project.delete", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent", 204, null);
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<TaskPage> ListTasks(IdentityPrincipal actor, Guid? projectId = null, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX12", "tasks.task.read", "tasks.view")) return ModuleUnavailable<TaskPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var taskScope = projectId?.ToString("N") ?? string.Empty;
        TaskListCursor? position = null;
        if (cursor is not null && (!TryDecodeCursor(cursor, out position) || position is null || position.Scope != taskScope))
            return Failure<TaskPage>("InvalidCursor", 422, "The Task cursor is invalid or does not match this query.");
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [ProjectId], [Title], [Description], [Status], [DueAt], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson], [Rank], [ReminderAt]
            FROM [productivity].[Task]
            WHERE [OwnerId] = @OwnerId AND [Status] <> 'Deleted'
              AND (@ProjectId IS NULL OR [ProjectId] = @ProjectId)
              AND (@HasCursor = 0 OR
                   CASE WHEN [DueAt] IS NULL THEN 1 ELSE 0 END > @CursorDueNull
                   OR (CASE WHEN [DueAt] IS NULL THEN 1 ELSE 0 END = @CursorDueNull AND
                       ((@CursorDueNull = 0 AND ([DueAt] > @CursorDueAt OR ([DueAt] = @CursorDueAt AND ([UpdatedAt] < @CursorUpdatedAt OR ([UpdatedAt] = @CursorUpdatedAt AND [Id] > @CursorId)))))
                        OR (@CursorDueNull = 1 AND ([UpdatedAt] < @CursorUpdatedAt OR ([UpdatedAt] = @CursorUpdatedAt AND [Id] > @CursorId))))))
            ORDER BY CASE WHEN [DueAt] IS NULL THEN 1 ELSE 0 END, [DueAt], [UpdatedAt] DESC, [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, (object?)projectId ?? DBNull.Value);
        Add(command, "@HasCursor", SqlDbType.Bit, cursor is not null);
        Add(command, "@CursorDueNull", SqlDbType.Int, position is null || !position.HasDueAt ? 1 : 0);
        Add(command, "@CursorDueAt", SqlDbType.DateTime2, (object?)position?.DueAt ?? DBNull.Value);
        Add(command, "@CursorUpdatedAt", SqlDbType.DateTime2, (object?)position?.UpdatedAt ?? DBNull.Value);
        Add(command, "@CursorId", SqlDbType.UniqueIdentifier, (object?)position?.Id ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var items = new List<TaskRecord>();
        while (reader.Read()) items.Add(ReadTask(reader));
        var hasMore = items.Count > take;
        if (hasMore) items.RemoveAt(items.Count - 1);
        var nextCursor = hasMore && items.Count > 0
            ? EncodeCursor(new TaskListCursor(taskScope, items[^1].DueAt is not null, items[^1].DueAt?.UtcDateTime, items[^1].UpdatedAt.UtcDateTime, items[^1].Id))
            : null;
        return IdentityOperationResult<TaskPage>.Success(new TaskPage(items, nextCursor));
    }

    public IdentityOperationResult<TaskRecord> GetTask(IdentityPrincipal actor, Guid taskId)
    {
        if (!ModuleAvailable(actor, "FX12", "tasks.task.read", "tasks.view")) return ModuleUnavailable<TaskRecord>();
        if (taskId == Guid.Empty) return Failure<TaskRecord>("ResourceUnavailable", 404, "Task unavailable.");
        using var connection = _connections.Create();
        connection.Open();
        var task = ReadTask(connection, null, actor.OwnerId, taskId, forUpdate: false);
        return task is null || task.Status == "Deleted"
            ? Failure<TaskRecord>("ResourceUnavailable", 404, "Task unavailable.")
            : IdentityOperationResult<TaskRecord>.Success(task);
    }

    public IdentityOperationResult<TaskRecord> CreateTask(IdentityPrincipal actor, TaskCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12", "tasks.task.create", "tasks.create")) return ModuleUnavailable<TaskRecord>();
        var validation = ValidateTask(command);
        if (validation is not null) return validation;
        if (command.Status is "Completed" or "Skipped")
            return IdentityOperationResult<TaskRecord>.Failure("TaskStateTransitionInvalid", 409, "A new Task must start in NotStarted or InProgress.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<TaskRecord>(connection, transaction, actor,
            "productivity.task.create",
            idempotencyKey,
            CanonicalTask(command),
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        if (!ProjectExists(connection, transaction, actor.OwnerId, command.ProjectId))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("ProjectUnavailable", 422, "Task must belong to an active Project owned by the current user.");
        }
        if (!command.ConfirmProjectTimeBounds && TaskOutsideProjectBounds(connection, transaction, actor.OwnerId, command.ProjectId, command.StartAt!.Value, command.EndAt!.Value))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("ProjectTaskTimeWarning", 409, "The Task falls outside the Project time bounds. Confirm to save without changing the Project.");
        }
        var id = Guid.NewGuid();
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO [productivity].[Task] ([Id], [OwnerId], [ProjectId], [Title], [Description], [Status], [DueAt], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson], [Rank], [ReminderAt])
            VALUES (@Id, @OwnerId, @ProjectId, @Title, @Description, @Status, @DueAt, @StartAt, @EndAt, @Priority, @TagsJson, @AcceptanceCriteriaJson, @Rank, @ReminderAt);
            """;
        Add(insert, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(insert, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(insert, "@ProjectId", SqlDbType.UniqueIdentifier, command.ProjectId);
        Add(insert, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
        Add(insert, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
        Add(insert, "@Status", SqlDbType.VarChar, command.Status);
        Add(insert, "@DueAt", SqlDbType.DateTime2, (object?)command.DueAt?.UtcDateTime ?? DBNull.Value);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(insert, "@Priority", SqlDbType.VarChar, (object?)NormalizePriority(command.Priority) ?? DBNull.Value, 2);
        Add(insert, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(insert, "@AcceptanceCriteriaJson", SqlDbType.NVarChar, NormalizeJson(command.AcceptanceCriteriaJson), -1);
        Add(insert, "@Rank", SqlDbType.Int, command.Rank);
        Add(insert, "@ReminderAt", SqlDbType.DateTime2, (object?)command.ReminderAt?.UtcDateTime ?? DBNull.Value);
        insert.ExecuteNonQuery();
        var result = ReadTask(connection, transaction, actor.OwnerId, id, forUpdate: false);
        if (result is not null)
        {
            result = ReconcileTaskReminder(connection, transaction, result, command);
            InsertTaskHistory(connection, transaction, actor.OwnerId, result, null);
            UpsertTaskCalendarProjection(connection, transaction, actor.OwnerId, result);
        }
        WriteAudit(connection, transaction, actor, id, "productivity.task.create", traceId);
        CompleteReceipt(connection, transaction, receipt, "TaskCreated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<TaskRecord>.Failure("PersistenceFailure", 500, "Task could not be loaded after creation.")
            : IdentityOperationResult<TaskRecord>.Success(result);
    }

    public IdentityOperationResult<TaskRecord> UpdateTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, TaskCommand command, string? idempotencyKey = null, string? traceId = null) =>
        UpdateTaskCore(actor, taskId, ifMatch, command, idempotencyKey, traceId,
            "productivity.task.update", "productivity.task.update", "tasks.task.update", "tasks.update");

    private IdentityOperationResult<TaskRecord> UpdateTaskCore(IdentityPrincipal actor, Guid taskId, string? ifMatch, TaskCommand command, string? idempotencyKey, string? traceId,
        string receiptOperationKey, string auditAction, params string[] actionKeys)
    {
        if (!ModuleAvailable(actor, "FX12", actionKeys)) return ModuleUnavailable<TaskRecord>();
        var validation = ValidateTask(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<TaskRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<TaskRecord>(connection, transaction, actor,
            receiptOperationKey,
            idempotencyKey,
            CanonicalTask(command, taskId, ifMatch),
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("ResourceUnavailable", 404, "Task unavailable.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("RevisionConflict", 412, "Task revision changed.");
        }
        if (current.ProjectId != command.ProjectId)
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("TaskProjectImmutable", 422, "A Task cannot be moved between Projects in the current Release 1 model.");
        }
        if (!ProjectAllowsTask(connection, transaction, actor.OwnerId, current.ProjectId))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("ProjectTerminal", 409, "Tasks in a completed or skipped Project are read-only.");
        }
        if (!command.ConfirmProjectTimeBounds && TaskOutsideProjectBounds(connection, transaction, actor.OwnerId, current.ProjectId, command.StartAt!.Value, command.EndAt!.Value))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("ProjectTaskTimeWarning", 409, "The Task falls outside the Project time bounds. Confirm to save without changing the Project.");
        }
        if (!IsAllowedStatusTransition(current.Status, command.Status, command.TransitionReason))
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("TaskStateTransitionInvalid", 409, "The requested Task status transition is not supported.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [productivity].[Task]
            SET [Title] = @Title, [Description] = @Description, [Status] = @Status,
                [DueAt] = @DueAt, [StartAt] = @StartAt, [EndAt] = @EndAt,
                [Priority] = @Priority, [TagsJson] = @TagsJson,
                [AcceptanceCriteriaJson] = @AcceptanceCriteriaJson, [Rank] = @Rank,
                [ReminderAt] = @ReminderAt, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] <> 'Deleted';
            """;
        Add(update, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
        Add(update, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
        Add(update, "@Status", SqlDbType.VarChar, command.Status);
        Add(update, "@DueAt", SqlDbType.DateTime2, (object?)command.DueAt?.UtcDateTime ?? DBNull.Value);
        Add(update, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(update, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(update, "@Priority", SqlDbType.VarChar, (object?)NormalizePriority(command.Priority) ?? DBNull.Value, 2);
        Add(update, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(update, "@AcceptanceCriteriaJson", SqlDbType.NVarChar, NormalizeJson(command.AcceptanceCriteriaJson), -1);
        Add(update, "@Rank", SqlDbType.Int, command.Rank);
        var effectiveReminderAt = command.ManageReminder ? command.ReminderAt : current.ReminderAt;
        Add(update, "@ReminderAt", SqlDbType.DateTime2, (object?)effectiveReminderAt?.UtcDateTime ?? DBNull.Value);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, taskId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("RevisionConflict", 412, "Task revision changed.");
        }
        var result = ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: false);
        if (result is not null)
        {
            result = ReconcileTaskReminder(connection, transaction, result, command);
            InsertTaskHistory(connection, transaction, actor.OwnerId, result,
                command.TransitionReason ?? (command.ManageReminder ? "ReminderConfiguration" : null));
            UpsertTaskCalendarProjection(connection, transaction, actor.OwnerId, result);
        }
        WriteAudit(connection, transaction, actor, taskId, auditAction, traceId);
        CompleteReceipt(connection, transaction, receipt, "TaskUpdated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<TaskRecord>.Failure("PersistenceFailure", 500, "Task could not be loaded after update.")
            : IdentityOperationResult<TaskRecord>.Success(result);
    }

    public IdentityOperationResult<TaskRecord> TransitionTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12", TaskTransitionAction(status))) return ModuleUnavailable<TaskRecord>();
        if (status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task status is invalid.");
        if (!TryDecodeETag(ifMatch, out _))
            return Precondition<TaskRecord>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        var current = ReadTask(connection, null, actor.OwnerId, taskId, forUpdate: false);
        if (current is null) return IdentityOperationResult<TaskRecord>.Failure("ResourceUnavailable", 404, "Task unavailable.");
        return UpdateTaskCore(actor, taskId, ifMatch,
            new TaskCommand(current.ProjectId, current.Title, current.Description, status, current.DueAt,
                current.StartAt, current.EndAt, current.Priority, current.TagsJson, current.AcceptanceCriteriaJson,
                current.Rank, current.ReminderAt, reason), idempotencyKey, traceId,
            "productivity.task.transition", "productivity.task.transition", TaskTransitionAction(status));
    }

    public IdentityOperationResult<object?> DeleteTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12", "tasks.task.trash", "tasks.delete")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "productivity.task.delete",
            idempotencyKey,
            $"task:{taskId:N}|etag:{ifMatch}",
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("ResourceUnavailable", 404, "Task unavailable.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("RevisionConflict", 412, "Task revision changed.");
        }
        if (!ProjectAllowsTask(connection, transaction, actor.OwnerId, current.ProjectId))
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("ProjectTerminal", 409, "Tasks in a completed or skipped Project are read-only.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE [productivity].[Task] SET [Status] = 'Deleted', [DeletedAt] = SYSUTCDATETIME(), [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;";
        Add(update, "@Id", SqlDbType.UniqueIdentifier, taskId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("RevisionConflict", 412, "Task revision changed.");
        }
        Execute(connection, transaction,
            "INSERT INTO [platform].[TrashItem] ([OwnerId], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus]) VALUES (@OwnerId, 'Task', @ResourceId, @Batch, @PriorStatus);",
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@ResourceId", SqlDbType.UniqueIdentifier, (object)taskId),
            ("@Batch", SqlDbType.UniqueIdentifier, (object)Guid.NewGuid()),
            ("@PriorStatus", SqlDbType.VarChar, (object)current.Status));
        CancelTaskCalendarProjection(connection, transaction, actor.OwnerId, taskId);
        InsertTaskHistory(connection, transaction, actor.OwnerId, current with { Status = "Deleted" }, "Trash");
        WriteAudit(connection, transaction, actor, taskId, "productivity.task.delete", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent", 204, null);
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<EventPage> ListEvents(IdentityPrincipal actor, DateTimeOffset? from = null, DateTimeOffset? to = null, int? limit = null, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX13", "calendar.event.read", "calendar.view")) return ModuleUnavailable<EventPage>();
        var take = Math.Clamp(limit ?? 25, 1, 100);
        var eventScope = $"{from?.UtcDateTime:o}|{to?.UtcDateTime:o}";
        EventListCursor? position = null;
        if (cursor is not null && (!TryDecodeCursor(cursor, out position) || position is null || position.Scope != eventScope))
            return Failure<EventPage>("InvalidCursor", 422, "The Calendar cursor is invalid or does not match this query.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var taskSourceAllowed = _capabilities.IsAllowed(connection, transaction, actor, "FX12", "tasks.task.read", "tasks.view");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT TOP (@Limit) e.[Id], e.[Title], e.[Description], e.[StartAt], e.[EndAt], e.[TimeZoneId], e.[Status], e.[CreatedAt], e.[UpdatedAt], e.[RowVersion], e.[IsAllDay], e.[SourceUid], e.[SourceKind], e.[TaskId]
            FROM [calendar].[Event] e
            LEFT JOIN [productivity].[Task] taskRow
              ON taskRow.[Id] = e.[TaskId] AND taskRow.[OwnerId] = e.[OwnerId]
            WHERE e.[OwnerId] = @OwnerId AND e.[Status] <> 'Deleted'
              AND (e.[TaskId] IS NULL OR (@TaskSourceAllowed = 1 AND taskRow.[Id] IS NOT NULL AND taskRow.[Status] <> 'Deleted'))
              AND (@From IS NULL OR e.[EndAt] > @From)
              AND (@To IS NULL OR e.[StartAt] < @To)
              AND (@HasCursor = 0 OR e.[StartAt] > @CursorStartAt OR (e.[StartAt] = @CursorStartAt AND e.[Id] > @CursorId))
            ORDER BY e.[StartAt], e.[Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take + 1);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@TaskSourceAllowed", SqlDbType.Bit, taskSourceAllowed);
        Add(command, "@From", SqlDbType.DateTime2, (object?)from?.UtcDateTime ?? DBNull.Value);
        Add(command, "@To", SqlDbType.DateTime2, (object?)to?.UtcDateTime ?? DBNull.Value);
        Add(command, "@HasCursor", SqlDbType.Bit, cursor is not null);
        Add(command, "@CursorStartAt", SqlDbType.DateTime2, (object?)position?.StartAt ?? DBNull.Value);
        Add(command, "@CursorId", SqlDbType.UniqueIdentifier, (object?)position?.Id ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var items = new List<EventRecord>();
        while (reader.Read()) items.Add(ReadEvent(reader));
        transaction.Commit();
        var hasMore = items.Count > take;
        if (hasMore) items.RemoveAt(items.Count - 1);
        var nextCursor = hasMore && items.Count > 0
            ? EncodeCursor(new EventListCursor(eventScope, items[^1].StartAt.UtcDateTime, items[^1].Id))
            : null;
        return IdentityOperationResult<EventPage>.Success(new EventPage(items, nextCursor));
    }

    public IdentityOperationResult<EventRecord> GetEvent(IdentityPrincipal actor, Guid eventId)
    {
        if (!ModuleAvailable(actor, "FX13", "calendar.event.read", "calendar.view")) return ModuleUnavailable<EventRecord>();
        if (eventId == Guid.Empty) return Failure<EventRecord>("ResourceUnavailable", 404, "Event unavailable.");
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var current = ReadEvent(connection, transaction, actor.OwnerId, eventId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return Failure<EventRecord>("ResourceUnavailable", 404, "Event unavailable.");
        }
        if (current.TaskId is { } taskId)
        {
            var taskSourceAllowed = _capabilities.IsAllowed(connection, transaction, actor, "FX12", "tasks.task.read", "tasks.view");
            var task = taskSourceAllowed ? ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: true) : null;
            if (!taskSourceAllowed || task is null || task.Status == "Deleted")
            {
                transaction.Rollback();
                return Failure<EventRecord>("ResourceUnavailable", 404, "Event unavailable.");
            }
        }
        transaction.Commit();
        return IdentityOperationResult<EventRecord>.Success(current);
    }

    public IdentityOperationResult<EventRecord> CreateEvent(IdentityPrincipal actor, EventCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13", "calendar.event.create", "calendar.create")) return ModuleUnavailable<EventRecord>();
        var validation = ValidateEvent(command);
        if (validation is not null) return validation;
        var id = Guid.NewGuid();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<EventRecord>(connection, transaction, actor,
            "calendar.event.create",
            idempotencyKey,
            $"title:{command.Title.Trim()}|description:{command.Description?.Trim()}|start:{command.StartAt.UtcDateTime:o}|end:{command.EndAt.UtcDateTime:o}|timezone:{command.TimeZoneId.Trim()}|allDay:{command.IsAllDay}|uid:{command.SourceUid}",
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO [calendar].[Event] ([Id], [OwnerId], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [IsAllDay], [SourceUid], [SourceKind])
            VALUES (@Id, @OwnerId, @Title, @Description, @StartAt, @EndAt, @TimeZoneId, @IsAllDay, @SourceUid, 'Manual');
            """;
        Add(insert, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(insert, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(insert, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
        Add(insert, "@Description", SqlDbType.NVarChar, command.Description!.Trim(), 20000);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt.UtcDateTime);
        Add(insert, "@TimeZoneId", SqlDbType.NVarChar, command.TimeZoneId.Trim(), 128);
        Add(insert, "@IsAllDay", SqlDbType.Bit, command.IsAllDay);
        Add(insert, "@SourceUid", SqlDbType.NVarChar, (object?)TrimOrNull(command.SourceUid, 255) ?? DBNull.Value, 255);
        insert.ExecuteNonQuery();
        WriteAudit(connection, transaction, actor, id, "calendar.event.create", traceId);
        var result = ReadEvent(connection, transaction, actor.OwnerId, id, forUpdate: false);
        CompleteReceipt(connection, transaction, receipt, "EventCreated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<EventRecord>.Failure("PersistenceFailure", 500, "Event could not be loaded after creation.")
            : IdentityOperationResult<EventRecord>.Success(result);
    }

    public IdentityOperationResult<EventRecord> UpdateEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, EventCommand command, string? idempotencyKey = null, string? traceId = null) =>
        UpdateEventCore(actor, eventId, ifMatch, command, idempotencyKey, traceId,
            "calendar.event.update", "calendar.event.update", "calendar.event.update", "calendar.update");

    private IdentityOperationResult<EventRecord> UpdateEventCore(IdentityPrincipal actor, Guid eventId, string? ifMatch, EventCommand command, string? idempotencyKey, string? traceId,
        string receiptOperationKey, string auditAction, params string[] actionKeys)
    {
        if (!ModuleAvailable(actor, "FX13", actionKeys)) return ModuleUnavailable<EventRecord>();
        var validation = ValidateEvent(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<EventRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<EventRecord>(connection, transaction, actor,
            receiptOperationKey,
            idempotencyKey,
            $"event:{eventId:N}|etag:{ifMatch}|title:{command.Title.Trim()}|description:{command.Description?.Trim()}|start:{command.StartAt.UtcDateTime:o}|end:{command.EndAt.UtcDateTime:o}|timezone:{command.TimeZoneId.Trim()}|allDay:{command.IsAllDay}|uid:{command.SourceUid}|status:{command.Status}",
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadEvent(connection, transaction, actor.OwnerId, eventId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<EventRecord>.Failure("ResourceUnavailable", 404, "Event unavailable.");
        }
        if (current.TaskId is not null)
        {
            transaction.Rollback();
            return IdentityOperationResult<EventRecord>.Failure("TaskCalendarProjectionReadOnly", 409, "Task Calendar projections are controlled by the Task and cannot be edited here.");
        }
        if (current.Status is "Completed" or "Canceled")
        {
            transaction.Rollback();
            return IdentityOperationResult<EventRecord>.Failure("EventTerminal", 409, "Completed or canceled events are read-only.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<EventRecord>.Failure("RevisionConflict", 412, "Event revision changed.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [calendar].[Event]
            SET [Title] = @Title, [Description] = @Description, [StartAt] = @StartAt,
                [EndAt] = @EndAt, [TimeZoneId] = @TimeZoneId, [IsAllDay] = @IsAllDay,
                [SourceUid] = @SourceUid, [Status] = COALESCE(@Status, [Status]), [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] <> 'Deleted';
            """;
        Add(update, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 200);
        Add(update, "@Description", SqlDbType.NVarChar, command.Description!.Trim(), 20000);
        Add(update, "@StartAt", SqlDbType.DateTime2, command.StartAt.UtcDateTime);
        Add(update, "@EndAt", SqlDbType.DateTime2, command.EndAt.UtcDateTime);
        Add(update, "@TimeZoneId", SqlDbType.NVarChar, command.TimeZoneId.Trim(), 128);
        Add(update, "@IsAllDay", SqlDbType.Bit, command.IsAllDay);
        Add(update, "@SourceUid", SqlDbType.NVarChar, (object?)TrimOrNull(command.SourceUid, 255) ?? DBNull.Value, 255);
        Add(update, "@Status", SqlDbType.VarChar, (object?)command.Status ?? DBNull.Value, 16);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, eventId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<EventRecord>.Failure("RevisionConflict", 412, "Event revision changed.");
        }
        WriteAudit(connection, transaction, actor, eventId, auditAction, traceId);
        var result = ReadEvent(connection, transaction, actor.OwnerId, eventId, forUpdate: false);
        CompleteReceipt(connection, transaction, receipt, "EventUpdated", 200, result is null ? null : JsonSerializer.Serialize(result));
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<EventRecord>.Failure("PersistenceFailure", 500, "Event could not be loaded after update.")
            : IdentityOperationResult<EventRecord>.Success(result);
    }

    public IdentityOperationResult<EventRecord> TransitionEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string status, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13", EventTransitionAction(status))) return ModuleUnavailable<EventRecord>();
        if (status is not ("Scheduled" or "Completed" or "Canceled"))
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event status is invalid.");
        if (!TryDecodeETag(ifMatch, out _))
            return Precondition<EventRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        var current = ReadEvent(connection, null, actor.OwnerId, eventId, forUpdate: false);
        if (current is null) return IdentityOperationResult<EventRecord>.Failure("ResourceUnavailable", 404, "Event unavailable.");
        if (current.TaskId is not null) return IdentityOperationResult<EventRecord>.Failure("TaskCalendarProjectionReadOnly", 409, "Task Calendar projections are controlled by the Task and cannot be edited here.");
        if (current.Status is "Completed" or "Canceled")
            return IdentityOperationResult<EventRecord>.Failure("EventTerminal", 409, "Completed or canceled events are read-only.");
        return UpdateEventCore(actor, eventId, ifMatch,
            new EventCommand(current.Title, current.Description, current.StartAt, current.EndAt, current.TimeZoneId, current.IsAllDay, current.SourceUid, status),
            idempotencyKey, traceId, "calendar.event.transition", "calendar.event.transition", EventTransitionAction(status));
    }

    public IdentityOperationResult<object?> DeleteEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13", "calendar.event.cancel", "calendar.event.delete")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "calendar.event.delete",
            idempotencyKey,
            $"event:{eventId:N}|etag:{ifMatch}",
            out var receipt);
        if (receiptFailure is not null)
        {
            transaction.Rollback();
            return receiptFailure;
        }
        var current = ReadEvent(connection, transaction, actor.OwnerId, eventId, forUpdate: true);
        if (current is null || current.Status == "Deleted")
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("ResourceUnavailable", 404, "Event unavailable.");
        }
        if (current.TaskId is not null)
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("TaskCalendarProjectionReadOnly", 409, "Task Calendar projections are controlled by the Task and cannot be edited here.");
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("RevisionConflict", 412, "Event revision changed.");
        }
        if (current.Status is "Completed" or "Canceled")
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("EventTerminal", 409, "Completed or canceled events are read-only.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE [calendar].[Event] SET [Status] = 'Canceled', [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] = 'Scheduled';";
        Add(update, "@Id", SqlDbType.UniqueIdentifier, eventId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<object?>.Failure("RevisionConflict", 412, "Event revision changed.");
        }
        WriteAudit(connection, transaction, actor, eventId, "calendar.event.delete", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent", 204, null);
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    private static IdentityOperationResult<ProjectRecord>? ValidateProject(ProjectCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project Title is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(command.Description) || command.Description.Trim().Length > 20000)
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project Description is required and must be at most 20,000 characters.");
        if (command.StartAt is null || command.EndAt is null || command.EndAt <= command.StartAt)
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project start and end are required and end must be after start.");
        if (!IsPriority(command.Priority))
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project priority must be P0, P1, P2 or P3.");
        if (!IsJsonArray(command.TagsJson) || command.Notes is { Length: > 20000 })
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project tags or notes are invalid.");
        return null;
    }

    private static IdentityOperationResult<TaskRecord>? ValidateTask(TaskCommand command)
    {
        if (command.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task project and title are required; title must be at most 200 characters.");
        if (command.Description is { Length: > 4000 })
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task description is too long.");
        if (command.Status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task status must be NotStarted, InProgress, Completed or Skipped.");
        if (command.StartAt is null || command.EndAt is null || command.EndAt <= command.StartAt)
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task start and end are required and end must be after start.");
        if (!IsPriority(command.Priority) || !IsJsonArray(command.TagsJson) || !IsJsonArray(command.AcceptanceCriteriaJson))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task priority, tags or acceptance criteria are invalid.");
        if (command.Rank < 0)
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task rank is invalid.");
        return null;
    }

    private static IdentityOperationResult<EventRecord>? ValidateEvent(EventCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 200)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event title is required and must be at most 200 characters.");
        if (string.IsNullOrWhiteSpace(command.Description) || command.Description.Trim().Length > 20000)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event Description is required and must be at most 20,000 characters.");
        if (command.SourceUid is { Length: > 255 })
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event source UID is too long.");
        if (command.Status is not null && command.Status is not ("Scheduled" or "Completed" or "Canceled"))
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event status is invalid.");
        if (command.EndAt <= command.StartAt)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event end must be after start.");
        var decision = RegistrationPolicy.ValidateTimeZoneId(command.TimeZoneId);
        if (!decision.Allowed)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, decision.Message);
        if (command.IsAllDay && (!TryGetTimeZone(command.TimeZoneId, out var timeZone) ||
            TimeZoneInfo.ConvertTime(command.StartAt, timeZone).TimeOfDay != TimeSpan.Zero ||
            TimeZoneInfo.ConvertTime(command.EndAt, timeZone).TimeOfDay != TimeSpan.Zero ||
            TimeZoneInfo.ConvertTime(command.EndAt, timeZone).Date <= TimeZoneInfo.ConvertTime(command.StartAt, timeZone).Date))
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "All-day events require local midnight boundaries and an exclusive end date after the start date.");
        return null;
    }

    private static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out var windowsId))
            {
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                    return true;
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
        }
        catch (InvalidTimeZoneException) { }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }

    private static bool IsPriority(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim() is "P0" or "P1" or "P2" or "P3";
    private static string? NormalizePriority(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsJsonArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(value);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array;
        }
        catch (System.Text.Json.JsonException) { return false; }
    }

    private static string NormalizeJson(string? value) => string.IsNullOrWhiteSpace(value) ? "[]" : value.Trim();
    private static string CanonicalProject(ProjectCommand command, Guid? id = null, string? etag = null) =>
        $"project:{id?.ToString("N")}|etag:{etag}|name:{command.Name.Trim()}|description:{command.Description?.Trim()}|start:{command.StartAt?.UtcDateTime:o}|end:{command.EndAt?.UtcDateTime:o}|priority:{command.Priority}|tags:{NormalizeJson(command.TagsJson)}|notes:{command.Notes?.Trim()}|confirmTaskBounds:{command.ConfirmTaskBounds}";

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) =>
        _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private static string ProjectTransitionAction(string status) => status switch
    {
        "InProgress" => "projects.project.start",
        "NotStarted" => "projects.project.revert",
        "Completed" => "projects.project.complete",
        "Skipped" => "projects.project.skip",
        _ => "projects.project.update"
    };

    private static string TaskTransitionAction(string status) => status switch
    {
        "InProgress" => "tasks.task.start",
        "NotStarted" => "tasks.task.revert",
        "Completed" => "tasks.task.complete",
        "Skipped" => "tasks.task.skip",
        _ => "tasks.task.update"
    };

    private static string EventTransitionAction(string status) => status switch
    {
        "Completed" => "calendar.event.complete",
        "Canceled" => "calendar.event.cancel",
        _ => "calendar.event.update"
    };

    private static bool ProjectExists(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM [productivity].[Project] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Status] NOT IN ('Completed','Skipped','Deleted');";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, projectId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return command.ExecuteScalar() is not null;
    }

    private static bool HasOpenTasks(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT_BIG(1) FROM [productivity].[Task] WHERE [OwnerId] = @OwnerId AND [ProjectId] = @ProjectId AND [Status] NOT IN ('Completed','Skipped','Deleted');";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        return Convert.ToInt64(command.ExecuteScalar() ?? 0) > 0;
    }

    private static bool HasTasks(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT_BIG(1) FROM [productivity].[Task] WHERE [OwnerId] = @OwnerId AND [ProjectId] = @ProjectId AND [Status] <> 'Deleted';";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        return Convert.ToInt64(command.ExecuteScalar() ?? 0) > 0;
    }

    private static bool HasTasksOutsideProjectBounds(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [productivity].[Task] WHERE [OwnerId] = @OwnerId AND [ProjectId] = @ProjectId AND [Status] <> 'Deleted' AND ([StartAt] < @StartAt OR [EndAt] > @EndAt)) THEN 1 ELSE 0 END;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        Add(command, "@StartAt", SqlDbType.DateTime2, startAt.UtcDateTime);
        Add(command, "@EndAt", SqlDbType.DateTime2, endAt.UtcDateTime);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static bool TaskOutsideProjectBounds(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN [StartAt] > @StartAt OR [EndAt] < @EndAt THEN 1 ELSE 0 END FROM [productivity].[Project] WHERE [Id] = @ProjectId AND [OwnerId] = @OwnerId AND [Status] NOT IN ('Completed','Skipped','Deleted');";
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@StartAt", SqlDbType.DateTime2, startAt.UtcDateTime);
        Add(command, "@EndAt", SqlDbType.DateTime2, endAt.UtcDateTime);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static bool ProjectAllowsTask(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN [Status] NOT IN ('Completed','Skipped','Deleted') THEN 1 ELSE 0 END FROM [productivity].[Project] WHERE [Id] = @ProjectId AND [OwnerId] = @OwnerId;";
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, projectId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static IdentityOperationResult<ProjectRecord> MissingProject() =>
        IdentityOperationResult<ProjectRecord>.Failure("ResourceUnavailable", 404, "Project unavailable.");

    private static IdentityOperationResult<T> Revision<T>(string resource) =>
        IdentityOperationResult<T>.Failure("RevisionConflict", 412, $"{resource} revision changed.");

    private static string CanonicalTask(TaskCommand command, Guid? id = null, string? etag = null) =>
        $"task:{id?.ToString("N")}|etag:{etag}|project:{command.ProjectId:N}|title:{command.Title.Trim()}|description:{command.Description?.Trim()}|status:{command.Status}|due:{command.DueAt?.UtcDateTime:o}|start:{command.StartAt?.UtcDateTime:o}|end:{command.EndAt?.UtcDateTime:o}|priority:{NormalizePriority(command.Priority)}|tags:{NormalizeJson(command.TagsJson)}|acceptance:{NormalizeJson(command.AcceptanceCriteriaJson)}|rank:{command.Rank}|reminder:{command.ReminderAt?.UtcDateTime:o}|manageReminder:{command.ManageReminder}|confirmProjectTimeBounds:{command.ConfirmProjectTimeBounds}";

    private static void InsertProjectHistory(SqlConnection connection, SqlTransaction transaction, Guid ownerId, ProjectRecord project, string? reason)
    {
        Execute(connection, transaction,
            "INSERT INTO [productivity].[ProjectHistory] ([ProjectId], [OwnerId], [Name], [Description], [StartAt], [EndAt], [Priority], [TagsJson], [Notes], [Status], [Reason]) VALUES (@ProjectId, @OwnerId, @Name, @Description, @StartAt, @EndAt, @Priority, @TagsJson, @Notes, @Status, @Reason);",
            ("@ProjectId", SqlDbType.UniqueIdentifier, (object)project.Id),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
            ("@Name", SqlDbType.NVarChar, (object)project.Name),
            ("@Description", SqlDbType.NVarChar, (object?)project.Description ?? DBNull.Value),
            ("@StartAt", SqlDbType.DateTime2, (object)(project.StartAt?.UtcDateTime ?? project.CreatedAt.UtcDateTime)),
            ("@EndAt", SqlDbType.DateTime2, (object)(project.EndAt?.UtcDateTime ?? project.UpdatedAt.UtcDateTime)),
            ("@Priority", SqlDbType.VarChar, (object)project.Priority),
            ("@TagsJson", SqlDbType.NVarChar, (object)NormalizeJson(project.TagsJson)),
            ("@Notes", SqlDbType.NVarChar, (object?)project.Notes ?? DBNull.Value),
            ("@Status", SqlDbType.VarChar, (object)project.Status),
            ("@Reason", SqlDbType.NVarChar, (object?)reason ?? DBNull.Value));
    }

    private static void InsertTaskHistory(SqlConnection connection, SqlTransaction transaction, Guid ownerId, TaskRecord task, string? reason)
    {
        Execute(connection, transaction,
            "INSERT INTO [productivity].[TaskHistory] ([TaskId], [OwnerId], [ProjectId], [Title], [Description], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson], [Rank], [ReminderAt], [Status], [Reason]) VALUES (@TaskId, @OwnerId, @ProjectId, @Title, @Description, @StartAt, @EndAt, @Priority, @TagsJson, @AcceptanceCriteriaJson, @Rank, @ReminderAt, @Status, @Reason);",
            ("@TaskId", SqlDbType.UniqueIdentifier, (object)task.Id),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
            ("@ProjectId", SqlDbType.UniqueIdentifier, (object)task.ProjectId),
            ("@Title", SqlDbType.NVarChar, (object)task.Title),
            ("@Description", SqlDbType.NVarChar, (object?)task.Description ?? DBNull.Value),
            ("@StartAt", SqlDbType.DateTime2, (object)(task.StartAt?.UtcDateTime ?? task.CreatedAt.UtcDateTime)),
            ("@EndAt", SqlDbType.DateTime2, (object)(task.EndAt?.UtcDateTime ?? task.UpdatedAt.UtcDateTime)),
            ("@Priority", SqlDbType.VarChar, (object?)task.Priority ?? DBNull.Value),
            ("@TagsJson", SqlDbType.NVarChar, (object)NormalizeJson(task.TagsJson)),
            ("@AcceptanceCriteriaJson", SqlDbType.NVarChar, (object)NormalizeJson(task.AcceptanceCriteriaJson)),
            ("@Rank", SqlDbType.Int, (object)task.Rank),
            ("@ReminderAt", SqlDbType.DateTime2, (object?)task.ReminderAt?.UtcDateTime ?? DBNull.Value),
            ("@Status", SqlDbType.VarChar, (object)task.Status),
            ("@Reason", SqlDbType.NVarChar, (object?)reason ?? DBNull.Value));
    }

    /// <summary>
    /// Keeps the post-0022 Reminder row in the same SQL transaction as an
    /// embedded Task command. Task.ReminderAt is retained as a compatibility
    /// projection for the existing wire contract; the Reminder row owns the
    /// configuration/state used by dispatch.
    /// </summary>
    private static TaskRecord ReconcileTaskReminder(SqlConnection connection, SqlTransaction transaction,
        TaskRecord task, TaskCommand command)
    {
        var ownerId = GetTaskOwner(connection, transaction, task.Id);
        using var read = connection.CreateCommand();
        read.Transaction = transaction;
        read.CommandText = """
            SELECT [Id], [ConfigType], [ExactAt]
            FROM [calendar].[Reminder] WITH (UPDLOCK, ROWLOCK)
            WHERE [OwnerId] = @OwnerId AND [SourceType] = 'Task' AND [SourceId] = @TaskId;
            """;
        Add(read, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(read, "@TaskId", SqlDbType.UniqueIdentifier, task.Id);
        Guid? reminderId = null;
        string? currentConfig = null;
        DateTimeOffset? currentExactAt = null;
        using (var reader = read.ExecuteReader())
        {
            if (reader.Read())
            {
                reminderId = reader.GetGuid(0);
                currentConfig = reader.GetString(1);
                currentExactAt = reader.IsDBNull(2) ? null : ToOffset(reader.GetDateTime(2));
            }
        }

        // An ordinary Task edit preserves the canonical configuration, but it
        // must still recompute BeforeStart15m, lifecycle state and source
        // revision. A non-null legacy field is accepted on create because the
        // Task form predates the separate reminder configuration screen.
        var commandOwnsConfiguration = command.ManageReminder || reminderId is null && command.ReminderAt is not null;
        if (!commandOwnsConfiguration && reminderId is null)
            return task;

        var configType = commandOwnsConfiguration
            ? command.ReminderAt is null
                ? ReminderConfigurationTypes.None
                : ReminderConfigurationTypes.Exact
            : currentConfig ?? ReminderConfigurationTypes.None;
        var exactAt = configType == ReminderConfigurationTypes.Exact
            ? commandOwnsConfiguration ? command.ReminderAt : currentExactAt
            : null;
        var schedule = ReminderPolicy.Resolve(configType, exactAt, task.StartAt!.Value, DateTimeOffset.UtcNow, allowExpired: true);
        if (!schedule.IsValid)
            throw new InvalidOperationException("The embedded Task reminder configuration could not be resolved.");
        var dueAt = schedule.DueAt;
        var state = configType == ReminderConfigurationTypes.None
            ? ReminderStates.None
            : task.Status is "Completed" or "Skipped" or "Deleted"
                ? ReminderStates.Canceled
                : dueAt <= DateTimeOffset.UtcNow ? ReminderStates.Expired : ReminderStates.Pending;

        // Task.ReminderAt remains a compatibility projection. Keep it aligned
        // with the canonical row, including when a StartAt edit moves a preset.
        if (task.ReminderAt != dueAt)
        {
            Execute(connection, transaction, "UPDATE [productivity].[Task] SET [ReminderAt] = @ReminderAt, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @TaskId AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
                ("@ReminderAt", SqlDbType.DateTime2, (object?)dueAt?.UtcDateTime ?? DBNull.Value),
                ("@TaskId", SqlDbType.UniqueIdentifier, (object)task.Id),
                ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
                ("@RowVersion", SqlDbType.Binary, (object)DecodeETag(task.ETag)));
            task = ReadTask(connection, transaction, ownerId, task.Id, forUpdate: false)
                ?? throw new InvalidOperationException("Task was not visible after reminder reconciliation.");
        }
        var sourceRevision = DecodeETag(task.ETag);

        if (reminderId is null)
        {
            Execute(connection, transaction, """
                INSERT INTO [calendar].[Reminder]
                    ([OwnerId], [SourceType], [SourceId], [ConfigType], [ExactAt], [TimeZoneId], [DueAt], [SourceRevision], [State])
                SELECT @OwnerId, 'Task', @TaskId, @ConfigType, @ExactAt, userRow.[TimeZoneId], @DueAt,
                       CONVERT(bigint, @SourceRevision), @State
                FROM [platform].[PersonalSpace] spaceRow
                INNER JOIN [identity].[User] userRow ON userRow.[Id] = spaceRow.[UserId]
                WHERE spaceRow.[Id] = @OwnerId;
                """,
                ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
                ("@TaskId", SqlDbType.UniqueIdentifier, (object)task.Id),
                ("@ConfigType", SqlDbType.VarChar, (object)configType),
                ("@ExactAt", SqlDbType.DateTime2, (object?)exactAt?.UtcDateTime ?? DBNull.Value),
                ("@DueAt", SqlDbType.DateTime2, (object?)dueAt?.UtcDateTime ?? DBNull.Value),
                ("@SourceRevision", SqlDbType.Binary, (object)sourceRevision),
                ("@State", SqlDbType.VarChar, (object)state));
        }
        else
        {
            Execute(connection, transaction, """
                UPDATE [calendar].[Reminder]
                SET [ConfigType] = @ConfigType, [ExactAt] = @ExactAt, [DueAt] = @DueAt,
                    [TimeZoneId] = @TimeZoneId, [SourceRevision] = CONVERT(bigint, @SourceRevision), [State] = @State,
                    [LastNotificationId] = CASE WHEN @State = 'Pending' THEN NULL ELSE [LastNotificationId] END,
                    [UpdatedAt] = SYSUTCDATETIME()
                WHERE [Id] = @ReminderId AND [OwnerId] = @OwnerId AND [SourceType] = 'Task' AND [SourceId] = @TaskId;
                """,
                ("@ReminderId", SqlDbType.UniqueIdentifier, (object)reminderId.Value),
                ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
                ("@TaskId", SqlDbType.UniqueIdentifier, (object)task.Id),
                ("@ConfigType", SqlDbType.VarChar, (object)configType),
                ("@ExactAt", SqlDbType.DateTime2, (object?)exactAt?.UtcDateTime ?? DBNull.Value),
                ("@TimeZoneId", SqlDbType.NVarChar, (object)ReadOwnerTimeZone(connection, transaction, ownerId)),
                ("@DueAt", SqlDbType.DateTime2, (object?)dueAt?.UtcDateTime ?? DBNull.Value),
                ("@SourceRevision", SqlDbType.Binary, (object)sourceRevision),
                ("@State", SqlDbType.VarChar, (object)state));
        }

        static Guid GetTaskOwner(SqlConnection connection, SqlTransaction transaction, Guid taskId)
        {
            using var owner = connection.CreateCommand();
            owner.Transaction = transaction;
            owner.CommandText = "SELECT [OwnerId] FROM [productivity].[Task] WHERE [Id] = @TaskId;";
            Add(owner, "@TaskId", SqlDbType.UniqueIdentifier, taskId);
            return (Guid)(owner.ExecuteScalar() ?? throw new InvalidOperationException("Task owner is unavailable while reconciling its reminder."));
        }

        return task;
    }

    private static void UpsertTaskCalendarProjection(SqlConnection connection, SqlTransaction transaction, Guid ownerId, TaskRecord task)
    {
        var projectedStatus = task.Status switch
        {
            "Completed" => "Completed",
            "Skipped" or "Deleted" => "Canceled",
            _ => "Scheduled"
        };
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE [calendar].[Event]
            SET [Title] = @Title, [Description] = @Description, [StartAt] = @StartAt,
                [EndAt] = @EndAt, [TimeZoneId] = @TimeZoneId, [Status] = @Status,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [OwnerId] = @OwnerId AND [TaskId] = @TaskId;
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [calendar].[Event]
                    ([OwnerId], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [Status], [SourceUid], [SourceKind], [TaskId])
                VALUES
                    (@OwnerId, @Title, @Description, @StartAt, @EndAt, @TimeZoneId, @Status, NULL, 'Task', @TaskId);
            END;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@TaskId", SqlDbType.UniqueIdentifier, task.Id);
        Add(command, "@Title", SqlDbType.NVarChar, task.Title, 200);
        Add(command, "@Description", SqlDbType.NVarChar, (object?)task.Description ?? DBNull.Value, 20000);
        Add(command, "@StartAt", SqlDbType.DateTime2, task.StartAt!.Value.UtcDateTime);
        Add(command, "@EndAt", SqlDbType.DateTime2, task.EndAt!.Value.UtcDateTime);
        Add(command, "@TimeZoneId", SqlDbType.NVarChar, ReadOwnerTimeZone(connection, transaction, ownerId), 128);
        Add(command, "@Status", SqlDbType.VarChar, projectedStatus, 16);
        command.ExecuteNonQuery();
    }

    private static void CancelTaskCalendarProjection(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid taskId)
    {
        Execute(connection, transaction,
            "UPDATE [calendar].[Event] SET [Status] = 'Canceled', [UpdatedAt] = SYSUTCDATETIME() WHERE [OwnerId] = @OwnerId AND [TaskId] = @TaskId AND [Status] <> 'Canceled';",
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId),
            ("@TaskId", SqlDbType.UniqueIdentifier, (object)taskId));
    }

    private static string ReadOwnerTimeZone(SqlConnection connection, SqlTransaction transaction, Guid ownerId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT u.[TimeZoneId] FROM [platform].[PersonalSpace] ps INNER JOIN [identity].[User] u ON u.[Id] = ps.[UserId] WHERE ps.[Id] = @OwnerId;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        return Convert.ToString(command.ExecuteScalar()) ?? "UTC";
    }

    private static bool IsAllowedStatusTransition(string current, string next, string? reason = null) => current == next || current switch
    {
        "NotStarted" => next is "InProgress" or "Completed" or "Skipped",
        "InProgress" => next is "Completed" or "Skipped" || (next == "NotStarted" && !string.IsNullOrWhiteSpace(reason)),
        "Completed" or "Skipped" => (next is "InProgress" or "NotStarted") && !string.IsNullOrWhiteSpace(reason),
        _ => false
    };

    private static ProjectRecord? ReadProject(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid id, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [Name], [Description], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [Notes] FROM [productivity].[Project] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadProject(reader) : null;
    }

    private static TaskRecord? ReadTask(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid id, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [ProjectId], [Title], [Description], [Status], [DueAt], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson], [Rank], [ReminderAt] FROM [productivity].[Task] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadTask(reader) : null;
    }

    private static EventRecord? ReadEvent(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid id, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [IsAllDay], [SourceUid], [SourceKind], [TaskId] FROM [calendar].[Event] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadEvent(reader) : null;
    }

    private static ProjectRecord ReadProject(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3), ToOffset(reader.GetDateTime(4)), ToOffset(reader.GetDateTime(5)), EncodeETag((byte[])reader[6]), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), reader.GetString(9), reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11));

    private static TaskRecord ReadTask(SqlDataReader reader)
    {
        var status = reader.GetString(4);
        var endAt = ToOffset(reader.GetDateTime(10));
        return new TaskRecord(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), status, reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), ToOffset(reader.GetDateTime(6)), ToOffset(reader.GetDateTime(7)), EncodeETag((byte[])reader[8]), ToOffset(reader.GetDateTime(9)), endAt, reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetString(12), reader.GetString(13), reader.GetInt32(14), reader.IsDBNull(15) ? null : ToOffset(reader.GetDateTime(15)), endAt < DateTimeOffset.UtcNow && status is not ("Completed" or "Skipped" or "Deleted"));
    }

    private static EventRecord ReadEvent(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), ToOffset(reader.GetDateTime(3)), ToOffset(reader.GetDateTime(4)), reader.GetString(5), reader.GetString(6), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag((byte[])reader[9]), reader.GetBoolean(10), reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetString(12), reader.IsDBNull(13) ? null : reader.GetGuid(13));

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@ActorUserId, @OwnerUserId, @Action, N'PersonalResource', @TargetId, 'Succeeded', @TraceId);";
        Add(command, "@ActorUserId", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(command, "@OwnerUserId", SqlDbType.UniqueIdentifier, actor.UserId);
        Add(command, "@Action", SqlDbType.NVarChar, action, 160);
        Add(command, "@TargetId", SqlDbType.UniqueIdentifier, targetId);
        Add(command, "@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 128);
        command.ExecuteNonQuery();
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => IdentityOperationResult<T>.Failure("ModuleUnavailable", 409, "The requested module is disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Precondition<T>(string? ifMatch) => string.IsNullOrWhiteSpace(ifMatch)
        ? IdentityOperationResult<T>.Failure("PreconditionRequired", 428, "If-Match is required.")
        : IdentityOperationResult<T>.Failure("RevisionConflict", 412, "If-Match is invalid.");

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
    {
        claim = default;
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return null;
        claim = _receipts.TryClaim(connection, transaction, actor.UserId, operationKey, idempotencyKey!, canonicalRequest, DateTime.UtcNow);
        if (claim.IsClaimed) return null;
        if (claim.IsInvalid)
            return IdentityOperationResult<T>.Failure("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
        if (claim.IsReplay)
        {
            var replay = ReplayReceipt<T>(connection, transaction, actor, operationKey, canonicalRequest, claim);
            if (replay is not null) return replay;
        }

        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        var message = claim.IsConflict
            ? "The same Idempotency-Key was already used with a different request."
            : "The request was already completed or is in progress.";
        return IdentityOperationResult<T>.Failure(code, 409, message);
    }

    private IdentityOperationResult<T>? ReplayReceipt<T>(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string canonicalRequest, ReceiptClaim claim)
    {
        var operationStatus = EvaluateReplayOperation(connection, transaction, actor, operationKey, canonicalRequest);
        if (operationStatus == SqlCapabilityStatus.PermissionDenied)
            return IdentityOperationResult<T>.Failure("PermissionDenied", 403, "The idempotent operation is no longer allowed.");
        if (operationStatus != SqlCapabilityStatus.Allowed)
            return ModuleUnavailable<T>();
        if (claim.ResultStatusCode == 204)
            return IdentityOperationResult<T>.NoContent(claim.ResultCode ?? "NoContent");
        if (string.IsNullOrWhiteSpace(claim.ResultJson)) return null;

        try
        {
            var value = JsonSerializer.Deserialize<T>(claim.ResultJson);
            if (value is null) return null;

            if (value is ProjectRecord project)
            {
                var current = ReadProject(connection, transaction, actor.OwnerId, project.Id, forUpdate: true);
                return current is null || current.Status == "Deleted"
                    ? Failure<T>("ResourceUnavailable", 404, "Project unavailable.")
                    : IdentityOperationResult<T>.Success((T)(object)current, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
            }

            if (value is TaskRecord task)
            {
                var current = ReadTask(connection, transaction, actor.OwnerId, task.Id, forUpdate: true);
                return current is null || current.Status == "Deleted"
                    ? Failure<T>("ResourceUnavailable", 404, "Task unavailable.")
                    : IdentityOperationResult<T>.Success((T)(object)current, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
            }

            if (value is EventRecord calendarEvent)
            {
                var current = ReadEvent(connection, transaction, actor.OwnerId, calendarEvent.Id, forUpdate: true);
                if (current is null || current.Status == "Deleted")
                    return Failure<T>("ResourceUnavailable", 404, "Event unavailable.");
                if (current.TaskId is { } taskId)
                {
                    if (!_capabilities.IsAllowed(connection, transaction, actor, "FX12", "tasks.task.read", "tasks.view"))
                        return Failure<T>("ResourceUnavailable", 404, "Event unavailable.");
                    var source = ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: true);
                    if (source is null || source.Status == "Deleted")
                        return Failure<T>("ResourceUnavailable", 404, "Event unavailable.");
                }

                return IdentityOperationResult<T>.Success((T)(object)current, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
            }

            return IdentityOperationResult<T>.Success(value, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private SqlCapabilityStatus EvaluateReplayOperation(SqlConnection connection, SqlTransaction transaction,
        IdentityPrincipal actor, string operationKey, string canonicalRequest) => operationKey switch
        {
            "productivity.project.create" => _capabilities.Evaluate(connection, transaction, actor, "FX11", "projects.project.create", "projects.create"),
            "productivity.project.update" => _capabilities.Evaluate(connection, transaction, actor, "FX11", "projects.project.update", "projects.update"),
            "productivity.project.transition" => _capabilities.Evaluate(connection, transaction, actor, "FX11", ProjectTransitionAction(CanonicalValue(canonicalRequest, "status") ?? string.Empty)),
            "productivity.project.delete" => _capabilities.Evaluate(connection, transaction, actor, "FX11", "projects.project.trash", "projects.delete"),
            "productivity.task.create" => _capabilities.Evaluate(connection, transaction, actor, "FX12", "tasks.task.create", "tasks.create"),
            "productivity.task.update" => _capabilities.Evaluate(connection, transaction, actor, "FX12", "tasks.task.update", "tasks.update"),
            "productivity.task.transition" => _capabilities.Evaluate(connection, transaction, actor, "FX12", TaskTransitionAction(CanonicalValue(canonicalRequest, "status") ?? string.Empty)),
            "productivity.task.delete" => _capabilities.Evaluate(connection, transaction, actor, "FX12", "tasks.task.trash", "tasks.delete"),
            "calendar.event.create" => _capabilities.Evaluate(connection, transaction, actor, "FX13", "calendar.event.create", "calendar.create"),
            "calendar.event.update" => _capabilities.Evaluate(connection, transaction, actor, "FX13", "calendar.event.update", "calendar.update"),
            "calendar.event.transition" => _capabilities.Evaluate(connection, transaction, actor, "FX13", EventTransitionAction(CanonicalValue(canonicalRequest, "status") ?? string.Empty)),
            "calendar.event.delete" => _capabilities.Evaluate(connection, transaction, actor, "FX13", "calendar.event.cancel", "calendar.event.delete"),
            _ => SqlCapabilityStatus.ModuleUnavailable
        };

    private static string? CanonicalValue(string canonicalRequest, string key)
    {
        var prefix = key + ":";
        return canonicalRequest.Split('|')
            .FirstOrDefault(part => part.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];
    }

    private static string EncodeCursor<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeCursor<T>(string value, out T? result)
    {
        result = default;
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
            result = JsonSerializer.Deserialize<T>(Convert.FromBase64String(padded));
            return result is not null;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record ProjectListCursor(string Scope, string Name, Guid Id);
    private sealed record TaskListCursor(string Scope, bool HasDueAt, DateTime? DueAt, DateTime UpdatedAt, Guid Id);
    private sealed record EventListCursor(string Scope, DateTime StartAt, Guid Id);

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim,
        string resultCode, int resultStatusCode, string? resultJson) =>
        _receipts.Complete(connection, transaction, claim, resultCode, resultStatusCode, resultJson);

    private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql,
        params (string Name, SqlDbType Type, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Type, parameter.Value);
        }

        command.ExecuteNonQuery();
    }

    private static void Add(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = command.Parameters.Add(name, type, size);
        parameter.Value = value;
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static string? TrimOrNull(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
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
}

