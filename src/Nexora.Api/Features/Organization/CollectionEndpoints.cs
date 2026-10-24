using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Organization;

namespace Nexora.Api.Features.Organization;

public static class CollectionEndpoints
{
    public static WebApplication MapCollectionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/organization/collections").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Capabilities(a))).WithName("collectionCapabilities");
        api.MapGet("/candidates", (HttpContext c, string resourceType, string? query, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Candidates(a, resourceType, query))).WithName("collectionCandidates");
        api.MapGet("", (HttpContext c, string? query, int? page, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.List(a, query, page ?? 1))).WithName("listCollections");
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Get(a, id))).WithName("getCollection");
        api.MapPost("", (HttpContext c, CollectionWrite body, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Create(a, body, Key(c), c.TraceIdentifier))).WithName("createCollection");
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, CollectionWrite body, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Update(a, id, body, ETag(c), Key(c), c.TraceIdentifier))).WithName("updateCollection");
        api.MapDelete("/{id:guid}", (HttpContext c, Guid id, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Delete(a, id, ETag(c), Key(c), c.TraceIdentifier))).WithName("deleteCollection");
        api.MapPost("/{id:guid}/members", (HttpContext c, Guid id, CollectionAdd body, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Add(a, id, body, ETag(c), Key(c), c.TraceIdentifier))).WithName("addCollectionMember");
        api.MapDelete("/{id:guid}/members/{memberId:guid}", (HttpContext c, Guid id, Guid memberId, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Remove(a, id, memberId, ETag(c), Key(c), c.TraceIdentifier))).WithName("removeCollectionMember");
        api.MapPut("/{id:guid}/members/order", (HttpContext c, Guid id, CollectionOrder body, ICollectionService s, IIdentityService i, SessionCookieService cookies) =>
            Run(c, i, cookies, a => s.Reorder(a, id, body, ETag(c), Key(c), c.TraceIdentifier))).WithName("reorderCollectionMembers");
        return app;
    }

    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static string ETag(HttpContext c) => c.Request.Headers.IfMatch.ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> run)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        if (!auth.Succeeded || auth.Value is null) return new ApiResult<object>(false, null, auth.Code, auth.StatusCode, auth.Title).ToHttp(c);
        var r = run(auth.Value);
        c.Response.Headers.CacheControl = "no-store";
        if (r.Acknowledgement is { } ack) return new ApiResult<CommittedResourceAcknowledgement>(true, ack, r.Code, r.StatusCode, r.Title).ToHttp(c);
        if (r.Value is CollectionDetail detail) c.Response.Headers.ETag = detail.Collection.ETag;
        return new ApiResult<T>(r.Succeeded, r.Value, r.Code, r.StatusCode, r.Title).ToHttp(c);
    }
}
