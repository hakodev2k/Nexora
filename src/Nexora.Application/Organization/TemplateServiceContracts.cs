using System.Text.Json.Serialization;
using Nexora.Application.Identity;

namespace Nexora.Application.Organization;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record BookmarkTemplateSeed(string? Title, string? Url, string? Description);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TemplateWrite(string Name, BookmarkTemplateSeed Seed);
public sealed record TemplateSummary(Guid Id, string Name, string ModuleCode, string ResourceType, int SchemaVersion, string Status, string ETag);
public sealed record TemplateDetail(TemplateSummary Template, BookmarkTemplateSeed Seed, bool CanUse, string? UnavailableReason);
public sealed record TemplatePage(IReadOnlyList<TemplateSummary> Items, bool HasMore, int Page);
public sealed record TemplateDraft(Guid TemplateId, string TemplateETag, BookmarkTemplateSeed Seed, string ModuleCode, string ResourceType);

public static class BookmarkTemplateRules
{
    public static bool TryNormalize(TemplateWrite? body, out TemplateWrite? normalized)
    {
        normalized = null;
        if (body?.Name?.Trim() is not { Length: >= 1 and <= 100 } name || body.Seed is null) return false;
        var title = body.Seed.Title?.Trim() ?? "";
        var url = body.Seed.Url?.Trim() ?? "";
        var description = body.Seed.Description?.Trim();
        if (title.Length > 200 || url.Length > 2048 || description is { Length: > 4000 }) return false;
        if (url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            parsed.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(parsed.Host) || parsed.UserInfo.Length > 0)) return false;
        normalized = new(name, new(title, url, string.IsNullOrEmpty(description) ? null : description));
        return true;
    }
}

public interface ITemplateService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<TemplatePage> List(IdentityPrincipal actor, string? query, string state, int page);
    IdentityOperationResult<TemplateDetail> Get(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<TemplateDraft> Draft(IdentityPrincipal actor, Guid id, string etag);
    IdentityOperationResult<TemplateSummary> Create(IdentityPrincipal actor, TemplateWrite body, string key, string? trace);
    IdentityOperationResult<TemplateSummary> Update(IdentityPrincipal actor, Guid id, TemplateWrite body, string etag, string key, string? trace);
    IdentityOperationResult<TemplateSummary> Transition(IdentityPrincipal actor, Guid id, string action, string etag, string key, string? trace);
}
