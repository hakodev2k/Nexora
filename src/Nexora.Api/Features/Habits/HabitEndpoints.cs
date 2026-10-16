using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Habits;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Habits;

public static class HabitEndpoints
{
    public static WebApplication MapHabitEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/habits", (HttpContext context, string? state, string? query, int? limit, IHabitService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.List(principal, state, query, limit),
                value => new HabitPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listHabits");

        api.MapGet("/habits/{habitId:guid}", (HttpContext context, Guid habitId, DateOnly? from, DateOnly? to,
            IHabitService service, IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Get(principal, habitId, from, to)))
            .WithName("getHabit");

        api.MapPost("/habits", (HttpContext context, HabitCreateRequest request, IHabitService service,
            IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Create(principal, new HabitCreateCommand(request.Title, request.Kind, request.TargetCount,
                    request.Unit, request.EffectiveFrom, request.WeekdayMask, request.TimeZoneId, request.ReminderLocalTime),
                    IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("createHabit");

        api.MapPut("/habits/{habitId:guid}", (HttpContext context, Guid habitId, HabitUpdateRequest request,
            IHabitService service, IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Update(principal, habitId, context.Request.Headers.IfMatch.ToString(),
                    new HabitUpdateCommand(request.Title, request.Unit, request.ReminderLocalTime, request.TimeZoneId),
                    IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("updateHabit");

        api.MapPut("/habits/{habitId:guid}/schedule", (HttpContext context, Guid habitId, HabitScheduleRequest request,
            IHabitService service, IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.SetSchedule(principal, habitId, context.Request.Headers.IfMatch.ToString(),
                    new HabitScheduleCommand(request.EffectiveFrom, request.WeekdayMask, request.TargetCount),
                    IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("setHabitSchedule");

        api.MapPost("/habits/{habitId:guid}/check-ins", (HttpContext context, Guid habitId, HabitCheckInRequest request,
            IHabitService service, IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.RecordCheckIn(principal, habitId, context.Request.Headers.IfMatch.ToString(),
                    new HabitCheckInCommand(request.LocalDate, request.Count, request.Note), IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("recordHabitCheckIn");

        api.MapPost("/habits/{habitId:guid}/transition", (HttpContext context, Guid habitId, HabitTransitionRequest request,
            IHabitService service, IIdentityService identity, SessionCookieService cookies) =>
            MapDetail(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Transition(principal, habitId, context.Request.Headers.IfMatch.ToString(), request.State,
                    IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("transitionHabit");

        return app;
    }

    private static IResult MapDetail(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<HabitDetail>> operation)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<HabitDetailResponse>(false,
            default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is not null) context.Response.Headers.ETag = result.Value.Habit.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<HabitDetailResponse>.Success(ToResponse(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<HabitDetailResponse>(false, default, result.Code, result.StatusCode, result.Title));
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

    private static HabitScheduleResponse ToResponse(HabitScheduleRecord value) => new(value.Id, value.EffectiveFrom,
        value.EffectiveUntil, value.WeekdayMask, value.TargetCount, value.Paused, value.ETag);

    private static HabitCheckInResponse ToResponse(HabitCheckInRecord value) => new(value.Id, value.LocalDate, value.Count,
        value.Note, value.ScheduleId, value.UpdatedAt, value.ETag);

    private static HabitResponse ToResponse(HabitRecord value) => new(value.Id, value.Title, value.Kind, value.TargetCount,
        value.Unit, value.State, value.TimeZoneId, value.ReminderLocalTime,
        value.CurrentSchedule is null ? null : ToResponse(value.CurrentSchedule), value.CurrentStreak, value.CreatedAt,
        value.UpdatedAt, value.ETag);

    private static HabitDetailResponse ToResponse(HabitDetail value) => new(ToResponse(value.Habit),
        value.Schedules.Select(ToResponse).ToArray(), value.CheckIns.Select(ToResponse).ToArray());

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
}
