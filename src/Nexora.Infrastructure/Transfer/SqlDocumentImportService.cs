using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Files;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Transfer;

public sealed class SqlDocumentImportService(SqlConnectionFactory connections,SqlDocumentService documents,SqlFileService files,string secret,TimeProvider clock):IDocumentImportService
{
    private readonly SqlImportAuthority authority=new(connections,clock);
    private readonly SqlRequestReceiptStore receipts=new(secret);
    private sealed record Receipt(int Version,Guid ActorId,Guid OwnerId,Guid PageId,Guid JobId,Guid FileId,string Mode);
    private SqlImportUnit Open(IdentityPrincipal actor)=>new(connections,actor,clock.GetUtcNow().UtcDateTime);
    private static IdentityOperationResult<T> Fail<T>(string code,int status)=>IdentityOperationResult<T>.Failure(code,status,"The document import, explicit draft or owned clean source is unavailable. Check source revision, permissions, preview and format.");
    private bool Allowed(SqlImportUnit unit,string action,bool source=true)=>authority.AccountZone(unit)is not null&&authority.Allowed(unit,"FX10",action)&&authority.Allowed(unit,"FX20","documents.format.import")
        &&(!source||(authority.Allowed(unit,"FX20","documents.page.create")&&authority.Allowed(unit,"FX07","files.file.read")&&authority.Allowed(unit,"FX07","files.file.download")));
    private string Token(SqlImportUnit unit,DocumentImportSelection selection,DocumentImportConversion converted,long expiry)
        =>$"di1.{expiry.ToString(CultureInfo.InvariantCulture)}."+receipts.DigestSensitive(JsonSerializer.Serialize(new{version=1,unit.Actor.UserId,unit.Actor.OwnerId,selection,converted.SourceDigest,body=receipts.DigestSensitive(converted.Body),converted.Warnings,expiry}));
    private bool ValidToken(SqlImportUnit unit,DocumentImportSelection selection,DocumentImportConversion converted,string token)
    {
        if(string.IsNullOrEmpty(token)||token.Length>1024)return false;var parts=token.Split('.');var now=clock.GetUtcNow().ToUnixTimeSeconds();
        if(parts.Length!=3||parts[0]!="di1"||!long.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out var expiry)||expiry<=now||expiry>now+300)return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Token(unit,selection,converted,expiry)),Encoding.UTF8.GetBytes(token));
    }
    public IdentityOperationResult<DocumentImportPreview> Preview(IdentityPrincipal actor,DocumentImportSelection selection)
    {
        if(selection.FileId==Guid.Empty||string.IsNullOrEmpty(selection.FileETag)||selection.EditorMode is not("Block" or "Markdown"))return Fail<DocumentImportPreview>("ValidationFailed",422);
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.import.preview"))return Fail<DocumentImportPreview>("ModuleUnavailable",403);
            var file=files.ReadDocumentImportWithin(unit,selection.FileId,selection.FileETag);if(!file.Succeeded||file.Value is null)return Fail<DocumentImportPreview>(file.Code,file.StatusCode);
            var converted=DocumentImportCodec.Convert(file.Value.Bytes,file.Value.Format,selection.EditorMode);var expiry=clock.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds();
            var result=new DocumentImportPreview(selection.FileId,selection.FileETag,1,selection.EditorMode,converted.Body,converted.Warnings,Token(unit,selection,converted,expiry),DateTimeOffset.FromUnixTimeSeconds(expiry));
            if(!Allowed(unit,"transfer.import.preview")||!files.DocumentImportCurrentWithin(unit,selection.FileId,selection.FileETag,converted.SourceDigest))return Fail<DocumentImportPreview>("ModuleUnavailable",403);
            return IdentityOperationResult<DocumentImportPreview>.Success(result);
        }
        catch(DocumentConversionException error){return Fail<DocumentImportPreview>(error.Message,422);}
        catch(SqlException){return Fail<DocumentImportPreview>("ImportUnavailable",503);}
    }
    public IdentityOperationResult<object?> Commit(IdentityPrincipal actor,DocumentImportCommand body,string key,string? trace)
    {
        if(body.Page is null||body.FileId==Guid.Empty||string.IsNullOrEmpty(body.FileETag)||body.AcceptedWarnings is null||body.AcceptedWarnings.Length>64
            ||!Guid.TryParseExact(key,"D",out var uuid)||uuid==Guid.Empty)return Fail<object?>("ValidationFailed",422);
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.import.commit")||!documents.ImportedCreateAllowedWithin(unit,body.Page))return Fail<object?>("ModuleUnavailable",403);
            var claim=receipts.TryClaim(unit.Connection,unit.Transaction,actor.UserId,"documents.format.import:v1",key,JsonSerializer.Serialize(new{version=1,actor.OwnerId,body}),unit.Now);
            if(claim.IsConflict)return Fail<object?>("IdempotencyConflict",409);
            if(claim.IsReplay)
            {
                Receipt? saved=null;try{if(claim.ResultJson is not null)saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson);}catch(JsonException){}
                if(saved is null||saved.Version!=1||saved.ActorId!=actor.UserId||saved.OwnerId!=actor.OwnerId||saved.PageId==Guid.Empty||saved.JobId==Guid.Empty||saved.FileId!=body.FileId||saved.Mode!=body.Page.EditorMode||claim.ResultStatusCode!=201||claim.ResultCode!="DocumentImportCommitted")return Fail<object?>("ReceiptUnavailable",503);
                if(!Allowed(unit,"transfer.import.commit")||!documents.ImportedCreateAllowedWithin(unit,body.Page))return Fail<object?>("ModuleUnavailable",403);
                return IdentityOperationResult<object?>.Committed(saved.PageId,201,"DocumentImportCommitted");
            }
            if(!claim.IsClaimed)return Fail<object?>("RequestInProgress",409);
            var file=files.ReadDocumentImportWithin(unit,body.FileId,body.FileETag);if(!file.Succeeded||file.Value is null)return Fail<object?>(file.Code,file.StatusCode);
            var converted=DocumentImportCodec.Convert(file.Value.Bytes,file.Value.Format,body.Page.EditorMode);var selection=new DocumentImportSelection(body.FileId,body.FileETag,body.Page.EditorMode);
            if(!body.Confirmed||!ValidToken(unit,selection,converted,body.PreviewToken)||!body.AcceptedWarnings.Order(StringComparer.Ordinal).SequenceEqual(converted.Warnings,StringComparer.Ordinal))return Fail<object?>("PreviewChanged",409);
            if(body.DraftEdited!=((body.Page.Body??"")!=converted.Body))return Fail<object?>("DraftEditConfirmationRequired",422);
            var created=documents.CreateImportedWithin(unit,body.Page,trace);if(!created.Succeeded||created.Value is null)return Fail<object?>(created.Code,created.StatusCode);var id=Guid.NewGuid();var page=created.Value.Id;
            using(var sql=unit.Command("""
                INSERT [operations].[DocumentImportJob](Id,OwnerId,PageId,FileId,SourceDigest,ParserVersion,EditorMode,WarningsJson,DraftEdited,CreatedAt,CreatedByUserId)
                VALUES(@Id,@Owner,@Page,@File,@Digest,1,@Mode,@Warnings,@Edited,@Now,@User);
                """))
            {SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(sql,"@Page",SqlDbType.UniqueIdentifier,page);SqlImportUnit.Add(sql,"@File",SqlDbType.UniqueIdentifier,body.FileId);SqlImportUnit.Add(sql,"@Digest",SqlDbType.Char,converted.SourceDigest,64);SqlImportUnit.Add(sql,"@Mode",SqlDbType.VarChar,body.Page.EditorMode,16);SqlImportUnit.Add(sql,"@Warnings",SqlDbType.NVarChar,JsonSerializer.Serialize(converted.Warnings),4000);SqlImportUnit.Add(sql,"@Edited",SqlDbType.Bit,body.DraftEdited);sql.ExecuteNonQuery();}
            authority.Audit(unit,id,"transfer.import.commit",trace,"DocumentImportJob");authority.Audit(unit,page,"documents.format.import",trace,"DocumentPage");
            receipts.Complete(unit.Connection,unit.Transaction,claim,"DocumentImportCommitted",201,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,page,id,body.FileId,body.Page.EditorMode)));
            if(!Allowed(unit,"transfer.import.commit")||!documents.ImportedCreateAllowedWithin(unit,body.Page)||!files.DocumentImportCurrentWithin(unit,body.FileId,body.FileETag,converted.SourceDigest))return Fail<object?>("ModuleUnavailable",403);
            unit.Transaction.Commit();return IdentityOperationResult<object?>.Committed(page,201,"DocumentImportCommitted");
        }
        catch(DocumentConversionException error){return Fail<object?>(error.Message,422);}
        catch(TimeoutException){return Fail<object?>("RetryableConflict",409);}
        catch(SqlException error)when(error.Number==1205){return Fail<object?>("RetryableConflict",409);}
        catch(SqlException){return Fail<object?>("ImportUnavailable",503);}
    }
    public IdentityOperationResult<DocumentImportReport> GetByPage(IdentityPrincipal actor,Guid pageId)
    {
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.import.read",false))return Fail<DocumentImportReport>("ModuleUnavailable",403);
            using var sql=unit.Command("SELECT Id,PageId,FileId,ParserVersion,EditorMode,CreatedAt,RowVersion FROM [operations].[DocumentImportJob] WHERE OwnerId=@Owner AND PageId=@Page;");SqlImportUnit.Add(sql,"@Page",SqlDbType.UniqueIdentifier,pageId);
            DocumentImportReport? result;using(var r=sql.ExecuteReader())result=r.Read()?new(r.GetGuid(0),r.GetGuid(1),r.GetGuid(2),r.GetInt32(3),r.GetString(4),"Committed",new(DateTime.SpecifyKind(r.GetDateTime(5),DateTimeKind.Utc)),SqlImportUnit.ETag(r.GetFieldValue<byte[]>(6))):null;
            if(result is null)return Fail<DocumentImportReport>("ResourceUnavailable",404);if(!Allowed(unit,"transfer.import.read",false))return Fail<DocumentImportReport>("ModuleUnavailable",403);return IdentityOperationResult<DocumentImportReport>.Success(result);
        }
        catch(SqlException){return Fail<DocumentImportReport>("ImportUnavailable",503);}
    }
}
