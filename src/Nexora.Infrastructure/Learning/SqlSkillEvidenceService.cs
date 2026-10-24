using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;

namespace Nexora.Infrastructure.Learning;

public sealed partial class SqlSkillService
{
    private const string EvidenceAction = "learning.skill.evidence";
    private sealed record EvidenceReceipt(int Version, Guid ActorId, Guid OwnerId, Guid SkillId, Guid EvidenceId, bool Remove);

    public IdentityOperationResult<SkillEvidencePage> ListEvidence(IdentityPrincipal actor, Guid skillId, Guid? cursor)
    {
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        if (!Allowed(c, tx, actor, "learning.skill.read")) return Denied<SkillEvidencePage>();
        var skill = Read(c, tx, actor.OwnerId, skillId); if (skill is null) return Missing<SkillEvidencePage>();
        var metadata = new List<(Guid Id, Guid Source, string Type, string? Note)>();
        using (var sql = Command(c, tx, "SELECT TOP(26) Id,ResourceId,ResourceType,Note FROM [learning].[SkillEvidence] WHERE OwnerId=@Owner AND SkillId=@Skill AND (@Cursor IS NULL OR Id>@Cursor) ORDER BY Id;", actor.OwnerId))
        {
            Add(sql, "@Skill", SqlDbType.UniqueIdentifier, skillId); Add(sql, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
            using var reader = sql.ExecuteReader();
            while (reader.Read()) metadata.Add((reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        }
        var items = metadata.Take(25).Select(row =>
        {
            var source = row.Type == "learning.Course" ? SqlCourseService.ReadEvidenceSource(c, tx, actor, row.Source, capabilities) : null;
            return new SkillEvidence(row.Id, skillId, row.Type, row.Source, row.Note,
                source.HasValue ? "Available" : "Unavailable", source?.Title, source?.Status);
        }).ToArray();
        if (!Allowed(c, tx, actor, "learning.skill.read")) return Denied<SkillEvidencePage>();
        // A revoked source action removes private projections, while retaining owned cleanup metadata.
        if (!Allowed(c, tx, actor, "learning.course.read"))
            items = items.Select(item => item with { State = "Unavailable", SourceTitle = null, SourceStatus = null }).ToArray();
        tx.Commit(); return IdentityOperationResult<SkillEvidencePage>.Success(new(items, metadata.Count > 25 ? items[^1].Id : null, skill.ETag));
    }

    public IdentityOperationResult<object?> AddEvidence(IdentityPrincipal actor, Guid skillId, string? etag,
        SkillEvidenceCommand command, string key, string? trace) => EvidenceMutation(actor, skillId, null, etag, command, key, trace);
    public IdentityOperationResult<object?> RemoveEvidence(IdentityPrincipal actor, Guid skillId, Guid evidenceId,
        string? etag, string key, string? trace) => EvidenceMutation(actor, skillId, evidenceId, etag, null, key, trace);

    private IdentityOperationResult<object?> EvidenceMutation(IdentityPrincipal actor, Guid skillId, Guid? evidenceId,
        string? etag, SkillEvidenceCommand? body, string key, string? trace)
    {
        var remove = evidenceId.HasValue;
        if (!Guid.TryParseExact(key, "D", out var parsedKey) || parsedKey == Guid.Empty)
            return Fail<object?>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        if (body is not null && (body.ResourceType != "learning.Course" || body.ResourceId == Guid.Empty || body.Note is { Length: > 1000 }))
            return Fail<object?>("ValidationFailed", 422, "Choose an installed evidence type and a note up to 1000 characters.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        bool Authorized() => Allowed(c, tx, actor, EvidenceAction) && (remove || Allowed(c, tx, actor, "learning.course.read"));
        if (!Authorized()) return Denied<object?>();
        // Shared owner lock order matches the existing Skill writers, including purge.
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<object?>(); }
        var canonical = JsonSerializer.Serialize(new { version = 1, skillId, evidenceId, etag, body });
        var claim = receipts.TryClaim(c, tx, actor.UserId, EvidenceAction + (remove ? ".remove" : ".add"), key, canonical, DateTime.UtcNow);
        if (claim.IsConflict) return Fail<object?>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay)
        {
            EvidenceReceipt? saved = null;
            try { if (claim.ResultJson is not null) saved = JsonSerializer.Deserialize<EvidenceReceipt>(claim.ResultJson); } catch (JsonException) { }
            if (saved is null || saved.Version != 1 || saved.ActorId != actor.UserId || saved.OwnerId != actor.OwnerId || saved.SkillId != skillId
                || saved.EvidenceId == Guid.Empty || saved.Remove != remove || (remove && saved.EvidenceId != evidenceId)
                || claim.ResultStatusCode != (remove ? 204 : 201) || claim.ResultCode != "SkillEvidenceRecorded")
                return Fail<object?>("ReceiptUnavailable", 503, "The completed outcome cannot be safely resolved.");
            if (!Authorized()) return Denied<object?>();
            return remove ? IdentityOperationResult<object?>.Success(null, 204, "SkillEvidenceRecorded")
                : IdentityOperationResult<object?>.Committed(skillId, 201, "SkillEvidenceRecorded");
        }
        if (!claim.IsClaimed) return Fail<object?>("RequestInProgress", 409, "The request is already in progress.");
        var skill = Read(c, tx, actor.OwnerId, skillId); if (skill is null) return Missing<object?>();
        if (string.IsNullOrWhiteSpace(etag)) return Fail<object?>("PreconditionRequired", 428, "If-Match is required.");
        if (skill.ETag != etag) return Fail<object?>("RevisionConflict", 412, "Reload the current Skill revision.");
        if (skill.Status != "Active") return Fail<object?>("LifecycleLocked", 409, "Restore or unarchive the Skill before changing evidence.");
        if (!remove && SqlCourseService.ReadEvidenceSource(c, tx, actor, body!.ResourceId, capabilities) is null) return Missing<object?>();
        var id = evidenceId ?? Guid.NewGuid();
        if (!remove)
        {
            using var duplicate = Command(c, tx, "SELECT COUNT(*) FROM [learning].[SkillEvidence] WHERE OwnerId=@Owner AND SkillId=@Skill AND ResourceId=@Source;", actor.OwnerId);
            Add(duplicate, "@Skill", SqlDbType.UniqueIdentifier, skillId); Add(duplicate, "@Source", SqlDbType.UniqueIdentifier, body!.ResourceId);
            if (Convert.ToInt32(duplicate.ExecuteScalar()) != 0) return Fail<object?>("EvidenceAlreadyLinked", 409, "This source is already linked.");
        }
        using (var sql = Command(c, tx, remove
            ? "DELETE [learning].[SkillEvidence] WHERE OwnerId=@Owner AND SkillId=@Skill AND Id=@Id;"
            : "INSERT [learning].[SkillEvidence](Id,OwnerId,SkillId,ResourceType,ResourceId,Note,CreatedByUserId,UpdatedByUserId) VALUES(@Id,@Owner,@Skill,@Type,@Source,@Note,@User,@User);", actor.OwnerId))
        {
            Add(sql, "@Id", SqlDbType.UniqueIdentifier, id); Add(sql, "@Skill", SqlDbType.UniqueIdentifier, skillId); Add(sql, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            if (!remove) { Add(sql, "@Type", SqlDbType.VarChar, body!.ResourceType, 64); Add(sql, "@Source", SqlDbType.UniqueIdentifier, body.ResourceId); Add(sql, "@Note", SqlDbType.NVarChar, body.Note?.Trim(), 1000); }
            if (sql.ExecuteNonQuery() != 1) return Missing<object?>();
        }
        using (var revision = Command(c, tx, "UPDATE [learning].[Skill] SET UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId))
        { Add(revision, "@Id", SqlDbType.UniqueIdentifier, skillId); Add(revision, "@User", SqlDbType.UniqueIdentifier, actor.UserId); revision.ExecuteNonQuery(); }
        UpdateResource(c, tx, actor, skillId, "Active");
        using (var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.Skill',@Id,'Succeeded',@Trace);", actor.OwnerId))
        {
            Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, EvidenceAction, 160);
            Add(audit, "@Id", SqlDbType.UniqueIdentifier, skillId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        }
        receipts.Complete(c, tx, claim, "SkillEvidenceRecorded", remove ? 204 : 201, JsonSerializer.Serialize(new EvidenceReceipt(1, actor.UserId, actor.OwnerId, skillId, id, remove)));
        if (!Authorized()) return Denied<object?>();
        if (!remove && SqlCourseService.ReadEvidenceSource(c, tx, actor, body!.ResourceId, capabilities) is null) return Missing<object?>();
        tx.Commit();
        return remove ? IdentityOperationResult<object?>.Success(null, 204, "SkillEvidenceRecorded")
            : IdentityOperationResult<object?>.Committed(skillId, 201, "SkillEvidenceRecorded");
    }
}
