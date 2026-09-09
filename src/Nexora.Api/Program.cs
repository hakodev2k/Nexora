using Nexora.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<CsrfTokenService>();
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

var app = builder.Build();

app.UseSecurityHeaders();

app.MapGet("/health/live", () => Results.Ok(new HealthEnvelope("Live", "Nexora.Api")))
    .WithName("liveHealth");

app.MapGet("/health/ready", () => Results.Ok(new HealthEnvelope("ReadyForS00", "Nexora.Api")))
    .WithName("readyHealth");

app.MapGet("/api/v1/auth/csrf", (HttpContext context, CsrfTokenService tokens) =>
{
    var issued = tokens.Issue();

    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Cookies.Append("__Host-NexoraCsrf", issued.CookieSecret, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(30)
    });

    return Results.Ok(new CsrfEnvelope(issued.RequestToken, "csrf", 1800));
})
.WithName("getCsrf");

app.MapFallback(() => Results.Problem(
    title: "Resource unavailable",
    detail: "The requested Nexora endpoint is not implemented in the current approved slice.",
    statusCode: StatusCodes.Status404NotFound,
    type: "/problems/ResourceUnavailable",
    extensions: new Dictionary<string, object?> { ["code"] = "ResourceUnavailable" }));

app.Run();

public partial class Program;

internal sealed record HealthEnvelope(string Status, string Service);
internal sealed record CsrfEnvelope(string RequestToken, string TokenType, int ExpiresInSeconds);
