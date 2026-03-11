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
[Authorize]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly TeamFeatureOptions _teamFeature;

    public TeamController(ITeamService teamService, TeamFeatureOptions teamFeature)
    {
        _teamService = teamService;
        _teamFeature = teamFeature;
    }

    private IActionResult FeatureDisabled() =>
        NotFound(OperationResult.Fail("Team-Features sind in dieser Anwendung nicht aktiviert."));

    private string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpPost("create")]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.CreateTeamAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("my-team")]
    public async Task<IActionResult> GetMyTeam()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        // Admin-Sicht hat Vorrang, dann Mitglieds-Sicht
        var adminResult = await _teamService.GetTeamByAdminAsync(userId);
        if (adminResult.Success) return Ok(adminResult);

        var memberResult = await _teamService.GetTeamByMemberAsync(userId);
        return memberResult.Success ? Ok(memberResult) : NotFound(memberResult);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateTeam([FromBody] UpdateTeamRequest request)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.UpdateTeamAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteTeam()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.DeleteTeamAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.GetMembersAsync(userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("members/create")]
    public async Task<IActionResult> CreateMember([FromBody] CreateTeamMemberRequest request)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.CreateMemberAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("members/{memberId}")]
    public async Task<IActionResult> RemoveMember(string memberId)
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.RemoveMemberAsync(userId, memberId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("is-admin")]
    public async Task<IActionResult> IsAdmin()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.IsTeamAdminAsync(userId);
        return Ok(result);
    }

    [HttpGet("is-member")]
    public async Task<IActionResult> IsMember()
    {
        if (!_teamFeature.IsEnabled) return FeatureDisabled();
        var userId = UserId;
        if (userId == null) return Unauthorized();

        var result = await _teamService.IsTeamMemberAsync(userId);
        return Ok(result);
    }
}
