using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Planner;

namespace Nexora.Api.Features.Planner;

public static class PlannerEndpoints
{
    public static WebApplication MapPlannerEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/planner", (HttpContext context, DateOnly? from, DateOnly? to, IPlannerService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapPlan(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, from, to)))
            .WithName("listPlanner");

        api.MapPost("/planner/pins", (HttpContext context, PlannerPinRequest request, IPlannerService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapPin(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Pin(principal, new PlannerPinCommand(request.TaskId, request.PlanDate, request.Notes),
                    IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("pinPlannerTask");

        api.MapPut("/planner/pins/{pinId:guid}", (HttpContext context, Guid pinId, PlannerPinUpdateRequest request,
            IPlannerService service, IIdentityService identity, SessionCookieService cookies) =>
            MapPin(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, pinId, context.Request.Headers.IfMatch.ToString(),
                    new PlannerPinUpdateCommand(request.PlanDate, request.Notes), IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("updatePlannerPin");

        api.MapPost("/planner/reorder", (HttpContext context, PlannerReorderRequest request, IPlannerService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapPlan(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Reorder(principal, context.Request.Headers.IfMatch.ToString(),
                    new PlannerReorderCommand(request.PlanDate, request.PinIds), IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("reorderPlanner");

        api.MapDelete("/planner/pins/{pinId:guid}", (HttpContext context, Guid pinId, IPlannerService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Unpin(principal, pinId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("unpinPlannerTask");

        return app;
    }

    private static IResult MapPlan(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<PlannerPlan>> operation)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<PlannerPlanResponse>(false,
            default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is not null) context.Response.Headers.ETag = result.Value.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<PlannerPlanResponse>.Success(ToResponse(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<PlannerPlanResponse>(false, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapPin(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<PlannerPinRecord>> operation)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<PlannerPinResponse>(false,
            default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is not null) context.Response.Headers.ETag = result.Value.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<PlannerPinResponse>.Success(ToResponse(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<PlannerPinResponse>(false, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(false, default, result.Code, result.StatusCode, result.Title));
    }

    private static PlannerPinResponse ToResponse(PlannerPinRecord value) => new(value.Id, value.TaskId, value.TaskTitle,
        value.TaskStatus, value.ProjectName, value.StartAt, value.EndAt, value.PlanDate, value.Rank, value.Notes,
        value.SourceAvailable, value.UpdatedAt, value.ETag);

    private static PlannerPlanResponse ToResponse(PlannerPlan value) => new(value.From, value.To,
        value.Pins.Select(ToResponse).ToArray(), value.ETag);

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
