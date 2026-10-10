using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Goals;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Goals;

public static class GoalsEndpoints
{
    public static WebApplication MapGoalsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/goals", (HttpContext context, string? status, string? query, int? limit,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, status, query, limit),
                value => new GoalPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listGoals");

        api.MapGet("/goals/{goalId:guid}", (HttpContext context, Guid goalId,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, goalId), ToResponse))
            .WithName("getGoal");

        api.MapPost("/goals", (HttpContext context, GoalRequest request,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal, new GoalCommand(request.Title, request.Description,
                    request.StartDate, request.EndDate),
                    request.NumericTarget is null ? null : new NumericTargetCommand(request.NumericTarget.Title,
                        request.NumericTarget.InitialValue, request.NumericTarget.CurrentValue, request.NumericTarget.TargetValue),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createGoal");

        api.MapPut("/goals/{goalId:guid}", (HttpContext context, Guid goalId, GoalRequest request,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, goalId, context.Request.Headers.IfMatch.ToString(),
                    new GoalCommand(request.Title, request.Description, request.StartDate, request.EndDate),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateGoal");

        api.MapPost("/goals/{goalId:guid}/targets/{targetId:guid}/progress", (HttpContext context, Guid goalId, Guid targetId,
            GoalProgressRequest request, IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.RecordNumericProgress(principal, goalId, targetId,
                    context.Request.Headers.IfMatch.ToString(), new NumericProgressCommand(request.CurrentValue, request.Note),
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("recordGoalProgress");

        api.MapPost("/goals/{goalId:guid}/transition", (HttpContext context, Guid goalId, GoalTransitionRequest request,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, goalId, context.Request.Headers.IfMatch.ToString(), request.Status,
                    IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionGoal");

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
        if (result.Succeeded && result.Value is GoalDetail detail)
            context.Response.Headers.ETag = detail.Goal.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static GoalResponse ToResponse(GoalRecord value) => new(value.Id, value.Title, value.Description,
        value.StartDate, value.EndDate, value.Status, value.Progress, value.TargetCount,
        value.CreatedAt, value.UpdatedAt, value.ETag);

    private static GoalTargetResponse ToResponse(GoalTargetRecord value) => new(value.Id, value.Kind, value.Title,
        value.InitialValue, value.CurrentValue, value.TargetValue, value.Progress, value.UpdatedAt, value.ETag);

    private static GoalDetailResponse ToResponse(GoalDetail value) => new(ToResponse(value.Goal),
        value.Targets.Select(ToResponse).ToArray());
}
