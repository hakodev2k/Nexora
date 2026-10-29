using System.Data;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Organization;
using Nexora.Infrastructure.Authorization;

namespace Nexora.Infrastructure.Organization;

// Implementations live with source owners; SQL is never derived from request types.
internal abstract class CollectionSourceParticipant(SqlSelfCapability capabilities)
{
    internal abstract string Type { get; }
    protected abstract string Module { get; }
    protected abstract string Action { get; }
    protected abstract string Query { get; }
    internal bool Available(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor) => capabilities.IsAllowed(c, tx, actor, Module, Action);

    internal IReadOnlyList<CollectionCandidate> Read(SqlConnection c, SqlTransaction tx,
        IdentityPrincipal actor, Guid? id, string? query = null)
    {
        if (!Available(c, tx, actor)) return [];
        using var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandTimeout = 5;
        sql.CommandText = Query;
        sql.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId;
        sql.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = (object?)id ?? DBNull.Value;
        var literal = (query ?? "").Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        sql.Parameters.Add("@Query", SqlDbType.NVarChar, 204).Value = "%" + literal + "%";
        var rows = new List<CollectionCandidate>();
        using (var r = sql.ExecuteReader()) while (r.Read()) rows.Add(new(Type, r.GetGuid(0), r.GetString(1), r.GetString(2)));
        return Available(c, tx, actor) ? rows : [];
    }
}
