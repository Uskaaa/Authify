using System.Security.Claims;
using Authify.Application.Services;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[Route("auth")]
[ApiController]
public class ExternalAuthController : ControllerBase
{
    private readonly IExternalAuthService _externalAuthService;

    public ExternalAuthController(IExternalAuthService externalAuthService)
    {
        _externalAuthService = externalAuthService;
    }

    // GET /auth/external-login?provider=Google&returnUrl=/
    [HttpGet("external-login")]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null, string mode = "login")
    {
        if (string.IsNullOrWhiteSpace(provider))
            return BadRequest(new { error = "Provider is required." });

        string? userId = null;
        
        if (mode == "connect")
        {
            // HIER lesen wir die Claims aus, die durch den Token in der URL wiederhergestellt wurden.
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // Falls kein Token in der URL war oder er ungültig ist
                return Unauthorized(new { error = "You must be logged in to connect accounts." });
            }
        }

        var redirectUrl = _externalAuthService.GetRedirectUrl(provider, returnUrl, mode);
        var props = _externalAuthService.GetAuthProperties(provider, redirectUrl, userId);

        return Challenge(props, provider);
    }

    // GET /auth/externallogin-callback
    [HttpGet("externallogin-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string mode = "login", string? remoteError = null)
    {
        var result = await _externalAuthService.HandleExternalCallbackAsync(returnUrl, mode, remoteError);
        return result;
    }
}