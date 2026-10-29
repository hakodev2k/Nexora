using System.Text.Json.Serialization;
using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;
using Nexora.Application.Identity;
using Nexora.Application.News;
using Nexora.Application.Reading;

namespace Nexora.Api.Features.News;

public static class NewsReaderEndpoints
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record ReadRequest([property: JsonRequired] bool Read);
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record MarkAllRequest([property: JsonRequired] IReadOnlyList<Guid> SubscriptionIds, [property: JsonRequired, JsonConverter(typeof(ExplicitInstantJsonConverter))] DateTimeOffset Through);
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record ClearHistoryRequest([property: JsonRequired, JsonConverter(typeof(ExplicitInstantJsonConverter))] DateTimeOffset Through);
    public static WebApplication MapNewsReaderEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1/news").RequireCsrfForUnsafeMethods();
        api.MapGet("/capabilities", (HttpContext c, INewsReaderService s, IIdentityService i, SessionCookieService cookies) => Run(c,i,cookies,a=>s.Capabilities(a)));
        api.MapGet("/sources", (HttpContext c, Guid? cursor, string? query, Guid? categoryId, string? state, INewsReaderService s, IIdentityService i, SessionCookieService cookies) => Run(c,i,cookies,a=>s.Sources(a,cursor,query,categoryId,state)));
        api.MapPost("/sources", (HttpContext c, NewsFollow body, INewsReaderService s, IIdentityService i, SessionCookieService cookies) => Run(c,i,cookies,a=>s.Follow(a,body,Key(c),c.TraceIdentifier)));
        api.MapPut("/sources/{id:guid}", (HttpContext c, Guid id, NewsSourceMetadata body, INewsReaderService s, IIdentityService i, SessionCookieService cookies) => Run(c,i,cookies,a=>s.UpdateSource(a,id,c.Request.Headers.IfMatch.ToString(),body,Key(c),c.TraceIdentifier)));
        foreach(var operation in new[]{"pause","resume","unfollow"})
        {
            var selected=operation;
            api.MapPost("/sources/{id:guid}/"+selected,(HttpContext c,Guid id,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.SourceState(a,id,c.Request.Headers.IfMatch.ToString(),selected,Key(c),c.TraceIdentifier)));
        }
        api.MapGet("/articles",(HttpContext c,Guid? subscriptionId,Guid? categoryId,string? query,bool? unreadOnly,string? cursor,DateTimeOffset? from,DateTimeOffset? to,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>
            ExplicitInstantJsonConverter.QueryValid(c,"from")&&ExplicitInstantJsonConverter.QueryValid(c,"to")?s.Articles(a,subscriptionId,categoryId,query,unreadOnly??false,cursor,from,to):IdentityOperationResult<NewsArticlePage>.Failure("ValidationFailed",422,"Explicit UTC date offsets are required.")));
        api.MapGet("/articles/{id:guid}",(HttpContext c,Guid id,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Article(a,id)));
        api.MapPost("/articles/{id:guid}/read",(HttpContext c,Guid id,ReadRequest body,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.MarkRead(a,id,c.Request.Headers.IfMatch.ToString(),body.Read,Key(c),c.TraceIdentifier)));
        api.MapPost("/articles/{id:guid}/save",(HttpContext c,Guid id,IReadingService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.Save(a,new("NewsArticle",id),Key(c),c.TraceIdentifier)));
        api.MapPost("/articles/mark-all-read",(HttpContext c,MarkAllRequest body,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.MarkAllRead(a,new(body.SubscriptionIds,body.Through),Key(c),c.TraceIdentifier)));
        api.MapGet("/history",(HttpContext c,string? cursor,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.History(a,cursor)));
        api.MapGet("/articles/saved",(HttpContext c,string? cursor,IReadingService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.List(a,null,25,cursor,"NewsArticle")));
        api.MapPost("/history/clear",(HttpContext c,ClearHistoryRequest body,INewsReaderService s,IIdentityService i,SessionCookieService cookies)=>Run(c,i,cookies,a=>s.ClearHistory(a,body.Through,Key(c),c.TraceIdentifier)));
        return app;
    }
    private static string Key(HttpContext c)=>c.Request.Headers["Idempotency-Key"].ToString();
    private static IResult Run<T>(HttpContext c,IIdentityService identity,SessionCookieService cookies,Func<IdentityPrincipal,IdentityOperationResult<T>> operation)
    {
        var auth=identity.GetPrincipal(cookies.ReadRawHandle(c.Request));
        if(!auth.Succeeded||auth.Value is null)return new ApiResult<T>(false,default,auth.Code,auth.StatusCode,auth.Title).ToHttp(c);
        var result=operation(auth.Value);
        if(result.Value is NewsArticle article)c.Response.Headers.ETag=article.StateETag;
        if(result.Succeeded&&result.Acknowledgement is {} acknowledgement)return new ApiResult<CommittedResourceAcknowledgement>(true,acknowledgement,result.Code,result.StatusCode,result.Title).ToHttp(c);
        return new ApiResult<T>(result.Succeeded,result.Value,result.Code,result.StatusCode,result.Title).ToHttp(c);
    }
}
