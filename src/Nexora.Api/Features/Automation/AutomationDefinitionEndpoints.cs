using System.Text.Json;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Automation;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Automation;

public static class AutomationDefinitionEndpoints
{
    public static WebApplication MapAutomationDefinitionEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/automation/definitions").RequireCsrfForUnsafeMethods(256 * 1024);
        api.MapGet("/capabilities", (HttpContext c, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Capabilities(a)));
        api.MapGet("/", (HttpContext c, string? cursor, string? query, string? lifecycle, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.List(a, cursor, query, lifecycle ?? "Active")));
        api.MapGet("/{id:guid}", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Get(a, id)));
        api.MapGet("/{id:guid}/versions", (HttpContext c, Guid id, long? beforeVersion, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.History(a, id, beforeVersion)));
        api.MapPost("/validate", (HttpContext c, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Validate(a, body)));
        api.MapPost("/", (HttpContext c, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Create(a, body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPut("/{id:guid}", (HttpContext c, Guid id, AutomationDefinitionCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Save(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapGet("/{id:guid}/export", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Export(a, id)));
        api.MapPost("/import", (HttpContext c, JsonElement body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a =>
        {
            var fields = body.ValueKind == JsonValueKind.Object ? body.EnumerateObject().ToArray() : [];
            if (fields.Length != 3 || fields.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != 3 ||
                !body.TryGetProperty("schemaVersion", out var schema) || schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var number) || number != 1 ||
                !body.TryGetProperty("title", out var title) || title.ValueKind != JsonValueKind.String ||
                !body.TryGetProperty("definitionJson", out var json) || json.ValueKind != JsonValueKind.String)
                return IdentityOperationResult<AutomationDefinitionAcknowledgement>.Failure("DefinitionSchemaInvalid", 422, "Check the portable definition envelope.");
            return s.Import(a, new(number, title.GetString()!, json.GetString()!), c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier);
        }));
        api.MapPost("/{id:guid}/trash", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Trash(a, id, c.Request.Headers.IfMatch.ToString(), c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapPost("/{id:guid}/restore", (HttpContext c, Guid id, RestoreCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Restore(a, id, body.DeletionBatchId, c.Request.Headers.IfMatch.ToString(), c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapGet("/{id:guid}/purge-preview", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.PreviewPurge(a, id)));
        api.MapPost("/{id:guid}/purge", (HttpContext c, Guid id, AutomationPurgeCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Purge(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        api.MapGet("/{id:guid}/schedule", (HttpContext c, Guid id, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Schedule(a, id)));
        api.MapPost("/{id:guid}/schedule/preview", (HttpContext c, Guid id, AutomationScheduleCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.PreviewSchedule(a, id, body)));
        api.MapPut("/{id:guid}/schedule", (HttpContext c, Guid id, AutomationScheduleCommand body, IAutomationDefinitionService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.UpdateSchedule(a, id, c.Request.Headers.IfMatch.ToString(), body, c.Request.Headers["Idempotency-Key"].ToString(), c.TraceIdentifier)));
        return app;
    }
    public sealed record RestoreCommand(Guid DeletionBatchId);
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies, Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        c.Response.Headers.CacheControl = "no-store";
        var actor = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = actor.Succeeded && actor.Value is not null ? work(actor.Value) : IdentityOperationResult<T>.Failure(actor.Code, actor.StatusCode, actor.Title);
        if (result.Value is AutomationDefinitionDetail item) c.Response.Headers.ETag = item.Definition.ETag;
        if (result.Value is AutomationDefinitionAcknowledgement ack) c.Response.Headers.ETag = ack.ETag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
