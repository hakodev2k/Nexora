using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Files;
using Nexora.Application.Identity;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Files;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;

namespace Nexora.Infrastructure.Transfer;

/// <summary>Coordinates source-owned participants through a single bounded local SQL transaction.</summary>
public sealed class SqlCalendarImportService : ICalendarImportService
{
    public static readonly string[] Actions=["transfer.import.preview","transfer.import.commit","transfer.import.read","transfer.import.cancel","calendar.ics.preview","calendar.ics.import"];
    private readonly SqlConnectionFactory connections;private readonly TimeProvider clock;private readonly SqlRequestReceiptStore receipts;
    private readonly SqlImportAuthority authority;private readonly SqlFileImportSource files;private readonly SqlCalendarImportParticipant calendar;private readonly SqlImportBatchStore batches=new();
    public SqlCalendarImportService(SqlConnectionFactory connections,IFileService files,string secret,TimeProvider clock)
    {this.connections=connections;this.clock=clock;receipts=new(secret);authority=new(connections,clock);this.files=new(files);calendar=new(authority);}
    private SqlImportUnit Open(IdentityPrincipal actor)=>new(connections,actor,clock.GetUtcNow().UtcDateTime);
    private static IdentityOperationResult<T> Failure<T>(string code,int status,string title)=>IdentityOperationResult<T>.Failure(code,status,title);
    private static IdentityOperationResult<T> Denied<T>()=>Failure<T>("ModuleUnavailable",403,"The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>()=>Failure<T>("ResourceUnavailable",404,"The resource is unavailable.");
    private static IdentityOperationResult<T> Invalid<T>()=>Failure<T>("ValidationFailed",422,"Check the supported fields and explicit confirmation.");
    private static IdentityOperationResult<T> Conflict<T>()=>Failure<T>("RevisionConflict",409,"The preview or source changed. Create a fresh preview.");
    public IdentityOperationResult<IReadOnlyDictionary<string,bool>> Capabilities(IdentityPrincipal actor)
    {
        using var unit=Open(actor);if(authority.AccountZone(unit) is null)return Denied<IReadOnlyDictionary<string,bool>>();
        var result=Actions.ToDictionary(action=>action,action=>authority.Required(unit,action.StartsWith("calendar.",StringComparison.Ordinal)?action.EndsWith("preview",StringComparison.Ordinal)?"transfer.import.preview":"transfer.import.commit":action));
        return result.Values.Any(v=>v)?IdentityOperationResult<IReadOnlyDictionary<string,bool>>.Success(result):Denied<IReadOnlyDictionary<string,bool>>();
    }
    public IdentityOperationResult<ImportAcknowledgement> Preview(IdentityPrincipal actor,CalendarImportPreview body,string key,string? trace)=>
        Mutate(actor,"transfer.import.preview",body,key,trace,unit=>
        {
            if(body.FileId==Guid.Empty||string.IsNullOrWhiteSpace(body.FileETag))return Invalid<ImportAcknowledgement>();
            var source=files.Check(unit,body.FileId,body.FileETag);
            if(source is not null)return source=="RevisionConflict"?Conflict<ImportAcknowledgement>():Missing<ImportAcknowledgement>();
            var content=files.Read(actor,body.FileId,body.FileETag);
            if(!content.Succeeded||content.Value is null)return Failure<ImportAcknowledgement>(content.Code,content.StatusCode,content.Title);
            var zone=authority.AccountZone(unit)!;var parsed=CalendarIcsParser.Parse(content.Value,zone);
            if(!parsed.Succeeded)return Failure<ImportAcknowledgement>(parsed.ReasonCode!,422,"The file is not a supported bounded ICS calendar.");
            var cohort=calendar.Cohort(unit,zone);if(cohort is null)return Failure<ImportAcknowledgement>("PreviewLimitExceeded",422,"The readable Calendar cohort exceeds this local preview limit.");
            var rows=calendar.Preview(unit,parsed.Rows,cohort);
            var id=batches.Create(unit,body,new(1,zone,body.FileETag,cohort.Digest),rows);
            if(!files.Current(unit,body.FileId,body.FileETag))return Conflict<ImportAcknowledgement>();
            return IdentityOperationResult<ImportAcknowledgement>.Success(SqlImportBatchStore.Ack(batches.Get(unit,id)!.Public),201);
        });
    public IdentityOperationResult<ImportBatchPage> List(IdentityPrincipal actor,Guid? cursor,string? state)
    {
        using var unit=Open(actor);if(!authority.Required(unit,"transfer.import.read"))return Denied<ImportBatchPage>();
        if(state is not(null or "PreviewReady" or "Completed" or "Canceled"))return Invalid<ImportBatchPage>();
        var visible=files.VisibleRevisions(unit);if(visible is null)return Failure<ImportBatchPage>("SourceLimitExceeded",422,"The file cohort exceeds the local report limit.");
        return IdentityOperationResult<ImportBatchPage>.Success(batches.List(unit,cursor,state,visible));
    }
    public IdentityOperationResult<ImportBatch> Get(IdentityPrincipal actor,Guid id)
    {
        using var unit=Open(actor);if(!authority.Required(unit,"transfer.import.read"))return Denied<ImportBatch>();
        var batch=batches.Get(unit,id);if(batch is null)return Missing<ImportBatch>();
        if(!files.Current(unit,batch.Public.FileId,batch.Options.FileETag))return Missing<ImportBatch>();
        return IdentityOperationResult<ImportBatch>.Success(batch.Public);
    }
    public IdentityOperationResult<ImportRowPage> Rows(IdentityPrincipal actor,Guid id,int? cursor,string? outcome)
    {
        using var unit=Open(actor);if(!authority.Required(unit,"transfer.import.read"))return Denied<ImportRowPage>();
        if(cursor is <0||outcome is not(null or "Valid" or "Invalid" or "Duplicate" or "Applied" or "Failed"))return Invalid<ImportRowPage>();
        var batch=batches.Get(unit,id);if(batch is null||!files.Current(unit,batch.Public.FileId,batch.Options.FileETag))return Missing<ImportRowPage>();
        return IdentityOperationResult<ImportRowPage>.Success(batches.Rows(unit,id,cursor,outcome));
    }
    public IdentityOperationResult<ImportAcknowledgement> Commit(IdentityPrincipal actor,Guid id,string? etag,ImportConfirmation body,string key,string? trace)=>Change(actor,id,etag,body,key,trace,false);
    public IdentityOperationResult<ImportAcknowledgement> Cancel(IdentityPrincipal actor,Guid id,string? etag,ImportConfirmation body,string key,string? trace)=>Change(actor,id,etag,body,key,trace,true);
    private IdentityOperationResult<ImportAcknowledgement> Change(IdentityPrincipal actor,Guid id,string? etag,ImportConfirmation body,string key,string? trace,bool cancel)=>
        Mutate(actor,cancel?"transfer.import.cancel":"transfer.import.commit",new{id,etag,body},key,trace,unit=>
        {
            if(!body.Confirmed)return Invalid<ImportAcknowledgement>();
            var batch=batches.Get(unit,id);if(batch is null)return Missing<ImportAcknowledgement>();
            if(string.IsNullOrWhiteSpace(etag))return Failure<ImportAcknowledgement>("PreconditionRequired",428,"Supply the current preview revision.");
            if(etag!=batch.Public.ETag)return Conflict<ImportAcknowledgement>();
            if(batch.Public.State!="PreviewReady")return Failure<ImportAcknowledgement>("LifecycleLocked",409,"The import is already completed or canceled.");
            if(!files.Current(unit,batch.Public.FileId,batch.Options.FileETag))return Conflict<ImportAcknowledgement>();
            if(!cancel)
            {
                var zone=authority.AccountZone(unit);if(zone!=batch.Options.TimeZoneId)return Conflict<ImportAcknowledgement>();
                var cohort=calendar.Cohort(unit,zone!);if(cohort is null||cohort.Digest!=batch.Options.CohortDigest)return Conflict<ImportAcknowledgement>();
                var rows=batches.Rows(unit,id,null,"Valid",CalendarIcsParser.MaxEvents).Items;
                foreach(var row in rows)
                {
                    if(!authority.Required(unit,"transfer.import.commit"))return Denied<ImportAcknowledgement>();
                    var candidate=row.Candidate;if(candidate is null||candidate.SchemaVersion!=1||candidate.RowNumber!=row.RowNumber||candidate.TimeZoneId!=zone)return Failure<ImportAcknowledgement>("CandidateUnavailable",409,"The import preview is unavailable.");
                    var applied=calendar.Apply(unit,id,candidate);
                    if(applied.Reason=="UidDigestConflict")return Failure<ImportAcknowledgement>("UidDigestConflict",409,"The source identifier cannot be safely resolved.");
                    batches.Outcome(unit,id,row.RowNumber,applied.Reason is null?"Applied":"Duplicate",applied.Reason,applied.Resource);
                }
            }
            batches.Finish(unit,id,cancel);
            return IdentityOperationResult<ImportAcknowledgement>.Success(SqlImportBatchStore.Ack(batches.Get(unit,id)!.Public));
        });
    private IdentityOperationResult<ImportAcknowledgement> Mutate(IdentityPrincipal actor,string action,object body,string key,string? trace,Func<SqlImportUnit,IdentityOperationResult<ImportAcknowledgement>> apply)
    {
        try
        {
            using var unit=Open(actor);if(!authority.Required(unit,action))return Denied<ImportAcknowledgement>();
            var claim=receipts.TryClaim(unit.Connection,unit.Transaction,actor.UserId,action,key,JsonSerializer.Serialize(body,SqlImportBatchStore.Json),unit.Now);
            if(claim.IsInvalid)return Invalid<ImportAcknowledgement>();
            if(claim.IsConflict)return Failure<ImportAcknowledgement>("IdempotencyConflict",409,"The request key belongs to another request.");
            if(claim.IsReplay)
            {
                var acknowledgement=claim.ResultJson is null?null:JsonSerializer.Deserialize<ImportAcknowledgement>(claim.ResultJson,SqlImportBatchStore.Json);
                return acknowledgement is null?Conflict<ImportAcknowledgement>():IdentityOperationResult<ImportAcknowledgement>.Success(acknowledgement,claim.ResultStatusCode??200);
            }
            if(!claim.IsClaimed)return Invalid<ImportAcknowledgement>();
            var result=apply(unit);if(!result.Succeeded||result.Value is null)return result;
            if(!authority.Required(unit,action))return Denied<ImportAcknowledgement>();
            authority.Audit(unit,result.Value.BatchId,action,trace);receipts.Complete(unit.Connection,unit.Transaction,claim,"ImportAcknowledged",result.StatusCode,JsonSerializer.Serialize(result.Value,SqlImportBatchStore.Json));
            unit.Transaction.Commit();return result;
        }
        catch(SqlException error) when(error.Number==1205){return Failure<ImportAcknowledgement>("RetryableConflict",409,"The import conflicted. Retry using the same request key.");}
        catch(InvalidOperationException){return Failure<ImportAcknowledgement>("DependencyUnavailable",409,"The source import contribution is unavailable.");}
    }
}
