using System.Security.Claims;
using Authify.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[Route("auth")]
[ApiController]
public class ExternalAuthController : Controller
{
    private readonly IExternalAuthService _externalAuthService;

    public ExternalAuthController(IExternalAuthService externalAuthService)
    {
        _externalAuthService = externalAuthService;
    }

    // GET /auth/external-login?provider=Google&returnUrl=/&mode=login
    [HttpGet("external-login")]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null, string mode = "login")
    {
        if (string.IsNullOrWhiteSpace(provider))
            return BadRequest(new { error = "Provider is required." });

        string? userId = null;

        if (mode == "connect")
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "You must be logged in to connect accounts." });
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