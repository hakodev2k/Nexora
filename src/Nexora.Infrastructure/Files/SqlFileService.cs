using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Nexora.Application.Files;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Files;

/// <summary>
/// Local-only private file authority. Uploads are staged under a generated
/// session id, checked against an allow-list and a conservative local scanner,
/// and become readable only after a clean FileObject is committed in SQL.
/// </summary>
public sealed class SqlFileService : IFileService
{
    private const long MaxBytes = 25 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = new[] { ".pdf" },
        ["image/png"] = new[] { ".png" },
        ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
        ["image/webp"] = new[] { ".webp" },
        ["text/plain"] = new[] { ".txt" },
        ["text/markdown"] = new[] { ".md", ".markdown" },
        ["text/csv"] = new[] { ".csv" },
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = new[] { ".docx" },
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = new[] { ".xlsx" }
    };

    private readonly SqlConnectionFactory _connections;
    private readonly SqlRequestReceiptStore _receipts;
    private readonly SqlSelfCapability _capabilities;
    private readonly string _storageRoot;

    public SqlFileService(SqlConnectionFactory connections, string storageRoot, string? idempotencySecret = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        _capabilities = new SqlSelfCapability(connections);
        if (string.IsNullOrWhiteSpace(storageRoot)) throw new ArgumentException("A private local file storage root is required.", nameof(storageRoot));
        _storageRoot = Path.GetFullPath(storageRoot);
        Directory.CreateDirectory(_storageRoot);
        Directory.CreateDirectory(Path.Combine(_storageRoot, ".staging"));
    }

    public IdentityOperationResult<FilePage> List(IdentityPrincipal actor, int? limit = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.read")) return ModuleUnavailable<FilePage>();
        var take = Math.Clamp(limit ?? 50, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [OriginalName], [MediaType], [ByteLength], [ScanState], [Lifecycle],
                   [CurrentRevision], [CreatedAt], [UpdatedAt], [RowVersion]
            FROM [files].[FileObject]
            WHERE [OwnerId] = @OwnerId AND [Lifecycle] <> 'Purged'
            ORDER BY [UpdatedAt] DESC, [Id] DESC;
            """;
        Add(command, "@Limit", SqlDbType.Int, take);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, actor.OwnerId);
        using var reader = command.ExecuteReader();
        var items = new List<FileRecord>();
        while (reader.Read()) items.Add(ReadFile(reader));
        return IdentityOperationResult<FilePage>.Success(new FilePage(items, null));
    }

    public IdentityOperationResult<FileRecord> Get(IdentityPrincipal actor, Guid fileId)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.read")) return ModuleUnavailable<FileRecord>();
        using var connection = _connections.Create();
        connection.Open();
        var file = ReadFile(connection, null, actor.OwnerId, fileId, forUpdate: false);
        return file is null ? Missing<FileRecord>() : IdentityOperationResult<FileRecord>.Success(file);
    }

    public IdentityOperationResult<FileUploadSessionRecord> InitiateUpload(IdentityPrincipal actor, FileUploadCommand command,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.upload")) return ModuleUnavailable<FileUploadSessionRecord>();
        var validation = ValidateUpload(command, out var safeName, out var mediaType);
        if (validation is not null) return validation;
        var sessionId = Guid.NewGuid();
        var rawHandle = CreateHandle();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FileUploadSessionRecord>(connection, transaction, actor,
            "files.upload.initiate", idempotencyKey,
            $"name:{safeName}|media:{mediaType}|bytes:{command.ExpectedBytes}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        Execute(connection, transaction, """
            INSERT INTO [files].[UploadSession]
                ([Id], [OwnerId], [CreatedByUserId], [OriginalName], [MediaType], [UploadHandleHash], [ExpectedBytes], [ExpiresAt])
            VALUES (@Id, @OwnerId, @Actor, @OriginalName, @MediaType, @HandleHash, @ExpectedBytes, @ExpiresAt);
            """,
            ("@Id", SqlDbType.UniqueIdentifier, (object)sessionId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@OriginalName", SqlDbType.NVarChar, (object)safeName),
            ("@MediaType", SqlDbType.VarChar, (object)mediaType),
            ("@HandleHash", SqlDbType.Binary, (object)HashHandle(rawHandle)),
            ("@ExpectedBytes", SqlDbType.BigInt, (object)command.ExpectedBytes),
            ("@ExpiresAt", SqlDbType.DateTime2, (object)expiresAt.UtcDateTime));
        WriteAudit(connection, transaction, actor, sessionId, "files.upload.initiate", traceId);
        var created = ReadUploadSession(connection, transaction, actor.OwnerId, sessionId, HashHandle(rawHandle), forUpdate: false);
        if (created is null)
        {
            transaction.Rollback();
            return PersistenceFailure<FileUploadSessionRecord>();
        }
        CompleteReceipt(connection, transaction, receipt, "UploadSessionCreated");
        transaction.Commit();
        return IdentityOperationResult<FileUploadSessionRecord>.Success(ToResponse(created, rawHandle), 201, "UploadSessionCreated");
    }

    public async Task<IdentityOperationResult<FileRecord>> CompleteUploadAsync(IdentityPrincipal actor, Guid uploadSessionId,
        string uploadHandle, Stream content, long? contentLength, string? idempotencyKey = null, string? traceId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.upload")) return ModuleUnavailable<FileRecord>();
        if (string.IsNullOrWhiteSpace(uploadHandle) || uploadHandle.Length > 256 || content is null)
            return Missing<FileRecord>();

        var stagePath = StagingPath(uploadSessionId);
        TryDelete(stagePath);
        string? finalPath = null;
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FileRecord>(connection, transaction, actor,
            "files.upload.complete", idempotencyKey,
            $"session:{uploadSessionId:N}|length:{contentLength?.ToString() ?? "unknown"}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }

        try
        {
            var digest = HashHandle(uploadHandle);
            var session = ReadUploadSession(connection, transaction, actor.OwnerId, uploadSessionId, digest, forUpdate: true);
            if (session is null || session.State is "Canceled" or "Failed" or "Ready")
            {
                transaction.Rollback();
                return Missing<FileRecord>();
            }
            if (session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                FailUpload(connection, transaction, session.Id, actor.OwnerId, "UploadExpired", 0);
                CompleteReceipt(connection, transaction, receipt, "UploadRejected");
                transaction.Commit();
                return Failure<FileRecord>("UploadExpired", 422, "The upload session has expired.");
            }
            if (contentLength is { } suppliedLength && suppliedLength != session.ExpectedBytes)
            {
                FailUpload(connection, transaction, session.Id, actor.OwnerId, "ByteLengthMismatch", 0);
                CompleteReceipt(connection, transaction, receipt, "UploadRejected");
                transaction.Commit();
                return Failure<FileRecord>("ByteLengthMismatch", 422, "The upload length does not match the initiated session.");
            }

            Execute(connection, transaction, "UPDATE [files].[UploadSession] SET [State] = 'Scanning', [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [State] IN ('Created','Uploading');",
                ("@Id", SqlDbType.UniqueIdentifier, (object)session.Id), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
            var copied = await CopyToStagingAsync(content, stagePath, session.ExpectedBytes, cancellationToken);
            var scanError = ScanStagedFile(stagePath, session.OriginalName, session.MediaType);
            if (copied.Bytes != session.ExpectedBytes || scanError is not null)
            {
                var code = copied.Bytes != session.ExpectedBytes ? "ByteLengthMismatch" : scanError!;
                FailUpload(connection, transaction, session.Id, actor.OwnerId, code, Math.Min(copied.Bytes, session.ExpectedBytes));
                CompleteReceipt(connection, transaction, receipt, "UploadRejected");
                transaction.Commit();
                TryDelete(stagePath);
                return Failure<FileRecord>("UploadRejected", 422, "The file failed the local type, size or content safety checks.");
            }

            var fileId = Guid.NewGuid();
            var storageKey = $"{actor.OwnerId:N}/{fileId:N}/r1.bin";
            finalPath = ResolveStoragePath(storageKey);
            if (finalPath is null) throw new IOException("Generated storage path is outside the private storage root.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.Move(stagePath, finalPath);
            Execute(connection, transaction, """
                INSERT INTO [files].[FileObject]
                    ([Id], [OwnerId], [CreatedByUserId], [UpdatedByUserId], [StorageKey], [OriginalName], [MediaType], [ByteLength], [Digest], [ScanState], [Lifecycle])
                VALUES (@Id, @OwnerId, @Actor, @Actor, @StorageKey, @OriginalName, @MediaType, @ByteLength, @Digest, 'Clean', 'Active');
                UPDATE [files].[UploadSession]
                SET [FileObjectId] = @Id, [ReceivedBytes] = @ByteLength, [State] = 'Ready', [UpdatedAt] = SYSUTCDATETIME(), [ErrorCode] = NULL
                WHERE [Id] = @SessionId AND [OwnerId] = @OwnerId AND [State] = 'Scanning';
                """,
                ("@Id", SqlDbType.UniqueIdentifier, (object)fileId),
                ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
                ("@StorageKey", SqlDbType.NVarChar, (object)storageKey),
                ("@OriginalName", SqlDbType.NVarChar, (object)session.OriginalName),
                ("@MediaType", SqlDbType.VarChar, (object)session.MediaType),
                ("@ByteLength", SqlDbType.BigInt, (object)copied.Bytes),
                ("@Digest", SqlDbType.Binary, (object)copied.Digest),
                ("@SessionId", SqlDbType.UniqueIdentifier, (object)session.Id));
            WriteAudit(connection, transaction, actor, fileId, "files.upload.complete", traceId);
            var file = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: false);
            if (file is null)
            {
                transaction.Rollback();
                TryDelete(finalPath);
                return PersistenceFailure<FileRecord>();
            }
            CompleteReceipt(connection, transaction, receipt, "FileCreated");
            transaction.Commit();
            return IdentityOperationResult<FileRecord>.Success(file, 201, "FileCreated");
        }
        catch (OperationCanceledException)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            TryDelete(stagePath);
            TryDelete(finalPath);
            throw;
        }
        catch (SqlException)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            TryDelete(stagePath);
            TryDelete(finalPath);
            return PersistenceFailure<FileRecord>();
        }
        catch (IOException)
        {
            try { transaction.Rollback(); } catch (InvalidOperationException) { }
            TryDelete(stagePath);
            TryDelete(finalPath);
            return Failure<FileRecord>("StorageUnavailable", 503, "Private file storage is unavailable.");
        }
    }

    public IdentityOperationResult<object?> CancelUpload(IdentityPrincipal actor, Guid uploadSessionId, string uploadHandle,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.cancel_upload")) return ModuleUnavailable<object?>();
        if (string.IsNullOrWhiteSpace(uploadHandle) || uploadHandle.Length > 256) return Missing<object?>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor,
            "files.upload.cancel", idempotencyKey, $"session:{uploadSessionId:N}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var session = ReadUploadSession(connection, transaction, actor.OwnerId, uploadSessionId, HashHandle(uploadHandle), forUpdate: true);
        if (session is null)
        {
            transaction.Rollback();
            return Missing<object?>();
        }
        if (session.State == "Ready")
        {
            transaction.Rollback();
            return Failure<object?>("UploadAlreadyCompleted", 409, "The upload session has already completed.");
        }
        Execute(connection, transaction, "UPDATE [files].[UploadSession] SET [State] = 'Canceled', [UpdatedAt] = SYSUTCDATETIME(), [ErrorCode] = 'Canceled' WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [State] NOT IN ('Ready','Canceled');",
            ("@Id", SqlDbType.UniqueIdentifier, (object)uploadSessionId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId));
        WriteAudit(connection, transaction, actor, uploadSessionId, "files.upload.cancel", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        TryDelete(StagingPath(uploadSessionId));
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<FileRecord> Rename(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        FileRenameCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.rename")) return ModuleUnavailable<FileRecord>();
        var safeName = SafeFileName(command.OriginalName);
        if (safeName is null) return Failure<FileRecord>("ValidationFailed", 422, "File name is invalid.");
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<FileRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FileRecord>(connection, transaction, actor, "files.file.rename", idempotencyKey,
            $"file:{fileId:N}|etag:{ifMatch}|name:{safeName}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var current = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: true);
        if (current is null) { transaction.Rollback(); return Missing<FileRecord>(); }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(current.ETag))) { transaction.Rollback(); return Revision<FileRecord>(); }
        Execute(connection, transaction, "UPDATE [files].[FileObject] SET [OriginalName] = @Name, [UpdatedByUserId] = @Actor, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Lifecycle] <> 'Purged' AND [RowVersion] = @RowVersion;",
            ("@Name", SqlDbType.NVarChar, (object)safeName), ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Id", SqlDbType.UniqueIdentifier, (object)fileId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, fileId, "files.file.rename", traceId);
        var updated = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: false);
        if (updated is null) { transaction.Rollback(); return PersistenceFailure<FileRecord>(); }
        CompleteReceipt(connection, transaction, receipt, "FileRenamed");
        transaction.Commit();
        return IdentityOperationResult<FileRecord>.Success(updated);
    }

    public IdentityOperationResult<FileReferenceRecord> Attach(IdentityPrincipal actor, Guid fileId,
        FileReferenceCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.reference.attach")) return ModuleUnavailable<FileReferenceRecord>();
        var validation = ValidateReference(command);
        if (validation is not null) return validation;
        var sourceModule = command.ResourceType.Trim() switch { "Project" => "FX11", "Task" => "FX12", "Document" => "FX20", _ => string.Empty };
        if (string.IsNullOrEmpty(sourceModule) || !ModuleAvailable(actor, sourceModule, sourceModule == "FX20" ? "documents.page.read" : sourceModule == "FX12" ? "tasks.task.read" : "projects.project.read"))
            return ModuleUnavailable<FileReferenceRecord>();
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FileReferenceRecord>(connection, transaction, actor, "files.reference.attach", idempotencyKey,
            $"file:{fileId:N}|resource:{command.ResourceType}|id:{command.ResourceId:N}|version:{command.VersionNumber}|purpose:{command.Purpose}|key:{command.ReferenceKey}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var file = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: true);
        if (file is null || file.Lifecycle != "Active" || file.ScanState != "Clean")
        {
            transaction.Rollback();
            return Missing<FileReferenceRecord>();
        }
        if (!SourceAvailable(connection, transaction, command, actor.OwnerId))
        {
            transaction.Rollback();
            return Failure<FileReferenceRecord>("ResourceUnavailable", 404, "The attachment target is unavailable.");
        }
        var referenceId = Guid.NewGuid();
        try
        {
            Execute(connection, transaction, "INSERT INTO [files].[FileReference] ([Id], [OwnerId], [FileObjectId], [ResourceType], [ResourceId], [VersionNumber], [Purpose], [ReferenceKey]) VALUES (@Id, @OwnerId, @FileId, @ResourceType, @ResourceId, @VersionNumber, @Purpose, @ReferenceKey);",
                ("@Id", SqlDbType.UniqueIdentifier, (object)referenceId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
                ("@FileId", SqlDbType.UniqueIdentifier, (object)fileId), ("@ResourceType", SqlDbType.VarChar, (object)command.ResourceType.Trim()),
                ("@ResourceId", SqlDbType.UniqueIdentifier, (object)command.ResourceId), ("@VersionNumber", SqlDbType.BigInt, (object?)command.VersionNumber ?? DBNull.Value),
                ("@Purpose", SqlDbType.VarChar, (object)command.Purpose.Trim()), ("@ReferenceKey", SqlDbType.NVarChar, (object)command.ReferenceKey.Trim()));
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            transaction.Rollback();
            return Failure<FileReferenceRecord>("ReferenceAlreadyExists", 409, "The file is already attached to this target.");
        }
        WriteAudit(connection, transaction, actor, referenceId, "files.reference.attach", traceId);
        var reference = ReadReference(connection, transaction, actor.OwnerId, referenceId, forUpdate: false);
        if (reference is null) { transaction.Rollback(); return PersistenceFailure<FileReferenceRecord>(); }
        CompleteReceipt(connection, transaction, receipt, "FileAttached");
        transaction.Commit();
        return IdentityOperationResult<FileReferenceRecord>.Success(reference, 201, "FileAttached");
    }

    public IdentityOperationResult<object?> Detach(IdentityPrincipal actor, Guid referenceId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.reference.detach")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "files.reference.detach", idempotencyKey,
            $"reference:{referenceId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var reference = ReadReference(connection, transaction, actor.OwnerId, referenceId, forUpdate: true);
        if (reference is null) { transaction.Rollback(); return Missing<object?>(); }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(reference.ETag))) { transaction.Rollback(); return Revision<object?>(); }
        Execute(connection, transaction, "DELETE FROM [files].[FileReference] WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [RowVersion] = @RowVersion;",
            ("@Id", SqlDbType.UniqueIdentifier, (object)referenceId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, referenceId, "files.reference.detach", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<FileDownload> OpenContent(IdentityPrincipal actor, Guid fileId, bool inline)
    {
        var action = inline ? "files.file.preview" : "files.file.download";
        if (!ModuleAvailable(actor, "FX07", action)) return ModuleUnavailable<FileDownload>();
        using var connection = _connections.Create();
        connection.Open();
        var file = ReadFileWithStorage(connection, null, actor.OwnerId, fileId, forUpdate: false);
        if (file is null || file.Value.Record.Lifecycle != "Active" || file.Value.Record.ScanState != "Clean")
            return Missing<FileDownload>();
        var path = ResolveStoragePath(file.Value.StorageKey);
        if (path is null || !File.Exists(path)) return Failure<FileDownload>("StorageUnavailable", 503, "Private file storage is unavailable.");
        try
        {
            return IdentityOperationResult<FileDownload>.Success(new FileDownload(
                new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan),
                file.Value.Record.MediaType, file.Value.Record.OriginalName, file.Value.Record.ByteLength));
        }
        catch (IOException)
        {
            return Failure<FileDownload>("StorageUnavailable", 503, "Private file storage is unavailable.");
        }
    }

    public IdentityOperationResult<object?> Trash(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.trash")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "files.file.trash", idempotencyKey,
            $"file:{fileId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var file = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: true);
        if (file is null || file.Lifecycle != "Active") { transaction.Rollback(); return Missing<object?>(); }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(file.ETag))) { transaction.Rollback(); return Revision<object?>(); }
        if (HasReferences(connection, transaction, actor.OwnerId, fileId))
        {
            transaction.Rollback();
            return Failure<object?>("DependencyUnavailable", 409, "A referenced file cannot be moved to trash until it is detached.");
        }
        var batchId = Guid.NewGuid();
        Execute(connection, transaction, """
            INSERT INTO [platform].[TrashItem] ([OwnerId], [ResourceType], [ResourceId], [DeletionBatchId], [PriorStatus]) VALUES (@OwnerId, 'File', @FileId, @BatchId, 'Active');
            UPDATE [files].[FileObject] SET [Lifecycle] = 'Trash', [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @Actor WHERE [Id] = @FileId AND [OwnerId] = @OwnerId AND [Lifecycle] = 'Active' AND [RowVersion] = @RowVersion;
            """,
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@FileId", SqlDbType.UniqueIdentifier, (object)fileId),
            ("@BatchId", SqlDbType.UniqueIdentifier, (object)batchId), ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, fileId, "files.file.trash", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        return IdentityOperationResult<object?>.NoContent();
    }

    public IdentityOperationResult<FileRecord> Restore(IdentityPrincipal actor, Guid fileId, Guid deletionBatchId,
        string? ifMatch, string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.restore")) return ModuleUnavailable<FileRecord>();
        if (deletionBatchId == Guid.Empty || !TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<FileRecord>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<FileRecord>(connection, transaction, actor, "files.file.restore", idempotencyKey,
            $"file:{fileId:N}|batch:{deletionBatchId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var file = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: true);
        if (file is null || file.Lifecycle != "Trash" || !TrashBatchExists(connection, transaction, actor.OwnerId, fileId, deletionBatchId))
        {
            transaction.Rollback();
            return Missing<FileRecord>();
        }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(file.ETag))) { transaction.Rollback(); return Revision<FileRecord>(); }
        Execute(connection, transaction, "UPDATE [files].[FileObject] SET [Lifecycle] = 'Active', [UpdatedAt] = SYSUTCDATETIME(), [UpdatedByUserId] = @Actor WHERE [Id] = @FileId AND [OwnerId] = @OwnerId AND [Lifecycle] = 'Trash' AND [RowVersion] = @RowVersion; UPDATE [platform].[TrashItem] SET [RestoredAt] = COALESCE([RestoredAt], SYSUTCDATETIME()) WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'File' AND [ResourceId] = @FileId AND [DeletionBatchId] = @BatchId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;",
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@FileId", SqlDbType.UniqueIdentifier, (object)fileId),
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@RowVersion", SqlDbType.Binary, (object)expectedVersion),
            ("@BatchId", SqlDbType.UniqueIdentifier, (object)deletionBatchId));
        WriteAudit(connection, transaction, actor, fileId, "files.file.restore", traceId);
        var restored = ReadFile(connection, transaction, actor.OwnerId, fileId, forUpdate: false);
        if (restored is null) { transaction.Rollback(); return PersistenceFailure<FileRecord>(); }
        CompleteReceipt(connection, transaction, receipt, "FileRestored");
        transaction.Commit();
        return IdentityOperationResult<FileRecord>.Success(restored);
    }

    public IdentityOperationResult<object?> Purge(IdentityPrincipal actor, Guid fileId, string? ifMatch,
        string? idempotencyKey = null, string? traceId = null)
    {
        if (!ModuleAvailable(actor, "FX07", "files.file.purge")) return ModuleUnavailable<object?>();
        if (!TryDecodeETag(ifMatch, out var expectedVersion)) return Precondition<object?>(ifMatch);
        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var receiptFailure = CheckReceipt<object?>(connection, transaction, actor, "files.file.purge", idempotencyKey,
            $"file:{fileId:N}|etag:{ifMatch}", out var receipt);
        if (receiptFailure is not null) { transaction.Rollback(); return receiptFailure; }
        var file = ReadFileWithStorage(connection, transaction, actor.OwnerId, fileId, forUpdate: true);
        if (file is null || file.Value.Record.Lifecycle != "Trash") { transaction.Rollback(); return Missing<object?>(); }
        if (!expectedVersion.AsSpan().SequenceEqual(DecodeETag(file.Value.Record.ETag))) { transaction.Rollback(); return Revision<object?>(); }
        if (HasReferences(connection, transaction, actor.OwnerId, fileId))
        {
            transaction.Rollback();
            return Failure<object?>("DependencyUnavailable", 409, "A referenced file cannot be purged.");
        }
        if (!TrashBatchExists(connection, transaction, actor.OwnerId, fileId, deletionBatchId: null))
        {
            transaction.Rollback();
            return Missing<object?>();
        }
        Execute(connection, transaction, """
            UPDATE [platform].[TrashItem] SET [PurgedAt] = COALESCE([PurgedAt], SYSUTCDATETIME())
            WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'File' AND [ResourceId] = @FileId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;
            UPDATE [files].[UploadSession] SET [FileObjectId] = NULL, [UpdatedAt] = SYSUTCDATETIME() WHERE [OwnerId] = @OwnerId AND [FileObjectId] = @FileId;
            DELETE FROM [files].[FileObject] WHERE [Id] = @FileId AND [OwnerId] = @OwnerId AND [Lifecycle] = 'Trash' AND [RowVersion] = @RowVersion;
            """,
            ("@OwnerId", SqlDbType.UniqueIdentifier, (object)actor.OwnerId), ("@FileId", SqlDbType.UniqueIdentifier, (object)fileId),
            ("@RowVersion", SqlDbType.Binary, (object)expectedVersion));
        WriteAudit(connection, transaction, actor, fileId, "files.file.purge", traceId);
        CompleteReceipt(connection, transaction, receipt, "NoContent");
        transaction.Commit();
        var path = ResolveStoragePath(file.Value.StorageKey);
        try
        {
            if (path is not null && File.Exists(path)) File.Delete(path);
            return IdentityOperationResult<object?>.NoContent();
        }
        catch (IOException)
        {
            return Failure<object?>("StorageCleanupPending", 503, "File metadata was purged but private storage cleanup is pending.");
        }
    }

    private static IdentityOperationResult<FileUploadSessionRecord>? ValidateUpload(FileUploadCommand command, out string safeName, out string mediaType)
    {
        safeName = SafeFileName(command.OriginalName) ?? string.Empty;
        mediaType = command.MediaType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (safeName.Length == 0 || safeName.Length > 255 || !AllowedTypes.TryGetValue(mediaType, out var extensions) ||
            !extensions.Contains(Path.GetExtension(safeName), StringComparer.OrdinalIgnoreCase) || command.ExpectedBytes <= 0 || command.ExpectedBytes > MaxBytes)
            return Failure<FileUploadSessionRecord>("ValidationFailed", 422, "File name, type or size is invalid.");
        return null;
    }

    private static IdentityOperationResult<FileReferenceRecord>? ValidateReference(FileReferenceCommand command)
    {
        if (command.ResourceId == Guid.Empty || command.ResourceType?.Trim() is not ("Project" or "Task" or "Document") ||
            string.IsNullOrWhiteSpace(command.Purpose) || command.Purpose.Trim().Length > 64 ||
            string.IsNullOrWhiteSpace(command.ReferenceKey) || command.ReferenceKey.Trim().Length > 128 ||
            command.VersionNumber is <= 0)
            return Failure<FileReferenceRecord>("ValidationFailed", 422, "Attachment target or reference metadata is invalid.");
        return null;
    }

    private static string? SafeFileName(string? originalName)
    {
        if (string.IsNullOrWhiteSpace(originalName)) return null;
        var value = originalName.Trim();
        if (value is "." or ".." || value.Length > 255 || value.Any(char.IsControl) || value.Contains('/') || value.Contains('\\')) return null;
        if (!string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal)) return null;
        if (Path.GetInvalidFileNameChars().Any(character => value.Contains(character))) return null;
        return value;
    }

    private static async Task<(long Bytes, byte[] Digest)> CopyToStagingAsync(Stream content, string path, long expectedBytes, CancellationToken cancellationToken)
    {
        long total = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await content.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > MaxBytes || total > expectedBytes) break;
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        await output.FlushAsync(cancellationToken);
        return (total, hash.GetHashAndReset());
    }

    private static string? ScanStagedFile(string path, string originalName, string mediaType)
    {
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (new FileInfo(path).Length == 0) return mediaType is "text/plain" or "text/markdown" or "text/csv" ? null : "EmptyBinary";
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        var header = new byte[16];
        var headerLength = input.Read(header, 0, header.Length);
        input.Position = 0;
        if (extension == ".pdf" && !StartsWith(header, headerLength, Encoding.ASCII.GetBytes("%PDF-"))) return "PdfSignatureInvalid";
        if (extension == ".png" && !StartsWith(header, headerLength, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "PngSignatureInvalid";
        if (extension is ".jpg" or ".jpeg" && !(headerLength >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff)) return "JpegSignatureInvalid";
        if (extension == ".webp" && !(StartsWith(header, headerLength, Encoding.ASCII.GetBytes("RIFF")) && headerLength >= 12 && Encoding.ASCII.GetString(header, 8, 4) == "WEBP")) return "WebpSignatureInvalid";
        if (extension is ".docx" or ".xlsx" && !StartsWith(header, headerLength, new byte[] { 0x50, 0x4b, 0x03, 0x04 })) return "OfficeArchiveInvalid";
        if (mediaType is "text/plain" or "text/markdown" or "text/csv") return ScanText(path);
        if (extension is ".docx" or ".xlsx") return ScanOfficeArchive(path);
        return null;
    }

    private static string? ScanText(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Contains((byte)0)) return "BinaryTextPayload";
            var text = new UTF8Encoding(false, true).GetString(bytes);
            var lower = text.ToLowerInvariant();
            return lower.Contains("<script", StringComparison.Ordinal) || lower.Contains("javascript:", StringComparison.Ordinal) ||
                   lower.Contains("<iframe", StringComparison.Ordinal) || lower.Contains("data:text/html", StringComparison.Ordinal)
                ? "ScriptPayloadRejected" : null;
        }
        catch (DecoderFallbackException) { return "InvalidTextEncoding"; }
    }

    private static string? ScanOfficeArchive(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            long uncompressed = 0;
            if (archive.Entries.Count > 1000) return "ArchiveTooManyEntries";
            foreach (var entry in archive.Entries)
            {
                uncompressed += entry.Length;
                if (entry.Length > MaxBytes || uncompressed > 100 * 1024 * 1024 || entry.FullName.StartsWith("/", StringComparison.Ordinal) || entry.FullName.Contains("..", StringComparison.Ordinal)) return "ArchiveUnsafe";
                var lower = entry.FullName.ToLowerInvariant();
                if (lower.EndsWith(".js", StringComparison.Ordinal) || lower.EndsWith(".html", StringComparison.Ordinal) || lower.EndsWith(".svg", StringComparison.Ordinal) || lower.Contains("vbaproject", StringComparison.Ordinal)) return "ArchiveScriptPayload";
                if (lower.EndsWith(".rels", StringComparison.Ordinal) && entry.Length <= 64 * 1024)
                {
                    using var reader = new StreamReader(entry.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false);
                    var rels = reader.ReadToEnd();
                    if (rels.Contains("TargetMode=\"External\"", StringComparison.OrdinalIgnoreCase)) return "ArchiveExternalReference";
                }
            }
            return null;
        }
        catch (InvalidDataException) { return "ArchiveInvalid"; }
        catch (IOException) { return "ArchiveUnreadable"; }
    }

    private static bool StartsWith(byte[] data, int length, byte[] prefix) => length >= prefix.Length && data.AsSpan(0, prefix.Length).SequenceEqual(prefix);

    private static bool SourceAvailable(SqlConnection connection, SqlTransaction transaction, FileReferenceCommand command, Guid ownerId)
    {
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = command.ResourceType.Trim() switch
        {
            "Project" => "SELECT CASE WHEN [Status] NOT IN ('Completed','Skipped','Deleted') THEN 1 ELSE 0 END FROM [productivity].[Project] WHERE [Id] = @Id AND [OwnerId] = @OwnerId",
            "Task" => "SELECT CASE WHEN t.[Status] NOT IN ('Completed','Skipped','Deleted') AND p.[Status] NOT IN ('Completed','Skipped','Deleted') THEN 1 ELSE 0 END FROM [productivity].[Task] t INNER JOIN [productivity].[Project] p ON p.[Id] = t.[ProjectId] AND p.[OwnerId] = t.[OwnerId] WHERE t.[Id] = @Id AND t.[OwnerId] = @OwnerId",
            _ => "SELECT CASE WHEN [DeletedAt] IS NULL THEN 1 ELSE 0 END FROM [documents].[Page] WHERE [Id] = @Id AND [OwnerId] = @OwnerId"
        };
        Add(query, "@Id", SqlDbType.UniqueIdentifier, command.ResourceId);
        Add(query, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        if (command.ResourceType.Trim() == "Document" && command.VersionNumber is { } version)
        {
            query.CommandText += " AND EXISTS (SELECT 1 FROM [documents].[PageVersion] WHERE [PageId] = @Id AND [OwnerId] = @OwnerId AND [VersionNumber] = @Version)";
            Add(query, "@Version", SqlDbType.BigInt, version);
        }
        query.CommandText += ";";
        return Convert.ToInt32(query.ExecuteScalar() ?? 0) == 1;
    }

    private static FileRecord? ReadFile(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid fileId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [OriginalName], [MediaType], [ByteLength], [ScanState], [Lifecycle], [CurrentRevision], [CreatedAt], [UpdatedAt], [RowVersion] FROM [files].[FileObject] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : "WITH (NOLOCK)")} WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Lifecycle] <> 'Purged';";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, fileId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadFile(reader) : null;
    }

    private static FileRecord ReadFile(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetString(4), reader.GetString(5), reader.GetInt64(6), ToOffset(reader.GetDateTime(7)), ToOffset(reader.GetDateTime(8)), EncodeETag(reader.GetFieldValue<byte[]>(9)));

    private static (FileRecord Record, string StorageKey)? ReadFileWithStorage(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid fileId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [StorageKey], [Id], [OriginalName], [MediaType], [ByteLength], [ScanState], [Lifecycle], [CurrentRevision], [CreatedAt], [UpdatedAt], [RowVersion] FROM [files].[FileObject] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : "WITH (NOLOCK)")} WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [Lifecycle] <> 'Purged';";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, fileId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (new FileRecord(reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetInt64(4), reader.GetString(5), reader.GetString(6), reader.GetInt64(7), ToOffset(reader.GetDateTime(8)), ToOffset(reader.GetDateTime(9)), EncodeETag(reader.GetFieldValue<byte[]>(10))), reader.GetString(0));
    }

    private static FileReferenceRecord? ReadReference(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid referenceId, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [FileObjectId], [ResourceType], [ResourceId], [VersionNumber], [Purpose], [ReferenceKey], [CreatedAt], [RowVersion] FROM [files].[FileReference] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : "WITH (NOLOCK)")} WHERE [Id] = @Id AND [OwnerId] = @OwnerId;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, referenceId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return !reader.Read() ? null : new FileReferenceRecord(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetGuid(3), reader.IsDBNull(4) ? null : reader.GetInt64(4), reader.GetString(5), reader.GetString(6), ToOffset(reader.GetDateTime(7)), EncodeETag(reader.GetFieldValue<byte[]>(8)));
    }

    private static UploadSessionData? ReadUploadSession(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid id, byte[] handleHash, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT [Id], [OriginalName], [MediaType], [ExpectedBytes], [ReceivedBytes], [State], [ExpiresAt], [RowVersion] FROM [files].[UploadSession] {(forUpdate ? "WITH (UPDLOCK, ROWLOCK)" : "WITH (NOLOCK)")} WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [UploadHandleHash] = @HandleHash;";
        Add(command, "@Id", SqlDbType.UniqueIdentifier, id);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@HandleHash", SqlDbType.Binary, handleHash, 32);
        using var reader = command.ExecuteReader();
        return !reader.Read() ? null : new UploadSessionData(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetString(5), ToOffset(reader.GetDateTime(6)), EncodeETag(reader.GetFieldValue<byte[]>(7)));
    }

    private static FileUploadSessionRecord ToResponse(UploadSessionData session, string rawHandle) => new(session.Id, session.ExpectedBytes, session.ReceivedBytes, session.State, session.ExpiresAt, rawHandle, session.ETag);

    private static bool HasReferences(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid fileId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM [files].[FileReference] WHERE [OwnerId] = @OwnerId AND [FileObjectId] = @FileId) THEN 1 ELSE 0 END;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@FileId", SqlDbType.UniqueIdentifier, fileId);
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
    }

    private static bool TrashBatchExists(SqlConnection connection, SqlTransaction transaction, Guid ownerId, Guid fileId, Guid? deletionBatchId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = deletionBatchId is null
            ? "SELECT TOP (1) 1 FROM [platform].[TrashItem] WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'File' AND [ResourceId] = @FileId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL ORDER BY [DeletedAt] DESC;"
            : "SELECT 1 FROM [platform].[TrashItem] WHERE [OwnerId] = @OwnerId AND [ResourceType] = 'File' AND [ResourceId] = @FileId AND [DeletionBatchId] = @BatchId AND [RestoredAt] IS NULL AND [PurgedAt] IS NULL;";
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        Add(command, "@FileId", SqlDbType.UniqueIdentifier, fileId);
        if (deletionBatchId is not null) Add(command, "@BatchId", SqlDbType.UniqueIdentifier, deletionBatchId.Value);
        return command.ExecuteScalar() is not null;
    }

    private void FailUpload(SqlConnection connection, SqlTransaction transaction, Guid sessionId, Guid ownerId, string errorCode, long receivedBytes) =>
        Execute(connection, transaction, "UPDATE [files].[UploadSession] SET [State] = 'Failed', [ReceivedBytes] = @Received, [ErrorCode] = @ErrorCode, [UpdatedAt] = SYSUTCDATETIME() WHERE [Id] = @Id AND [OwnerId] = @OwnerId;",
            ("@Received", SqlDbType.BigInt, (object)receivedBytes), ("@ErrorCode", SqlDbType.VarChar, (object)errorCode),
            ("@Id", SqlDbType.UniqueIdentifier, (object)sessionId), ("@OwnerId", SqlDbType.UniqueIdentifier, (object)ownerId));

    private string StagingPath(Guid sessionId) => Path.Combine(_storageRoot, ".staging", sessionId.ToString("N") + ".upload");
    private string? ResolveStoragePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains('\\') || storageKey.Contains("..", StringComparison.Ordinal)) return null;
        var full = Path.GetFullPath(Path.Combine(_storageRoot, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var root = _storageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return full.StartsWith(root, StringComparison.Ordinal) ? full : null;
    }

    private static string CreateHandle() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static byte[] HashHandle(string handle) => SHA256.HashData(Encoding.UTF8.GetBytes(handle));
    private static void TryDelete(string? path) { try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string? traceId) =>
        Execute(connection, transaction, "INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId]) VALUES (@Actor, @OwnerUser, @Action, N'files.FileObject', @Target, 'Succeeded', @TraceId);",
            ("@Actor", SqlDbType.UniqueIdentifier, (object)actor.UserId), ("@OwnerUser", SqlDbType.UniqueIdentifier, (object)actor.UserId),
            ("@Action", SqlDbType.NVarChar, (object)action), ("@Target", SqlDbType.UniqueIdentifier, (object)targetId), ("@TraceId", SqlDbType.NVarChar, (object?)traceId ?? DBNull.Value));

    private bool ModuleAvailable(IdentityPrincipal actor, string moduleCode, params string[] actionKeys) => _capabilities.IsAllowed(actor, moduleCode, actionKeys);

    private IdentityOperationResult<T>? CheckReceipt<T>(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, string operationKey, string? idempotencyKey, string canonicalRequest, out ReceiptClaim claim)
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

    private static IdentityOperationResult<T> ModuleUnavailable<T>() => Failure<T>("ModuleUnavailable", 409, "Files are disabled or unavailable for this user.");
    private static IdentityOperationResult<T> Missing<T>() => Failure<T>("ResourceUnavailable", 404, "File resource unavailable.");
    private static IdentityOperationResult<T> Revision<T>() => Failure<T>("RevisionConflict", 412, "File resource revision changed.");
    private static IdentityOperationResult<T> Precondition<T>(string? ifMatch) => Failure<T>(string.IsNullOrWhiteSpace(ifMatch) ? "PreconditionRequired" : "RevisionConflict", string.IsNullOrWhiteSpace(ifMatch) ? 428 : 412, string.IsNullOrWhiteSpace(ifMatch) ? "If-Match is required." : "If-Match is invalid.");
    private static IdentityOperationResult<T> PersistenceFailure<T>() => Failure<T>("PersistenceUnavailable", 503, "File persistence is unavailable.");
    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);

    private sealed record UploadSessionData(Guid Id, string OriginalName, string MediaType, long ExpectedBytes, long ReceivedBytes, string State, DateTimeOffset ExpiresAt, string ETag);
}
