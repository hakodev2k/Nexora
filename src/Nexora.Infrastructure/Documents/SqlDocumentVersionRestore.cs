using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    private const string RestoreVersionAction = "documents.page.restore_version";
    private static readonly string[] RestoreVersionActions = [RestoreVersionAction, HistoryAction, "documents.page.save"];

    private bool VersionAuthority(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, params string[] actions) =>
        _capabilities.IsAllowed(connection, transaction, actor, "FX20", actions);

    private static DocumentVersionPreview? ReadVersionSnapshot(SqlConnection connection, SqlTransaction transaction,
        Guid owner, Guid page, long version)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"SELECT {VersionColumns}, [Body] FROM [documents].[PageVersion] WHERE [OwnerId]=@Owner AND [PageId]=@Page AND [VersionNumber]=@Version;";
        Add(command, "@Owner", SqlDbType.UniqueIdentifier, owner); Add(command, "@Page", SqlDbType.UniqueIdentifier, page);
        Add(command, "@Version", SqlDbType.BigInt, version);
        using var reader = command.ExecuteReader();
        return reader.Read() ? new(ReadVersion(reader), reader.GetString(13)) : null;
    }

    public IdentityOperationResult<DocumentVersionComparison> Compare(IdentityPrincipal actor, Guid pageId, long before, long after)
    {
        if (before <= 0 || after <= 0) return Failure<DocumentVersionComparison>("ValidationFailed", 422, "Version numbers must be positive.");
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!VersionAuthority(connection, transaction, actor, HistoryAction, "documents.page.read")) return ModuleUnavailable<DocumentVersionComparison>();
        if (!HistoryPageExists(connection, transaction, actor.OwnerId, pageId)) return Failure<DocumentVersionComparison>("ResourceUnavailable", 404, "Document unavailable.");
        var left = ReadVersionSnapshot(connection, transaction, actor.OwnerId, pageId, before);
        var right = ReadVersionSnapshot(connection, transaction, actor.OwnerId, pageId, after);
        if (left is null || right is null) return Failure<DocumentVersionComparison>("ResourceUnavailable", 404, "Document version unavailable.");
        if (left.Snapshot.DocumentType != right.Snapshot.DocumentType || left.Snapshot.EditorMode != right.Snapshot.EditorMode)
            return Failure<DocumentVersionComparison>("SnapshotIncompatible", 409, "The snapshots have incompatible immutable formats.");
        if (!VersionAuthority(connection, transaction, actor, HistoryAction, "documents.page.read")) return ModuleUnavailable<DocumentVersionComparison>();
        transaction.Commit();
        return IdentityOperationResult<DocumentVersionComparison>.Success(new(left, right));
    }

    public IdentityOperationResult<DocumentDetail> RestoreVersion(IdentityPrincipal actor, Guid pageId, string? ifMatch,
        DocumentRestoreVersionCommand command, string? idempotencyKey = null, string? traceId = null)
    {
        if (command.VersionNumber <= 0 || command.Tag?.Id == Guid.Empty || command.ChangeNote is { Length: > 500 })
            return Failure<DocumentDetail>("ValidationFailed", 422, "Restore selection is invalid.");
        if (!TryDecodeETag(ifMatch, out var expected)) return Precondition(ifMatch);
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!VersionAuthority(connection, transaction, actor, RestoreVersionActions)) return ModuleUnavailable<DocumentDetail>();
        SqlDocumentScopeLock.Acquire(connection, transaction, actor.OwnerId);
        var receipt = default(Nexora.Infrastructure.Identity.ReceiptClaim);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            receipt = _receipts.TryClaim(connection, transaction, actor.UserId, RestoreVersionAction, idempotencyKey,
                JsonSerializer.Serialize(new { version = 1, actor.OwnerId, pageId, etag = EncodeETag(expected), command.VersionNumber,
                    tagSpecified = command.Tag is not null, tagId = command.Tag?.Id, note = TrimOrNull(command.ChangeNote, 500) }), DateTime.UtcNow);
            if (!receipt.IsClaimed)
            {
                if (receipt.IsInvalid) return Failure<DocumentDetail>("InvalidIdempotencyKey", 422, "The Idempotency-Key must be a UUID.");
                if (receipt.IsConflict) return Failure<DocumentDetail>("IdempotencyConflict", 409, "The key belongs to another request.");
                if (!receipt.IsReplay) return Failure<DocumentDetail>("RequestInProgress", 409, "The request is in progress.");
                RestoreReceipt? stored = null;
                try { stored = JsonSerializer.Deserialize<RestoreReceipt>(receipt.ResultJson ?? string.Empty); } catch (JsonException) { }
                if (stored is null || stored.Version != 1 || stored.Id != pageId || stored.UserId != actor.UserId
                    || stored.OwnerId != actor.OwnerId || stored.SourceVersion != command.VersionNumber
                    || receipt.ResultCode != "DocumentVersionRestored" || receipt.ResultStatusCode != 200)
                    return Failure<DocumentDetail>("ReceiptUnavailable", 503, "The committed restore receipt is unavailable.");
                if (!VersionAuthority(connection, transaction, actor, RestoreVersionActions)) return ModuleUnavailable<DocumentDetail>();
                return IdentityOperationResult<DocumentDetail>.Committed(pageId, 200, "DocumentVersionRestored");
            }
        }
        try
        {
            var current = ReadDetail(connection, transaction, actor.OwnerId, pageId, true);
            if (current is null) return Missing();
            if (!CryptographicOperations.FixedTimeEquals(expected, DecodeETag(current.ETag))) return Revision();
            if (current.Status is not ("Draft" or "Published")) return Failure<DocumentDetail>("LifecycleLocked", 409, "Archived documents are read-only.");
            if (!_tree.CanWritePageWithin(connection,transaction,actor,pageId)) return Failure<DocumentDetail>("LifecycleLocked",409,"The page container is readonly or unavailable.");
            var snapshot = ReadVersionSnapshot(connection, transaction, actor.OwnerId, pageId, command.VersionNumber);
            if (snapshot is null) return Failure<DocumentDetail>("ResourceUnavailable", 404, "Document version unavailable.");
            if (snapshot.Snapshot.EditorMode != current.EditorMode || snapshot.Snapshot.DocumentType != current.DocumentType)
                return Failure<DocumentDetail>("SnapshotIncompatible", 409, "A version cannot change the page's immutable format.");
            var validation = ValidateBody(snapshot.Body); if (validation is not null) return validation;
            if (current.EditorMode == "Block" && !DocumentBlockSchema.IsValid(snapshot.Body) && !DocumentBlockSchema.IsLegacyText(snapshot.Body))
                return Failure<DocumentDetail>("UnsupportedBlockSchema",422,"This snapshot requires a supported block schema before restoration.");
            if (command.Tag is null && !snapshot.Snapshot.TagSnapshotCaptured)
                return Failure<DocumentDetail>("TagRebindRequired", 409, "Choose a current Tag or explicitly clear it for this legacy snapshot.");
            var tagId = command.Tag is null ? snapshot.Snapshot.TagId : command.Tag.Id;
            var tagName = tagId is { } id ? ReadTagName(connection, transaction, actor.OwnerId, id) : null;
            if (tagId.HasValue && tagName is null) return Failure<DocumentDetail>("TagRebindRequired", 409, "Choose an available current Tag before restoring.");
            if (command.Tag is null && tagId.HasValue && tagName != snapshot.Snapshot.TagName)
                return Failure<DocumentDetail>("TagRebindRequired", 409, "The historical Tag label changed. Confirm the current Tag explicitly.");
            var next = checked(current.VersionNumber + 1);
            Execute(connection, transaction, """
                UPDATE [documents].[Page] SET [Title]=@Title,[Body]=@Body,[TagId]=@Tag,[VersionNumber]=@Next,
                    [UpdatedAt]=SYSUTCDATETIME(),[UpdatedByUserId]=@User
                WHERE [Id]=@Id AND [OwnerId]=@Owner AND [RowVersion]=@ETag AND [DeletedAt] IS NULL;
                IF @@ROWCOUNT <> 1 THROW 51060, 'Document revision unavailable.', 1;
                INSERT INTO [documents].[PageVersion] ([PageId],[OwnerId],[VersionNumber],[Title],[Body],[DocumentType],[EditorMode],
                    [Status],[ChangeNote],[CreatedByUserId],[TagIdSnapshot],[TagNameSnapshot],[TagSnapshotCaptured],[SourceVersionNumber])
                VALUES (@Id,@Owner,@Next,@Title,@Body,@Type,@Mode,@State,@Note,@User,@Tag,@TagName,1,@Source);
                """, ("@Title", SqlDbType.NVarChar, snapshot.Snapshot.Title), ("@Body", SqlDbType.NVarChar, snapshot.Body),
                ("@Tag", SqlDbType.UniqueIdentifier, (object?)tagId ?? DBNull.Value), ("@TagName", SqlDbType.NVarChar, (object?)tagName ?? DBNull.Value),
                ("@Next", SqlDbType.BigInt, next), ("@User", SqlDbType.UniqueIdentifier, actor.UserId), ("@Id", SqlDbType.UniqueIdentifier, pageId),
                ("@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId), ("@ETag", SqlDbType.Binary, expected),
                ("@Type", SqlDbType.VarChar, current.DocumentType), ("@Mode", SqlDbType.VarChar, current.EditorMode),
                ("@State", SqlDbType.VarChar, current.Status), ("@Note", SqlDbType.NVarChar, (object?)TrimOrNull(command.ChangeNote, 500) ?? DBNull.Value),
                ("@Source", SqlDbType.BigInt, command.VersionNumber));
            WriteAudit(connection, transaction, actor, pageId, RestoreVersionAction, traceId);
            TouchPageRegistryWithin(connection, transaction, actor, pageId, "Active");
            _receipts.Complete(connection, transaction, receipt, "DocumentVersionRestored", 200,
                JsonSerializer.Serialize(new RestoreReceipt(1, pageId, actor.UserId, actor.OwnerId, command.VersionNumber)));
            if (!VersionAuthority(connection, transaction, actor, RestoreVersionActions)) return ModuleUnavailable<DocumentDetail>();
            var detail = VersionAuthority(connection, transaction, actor, "documents.page.read")
                ? ReadDetail(connection, transaction, actor.OwnerId, pageId, false) : null;
            var readable = detail is not null && VersionAuthority(connection, transaction, actor, "documents.page.read");
            if (!VersionAuthority(connection, transaction, actor, RestoreVersionActions)) return ModuleUnavailable<DocumentDetail>();
            transaction.Commit();
            return readable ? IdentityOperationResult<DocumentDetail>.Success(detail!, 200, "DocumentVersionRestored")
                : IdentityOperationResult<DocumentDetail>.Committed(pageId, 200, "DocumentVersionRestored");
        }
        catch (SqlException exception) { return PersistenceFailure<DocumentDetail>(exception); }
    }

    private sealed record RestoreReceipt(int Version, Guid Id, Guid UserId, Guid OwnerId, long SourceVersion);
}
