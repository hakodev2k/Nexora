using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Productivity;

namespace Nexora.Infrastructure.Transfer;

internal sealed class SqlExportJobStore
{
    private const string Columns = "Id,State,EventCount,FilterJson,TimeZoneId,CreatedAt,ExpiresAt,RowVersion";
    internal CalendarExportJob? Get(SqlImportUnit unit, Guid id)
    {
        using var command = unit.Command($"SELECT {Columns} FROM [operations].[ExportJob] WHERE OwnerId=@Owner AND Id=@Id AND Format='ICS';");
        SqlImportUnit.Add(command, "@Id", SqlDbType.UniqueIdentifier, id); using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }
    internal IReadOnlyList<CalendarExportJob>? List(SqlImportUnit unit)
    {
        using var command = unit.Command($"SELECT TOP(10001) {Columns} FROM [operations].[ExportJob] WHERE OwnerId=@Owner AND Format='ICS' ORDER BY CreatedAt DESC,Id DESC;");
        var result = new List<CalendarExportJob>(); using var reader = command.ExecuteReader();
        while (reader.Read()) { if (result.Count == 10000) return null; result.Add(Read(reader)); } return result;
    }
    internal IReadOnlyList<ExportSourceIdentity> Sources(SqlImportUnit unit, Guid id)
    {
        using var command = unit.Command("SELECT EventId,SourceKind,TaskId,ProjectId FROM [operations].[ExportSource] WHERE OwnerId=@Owner AND JobId=@Id ORDER BY EventId;");
        SqlImportUnit.Add(command, "@Id", SqlDbType.UniqueIdentifier, id); using var reader = command.ExecuteReader(); var result = new List<ExportSourceIdentity>();
        while (reader.Read()) result.Add(new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.IsDBNull(3) ? null : reader.GetGuid(3))); return result;
    }
    internal byte[]? Content(SqlImportUnit unit, Guid id)
    {
        using var command = unit.Command("SELECT Content FROM [operations].[ExportArtifact] WHERE OwnerId=@Owner AND JobId=@Id;");
        SqlImportUnit.Add(command, "@Id", SqlDbType.UniqueIdentifier, id); return command.ExecuteScalar() as byte[];
    }
    internal Guid Create(SqlImportUnit unit, CalendarExportFilter filter, string zone, CalendarExportSnapshot snapshot, byte[] content)
    {
        var id = Guid.NewGuid(); using var command = unit.Command("""
            INSERT [operations].[ExportJob](Id,OwnerId,ModuleId,Format,FilterJson,State,FileObjectId,ExpiresAt,SourceWatermark,TimeZoneId,EventCount,CreatedAt,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,Id,'ICS',@Filter,'Ready',NULL,@Expiry,@Watermark,@Zone,@Count,@Now,@Now,@User,@User FROM [platform].[Module] WHERE Code='FX13';
            INSERT [operations].[ExportArtifact](OwnerId,JobId,Content) VALUES(@Owner,@Id,@Content);
            """);
        SqlImportUnit.Add(command, "@Id", SqlDbType.UniqueIdentifier, id); SqlImportUnit.Add(command, "@Filter", SqlDbType.NVarChar, JsonSerializer.Serialize(filter, SqlImportBatchStore.Json), 4000);
        SqlImportUnit.Add(command, "@Expiry", SqlDbType.DateTime2, unit.Now.AddMinutes(15)); SqlImportUnit.Add(command, "@Watermark", SqlDbType.NVarChar, snapshot.Digest, 200);
        SqlImportUnit.Add(command, "@Zone", SqlDbType.NVarChar, zone, 128); SqlImportUnit.Add(command, "@Count", SqlDbType.Int, snapshot.Events.Count);
        SqlImportUnit.Add(command, "@Content", SqlDbType.VarBinary, content, -1); command.ExecuteNonQuery();
        foreach (var source in snapshot.Sources)
        {
            using var insert = unit.Command("INSERT [operations].[ExportSource](OwnerId,JobId,EventId,SourceKind,TaskId,ProjectId) VALUES(@Owner,@Job,@Event,@Kind,@Task,@Project);");
            SqlImportUnit.Add(insert, "@Job", SqlDbType.UniqueIdentifier, id); SqlImportUnit.Add(insert, "@Event", SqlDbType.UniqueIdentifier, source.EventId);
            SqlImportUnit.Add(insert, "@Kind", SqlDbType.VarChar, source.SourceKind, 16); SqlImportUnit.Add(insert, "@Task", SqlDbType.UniqueIdentifier, source.TaskId);
            SqlImportUnit.Add(insert, "@Project", SqlDbType.UniqueIdentifier, source.ProjectId); insert.ExecuteNonQuery();
        }
        return id;
    }
    private static CalendarExportJob Read(SqlDataReader reader) => new(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2),
        JsonSerializer.Deserialize<CalendarExportFilter>(reader.GetString(3), SqlImportBatchStore.Json)!, reader.GetString(4), Utc(reader.GetDateTime(5)), Utc(reader.GetDateTime(6)), SqlImportUnit.ETag(reader.GetFieldValue<byte[]>(7)));
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
