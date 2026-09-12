using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace Nexora.Api.Security;

public sealed class CsrfTokenService
{
    private readonly byte[] _hmacKey = RandomNumberGenerator.GetBytes(32);

    public IssuedCsrfToken Issue()
    {
        var cookieSecret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        using var hmac = new HMACSHA256(_hmacKey);
        var digest = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cookieSecret));
        return new IssuedCsrfToken(cookieSecret, WebEncoders.Base64UrlEncode(digest));
    }

    public void AppendCookie(HttpResponse response, IssuedCsrfToken issued)
    {
        response.Cookies.Append("__Host-NexoraCsrf", issued.CookieSecret, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(30)
        });
        response.Headers["X-CSRF-Token"] = issued.RequestToken;
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
}

public sealed record IssuedCsrfToken(string CookieSecret, string RequestToken);
