using Microsoft.AspNetCore.Mvc;
using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;

namespace Nexora.Api.Controllers;

[ApiController]
[ValidateCsrf]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    private readonly DevelopmentIdentityStore _store;
    private readonly SessionCookieService _cookies;

    public MeController(DevelopmentIdentityStore store, SessionCookieService cookies)
    {
        _store = store;
        _cookies = cookies;
    }

    [HttpGet(Name = "getMe")]
    public ActionResult GetMe()
    {
        var result = _store.GetMe(_cookies.ReadRawHandle(Request));
        if (!result.Succeeded || result.Value is null)
        {
            return result.ToActionResult(this);
        }

        Response.Headers["ETag"] = result.Value.ETag;
        Response.Headers["Cache-Control"] = "no-store";
        return Ok(result.Value.Profile);
    }

    [HttpPatch(Name = "updateMe")]
    public ActionResult UpdateMe([FromBody] ProfilePatchRequest request)
    {
        var result = _store.UpdateMe(_cookies.ReadRawHandle(Request), Request.Headers.IfMatch.ToString(), request);
        if (!result.Succeeded || result.Value is null)
        {
            return result.ToActionResult(this);
        }

        Response.Headers["ETag"] = result.Value.ETag;
        Response.Headers["Cache-Control"] = "no-store";
        return Ok(result.Value.Profile);
    }

    [HttpGet("sessions", Name = "listSessions")]
    public ActionResult ListSessions() =>
        _store.ListSessions(_cookies.ReadRawHandle(Request)).ToActionResult(this);

    [HttpDelete("sessions/{sessionId:guid}", Name = "revokeSession")]
    public ActionResult RevokeSession(Guid sessionId) =>
        _store.RevokeSession(_cookies.ReadRawHandle(Request), sessionId).ToActionResult(this);

    [HttpPost("sessions/revoke-all", Name = "revokeAll")]
    public ActionResult RevokeAll()
    {
        var result = _store.RevokeAll(_cookies.ReadRawHandle(Request));
        if (result.Succeeded)
        {
            _cookies.Clear(Response);
        }

        return result.ToActionResult(this);
    }
}
