using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentService
{
    private const string HistoryAction = "documents.page.history";
    private const int HistoryTake = 25;
    private const string VersionColumns = "[Id], [PageId], [VersionNumber], [Title], [DocumentType], [EditorMode], [Status], [ChangeNote], [CreatedAt], [CreatedByUserId], [TagSnapshotCaptured], [TagIdSnapshot], [TagNameSnapshot]";

    public IdentityOperationResult<DocumentVersionPage> History(IdentityPrincipal actor, Guid documentId, string? cursor = null)
    {
        if (!ModuleAvailable(actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPage>();
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPage>();
        var context = new DocumentHistoryCursor.Context(actor.OwnerId, documentId, HistoryTake);
        long before = 0;
        if (!string.IsNullOrEmpty(cursor) && !_historyCursor.TryDecode(cursor, context, out before))
            return Failure<DocumentVersionPage>("InvalidCursor", 422, "History cursor does not match this page.");
        var exists = HistoryPageExists(connection, transaction, actor.OwnerId, documentId);
        var items = new List<DocumentVersionSummary>();
        if (exists)
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"SELECT TOP (@Take) {VersionColumns} FROM [documents].[PageVersion] WHERE [OwnerId]=@Owner AND [PageId]=@Page AND (@Before=0 OR [VersionNumber]<@Before) ORDER BY [VersionNumber] DESC;";
            Add(command, "@Take", SqlDbType.Int, HistoryTake + 1);
            Add(command, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@Page", SqlDbType.UniqueIdentifier, documentId);
            Add(command, "@Before", SqlDbType.BigInt, before);
            using var reader = command.ExecuteReader();
            while (reader.Read()) items.Add(ReadVersion(reader));
        }
        var hasMore = items.Count > HistoryTake;
        if (hasMore) items.RemoveAt(items.Count - 1);
        var next = hasMore ? _historyCursor.Encode(context, items[^1].VersionNumber) : null;
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPage>();
        transaction.Commit();
        return exists ? IdentityOperationResult<DocumentVersionPage>.Success(new(items, next))
            : Failure<DocumentVersionPage>("ResourceUnavailable", 404, "Document history unavailable.");
    }

    public IdentityOperationResult<DocumentVersionPreview> Version(IdentityPrincipal actor, Guid documentId, long versionNumber)
    {
        if (!ModuleAvailable(actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPreview>();
        if (versionNumber <= 0) return Failure<DocumentVersionPreview>("ValidationFailed", 422, "Version number must be positive.");
        using var connection = _connections.Create(); connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPreview>();
        DocumentVersionPreview? result = null;
        if (HistoryPageExists(connection, transaction, actor.OwnerId, documentId))
        {
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = $"SELECT {VersionColumns}, [Body] FROM [documents].[PageVersion] WHERE [OwnerId]=@Owner AND [PageId]=@Page AND [VersionNumber]=@Version;";
            Add(command, "@Owner", SqlDbType.UniqueIdentifier, actor.OwnerId);
            Add(command, "@Page", SqlDbType.UniqueIdentifier, documentId);
            Add(command, "@Version", SqlDbType.BigInt, versionNumber);
            using var reader = command.ExecuteReader();
            if (reader.Read()) result = new(ReadVersion(reader), reader.GetString(13));
        }
        if (!_capabilities.IsAllowed(connection, transaction, actor, "FX20", HistoryAction)) return ModuleUnavailable<DocumentVersionPreview>();
        transaction.Commit();
        return result is not null ? IdentityOperationResult<DocumentVersionPreview>.Success(result)
            : Failure<DocumentVersionPreview>("ResourceUnavailable", 404, "Document history unavailable.");
    }

    private static bool HistoryPageExists(SqlConnection connection, SqlTransaction transaction, Guid owner, Guid page)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = """
            SELECT 1 FROM [documents].[Page] p WHERE p.[OwnerId]=@Owner AND p.[Id]=@Page
              AND p.[DeletedAt] IS NULL AND p.[Status] IN ('Draft','Published','Archived')
              AND NOT EXISTS (SELECT 1 FROM [platform].[TrashItem] tr WHERE tr.[OwnerId]=p.[OwnerId]
                  AND tr.[ResourceType]='Document' AND tr.[ResourceId]=p.[Id] AND tr.[RestoredAt] IS NULL AND tr.[PurgedAt] IS NULL);
            """;
        Add(command, "@Owner", SqlDbType.UniqueIdentifier, owner);
        Add(command, "@Page", SqlDbType.UniqueIdentifier, page);
        return command.ExecuteScalar() is not null;
    }

    private static DocumentVersionSummary ReadVersion(SqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetInt64(2), reader.GetString(3), reader.GetString(4), reader.GetString(5),
        reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetString(7), ToOffset(reader.GetDateTime(8)),
        reader.IsDBNull(9) ? null : reader.GetGuid(9), reader.GetBoolean(10),
        reader.IsDBNull(11) ? null : reader.GetGuid(11), reader.IsDBNull(12) ? null : reader.GetString(12));
}
