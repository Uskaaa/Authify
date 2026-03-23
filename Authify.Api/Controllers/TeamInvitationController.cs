using System.Security.Claims;
using Authify.Core.Common;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[Controller]")]
public class TeamInvitationController : ControllerBase
{
    private readonly ITeamInvitationService _invitationService;
    private readonly TeamFeatureOptions _teamFeature;

    public TeamInvitationController(ITeamInvitationService invitationService, TeamFeatureOptions teamFeature)
    {
        _invitationService = invitationService;
        _teamFeature = teamFeature;
    }

    private IActionResult FeatureDisabled() =>
        NotFound(OperationResult.Fail("Team-Features sind in dieser Anwendung nicht aktiviert."));

    private string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _invitationService.CreateInvitationAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> GetInvitations()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _invitationService.GetInvitationsAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpDelete("{invitationId}/revoke")]
    public async Task<IActionResult> RevokeInvitation(string invitationId)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _invitationService.RevokeInvitationAsync(userId, invitationId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Öffentlicher Endpunkt – kein Auth erforderlich, nur gültiger Token.
    /// </summary>
    [HttpGet("by-token/{token}")]
    public async Task<IActionResult> GetByToken(string token)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var result = await _invitationService.GetInvitationByTokenAsync(token);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Öffentlicher Endpunkt – Einladung annehmen und Account erstellen.
    /// Gibt Redirect-Pfad zu change-password zurück.
    /// </summary>
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var result = await _invitationService.AcceptInvitationAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
