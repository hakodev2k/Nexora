using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Documents;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Documents;

public static class DocumentEndpoints
{
    public static WebApplication MapDocumentEndpoints(this WebApplication app)
    {
        // The domain limit is 1 MiB UTF-8 for the body. JSON quoting and the
        // title/change-note envelope need bounded headroom; keep this local to
        // the document group instead of weakening the global 64 KiB default.
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods(8 * 1024 * 1024);

        api.MapGet("/documents", (HttpContext context, string? status, int? limit, string? cursor, string? query, string? documentType, DateOnly? createdFrom, DateOnly? createdTo, Guid? tagId, Guid? folderId, Guid? parentPageId, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, status, limit, cursor, query, documentType, createdFrom, createdTo, tagId, folderId, parentPageId), value =>
                    new DocumentPageResponse(value.Items.Select(ToSummary).ToArray(), value.NextCursor)))
            .WithName("listDocuments");

        api.MapGet("/documents/{documentId:guid}", (HttpContext context, Guid documentId, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, documentId), ToResponse))
            .WithName("getDocument");

        api.MapGet("/documents/{documentId:guid}/versions", (HttpContext context, Guid documentId, string? cursor,
            IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.History(principal, documentId, cursor), value =>
                    new DocumentVersionsResponse(value.Items.Select(ToVersionResponse).ToArray(), value.NextCursor)))
            .WithName("listDocumentVersions");

        api.MapGet("/documents/{documentId:guid}/versions/{versionNumber:long}", (HttpContext context, Guid documentId, long versionNumber,
            IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Version(principal, documentId, versionNumber), value =>
                    new DocumentVersionPreviewResponse(ToVersionResponse(value.Snapshot), value.Body)))
            .WithName("getDocumentVersion");

        api.MapPost("/documents", (HttpContext context, DocumentCreateRequest request, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal,
                    new DocumentCreateCommand(request.Title, request.DocumentType, request.EditorMode, request.Body, request.TagId, request.FolderId, request.ParentPageId),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createDocument");

        api.MapGet("/documents/{documentId:guid}/versions/compare", (HttpContext context, Guid documentId, long before, long after,
            IDocumentVersionService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Compare(principal, documentId, before, after), value => new DocumentVersionComparisonResponse(
                    new(ToVersionResponse(value.Before.Snapshot), value.Before.Body), new(ToVersionResponse(value.After.Snapshot), value.After.Body))))
            .WithName("compareDocumentVersions");

        api.MapPost("/documents/{documentId:guid}/versions/restore", (HttpContext context, Guid documentId,
            DocumentRestoreVersionRequest request, IDocumentVersionService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.RestoreVersion(principal, documentId, context.Request.Headers.IfMatch.ToString(),
                    new(request.VersionNumber, request.Tag is null ? null : new DocumentTagChange(request.Tag.Id), request.ChangeNote),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("restoreDocumentVersion");

        api.MapPut("/documents/{documentId:guid}", (HttpContext context, Guid documentId, DocumentSaveRequest request,
            IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Save(principal, documentId, context.Request.Headers.IfMatch.ToString(),
                    new DocumentSaveCommand(request.Title, request.Body, request.ChangeNote, request.Tag is null ? null : new DocumentTagChange(request.Tag.Id)), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("saveDocument");

        api.MapPost("/documents/{documentId:guid}/transition", (HttpContext context, Guid documentId,
            DocumentTransitionRequest request, IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, documentId, context.Request.Headers.IfMatch.ToString(),
                    new DocumentTransitionCommand(request.Status), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionDocument");

        return app;
    }

    private static DocumentVersionResponse ToVersionResponse(DocumentVersionSummary value) => new(
        value.Id, value.PageId, value.VersionNumber, value.Title, value.DocumentType, value.EditorMode, value.SavedStatus,
        value.ChangeNote, value.CreatedAt, value.CreatedByUserId, value.TagSnapshotCaptured, value.TagId, value.TagName);

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Acknowledgement is { } acknowledgement)
            return new ApiResult<CommittedResourceAcknowledgement>(true, acknowledgement, result.Code, result.StatusCode, result.Title).ToHttp(context);
        if (result.Succeeded && result.Value is DocumentDetail document)
            context.Response.Headers.ETag = document.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static DocumentSummaryResponse ToSummary(DocumentSummary value) => new(
        value.Id, value.Title, value.DocumentType, value.EditorMode, value.Status, value.PreArchiveStatus, value.VersionNumber,
        value.CreatedAt, value.UpdatedAt, value.ETag, value.TagId, value.TagName, value.FolderId, value.ParentPageId, value.Depth);

    private static DocumentResponse ToResponse(DocumentDetail value) => new(
        value.Id, value.Title, value.DocumentType, value.EditorMode, value.Body, value.Status, value.PreArchiveStatus, value.VersionNumber,
        value.CreatedAt, value.UpdatedAt, value.ETag, value.TagId, value.TagName, value.FolderId, value.ParentPageId, value.Depth);
}
