using Nexora.Application.Identity;
namespace Nexora.Application.Assets;

public sealed record PersonalAsset(Guid Id, string Title, string Kind, string? Brand, string? Model,
    string? Category, string? Notes, string State, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record PersonalAssetPage(IReadOnlyList<PersonalAsset> Items, Guid? NextCursor);
public sealed record PersonalAssetCreate(string Title, string Kind, string State, string? Brand = null,
    string? Model = null, string? Category = null, string? Notes = null);
public sealed record PersonalAssetMetadata(string Title, string Kind, string? Brand = null,
    string? Model = null, string? Category = null, string? Notes = null);
public sealed record PersonalAssetState(string State, string? Reason = null);
public sealed record PersonalAssetAcknowledgement(Guid ItemId, string? ETag);
public sealed record PersonalAssetPreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record PersonalAssetConfirmation(bool Confirm);
public sealed record PersonalAssetSnapshotFields(string Title, string Kind, string? Brand, string? Model,
    string? Category, string? Notes, string State, string? PreArchiveState, string? PreTrashState);
public sealed record PersonalAssetSnapshot(int SchemaVersion, string ResourceType, PersonalAssetSnapshotFields Fields,
    IReadOnlyList<object> Relations, IReadOnlyList<Guid> EvidenceFileIds);
public sealed record PersonalAssetVersion(Guid Id, long VersionNumber, string ActionKey, DateTimeOffset CreatedAt,
    Guid? CreatedByUserId, string? Reason, PersonalAssetSnapshot Snapshot);
public sealed record PersonalAssetHistoryPage(IReadOnlyList<PersonalAssetVersion> Items, Guid? NextCursor);
public interface IPersonalAssetService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<PersonalAssetPage> List(IdentityPrincipal actor, Guid? cursor, string? state, string? kind, string? category, string? query);
    IdentityOperationResult<PersonalAsset> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<PersonalAssetHistoryPage> History(IdentityPrincipal actor, Guid id, Guid? cursor, string? action, string? from, string? to, string? version);
    IdentityOperationResult<PersonalAssetAcknowledgement> Create(IdentityPrincipal actor, PersonalAssetCreate body, string key, string? trace);
    IdentityOperationResult<PersonalAssetAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, PersonalAssetMetadata body, string key, string? trace);
    IdentityOperationResult<PersonalAssetAcknowledgement> SetState(IdentityPrincipal actor, Guid id, string? etag, PersonalAssetState body, string key, string? trace);
    IdentityOperationResult<PersonalAssetPreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<PersonalAssetAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag, string operation, PersonalAssetConfirmation body, string key, string? trace);
}
