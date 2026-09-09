using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Http;

public sealed record ApiResult<T>(
    bool Succeeded,
    T? Value,
    string Code,
    int StatusCode,
    string Title)
{
    public static ApiResult<T> Success(T value, int statusCode = StatusCodes.Status200OK, string code = "Ok") =>
        new(true, value, code, statusCode, code);

    public static ApiResult<T> NoContent(string code = "NoContent") =>
        new(true, default, code, StatusCodes.Status204NoContent, code);

    public static ApiResult<T> Failure(string code, int statusCode, string title) =>
        new(false, default, code, statusCode, title);
}

public static class ApiActionResultExtensions
{
    public static ActionResult ToActionResult<T>(this ApiResult<T> result, ControllerBase controller)
    {
        controller.Response.Headers["Cache-Control"] = "no-store";

        if (result.Succeeded)
        {
            if (result.StatusCode == StatusCodes.Status204NoContent)
            {
                return controller.NoContent();
            }

            return new ObjectResult(result.Value)
            {
                StatusCode = result.StatusCode
            };
        }

        var problem = new ProblemDetails
        {
            Title = result.Title,
            Status = result.StatusCode,
            Type = $"/problems/{result.Code}"
        };
        problem.Extensions["code"] = result.Code;
        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problem)
        {
            StatusCode = result.StatusCode
        };
    }
}
