using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Dashboard;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static WebApplication MapDashboardEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/dashboard", (HttpContext context, IDashboardService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                service.Get, ToResponse))
            .WithName("getDashboard");

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

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static DashboardSnapshotResponse ToResponse(DashboardSnapshot value) => new(
        value.TimeZoneId,
        value.GeneratedAt,
        value.Widgets.Select(ToResponse).ToArray());

    private static DashboardWidgetResponse ToResponse(DashboardWidget value) => new(
        value.Id,
        value.Title,
        value.SourceModule,
        value.State,
        value.Message,
        value.RefreshedAt,
        value.Count,
        value.Items.Select(ToResponse).ToArray());

    private static DashboardItemResponse ToResponse(DashboardItem value) => new(
        value.Id,
        value.Kind,
        value.Title,
        value.Status,
        value.At,
        value.Detail);
}
