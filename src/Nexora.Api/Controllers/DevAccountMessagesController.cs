using Microsoft.AspNetCore.Mvc;
using Nexora.Api.Features.Identity;

namespace Nexora.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v1/dev/account-messages")]
public sealed class DevAccountMessagesController : ControllerBase
{
    private readonly DevelopmentIdentityStore _store;
    private readonly IHostEnvironment _environment;

    public DevAccountMessagesController(DevelopmentIdentityStore store, IHostEnvironment environment)
    {
        _store = store;
        _environment = environment;
    }

    [HttpGet(Name = "devListAccountMessages")]
    public ActionResult<IReadOnlyList<DevAccountMessage>> List()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] = "no-store";
        return Ok(_store.CapturedMessages());
    }
}
