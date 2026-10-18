using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService
{
    private sealed record Receipt(int Version,Guid ActorId,Guid OwnerId,Guid RootId,string Kind,string Operation,string[] Actions,int Status);
    private static IReadOnlyList<Node> Effects(string operation,Graph graph)=>operation switch
    {
        "archive"=>graph.Nodes.Where(node=>node.Kind=="Page"&&node.State is "Draft" or "Published").ToArray(),
        "unarchive"=>graph.Root.ArchiveBatchId is {} batch?graph.Nodes.Where(node=>node.Kind=="Page"&&node.State=="Archived"&&node.ArchiveBatchId==batch).ToArray():[graph.Root],
        _=>graph.Nodes
    };
    private bool ParentsReady(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph,string operation)
    {
        if(operation=="purge")return true;var ids=graph.Nodes.Select(node=>node.Id).ToHashSet();
        bool ParentReady(Node node,int remaining)
        {
            if(node.ParentId is not {} parentId||ids.Contains(parentId))return true;if(remaining==0)return false;
            var parentKind=node.Kind=="Page"&&node.AbsoluteDepth==2?"Page":"Folder";var parent=Root(c,tx,actor,parentKind,parentId);
            return parent is not null&&!parent.DeletedAt.HasValue
                &&(!(operation is "restore" or "unarchive" or "write")||parent.Kind!="Page"||parent.State!="Archived")&&ParentReady(parent,remaining-1);
        }
        return graph.Nodes.All(node=>ParentReady(node,4));
    }
    private IdentityOperationResult<object?>? Guard(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph,string operation)
    {
        if(operation=="archive"&&graph.Root.State is not("Draft" or "Published"))return Fail<object?>("LifecycleLocked",409);
        if(operation=="unarchive"&&(graph.Root.State!="Archived"||graph.Root.PreviousState is not("Draft" or "Published")))return Fail<object?>("CohortUnavailable",409);
        if(!ParentsReady(c,tx,actor,graph,operation))return Fail<object?>("LifecycleLocked",409);
        return null;
    }
    public IdentityOperationResult<object?> Execute(IdentityPrincipal actor,string kind,Guid id,string operation,string? etag,DocumentTreeCommand command,string key,string? trace)
    {
        var action=Action(kind,operation);if(action is null||id==Guid.Empty||!Guid.TryParseExact(key,"D",out var uuid)||uuid==Guid.Empty)return Fail<object?>("ValidationFailed",422);
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);if(!Allowed(c,tx,actor,action))return Denied<object?>();SqlDocumentScopeLock.Acquire(c,tx,actor.OwnerId);
        var status=operation=="purge"?204:200;var canonical=JsonSerializer.Serialize(new{version=1,actor.OwnerId,kind,id,operation,etag,command});
        var claim=receipts.TryClaim(c,tx,actor.UserId,$"documents.tree.{kind.ToLowerInvariant()}.{operation}:v1",key,canonical,DateTime.UtcNow);
        if(claim.IsConflict)return Fail<object?>("IdempotencyConflict",409);
        if(claim.IsReplay)
        {
            Receipt? saved=null;try{if(claim.ResultJson is not null)saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson);}catch(JsonException){}
            var additional=kind=="Folder"?Action("Page",operation):null;
            if(saved is null||saved.Version!=1||saved.ActorId!=actor.UserId||saved.OwnerId!=actor.OwnerId||saved.RootId!=id||saved.Kind!=kind||saved.Operation!=operation
                ||saved.Status!=status||claim.ResultStatusCode!=status||claim.ResultCode!="DocumentTreeCommitted"||saved.Actions is null||!saved.Actions.Contains(action,StringComparer.Ordinal)
                ||saved.Actions.Distinct().Count()!=saved.Actions.Length||saved.Actions.Any(required=>required!=action&&required!=additional))return Fail<object?>("ReceiptUnavailable",503);
            if(!Allowed(c,tx,actor,saved.Actions))return Denied<object?>();
            return status==204?IdentityOperationResult<object?>.Success(null,204,"DocumentTreeCommitted"):IdentityOperationResult<object?>.Committed(id,200,"DocumentTreeCommitted");
        }
        if(!claim.IsClaimed)return Fail<object?>("RequestInProgress",409);
        var result=ReadGraph(c,tx,actor,kind,id,operation);if(!result.Succeeded||result.Value is null)return Fail<object?>(result.Code,result.StatusCode);var graph=result.Value;
        if(string.IsNullOrEmpty(etag))return Fail<object?>("PreconditionRequired",428);if(etag!=graph.Root.ETag)return Fail<object?>("RevisionConflict",412);
        if(!command.Confirm||(operation=="purge"&&command.Confirmation!="PURGE"))return Fail<object?>("ConfirmationRequired",422);
        if(command.PreviewToken!=Token(actor,operation,graph))return Fail<object?>("PreviewChanged",409);
        var actions=Actions(operation,graph);if(!Allowed(c,tx,actor,actions))return Denied<object?>();var guard=Guard(c,tx,actor,graph,operation);if(guard is not null)return guard;
        var effect=Apply(c,tx,actor,graph,operation);if(effect is not null)return effect;
        var affected=Effects(operation,graph);Audit(c,tx,actor,affected,operation,trace);
        receipts.Complete(c,tx,claim,"DocumentTreeCommitted",status,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,id,kind,operation,actions,status)));
        if(!Allowed(c,tx,actor,actions))return Denied<object?>();tx.Commit();
        return status==204?IdentityOperationResult<object?>.Success(null,204,"DocumentTreeCommitted"):IdentityOperationResult<object?>.Committed(id,200,"DocumentTreeCommitted");
    }
    internal IdentityOperationResult<object?>? TransitionWithin(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id,string target)
    {
        var root=Root(c,tx,actor,"Page",id);if(root is null)return Fail<object?>("ResourceUnavailable",404);
        if(root.State==target)return null;
        var operation=target=="Archived"?"archive":root.State=="Archived"?"unarchive":"write";
        if(operation=="write")return CanWritePageWithin(c,tx,actor,id)?null:Fail<object?>("LifecycleLocked",409);
        var result=ReadGraph(c,tx,actor,"Page",id,operation);if(!result.Succeeded||result.Value is null)return Fail<object?>(result.Code,result.StatusCode);var graph=result.Value;
        var guard=Guard(c,tx,actor,graph,operation);if(guard is not null)return guard;
        if(operation=="unarchive"&&graph.Root.PreviousState!=target)return Fail<object?>("LifecycleLocked",409);
        if(Effects(operation,graph).Count>1)return Fail<object?>("PreviewRequired",409);
        return Apply(c,tx,actor,graph,operation);
    }
    internal bool CanWritePageWithin(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Guid id)
    {
        var root=Root(c,tx,actor,"Page",id);return root is not null&&!root.DeletedAt.HasValue&&(root.State is "Draft" or "Published")
            &&ParentsReady(c,tx,actor,new(root,[root],null,[]),"write");
    }
    private static string Ids(IEnumerable<Node> nodes)=>JsonSerializer.Serialize(nodes.Select(node=>node.Id));
    private static string Rows(IEnumerable<Node> nodes)=>JsonSerializer.Serialize(nodes.Select(node=>new{id=node.Id,kind=node.Kind,parent=node.ParentId,state=node.State,depth=node.ScopeDepth}));
    private static void Audit(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,IReadOnlyList<Node> nodes,string operation,string? trace)
    {
        using var sql=Command(c,tx,actor.OwnerId,"""
            INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
            SELECT @User,@User,CASE WHEN Kind='Folder' THEN 'documents.folder.' ELSE 'documents.page.' END+@Operation,
                CASE WHEN Kind='Folder' THEN 'DocumentFolder' ELSE 'DocumentPage' END,Id,'Succeeded',@Trace
            FROM OPENJSON(@Rows) WITH(Id uniqueidentifier '$.id',Kind varchar(16) '$.kind');
            """);Add(sql,"@Rows",SqlDbType.NVarChar,Rows(nodes),-1);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(sql,"@Operation",SqlDbType.VarChar,operation,32);Add(sql,"@Trace",SqlDbType.NVarChar,trace,100);sql.ExecuteNonQuery();
    }
    private static void Registry(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,IReadOnlyList<Node> nodes,string state)
    {
        using var sql=Command(c,tx,actor.OwnerId,"""
            UPDATE r SET Availability=CASE WHEN @State='Restored' THEN CASE WHEN p.Status='Archived' THEN 'Archived' ELSE 'Active' END ELSE @State END,
                Revision=Revision+1,PurgedAt=CASE WHEN @State='Purged' THEN SYSUTCDATETIME() ELSE NULL END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
            FROM [platform].[Resource] r LEFT JOIN [documents].[Page] p ON p.OwnerId=r.OwnerId AND p.Id=r.Id
            WHERE r.OwnerId=@Owner AND r.Id IN(SELECT Id FROM OPENJSON(@Ids)WITH(Id uniqueidentifier '$'));
            IF @@ROWCOUNT<>@Count THROW 51070,'Documents registry integrity is unavailable.',1;
            """);Add(sql,"@Ids",SqlDbType.NVarChar,Ids(nodes),-1);Add(sql,"@State",SqlDbType.VarChar,state,32);Add(sql,"@Count",SqlDbType.Int,nodes.Count);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();
    }
    private IdentityOperationResult<object?>? Apply(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,Graph graph,string operation)=>operation switch
    {
        "archive"=>Archive(c,tx,actor,graph),"unarchive"=>Unarchive(c,tx,actor,graph),"trash"=>Trash(c,tx,actor,graph),
        "restore"=>Restore(c,tx,actor,graph),"purge"=>Purge(c,tx,actor,graph),_=>Fail<object?>("ValidationFailed",422)
    };
}
