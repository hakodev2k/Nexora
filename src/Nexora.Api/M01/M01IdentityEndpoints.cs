using Nexora.Api.Security;

namespace Nexora.Api.M01;

public static class M01IdentityEndpoints
{
    public static WebApplication MapM01IdentityEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        api.AddEndpointFilter(async (invocationContext, next) =>
        {
            var http = invocationContext.HttpContext;
            if (HttpMethods.IsPost(http.Request.Method) || HttpMethods.IsPut(http.Request.Method) || HttpMethods.IsPatch(http.Request.Method) || HttpMethods.IsDelete(http.Request.Method))
            {
                var csrf = http.RequestServices.GetRequiredService<CsrfTokenService>();
                var cookieSecret = http.Request.Cookies["__Host-NexoraCsrf"];
                var requestToken = http.Request.Headers["X-CSRF-Token"].ToString();
                if (!csrf.Validate(cookieSecret, requestToken))
                {
                    return Results.Problem(
                        title: "CSRF token is invalid.",
                        statusCode: StatusCodes.Status403Forbidden,
                        type: "/problems/CsrfInvalid",
                        extensions: new Dictionary<string, object?>
                        {
                            ["code"] = "CsrfInvalid",
                            ["traceId"] = http.TraceIdentifier
                        });
                }
            }

            return await next(invocationContext);
        });

        api.MapGet("/auth/csrf", (HttpContext context, CsrfTokenService tokens) =>
        {
            var issued = tokens.Issue();

            context.Response.Headers.CacheControl = "no-store";
            context.Response.Cookies.Append("__Host-NexoraCsrf", issued.CookieSecret, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(30)
            });

            return Results.Ok(new CsrfResponse(issued.RequestToken, "csrf", 1800));
        })
        .WithName("getCsrf");

        api.MapPost("/auth/registrations", (HttpContext context, RegistrationRequest request, M01RuntimeStore store) =>
            store.Register(request).ToHttp(context))
            .WithName("register");

        api.MapPost("/auth/verifications", (HttpContext context, TokenProofRequest request, M01RuntimeStore store) =>
            store.Verify(request).ToHttp(context))
            .WithName("verify");

        api.MapPost("/auth/verifications/resend", (HttpContext context, EmailRequest request, M01RuntimeStore store) =>
            store.ResendVerification(request).ToHttp(context))
            .WithName("resendVerification");

        api.MapPost("/auth/login", (HttpContext context, CredentialsRequest request, M01RuntimeStore store, SessionCookieService cookies) =>
        {
            var result = store.Login(request);
            if (!result.Succeeded || result.Value is null)
            {
                return result.ToHttp(context);
            }

            cookies.Append(context.Response, result.Value.RawSessionHandle, result.Value.ExpiresAt);
            return Results.Ok(new LoginResponse(result.Value.Profile, result.Value.ExpiresAt));
        })
        .WithName("login");

        api.MapPost("/auth/logout", (HttpContext context, M01RuntimeStore store, SessionCookieService cookies) =>
        {
            var result = store.Logout(cookies.ReadRawHandle(context.Request));
            cookies.Clear(context.Response);
            return result.ToHttp(context);
        })
        .WithName("logout");

        api.MapPost("/auth/reauth", (HttpContext context, PasswordProofRequest request, M01RuntimeStore store, SessionCookieService cookies) =>
            store.Reauth(cookies.ReadRawHandle(context.Request), request).ToHttp(context))
            .WithName("reauth");

        api.MapPost("/auth/password-resets", (HttpContext context, EmailRequest request, M01RuntimeStore store) =>
            store.RequestPasswordReset(request).ToHttp(context))
            .WithName("requestReset");

        api.MapPost("/auth/password-resets/confirm", (HttpContext context, ResetProofRequest request, M01RuntimeStore store) =>
            store.ConfirmPasswordReset(request).ToHttp(context))
            .WithName("confirmReset");

        api.MapGet("/me", (HttpContext context, M01RuntimeStore store, SessionCookieService cookies) =>
        {
            var result = store.GetMe(cookies.ReadRawHandle(context.Request));
            if (!result.Succeeded || result.Value is null)
            {
                return result.ToHttp(context);
            }

            context.Response.Headers.ETag = result.Value.ETag;
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(result.Value.Profile);
        })
        .WithName("getMe");

        api.MapPatch("/me", (HttpContext context, ProfilePatchRequest request, M01RuntimeStore store, SessionCookieService cookies) =>
        {
            var result = store.UpdateMe(cookies.ReadRawHandle(context.Request), context.Request.Headers.IfMatch.ToString(), request);
            if (!result.Succeeded || result.Value is null)
            {
                return result.ToHttp(context);
            }

            context.Response.Headers.ETag = result.Value.ETag;
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(result.Value.Profile);
        })
        .WithName("updateMe");

        api.MapGet("/me/sessions", (HttpContext context, M01RuntimeStore store, SessionCookieService cookies) =>
            store.ListSessions(cookies.ReadRawHandle(context.Request)).ToHttp(context))
            .WithName("listSessions");

        api.MapDelete("/me/sessions/{sessionId:guid}", (HttpContext context, Guid sessionId, M01RuntimeStore store, SessionCookieService cookies) =>
            store.RevokeSession(cookies.ReadRawHandle(context.Request), sessionId).ToHttp(context))
            .WithName("revokeSession");

        api.MapPost("/me/sessions/revoke-all", (HttpContext context, M01RuntimeStore store, SessionCookieService cookies) =>
        {
            var result = store.RevokeAll(cookies.ReadRawHandle(context.Request));
            if (result.Succeeded)
            {
                cookies.Clear(context.Response);
            }

            return result.ToHttp(context);
        })
        .WithName("revokeAll");

        if (app.Environment.IsDevelopment())
        {
            api.MapGet("/dev/account-messages", (M01RuntimeStore store) => Results.Ok(store.CapturedMessages()))
                .WithName("devListAccountMessages");
        }

        return app;
    }
}
