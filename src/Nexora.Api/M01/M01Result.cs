namespace Nexora.Api.M01;

public sealed record M01Result<T>(
    bool Succeeded,
    T? Value,
    string Code,
    int StatusCode,
    string Title)
{
    public static M01Result<T> Success(T value, int statusCode = StatusCodes.Status200OK, string code = "Ok") =>
        new(true, value, code, statusCode, code);

    public static M01Result<T> NoContent(string code = "NoContent") =>
        new(true, default, code, StatusCodes.Status204NoContent, code);

    public static M01Result<T> Failure(string code, int statusCode, string title) =>
        new(false, default, code, statusCode, title);
}

public static class M01HttpResultExtensions
{
    public static IResult ToHttp<T>(this M01Result<T> result, HttpContext context)
    {
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
