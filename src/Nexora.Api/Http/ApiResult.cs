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

public static class ApiHttpResultExtensions
{
    public static IResult ToHttp<T>(this ApiResult<T> result, HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";

        if (result.Succeeded)
        {
            if (result.StatusCode == StatusCodes.Status204NoContent)
            {
                return Results.NoContent();
            }

            return Results.Json(result.Value, statusCode: result.StatusCode);
        }

        return Results.Problem(
            title: result.Title,
            statusCode: result.StatusCode,
            type: $"/problems/{result.Code}",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Code,
                ["traceId"] = context.TraceIdentifier
            });
    }
}
