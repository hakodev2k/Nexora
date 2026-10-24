using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexora.Api.Security;
using Nexora.Api.Features.Identity;
using Xunit;

namespace Nexora.UnitTests;

public sealed class AuthenticationRateLimitTests
{
    [Fact]
    public async Task Invalid_csrf_is_rejected_before_reauthentication_subject_resolution()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(new CsrfTokenService("isolated-csrf-order-test-signing-key"));
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapGroup("").RequireCsrfForUnsafeMethods()
            .MapPost("/api/v1/auth/reauth", () => Results.NoContent());
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };

        // No Identity/SQL dependency is registered. A subject lookup would fail
        // this request instead of returning the required safe CSRF rejection.
        using var denied = await client.PostAsync("/api/v1/auth/reauth", null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Contains("CsrfInvalid", await denied.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/v1/auth/registrations/")]
    [InlineData("/API/V1/AUTH/VERIFICATIONS/")]
    public async Task Equivalent_identity_routes_still_require_idempotency_keys(string requestPath)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(new CsrfTokenService("isolated-idempotency-test-signing-key"));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AuthenticationRequestThrottle>();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        app.MapGroup("").RequireCsrfForUnsafeMethods()
            .MapPost(requestPath.TrimEnd('/'), () => Results.NoContent());
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var csrf = app.Services.GetRequiredService<CsrfTokenService>().Issue();
        client.DefaultRequestHeaders.Add("Cookie", $"__Host-NexoraCsrf={csrf.CookieSecret}");
        client.DefaultRequestHeaders.Add("X-CSRF-Token", csrf.RequestToken);

        using var denied = await client.PostAsync(requestPath, null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);
        Assert.Contains("IdempotencyKeyRequired", await denied.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Equivalent_login_routes_share_the_same_throttle(bool uppercase, bool trailingSlash)
    {
        // Exercise the actual security filter and ASP.NET routing over loopback,
        // without starting Identity, SQL or any provider/runtime business flow.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(new CsrfTokenService("isolated-rate-limit-test-signing-key"));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AuthenticationRequestThrottle>();
        await using var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");
        var route = $"/rate-{Guid.NewGuid():N}/auth/login";
        app.MapGroup("").RequireCsrfForUnsafeMethods()
            .MapPost(route, (CredentialsRequest request) => Results.Unauthorized());
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var csrf = app.Services.GetRequiredService<CsrfTokenService>().Issue();
        client.DefaultRequestHeaders.Add("Cookie", $"__Host-NexoraCsrf={csrf.CookieSecret}");
        client.DefaultRequestHeaders.Add("X-CSRF-Token", csrf.RequestToken);

        for (var index = 0; index < 10; index++)
        {
            using var allowed = await client.PostAsJsonAsync(route, new CredentialsRequest(" User@Example.test ", "invalid-test-proof"));
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var equivalentRoute = uppercase ? route.ToUpperInvariant() : route;
        if (trailingSlash) equivalentRoute += "/";
        using var denied = await client.PostAsJsonAsync(equivalentRoute, new CredentialsRequest("user@example.test", "invalid-test-proof"));
        Assert.Equal(HttpStatusCode.TooManyRequests, denied.StatusCode);
        Assert.NotNull(denied.Headers.RetryAfter);
    }
}
