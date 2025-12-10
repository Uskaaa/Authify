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

        var redirectUrl = _externalAuthService.GetRedirectUrl(provider, returnUrl, mode);
        var props = _externalAuthService.GetAuthProperties(provider, redirectUrl);

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