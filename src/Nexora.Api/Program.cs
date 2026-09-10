using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Nexora.Api.Features.Access;
using Nexora.Api.Features.Identity;
using Nexora.Api.Features.Modules;
using Nexora.Api.Features.Notifications;
using Nexora.Api.Features.Documents;
using Nexora.Api.Features.Finance;
using Nexora.Api.Features.Bookmarks;
using Nexora.Api.Features.Snippets;
using Nexora.Api.Features.Productivity;
using Nexora.Api.Features.Settings;
using Nexora.Api.Features.Trash;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Modules;
using Nexora.Application.Productivity;
using Nexora.Application.Notifications;
using Nexora.Application.Documents;
using Nexora.Application.Settings;
using Nexora.Application.Trash;
using Nexora.Application.Finance;
using Nexora.Application.Bookmarks;
using Nexora.Application.Snippets;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Access;
using Nexora.Infrastructure.Local;
using Nexora.Infrastructure.Modules;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Productivity;
using Nexora.Infrastructure.Notifications;
using Nexora.Infrastructure.Documents;
using Nexora.Infrastructure.Settings;
using Nexora.Infrastructure.Trash;
using Nexora.Infrastructure.Finance;
using Nexora.Infrastructure.Bookmarks;
using Nexora.Infrastructure.Snippets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});

builder.Services.AddSingleton<CsrfTokenService>();
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
var idempotencySecret = Environment.GetEnvironmentVariable("NEXORA_IDEMPOTENCY_SECRET") ?? resolvedSqlConnectionString;
builder.Services.AddSingleton(new SqlConnectionFactory(resolvedSqlConnectionString));
builder.Services.AddSingleton<IAccountMessageSink, LocalAccountMessageSink>();
builder.Services.AddSingleton<IIdentityService>(services =>
    new SqlIdentityService(services.GetRequiredService<SqlConnectionFactory>(),
        services.GetRequiredService<IAccountMessageSink>(), idempotencySecret));
builder.Services.AddSingleton<IModulePolicyService>(services =>
    new SqlModulePolicyService(
        services.GetRequiredService<SqlConnectionFactory>(),
        builder.Configuration["Nexora:ModulePreviewSecret"] ?? Environment.GetEnvironmentVariable("NEXORA_MODULE_PREVIEW_SECRET"),
        idempotencySecret));
builder.Services.AddSingleton<IProductivityService>(services =>
    new SqlProductivityService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<IAdminAccessService>(services =>
    new SqlAdminAccessService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<INotificationService>(services =>
    new SqlNotificationService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<ITrashService>(services =>
    new SqlTrashService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<ISettingsService>(services =>
    new SqlSettingsService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<IDocumentService>(services =>
    new SqlDocumentService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<IFinanceService>(services =>
    new SqlFinanceService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<IBookmarkService>(services =>
    new SqlBookmarkService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.AddSingleton<ISnippetService>(services =>
    new SqlSnippetService(services.GetRequiredService<SqlConnectionFactory>(), idempotencySecret));
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false;
});

var app = builder.Build();

app.UseSecurityHeaders();

app.MapGet("/health/live", () => Results.Ok(new HealthEnvelope("Live", "Nexora.Api")))
    .WithName("liveHealth");

app.MapGet("/health/ready", () => Results.Ok(new HealthEnvelope("ReadyForSqlBackedLocalFeatureSurface", "Nexora.Api")))
    .WithName("readyHealth");

app.MapIdentityEndpoints();
app.MapModuleEndpoints();
app.MapAdminAccessEndpoints();
app.MapNotificationEndpoints();
app.MapTrashEndpoints();
app.MapSettingsEndpoints();
app.MapDocumentEndpoints();
app.MapProductivityEndpoints();
app.MapFinanceEndpoints();
app.MapBookmarkEndpoints();
app.MapSnippetEndpoints();

app.MapFallback(() => Results.Problem(
    title: "Resource unavailable",
    detail: "The requested Nexora endpoint is not implemented in the current approved slice.",
    statusCode: StatusCodes.Status404NotFound,
    type: "/problems/ResourceUnavailable",
    extensions: new Dictionary<string, object?> { ["code"] = "ResourceUnavailable" }));

app.Run();

public partial class Program;

internal sealed record HealthEnvelope(string Status, string Service);
