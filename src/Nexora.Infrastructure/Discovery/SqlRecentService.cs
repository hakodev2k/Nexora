using System.Data;
using System.Data.SqlTypes;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Discovery;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Discovery;

public sealed class SqlRecentService : IRecentService
{
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private readonly RecentTokens tokens;
    private readonly TimeProvider clock;
    private readonly SqlDiscoverySources sources;
    public SqlRecentService(SqlConnectionFactory connections, string secret, TimeProvider? clock = null)
    {
        this.connections = connections; capabilities = new(connections); receipts = new(secret); tokens = new(secret); this.clock = clock ?? TimeProvider.System;
        sources = new(connections, secret);
    }
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string verb) => capabilities.IsAllowed(c, tx, actor, "FX25", "discovery.recent." + verb);
    public IdentityOperationResult<RecentCapabilities> Capabilities(IdentityPrincipal actor)
    {
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            var read = Allowed(c, tx, actor, "read"); var clear = Allowed(c, tx, actor, "clear");
            read = read && Allowed(c, tx, actor, "read"); clear = clear && Allowed(c, tx, actor, "clear");
            tx.Commit(); return IdentityOperationResult<RecentCapabilities>.Success(new(read, clear));
        }
        catch (SqlException) { return Fail<RecentCapabilities>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<RecentPage> List(IdentityPrincipal actor, string? resourceType = null, string? cursor = null)
    {
        resourceType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType.Trim();
        if (resourceType is not null && !SqlDiscoverySources.Types.Contains(resourceType, StringComparer.Ordinal)) return Fail<RecentPage>("ValidationFailed", 422);
        RecentSeek? after = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            after = tokens.Decode<RecentSeek>("page", cursor);
            if (after is null || after.Version != 1 || after.UserId != actor.UserId || after.OwnerId != actor.OwnerId || after.Type != resourceType || after.Id == Guid.Empty || RecentTokens.Revision(after.Revision) is null)
                return Fail<RecentPage>("InvalidCursor", 422);
        }
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(c, tx, actor, "read")) return Fail<RecentPage>("ActionDenied", 403);
            var rows = new List<Row>();
            using (var sql = Command(c, tx, actor, """
                SELECT TOP(101) Id,ResourceType,ResourceId,LastOpenedAt,RowVersion FROM discovery.RecentItem
                WHERE OwnerId=@Owner AND (@Type IS NULL OR ResourceType=@Type) ORDER BY LastOpenedAt DESC,RowVersion DESC,Id DESC;
                """))
            {
                sql.Parameters.Add("@Type", SqlDbType.VarChar, 32).Value = (object?)resourceType ?? DBNull.Value;
                using var r = sql.ExecuteReader(); while (r.Read()) rows.Add(new(r.GetGuid(0), r.GetString(1), r.GetGuid(2), r.GetDateTime(3), r.GetFieldValue<byte[]>(4)));
            }
            if (rows.Count > 100) return Fail<RecentPage>("PersistenceUnavailable", 503);
            var items = new List<(RecentItem Item, Row Row)>();
            foreach (var row in rows)
            {
                if (after is not null && !IsAfter(row, after)) continue;
                var source = sources.ReadWithin(c, tx, actor, row.Type, row.ResourceId); if (source is null) continue;
                items.Add((new(row.Id, row.Type, row.ResourceId, source.Title, source.Status, new(DateTime.SpecifyKind(row.OpenedAt, DateTimeKind.Utc))), row));
                if (items.Count == 26) break;
            }
            var more = items.Count > 25; if (more) items.RemoveAt(25);
            var next = more ? tokens.Encode("page", new RecentSeek(1, actor.UserId, actor.OwnerId, resourceType, items[^1].Row.OpenedAt, Convert.ToBase64String(items[^1].Row.Revision), items[^1].Row.Id)) : null;
            var clear = Allowed(c, tx, actor, "clear") ? ClearToken(c, tx, actor) : null;
            if (!Allowed(c, tx, actor, "read")) return Fail<RecentPage>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<RecentPage>.Success(new(items.Select(item => item.Item).ToArray(), next, clear));
        }
        catch (Exception error) when (error is SqlException or InvalidOperationException) { return Fail<RecentPage>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<RecentClearPreview> PreviewClear(IdentityPrincipal actor)
    {
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(c, tx, actor, "clear")) return Fail<RecentClearPreview>("ActionDenied", 403);
            var token = ClearToken(c, tx, actor);
            if (!Allowed(c, tx, actor, "clear")) return Fail<RecentClearPreview>("ActionDenied", 403);
            tx.Commit(); return IdentityOperationResult<RecentClearPreview>.Success(new(token));
        }
        catch (SqlException) { return Fail<RecentClearPreview>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<object?> Clear(IdentityPrincipal actor, string token, string key, string? trace)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4096 || (!Guid.TryParseExact(key, "D", out var requestId) && !Guid.TryParseExact(key, "N", out requestId)) || requestId == Guid.Empty)
            return Fail<object?>("ValidationFailed", 422);
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(c, tx, actor, "clear")) return Fail<object?>("ActionDenied", 403);
            var claim = receipts.TryClaim(c, tx, actor.UserId, "discovery.recent.clear", key, JsonSerializer.Serialize(new { actor.OwnerId, token }), clock.GetUtcNow().UtcDateTime);
            if (claim.IsReplay && claim.ResultStatusCode == 204)
                return Allowed(c, tx, actor, "clear") ? IdentityOperationResult<object?>.NoContent("RecentsCleared") : Fail<object?>("ActionDenied", 403);
            if (!claim.IsClaimed) return Fail<object?>(claim.IsInvalid ? "InvalidIdempotencyKey" : claim.IsConflict ? "IdempotencyConflict" : "RequestInProgress", claim.IsInvalid ? 422 : 409);
            var scope = tokens.Decode<RecentClearScope>("clear", token);
            if (scope is null || scope.Version != 1 || scope.UserId != actor.UserId || scope.OwnerId != actor.OwnerId || RecentTokens.Revision(scope.Revision) is not { } revision)
                return Fail<object?>("InvalidPreview", 422);
            if (scope.ExpiresAt <= clock.GetUtcNow()) return Fail<object?>("PreviewExpired", 410);
            if (!SqlRecentActivity.LockOwner(c, tx, actor.OwnerId)) return Fail<object?>("PersistenceUnavailable", 503);
            using (var sql = Command(c, tx, actor, "DELETE FROM discovery.RecentItem WHERE OwnerId=@Owner AND RowVersion<=@Revision;"))
            { sql.Parameters.Add("@Revision", SqlDbType.Binary, 8).Value = revision; sql.ExecuteNonQuery(); }
            using (var audit = Command(c, tx, actor, """
                INSERT security.AuditEvent(ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
                VALUES(@User,@User,'discovery.recent.clear','discovery.RecentItem',@Owner,'Succeeded',@Trace);
                """))
            { audit.Parameters.Add("@Trace", SqlDbType.NVarChar, 200).Value = (object?)trace ?? DBNull.Value; audit.ExecuteNonQuery(); }
            receipts.Complete(c, tx, claim, "RecentsCleared", 204, null);
            if (!Allowed(c, tx, actor, "clear")) return Fail<object?>("ActionDenied", 403);
            if (scope.ExpiresAt <= clock.GetUtcNow()) return Fail<object?>("PreviewExpired", 410);
            tx.Commit(); return IdentityOperationResult<object?>.NoContent("RecentsCleared");
        }
        catch (SqlException) { return Fail<object?>("PersistenceUnavailable", 503); }
    }
    private string ClearToken(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor)
    {
        using var sql = Command(c, tx, actor, "SELECT TOP(1) RowVersion FROM discovery.RecentItem WHERE OwnerId=@Owner ORDER BY RowVersion DESC;");
        var revision = sql.ExecuteScalar() as byte[] ?? new byte[8];
        return tokens.Encode("clear", new RecentClearScope(1, actor.UserId, actor.OwnerId, Convert.ToBase64String(revision), clock.GetUtcNow().AddMinutes(5)));
    }
    private static bool IsAfter(Row row, RecentSeek cursor)
    {
        var time = row.OpenedAt.CompareTo(cursor.LastOpenedAt); if (time != 0) return time < 0;
        var revision = row.Revision.AsSpan().SequenceCompareTo(RecentTokens.Revision(cursor.Revision));
        return revision < 0 || (revision == 0 && new SqlGuid(row.Id).CompareTo(new SqlGuid(cursor.Id)) < 0);
    }
    private static SqlCommand Command(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string text)
    {
        var sql = c.CreateCommand(); sql.Transaction = tx; sql.CommandText = text; sql.CommandTimeout = 5;
        sql.Parameters.Add("@Owner", SqlDbType.UniqueIdentifier).Value = actor.OwnerId; sql.Parameters.Add("@User", SqlDbType.UniqueIdentifier).Value = actor.UserId; return sql;
    }
    private sealed record Row(Guid Id, string Type, Guid ResourceId, DateTime OpenedAt, byte[] Revision);
    private static IdentityOperationResult<T> Fail<T>(string code, int status) => IdentityOperationResult<T>.Failure(code, status, "Recent activity unavailable.");
}
