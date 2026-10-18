using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Goals;
using Nexora.Application.Identity;
using System.Globalization;

namespace Nexora.Api.Features.Goals;

public static class GoalsEndpoints
{
    public static WebApplication MapGoalsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/goals/{goalId:guid}/target-history",(HttpContext context,Guid goalId,string? cursor,
            IGoalService service,IIdentityService identity,SessionCookieService cookies)=>
            Map(context,identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal=>service.TargetDefinitionHistory(principal,goalId,cursor),value=>value))
            .WithName("getGoalTargetDefinitionHistory");

        api.MapGet("/goals/{goalId:guid}/metadata-history",(HttpContext context,Guid goalId,string? cursor,
            IGoalService service,IIdentityService identity,SessionCookieService cookies)=>
            Map(context,identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal=>service.MetadataHistory(principal,goalId,cursor),value=>value))
            .WithName("getGoalMetadataHistory");

        api.MapGet("/goals/{goalId:guid}/state-history", (HttpContext context, Guid goalId, string? cursor,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.StateHistory(principal, goalId, cursor), value => value))
            .WithName("getGoalStateHistory");

        api.MapGet("/goals/{goalId:guid}/progress-history", (HttpContext context, Guid goalId, string? cursor,
            IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ProgressHistory(principal, goalId, cursor), value => value))
            .WithName("getGoalProgressHistory");

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
                principal => request.NumericTarget is not null
                    ? IdentityOperationResult<GoalDetail>.Failure("ValidationFailed", 422, "Targets cannot be changed by Goal metadata update.")
                    : service.Update(principal, goalId, context.Request.Headers.IfMatch.ToString(),
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

        api.MapPost("/goals/{goalId:guid}/targets/boolean", (HttpContext context, Guid goalId,
            BooleanTargetRequest request, IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => request.BooleanValue is not { } value
                    ? IdentityOperationResult<GoalDetail>.Failure("ValidationFailed", 422, "Boolean value is required.")
                    : service.CreateBooleanTarget(principal, goalId, context.Request.Headers.IfMatch.ToString(),
                        new BooleanTargetCommand(request.Title, value), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createBooleanGoalTarget");

        api.MapPost("/goals/{goalId:guid}/targets/{targetId:guid}/boolean-progress", (HttpContext context,
            Guid goalId, Guid targetId, BooleanProgressRequest request, IGoalService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => request.BooleanValue is not { } value
                    ? IdentityOperationResult<GoalDetail>.Failure("ValidationFailed", 422, "Boolean value is required.")
                    : service.RecordBooleanProgress(principal, goalId, targetId, context.Request.Headers.IfMatch.ToString(),
                        new BooleanProgressCommand(value, request.Note), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("recordBooleanGoalProgress");

        api.MapPost("/goals/{goalId:guid}/targets/numeric", (HttpContext context, Guid goalId,
            NumericTargetCreateRequest request, IGoalService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => request.InitialValue is not { } initial || request.CurrentValue is not { } current || request.TargetValue is not { } target
                    ? IdentityOperationResult<GoalDetail>.Failure("ValidationFailed", 422, "Initial, current and target values are required.")
                    : service.CreateNumericTarget(principal, goalId, context.Request.Headers.IfMatch.ToString(),
                        new NumericTargetCommand(request.Title, initial, current, target),
                        IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createNumericGoalTarget");

        api.MapPost("/goals/{goalId:guid}/targets/{targetId:guid}/change-preview", (HttpContext context,
            Guid goalId, Guid targetId, GoalTargetChangeRequest request, IGoalService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.PreviewTargetChange(principal, goalId, targetId, context.Request.Headers.IfMatch.ToString(),
                    new GoalTargetChange(request.Mode, request.Title, request.InitialValue, request.TargetValue)), value => value))
            .WithName("previewGoalTargetChange");

        api.MapPost("/goals/{goalId:guid}/targets/{targetId:guid}/update-definition", (HttpContext context,
            Guid goalId, Guid targetId, GoalTargetCommitRequest request, IGoalService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ChangeTarget(principal, goalId, targetId, "Update", context.Request.Headers.IfMatch.ToString(),
                    request.PreviewToken, IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateGoalTargetDefinition");

        api.MapPost("/goals/{goalId:guid}/targets/{targetId:guid}/remove", (HttpContext context,
            Guid goalId, Guid targetId, GoalTargetCommitRequest request, IGoalService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ChangeTarget(principal, goalId, targetId, "Remove", context.Request.Headers.IfMatch.ToString(),
                    request.PreviewToken, IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("removeGoalTarget");

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
        if (result.Succeeded && result.Acknowledgement is { } acknowledgement)
            return new ApiResult<CommittedResourceAcknowledgement>(true, acknowledgement, result.Code,
                result.StatusCode, result.Title).ToHttp(context);
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
        value.CreatedAt, value.UpdatedAt, value.ETag, value.PreArchiveStatus);

    private static GoalTargetResponse ToResponse(GoalTargetRecord value) => new(value.Id, value.Kind, value.Title,
        value.InitialValue, value.CurrentValue, value.TargetValue, value.Progress, value.UpdatedAt, value.ETag,
        value.InitialValue?.ToString("G29", CultureInfo.InvariantCulture), value.CurrentValue?.ToString("G29", CultureInfo.InvariantCulture),
        value.TargetValue?.ToString("G29", CultureInfo.InvariantCulture), value.BooleanValue);

    private static GoalDetailResponse ToResponse(GoalDetail value) => new(ToResponse(value.Goal),
        value.Targets.Select(ToResponse).ToArray());
}
