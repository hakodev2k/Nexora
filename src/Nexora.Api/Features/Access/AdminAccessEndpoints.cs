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

        admin.MapPut("/users/{userId:guid}/role", (HttpContext context, Guid userId, AdminRoleRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.SetRole(principal, userId, new AdminRoleCommand(request.Role, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("setAdminUserRole");

        admin.MapPut("/users/{userId:guid}/permissions", (HttpContext context, Guid userId, AdminActionGrantRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.SetActionGrant(principal, userId, new AdminActionGrantCommand(request.ActionKey, request.Effect, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("setAdminActionGrant");

        admin.MapPut("/users/{userId:guid}/modules/{moduleCode}", (HttpContext context, Guid userId, string moduleCode, AdminModuleGrantRequest request, IAdminAccessService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.SetModuleGrant(principal, userId, new AdminModuleGrantCommand(moduleCode, request.Enabled, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("setAdminModuleGrant");

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

    private static AdminUserAccessResponse ToResponse(AdminUserAccess value) => new(
        ToResponse(value.User),
        value.ActionGrants.Select(grant => new AdminActionGrantResponse(grant.ActionKey, grant.Effect, grant.Status, grant.UpdatedAt)).ToArray(),
        value.ModuleGrants.Select(grant => new AdminModuleGrantResponse(grant.Code, grant.Enabled, grant.State, grant.SystemEnabled)).ToArray());

    private static AdminUserResponse ToResponse(AdminUserRecord value) => new(value.Id, value.Email, value.DisplayName,
        value.State, value.EmailConfirmed, value.Role, value.PersonalSpaceId, value.PersonalSpaceState,
        value.CreatedAt, value.UpdatedAt, value.ETag);
}
