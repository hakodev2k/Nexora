using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Files;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Files;

public static class FileEndpoints
{
    public static WebApplication MapFileEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();
        var uploadApi = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods(25 * 1024 * 1024);

        api.MapGet("/files", (HttpContext context, int? limit, IFileService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, limit), value =>
                    new FilePageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listFiles");

        api.MapGet("/files/{fileId:guid}", (HttpContext context, Guid fileId, IFileService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, fileId), ToResponse))
            .WithName("getFile");

        api.MapPost("/files/upload-sessions", (HttpContext context, FileUploadRequest request, IFileService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.InitiateUpload(principal, new FileUploadCommand(request.OriginalName, request.MediaType, request.ExpectedBytes),
                    IdempotencyKey(context), context.TraceIdentifier), value => ToResponse(value)))
            .WithName("initiateFileUpload");

        uploadApi.MapPut("/files/upload-sessions/{uploadSessionId:guid}/content", async (HttpContext context,
            Guid uploadSessionId, IFileService service, IIdentityService identity, SessionCookieService cookies) =>
        {
            var auth = identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
            if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
            var handle = context.Request.Headers["X-Upload-Handle"].ToString();
            var result = await service.CompleteUploadAsync(auth.Value, uploadSessionId, handle, context.Request.Body,
                context.Request.ContentLength, IdempotencyKey(context), context.TraceIdentifier, context.RequestAborted);
            if (!result.Succeeded || result.Value is null) return ToHttp(context, result);
            context.Response.Headers.ETag = result.Value.ETag;
            return ToHttp(context, IdentityOperationResult<FileResponse>.Success(ToResponse(result.Value), result.StatusCode, result.Code));
        }).WithName("completeFileUpload");

        api.MapDelete("/files/upload-sessions/{uploadSessionId:guid}", (HttpContext context, Guid uploadSessionId,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.CancelUpload(principal, uploadSessionId, context.Request.Headers["X-Upload-Handle"].ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("cancelFileUpload");

        api.MapPatch("/files/{fileId:guid}", (HttpContext context, Guid fileId, FileRenameRequest request,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Rename(principal, fileId, context.Request.Headers.IfMatch.ToString(),
                    new FileRenameCommand(request.OriginalName), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("renameFile");

        api.MapGet("/files/{fileId:guid}/content", (HttpContext context, Guid fileId, bool? inline,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
        {
            var auth = identity.GetPrincipal(cookies.ReadRawHandle(context.Request));
            if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
            var result = service.OpenContent(auth.Value, fileId, inline ?? false);
            if (!result.Succeeded || result.Value is null) return ToHttp(context, result);
            context.Response.Headers.CacheControl = "no-store";
            return Results.File(result.Value.Content, result.Value.MediaType, result.Value.DownloadName,
                enableRangeProcessing: false);
        }).WithName("downloadFile");

        api.MapPost("/files/{fileId:guid}/references", (HttpContext context, Guid fileId, FileReferenceRequest request,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Attach(principal, fileId, new FileReferenceCommand(request.ResourceType, request.ResourceId,
                    request.VersionNumber, request.Purpose, request.ReferenceKey), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("attachFile");

        api.MapDelete("/files/references/{referenceId:guid}", (HttpContext context, Guid referenceId,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Detach(principal, referenceId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("detachFile");

        api.MapPost("/files/{fileId:guid}/trash", (HttpContext context, Guid fileId, IFileService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Trash(principal, fileId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("trashFile");

        api.MapPost("/files/{fileId:guid}/restore", (HttpContext context, Guid fileId, FileRestoreRequest request,
            IFileService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Restore(principal, fileId, request.DeletionBatchId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("restoreFile");

        api.MapPost("/files/{fileId:guid}/purge", (HttpContext context, Guid fileId, IFileService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Purge(principal, fileId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("purgeFile");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
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
        if (result.Succeeded && result.Value is FileRecord file) context.Response.Headers.ETag = file.ETag;
        if (result.Succeeded && result.Value is FileReferenceRecord reference) context.Response.Headers.ETag = reference.ETag;
        if (result.Succeeded && result.Value is FileUploadSessionRecord upload) context.Response.Headers.ETag = upload.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static FileResponse ToResponse(FileRecord value) => new(value.Id, value.OriginalName, value.MediaType, value.ByteLength,
        value.ScanState, value.Lifecycle, value.CurrentRevision, value.CreatedAt, value.UpdatedAt, value.ETag);

    private static FileUploadSessionResponse ToResponse(FileUploadSessionRecord value) => new(value.Id, value.ExpectedBytes,
        value.ReceivedBytes, value.State, value.ExpiresAt, value.UploadHandle, value.ETag);

    private static FileReferenceResponse ToResponse(FileReferenceRecord value) => new(value.Id, value.FileObjectId, value.ResourceType,
        value.ResourceId, value.VersionNumber, value.Purpose, value.ReferenceKey, value.CreatedAt, value.ETag);

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
