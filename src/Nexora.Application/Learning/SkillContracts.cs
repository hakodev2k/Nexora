using Nexora.Application.Identity;

namespace Nexora.Application.Learning;

public sealed record Skill(Guid Id, string Title, string Level, string? Description, string? Category,
    DateOnly? LastUsed, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record SkillPage(IReadOnlyList<Skill> Items, Guid? NextCursor);
public sealed record SkillCreate(string Title, string Level, string? Description = null, string? Category = null, DateOnly? LastUsed = null);
public sealed record SkillMetadata(string Title, string? Description = null, string? Category = null, DateOnly? LastUsed = null);
public sealed record SkillProficiency(string Level);
public sealed record SkillAcknowledgement(Guid ItemId, string? ETag);
public sealed record SkillPreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record SkillConfirmation(bool Confirm);

public interface ISkillService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<SkillPage> List(IdentityPrincipal actor, Guid? cursor, string? status, string? query);
    IdentityOperationResult<Skill> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<SkillAcknowledgement> Create(IdentityPrincipal actor, SkillCreate body, string key, string? trace);
    IdentityOperationResult<SkillAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, SkillMetadata body, string key, string? trace);
    IdentityOperationResult<SkillAcknowledgement> Proficiency(IdentityPrincipal actor, Guid id, string? etag, SkillProficiency body, string key, string? trace);
    IdentityOperationResult<SkillPreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<SkillAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, SkillConfirmation body, string key, string? trace);
}
