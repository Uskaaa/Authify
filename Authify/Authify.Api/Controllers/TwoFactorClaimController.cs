using System.Security.Claims;
using Authify.Core.Interfaces;
using Authify.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TwoFactorClaimController : ControllerBase
{
    private readonly ITwoFactorClaimService _twoFactorClaimService;

    public TwoFactorClaimController(ITwoFactorClaimService twoFactorClaimService)
    {
        _twoFactorClaimService = twoFactorClaimService;
    }

    private string? GetUserId()
    {
        return User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Fügt eine neue TwoFactor-Methode hinzu oder aktualisiert eine bestehende.
    /// </summary>
    [HttpPost("add-or-update")]
    public async Task<IActionResult> AddOrUpdate([FromBody] TwoFactorRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _twoFactorClaimService.AddOrUpdateAsync(userId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Entfernt eine TwoFactor-Methode.
    /// </summary>
    [HttpPost("remove")]
    public async Task<IActionResult> Remove([FromBody] TwoFactorRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _twoFactorClaimService.RemoveAsync(userId, request);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gibt alle TwoFactor-Methoden des aktuellen Users zurück.
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _twoFactorClaimService.GetAllAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Gibt die bevorzugte TwoFactor-Methode des aktuellen Users zurück.
    /// </summary>
    [HttpGet("preferred")]
    public async Task<IActionResult> GetPreferred()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _twoFactorClaimService.GetPreferredAsync(userId);
        return result.Success ? Ok(result) : NotFound(result);
    }
}