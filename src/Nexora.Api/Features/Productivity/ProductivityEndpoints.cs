using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Productivity;

namespace Nexora.Api.Features.Productivity;

public static class ProductivityEndpoints
{
    public static WebApplication MapProductivityEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1")
            .RequireCsrfForUnsafeMethods();

        api.MapGet("/projects", (HttpContext context, int? limit, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.ListProjects(principal, limit), value => new ProjectPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listProjects");

        api.MapPost("/projects", (HttpContext context, ProjectRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.CreateProject(principal, new ProjectCommand(request.Name, request.Description, request.StartAt, request.EndAt, request.Priority, request.TagsJson, request.Notes), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createProject");

        api.MapPut("/projects/{projectId:guid}", (HttpContext context, Guid projectId, ProjectRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.UpdateProject(principal, projectId, context.Request.Headers.IfMatch.ToString(), new ProjectCommand(request.Name, request.Description, request.StartAt, request.EndAt, request.Priority, request.TagsJson, request.Notes, ConfirmTaskBounds: request.ConfirmTaskBounds), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateProject");

        api.MapPost("/projects/{projectId:guid}/transition", (HttpContext context, Guid projectId, ProductivityTransitionRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.TransitionProject(principal, projectId, context.Request.Headers.IfMatch.ToString(), request.Status, request.Reason, IdempotencyKey(context), context.TraceIdentifier, request.Confirm), ToResponse))
            .WithName("transitionProject");

        api.MapDelete("/projects/{projectId:guid}", (HttpContext context, Guid projectId, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.DeleteProject(principal, projectId, context.Request.Headers.IfMatch.ToString(), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("deleteProject");

        api.MapGet("/tasks", (HttpContext context, Guid? projectId, int? limit, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.ListTasks(principal, projectId, limit), value => new TaskPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listTasks");

        api.MapPost("/tasks", (HttpContext context, TaskRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.CreateTask(principal, new TaskCommand(request.ProjectId, request.Title, request.Description, request.Status, request.DueAt, request.StartAt, request.EndAt, request.Priority, request.TagsJson, request.AcceptanceCriteriaJson, request.Rank, request.ReminderAt, ConfirmProjectTimeBounds: request.ConfirmProjectTimeBounds), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createTask");

        api.MapPut("/tasks/{taskId:guid}", (HttpContext context, Guid taskId, TaskRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.UpdateTask(principal, taskId, context.Request.Headers.IfMatch.ToString(), new TaskCommand(request.ProjectId, request.Title, request.Description, request.Status, request.DueAt, request.StartAt, request.EndAt, request.Priority, request.TagsJson, request.AcceptanceCriteriaJson, request.Rank, request.ReminderAt, ConfirmProjectTimeBounds: request.ConfirmProjectTimeBounds), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateTask");

        api.MapPost("/tasks/{taskId:guid}/transition", (HttpContext context, Guid taskId, ProductivityTransitionRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.TransitionTask(principal, taskId, context.Request.Headers.IfMatch.ToString(), request.Status, request.Reason, IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionTask");

        api.MapDelete("/tasks/{taskId:guid}", (HttpContext context, Guid taskId, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.DeleteTask(principal, taskId, context.Request.Headers.IfMatch.ToString(), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("deleteTask");

        api.MapGet("/calendar/events", (HttpContext context, DateTimeOffset? from, DateTimeOffset? to, int? limit, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.ListEvents(principal, from, to, limit), value => new EventPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listEvents");

        api.MapPost("/calendar/events", (HttpContext context, EventRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.CreateEvent(principal, new EventCommand(request.Title, request.Description, request.StartAt, request.EndAt, request.TimeZoneId, request.IsAllDay, request.SourceUid), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("createEvent");

        api.MapPut("/calendar/events/{eventId:guid}", (HttpContext context, Guid eventId, EventRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.UpdateEvent(principal, eventId, context.Request.Headers.IfMatch.ToString(), new EventCommand(request.Title, request.Description, request.StartAt, request.EndAt, request.TimeZoneId, request.IsAllDay, request.SourceUid), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateEvent");

        api.MapPost("/calendar/events/{eventId:guid}/transition", (HttpContext context, Guid eventId, ProductivityTransitionRequest request, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.TransitionEvent(principal, eventId, context.Request.Headers.IfMatch.ToString(), request.Status, IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("transitionEvent");

        api.MapDelete("/calendar/events/{eventId:guid}", (HttpContext context, Guid eventId, IProductivityService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.DeleteEvent(principal, eventId, context.Request.Headers.IfMatch.ToString(), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("deleteEvent");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, auth);
        }

        var result = operation(auth.Value);
        return ToHttp(context, result, map);
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) =>
        context.Request.Headers["Idempotency-Key"].ToString();

    private static IResult ToHttp<TIn, TOut>(HttpContext context, IdentityOperationResult<TIn> result, Func<TIn, TOut> map)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
        }

        return ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, auth);
        }

        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is not null && result.Value is ProjectRecord project)
        {
            context.Response.Headers.ETag = project.ETag;
        }
        else if (result.Succeeded && result.Value is TaskRecord task)
        {
            context.Response.Headers.ETag = task.ETag;
        }
        else if (result.Succeeded && result.Value is EventRecord calendarEvent)
        {
            context.Response.Headers.ETag = calendarEvent.ETag;
        }

        return ToHttp(context, result, map);
    }

    private static ProjectResponse ToResponse(ProjectRecord value) => new(value.Id, value.Name, value.Description, value.Status, value.CreatedAt, value.UpdatedAt, value.ETag, value.StartAt, value.EndAt, value.Priority, value.TagsJson, value.Notes);
    private static TaskResponse ToResponse(TaskRecord value) => new(value.Id, value.ProjectId, value.Title, value.Description, value.Status, value.DueAt, value.CreatedAt, value.UpdatedAt, value.ETag, value.StartAt, value.EndAt, value.Priority, value.TagsJson, value.AcceptanceCriteriaJson, value.Rank, value.ReminderAt, value.IsOverdue);
    private static EventResponse ToResponse(EventRecord value) => new(value.Id, value.Title, value.Description, value.StartAt, value.EndAt, value.TimeZoneId, value.Status, value.CreatedAt, value.UpdatedAt, value.ETag, value.IsAllDay, value.SourceUid, value.SourceKind, value.TaskId);
}

