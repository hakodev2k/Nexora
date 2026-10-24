using System.Collections.Concurrent;
using System.Globalization;
using Nexora.Api.Features.Identity;
using Nexora.Application.Identity;
using Nexora.Domain.Identity;

namespace Nexora.Api.Security;

public static class EndpointSecurityFilters
{
    private const long DefaultMaxMutationBodyBytes = 64 * 1024;

    public static RouteGroupBuilder RequireCsrfForUnsafeMethods(this RouteGroupBuilder group, long maxMutationBodyBytes = DefaultMaxMutationBodyBytes)
    {
        if (maxMutationBodyBytes <= 0 || maxMutationBodyBytes > 25 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(maxMutationBodyBytes));
        // Attach a server-enforced limit so chunked requests cannot bypass the
        // Content-Length check below. The filter remains responsible for the
        // safe problem response when the hosting server exposes the length.
        group.WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(maxMutationBodyBytes));
        group.AddEndpointFilter(async (invocationContext, next) =>
        {
            var http = invocationContext.HttpContext;
            if (!HttpMethods.IsPost(http.Request.Method) &&
                !HttpMethods.IsPut(http.Request.Method) &&
                !HttpMethods.IsPatch(http.Request.Method) &&
                !HttpMethods.IsDelete(http.Request.Method))
            {
                return await next(invocationContext);
            }

            if (http.Request.ContentLength is long contentLength && contentLength > maxMutationBodyBytes)
            {
                return Results.Problem(
                    title: "Request body is too large.",
                    statusCode: StatusCodes.Status413PayloadTooLarge,
                    type: "/problems/RequestBodyTooLarge",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "RequestBodyTooLarge",
                        ["traceId"] = http.TraceIdentifier
                    });
            }

            var origin = http.Request.Headers.Origin.ToString();
            if (!string.IsNullOrWhiteSpace(origin) && !OriginAllowed(http, origin))
            {
                return Results.Problem(
                    title: "Request origin is not allowed.",
                    statusCode: StatusCodes.Status403Forbidden,
                    type: "/problems/OriginNotAllowed",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "OriginNotAllowed",
                        ["traceId"] = http.TraceIdentifier
                    });
            }

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

            // ASP.NET routes accept casing variants and a trailing slash. Apply
            // security policy to the same logical path so those equivalent
            // requests cannot skip a suffix guard or obtain a fresh bucket.
            var path = (http.Request.Path.Value ?? string.Empty).TrimEnd('/');
            using var admission = http.RequestServices.GetRequiredService<AuthenticationRequestThrottle>()
                .TryAcquire(path, Subject(invocationContext, path), ClientKey(http), out var retryAfter);
            if (admission is null)
            {
                http.Response.Headers["Retry-After"] = retryAfter.ToString(CultureInfo.InvariantCulture);
                return Results.Problem(
                    title: "Too many requests.",
                    statusCode: StatusCodes.Status429TooManyRequests,
                    type: "/problems/RateLimited",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "RateLimited",
                        ["retryAfter"] = retryAfter,
                        ["traceId"] = http.TraceIdentifier
                    });
            }

            if (!RequiresIdempotencyKey(http.Request.Method, path) ||
                Guid.TryParse(http.Request.Headers["Idempotency-Key"].ToString(), out _))
            {
                var result = await next(invocationContext);
                admission.Complete((result as IStatusCodeHttpResult)?.StatusCode);
                return result;
            }

            return Results.Problem(
                title: "Idempotency-Key must be a UUID.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "/problems/IdempotencyKeyRequired",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "IdempotencyKeyRequired",
                    ["traceId"] = http.TraceIdentifier
                });
        });

        return group;
    }

    private static string ClientKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";

    private static bool OriginAllowed(HttpContext context, string origin)
    {
        if (Uri.TryCreate(origin, UriKind.Absolute, out var parsed) &&
            string.Equals(parsed.Scheme, context.Request.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(parsed.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var configured = Environment.GetEnvironmentVariable("NEXORA_ALLOWED_ORIGINS");
        if (!string.IsNullOrWhiteSpace(configured) && configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(value, origin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        return environment.IsDevelopment() &&
            (string.Equals(origin, "http://localhost:5173", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(origin, "http://127.0.0.1:5173", StringComparison.OrdinalIgnoreCase));
    }

    private static string Subject(EndpointFilterInvocationContext invocation, string path)
    {
        string? email = invocation.Arguments.OfType<CredentialsRequest>().FirstOrDefault()?.Email
            ?? invocation.Arguments.OfType<RegistrationRequest>().FirstOrDefault()?.Email
            ?? invocation.Arguments.OfType<EmailRequest>().FirstOrDefault()?.Email;
        if (path.EndsWith("/auth/reauth", StringComparison.OrdinalIgnoreCase))
        {
            var http = invocation.HttpContext;
            var handle = http.RequestServices.GetRequiredService<SessionCookieService>().ReadRawHandle(http.Request);
            var profile = http.RequestServices.GetRequiredService<IIdentityService>().GetMe(handle);
            email = profile.Succeeded ? profile.Value?.Profile.Email : null;
        }
        try
        {
            return email is { Length: <= 320 } ? EmailNormalizer.Normalize(email) : "invalid-subject";
        }
        catch (ArgumentException) { return "invalid-subject"; }
        catch (FormatException) { return "invalid-subject"; }
    }

    private static bool RequiresIdempotencyKey(string method, string path)
    {
        if (HttpMethods.IsPatch(method) && string.Equals(path, "/api/v1/me", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HttpMethods.IsPost(method))
        {
            return string.Equals(path, "/api/v1/auth/registrations", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(path, "/api/v1/auth/verifications", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(path, "/api/v1/auth/verifications/resend", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(path, "/api/v1/auth/password-resets", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(path, "/api/v1/auth/password-resets/confirm", StringComparison.OrdinalIgnoreCase);
        }

        if (!HttpMethods.IsPut(method))
        {
            return false;
        }

        return path.StartsWith("/api/v1/admin/users/", StringComparison.OrdinalIgnoreCase) ||
               (path.StartsWith("/api/v1/admin/modules/", StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith("/policy", StringComparison.OrdinalIgnoreCase));
    }

}

// Existing shared-link operational guard; authentication uses its dedicated
// result-aware subject/source limiter above.
internal static class RequestRateLimiter
{
    private static readonly ConcurrentDictionary<string, Window> Windows = new(StringComparer.Ordinal);

    public static bool Allow(string key, int maxRequests, TimeSpan window, out int retryAfterSeconds)
    {
        var now = DateTimeOffset.UtcNow;
        var current = Windows.GetOrAdd(key, _ => new Window(now));
        lock (current)
        {
            if (now - current.Start >= window)
            {
                current.Start = now;
                current.Count = 0;
            }
            current.Count++;
            var remaining = window - (now - current.Start);
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            return current.Count <= maxRequests;
        }
    }

    private sealed class Window(DateTimeOffset start)
    {
        public DateTimeOffset Start { get; set; } = start;
        public int Count { get; set; }
    }
}
