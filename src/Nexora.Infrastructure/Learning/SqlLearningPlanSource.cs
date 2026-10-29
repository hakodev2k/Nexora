using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Learning;

// Private protocol for module-owned participants; every call uses the caller's
// Serializable transaction. Source payload state, never registry freshness, is authority.
internal interface ISqlLearningPlanSource
{
    string Kind { get; }
    string Module { get; }
    string ReadAction { get; }
    string RegistryVersion { get; }
    string EligibleSelect { get; }
}
internal sealed class SqlLearningPlanSources(SqlSelfCapability capabilities)
{
    private static readonly ISqlLearningPlanSource[] Participants =
        [new Nexora.Infrastructure.Goals.SqlLearningPlanGoalSource(), new Nexora.Infrastructure.Productivity.SqlLearningPlanTaskSource(), new SqlLearningPlanCourseSource()];
    internal static ISqlLearningPlanSource? Participant(string kind) => Participants.SingleOrDefault(p => p.Kind == kind);
    internal bool CanRead(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, ISqlLearningPlanSource p) =>
        capabilities.IsAllowed(c, tx, a, p.Module, p.ReadAction);
    internal LearningPlanSource Read(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, LearningPlanSourceRef source, bool installRegistry = false)
    {
        var p = Participant(source.Kind);
        if (p is null || source.Id == Guid.Empty || !CanRead(c, tx, a, p)) return Unavailable();
        // SQL text is owned by a fixed module participant; never supplied by the client.
        using var cmd = Command(c, tx, p.EligibleSelect + " AND s.Id=@Id;", a.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, source.Id);
        string? title = null, state = null;
        using (var r = cmd.ExecuteReader()) if (r.Read()) { title = r.GetString(1); state = r.GetString(2); }
        if (title is null || !CanRead(c, tx, a, p)) return Unavailable();
        if (installRegistry && !EnsureRegistry(c, tx, a, p, source.Id)) return Unavailable();
        if (!RegistryMatches(c, tx, a.OwnerId, p, source.Id)) return Unavailable();
        // No cross-module progress formula is introduced by this consumer.
        return new("NoProgressMetric", title, state, null);
    }
    internal List<(Guid Id, string Title, DateTime Updated)> Choices(SqlConnection c, SqlTransaction tx, IdentityPrincipal a,
        ISqlLearningPlanSource p, LearningPlanCursor.Boundary? before)
    {
        var result = new List<(Guid, string, DateTime)>();
        if (!CanRead(c, tx, a, p)) return result;
        using var cmd = Command(c, tx, "SELECT TOP(26) q.Id,q.Title,q.UpdatedAt FROM (" + p.EligibleSelect + ") q " +
            "WHERE (@Before IS NULL OR q.UpdatedAt<@Before OR (q.UpdatedAt=@Before AND q.Id<@BeforeId)) ORDER BY q.UpdatedAt DESC,q.Id DESC;", a.OwnerId);
        Add(cmd, "@Before", SqlDbType.DateTime2, before?.UpdatedAt); Add(cmd, "@BeforeId", SqlDbType.UniqueIdentifier, before?.Id);
        using (var reader = cmd.ExecuteReader()) while (reader.Read()) result.Add((reader.GetGuid(0), reader.GetString(1), reader.GetDateTime(2)));
        return CanRead(c, tx, a, p) ? result : [];
    }
    private static bool RegistryMatches(SqlConnection c, SqlTransaction tx, Guid owner, ISqlLearningPlanSource p, Guid id)
    {
        using var cmd = Command(c, tx, """
            SELECT 1 FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId
            JOIN [platform].[Module] m ON m.Id=rt.ModuleId
            WHERE r.OwnerId=@Owner AND r.Id=@Id AND m.Code=@Module AND rt.Code=@Kind AND rt.ContractVersion=@Version AND r.PurgedAt IS NULL;
            """, owner);
        RegistryParameters(cmd, p, id); return cmd.ExecuteScalar() is not null;
    }
    private static bool EnsureRegistry(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, ISqlLearningPlanSource p, Guid id)
    {
        // Goal/Task lack native registry rows at the source revision. Their module
        // participant can enroll an already-proven source for reference integrity.
        // Never relabel an existing identity, backfill data, or change source state.
        if (p.Kind == "Course") return RegistryMatches(c, tx, a.OwnerId, p, id);
        using var cmd = Command(c, tx, """
            IF NOT EXISTS(SELECT 1 FROM [platform].[Resource] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Id)
              INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
              SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
              JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code=@Module AND rt.Code=@Kind AND rt.ContractVersion=@Version;
            """, a.OwnerId);
        RegistryParameters(cmd, p, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); cmd.ExecuteNonQuery();
        return RegistryMatches(c, tx, a.OwnerId, p, id);
    }
    private static void RegistryParameters(SqlCommand cmd, ISqlLearningPlanSource p, Guid id)
    { Add(cmd,"@Id",SqlDbType.UniqueIdentifier,id); Add(cmd,"@Module",SqlDbType.VarChar,p.Module,16); Add(cmd,"@Kind",SqlDbType.VarChar,p.Kind,64); Add(cmd,"@Version",SqlDbType.VarChar,p.RegistryVersion,64); }
    internal static LearningPlanSource Unavailable() => new("Unavailable", null, null, null);
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, string sql, Guid owner)
    { var cmd=c.CreateCommand(); cmd.Transaction=tx; cmd.CommandText=sql; Add(cmd,"@Owner",SqlDbType.UniqueIdentifier,owner); return cmd; }
    private static void Add(SqlCommand cmd,string name,SqlDbType type,object? value,int size=0)
    { var p=cmd.Parameters.Add(name,type); if(size!=0) p.Size=size; p.Value=value??DBNull.Value; }
}
