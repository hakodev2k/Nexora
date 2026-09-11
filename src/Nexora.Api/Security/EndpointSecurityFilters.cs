using System.Collections.Concurrent;
using System.Globalization;

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

            var idempotencyKey = http.Request.Headers["Idempotency-Key"].ToString();
            if (!Guid.TryParse(idempotencyKey, out _))
            {
                return Results.Problem(
                    title: "Idempotency-Key must be a UUID.",
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    type: "/problems/IdempotencyKeyRequired",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "IdempotencyKeyRequired",
                        ["traceId"] = http.TraceIdentifier
                    });
            }

            var path = http.Request.Path.Value ?? string.Empty;
            var limit = LimitFor(path);
            if (limit is { } rate && !RequestRateLimiter.Allow(ClientKey(http) + ":" + path, rate.MaxRequests, rate.Window, out var retryAfter))
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

            var csrf = http.RequestServices.GetRequiredService<CsrfTokenService>();
            var cookieSecret = http.Request.Cookies["__Host-NexoraCsrf"];
            var requestToken = http.Request.Headers["X-CSRF-Token"].ToString();
            if (csrf.Validate(cookieSecret, requestToken))
            {
                return await next(invocationContext);
            }

            return Results.Problem(
                title: "CSRF token is invalid.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "/problems/CsrfInvalid",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "CsrfInvalid",
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

    private static (int MaxRequests, TimeSpan Window)? LimitFor(string path)
    {
        if (path.EndsWith("/auth/login", StringComparison.OrdinalIgnoreCase)) return (10, TimeSpan.FromMinutes(1));
        if (path.Contains("/auth/verifications", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("/auth/password-resets", StringComparison.OrdinalIgnoreCase)) return (5, TimeSpan.FromMinutes(5));
        if (path.EndsWith("/auth/registrations", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/auth/reauth", StringComparison.OrdinalIgnoreCase)) return (10, TimeSpan.FromMinutes(5));
        return null;
    }

}

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
