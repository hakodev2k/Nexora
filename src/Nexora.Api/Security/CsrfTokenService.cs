using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Nexora.Api.Security;

public sealed class CsrfTokenService
{
    private const string CsrfCookieName = "__Host-NexoraCsrf";
    private const string AnonymousSessionCookieName = "__Host-NexoraAnonymousSession";
    private const string AnonymousSessionDomain = "anonymous-session:";
    private static readonly TimeSpan AnonymousSessionLifetime = TimeSpan.FromHours(24);
    private readonly byte[] _hmacKey;

    // Kept for source-compatible construction in isolated non-host callers.
    // The API composition must use the configured constructor below so a
    // restart does not silently replace the anonymous receipt namespace.
    public CsrfTokenService()
        : this(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
    {
    }

    public CsrfTokenService(string signingSecret)
    {
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            throw new ArgumentException("A stable CSRF signing secret is required.", nameof(signingSecret));
        }

        _hmacKey = SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("Nexora.CsrfAndAnonymousSession:" + signingSecret));
    }

    public IssuedCsrfToken Issue()
    {
        var cookieSecret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        using var hmac = new HMACSHA256(_hmacKey);
        var digest = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cookieSecret));
        return new IssuedCsrfToken(cookieSecret, WebEncoders.Base64UrlEncode(digest));
    }

    public void AppendCookie(HttpContext context, IssuedCsrfToken issued)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(issued);

        context.Response.Cookies.Append(CsrfCookieName, issued.CookieSecret, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(30)
        });

        var anonymousSessionBinding = context.Request.Cookies[AnonymousSessionCookieName];
        if (!IsValidAnonymousSessionBinding(anonymousSessionBinding))
        {
            anonymousSessionBinding = IssueAnonymousSessionBinding();
            context.Response.Cookies.Append(AnonymousSessionCookieName, anonymousSessionBinding, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = AnonymousSessionLifetime
            });
        }

        context.Response.Headers["X-CSRF-Token"] = issued.RequestToken;
    }

    public bool Validate(string? cookieSecret, string? requestToken)
    {
        if (string.IsNullOrWhiteSpace(cookieSecret) || string.IsNullOrWhiteSpace(requestToken))
        {
            return false;
        }

        using var hmac = new HMACSHA256(_hmacKey);
        var expectedBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cookieSecret));
        var expected = WebEncoders.Base64UrlEncode(expectedBytes);
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(expected),
            System.Text.Encoding.UTF8.GetBytes(requestToken));
    }

    /// <summary>
    /// Returns the separately signed, opaque anonymous-session binding only
    /// after validating it and its paired CSRF request token. Callers may use
    /// the returned value as transient HMAC input for anonymous request
    /// scoping; it must never be persisted or logged.
    /// </summary>
    public string? GetValidatedAnonymousSessionBinding(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var cookieSecret = context.Request.Cookies[CsrfCookieName];
        var requestToken = context.Request.Headers["X-CSRF-Token"].ToString();
        var anonymousSessionBinding = context.Request.Cookies[AnonymousSessionCookieName];
        return Validate(cookieSecret, requestToken) && IsValidAnonymousSessionBinding(anonymousSessionBinding)
            ? anonymousSessionBinding
            : null;
    }

    private string IssueAnonymousSessionBinding()
    {
        var payload = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var signature = ComputeHmac(AnonymousSessionDomain + payload);
        return $"{payload}.{WebEncoders.Base64UrlEncode(signature)}";
    }

    private bool IsValidAnonymousSessionBinding(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        var separator = value.IndexOf('.');
        if (separator <= 0 || separator != value.LastIndexOf('.') || separator == value.Length - 1)
        {
            return false;
        }

        var payload = value[..separator];
        var signature = value[(separator + 1)..];
        if (payload.Length != 43 || signature.Length != 43)
        {
            return false;
        }

        try
        {
            var expected = WebEncoders.Base64UrlEncode(ComputeHmac(AnonymousSessionDomain + payload));
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(expected),
                System.Text.Encoding.UTF8.GetBytes(signature));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private byte[] ComputeHmac(string value)
    {
        using var hmac = new HMACSHA256(_hmacKey);
        return hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
    }
}

public sealed record IssuedCsrfToken(string CookieSecret, string RequestToken);
