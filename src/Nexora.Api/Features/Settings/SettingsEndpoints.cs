using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Settings;

namespace Nexora.Api.Features.Settings;

public static class SettingsEndpoints
{
    public static WebApplication MapSettingsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/settings/preferences", (HttpContext context, ISettingsService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.ListPreferences(principal), value => new PreferencePageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listPreferences");

        api.MapPut("/settings/preferences/{preferenceKey}", (HttpContext context, string preferenceKey, PreferenceUpdateRequest request, ISettingsService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)), principal => service.UpdatePreference(principal,
                new PreferenceUpdateCommand(preferenceKey, request.SchemaVersion, request.ValueJson, context.Request.Headers.IfMatch.ToString()), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updatePreference");

        return app;
    }

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, new IdentityOperationResult<TOut>(false, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth, Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is PreferenceRecord preference) context.Response.Headers.ETag = preference.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) => new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);
    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();
    private static PreferenceResponse ToResponse(PreferenceRecord value) => new(value.Id, value.PreferenceKey, value.SchemaVersion, value.ValueJson, value.CreatedAt, value.UpdatedAt, value.ETag);
}
