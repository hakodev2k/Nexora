using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nexora.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ValidateCsrfAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        {
            await next();
            return;
        }

        var csrf = context.HttpContext.RequestServices.GetRequiredService<CsrfTokenService>();
        var cookieSecret = context.HttpContext.Request.Cookies["__Host-NexoraCsrf"];
        var requestToken = context.HttpContext.Request.Headers["X-CSRF-Token"].ToString();

        if (csrf.Validate(cookieSecret, requestToken))
        {
            await next();
            return;
        }

        var problem = new ProblemDetails
        {
            Title = "CSRF token is invalid.",
            Status = StatusCodes.Status403Forbidden,
            Type = "/problems/CsrfInvalid"
        };
        problem.Extensions["code"] = "CsrfInvalid";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
