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
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/documents", (HttpContext context, string? status, int? limit, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, status, limit), value =>
                    new DocumentPageResponse(value.Items.Select(ToSummary).ToArray(), value.NextCursor)))
            .WithName("listDocuments");

        api.MapGet("/documents/{documentId:guid}", (HttpContext context, Guid documentId, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, documentId), ToResponse))
            .WithName("getDocument");

        api.MapPost("/documents", (HttpContext context, DocumentCreateRequest request, IDocumentService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal,
                    new DocumentCreateCommand(request.Title, request.DocumentType, request.EditorMode, request.Body),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createDocument");

        api.MapPut("/documents/{documentId:guid}", (HttpContext context, Guid documentId, DocumentSaveRequest request,
            IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Save(principal, documentId, context.Request.Headers.IfMatch.ToString(),
                    new DocumentSaveCommand(request.Title, request.Body, request.ChangeNote), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("saveDocument");

        api.MapPost("/documents/{documentId:guid}/transition", (HttpContext context, Guid documentId,
            DocumentTransitionRequest request, IDocumentService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, documentId, context.Request.Headers.IfMatch.ToString(),
                    new DocumentTransitionCommand(request.Status), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionDocument");

        return app;
    }

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
        value.CreatedAt, value.UpdatedAt, value.ETag);

    private static DocumentResponse ToResponse(DocumentDetail value) => new(
        value.Id, value.Title, value.DocumentType, value.EditorMode, value.Body, value.Status, value.PreArchiveStatus, value.VersionNumber,
        value.CreatedAt, value.UpdatedAt, value.ETag);
}
