using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.DeveloperTools;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.DeveloperTools;

public static class DeveloperToolsEndpoints
{
    public static WebApplication MapDeveloperToolsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/developer/tools", (HttpContext context,
            IToolboxService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                service.Catalog,
                value => new ToolboxCatalogResponse(value.Items.Select(ToResponse).ToArray())))
            .WithName("listDeveloperTools");

        api.MapPost("/developer/tools/run", (HttpContext context, ToolboxRunRequest request,
            IToolboxService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.Run(principal,
                    new ToolboxRunCommand(request.ToolCode, request.Input, request.Options)),
                ToResponse))
            .WithName("runDeveloperTool");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context,
        IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation,
        Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(
                auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        }

        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(
                result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static ToolboxToolResponse ToResponse(ToolboxTool value) => new(
        value.Code, value.Name, value.Category, value.Description, value.ExecutionMode,
        value.ActionKey, value.AcceptsOptions);

    private static ToolboxRunResponse ToResponse(ToolboxRunResult value) => new(
        value.ToolCode, value.Output, value.Warning, value.ErrorPath, value.DurationMilliseconds);
}
