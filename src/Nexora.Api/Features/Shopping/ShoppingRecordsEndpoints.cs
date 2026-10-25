using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.Shopping;

namespace Nexora.Api.Features.Shopping;

public static class ShoppingRecordsEndpoints
{
    public static WebApplication MapShoppingRecordsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/shopping/records").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, s.Capabilities));
        api.MapGet("/sellers", (HttpContext c, int? page, string? query, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Sellers(a, page ?? 1, query)));
        api.MapGet("/sellers/{id:guid}", (HttpContext c, Guid id, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Seller(a, id)));
        api.MapPost("/sellers", (HttpContext c, SellerWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveSeller(a, null, null, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/sellers/{id:guid}", (HttpContext c, Guid id, SellerWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveSeller(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapGet("/orders", (HttpContext c, int? page, string? query, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Orders(a, page ?? 1, query)));
        api.MapGet("/orders/{id:guid}", (HttpContext c, Guid id, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Order(a, id)));
        api.MapPost("/orders", (HttpContext c, OrderWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveOrder(a, null, null, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/orders/{id:guid}", (HttpContext c, Guid id, OrderWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveOrder(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        api.MapGet("/warranties", (HttpContext c, int? page, string? query, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Warranties(a, page ?? 1, query)));
        api.MapGet("/warranties/{id:guid}", (HttpContext c, Guid id, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.Warranty(a, id)));
        api.MapPost("/warranties", (HttpContext c, WarrantyWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveWarranty(a, null, null, body, Key(c), c.TraceIdentifier)));
        api.MapPut("/warranties/{id:guid}", (HttpContext c, Guid id, WarrantyWrite body, IShoppingRecordsService s, IIdentityService i, SessionCookieService cookies) => Run(c, i, cookies, a => s.SaveWarranty(a, id, c.Request.Headers.IfMatch.ToString(), body, Key(c), c.TraceIdentifier)));
        return app;
    }
    private static string Key(HttpContext c) => c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c, IIdentityService identity, SessionCookieService cookies,
        Func<IdentityPrincipal, IdentityOperationResult<T>> work)
    {
        var auth = identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        var result = auth.Succeeded && auth.Value is not null ? work(auth.Value) : IdentityOperationResult<T>.Failure(auth.Code, auth.StatusCode, auth.Title);
        var tag = result.Value switch { SellerRecord s => s.ETag, OrderRecord o => o.ETag, WarrantyRecord w => w.ETag, _ => null };
        if (tag is not null) c.Response.Headers.ETag = tag;
        return new ApiResult<T>(result.Succeeded, result.Value, result.Code, result.StatusCode, result.Title).ToHttp(c);
    }
}
