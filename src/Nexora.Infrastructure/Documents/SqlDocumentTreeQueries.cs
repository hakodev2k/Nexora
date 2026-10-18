using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Infrastructure.Documents;

public sealed partial class SqlDocumentTreeService
{
    private static readonly string[] InstalledActions=["documents.library.read","documents.page.read","documents.page.create","documents.page.save","documents.page.publish","documents.page.unpublish",
        "documents.page.archive","documents.page.unarchive","documents.page.trash","documents.page.restore","documents.page.purge","documents.page.history","documents.page.restore_version",
        "documents.folder.read","documents.folder.create","documents.folder.rename","documents.folder.trash","documents.folder.restore","documents.folder.purge",
        "documents.tag.read","documents.tag.create","documents.tag.rename","documents.tag.remove","documents.format.export","documents.format.import"];
    public IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal actor)
    {
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        var result=InstalledActions.ToDictionary(action=>action,action=>Allowed(c,tx,actor,action)
            &&(action!="documents.folder.rename"||Allowed(c,tx,actor,"documents.folder.read"))
            &&(action!="documents.page.restore_version"||Allowed(c,tx,actor,"documents.page.history","documents.page.save")));
        result["lifecycle.trash.read"]=capabilities.IsAllowed(c,tx,actor,"FX08","lifecycle.trash.read");
        foreach(var action in new[]{"transfer.export.request","transfer.export.read","transfer.export.download"})result[action]=capabilities.IsAllowed(c,tx,actor,"FX10",action);
        foreach(var action in new[]{"transfer.import.preview","transfer.import.commit","transfer.import.read"})result[action]=capabilities.IsAllowed(c,tx,actor,"FX10",action);
        foreach(var action in new[]{"files.file.read","files.file.download"})result[action]=capabilities.IsAllowed(c,tx,actor,"FX07",action);
        tx.Commit();return result.Values.Any(value=>value)?IdentityOperationResult<IReadOnlyDictionary<string,bool>>.Success(result):Denied<IReadOnlyDictionary<string,bool>>();
    }
    public IdentityOperationResult<DocumentTrashPage> TrashRoots(IdentityPrincipal actor,string? cursor)
    {
        var context=new DocumentListCursor.Context(actor.OwnerId,"Trash","cohorts","DocumentTrash",25,null,null,null,null,null,null,null);
        DocumentListCursor.Position? after=null;if(!string.IsNullOrEmpty(cursor)&&!cursorCodec.TryDecode(cursor,context,out after))return Fail<DocumentTrashPage>("InvalidCursor",422);
        using var c=connections.Create();c.Open();using var tx=c.BeginTransaction(IsolationLevel.Serializable);
        bool Authorized()=>Allowed(c,tx,actor,"documents.library.read")&&capabilities.IsAllowed(c,tx,actor,"FX08","lifecycle.trash.read");
        if(!Authorized())return Denied<DocumentTrashPage>();
        using var sql=Command(c,tx,actor.OwnerId,"""
            WITH roots AS(
              SELECT p.Id,CONVERT(varchar(16),'Page') Kind,CASE WHEN @ReadPage=1 THEN p.Title ELSE NULL END Title,
                CASE WHEN @ReadPage=1 THEN p.DocumentType ELSE NULL END DocumentType,b.DeletedAt,p.RowVersion
              FROM [documents].[Page] p JOIN [operations].[TrashBatch] b ON b.OwnerId=p.OwnerId AND b.Id=p.TrashBatchId AND b.RootResourceId=p.Id
              WHERE p.OwnerId=@Owner AND p.DeletedAt IS NOT NULL AND b.State='Trashed'
              UNION ALL SELECT f.Id,CONVERT(varchar(16),'Folder'),CASE WHEN @ReadFolder=1 THEN f.Title ELSE NULL END,CONVERT(varchar(32),NULL),b.DeletedAt,f.RowVersion
              FROM [documents].[Folder] f JOIN [operations].[TrashBatch] b ON b.OwnerId=f.OwnerId AND b.Id=f.TrashBatchId AND b.RootResourceId=f.Id
              WHERE f.OwnerId=@Owner AND f.DeletedAt IS NOT NULL AND b.State='Trashed'
            )SELECT TOP(26) Id,Kind,Title,DocumentType,DeletedAt,RowVersion FROM roots
              WHERE @After IS NULL OR DeletedAt<@After OR(DeletedAt=@After AND Id<@AfterId) ORDER BY DeletedAt DESC,Id DESC;
            """);ReadFlags(sql,c,tx,actor);Add(sql,"@After",SqlDbType.DateTime2,after?.UpdatedAt);Add(sql,"@AfterId",SqlDbType.UniqueIdentifier,after?.Id);
        var items=new List<DocumentTrashEntry>();using(var r=sql.ExecuteReader())while(r.Read())items.Add(new(r.GetGuid(0),r.GetString(1),Text(r,2),Text(r,3),new(DateTime.SpecifyKind(r.GetDateTime(4),DateTimeKind.Utc)),Tag(r.GetFieldValue<byte[]>(5))));
        if(!Authorized())return Denied<DocumentTrashPage>();
        var pageRead=Allowed(c,tx,actor,"documents.page.read");var folderRead=Allowed(c,tx,actor,"documents.folder.read");
        items=items.Select(item=>(item.Kind=="Page"?pageRead:folderRead)?item:item with{Title=null,DocumentType=null}).ToList();
        var more=items.Count>25;if(more)items.RemoveAt(25);var next=more?cursorCodec.Encode(context,items[^1].DeletedAt,items[^1].Id):null;
        tx.Commit();return IdentityOperationResult<DocumentTrashPage>.Success(new(items,next));
    }
}
