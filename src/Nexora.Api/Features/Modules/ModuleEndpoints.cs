using Nexora.Api.Http;
using Nexora.Api.Security;

namespace Nexora.Api.Features.Modules;

public static class ModuleEndpoints
{
    public static WebApplication MapModuleEndpoints(this WebApplication app)
    {
        var modules = app.MapGroup("/api/v1/admin/modules")
            .RequireCsrfForUnsafeMethods()
            .RequireDevelopmentSuperAdminProof();

        modules.MapGet("/", (int? limit, DevelopmentModuleStore store) =>
            store.ListModules(limit).ToHttp())
            .WithName("listModules");

        modules.MapPost("/{moduleId:guid}/preview", (Guid moduleId, ModulePolicyChangeRequest request, DevelopmentModuleStore store) =>
            store.PreviewModule(moduleId, request).ToHttp())
            .WithName("previewModule");

        modules.MapPut("/{moduleId:guid}/policy", (HttpContext context, Guid moduleId, ModulePolicyCommitRequest request, DevelopmentModuleStore store) =>
            store.SetModulePolicy(moduleId, context.Request.Headers.IfMatch.ToString(), request).ToHttp())
            .WithName("setModulePolicy");

        return app;
    }
}
