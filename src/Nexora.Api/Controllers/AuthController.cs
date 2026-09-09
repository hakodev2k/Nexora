using Microsoft.AspNetCore.Mvc;
using Nexora.Api.Features.Identity;
using Nexora.Api.Http;
using Nexora.Api.Security;

namespace Nexora.Api.Controllers;

[ApiController]
[ValidateCsrf]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CsrfTokenService _csrfTokens;
    private readonly DevelopmentIdentityStore _store;
    private readonly SessionCookieService _cookies;

    public AuthController(CsrfTokenService csrfTokens, DevelopmentIdentityStore store, SessionCookieService cookies)
    {
        _csrfTokens = csrfTokens;
        _store = store;
        _cookies = cookies;
    }

    [HttpGet("csrf", Name = "getCsrf")]
    public ActionResult<CsrfResponse> GetCsrf()
    {
        var issued = _csrfTokens.Issue();

        Response.Headers["Cache-Control"] = "no-store";
        Response.Cookies.Append("__Host-NexoraCsrf", issued.CookieSecret, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(30)
        });

        return Ok(new CsrfResponse(issued.RequestToken, "csrf", 1800));
    }

    [HttpPost("registrations", Name = "register")]
    public ActionResult Register([FromBody] RegistrationRequest request) =>
        _store.Register(request).ToActionResult(this);

    [HttpPost("verifications", Name = "verify")]
    public ActionResult Verify([FromBody] TokenProofRequest request) =>
        _store.Verify(request).ToActionResult(this);

    [HttpPost("verifications/resend", Name = "resendVerification")]
    public ActionResult ResendVerification([FromBody] EmailRequest request) =>
        _store.ResendVerification(request).ToActionResult(this);

    [HttpPost("login", Name = "login")]
    public ActionResult Login([FromBody] CredentialsRequest request)
    {
        var result = _store.Login(request);
        if (!result.Succeeded || result.Value is null)
        {
            return result.ToActionResult(this);
        }

        _cookies.Append(Response, result.Value.RawSessionHandle, result.Value.ExpiresAt);
        return Ok(new LoginResponse(result.Value.Profile, result.Value.ExpiresAt));
    }

    [HttpPost("logout", Name = "logout")]
    public ActionResult Logout()
    {
        var result = _store.Logout(_cookies.ReadRawHandle(Request));
        _cookies.Clear(Response);
        return result.ToActionResult(this);
    }

    [HttpPost("reauth", Name = "reauth")]
    public ActionResult Reauth([FromBody] PasswordProofRequest request) =>
        _store.Reauth(_cookies.ReadRawHandle(Request), request).ToActionResult(this);

    [HttpPost("password-resets", Name = "requestReset")]
    public ActionResult RequestPasswordReset([FromBody] EmailRequest request) =>
        _store.RequestPasswordReset(request).ToActionResult(this);

    [HttpPost("password-resets/confirm", Name = "confirmReset")]
    public ActionResult ConfirmPasswordReset([FromBody] ResetProofRequest request) =>
        _store.ConfirmPasswordReset(request).ToActionResult(this);
}
