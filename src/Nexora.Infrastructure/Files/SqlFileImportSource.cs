using System.Data;
using Nexora.Application.Files;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Files;

// Files owns both authorized content access and the transaction-level source revision guard.
internal sealed class SqlFileImportSource(IFileService files)
{
    internal IdentityOperationResult<byte[]> Read(IdentityPrincipal actor,Guid id,string etag)
    {
        var metadata=files.Get(actor,id);
        if(!metadata.Succeeded||metadata.Value is null)return IdentityOperationResult<byte[]>.Failure(metadata.Code,metadata.StatusCode,metadata.Title);
        var file=metadata.Value;
        if(file.ETag!=etag)return IdentityOperationResult<byte[]>.Failure("RevisionConflict",409,"The source file changed. Select it again.");
        if(file.Lifecycle!="Active"||file.ScanState!="Clean"||file.MediaType!="text/calendar"||!file.OriginalName.EndsWith(".ics",StringComparison.OrdinalIgnoreCase)||file.ByteLength is <1 or >CalendarIcsParser.MaxBytes)
            return IdentityOperationResult<byte[]>.Failure("FileUnavailable",422,"Choose a supported clean ICS file.");
        var download=files.OpenContent(actor,id,false);
        if(!download.Succeeded||download.Value is null)return IdentityOperationResult<byte[]>.Failure(download.Code,download.StatusCode,download.Title);
        using var stream=download.Value.Content;using var output=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=stream.Read(buffer,0,buffer.Length))>0){if(output.Length+count>CalendarIcsParser.MaxBytes)return IdentityOperationResult<byte[]>.Failure("FileLimitExceeded",422,"The ICS file exceeds the supported limit.");output.Write(buffer,0,count);}
        return IdentityOperationResult<byte[]>.Success(output.ToArray());
    }
    internal bool Current(SqlImportUnit unit,Guid id,string etag)
        =>Check(unit,id,etag) is null;
    internal string? Check(SqlImportUnit unit,Guid id,string etag)
    {
        using var command=unit.Command("""
            SELECT RowVersion FROM [files].[FileObject] WITH(UPDLOCK,HOLDLOCK)
            WHERE OwnerId=@Owner AND Id=@Id AND Lifecycle='Active' AND ScanState='Clean'
              AND MediaType='text/calendar' AND ByteLength BETWEEN 1 AND 1048576;
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);
        return command.ExecuteScalar() is not byte[] revision ? "ResourceUnavailable" : SqlImportUnit.ETag(revision)==etag ? null : "RevisionConflict";
    }
    internal IReadOnlyList<object>? VisibleRevisions(SqlImportUnit unit)
    {
        using var command=unit.Command("SELECT TOP(10001) Id,RowVersion FROM [files].[FileObject] WHERE OwnerId=@Owner AND Lifecycle='Active' AND ScanState='Clean' AND MediaType='text/calendar' AND ByteLength BETWEEN 1 AND 1048576 ORDER BY Id;");
        var files=new List<object>();using var reader=command.ExecuteReader();
        while(reader.Read()){if(files.Count==10000)return null;files.Add(new{id=reader.GetGuid(0),etag=SqlImportUnit.ETag(reader.GetFieldValue<byte[]>(1))});}
        return files;
    }
}
