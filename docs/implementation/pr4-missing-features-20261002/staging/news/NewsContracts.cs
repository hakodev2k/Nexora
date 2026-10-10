using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.News;

public sealed record NewsCategoryMetadata([property: JsonRequired] int SchemaVersion, [property: JsonRequired] string Name);
public sealed record NewsCategoryCommand([property: JsonRequired] NewsCategoryMetadata Metadata);
public sealed record NewsCategoryRecord(Guid Id, NewsCategoryMetadata Metadata, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string ETag);
public sealed record NewsCategoryPage(IReadOnlyList<NewsCategoryRecord> Items, Guid? NextCursor);
public sealed record NewsCategoryAcknowledgement(Guid Id, string ETag);
public interface INewsCategoryService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<NewsCategoryPage> List(IdentityPrincipal actor, Guid? cursor, string? query);
    IdentityOperationResult<NewsCategoryRecord> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<NewsCategoryAcknowledgement> Create(IdentityPrincipal actor, NewsCategoryCommand body, string key, string? trace);
    IdentityOperationResult<NewsCategoryAcknowledgement> Update(IdentityPrincipal actor, Guid id, string? etag, NewsCategoryCommand body, string key, string? trace);
}
