using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1")
            .RequireCsrfForUnsafeMethods();

        api.MapGet("/auth/csrf", (HttpContext context, CsrfTokenService tokens) =>
        {
            var issued = tokens.Issue();
            context.Response.Headers.CacheControl = "no-store";
            tokens.AppendCookie(context.Response, issued);
            return Results.Ok(new CsrfResponse(issued.RequestToken, "csrf", 1800));
        }).WithName("getCsrf");

        api.MapPost("/auth/registrations", (HttpContext context, RegistrationRequest request, IIdentityService service) =>
            ToHttp(context, service.Register(new RegistrationCommand(request.Email, request.Password, request.TimeZoneId, request.DisplayName, null), IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("register");

        api.MapPost("/auth/verifications", (HttpContext context, TokenProofRequest request, IIdentityService service) =>
            ToHttp(context, service.Verify(request.Token, IdempotencyKey(context), context.TraceIdentifier), value => new VerificationResponse(value.Status, value.MessageCode, ToProfile(value.Profile))))
            .WithName("verify");

        api.MapPost("/auth/verifications/resend", (HttpContext context, EmailRequest request, IIdentityService service) =>
            ToHttp(context, service.ResendVerification(request.Email, IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("resendVerification");

        api.MapPost("/auth/login", (HttpContext context, CredentialsRequest request, IIdentityService service, SessionCookieService cookies, CsrfTokenService csrfTokens) =>
        {
            var result = service.Login(request.Email, request.Password, context.Request.Headers.UserAgent.ToString(), context.Request.Headers["Idempotency-Key"].ToString(), context.TraceIdentifier);
            if (!result.Succeeded || result.Value is null)
            {
                return ToHttp(context, result);
            }

            cookies.Append(context.Response, result.Value.RawSessionHandle, result.Value.ExpiresAt);
            // A successful login rotates the CSRF cookie. The request token is
            // returned as a response header and kept in frontend memory only.
            csrfTokens.AppendCookie(context.Response, csrfTokens.Issue());
            return ToHttp(context, result, value => new LoginResponse(ToProfile(value.Profile), value.ExpiresAt));
        }).WithName("login");

        api.MapPost("/auth/logout", (HttpContext context, IIdentityService service, SessionCookieService cookies) =>
        {
            var result = service.Logout(cookies.ReadRawHandle(context.Request), IdempotencyKey(context), context.TraceIdentifier);
            cookies.Clear(context.Response);
            return ToHttp(context, result);
        }).WithName("logout");

        api.MapPost("/auth/reauth", (HttpContext context, PasswordProofRequest request, IIdentityService service, SessionCookieService cookies) =>
            ToHttp(context, service.Reauthenticate(cookies.ReadRawHandle(context.Request), request.Password, IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("reauth");

        api.MapPost("/auth/password-resets", (HttpContext context, EmailRequest request, IIdentityService service) =>
            ToHttp(context, service.RequestPasswordReset(request.Email, IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("requestReset");

        api.MapPost("/auth/password-resets/confirm", (HttpContext context, ResetProofRequest request, IIdentityService service) =>
            ToHttp(context, service.ConfirmPasswordReset(request.Token, request.NewPassword, IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("confirmReset");

        api.MapGet("/me", (HttpContext context, IIdentityService service, SessionCookieService cookies) =>
        {
            var result = service.GetMe(cookies.ReadRawHandle(context.Request));
            if (!result.Succeeded || result.Value is null)
            {
                return ToHttp(context, result);
            }

            context.Response.Headers.ETag = result.Value.ETag;
            context.Response.Headers.CacheControl = "no-store";
            return ToHttp(context, result, value => ToProfile(value.Profile));
        }).WithName("getMe");

        api.MapPatch("/me", (HttpContext context, ProfilePatchRequest request, IIdentityService service, SessionCookieService cookies) =>
        {
            var result = service.UpdateMe(
                cookies.ReadRawHandle(context.Request),
                context.Request.Headers.IfMatch.ToString(),
                new Nexora.Application.Identity.ProfilePatchRequest(request.DisplayName, request.TimeZoneId, request.Locale),
                IdempotencyKey(context),
                context.TraceIdentifier);
            if (!result.Succeeded || result.Value is null)
            {
                return ToHttp(context, result);
            }

            context.Response.Headers.ETag = result.Value.ETag;
            context.Response.Headers.CacheControl = "no-store";
            return ToHttp(context, result, value => ToProfile(value.Profile));
        }).WithName("updateMe");

        api.MapDelete("/me", (HttpContext context, DeleteAccountRequest request, IIdentityService service, SessionCookieService cookies) =>
        {
            var result = service.SoftDelete(cookies.ReadRawHandle(context.Request), request.Confirmation, request.Password, IdempotencyKey(context), context.TraceIdentifier);
            if (result.Succeeded)
            {
                cookies.Clear(context.Response);
            }

            return ToHttp(context, result);
        }).WithName("softDeleteAccount");

        api.MapGet("/me/sessions", (HttpContext context, IIdentityService service, SessionCookieService cookies) =>
            ToHttp(context, service.ListSessions(cookies.ReadRawHandle(context.Request)), value =>
                new SessionPage(value.Items.Select(session => new SessionProjection(session.Id, session.DeviceLabel, session.CreatedAt, session.LastSeenAt, session.ExpiresAt, session.IsCurrent)).ToArray(), value.NextCursor)))
            .WithName("listSessions");

        api.MapDelete("/me/sessions/{sessionId:guid}", (HttpContext context, Guid sessionId, IIdentityService service, SessionCookieService cookies) =>
            ToHttp(context, service.RevokeSession(cookies.ReadRawHandle(context.Request), sessionId, IdempotencyKey(context), context.TraceIdentifier)))
            .WithName("revokeSession");

        api.MapPost("/me/sessions/revoke-all", (HttpContext context, IIdentityService service, SessionCookieService cookies) =>
        {
            var result = service.RevokeAll(cookies.ReadRawHandle(context.Request), IdempotencyKey(context), context.TraceIdentifier);
            if (result.Succeeded) cookies.Clear(context.Response);
            return ToHttp(context, result);
        }).WithName("revokeAll");

        return app;
    }

    private static ProfileResponse ToProfile(IdentityProfile profile) => new(
        profile.Id,
        profile.Email,
        profile.DisplayName,
        profile.TimeZoneId,
        profile.Locale,
        profile.State,
        profile.PersonalSpaceId,
        profile.Modules.Select(module => new ModuleProjection(module.Code, module.Enabled, module.UnavailableReason)).ToArray(),
        profile.Role);

    private static string? IdempotencyKey(HttpContext context) =>
        context.Request.Headers["Idempotency-Key"].ToString();

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static IResult ToHttp<TIn, TOut>(HttpContext context, IdentityOperationResult<TIn> result, Func<TIn, TOut> map)
    {
        if (!result.Succeeded || result.Value is null)
        {
            return ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
        }

        return ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code));
    }
}
