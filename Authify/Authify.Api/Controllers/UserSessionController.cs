using System.Security.Claims;
using Authify.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

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

    /// <summary>
    /// Gibt alle aktiven Sessions des aktuell eingeloggten Users zurück.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var sessions = await _userSessionService.GetActiveSessionsAsync(userId);
        return Ok(sessions);
    }

    /// <summary>
    /// Registriert eine neue Session für den User.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterSession([FromBody] RegisterSessionRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var session = await _userSessionService.RegisterSessionAsync(userId, request.DeviceName, request.IpAddress, request.AuthType);
        return Ok(session);
    }

    /// <summary>
    /// Markiert eine Session als inaktiv.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutSessionRequest request)
    {
        await _userSessionService.MarkSessionInactiveAsync(request.SessionId);
        return Ok();
    }

    private string? GetUserId()
    {
        // Nutzt ClaimTypes.NameIdentifier
        return User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}

/// <summary>
/// DTO für das Registrieren einer Session
/// </summary>
public class RegisterSessionRequest
{
    public string DeviceName { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string AuthType { get; set; } = default!; // z.B. "JWT", "Cookie", "OAuth"
}

/// <summary>
/// DTO für Logout / Session inaktiv setzen
/// </summary>
public class LogoutSessionRequest
{
    public string SessionId { get; set; } = default!;
}