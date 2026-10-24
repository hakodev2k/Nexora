using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Documents;

public sealed class SqlDocumentFolderService : IDocumentFolderService
{
    private readonly SqlConnectionFactory connections;private readonly SqlSelfCapability capabilities;private readonly SqlRequestReceiptStore receipts;private readonly DocumentFolderCursor cursors;
    public SqlDocumentFolderService(SqlConnectionFactory connections,string secret){this.connections=connections;capabilities=new(connections);receipts=new(secret);cursors=new(secret);}
    private bool Allowed(SqlConnection c,SqlTransaction tx,IdentityPrincipal actor,params string[] actions)=>capabilities.IsAllowed(c,tx,actor,"FX20",actions);
    private static IdentityOperationResult<T> Fail<T>(string code,int status)=>IdentityOperationResult<T>.Failure(code,status,"The Documents folder is unavailable or its state has changed.");
    private static IdentityOperationResult<T> Denied<T>()=>Fail<T>("ModuleUnavailable",403);
    private static SqlCommand Command(SqlConnection c,SqlTransaction tx,Guid owner,string text){var sql=c.CreateCommand();sql.Transaction=tx;sql.CommandText=text;Add(sql,"@Owner",SqlDbType.UniqueIdentifier,owner);return sql;}
    private static void Add(SqlCommand sql,string name,SqlDbType type,object? value,int size=0){var p=sql.Parameters.Add(name,type);if(size!=0)p.Size=size;p.Value=value??DBNull.Value;}
    private const string Columns="f.Id,f.Title,f.ParentFolderId,f.Depth,f.CreatedAt,f.UpdatedAt,f.RowVersion";
    private const string Visible="f.DeletedAt IS NULL AND (f.ParentFolderId IS NULL OR EXISTS(SELECT 1 FROM [documents].[Folder] parent WHERE parent.OwnerId=f.OwnerId AND parent.Id=f.ParentFolderId AND parent.DeletedAt IS NULL))";
    private static DocumentFolder Row(SqlDataReader r)=>new(r.GetGuid(0),r.GetString(1),r.IsDBNull(2)?null:r.GetGuid(2),r.GetByte(3),new(DateTime.SpecifyKind(r.GetDateTime(4),DateTimeKind.Utc)),new(DateTime.SpecifyKind(r.GetDateTime(5),DateTimeKind.Utc)),"\""+Convert.ToBase64String(r.GetFieldValue<byte[]>(6))+"\"");
    internal static DocumentFolder? ReadWithin(SqlConnection c,SqlTransaction tx,Guid owner,Guid id)
    {
        using var sql=Command(c,tx,owner,$"SELECT {Columns} FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND f.Id=@Id AND {Visible};");Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);using var r=sql.ExecuteReader();return r.Read()?Row(r):null;
    }
    public IdentityOperationResult<DocumentFolders> ListFolders(IdentityPrincipal actor,Guid? parentFolderId,string? cursor)
    {
        if(parentFolderId==Guid.Empty)return Fail<DocumentFolders>("ValidationFailed",422);
        string? afterTitle=null;Guid? afterId=null;
        if(!string.IsNullOrEmpty(cursor)&&!cursors.TryDecode(cursor,actor.OwnerId,parentFolderId,out afterTitle,out afterId))return Fail<DocumentFolders>("InvalidCursor",422);
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        string[] actions=parentFolderId.HasValue?["documents.library.read","documents.folder.read"]:["documents.library.read"];
        if(!Allowed(c,tx,actor,actions))return Denied<DocumentFolders>();
        if(parentFolderId is {} parent&&ReadWithin(c,tx,actor.OwnerId,parent)is null)return Fail<DocumentFolders>("ResourceUnavailable",404);
        using var sql=Command(c,tx,actor.OwnerId,$"SELECT TOP(26) {Columns} FROM [documents].[Folder] f WHERE f.OwnerId=@Owner AND {Visible} AND ((@Parent IS NULL AND f.ParentFolderId IS NULL) OR f.ParentFolderId=@Parent) AND (@AfterId IS NULL OR f.Title>@AfterTitle OR(f.Title=@AfterTitle AND f.Id>@AfterId)) ORDER BY f.Title,f.Id;");
        Add(sql,"@Parent",SqlDbType.UniqueIdentifier,parentFolderId);Add(sql,"@AfterTitle",SqlDbType.NVarChar,afterTitle,200);Add(sql,"@AfterId",SqlDbType.UniqueIdentifier,afterId);
        var items=new List<DocumentFolder>();using(var r=sql.ExecuteReader())while(r.Read())items.Add(Row(r));
        if(!Allowed(c,tx,actor,actions))return Denied<DocumentFolders>();var more=items.Count>25;if(more)items.RemoveAt(25);
        var next=more?cursors.Encode(actor.OwnerId,parentFolderId,items[^1].Title,items[^1].Id):null;tx.Commit();return IdentityOperationResult<DocumentFolders>.Success(new(items,next));
    }
    public IdentityOperationResult<DocumentFolder> GetFolder(IdentityPrincipal actor,Guid id)
    {
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        if(!Allowed(c,tx,actor,"documents.folder.read"))return Denied<DocumentFolder>();var item=ReadWithin(c,tx,actor.OwnerId,id);if(item is null)return Fail<DocumentFolder>("ResourceUnavailable",404);
        if(!Allowed(c,tx,actor,"documents.folder.read"))return Denied<DocumentFolder>();tx.Commit();return IdentityOperationResult<DocumentFolder>.Success(item);
    }
    private sealed record Receipt(int Version,Guid ActorId,Guid OwnerId,Guid Id,string Action,int Status);
    private IdentityOperationResult<object?> Mutate(IdentityPrincipal actor,string action,string[] dependencies,object request,string key,string? trace,Func<SqlConnection,SqlTransaction,IdentityOperationResult<Guid>> work,int status,Guid? expectedId=null)
    {
        if(!Guid.TryParseExact(key,"D",out var parsed)||parsed==Guid.Empty)return Fail<object?>("IdempotencyKeyRequired",422);
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        bool Authorized()=>Allowed(c,tx,actor,[action,..dependencies]);if(!Authorized())return Denied<object?>();SqlDocumentScopeLock.Acquire(c,tx,actor.OwnerId);
        var claim=receipts.TryClaim(c,tx,actor.UserId,action,key,JsonSerializer.Serialize(new{version=1,request}),DateTime.UtcNow);
        if(claim.IsConflict)return Fail<object?>("IdempotencyConflict",409);
        if(claim.IsReplay)
        {
            Receipt? saved=null;try{if(claim.ResultJson is not null)saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson);}catch(JsonException){}
            if(saved is null||saved.Version!=1||saved.ActorId!=actor.UserId||saved.OwnerId!=actor.OwnerId||saved.Id==Guid.Empty||(expectedId.HasValue&&saved.Id!=expectedId.Value)||saved.Action!=action||saved.Status!=status||claim.ResultStatusCode!=status||claim.ResultCode!="DocumentFolderCommitted")return Fail<object?>("ReceiptUnavailable",503);
            return Authorized()?IdentityOperationResult<object?>.Committed(saved.Id,status,"DocumentFolderCommitted"):Denied<object?>();
        }
        if(!claim.IsClaimed)return Fail<object?>("RequestInProgress",409);var result=work(c,tx);if(!result.Succeeded)return Fail<object?>(result.Code,result.StatusCode);
        using(var audit=Command(c,tx,actor.OwnerId,"INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)VALUES(@User,@User,@Action,'DocumentFolder',@Id,'Succeeded',@Trace);"))
        {Add(audit,"@User",SqlDbType.UniqueIdentifier,actor.UserId);Add(audit,"@Action",SqlDbType.NVarChar,action,160);Add(audit,"@Id",SqlDbType.UniqueIdentifier,result.Value);Add(audit,"@Trace",SqlDbType.NVarChar,trace,100);audit.ExecuteNonQuery();}
        receipts.Complete(c,tx,claim,"DocumentFolderCommitted",status,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,result.Value,action,status)));
        if(!Authorized())return Denied<object?>();tx.Commit();return IdentityOperationResult<object?>.Committed(result.Value,status,"DocumentFolderCommitted");
    }
    public IdentityOperationResult<object?> CreateFolder(IdentityPrincipal actor,DocumentFolderCreate command,string key,string? trace)
    {
        if(string.IsNullOrWhiteSpace(command.Title)||command.Title.Length>200||command.ParentFolderId==Guid.Empty)return Fail<object?>("ValidationFailed",422);
        var title=command.Title.Trim();return Mutate(actor,"documents.folder.create",command.ParentFolderId.HasValue?["documents.folder.read"]:[],new{title,command.ParentFolderId},key,trace,(c,tx)=>
        {
            if(command.ParentFolderId is {} parent){var row=ReadWithin(c,tx,actor.OwnerId,parent);if(row is null)return Fail<Guid>("ResourceUnavailable",404);if(row.Depth!=1)return Fail<Guid>("DepthLimit",422);}
            var id=Guid.NewGuid();using var sql=Command(c,tx,actor.OwnerId,"""
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
                  WHERE m.Code='FX20' AND rt.Code='Folder' AND rt.ContractVersion='documents-folder-v1';
                IF @@ROWCOUNT<>1 THROW 51069,'Documents Folder registration is unavailable.',1;
                INSERT [documents].[Folder](Id,OwnerId,Title,ParentFolderId,Depth,ParentDepth,CreatedByUserId,UpdatedByUserId)
                  VALUES(@Id,@Owner,@Title,@Parent,CASE WHEN @Parent IS NULL THEN 1 ELSE 2 END,CASE WHEN @Parent IS NULL THEN NULL ELSE 1 END,@User,@User);
                """);Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@Title",SqlDbType.NVarChar,title,200);Add(sql,"@Parent",SqlDbType.UniqueIdentifier,command.ParentFolderId);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();return IdentityOperationResult<Guid>.Success(id);
        },201);
    }
    public IdentityOperationResult<object?> RenameFolder(IdentityPrincipal actor,Guid id,string? etag,DocumentFolderRename command,string key,string? trace)
    {
        if(string.IsNullOrWhiteSpace(command.Title)||command.Title.Length>200)return Fail<object?>("ValidationFailed",422);var title=command.Title.Trim();
        return Mutate(actor,"documents.folder.rename",["documents.folder.read"],new{id,etag,title},key,trace,(c,tx)=>
        {
            var row=ReadWithin(c,tx,actor.OwnerId,id);if(row is null)return Fail<Guid>("ResourceUnavailable",404);if(string.IsNullOrEmpty(etag))return Fail<Guid>("PreconditionRequired",428);if(row.ETag!=etag)return Fail<Guid>("RevisionConflict",412);
            using var sql=Command(c,tx,actor.OwnerId,"UPDATE [documents].[Folder] SET Title=@Title,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;UPDATE [platform].[Resource] SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;");
            Add(sql,"@Title",SqlDbType.NVarChar,title,200);Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);Add(sql,"@User",SqlDbType.UniqueIdentifier,actor.UserId);sql.ExecuteNonQuery();return IdentityOperationResult<Guid>.Success(id);
        },200,id);
    }
}
