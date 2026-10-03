using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Transfer;

namespace Nexora.Infrastructure.Transfer;

internal sealed record ImportOptions(int SchemaVersion,string TimeZoneId,string FileETag,string CohortDigest);
internal sealed record StoredImport(ImportBatch Public,ImportOptions Options);
// Owns Operations tables only; other participants share the supplied local unit of work.
internal sealed class SqlImportBatchStore
{
    internal static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    private const string Columns="Id,FileObjectId,State,OptionsJson,AcceptedCount,SkippedCount,AppliedCount,CreatedAt,RowVersion,TotalCount";
    internal StoredImport? Get(SqlImportUnit unit,Guid id)
    {using var command=unit.Command($"SELECT {Columns} FROM [operations].[ImportBatch] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND Id=@Id;");SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);using var reader=command.ExecuteReader();return reader.Read()?Read(reader):null;}
    internal ImportBatchPage List(SqlImportUnit unit,Guid? cursor,string? state,IReadOnlyList<object> visibleFiles)
    {
        using var command=unit.Command($"""
            WITH selected AS(SELECT {Columns} FROM [operations].[ImportBatch] WHERE OwnerId=@Owner AND (@State IS NULL OR State=@State)
              AND EXISTS(SELECT 1 FROM OPENJSON(@Files) WITH(id uniqueidentifier,etag nvarchar(100)) f WHERE f.id=FileObjectId AND f.etag=JSON_VALUE(OptionsJson,'$.fileETag')))
            SELECT TOP(26) {Columns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected p WHERE p.Id=@Cursor
              AND (selected.CreatedAt<p.CreatedAt OR(selected.CreatedAt=p.CreatedAt AND selected.Id<p.Id))) ORDER BY CreatedAt DESC,Id DESC;
            """);
        SqlImportUnit.Add(command,"@State",SqlDbType.VarChar,state,64);SqlImportUnit.Add(command,"@Cursor",SqlDbType.UniqueIdentifier,cursor);
        SqlImportUnit.Add(command,"@Files",SqlDbType.NVarChar,JsonSerializer.Serialize(visibleFiles,Json),-1);
        var items=new List<ImportBatch>();using(var reader=command.ExecuteReader())while(reader.Read())items.Add(Read(reader).Public);
        return new(items.Take(25).ToArray(),items.Count>25?items[24].Id:null);
    }
    internal ImportRowPage Rows(SqlImportUnit unit,Guid id,int? cursor,string? outcome,int take=25)
    {
        using var command=unit.Command("""
            SELECT TOP(@Take) RowNumber,Outcome,ReasonCode,WarningsJson,CandidateJson,ResultResourceId FROM [operations].[ImportRow]
            WHERE OwnerId=@Owner AND BatchId=@Id AND RowNumber>@Cursor AND(@Outcome IS NULL OR Outcome=@Outcome) ORDER BY RowNumber;
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(command,"@Cursor",SqlDbType.Int,cursor??0);SqlImportUnit.Add(command,"@Outcome",SqlDbType.VarChar,outcome,64);SqlImportUnit.Add(command,"@Take",SqlDbType.Int,take+1);
        var rows=new List<ImportRow>();using(var reader=command.ExecuteReader())while(reader.Read())rows.Add(new(reader.GetInt32(0),reader.GetString(1),reader.IsDBNull(2)?null:reader.GetString(2),JsonSerializer.Deserialize<string[]>(reader.GetString(3),Json)!,reader.IsDBNull(4)?null:JsonSerializer.Deserialize<CalendarImportCandidate>(reader.GetString(4),Json),reader.IsDBNull(5)?null:reader.GetGuid(5)));
        return new(rows.Take(take).ToArray(),rows.Count>take?rows[take-1].RowNumber:null);
    }
    internal Guid Create(SqlImportUnit unit,CalendarImportPreview body,ImportOptions options,IReadOnlyList<ImportRow> rows)
    {
        var id=Guid.NewGuid();using var command=unit.Command("""
            INSERT [operations].[ImportBatch](Id,OwnerId,ModuleId,Format,FileObjectId,State,OptionsJson,TotalCount,AcceptedCount,SkippedCount,AppliedCount,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,Id,'ICS',@File,'PreviewReady',@Options,@Total,@Accepted,@Skipped,0,@Now,@Now,@User,@User FROM [platform].[Module] WHERE Code='FX13';
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(command,"@File",SqlDbType.UniqueIdentifier,body.FileId);SqlImportUnit.Add(command,"@Options",SqlDbType.NVarChar,JsonSerializer.Serialize(options,Json),-1);SqlImportUnit.Add(command,"@Total",SqlDbType.Int,rows.Count);SqlImportUnit.Add(command,"@Accepted",SqlDbType.Int,rows.Count(r=>r.Outcome=="Valid"));SqlImportUnit.Add(command,"@Skipped",SqlDbType.Int,rows.Count(r=>r.Outcome!="Valid"));
        if(command.ExecuteNonQuery()!=1)throw new InvalidOperationException("Importer contribution unavailable.");
        foreach(var row in rows)
        {
            using var insert=unit.Command("""
                INSERT [operations].[ImportRow](Id,OwnerId,BatchId,RowNumber,ExternalKeyHash,Outcome,ReasonCode,WarningsJson,CandidateJson,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(NEWID(),@Owner,@Batch,@Number,@Digest,@Outcome,@Reason,@Warnings,@Candidate,@Now,@Now,@User,@User);
                """);
            SqlImportUnit.Add(insert,"@Batch",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(insert,"@Number",SqlDbType.Int,row.RowNumber);
            SqlImportUnit.Add(insert,"@Digest",SqlDbType.Binary,row.Candidate is null?null:SHA256.HashData(Encoding.UTF8.GetBytes(row.Candidate.Uid)),32);
            SqlImportUnit.Add(insert,"@Outcome",SqlDbType.VarChar,row.Outcome,64);SqlImportUnit.Add(insert,"@Reason",SqlDbType.NVarChar,row.ReasonCode,100);SqlImportUnit.Add(insert,"@Warnings",SqlDbType.NVarChar,JsonSerializer.Serialize(row.Warnings,Json),-1);SqlImportUnit.Add(insert,"@Candidate",SqlDbType.NVarChar,row.Candidate is null?null:JsonSerializer.Serialize(row.Candidate,Json),-1);insert.ExecuteNonQuery();
        }
        return id;
    }
    internal void Outcome(SqlImportUnit unit,Guid id,int row,string outcome,string? reason,Guid? resource)
    {
        using var command=unit.Command("""
            UPDATE [operations].[ImportRow] SET Outcome=@Outcome,ReasonCode=@Reason,ResultResourceId=@Resource,UpdatedAt=@Now,UpdatedByUserId=@User
            WHERE OwnerId=@Owner AND BatchId=@Id AND RowNumber=@Row AND Outcome='Valid';
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(command,"@Row",SqlDbType.Int,row);SqlImportUnit.Add(command,"@Outcome",SqlDbType.VarChar,outcome,64);SqlImportUnit.Add(command,"@Reason",SqlDbType.NVarChar,reason,100);SqlImportUnit.Add(command,"@Resource",SqlDbType.UniqueIdentifier,resource);
        if(command.ExecuteNonQuery()!=1)throw new InvalidOperationException("Import row changed.");
    }
    internal void Finish(SqlImportUnit unit,Guid id,bool cancel)
    {
        using var command=unit.Command("""
            UPDATE [operations].[ImportBatch] SET State=@State,UpdatedAt=@Now,UpdatedByUserId=@User,
              AppliedCount=(SELECT COUNT(*) FROM [operations].[ImportRow] WHERE OwnerId=@Owner AND BatchId=@Id AND Outcome='Applied'),
              SkippedCount=(SELECT COUNT(*) FROM [operations].[ImportRow] WHERE OwnerId=@Owner AND BatchId=@Id AND Outcome IN('Invalid','Duplicate','Failed'))
            WHERE OwnerId=@Owner AND Id=@Id AND State='PreviewReady';
            IF @Cancel=1 UPDATE [operations].[ImportRow] SET CandidateJson=NULL,ExternalKeyHash=NULL,UpdatedAt=@Now,UpdatedByUserId=@User WHERE OwnerId=@Owner AND BatchId=@Id;
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(command,"@State",SqlDbType.VarChar,cancel?"Canceled":"Completed",64);SqlImportUnit.Add(command,"@Cancel",SqlDbType.Bit,cancel);command.ExecuteNonQuery();
    }
    private static StoredImport Read(SqlDataReader reader)
    {
        var options=JsonSerializer.Deserialize<ImportOptions>(reader.GetString(3),Json)!;
        return new(new(reader.GetGuid(0),reader.GetGuid(1),reader.GetString(2),options.TimeZoneId,reader.GetInt32(4),reader.GetInt32(5),reader.GetInt32(6),new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7),DateTimeKind.Utc)),SqlImportUnit.ETag(reader.GetFieldValue<byte[]>(8)),reader.GetInt32(9)),options);
    }
    internal static ImportAcknowledgement Ack(ImportBatch batch)=>new(batch.Id,batch.State,batch.AcceptedCount,batch.SkippedCount,batch.AppliedCount,batch.ETag,batch.TotalCount);
}
