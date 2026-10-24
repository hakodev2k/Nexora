using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Files;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService : IDocumentTreeService
{
    private const int MaxMembers=5000;
    private readonly SqlConnectionFactory connections;private readonly SqlSelfCapability capabilities;private readonly SqlRequestReceiptStore receipts;
    private readonly DocumentListCursor cursorCodec;
    public SqlDocumentTreeService(SqlConnectionFactory connections,string secret){this.connections=connections;capabilities=new(connections);receipts=new(secret);cursorCodec=new(secret);}
    private sealed record Node(Guid Id,string Kind,Guid? ParentId,Guid? FolderId,int AbsoluteDepth,string State,DateTime? DeletedAt,
        Guid? TrashBatchId,Guid? ArchiveBatchId,string? PreviousState,string ETag,string? Title,string? DocumentType,int ScopeDepth=0);
    private sealed record Frozen(Guid ResourceId,Guid? ParentId,string PreviousState,int Depth,DateTime? PurgedAt,string ETag);
    private sealed record Graph(Node Root,IReadOnlyList<Node> Nodes,Guid? TrashBatchId,IReadOnlyList<Frozen> Frozen);
    private bool Allowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,params string[] actions)=>capabilities.IsAllowed(c,tx,actor,"FX20",actions);
    private static IdentityOperationResult<T> Fail<T>(string code,int status)=>IdentityOperationResult<T>.Failure(code,status,"The Documents tree is unavailable or its preview has changed.");
    private static IdentityOperationResult<T> Denied<T>()=>Fail<T>("ModuleUnavailable",403);
    private static string? Action(string kind,string operation)=>kind is not("Page" or "Folder")||operation is not("archive" or "unarchive" or "trash" or "restore" or "purge")
        ||(kind=="Folder"&&operation is "archive" or "unarchive")?null:$"documents.{kind.ToLowerInvariant()}.{operation}";
    private static SqlCommand Command(SqlConnection c,SqlTransaction tx,Guid owner,string text){var sql=c.CreateCommand();sql.Transaction=tx;sql.CommandText=text;Add(sql,"@Owner",SqlDbType.UniqueIdentifier,owner);return sql;}
    private static void Add(SqlCommand sql,string name,SqlDbType type,object? value,int size=0){var p=sql.Parameters.Add(name,type);if(size!=0)p.Size=size;p.Value=value??DBNull.Value;}
    private static string Tag(byte[] bytes)=>"\""+Convert.ToBase64String(bytes)+"\"";
    private static Guid? Id(SqlDataReader r,int index)=>r.IsDBNull(index)?null:r.GetGuid(index);
    private static string? Text(SqlDataReader r,int index)=>r.IsDBNull(index)?null:r.GetString(index);
    private static Node ReadNode(SqlDataReader r)=>new(r.GetGuid(0),r.GetString(1),Id(r,2),Id(r,3),r.GetInt32(4),r.GetString(5),r.IsDBNull(6)?null:r.GetDateTime(6),Id(r,7),Id(r,8),Text(r,9),Tag(r.GetFieldValue<byte[]>(10)),Text(r,11),Text(r,12));
    private const string PageColumns="p.Id,CONVERT(varchar(16),'Page'),COALESCE(p.ParentPageId,p.FolderId),p.FolderId,CONVERT(int,p.Depth),p.Status,COALESCE(p.DeletedAt,(SELECT TOP(1) tr.DeletedAt FROM [platform].[TrashItem] tr WHERE tr.OwnerId=p.OwnerId AND tr.ResourceType='Document' AND tr.ResourceId=p.Id AND tr.RestoredAt IS NULL AND tr.PurgedAt IS NULL)),p.TrashBatchId,p.ArchiveBatchId,p.PreArchiveStatus,p.RowVersion,CASE WHEN @ReadPage=1 THEN p.Title ELSE NULL END,CASE WHEN @ReadPage=1 THEN p.DocumentType ELSE NULL END";
    private const string FolderColumns="f.Id,CONVERT(varchar(16),'Folder'),f.ParentFolderId,CONVERT(uniqueidentifier,NULL),CONVERT(int,f.Depth),CONVERT(varchar(16),'Active'),f.DeletedAt,f.TrashBatchId,CONVERT(uniqueidentifier,NULL),CONVERT(varchar(16),NULL),f.RowVersion,CASE WHEN @ReadFolder=1 THEN f.Title ELSE NULL END,CONVERT(varchar(32),NULL)";
    private void ReadFlags(SqlCommand sql,SqlConnection c,SqlTransaction tx,IdentityPrincipal actor)
    {Add(sql,"@ReadPage",SqlDbType.Bit,Allowed(c,tx,actor,"documents.page.read"));Add(sql,"@ReadFolder",SqlDbType.Bit,Allowed(c,tx,actor,"documents.folder.read"));}
    private Node? Root(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,string kind,Guid id)
    {
        using var sql=Command(c,tx,actor.OwnerId,kind=="Page"?$"SELECT {PageColumns} FROM [documents].[Page] p WHERE p.OwnerId=@Owner AND p.Id=@Id;":$"SELECT {FolderColumns} FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND f.Id=@Id;");
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);ReadFlags(sql,c,tx,actor);using var r=sql.ExecuteReader();return r.Read()?ReadNode(r):null;
    }
    private IdentityOperationResult<Graph> ReadGraph(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,string kind,Guid id,string operation)
    {
        var root=Root(c,tx,actor,kind,id);if(root is null)return Fail<Graph>("ResourceUnavailable",404);
        if(operation is "restore" or "purge")return ReadFrozen(c,tx,actor,root);
        if(root.DeletedAt.HasValue)return Fail<Graph>("LifecycleLocked",409);
        var nodes=new List<Node>();
        using var sql=Command(c,tx,actor.OwnerId,kind=="Page"?$"""
            SELECT TOP({MaxMembers+1}) {PageColumns} FROM [documents].[Page] p
            WHERE p.OwnerId=@Owner AND (p.Id=@Id OR p.ParentPageId=@Id) AND p.DeletedAt IS NULL;
            """:$"""
            SELECT TOP({MaxMembers+1}) {FolderColumns} FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND (f.Id=@Id OR f.ParentFolderId=@Id) AND f.DeletedAt IS NULL
            UNION ALL SELECT TOP({MaxMembers+1}) {PageColumns} FROM [documents].[Page] p WHERE p.OwnerId=@Owner AND p.DeletedAt IS NULL AND p.FolderId IN(
                SELECT f.Id FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND (f.Id=@Id OR f.ParentFolderId=@Id) AND f.DeletedAt IS NULL);
            """);
        Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);ReadFlags(sql,c,tx,actor);using(var r=sql.ExecuteReader())while(r.Read())nodes.Add(ReadNode(r));
        nodes=nodes.Where(node=>!node.DeletedAt.HasValue).ToList();
        if(nodes.Count>MaxMembers)return Fail<Graph>("AggregateTooLarge",413);
        var byId=nodes.ToDictionary(node=>node.Id);
        int Depth(Node node,int remaining=4)=>node.Id==root.Id?0:remaining==0||node.ParentId is not {} parent||!byId.TryGetValue(parent,out var parentNode)?-10:1+Depth(parentNode,remaining-1);
        nodes=nodes.Select(node=>node with{ScopeDepth=Depth(node)}).ToList();if(nodes.Any(node=>node.ScopeDepth<0))return Fail<Graph>("TopologyUnavailable",409);
        return IdentityOperationResult<Graph>.Success(new(root,nodes,null,[]));
    }
    private IdentityOperationResult<Graph> ReadFrozen(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Node root)
    {
        if(!root.DeletedAt.HasValue||root.TrashBatchId is not {} batch)return Fail<Graph>("LifecycleLocked",409);
        using(var header=Command(c,tx,actor.OwnerId,"SELECT 1 FROM [operations].[TrashBatch] WHERE OwnerId=@Owner AND Id=@Batch AND RootResourceId=@Root AND State='Trashed';"))
        {Add(header,"@Batch",SqlDbType.UniqueIdentifier,batch);Add(header,"@Root",SqlDbType.UniqueIdentifier,root.Id);if(header.ExecuteScalar()is null)return Fail<Graph>("CohortUnavailable",409);}
        var frozen=new List<Frozen>();using(var members=Command(c,tx,actor.OwnerId,$"SELECT TOP({MaxMembers+1}) ResourceId,ParentResourceId,PreviousLifecycle,Depth,PurgedAt,RowVersion FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=@Batch;"))
        {Add(members,"@Batch",SqlDbType.UniqueIdentifier,batch);using var r=members.ExecuteReader();while(r.Read())frozen.Add(new(r.GetGuid(0),Id(r,1),r.GetString(2),r.GetInt32(3),r.IsDBNull(4)?null:r.GetDateTime(4),Tag(r.GetFieldValue<byte[]>(5))));}
        if(frozen.Count is 0 or >MaxMembers||!frozen.Any(member=>member.ResourceId==root.Id&&member.Depth==0&&!member.PurgedAt.HasValue))return Fail<Graph>("CohortUnavailable",409);
        var nodes=new List<Node>();using(var sql=Command(c,tx,actor.OwnerId,$"""
            SELECT {PageColumns} FROM [documents].[Page] p JOIN [operations].[TrashMember] m ON m.OwnerId=p.OwnerId AND m.ResourceId=p.Id
              WHERE p.OwnerId=@Owner AND m.BatchId=@Batch AND m.PurgedAt IS NULL
            UNION ALL SELECT {FolderColumns} FROM [documents].[Folder] f JOIN [operations].[TrashMember] m ON m.OwnerId=f.OwnerId AND m.ResourceId=f.Id
              WHERE f.OwnerId=@Owner AND m.BatchId=@Batch AND m.PurgedAt IS NULL;
            """))
        {Add(sql,"@Batch",SqlDbType.UniqueIdentifier,batch);ReadFlags(sql,c,tx,actor);using var r=sql.ExecuteReader();while(r.Read())nodes.Add(ReadNode(r));}
        if(nodes.Count!=frozen.Count(member=>!member.PurgedAt.HasValue)||nodes.Any(node=>node.TrashBatchId!=batch||!node.DeletedAt.HasValue))return Fail<Graph>("CohortUnavailable",409);
        var previous=frozen.ToDictionary(member=>member.ResourceId);nodes=nodes.Select(node=>node with{ScopeDepth=previous[node.Id].Depth}).ToList();
        if(nodes.Any(node=>node.ParentId!=previous[node.Id].ParentId||node.State!=previous[node.Id].PreviousState))return Fail<Graph>("CohortUnavailable",409);
        return IdentityOperationResult<Graph>.Success(new(root,nodes,batch,frozen));
    }
    private string Token(IdentityPrincipal actor,string operation,Graph graph)=>"dt1."+receipts.DigestSensitive(JsonSerializer.Serialize(new{version=1,actor.UserId,actor.OwnerId,graph.Root.Kind,rootId=graph.Root.Id,operation,
        nodes=graph.Nodes.OrderBy(node=>node.Id.ToString("N"),StringComparer.Ordinal).Select(node=>new{node.Id,node.Kind,node.ParentId,node.FolderId,node.AbsoluteDepth,node.ScopeDepth,node.State,node.DeletedAt,node.TrashBatchId,node.ArchiveBatchId,node.PreviousState,node.ETag}),
        frozen=graph.Frozen.OrderBy(member=>member.ResourceId.ToString("N"),StringComparer.Ordinal)}));
    private string[] Actions(string operation,Graph graph)=>graph.Root.Kind=="Folder"&&graph.Nodes.Any(node=>node.Kind=="Page")
        ?[Action("Folder",operation)!,Action("Page",operation)!]:[Action(graph.Root.Kind,operation)!];
    public IdentityOperationResult<DocumentTreePreview> Preview(IdentityPrincipal actor,string kind,Guid id,string operation)
    {
        var action=Action(kind,operation);if(action is null||id==Guid.Empty)return Fail<DocumentTreePreview>("ValidationFailed",422);
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);if(!Allowed(c,tx,actor,action))return Denied<DocumentTreePreview>();SqlDocumentScopeLock.Acquire(c,tx,actor.OwnerId);
        var result=ReadGraph(c,tx,actor,kind,id,operation);if(!result.Succeeded||result.Value is null)return Fail<DocumentTreePreview>(result.Code,result.StatusCode);var graph=result.Value;
        if(!Allowed(c,tx,actor,Actions(operation,graph)))return Denied<DocumentTreePreview>();
        var guard=Guard(c,tx,actor,graph,operation);if(guard is not null)return Fail<DocumentTreePreview>(guard.Code,guard.StatusCode);
        if(operation=="purge"&&Pinned(c,tx,actor,graph))return Fail<DocumentTreePreview>("ResourcePinned",409);
        var readable=Allowed(c,tx,actor,"documents.page.read")&&(kind!="Folder"||Allowed(c,tx,actor,"documents.folder.read"));
        var effects=Effects(operation,graph);
        var members=readable?effects.OrderBy(node=>node.ScopeDepth).ThenBy(node=>node.Id).Select(node=>new DocumentTreeMember(node.Id,node.Kind,node.Title,node.DocumentType,node.DeletedAt.HasValue?$"Trash (previous {node.State})":node.State,node.ScopeDepth)).ToArray():null;
        var preview=new DocumentTreePreview(id,kind,operation,graph.Root.ETag,Token(actor,operation,graph),!readable,readable?effects.Count:null,members);
        if(!Allowed(c,tx,actor,Actions(operation,graph)))return Denied<DocumentTreePreview>();
        if(readable&&(!Allowed(c,tx,actor,"documents.page.read")||(kind=="Folder"&&!Allowed(c,tx,actor,"documents.folder.read"))))preview=preview with{DetailsHidden=true,AffectedCount=null,Members=null};
        tx.Commit();return IdentityOperationResult<DocumentTreePreview>.Success(preview);
    }
}
