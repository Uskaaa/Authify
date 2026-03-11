using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;
using Microsoft.AspNetCore.Http;

namespace Authify.Client.Server.Services;

/// <summary>
/// Server-seitige Implementierung von ITeamDataService.
/// Delegiert direkt an die Team-Services (kein HTTP-Proxy nötig da kein Cookie-Set).
/// </summary>
public class ServerTeamDataService : ITeamDataService
{
    private readonly ITeamService _teamService;
    private readonly ITeamInvitationService _invitationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerTeamDataService(ITeamService teamService, ITeamInvitationService invitationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _teamService = teamService;
        _invitationService = invitationService;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? GetCurrentUserId() =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<OperationResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<TeamDto>.Fail("Nicht authentifiziert.");
        return await _teamService.CreateTeamAsync(userId, request);
    }

    public async Task<OperationResult<TeamDto>> GetMyTeamAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<TeamDto>.Fail("Nicht authentifiziert.");

        var adminResult = await _teamService.GetTeamByAdminAsync(userId);
        if (adminResult.Success) return adminResult;

        return await _teamService.GetTeamByMemberAsync(userId);
    }

    public async Task<OperationResult<TeamDto>> UpdateTeamAsync(UpdateTeamRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<TeamDto>.Fail("Nicht authentifiziert.");
        return await _teamService.UpdateTeamAsync(userId, request);
    }

    public async Task<OperationResult> DeleteTeamAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult.Fail("Nicht authentifiziert.");
        return await _teamService.DeleteTeamAsync(userId);
    }

    public async Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<List<TeamMemberDto>>.Fail("Nicht authentifiziert.");
        return await _teamService.GetMembersAsync(userId);
    }

    public async Task<OperationResult<TeamMemberDto>> CreateMemberAsync(CreateTeamMemberRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<TeamMemberDto>.Fail("Nicht authentifiziert.");
        return await _teamService.CreateMemberAsync(userId, request);
    }

    public async Task<OperationResult> RemoveMemberAsync(string memberId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult.Fail("Nicht authentifiziert.");
        return await _teamService.RemoveMemberAsync(userId, memberId);
    }

    public async Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(CreateInvitationRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<TeamInvitationDto>.Fail("Nicht authentifiziert.");
        return await _invitationService.CreateInvitationAsync(userId, request);
    }

    public async Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<List<TeamInvitationDto>>.Fail("Nicht authentifiziert.");
        return await _invitationService.GetInvitationsAsync(userId);
    }

    public async Task<OperationResult> RevokeInvitationAsync(string invitationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult.Fail("Nicht authentifiziert.");
        return await _invitationService.RevokeInvitationAsync(userId, invitationId);
    }

    public Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token) =>
        _invitationService.GetInvitationByTokenAsync(token);

    public Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request) =>
        _invitationService.AcceptInvitationAsync(request);

    public async Task<OperationResult<bool>> IsTeamAdminAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<bool>.Ok(false);
        return await _teamService.IsTeamAdminAsync(userId);
    }

    public async Task<OperationResult<bool>> IsTeamMemberAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return OperationResult<bool>.Ok(false);
        return await _teamService.IsTeamMemberAsync(userId);
    }
}
