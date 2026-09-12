using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Modules;

namespace Nexora.Api.Features.Modules;

public static class ModuleEndpoints
{
    public static WebApplication MapModuleEndpoints(this WebApplication app)
    {
        var modules = app.MapGroup("/api/v1/admin/modules")
            .RequireCsrfForUnsafeMethods();

        modules.MapGet("/", (HttpContext context, int? limit, IModulePolicyService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.List(limit, principal), value => new ModulePageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listModules");

        modules.MapPost("/{moduleId:guid}/preview", (HttpContext context, Guid moduleId, ModulePolicyChangeRequest request, IModulePolicyService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.Preview(moduleId, new ModulePolicyChange(request.SystemEnabled, request.RegistrationEnabled), principal), ToResponse))
            .WithName("previewModule");

        modules.MapPut("/{moduleId:guid}/policy", (HttpContext context, Guid moduleId, ModulePolicyCommitRequest request, IModulePolicyService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal =>
                service.Commit(moduleId, context.Request.Headers.IfMatch.ToString(), new ModulePolicyCommit(new ModulePolicyChange(request.SystemEnabled, request.RegistrationEnabled), request.PreviewToken), principal, context.Request.Headers["Idempotency-Key"].ToString(), context.TraceIdentifier), ToResponse))
            .WithName("setModulePolicy");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        }

        var result = operation(auth.Value);
        if (!result.Succeeded || result.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
        }

        return ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static ModuleResponse ToResponse(ModulePolicyRecord value) => new(
        value.Id,
        value.Code,
        value.Name,
        value.State,
        value.SystemEnabled,
        value.RegistrationEnabled,
        value.PolicyRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
        value.ETag,
        value.RequiredDependencies,
        value.RequiredBy,
        value.UnavailableReason);

    private static ModulePolicyPreviewResponse ToResponse(ModulePolicyPreview value) => new(
        value.PreviewToken,
        value.ExpiresAt,
        value.ETag,
        value.Changes.Select(change => new ModuleChangeDiff(change.Field, change.Before, change.After)).ToArray(),
        value.Blockers.Select(blocker => new ModulePolicyBlocker(blocker.Code, blocker.Message, blocker.Field)).ToArray());
}
