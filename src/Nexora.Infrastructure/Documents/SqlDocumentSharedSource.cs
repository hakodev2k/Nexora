using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Sharing;
using Nexora.Infrastructure.Sharing;

namespace Nexora.Infrastructure.Documents;

internal static class SqlDocumentSharedSource
{
    internal static (Guid OwnerId,string Status)? ReadState(SqlConnection connection, SqlTransaction? transaction, Guid ownerId, Guid resourceId)
    {
        using var command=connection.CreateCommand(); command.Transaction=transaction;
        command.CommandText="SELECT [OwnerId], CASE WHEN [DeletedAt] IS NOT NULL THEN 'Deleted' ELSE [Status] END FROM [documents].[Page] WITH(HOLDLOCK) WHERE [Id]=@Id AND [OwnerId]=@OwnerId;";
        Add(command,"@Id",SqlDbType.UniqueIdentifier,resourceId); Add(command,"@OwnerId",SqlDbType.UniqueIdentifier,ownerId);
        using var reader=command.ExecuteReader(); return reader.Read()?(reader.GetGuid(0),reader.GetString(1)):null;
    }
    internal static SharedDocumentProjection? ReadProjection(SqlConnection connection, SqlTransaction transaction, SharedLinkSourceContext source)
    {
        var ownerId=source.OwnerId; var documentId=source.ResourceId;
        using var command = connection.CreateCommand();
        command.Transaction=transaction;
        command.CommandText = """
            SELECT [Id], [Title], [DocumentType], [EditorMode], [Body], [Status], [VersionNumber], [UpdatedAt]
            FROM [documents].[Page]
            WHERE [Id] = @Id AND [OwnerId] = @OwnerId AND [DeletedAt] IS NULL AND [Status] IN ('Published','Archived');
            """;
        Add(command, "@Id", SqlDbType.UniqueIdentifier, documentId);
        Add(command, "@OwnerId", SqlDbType.UniqueIdentifier, ownerId);
        using var reader = command.ExecuteReader();
        return !reader.Read()
            ? null
            : new SharedDocumentProjection(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetInt64(6), ToOffset(reader.GetDateTime(7)));
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static void Add(SqlCommand command, string name, SqlDbType type, object value) => command.Parameters.Add(name,type).Value=value;
}
