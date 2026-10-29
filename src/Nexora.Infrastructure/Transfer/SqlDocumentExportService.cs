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
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Transfer;

public sealed class SqlDocumentExportService(SqlConnectionFactory connections,SqlDocumentService documents,string secret,TimeProvider clock):IDocumentExportService
{
    private readonly SqlImportAuthority authority=new(connections,clock);
    private readonly SqlRequestReceiptStore receipts=new(secret);
    private sealed record Receipt(int Version,Guid ActorId,Guid OwnerId,Guid PageId,Guid JobId,long SourceVersion,string Format,bool History);
    private sealed record Stored(DocumentExportJob Job,string Digest);
    private SqlImportUnit Open(IdentityPrincipal actor)=>new(connections,actor,clock.GetUtcNow().UtcDateTime);
    private bool Allowed(SqlImportUnit unit,string action)=>authority.AccountZone(unit) is not null&&authority.Allowed(unit,"FX10",action)
        &&authority.Allowed(unit,"FX20","documents.format.export")&&authority.Allowed(unit,"FX20","documents.page.read");
    private static IdentityOperationResult<T> Fail<T>(string code,int status)=>IdentityOperationResult<T>.Failure(code,status,"The document export or current source is unavailable. Check permissions, saved revision, preview and format.");
    private static bool Valid(Guid page,long version,string format)=>page!=Guid.Empty&&version>0&&format is "MD" or "DOCX";
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
    private string Token(SqlImportUnit unit,DocumentExportSnapshot source,string format,DocumentExportConversion output,long expiry)
        =>$"de1.{expiry.ToString(CultureInfo.InvariantCulture)}."+receipts.DigestSensitive(JsonSerializer.Serialize(new{version=1,unit.Actor.UserId,unit.Actor.OwnerId,source.PageId,source.VersionNumber,source.PageETag,source.Digest,format,codec=1,output.Warnings,artifact=Hash(output.Bytes),expiry}));
    private bool ValidToken(SqlImportUnit unit,DocumentExportSnapshot source,string format,DocumentExportConversion output,string token)
    {
        if(string.IsNullOrEmpty(token)||token.Length>1024)return false;var parts=token.Split('.');
        if(parts.Length!=3||parts[0]!="de1"||!long.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out var expiry))return false;
        var now=clock.GetUtcNow().ToUnixTimeSeconds();if(expiry<=now||expiry>now+300)return false;
        var expected=Token(unit,source,format,output,expiry);return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected),Encoding.UTF8.GetBytes(token));
    }
    public IdentityOperationResult<DocumentExportPreview> Preview(IdentityPrincipal actor,Guid pageId,string? etag,DocumentExportSelection selection)
    {
        if(!Valid(pageId,selection.VersionNumber,selection.Format))return Fail<DocumentExportPreview>("ValidationFailed",422);
        if(string.IsNullOrEmpty(etag))return Fail<DocumentExportPreview>("PreconditionRequired",428);
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.export.request"))return Fail<DocumentExportPreview>("ModuleUnavailable",403);
            var read=documents.ReadExportWithin(unit,pageId,selection.VersionNumber,etag);if(!read.Succeeded||read.Value is null)return Fail<DocumentExportPreview>(read.Code,read.StatusCode);
            var source=read.Value;var output=DocumentExportCodec.Convert(source,selection.Format);var expiry=clock.GetUtcNow().AddMinutes(5).ToUnixTimeSeconds();
            var result=new DocumentExportPreview(pageId,source.VersionNumber,selection.Format,source.PageETag,output.Warnings,Token(unit,source,selection.Format,output,expiry),DateTimeOffset.FromUnixTimeSeconds(expiry),output.Bytes.Length);
            var final=documents.ReadExportWithin(unit,pageId,selection.VersionNumber,etag);
            if(!Allowed(unit,"transfer.export.request")||!final.Succeeded||final.Value?.Digest!=source.Digest)return Fail<DocumentExportPreview>("ModuleUnavailable",403);
            return IdentityOperationResult<DocumentExportPreview>.Success(result);
        }
        catch(DocumentConversionException error){return Fail<DocumentExportPreview>(error.Message,422);}
        catch(SqlException){return Fail<DocumentExportPreview>("ExportUnavailable",503);}
    }
    public IdentityOperationResult<object?> Request(IdentityPrincipal actor,Guid pageId,string? etag,DocumentExportRequest body,string key,string? trace)
    {
        if(!Valid(pageId,body.VersionNumber,body.Format)||!Guid.TryParseExact(key,"D",out var uuid)||uuid==Guid.Empty||body.AcceptedWarnings is null||body.AcceptedWarnings.Length>32)return Fail<object?>("ValidationFailed",422);
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.export.request"))return Fail<object?>("ModuleUnavailable",403);
            var claim=receipts.TryClaim(unit.Connection,unit.Transaction,actor.UserId,"documents.format.export:v1",key,JsonSerializer.Serialize(new{version=1,actor.OwnerId,pageId,etag,body}),unit.Now);
            if(claim.IsConflict)return Fail<object?>("IdempotencyConflict",409);
            if(claim.IsReplay)
            {
                Receipt? saved=null;try{if(claim.ResultJson is not null)saved=JsonSerializer.Deserialize<Receipt>(claim.ResultJson);}catch(JsonException){}
                if(saved is null||saved.Version!=1||saved.ActorId!=actor.UserId||saved.OwnerId!=actor.OwnerId||saved.PageId!=pageId||saved.JobId==Guid.Empty
                    ||saved.SourceVersion!=body.VersionNumber||saved.Format!=body.Format||claim.ResultStatusCode!=201||claim.ResultCode!="DocumentExportCommitted")return Fail<object?>("ReceiptUnavailable",503);
                if(!Allowed(unit,"transfer.export.request")||(saved.History&&!authority.Allowed(unit,"FX20","documents.page.history")))return Fail<object?>("ModuleUnavailable",403);
                return IdentityOperationResult<object?>.Committed(saved.JobId,201,"DocumentExportCommitted");
            }
            if(!claim.IsClaimed)return Fail<object?>("RequestInProgress",409);
            if(string.IsNullOrEmpty(etag))return Fail<object?>("PreconditionRequired",428);
            var read=documents.ReadExportWithin(unit,pageId,body.VersionNumber,etag);if(!read.Succeeded||read.Value is null)return Fail<object?>(read.Code,read.StatusCode);var source=read.Value;
            var output=DocumentExportCodec.Convert(source,body.Format);
            if(!body.Confirmed||!ValidToken(unit,source,body.Format,output,body.PreviewToken)||!body.AcceptedWarnings.Order(StringComparer.Ordinal).SequenceEqual(output.Warnings,StringComparer.Ordinal))return Fail<object?>("PreviewChanged",409);
            var id=Guid.NewGuid();using(var sql=unit.Command("""
                INSERT [operations].[DocumentExportJob](Id,OwnerId,PageId,VersionNumber,Format,CodecVersion,SourceDigest,ArtifactDigest,Artifact,WarningsJson,CreatedAt,ExpiresAt,CreatedByUserId)
                VALUES(@Id,@Owner,@Page,@Version,@Format,1,@Digest,@ArtifactDigest,@Artifact,@Warnings,@Now,DATEADD(minute,15,@Now),@User);
                """))
            {SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(sql,"@Page",SqlDbType.UniqueIdentifier,pageId);SqlImportUnit.Add(sql,"@Version",SqlDbType.BigInt,body.VersionNumber);SqlImportUnit.Add(sql,"@Format",SqlDbType.VarChar,body.Format,8);SqlImportUnit.Add(sql,"@Digest",SqlDbType.Char,source.Digest,64);SqlImportUnit.Add(sql,"@ArtifactDigest",SqlDbType.Char,Hash(output.Bytes),64);SqlImportUnit.Add(sql,"@Artifact",SqlDbType.VarBinary,output.Bytes,-1);SqlImportUnit.Add(sql,"@Warnings",SqlDbType.NVarChar,JsonSerializer.Serialize(output.Warnings),2000);sql.ExecuteNonQuery();}
            authority.Audit(unit,pageId,"documents.format.export",trace,"DocumentPage");authority.Audit(unit,id,"transfer.export.request",trace,"DocumentExportJob");
            receipts.Complete(unit.Connection,unit.Transaction,claim,"DocumentExportCommitted",201,JsonSerializer.Serialize(new Receipt(1,actor.UserId,actor.OwnerId,pageId,id,body.VersionNumber,body.Format,source.RequiresHistory)));
            var final=documents.ReadExportWithin(unit,pageId,body.VersionNumber,etag);
            if(!Allowed(unit,"transfer.export.request")||!final.Succeeded||final.Value?.Digest!=source.Digest)return Fail<object?>("ModuleUnavailable",403);
            unit.Transaction.Commit();return IdentityOperationResult<object?>.Committed(id,201,"DocumentExportCommitted");
        }
        catch(DocumentConversionException error){return Fail<object?>(error.Message,422);}
        catch(SqlException){return Fail<object?>("ExportUnavailable",503);}
    }
    private Stored? Read(SqlImportUnit unit,Guid id)
    {
        using var sql=unit.Command("SELECT Id,PageId,VersionNumber,Format,CodecVersion,SourceDigest,CreatedAt,ExpiresAt,RowVersion FROM [operations].[DocumentExportJob] WHERE OwnerId=@Owner AND Id=@Id;");SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);
        using var r=sql.ExecuteReader();if(!r.Read())return null;var created=new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(6),DateTimeKind.Utc));var expiry=new DateTimeOffset(DateTime.SpecifyKind(r.GetDateTime(7),DateTimeKind.Utc));
        return new(new(r.GetGuid(0),r.GetGuid(1),r.GetInt64(2),r.GetString(3),r.GetInt32(4),expiry<=clock.GetUtcNow()?"Expired":"Ready",created,expiry,SqlImportUnit.ETag(r.GetFieldValue<byte[]>(8))),r.GetString(5));
    }
    private bool Available(SqlImportUnit unit,Stored stored)
    {var source=documents.ReadExportWithin(unit,stored.Job.PageId,stored.Job.VersionNumber);return source.Succeeded&&source.Value?.Digest==stored.Digest&&stored.Job.CodecVersion==1;}
    public IdentityOperationResult<DocumentExportJob> Get(IdentityPrincipal actor,Guid jobId)
    {
        try{using var unit=Open(actor);if(!Allowed(unit,"transfer.export.read"))return Fail<DocumentExportJob>("ModuleUnavailable",403);var stored=Read(unit,jobId);if(stored is null||!Available(unit,stored))return Fail<DocumentExportJob>("ResourceUnavailable",404);
            if(!Allowed(unit,"transfer.export.read")||!Available(unit,stored))return Fail<DocumentExportJob>("ModuleUnavailable",403);return IdentityOperationResult<DocumentExportJob>.Success(stored.Job with{State=stored.Job.ExpiresAt<=clock.GetUtcNow()?"Expired":"Ready"});}
        catch(SqlException){return Fail<DocumentExportJob>("ExportUnavailable",503);}
    }
    public IdentityOperationResult<DocumentExportDownload> Content(IdentityPrincipal actor,Guid jobId)
    {
        try
        {
            using var unit=Open(actor);if(!Allowed(unit,"transfer.export.download"))return Fail<DocumentExportDownload>("ModuleUnavailable",403);var stored=Read(unit,jobId);if(stored is null||!Available(unit,stored))return Fail<DocumentExportDownload>("ResourceUnavailable",404);
            if(stored.Job.ExpiresAt<=clock.GetUtcNow())return Fail<DocumentExportDownload>("ArtifactExpired",410);
            byte[] bytes;string digest;using(var sql=unit.Command("SELECT Artifact,ArtifactDigest FROM [operations].[DocumentExportJob] WHERE OwnerId=@Owner AND Id=@Id;")){SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,jobId);using var r=sql.ExecuteReader();if(!r.Read())return Fail<DocumentExportDownload>("ResourceUnavailable",404);bytes=r.GetFieldValue<byte[]>(0);digest=r.GetString(1);}
            if(bytes.Length>DocumentExportCodec.MaxOutputBytes||Hash(bytes)!=digest)return Fail<DocumentExportDownload>("ArtifactUnavailable",503);
            authority.Audit(unit,jobId,"transfer.export.download",null,"DocumentExportJob");
            if(!Allowed(unit,"transfer.export.download")||!Available(unit,stored))return Fail<DocumentExportDownload>("ModuleUnavailable",403);
            if(stored.Job.ExpiresAt<=clock.GetUtcNow())return Fail<DocumentExportDownload>("ArtifactExpired",410);
            unit.Transaction.Commit();var md=stored.Job.Format=="MD";return IdentityOperationResult<DocumentExportDownload>.Success(new(bytes,md?"text/markdown; charset=utf-8":"application/vnd.openxmlformats-officedocument.wordprocessingml.document",$"document-v{stored.Job.VersionNumber.ToString(CultureInfo.InvariantCulture)}.{(md?"md":"docx")}"));
        }
        catch(SqlException){return Fail<DocumentExportDownload>("ExportUnavailable",503);}
    }
}
