using System.Security.Claims;
using Authify.Core.Common;
using Authify.Core.Features;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authify.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LdapController : ControllerBase
{
    private readonly ILdapService _ldapService;
    private readonly ITeamService _teamService;
    private readonly LdapFeatureOptions _ldapFeature;

    public LdapController(ILdapService ldapService, ITeamService teamService, LdapFeatureOptions ldapFeature)
    {
        _ldapService = ldapService;
        _teamService = teamService;
        _ldapFeature = ldapFeature;
    }

    private IActionResult FeatureDisabled() =>
        NotFound(OperationResult.Fail("LDAP-Features sind in dieser Anwendung nicht aktiviert."));

    private string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Gibt die LDAP-Konfigurationen des Teams zurück, dessen Admin der aktuelle User ist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConfigurations()
    {
        if (!_ldapFeature.IsEnabled) return FeatureDisabled();
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return Forbid();

        var result = await _ldapService.GetConfigurationsForTeamAsync(teamId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLdapConfigurationRequest request)
    {
        if (!_ldapFeature.IsEnabled) return FeatureDisabled();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return Forbid();

        var result = await _ldapService.CreateConfigurationAsync(teamId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateLdapConfigurationRequest request)
    {
        if (!_ldapFeature.IsEnabled) return FeatureDisabled();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return Forbid();

        var result = await _ldapService.UpdateConfigurationAsync(id, teamId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!_ldapFeature.IsEnabled) return FeatureDisabled();

        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return Forbid();

        var result = await _ldapService.DeleteConfigurationAsync(id, teamId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] LdapTestConnectionRequest request)
    {
        if (!_ldapFeature.IsEnabled) return FeatureDisabled();

        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return Forbid();

        var result = await _ldapService.TestConnectionAsync(request);
        return Ok(result);
    }

    private async Task<string?> GetAdminTeamIdAsync()
    {
        var userId = UserId;
        if (userId == null) return null;

        var teamResult = await _teamService.GetTeamByAdminAsync(userId);
        return teamResult.Success && teamResult.Data != null ? teamResult.Data.Id : null;
    }
}
