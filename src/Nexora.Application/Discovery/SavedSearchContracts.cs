using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Discovery;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SavedSearchDefinition(string Text, string? ResourceType = null, DateOnly? From = null,
    DateOnly? To = null, bool IncludeArchived = false, string Sort = "relevance");
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SavedSearchCommand(string Title, int SchemaVersion, SavedSearchDefinition Query);
public sealed record SavedSearchRecord(Guid Id, string Title, int SchemaVersion, SavedSearchDefinition? Query, DateTimeOffset UpdatedAt, string ETag,
    string State = "Compatible");
public sealed record SavedSearchPage(IReadOnlyList<SavedSearchRecord> Items, string? NextCursor);
public interface ISavedSearchService
{
    IdentityOperationResult<SavedSearchPage> List(IdentityPrincipal actor, string? cursor = null);
    IdentityOperationResult<SavedSearchRecord> Save(IdentityPrincipal actor, Guid? id, string? ifMatch, SavedSearchCommand command, string? key = null, string? traceId = null);
    IdentityOperationResult<object?> Delete(IdentityPrincipal actor, Guid id, string? ifMatch, string? key = null, string? traceId = null);
    IdentityOperationResult<SearchPage> Run(IdentityPrincipal actor, Guid id, string? cursor = null);
}
