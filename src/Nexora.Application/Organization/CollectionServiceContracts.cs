using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Organization;

public sealed record CollectionSummary(Guid Id, string Name, string? Description, string ETag);
public sealed record CollectionPage(IReadOnlyList<CollectionSummary> Items, bool HasMore, int Page);
public sealed record CollectionMember(Guid Id, string ResourceType, Guid ResourceId, string Title, string Status);
public sealed record CollectionDetail(CollectionSummary Collection, IReadOnlyList<CollectionMember> Members);
public sealed record CollectionCandidate(string ResourceType, Guid ResourceId, string Title, string Status);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CollectionWrite(string Name, string? Description);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CollectionAdd(string ResourceType, Guid ResourceId);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CollectionOrder(IReadOnlyList<Guid> MemberIds);
public interface ICollectionService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<CollectionPage> List(IdentityPrincipal actor, string? query, int page);
    IdentityOperationResult<CollectionDetail> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<IReadOnlyList<CollectionCandidate>> Candidates(IdentityPrincipal actor, string type, string? query);
    IdentityOperationResult<CollectionSummary> Create(IdentityPrincipal actor, CollectionWrite body, string key, string? trace);
    IdentityOperationResult<CollectionSummary> Update(IdentityPrincipal actor, Guid id, CollectionWrite body, string etag, string key, string? trace);
    IdentityOperationResult<CollectionSummary> Delete(IdentityPrincipal actor, Guid id, string etag, string key, string? trace);
    IdentityOperationResult<CollectionSummary> Add(IdentityPrincipal actor, Guid id, CollectionAdd body, string etag, string key, string? trace);
    IdentityOperationResult<CollectionSummary> Remove(IdentityPrincipal actor, Guid id, Guid memberId, string etag, string key, string? trace);
    IdentityOperationResult<CollectionSummary> Reorder(IdentityPrincipal actor, Guid id, CollectionOrder body, string etag, string key, string? trace);
}
