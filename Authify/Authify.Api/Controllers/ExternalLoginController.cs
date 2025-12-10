using System.Security.Claims;
using Authify.Application.Data;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExternalLoginController : ControllerBase
{
    private readonly IExternalLoginManagementService _externalLoginService;

    public ExternalLoginController(
        IExternalLoginManagementService externalLoginService)
    {
        _externalLoginService = externalLoginService;
    }

    private string? GetUserId()
    {
        return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    
    [HttpGet("connected")]
    public async Task<IActionResult> GetConnectedProviders()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var result = await _externalLoginService.GetConnectedProvidersAsync(userId);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("disconnect/{provider}")]
    public async Task<IActionResult> DisconnectProvider(string provider)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _externalLoginService.DisconnectProviderAsync(userId, provider);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("connect")]
    public async Task<IActionResult> ConnectProvider([FromBody] ConnectExternalLoginRequest externalLoginRequest)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();
        
        var result = await _externalLoginService.ConnectProviderAsync(userId, externalLoginRequest);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}