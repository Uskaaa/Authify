using System.Security.Claims;
using Authify.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Application.Controllers;

[Route("auth")]
public class ExternalAuthController : Controller
{
    private readonly ExternalAuthService _externalAuthService;

    public ExternalAuthController(ExternalAuthService externalAuthService)
    {
        _externalAuthService = externalAuthService;
    }

    [HttpGet("login/{provider}")]
    public IActionResult ExternalLogin(string provider, string? returnUrl = "/")
    {
        var redirectUrl = _externalAuthService.GetRedirectUrl(provider, returnUrl);
        var props = _externalAuthService.GetAuthProperties(provider, redirectUrl);
        return Challenge(props, provider);
    }

    [HttpGet("externallogin-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = "/", string? remoteError = null)
    {
        var result = await _externalAuthService.HandleExternalCallbackAsync(returnUrl, remoteError);
        return result;
    }
}