using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService
{
    private IdentityOperationResult<object?>? Archive(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        var affected=Effects("archive",graph);if(affected.Count==0)return Fail<object?>("LifecycleLocked",409);var batch=Guid.NewGuid();
        using var sql=Command(c,tx,actor.OwnerId,"""
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            INSERT [documents].[ArchiveBatch](Id,OwnerId,RootPageId,ArchivedAt,CreatedByUserId,UpdatedByUserId)VALUES(@Batch,@Owner,@Root,@Now,@User,@User);
            INSERT [documents].[ArchiveMember](Id,OwnerId,BatchId,PageId,PreviousStatus,CreatedByUserId,UpdatedByUserId)
              SELECT NEWID(),@Owner,@Batch,p.Id,p.Status,@User,@User FROM [documents].[Page] p
              WHERE p.OwnerId=@Owner AND p.Id IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$')) AND p.DeletedAt IS NULL AND p.Status IN('Draft','Published');
            IF @@ROWCOUNT<>@Count THROW 51070,'The Documents archive scope changed.',1;
            UPDATE p SET PreArchiveStatus=p.Status,Status='Archived',ArchiveBatchId=@Batch,UpdatedAt=@Now,UpdatedByUserId=@User
              FROM [documents].[Page] p WHERE p.OwnerId=@Owner AND p.Id IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$')) AND p.DeletedAt IS NULL AND p.Status IN('Draft','Published');
            IF @@ROWCOUNT<>@Count THROW 51070,'The Documents archive scope changed.',1;
            """);Add(sql,"@Ids",SqlDbType.NVarChar,Ids(affected),-1);Add(sql,"@Count",SqlDbType.Int,affected.Count);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(sql,"@Root",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
        Registry(c,tx,actor,affected,"Archived");return null;
    }
    private IdentityOperationResult<object?>? Unarchive(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph)
    {
        var affected=Effects("unarchive",graph);if(affected.Count==0)return Fail<object?>("CohortUnavailable",409);
        if(graph.Root.ArchiveBatchId is not {} batch)
        {
            // Legacy flat archives predate cohort storage. Their explicit prior
            // state is authoritative; no former membership is synthesized.
            if(graph.Nodes.Count!=1||graph.Root.PreviousState is not("Draft" or "Published"))return Fail<object?>("CohortUnavailable",409);
            using var legacy=Command(c,tx,actor.OwnerId,"UPDATE [documents].[Page] SET Status=PreArchiveStatus,PreArchiveStatus=NULL,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Status='Archived' AND DeletedAt IS NULL AND PreArchiveStatus IN('Draft','Published');IF @@ROWCOUNT<>1 THROW 51070,'The Documents archive state changed.',1;");
            Add(legacy,"@Id",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(legacy,"@User",SqlDbType.UniqueIdentifier,actor.UserId);legacy.ExecuteNonQuery();Registry(c,tx,actor,affected,"Active");return null;
        }
        using(var verify=Command(c,tx,actor.OwnerId,"""
            SELECT COUNT(*) FROM [documents].[ArchiveMember] m JOIN [documents].[ArchiveBatch] b ON b.OwnerId=m.OwnerId AND b.Id=m.BatchId
              JOIN [documents].[Page] p ON p.OwnerId=m.OwnerId AND p.Id=m.PageId
            WHERE m.OwnerId=@Owner AND m.BatchId=@Batch AND m.RestoredAt IS NULL AND p.Status='Archived' AND p.DeletedAt IS NULL
              AND p.ArchiveBatchId=@Batch AND p.PreArchiveStatus=m.PreviousStatus AND p.Id IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$'));
            """))
        {Add(verify,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(verify,"@Ids",SqlDbType.NVarChar,Ids(affected),-1);if(Convert.ToInt32(verify.ExecuteScalar())!=affected.Count)return Fail<object?>("CohortUnavailable",409);}
        using var sql=Command(c,tx,actor.OwnerId,"""
            DECLARE @Now datetime2(7)=SYSUTCDATETIME();
            UPDATE p SET Status=m.PreviousStatus,PreArchiveStatus=NULL,ArchiveBatchId=NULL,UpdatedAt=@Now,UpdatedByUserId=@User
              FROM [documents].[Page] p JOIN [documents].[ArchiveMember] m ON m.OwnerId=p.OwnerId AND m.PageId=p.Id
              WHERE p.OwnerId=@Owner AND m.BatchId=@Batch AND m.RestoredAt IS NULL AND p.ArchiveBatchId=@Batch AND p.Status='Archived' AND p.DeletedAt IS NULL
                AND p.PreArchiveStatus=m.PreviousStatus AND p.Id IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$'));
            IF @@ROWCOUNT<>@Count THROW 51070,'The Documents unarchive scope changed.',1;
            UPDATE [documents].[ArchiveMember] SET RestoredAt=@Now,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND BatchId=@Batch
              AND PageId IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$')) AND RestoredAt IS NULL;
            UPDATE [documents].[ArchiveBatch] SET UnarchivedAt=COALESCE(UnarchivedAt,@Now),UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Batch AND RootPageId=@Root;
            """);Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(sql,"@Ids",SqlDbType.NVarChar,Ids(affected),-1);Add(sql,"@Count",SqlDbType.Int,affected.Count);Add(sql,"@Root",SqlDbType.UniqueIdentifier,graph.Root.Id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
        Registry(c,tx,actor,affected,"Active");return null;
    }
}
