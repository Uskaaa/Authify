using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Client.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSessionController : ControllerBase
{
    private readonly IUserSessionService _userSessionService;

    public UserSessionController(IUserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
    }

    private string? GetUserId() => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var sessions = await _userSessionService.GetActiveSessionsAsync(userId);
        return Ok(sessions);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterSession([FromBody] RegisterSessionRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var session = await _userSessionService.RegisterSessionAsync(userId, request.DeviceName, request.IpAddress, request.AuthType);
        return Ok(session);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutSessionRequest request)
    {
        await _userSessionService.MarkSessionInactiveAsync(request.SessionId);
        return Ok();
    }
}

public class RegisterSessionRequest
{
    public string DeviceName { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string AuthType { get; set; } = "Cookie";
}

public class LogoutSessionRequest
{
    public string SessionId { get; set; } = default!;
}
