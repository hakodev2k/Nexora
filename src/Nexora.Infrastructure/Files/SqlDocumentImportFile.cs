using System.Data;
using System.Security.Cryptography;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Files;

internal sealed record DocumentImportFile(byte[] Bytes,string Format,string Digest);

public sealed partial class SqlFileService
{
    internal bool DocumentImportCurrentWithin(SqlImportUnit unit,Guid id,string etag,string digest)
    {
        if(!_capabilities.IsAllowed(unit.Connection,unit.Transaction,unit.Actor,"FX07","files.file.read")||!_capabilities.IsAllowed(unit.Connection,unit.Transaction,unit.Actor,"FX07","files.file.download"))return false;
        var row=ReadFile(unit.Connection,unit.Transaction,unit.Actor.OwnerId,id,false);if(row is null||row.ETag!=etag||row.Lifecycle!="Active"||row.ScanState!="Clean")return false;
        using var sql=unit.Command("SELECT Digest FROM [files].[FileObject] WHERE OwnerId=@Owner AND Id=@Id AND Lifecycle='Active' AND ScanState='Clean';");SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);
        return sql.ExecuteScalar()is byte[] bytes&&Convert.ToHexString(bytes)==digest;
    }
    internal IdentityOperationResult<DocumentImportFile> ReadDocumentImportWithin(SqlImportUnit unit,Guid id,string etag)
    {
        IdentityOperationResult<DocumentImportFile> Fail(string code,int status)=>IdentityOperationResult<DocumentImportFile>.Failure(code,status,"The owned clean document source is unavailable.");
        bool Allowed()=>_capabilities.IsAllowed(unit.Connection,unit.Transaction,unit.Actor,"FX07","files.file.read")&&_capabilities.IsAllowed(unit.Connection,unit.Transaction,unit.Actor,"FX07","files.file.download");
        if(!Allowed())return Fail("ModuleUnavailable",403);
        var source=ReadFileWithStorage(unit.Connection,unit.Transaction,unit.Actor.OwnerId,id,false);
        if(source is null||source.Value.Record.Lifecycle!="Active"||source.Value.Record.ScanState!="Clean")return Fail("ResourceUnavailable",404);
        var record=source.Value.Record;if(record.ETag!=etag)return Fail("RevisionConflict",412);
        var format=record.MediaType.Equals("text/markdown",StringComparison.OrdinalIgnoreCase)&&record.OriginalName.EndsWith(".md",StringComparison.OrdinalIgnoreCase)?"MD":
            record.MediaType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document",StringComparison.OrdinalIgnoreCase)&&record.OriginalName.EndsWith(".docx",StringComparison.OrdinalIgnoreCase)?"DOCX":null;
        if(format is null||record.ByteLength<1||record.ByteLength>(format=="MD"?DocumentImportCodec.MaxBodyBytes:DocumentImportCodec.MaxDocxBytes))return Fail("SourceLimitOrFormatUnsupported",422);
        var path=ResolveOwnedStoragePath(unit.Actor.OwnerId,source.Value.StorageKey);if(path is null)return Fail("ResourceUnavailable",404);
        byte[] expected;using(var sql=unit.Command("SELECT Digest FROM [files].[FileObject] WHERE OwnerId=@Owner AND Id=@Id AND Lifecycle='Active' AND ScanState='Clean';"))
        {SqlImportUnit.Add(sql,"@Id",SqlDbType.UniqueIdentifier,id);if(sql.ExecuteScalar()is not byte[] digest||digest.Length!=32)return Fail("ResourceUnavailable",404);expected=digest;}
        try
        {
            using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.SequentialScan);
            using var output=new MemoryStream();var buffer=new byte[65536];int count;
            while((count=input.Read(buffer,0,buffer.Length))>0){if(output.Length+count>record.ByteLength)return Fail("SourceIntegrityUnavailable",409);output.Write(buffer,0,count);}
            if(output.Length!=record.ByteLength)return Fail("SourceIntegrityUnavailable",409);var bytes=output.ToArray();var actual=SHA256.HashData(bytes);
            if(!CryptographicOperations.FixedTimeEquals(actual,expected))return Fail("SourceIntegrityUnavailable",409);
            if(!Allowed()||ReadFile(unit.Connection,unit.Transaction,unit.Actor.OwnerId,id,false)?.ETag!=etag)return Fail("ModuleUnavailable",403);
            return IdentityOperationResult<DocumentImportFile>.Success(new(bytes,format,Convert.ToHexString(actual)));
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){return Fail("StorageUnavailable",503);}
    }
}
