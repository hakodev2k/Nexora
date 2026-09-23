using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Nexora.Api.Features.Access;
using Nexora.Api.Features.Identity;
using Nexora.Api.Features.Modules;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Access;
using Nexora.Application.Modules;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Access;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Modules;
using Nexora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddSingleton<SessionCookieService>();
var configuredSqlConnectionString = builder.Configuration.GetConnectionString("NexoraSql")
    ?? throw new InvalidOperationException("ConnectionStrings:NexoraSql is required.");
var sqlConnectionString = Environment.GetEnvironmentVariable("NEXORA_SQL_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(sqlConnectionString))
{
    var sqlPassword = Environment.GetEnvironmentVariable("NEXORA_SQL_PASSWORD");
    if (string.IsNullOrEmpty(sqlPassword))
    {
        throw new InvalidOperationException("NEXORA_SQL_PASSWORD or NEXORA_SQL_CONNECTION_STRING must be supplied; no default database credential is permitted.");
    }

    var connectionBuilder = new SqlConnectionStringBuilder(configuredSqlConnectionString)
    {
        Password = sqlPassword
    };
    sqlConnectionString = connectionBuilder.ConnectionString;
}
var resolvedSqlConnectionString = sqlConnectionString ?? throw new InvalidOperationException("SQL connection string could not be resolved.");
try
{
    // The current executable is a local-only surface. Never let a caller point
    // this build at a non-loopback or non-development database by overriding env.
    resolvedSqlConnectionString = LocalSqlTarget.Validate(
        resolvedSqlConnectionString,
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development");
}
catch (ArgumentException)
{
    throw new InvalidOperationException("Only a loopback SQL Server development target is allowed.");
}
catch (InvalidOperationException)
{
    throw new InvalidOperationException("Only a loopback SQL Server development target is allowed.");
}
var idempotencySecret = Environment.GetEnvironmentVariable("NEXORA_IDEMPOTENCY_SECRET");
if (string.IsNullOrWhiteSpace(idempotencySecret))
{
    throw new InvalidOperationException("NEXORA_IDEMPOTENCY_SECRET is required; it must not be derived from the SQL connection string.");
}
var csrfSigningSecret = Environment.GetEnvironmentVariable("NEXORA_CSRF_SECRET");
if (string.IsNullOrWhiteSpace(csrfSigningSecret))
{
    throw new InvalidOperationException("NEXORA_CSRF_SECRET is required and must remain stable across local API restarts.");
}
builder.Services.AddSingleton(new CsrfTokenService(csrfSigningSecret));
var localMessageKey = Environment.GetEnvironmentVariable("NEXORA_LOCAL_MESSAGE_KEY");
if (string.IsNullOrWhiteSpace(localMessageKey))
{
    throw new InvalidOperationException("NEXORA_LOCAL_MESSAGE_KEY is required for local account-message delivery.");
}
var localMessageCaptureDirectory = builder.Configuration["Nexora:LocalAccountMessageCapturePath"]
    ?? Environment.GetEnvironmentVariable("NEXORA_LOCAL_MESSAGE_CAPTURE_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, ".local-account-messages");
var localMessageOperatorSid = builder.Configuration["Nexora:LocalAccountMessageOperatorSid"]
    ?? Environment.GetEnvironmentVariable("NEXORA_LOCAL_MESSAGE_OPERATOR_SID");
var localMessageRuntimeSid = builder.Configuration["Nexora:LocalAccountMessageRuntimeSid"]
    ?? Environment.GetEnvironmentVariable("NEXORA_LOCAL_MESSAGE_RUNTIME_SID");
builder.Services.AddSingleton(new SqlConnectionFactory(resolvedSqlConnectionString));
builder.Services.AddSingleton<SqlReadinessProbe>();
builder.Services.AddSingleton(new LocalAccountMessageEnvelopeProtector(localMessageKey));
builder.Services.AddSingleton<LocalAccountMessageSink>(services =>
    new LocalAccountMessageSink(
        localMessageCaptureDirectory,
        builder.Environment.ContentRootPath,
        services.GetRequiredService<ILogger<LocalAccountMessageSink>>(),
        operatorSid: localMessageOperatorSid,
        runtimeSid: localMessageRuntimeSid));
builder.Services.AddSingleton<IAccountMessageSink>(services =>
    services.GetRequiredService<LocalAccountMessageSink>());
builder.Services.AddSingleton<IAccountMessageEffectSink>(services =>
    services.GetRequiredService<LocalAccountMessageSink>());
builder.Services.AddSingleton<IIdentityService>(services =>
    new SqlIdentityService(services.GetRequiredService<SqlConnectionFactory>(),
        services.GetRequiredService<IAccountMessageSink>(), idempotencySecret,
        services.GetRequiredService<LocalAccountMessageEnvelopeProtector>()));
builder.Services.AddHostedService<AccountMessageDeliveryWorker>();
builder.Services.AddSingleton<IModulePolicyService>(services =>
    new SqlModulePolicyService(
        services.GetRequiredService<SqlConnectionFactory>(),
        builder.Configuration["Nexora:ModulePreviewSecret"] ?? Environment.GetEnvironmentVariable("NEXORA_MODULE_PREVIEW_SECRET"),
        idempotencySecret));
builder.Services.AddSingleton<IAdminAccessService>(services =>
    new SqlAdminAccessService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var persistenceUnavailable = exception is SqlException or TimeoutException;
        context.Response.Headers.CacheControl = "no-store";
        await Results.Problem(
            title: persistenceUnavailable
                ? "Persistence is temporarily unavailable."
                : "The request could not be completed.",
            statusCode: persistenceUnavailable
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError,
            type: persistenceUnavailable
                ? "/problems/PersistenceUnavailable"
                : "/problems/InternalError",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = persistenceUnavailable ? "PersistenceUnavailable" : "InternalError",
                ["traceId"] = context.TraceIdentifier
            }).ExecuteAsync(context);
    });
});

app.UseSecurityHeaders();

app.MapGet("/health/live", () => Results.Ok(new HealthEnvelope("Live", "Nexora.Api")))
    .WithName("liveHealth");

app.MapGet("/health/ready", async (SqlReadinessProbe readiness, CancellationToken cancellationToken) =>
{
    var result = await readiness.CheckAsync(cancellationToken);
    var status = result.Ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
    return Results.Json(new ReadinessEnvelope(result.Status, "Nexora.Api", result.Dependencies), statusCode: status);
})
    .WithName("readyHealth");

app.MapIdentityEndpoints();
app.MapModuleEndpoints();
app.MapAdminAccessEndpoints();

app.MapFallback(() => Results.Problem(
    title: "Resource unavailable",
    detail: "The requested Nexora endpoint is not implemented in the current approved slice.",
    statusCode: StatusCodes.Status404NotFound,
    type: "/problems/ResourceUnavailable",
    extensions: new Dictionary<string, object?> { ["code"] = "ResourceUnavailable" }));

app.Run();

public partial class Program;

internal sealed record HealthEnvelope(string Status, string Service);

internal sealed record ReadinessEnvelope(
    string Status,
    string Service,
    IReadOnlyDictionary<string, string> Dependencies);
