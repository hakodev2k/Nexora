using Nexora.Application.Identity;

namespace Nexora.Application.Shopping;

public sealed record WishlistItem(Guid Id, string Title, string? Url, string Quantity,
    string? TargetAmount, string? Currency, string? Notes, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, string ETag);
public sealed record WishlistPage(IReadOnlyList<WishlistItem> Items, Guid? NextCursor);
public sealed record WishlistCommand(string Title, string Quantity, string? Url = null,
    string? TargetAmount = null, string? Currency = null, string? Notes = null);
public sealed record WishlistAcknowledgement(Guid ItemId, string? ETag);
public sealed record WishlistPreview(Guid ItemId, string ETag, string Operation, int ReferenceCount);
public sealed record WishlistConfirmation(bool Confirm);

public interface IWishlistService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<WishlistPage> List(IdentityPrincipal actor, Guid? cursor, string? status, string? query);
    IdentityOperationResult<WishlistItem> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<WishlistAcknowledgement> Save(IdentityPrincipal actor, Guid? id, string? etag,
        WishlistCommand body, string key, string? trace);
    IdentityOperationResult<WishlistPreview> Preview(IdentityPrincipal actor, Guid id, string operation);
    IdentityOperationResult<WishlistAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, WishlistConfirmation body, string key, string? trace);
}
