using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SkillEvidenceCommand(string ResourceType, Guid ResourceId, string? Note);
public sealed record SkillEvidence(Guid Id, Guid SkillId, string ResourceType, Guid ResourceId, string? Note,
    string State, string? SourceTitle, string? SourceStatus);
public sealed record SkillEvidencePage(IReadOnlyList<SkillEvidence> Items, Guid? NextCursor, string SkillETag);
public interface ISkillEvidenceService
{
    IdentityOperationResult<SkillEvidencePage> ListEvidence(IdentityPrincipal actor, Guid skillId, Guid? cursor);
    IdentityOperationResult<object?> AddEvidence(IdentityPrincipal actor, Guid skillId, string? etag, SkillEvidenceCommand command, string key, string? trace);
    IdentityOperationResult<object?> RemoveEvidence(IdentityPrincipal actor, Guid skillId, Guid evidenceId, string? etag, string key, string? trace);
}
