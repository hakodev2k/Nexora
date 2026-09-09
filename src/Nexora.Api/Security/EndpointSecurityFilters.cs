namespace Nexora.Api.Security;

public static class EndpointSecurityFilters
{
    public static RouteGroupBuilder RequireCsrfForUnsafeMethods(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (invocationContext, next) =>
        {
            var http = invocationContext.HttpContext;
            if (!HttpMethods.IsPost(http.Request.Method) &&
                !HttpMethods.IsPut(http.Request.Method) &&
                !HttpMethods.IsPatch(http.Request.Method) &&
                !HttpMethods.IsDelete(http.Request.Method))
            {
                return await next(invocationContext);
            }

            var csrf = http.RequestServices.GetRequiredService<CsrfTokenService>();
            var cookieSecret = http.Request.Cookies["__Host-NexoraCsrf"];
            var requestToken = http.Request.Headers["X-CSRF-Token"].ToString();
            if (csrf.Validate(cookieSecret, requestToken))
            {
                return await next(invocationContext);
            }

            return Results.Problem(
                title: "CSRF token is invalid.",
                statusCode: StatusCodes.Status403Forbidden,
                type: "/problems/CsrfInvalid",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "CsrfInvalid",
                    ["traceId"] = http.TraceIdentifier
                });
        });

        return group;
    }

    public static RouteGroupBuilder RequireDevelopmentSuperAdminProof(this RouteGroupBuilder group)
    {
        group.AddEndpointFilter(async (invocationContext, next) =>
        {
            var http = invocationContext.HttpContext;
            var environment = http.RequestServices.GetRequiredService<IHostEnvironment>();
            if (!environment.IsDevelopment())
            {
                return AdminDenied(http);
            }

            var proof = http.Request.Headers["X-Nexora-Dev-SuperAdmin"].ToString();
            if (!string.Equals(proof, "true", StringComparison.OrdinalIgnoreCase))
            {
                return AdminDenied(http);
            }

            return await next(invocationContext);
        });

        return group;
    }

    private static IResult AdminDenied(HttpContext http) => Results.Problem(
        title: "Permission denied.",
        statusCode: StatusCodes.Status403Forbidden,
        type: "/problems/PermissionDenied",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "PermissionDenied",
            ["traceId"] = http.TraceIdentifier
        });
}
