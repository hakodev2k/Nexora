using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;
using Nexora.Domain.Identity;
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

    public SqlProductivityService(SqlConnectionFactory connections, string? idempotencySecret = null)
    {
        _connections = connections;
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<ProjectPage> ListProjects(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX11")) return ModuleUnavailable<ProjectPage>();
        var take = Math.Clamp(limit ?? 50, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Name], [Description], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [Notes]
            FROM [productivity].[Project]
            WHERE [OwnerId] = @OwnerId AND [Status] <> 'Deleted'
            ORDER BY [UpdatedAt] DESC, [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        var items = new List<ProjectRecord>();
        while (reader.Read()) items.Add(ReadProject(reader));
        return IdentityOperationResult<ProjectPage>.Success(new ProjectPage(items, null));
    }

    public IdentityOperationResult<ProjectRecord> CreateProject(IdentityPrincipal actor, ProjectCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11")) return ModuleUnavailable<ProjectRecord>();
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
        Add(insert, "@Name", SqlDbType.NVarChar, command.Name.Trim(), 160);
        Add(insert, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 2000) ?? DBNull.Value, 2000);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(insert, "@Priority", SqlDbType.VarChar, command.Priority.Trim(), 2);
        Add(insert, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(insert, "@Notes", SqlDbType.NVarChar, (object?)TrimOrNull(command.Notes, 20000) ?? DBNull.Value, -1);
        insert.ExecuteNonQuery();
        var result = ReadProject(connection, transaction, actor.OwnerId, id, forUpdate: false);
        if (result is not null) InsertProjectHistory(connection, transaction, actor.OwnerId, result, null);
        WriteAudit(connection, transaction, actor, id, "productivity.project.create", traceId);
        CompleteReceipt(connection, transaction, receipt, "ProjectCreated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<ProjectRecord>.Failure("PersistenceFailure", 500, "Project could not be loaded after creation.")
            : IdentityOperationResult<ProjectRecord>.Success(result);
    }

    public IdentityOperationResult<ProjectRecord> UpdateProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, ProjectCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11")) return ModuleUnavailable<ProjectRecord>();
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
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag)))
        {
            transaction.Rollback();
            return IdentityOperationResult<ProjectRecord>.Failure("RevisionConflict", 412, "Project revision changed.");
        }
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [productivity].[Project]
            SET [Name] = @Name, [Description] = @Description, [StartAt] = @StartAt, [EndAt] = @EndAt,
                [Priority] = @Priority, [TagsJson] = @TagsJson, [Notes] = @Notes, [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion AND [Status] <> 'Deleted';
            """;
        Add(update, "@Name", SqlDbType.NVarChar, command.Name.Trim(), 160);
        Add(update, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 2000) ?? DBNull.Value, 2000);
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
        CompleteReceipt(connection, transaction, receipt, "ProjectUpdated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<ProjectRecord>.Failure("PersistenceFailure", 500, "Project could not be loaded after update.")
            : IdentityOperationResult<ProjectRecord>.Success(result);
    }

    public IdentityOperationResult<ProjectRecord> TransitionProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11")) return ModuleUnavailable<ProjectRecord>();
        if (status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project status is invalid.");
        if (!TryDecodeETag(ifMatch, out var expectedVersion))
            return Precondition<ProjectRecord>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<ProjectRecord>(connection, transaction, actor, "productivity.project.transition",
            idempotencyKey, $"project:{projectId:N}|etag:{ifMatch}|status:{status}|reason:{reason?.Trim()}", out var receipt);
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

        if (status is "Completed" or "Skipped" && string.IsNullOrWhiteSpace(reason) && HasOpenTasks(connection, transaction, actor.OwnerId, projectId))
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
        CompleteReceipt(connection, transaction, receipt, "ProjectTransitioned");
        transaction.Commit();
        return updated is null
            ? Failure<ProjectRecord>("PersistenceFailure", 500, "Project could not be loaded after transition.")
            : IdentityOperationResult<ProjectRecord>.Success(updated);
    }

    public IdentityOperationResult<object?> DeleteProject(IdentityPrincipal actor, Guid projectId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX11")) return ModuleUnavailable<object?>();
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
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<TaskPage> ListTasks(IdentityPrincipal actor, Guid? projectId = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX12")) return ModuleUnavailable<TaskPage>();
        var take = Math.Clamp(limit ?? 100, 1, 200);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [ProjectId], [Title], [Description], [Status], [DueAt], [CreatedAt], [UpdatedAt], [RowVersion], [StartAt], [EndAt], [Priority], [TagsJson], [AcceptanceCriteriaJson], [Rank], [ReminderAt]
            FROM [productivity].[Task]
            WHERE [OwnerId] = @OwnerId AND [Status] <> 'Deleted'
              AND (@ProjectId IS NULL OR [ProjectId] = @ProjectId)
            ORDER BY CASE WHEN [DueAt] IS NULL THEN 1 ELSE 0 END, [DueAt], [UpdatedAt] DESC, [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@ProjectId", SqlDbType.UniqueIdentifier, (object?)projectId ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var items = new List<TaskRecord>();
        while (reader.Read()) items.Add(ReadTask(reader));
        return IdentityOperationResult<TaskPage>.Success(new TaskPage(items, null));
    }

    public IdentityOperationResult<TaskRecord> CreateTask(IdentityPrincipal actor, TaskCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12")) return ModuleUnavailable<TaskRecord>();
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
        Add(insert, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 240);
        Add(insert, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
        Add(insert, "@Status", SqlDbType.VarChar, command.Status);
        Add(insert, "@DueAt", SqlDbType.DateTime2, (object?)command.DueAt?.UtcDateTime ?? DBNull.Value);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(insert, "@Priority", SqlDbType.VarChar, command.Priority.Trim(), 2);
        Add(insert, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(insert, "@AcceptanceCriteriaJson", SqlDbType.NVarChar, NormalizeJson(command.AcceptanceCriteriaJson), -1);
        Add(insert, "@Rank", SqlDbType.Int, command.Rank);
        Add(insert, "@ReminderAt", SqlDbType.DateTime2, (object?)command.ReminderAt?.UtcDateTime ?? DBNull.Value);
        insert.ExecuteNonQuery();
        var result = ReadTask(connection, transaction, actor.OwnerId, id, forUpdate: false);
        if (result is not null) InsertTaskHistory(connection, transaction, actor.OwnerId, result, null);
        WriteAudit(connection, transaction, actor, id, "productivity.task.create", traceId);
        CompleteReceipt(connection, transaction, receipt, "TaskCreated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<TaskRecord>.Failure("PersistenceFailure", 500, "Task could not be loaded after creation.")
            : IdentityOperationResult<TaskRecord>.Success(result);
    }

    public IdentityOperationResult<TaskRecord> UpdateTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, TaskCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12")) return ModuleUnavailable<TaskRecord>();
        var validation = ValidateTask(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<TaskRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<TaskRecord>(connection, transaction, actor,
            "productivity.task.update",
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
        Add(update, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 240);
        Add(update, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
        Add(update, "@Status", SqlDbType.VarChar, command.Status);
        Add(update, "@DueAt", SqlDbType.DateTime2, (object?)command.DueAt?.UtcDateTime ?? DBNull.Value);
        Add(update, "@StartAt", SqlDbType.DateTime2, command.StartAt!.Value.UtcDateTime);
        Add(update, "@EndAt", SqlDbType.DateTime2, command.EndAt!.Value.UtcDateTime);
        Add(update, "@Priority", SqlDbType.VarChar, command.Priority.Trim(), 2);
        Add(update, "@TagsJson", SqlDbType.NVarChar, NormalizeJson(command.TagsJson), -1);
        Add(update, "@AcceptanceCriteriaJson", SqlDbType.NVarChar, NormalizeJson(command.AcceptanceCriteriaJson), -1);
        Add(update, "@Rank", SqlDbType.Int, command.Rank);
        Add(update, "@ReminderAt", SqlDbType.DateTime2, (object?)command.ReminderAt?.UtcDateTime ?? DBNull.Value);
        Add(update, "@Id", SqlDbType.UniqueIdentifier, taskId);
        Add(update, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(update, "@RowVersion", SqlDbType.Binary, expectedVersion, 8);
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<TaskRecord>.Failure("RevisionConflict", 412, "Task revision changed.");
        }
        var result = ReadTask(connection, transaction, actor.OwnerId, taskId, forUpdate: false);
        if (result is not null) InsertTaskHistory(connection, transaction, actor.OwnerId, result, command.TransitionReason);
        WriteAudit(connection, transaction, actor, taskId, "productivity.task.update", traceId);
        CompleteReceipt(connection, transaction, receipt, "TaskUpdated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<TaskRecord>.Failure("PersistenceFailure", 500, "Task could not be loaded after update.")
            : IdentityOperationResult<TaskRecord>.Success(result);
    }

    public IdentityOperationResult<TaskRecord> TransitionTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string status, string? reason, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12")) return ModuleUnavailable<TaskRecord>();
        if (status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task status is invalid.");
        if (!TryDecodeETag(ifMatch, out _))
            return Precondition<TaskRecord>(ifMatch);

        using var connection = _connections.Create();
        connection.Open();
        var current = ReadTask(connection, null, actor.OwnerId, taskId, forUpdate: false);
        if (current is null) return IdentityOperationResult<TaskRecord>.Failure("ResourceUnavailable", 404, "Task unavailable.");
        return UpdateTask(actor, taskId, ifMatch,
            new TaskCommand(current.ProjectId, current.Title, current.Description, status, current.DueAt,
                current.StartAt, current.EndAt, current.Priority, current.TagsJson, current.AcceptanceCriteriaJson,
                current.Rank, current.ReminderAt, reason), idempotencyKey, traceId);
    }

    public IdentityOperationResult<object?> DeleteTask(IdentityPrincipal actor, Guid taskId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX12")) return ModuleUnavailable<object?>();
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
        InsertTaskHistory(connection, transaction, actor.OwnerId, current with { Status = "Deleted" }, "Trash");
        WriteAudit(connection, transaction, actor, taskId, "productivity.task.delete", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<EventPage> ListEvents(IdentityPrincipal actor, DateTimeOffset? from = null, DateTimeOffset? to = null, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX13")) return ModuleUnavailable<EventPage>();
        var take = Math.Clamp(limit ?? 100, 1, 200);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [IsAllDay], [SourceUid]
            FROM [calendar].[Event]
            WHERE [OwnerId] = @OwnerId AND [Status] <> 'Deleted'
              AND (@From IS NULL OR [EndAt] > @From)
              AND (@To IS NULL OR [StartAt] < @To)
            ORDER BY [StartAt], [Id];
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@From", SqlDbType.DateTime2, (object?)from?.UtcDateTime ?? DBNull.Value);
        Add(command, "@To", SqlDbType.DateTime2, (object?)to?.UtcDateTime ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var items = new List<EventRecord>();
        while (reader.Read()) items.Add(ReadEvent(reader));
        return IdentityOperationResult<EventPage>.Success(new EventPage(items, null));
    }

    public IdentityOperationResult<EventRecord> CreateEvent(IdentityPrincipal actor, EventCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13")) return ModuleUnavailable<EventRecord>();
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
        Add(insert, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 240);
        Add(insert, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
        Add(insert, "@StartAt", SqlDbType.DateTime2, command.StartAt.UtcDateTime);
        Add(insert, "@EndAt", SqlDbType.DateTime2, command.EndAt.UtcDateTime);
        Add(insert, "@TimeZoneId", SqlDbType.NVarChar, command.TimeZoneId.Trim(), 128);
        Add(insert, "@IsAllDay", SqlDbType.Bit, command.IsAllDay);
        Add(insert, "@SourceUid", SqlDbType.NVarChar, (object?)TrimOrNull(command.SourceUid, 255) ?? DBNull.Value, 255);
        insert.ExecuteNonQuery();
        WriteAudit(connection, transaction, actor, id, "calendar.event.create", traceId);
        var result = ReadEvent(connection, transaction, actor.OwnerId, id, forUpdate: false);
        CompleteReceipt(connection, transaction, receipt, "EventCreated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<EventRecord>.Failure("PersistenceFailure", 500, "Event could not be loaded after creation.")
            : IdentityOperationResult<EventRecord>.Success(result);
    }

    public IdentityOperationResult<EventRecord> UpdateEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, EventCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13")) return ModuleUnavailable<EventRecord>();
        var validation = ValidateEvent(command);
        if (validation is not null) return validation;
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<EventRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var receiptFailure = CheckReceipt<EventRecord>(connection, transaction, actor,
            "calendar.event.update",
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
        Add(update, "@Title", SqlDbType.NVarChar, command.Title.Trim(), 240);
        Add(update, "@Description", SqlDbType.NVarChar, (object?)TrimOrNull(command.Description, 4000) ?? DBNull.Value, 4000);
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
        WriteAudit(connection, transaction, actor, eventId, "calendar.event.update", traceId);
        var result = ReadEvent(connection, transaction, actor.OwnerId, eventId, forUpdate: false);
        CompleteReceipt(connection, transaction, receipt, "EventUpdated");
        transaction.Commit();
        return result is null
            ? IdentityOperationResult<EventRecord>.Failure("PersistenceFailure", 500, "Event could not be loaded after update.")
            : IdentityOperationResult<EventRecord>.Success(result);
    }

    public IdentityOperationResult<EventRecord> TransitionEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string status, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13")) return ModuleUnavailable<EventRecord>();
        if (status is not ("Scheduled" or "Completed" or "Canceled"))
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event status is invalid.");
        if (!TryDecodeETag(ifMatch, out _))
            return Precondition<EventRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        var current = ReadEvent(connection, null, actor.OwnerId, eventId, forUpdate: false);
        if (current is null) return IdentityOperationResult<EventRecord>.Failure("ResourceUnavailable", 404, "Event unavailable.");
        if (current.Status is "Completed" or "Canceled")
            return IdentityOperationResult<EventRecord>.Failure("EventTerminal", 409, "Completed or canceled events are read-only.");
        return UpdateEvent(actor, eventId, ifMatch,
            new EventCommand(current.Title, current.Description, current.StartAt, current.EndAt, current.TimeZoneId, current.IsAllDay, current.SourceUid, status),
            idempotencyKey, traceId);
    }

    public IdentityOperationResult<object?> DeleteEvent(IdentityPrincipal actor, Guid eventId, string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX13")) return ModuleUnavailable<object?>();
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
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    private static IdentityOperationResult<ProjectRecord>? ValidateProject(ProjectCommand command)
    {
        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project name is required and must be at most 160 characters.");
        if (command.Description is { Length: > 2000 })
            return IdentityOperationResult<ProjectRecord>.Failure("ValidationFailed", 422, "Project description is too long.");
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
        if (command.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 240)
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task project and title are required; title must be at most 240 characters.");
        if (command.Description is { Length: > 4000 })
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task description is too long.");
        if (command.Status is not ("NotStarted" or "InProgress" or "Completed" or "Skipped"))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task status must be NotStarted, InProgress, Completed or Skipped.");
        if (command.StartAt is null || command.EndAt is null || command.EndAt <= command.StartAt)
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task start and end are required and end must be after start.");
        if (!IsPriority(command.Priority) || !IsJsonArray(command.TagsJson) || !IsJsonArray(command.AcceptanceCriteriaJson))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task priority, tags or acceptance criteria are invalid.");
        if (command.Rank < 0 || command.ReminderAt is not null && (command.ReminderAt < command.StartAt || command.ReminderAt > command.EndAt))
            return IdentityOperationResult<TaskRecord>.Failure("ValidationFailed", 422, "Task rank or reminder is invalid.");
        return null;
    }

    private static IdentityOperationResult<EventRecord>? ValidateEvent(EventCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title) || command.Title.Trim().Length > 240)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event title is required and must be at most 240 characters.");
        if (command.Description is { Length: > 4000 })
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event description is too long.");
        if (command.SourceUid is { Length: > 255 })
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event source UID is too long.");
        if (command.Status is not null && command.Status is not ("Scheduled" or "Completed" or "Canceled"))
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event status is invalid.");
        if (command.EndAt <= command.StartAt)
            return IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, "Event end must be after start.");
        var decision = RegistrationPolicy.ValidateTimeZoneId(command.TimeZoneId);
        return decision.Allowed ? null : IdentityOperationResult<EventRecord>.Failure("ValidationFailed", 422, decision.Message);
    }

    private static bool IsPriority(string value) => value is "P0" or "P1" or "P2" or "P3";
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
        $"project:{id?.ToString("N")}|etag:{etag}|name:{command.Name.Trim()}|description:{command.Description?.Trim()}|start:{command.StartAt?.UtcDateTime:o}|end:{command.EndAt?.UtcDateTime:o}|priority:{command.Priority}|tags:{NormalizeJson(command.TagsJson)}|notes:{command.Notes?.Trim()}";

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode)
    {
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN m.[State] = 'Ready' AND m.[SystemEnabled] = 1
                              AND COALESCE(g.[Enabled], 0) = 1 THEN 1 ELSE 0 END
            FROM [platform].[Module] m
            LEFT JOIN [platform].[UserModuleGrant] g ON g.[ModuleId] = m.[Id] AND g.[UserId] =
                (SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId)
            WHERE m.[Code] = @Code;
            """;
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Code", SqlDbType.VarChar, moduleCode);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

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
        $"task:{id?.ToString("N")}|etag:{etag}|project:{command.ProjectId:N}|title:{command.Title.Trim()}|description:{command.Description?.Trim()}|status:{command.Status}|due:{command.DueAt?.UtcDateTime:o}|start:{command.StartAt?.UtcDateTime:o}|end:{command.EndAt?.UtcDateTime:o}|priority:{command.Priority}|tags:{NormalizeJson(command.TagsJson)}|acceptance:{NormalizeJson(command.AcceptanceCriteriaJson)}|rank:{command.Rank}|reminder:{command.ReminderAt?.UtcDateTime:o}";

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
            ("@Priority", SqlDbType.VarChar, (object)task.Priority),
            ("@TagsJson", SqlDbType.NVarChar, (object)NormalizeJson(task.TagsJson)),
            ("@AcceptanceCriteriaJson", SqlDbType.NVarChar, (object)NormalizeJson(task.AcceptanceCriteriaJson)),
            ("@Rank", SqlDbType.Int, (object)task.Rank),
            ("@ReminderAt", SqlDbType.DateTime2, (object?)task.ReminderAt?.UtcDateTime ?? DBNull.Value),
            ("@Status", SqlDbType.VarChar, (object)task.Status),
            ("@Reason", SqlDbType.NVarChar, (object?)reason ?? DBNull.Value));
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
        command.CommandText = $"SELECT [Id], [Title], [Description], [StartAt], [EndAt], [TimeZoneId], [Status], [CreatedAt], [UpdatedAt], [RowVersion], [IsAllDay], [SourceUid] FROM [calendar].[Event] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")}) WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadEvent(reader) : null;
    }

    private static ProjectRecord ReadProject(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3), ToOffset(reader.GetDateTime(4)), ToOffset(reader.GetDateTime(5)), EncodeETag((byte[])reader[6]), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), reader.GetString(9), reader.GetString(10), reader.IsDBNull(11) ? null : reader.GetString(11));

    private static TaskRecord ReadTask(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : ToOffset(reader.GetDateTime(5)), ToOffset(reader.GetDateTime(6)), ToOffset(reader.GetDateTime(7)), EncodeETag((byte[])reader[8]), ToOffset(reader.GetDateTime(9)), ToOffset(reader.GetDateTime(10)), reader.GetString(11), reader.GetString(12), reader.GetString(13), reader.GetInt32(14), reader.IsDBNull(15) ? null : ToOffset(reader.GetDateTime(15)));

    private static EventRecord ReadEvent(SqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), ToOffset(reader.GetDateTime(3)), ToOffset(reader.GetDateTime(4)), reader.GetString(5), reader.GetString(6), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag((byte[])reader[9]), reader.GetBoolean(10), reader.IsDBNull(11) ? null : reader.GetString(11));

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES ((SELECT [UserId] FROM [platform].[PersonalSpace] WHERE [Id] = @OwnerId), @OwnerId, @Action, N'PersonalResource', @TargetId, 'Succeeded', @TraceId);";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        Add(command, "@Action", SqlDbType.NVarChar, action, 160);
        Add(command, "@TargetId", SqlDbType.UniqueIdentifier, targetId);
        Add(command, "@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value, 128);
        command.ExecuteNonQuery();
    }

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => IdentityOperationResult<T>.Failure("ModuleUnavailable", 409, "The requested module is disabled or unavailable for this user.");
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
        var code = claim.IsConflict ? "IdempotencyConflict" : claim.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
        var message = claim.IsConflict
            ? "The same Idempotency-Key was already used with a different request."
            : "The request was already completed or is in progress.";
        return IdentityOperationResult<T>.Failure(code, 409, message);
    }

    private void CompleteReceipt(SqlConnection connection, SqlTransaction transaction, ReceiptClaim claim, string resultCode) =>
        _receipts.Complete(connection, transaction, claim, resultCode);

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
