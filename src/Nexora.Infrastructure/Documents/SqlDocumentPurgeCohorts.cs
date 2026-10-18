using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Files;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService
{
    private bool Pinned(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        var pages=graph.Nodes.Where(node=>node.Kind=="Page").ToArray();var folders=graph.Nodes.Where(node=>node.Kind=="Folder").ToArray();
        using var sql=Command(c,tx,actor.OwnerId,"""
            SELECT CASE WHEN EXISTS(SELECT 1 FROM [documents].[Page] p WHERE p.OwnerId=@Owner AND p.Id NOT IN(SELECT Id FROM OPENJSON(@All)WITH(Id uniqueidentifier '$'))
                AND (p.ParentPageId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$')) OR p.FolderId IN(SELECT Id FROM OPENJSON(@Folders)WITH(Id uniqueidentifier '$'))
                  OR p.ArchiveBatchId IN(SELECT Id FROM [documents].[ArchiveBatch] WHERE OwnerId=@Owner AND RootPageId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$')))))
              OR EXISTS(SELECT 1 FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND f.Id NOT IN(SELECT Id FROM OPENJSON(@All)WITH(Id uniqueidentifier '$'))
                AND f.ParentFolderId IN(SELECT Id FROM OPENJSON(@Folders)WITH(Id uniqueidentifier '$')))
              OR EXISTS(SELECT 1 FROM [platform].[ResourceLink] WITH(HOLDLOCK) WHERE OwnerId=@Owner AND State<>'Detached'
                AND (SourceResourceId IN(SELECT Id FROM OPENJSON(@All)WITH(Id uniqueidentifier '$')) OR TargetResourceId IN(SELECT Id FROM OPENJSON(@All)WITH(Id uniqueidentifier '$'))))
              THEN 1 ELSE 0 END;
            """);Add(sql,"@All",SqlDbType.NVarChar,Ids(graph.Nodes),-1);Add(sql,"@Pages",SqlDbType.NVarChar,Ids(pages),-1);Add(sql,"@Folders",SqlDbType.NVarChar,Ids(folders),-1);
        return Convert.ToInt32(sql.ExecuteScalar())==1||(pages.Length>0&&SqlDocumentFilePins.HasPinsWithin(c,tx,actor.OwnerId,pages.Select(node=>node.Id).ToArray()));
    }
    private IdentityOperationResult<object?>? Purge(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        if(graph.TrashBatchId is not {} batch)return Fail<object?>("CohortUnavailable",409);if(Pinned(c,tx,actor,graph))return Fail<object?>("ResourcePinned",409);
        var pages=graph.Nodes.Where(node=>node.Kind=="Page").ToArray();var folders=graph.Nodes.Where(node=>node.Kind=="Folder").ToArray();
        using var sql=Command(c,tx,actor.OwnerId,"""
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            UPDATE [documents].[Page] SET ArchiveBatchId=NULL WHERE OwnerId=@Owner AND Id IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'));
            DELETE [documents].[PageVersion] WHERE OwnerId=@Owner AND PageId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'));
            DELETE [documents].[ArchiveMember] WHERE OwnerId=@Owner AND PageId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'));
            DELETE [documents].[ArchiveBatch] WHERE OwnerId=@Owner AND RootPageId IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'));
            DELETE [documents].[Page] WHERE OwnerId=@Owner AND TrashBatchId=@Batch AND DeletedAt IS NOT NULL AND Id IN(SELECT Id FROM OPENJSON(@Pages)WITH(Id uniqueidentifier '$'));
            IF @@ROWCOUNT<>@PageCount THROW 51070,'The Documents page purge scope changed.',1;
            DELETE [documents].[Folder] WHERE OwnerId=@Owner AND TrashBatchId=@Batch AND DeletedAt IS NOT NULL AND Id IN(SELECT Id FROM OPENJSON(@Folders)WITH(Id uniqueidentifier '$'));
            IF @@ROWCOUNT<>@FolderCount THROW 51070,'The Documents folder purge scope changed.',1;
            UPDATE [operations].[TrashMember] SET PurgedAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND BatchId=@Batch
              AND ResourceId IN(SELECT Id FROM OPENJSON(@All)WITH(Id uniqueidentifier '$')) AND PurgedAt IS NULL;
            IF @@ROWCOUNT<>@Count THROW 51070,'The Documents purge cohort changed.',1;
            UPDATE [operations].[TrashBatch] SET State='Purged',RestoredAt=NULL,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Batch AND RootResourceId=@Root AND State='Trashed';
            IF @@ROWCOUNT<>1 THROW 51070,'The Documents purge root changed.',1;
            """);Add(sql,"@Pages",SqlDbType.NVarChar,Ids(pages),-1);Add(sql,"@Folders",SqlDbType.NVarChar,Ids(folders),-1);Add(sql,"@All",SqlDbType.NVarChar,Ids(graph.Nodes),-1);
        Add(sql,"@PageCount",SqlDbType.Int,pages.Length);Add(sql,"@FolderCount",SqlDbType.Int,folders.Length);Add(sql,"@Count",SqlDbType.Int,graph.Nodes.Count);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(sql,"@Root",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
        Registry(c,tx,actor,graph.Nodes,"Purged");return null;
    }
}
