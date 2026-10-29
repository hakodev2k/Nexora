using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService
{
    private IdentityOperationResult<object?>? Trash(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        var batch=Guid.NewGuid();using var sql=Command(c,tx,actor.OwnerId,"""
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
              VALUES(@Batch,@Owner,@Root,@Now,'Trashed',@Now,@User,@User);
            INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,ParentResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
              SELECT NEWID(),@Owner,@Batch,Id,ParentId,State,Depth,@Now,@User,@User
              FROM OPENJSON(@Rows)WITH(Id uniqueidentifier '$.id',ParentId uniqueidentifier '$.parent',State varchar(64) '$.state',Depth int '$.depth');
            IF @@ROWCOUNT<>@Count THROW 51070,'The Documents Trash scope changed.',1;
            UPDATE p SET DeletedAt=@Now,TrashBatchId=@Batch,UpdatedAt=@Now,UpdatedByUserId=@User FROM [documents].[Page] p
              WHERE p.OwnerId=@Owner AND p.DeletedAt IS NULL AND p.Id IN(SELECT Id FROM OPENJSON(@Rows)WITH(Id uniqueidentifier '$.id',Kind varchar(16) '$.kind')WHERE Kind='Page');
            IF @@ROWCOUNT<>@PageCount THROW 51070,'The Documents page Trash scope changed.',1;
            UPDATE f SET DeletedAt=@Now,TrashBatchId=@Batch,UpdatedAt=@Now,UpdatedByUserId=@User FROM [documents].[Folder] f
              WHERE f.OwnerId=@Owner AND f.DeletedAt IS NULL AND f.Id IN(SELECT Id FROM OPENJSON(@Rows)WITH(Id uniqueidentifier '$.id',Kind varchar(16) '$.kind')WHERE Kind='Folder');
            IF @@ROWCOUNT<>@FolderCount THROW 51070,'The Documents folder Trash scope changed.',1;
            """);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(sql,"@Root",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(sql,"@Rows",SqlDbType.NVarChar,Rows(graph.Nodes),-1);
        Add(sql,"@Count",SqlDbType.Int,graph.Nodes.Count);Add(sql,"@PageCount",SqlDbType.Int,graph.Nodes.Count(node=>node.Kind=="Page"));Add(sql,"@FolderCount",SqlDbType.Int,graph.Nodes.Count(node=>node.Kind=="Folder"));Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
        Registry(c,tx,actor,graph.Nodes,"Trash");return null;
    }
    private IdentityOperationResult<object?>? Restore(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        if(graph.TrashBatchId is not {} batch)return Fail<object?>("CohortUnavailable",409);
        using var sql=Command(c,tx,actor.OwnerId,"""
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            UPDATE p SET DeletedAt=NULL,TrashBatchId=NULL,UpdatedAt=@Now,UpdatedByUserId=@User
              FROM [documents].[Page] p WHERE p.OwnerId=@Owner AND p.DeletedAt IS NOT NULL AND p.TrashBatchId=@Batch
                AND p.Id IN(SELECT Id FROM OPENJSON(@Rows)WITH(Id uniqueidentifier '$.id',Kind varchar(16) '$.kind')WHERE Kind='Page');
            IF @@ROWCOUNT<>@PageCount THROW 51070,'The Documents page restore scope changed.',1;
            UPDATE f SET DeletedAt=NULL,TrashBatchId=NULL,UpdatedAt=@Now,UpdatedByUserId=@User
              FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND f.DeletedAt IS NOT NULL AND f.TrashBatchId=@Batch
                AND f.Id IN(SELECT Id FROM OPENJSON(@Rows)WITH(Id uniqueidentifier '$.id',Kind varchar(16) '$.kind')WHERE Kind='Folder');
            IF @@ROWCOUNT<>@FolderCount THROW 51070,'The Documents folder restore scope changed.',1;
            UPDATE [operations].[TrashBatch] SET State='Restored',RestoredAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User
              WHERE OwnerId=@Owner AND Id=@Batch AND RootResourceId=@Root AND State='Trashed';
            IF @@ROWCOUNT<>1 THROW 51070,'The Documents restore cohort changed.',1;
            """);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(sql,"@Root",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(sql,"@Rows",SqlDbType.NVarChar,Rows(graph.Nodes),-1);
        Add(sql,"@PageCount",SqlDbType.Int,graph.Nodes.Count(node=>node.Kind=="Page"));Add(sql,"@FolderCount",SqlDbType.Int,graph.Nodes.Count(node=>node.Kind=="Folder"));Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
        // The existing Sharing-owned invalidation trigger has already revoked
        // source-deleted links. Restoration deliberately never changes them.
        Registry(c,tx,actor,graph.Nodes,"Restored");return null;
    }
}
