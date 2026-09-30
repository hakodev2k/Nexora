using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Access;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Access;

public static class AdminAccessEndpoints
{
    public static WebApplication MapAdminAccessEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin")
            .RequireCsrfForUnsafeMethods();

        admin.MapGet("/users", (HttpContext context, string? q, int? limit, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.ListUsers(principal, q, limit), value => new AdminUserPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listAdminUsers");

        admin.MapGet("/users/{userId:guid}/access", (HttpContext context, Guid userId, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.GetUserAccess(principal, userId), ToResponse))
            .WithName("getAdminUserAccess");

        admin.MapPost("/users/{userId:guid}/access/preview", (HttpContext context, Guid userId, AdminAccessPreviewRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
            {
                var change = ToChange(request);
                return !change.Succeeded || change.Value is null
                    ? IdentityOperationResult<AdminAccessPreview>.Failure(change.Code, change.StatusCode, change.Title)
                    : service.PreviewAccess(principal, userId, change.Value);
            }, ToResponse))
            .WithName("previewAdminUserAccess");

        admin.MapPut("/users/{userId:guid}/access/role", (HttpContext context, Guid userId, AdminRoleCommitRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                HasUnknownFields(request.Extra) || !IsKind(request.Kind, "role") || string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.PreviewToken)
                    ? Invalid<AdminUserAccess>("A role change and preview token are required.")
                    : service.SetRole(principal, userId, context.Request.Headers.IfMatch.ToString(), new AdminRoleCommand(request.Role, request.PreviewToken), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("commitAdminUserRole");

        admin.MapPut("/users/{userId:guid}/access/permissions", (HttpContext context, Guid userId, AdminPermissionCommitRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                HasUnknownFields(request.Extra) || !IsKind(request.Kind, "permissions") || string.IsNullOrWhiteSpace(request.PreviewToken)
                    ? Invalid<AdminUserAccess>("Permission changes and preview token are required.")
                    : service.SetActionGrant(principal, userId, context.Request.Headers.IfMatch.ToString(),
                        new AdminActionGrantCommand(ToPermissionChanges(request.Changes), request.PreviewToken), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("commitAdminUserPermissions");

        admin.MapPut("/users/{userId:guid}/access/modules", (HttpContext context, Guid userId, AdminModuleGrantCommitRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                HasUnknownFields(request.Extra) || !IsKind(request.Kind, "modules") || string.IsNullOrWhiteSpace(request.PreviewToken)
                    ? Invalid<AdminUserAccess>("Module changes and preview token are required.")
                    : service.SetModuleGrant(principal, userId, context.Request.Headers.IfMatch.ToString(),
                        new AdminModuleGrantCommand(ToModuleChanges(request.Changes), request.PreviewToken), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("commitAdminUserModules");

        admin.MapPost("/users/{userId:guid}/disable", (HttpContext context, Guid userId, AdminDisableRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.DisableUser(principal, userId, new AdminDisableCommand(request.Confirmation, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("disableAdminUser");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));

        var result = operation(auth.Value);
        if (!result.Succeeded || result.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));

        if (result.Value is AdminUserAccess access)
            context.Response.Headers.ETag = access.User.ETag;
        return ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static IdentityOperationResult<AdminAccessChange> ToChange(AdminAccessPreviewRequest request)
    {
        if (HasUnknownFields(request.Extra))
            return Invalid<AdminAccessChange>("The request contains an unknown field.");

        if (IsKind(request.Kind, "role") && !string.IsNullOrWhiteSpace(request.Role) &&
            HasNoChanges(request.Changes))
        {
            return IdentityOperationResult<AdminAccessChange>.Success(new AdminAccessChange(
                "role", request.Role, Array.Empty<AdminPermissionChange>(), Array.Empty<AdminModuleGrantChange>()));
        }

        if (IsKind(request.Kind, "permissions") && string.IsNullOrWhiteSpace(request.Role))
        {
            return IdentityOperationResult<AdminAccessChange>.Success(new AdminAccessChange(
                "permissions", null, ToPermissionChanges(request.Changes), Array.Empty<AdminModuleGrantChange>()));
        }

        if (IsKind(request.Kind, "modules") && string.IsNullOrWhiteSpace(request.Role))
        {
            return IdentityOperationResult<AdminAccessChange>.Success(new AdminAccessChange(
                "modules", null, Array.Empty<AdminPermissionChange>(), ToModuleChanges(request.Changes)));
        }

        return Invalid<AdminAccessChange>("Submit exactly one valid access change kind.");
    }

    private static IReadOnlyList<AdminPermissionChange> ToPermissionChanges(System.Text.Json.JsonElement? changes)
    {
        if (changes is not { ValueKind: System.Text.Json.JsonValueKind.Array }) return Array.Empty<AdminPermissionChange>();
        return changes.Value.EnumerateArray()
            .Select(change => HasOnlyProperties(change, "actionKey", "effect")
                ? new AdminPermissionChange(ReadString(change, "actionKey") ?? string.Empty, ReadString(change, "effect") ?? string.Empty)
                : new AdminPermissionChange(string.Empty, string.Empty))
            .ToArray();
    }

    private static IReadOnlyList<AdminModuleGrantChange> ToModuleChanges(System.Text.Json.JsonElement? changes)
    {
        if (changes is not { ValueKind: System.Text.Json.JsonValueKind.Array }) return Array.Empty<AdminModuleGrantChange>();
        return changes.Value.EnumerateArray()
            .Select(change => HasOnlyProperties(change, "moduleId", "enabled") && HasBoolean(change, "enabled")
                ? new AdminModuleGrantChange(ReadGuid(change, "moduleId"), ReadBoolean(change, "enabled"))
                : new AdminModuleGrantChange(Guid.Empty, false))
            .ToArray();
    }

    private static string? ReadString(System.Text.Json.JsonElement value, string property) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object && value.TryGetProperty(property, out var item) && item.ValueKind == System.Text.Json.JsonValueKind.String
            ? item.GetString()
            : null;

    private static Guid ReadGuid(System.Text.Json.JsonElement value, string property) =>
        Guid.TryParse(ReadString(value, property), out var result) ? result : Guid.Empty;

    private static bool ReadBoolean(System.Text.Json.JsonElement value, string property) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object && value.TryGetProperty(property, out var item) && item.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False && item.GetBoolean();

    private static bool HasBoolean(System.Text.Json.JsonElement value, string property) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object && value.TryGetProperty(property, out var item) &&
        item.ValueKind is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False;

    private static bool HasOnlyProperties(System.Text.Json.JsonElement value, params string[] expected) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object &&
        value.EnumerateObject().All(property => expected.Contains(property.Name, StringComparer.Ordinal));

    private static bool IsKind(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static bool HasNoChanges(System.Text.Json.JsonElement? value) =>
        value is null || value.Value.ValueKind == System.Text.Json.JsonValueKind.Null;

    private static bool HasUnknownFields(IDictionary<string, System.Text.Json.JsonElement>? extra) => extra is { Count: > 0 };

    private static IdentityOperationResult<T> Invalid<T>(string title) =>
        IdentityOperationResult<T>.Failure("ValidationFailed", StatusCodes.Status422UnprocessableEntity, title);

    private static AdminUserAccessResponse ToResponse(AdminUserAccess value) => new(
        ToResponse(value.User),
        value.ActionGrants.Select(grant => new AdminActionGrantResponse(grant.ActionKey, grant.Effect, grant.Status, grant.UpdatedAt)).ToArray(),
        value.ModuleGrants.Select(grant => new AdminModuleGrantResponse(grant.ModuleId, grant.Code, grant.Enabled, grant.State, grant.SystemEnabled)).ToArray());

    private static AdminAccessPreviewResponse ToResponse(AdminAccessPreview value) => new(
        value.PreviewToken,
        value.ExpiresAt,
        value.ETag,
        value.Changes.Select(change => new AdminAccessChangeDiffResponse(change.Field, change.Before, change.After)).ToArray(),
        value.Blockers.Select(blocker => new AdminAccessBlockerResponse(blocker.Code, blocker.Message)).ToArray());

    private static AdminUserResponse ToResponse(AdminUserRecord value) => new(value.Id, value.Email, value.DisplayName,
        value.State, value.EmailConfirmed, value.Role, value.PersonalSpaceId, value.PersonalSpaceState,
        value.CreatedAt, value.UpdatedAt, value.ETag);
}
