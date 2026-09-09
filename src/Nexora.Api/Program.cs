using System.Text.Json.Serialization;
using Nexora.Api.M01;
using Nexora.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddSingleton<CsrfTokenService>();
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.AddSingleton<SessionCookieService>();
builder.Services.AddSingleton<M01RuntimeStore>();
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

var app = builder.Build();

app.UseSecurityHeaders();

app.MapGet("/health/live", () => Results.Ok(new HealthEnvelope("Live", "Nexora.Api")))
    .WithName("liveHealth");

app.MapGet("/health/ready", () => Results.Ok(new HealthEnvelope("ReadyForM01ApiSurface", "Nexora.Api")))
    .WithName("readyHealth");

app.MapM01IdentityEndpoints();

app.MapFallback(() => Results.Problem(
    title: "Resource unavailable",
    detail: "The requested Nexora endpoint is not implemented in the current approved slice.",
    statusCode: StatusCodes.Status404NotFound,
    type: "/problems/ResourceUnavailable",
    extensions: new Dictionary<string, object?> { ["code"] = "ResourceUnavailable" }));

app.Run();

public partial class Program;

internal sealed record HealthEnvelope(string Status, string Service);
