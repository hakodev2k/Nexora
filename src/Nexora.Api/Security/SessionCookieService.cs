using System.Security.Cryptography;
using Microsoft.Extensions.Primitives;

namespace Nexora.Api.Security;

public sealed class SessionCookieService
{
    public const string CookieName = "__Host-NexoraSession";

    public string IssueRawHandle() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Digest(string rawHandle)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawHandle);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    public string? ReadRawHandle(HttpRequest request)
    {
        return request.Cookies.TryGetValue(CookieName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    public void Append(HttpResponse response, string rawHandle, DateTimeOffset expiresAt)
    {
        response.Headers.CacheControl = new StringValues("no-store");
        response.Cookies.Append(CookieName, rawHandle, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt
        });
    }

    public void Clear(HttpResponse response)
    {
        response.Headers.CacheControl = new StringValues("no-store");
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}
