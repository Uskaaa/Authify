using System.Security.Claims;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Ldap;
using Microsoft.AspNetCore.Http;

namespace Authify.Client.Server.Services;

/// <summary>
/// Server-seitige Implementierung von ILdapDataService.
/// Delegiert direkt an ILdapService + ITeamService (kein HTTP-Proxy).
/// </summary>
public class ServerLdapDataService : ILdapDataService
{
    private readonly ILdapService _ldapService;
    private readonly ITeamService _teamService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerLdapDataService(ILdapService ldapService, ITeamService teamService,
        IHttpContextAccessor httpContextAccessor)
    {
        _ldapService = ldapService;
        _teamService = teamService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? GetCurrentUserId() =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<string?> GetAdminTeamIdAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;
        var teamResult = await _teamService.GetTeamByAdminAsync(userId);
        return teamResult.Success && teamResult.Data != null ? teamResult.Data.Id : null;
    }

    public async Task<OperationResult<List<LdapConfigurationDto>>> GetConfigurationsAsync()
    {
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return OperationResult<List<LdapConfigurationDto>>.Fail("Kein Team oder keine Admin-Rechte.");
        return await _ldapService.GetConfigurationsForTeamAsync(teamId);
    }

    public async Task<OperationResult<LdapConfigurationDto>> CreateConfigurationAsync(CreateLdapConfigurationRequest request)
    {
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return OperationResult<LdapConfigurationDto>.Fail("Kein Team oder keine Admin-Rechte.");
        return await _ldapService.CreateConfigurationAsync(teamId, request);
    }

    public async Task<OperationResult<LdapConfigurationDto>> UpdateConfigurationAsync(string configId, UpdateLdapConfigurationRequest request)
    {
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return OperationResult<LdapConfigurationDto>.Fail("Kein Team oder keine Admin-Rechte.");
        return await _ldapService.UpdateConfigurationAsync(configId, teamId, request);
    }

    public async Task<OperationResult> DeleteConfigurationAsync(string configId)
    {
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return OperationResult.Fail("Kein Team oder keine Admin-Rechte.");
        return await _ldapService.DeleteConfigurationAsync(configId, teamId);
    }

    public async Task<LdapTestConnectionResult> TestConnectionAsync(LdapTestConnectionRequest request)
    {
        var teamId = await GetAdminTeamIdAsync();
        if (teamId == null) return LdapTestConnectionResult.Fail("Kein Team oder keine Admin-Rechte.");
        return await _ldapService.TestConnectionAsync(request);
    }
}
