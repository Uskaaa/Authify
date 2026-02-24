using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExternalLoginController : ControllerBase
{
    private readonly IExternalLoginManagementService _externalLoginService;

    public ExternalLoginController(IExternalLoginManagementService externalLoginService)
    {
        _externalLoginService = externalLoginService;
    }

    private string? GetUserId() => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("connected")]
    public async Task<IActionResult> GetConnectedProviders()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _externalLoginService.GetConnectedProvidersAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("disconnect/{provider}")]
    public async Task<IActionResult> DisconnectProvider(string provider)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _externalLoginService.DisconnectProviderAsync(userId, provider);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectProvider([FromBody] ConnectExternalLoginRequest externalLoginRequest)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _externalLoginService.ConnectProviderAsync(userId, externalLoginRequest);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
