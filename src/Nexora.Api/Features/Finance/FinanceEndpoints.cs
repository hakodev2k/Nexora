using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Finance;
using Nexora.Application.Identity;

namespace Nexora.Api.Features.Finance;

public static class FinanceEndpoints
{
    public static WebApplication MapFinanceEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1").RequireCsrfForUnsafeMethods();

        api.MapGet("/finance/categories", (HttpContext context, string? query, int? limit,
            IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ListCategories(principal, query, limit),
                value => new FinanceCategoryPageResponse(value.Items.Select(ToResponse).ToArray(), value.NextCursor)))
            .WithName("listFinanceCategories");

        api.MapPost("/finance/categories", (HttpContext context, FinanceCategoryRequest request,
            IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.CreateCategory(principal,
                    new FinanceCategoryCommand(request.Title, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier),
                ToResponse))
            .WithName("createFinanceCategory");

        api.MapPut("/finance/categories/{categoryId:guid}", (HttpContext context, Guid categoryId,
            FinanceCategoryRequest request, IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.UpdateCategory(principal, categoryId, context.Request.Headers.IfMatch.ToString(),
                    new FinanceCategoryCommand(request.Title, request.IfMatch), IdempotencyKey(context), context.TraceIdentifier),
                ToResponse))
            .WithName("updateFinanceCategory");

        api.MapDelete("/finance/categories/{categoryId:guid}", (HttpContext context, Guid categoryId,
            IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.RemoveCategory(principal, categoryId, context.Request.Headers.IfMatch.ToString(),
                    IdempotencyKey(context), context.TraceIdentifier), _ => (object?)null))
            .WithName("removeFinanceCategory");

        api.MapGet("/finance/records", (HttpContext context, Guid? categoryId, string? currencyCode,
            DateOnly? from, DateOnly? to, string? query, int? limit, IFinanceService service,
            IIdentityService identity, SessionCookieService cookies) =>
            Map(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.ListRecords(principal, categoryId, currencyCode, from, to, query, limit),
                value => new FinanceRecordPageResponse(value.Items.Select(ToResponse).ToArray(),
                    value.Summaries.Select(summary => new FinanceSummaryResponse(summary.CurrencyCode, summary.Amount)).ToArray(),
                    value.NextCursor)))
            .WithName("listFinanceRecords");

        api.MapPost("/finance/records", (HttpContext context, FinanceRecordRequest request,
            IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.CreateRecord(principal, ToCommand(request), IdempotencyKey(context), context.TraceIdentifier),
                ToResponse))
            .WithName("createFinanceRecord");

        api.MapPut("/finance/records/{recordId:guid}", (HttpContext context, Guid recordId,
            FinanceRecordRequest request, IFinanceService service, IIdentityService identity, SessionCookieService cookies) =>
            MapResource(context, identity.GetPrincipal(cookies.ReadRawHandle(context.Request)),
                principal => service.UpdateRecord(principal, recordId, context.Request.Headers.IfMatch.ToString(),
                    ToCommand(request), IdempotencyKey(context), context.TraceIdentifier), ToResponse))
            .WithName("updateFinanceRecord");

        return app;
    }

    private static FinanceManualRecordCommand ToCommand(FinanceRecordRequest request) =>
        new(request.CategoryId, request.Amount, request.CurrencyCode, request.OccurredOn, request.Note);

    private static IResult Map<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null)
            return ToHttp(context, new IdentityOperationResult<TOut>(auth.Succeeded, default, auth.Code, auth.StatusCode, auth.Title));
        var result = operation(auth.Value);
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult MapResource<TIn, TOut>(HttpContext context, IdentityOperationResult<IdentityPrincipal> auth,
        Func<IdentityPrincipal, IdentityOperationResult<TIn>> operation, Func<TIn, TOut> map)
    {
        if (!auth.Succeeded || auth.Value is null) return ToHttp(context, auth);
        var result = operation(auth.Value);
        if (result.Succeeded && result.Value is FinanceCategoryRecord category)
            context.Response.Headers.ETag = category.ETag;
        else if (result.Succeeded && result.Value is FinanceManualRecord record)
            context.Response.Headers.ETag = record.ETag;
        return result.Succeeded && result.Value is not null
            ? ToHttp(context, IdentityOperationResult<TOut>.Success(map(result.Value), result.StatusCode, result.Code))
            : ToHttp(context, new IdentityOperationResult<TOut>(result.Succeeded, default, result.Code, result.StatusCode, result.Title));
    }

    private static IResult ToHttp<T>(HttpContext context, IdentityOperationResult<T> result) =>
        new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(context);

    private static string? IdempotencyKey(HttpContext context) => context.Request.Headers["Idempotency-Key"].ToString();

    private static FinanceCategoryResponse ToResponse(FinanceCategoryRecord value) =>
        new(value.Id, value.Title, value.UsageCount, value.CreatedAt, value.UpdatedAt, value.ETag);

    private static FinanceRecordResponse ToResponse(FinanceManualRecord value) =>
        new(value.Id, value.CategoryId, value.CategoryTitle, value.Amount, value.CurrencyCode, value.OccurredOn,
            value.Note, value.CreatedAt, value.UpdatedAt, value.ETag);
}
