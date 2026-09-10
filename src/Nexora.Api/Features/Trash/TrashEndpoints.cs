using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Trash;

namespace Nexora.Api.Features.Trash;

public static class TrashEndpoints
{
    public static WebApplication MapTrashEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/trash", (HttpContext context, int? limit, ITrashService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.List(principal, limit), value => new TrashPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listTrash");

        api.MapPost("/trash/batches/{deletionBatchId:guid}/restore", (HttpContext context, Guid deletionBatchId, ITrashService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.Restore(principal, deletionBatchId, IdempotencyKey(context), context.TraceIdentifier), value => new TrashRestoreResponse(value.DeletionBatchId, value.RestoredCount, value.RemainingCount)))
            .WithName("restoreTrashBatch");

        api.MapPost("/trash/batches/{deletionBatchId:guid}/purge", (HttpContext context, Guid deletionBatchId, TrashPurgeRequest request, ITrashService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.Purge(principal, new TrashPurgeCommand(deletionBatchId, request.Confirmation), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("purgeTrashBatch");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) => new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
    private static TrashItemResponse ToResponse(TrashItemRecord value) => new(value.Id, value.ResourceType, value.ResourceId, value.DeletionBatchId, value.PriorStatus, value.DeletedAt, value.RestoredAt, value.PurgedAt);
}
