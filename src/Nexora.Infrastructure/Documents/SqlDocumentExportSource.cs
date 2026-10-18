using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Documents;

internal sealed record DocumentExportSnapshot(Guid PageId,long VersionNumber,string Title,string Body,string Mode,string PageETag,bool RequiresHistory,string Digest);

public sealed partial class SqlDocumentService
{
    internal IdentityOperationResult<DocumentExportSnapshot> ReadExportWithin(SqlImportUnit unit,Guid pageId,long version,string? etag=null)
    {
        var actor=unit.Actor;var c=unit.Connection;var tx=unit.Transaction;
        bool Allowed()=>CurrentAuthority(c,tx,actor,"documents.page.read")&&CurrentAuthority(c,tx,actor,"documents.format.export");
        if(!Allowed())return Failure<DocumentExportSnapshot>("ModuleUnavailable",403,"Document export is unavailable.");
        var page=ReadDetail(c,tx,actor.OwnerId,pageId,false);
        if(page is null||page.Status is not("Draft" or "Published" or "Archived"))return Failure<DocumentExportSnapshot>("ResourceUnavailable",404,"Document source unavailable.");
        if(etag is not null&&(!TryDecodeETag(etag,out _)||page.ETag!=etag))return Failure<DocumentExportSnapshot>("RevisionConflict",412,"The saved source changed. Read a fresh preview.");
        var history=version!=page.VersionNumber;
        if(history&&!CurrentAuthority(c,tx,actor,"documents.page.history"))return Failure<DocumentExportSnapshot>("ModuleUnavailable",403,"Document export is unavailable.");
        var snapshot=ReadVersionSnapshot(c,tx,actor.OwnerId,pageId,version);
        if(snapshot is null)return Failure<DocumentExportSnapshot>("ResourceUnavailable",404,"Document source unavailable.");
        if(snapshot.Snapshot.EditorMode!=page.EditorMode||snapshot.Snapshot.DocumentType!=page.DocumentType)return Failure<DocumentExportSnapshot>("SnapshotIncompatible",409,"The saved format is incompatible.");
        var valid=ValidateBody(snapshot.Body);if(valid is not null)return Failure<DocumentExportSnapshot>(valid.Code,valid.StatusCode,valid.Title);
        var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{version=1,pageId,number=version,snapshot.Snapshot.Title,snapshot.Snapshot.EditorMode,snapshot.Body}))));
        if(!Allowed()||(history&&!CurrentAuthority(c,tx,actor,"documents.page.history")))return Failure<DocumentExportSnapshot>("ModuleUnavailable",403,"Document export is unavailable.");
        return IdentityOperationResult<DocumentExportSnapshot>.Success(new(pageId,version,snapshot.Snapshot.Title,snapshot.Body,page.EditorMode,page.ETag,history,digest));
    }
}
