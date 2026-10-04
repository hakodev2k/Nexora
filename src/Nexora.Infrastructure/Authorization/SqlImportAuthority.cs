using System.Data;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Transfer;

namespace Nexora.Infrastructure.Authorization;

internal sealed class SqlImportAuthority(SqlConnectionFactory connections,TimeProvider clock)
{
    private readonly SqlSelfCapability capabilities=new(connections);
    internal string? AccountZone(SqlImportUnit unit)
    {
        using var command=unit.Command("""
            SELECT u.TimeZoneId FROM [platform].[PersonalSpace] p WITH(UPDLOCK,HOLDLOCK)
            JOIN [identity].[User] u ON u.Id=p.UserId
            JOIN [identity].[Session] s ON s.UserId=u.Id AND s.Id=@Session
            WHERE p.Id=@Owner AND p.UserId=@User AND p.State='Active' AND u.State='Active'
              AND u.IsDeleted=0 AND u.EmailConfirmed=1 AND s.RevokedAt IS NULL
              AND s.IdleExpiresAt>@Now AND s.AbsoluteExpiresAt>@Now AND s.SecurityStamp=u.SecurityStamp;
            """);
        command.Parameters["@Now"].Value=clock.GetUtcNow().UtcDateTime;
        SqlImportUnit.Add(command,"@Session",SqlDbType.UniqueIdentifier,unit.Actor.SessionId);
        return command.ExecuteScalar() as string;
    }
    internal bool Allowed(SqlImportUnit unit,string module,string action)=>capabilities.IsAllowed(unit.Connection,unit.Transaction,unit.Actor,module,action);
    internal bool Required(SqlImportUnit unit,string action)=>AccountZone(unit) is not null &&
        Allowed(unit,"FX10",action) && Allowed(unit,"FX13",action=="transfer.import.preview"?"calendar.ics.preview":"calendar.ics.import") &&
        Allowed(unit,"FX13","calendar.event.create") && Allowed(unit,"FX07","files.file.read") && Allowed(unit,"FX07","files.file.download");
    internal Guid RegisterEvent(SqlImportUnit unit,Guid id)
    {
        using var command=unit.Command("""
            INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,t.Id,'Active',1,@Now,@Now,@User,@User FROM [platform].[ResourceType] t
            JOIN [platform].[Module] m ON m.Id=t.ModuleId WHERE m.Code='FX13' AND t.Code='ManualEvent';
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);
        if(command.ExecuteNonQuery()!=1)throw new InvalidOperationException("Calendar resource contribution is unavailable.");return id;
    }
    internal void Audit(SqlImportUnit unit,Guid id,string action,string? trace,string targetType="ImportBatch")
    {
        using var command=unit.Command("""
            INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
            VALUES(@User,@User,@Action,@TargetType,@Id,'Succeeded',@Trace);
            """);
        SqlImportUnit.Add(command,"@Id",SqlDbType.UniqueIdentifier,id);SqlImportUnit.Add(command,"@Action",SqlDbType.NVarChar,action,160);SqlImportUnit.Add(command,"@Trace",SqlDbType.NVarChar,trace,100);SqlImportUnit.Add(command,"@TargetType",SqlDbType.NVarChar,targetType,100);command.ExecuteNonQuery();
    }
}
