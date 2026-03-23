using Authify.Core.Common;
using Authify.Core.Interfaces;
using Authify.Core.Models.Teams;

namespace Authify.UI.Services;

/// <summary>
/// Fallback-Implementierung von ITeamDataService für Anwendungen ohne Team-Features.
/// Alle Methoden geben eine deaktivierte/leere Antwort zurück.
/// </summary>
public class NullTeamDataService : ITeamDataService
{
    private static readonly OperationResult _disabled =
        OperationResult.Fail("Team-Features sind nicht aktiviert.");

    private static readonly OperationResult<bool> _falseResult =
        OperationResult<bool>.Ok(false);

    public Task<OperationResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request) =>
        Task.FromResult(OperationResult<TeamDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult<TeamDto>> GetMyTeamAsync() =>
        Task.FromResult(OperationResult<TeamDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult<TeamDto>> UpdateTeamAsync(UpdateTeamRequest request) =>
        Task.FromResult(OperationResult<TeamDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult> DeleteTeamAsync() =>
        Task.FromResult(_disabled);

    public Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync() =>
        Task.FromResult(OperationResult<List<TeamMemberDto>>.Ok([]));

    public Task<OperationResult<TeamMemberDto>> CreateMemberAsync(CreateTeamMemberRequest request) =>
        Task.FromResult(OperationResult<TeamMemberDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult> RemoveMemberAsync(string memberId) =>
        Task.FromResult(_disabled);

    public Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(CreateInvitationRequest request) =>
        Task.FromResult(OperationResult<TeamInvitationDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync() =>
        Task.FromResult(OperationResult<List<TeamInvitationDto>>.Ok([]));

    public Task<OperationResult> RevokeInvitationAsync(string invitationId) =>
        Task.FromResult(_disabled);

    public Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token) =>
        Task.FromResult(OperationResult<TeamInvitationDto>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request) =>
        Task.FromResult(OperationResult<string>.Fail("Team-Features sind nicht aktiviert."));

    public Task<OperationResult<bool>> IsTeamAdminAsync() =>
        Task.FromResult(_falseResult);

    public Task<OperationResult<bool>> IsTeamMemberAsync() =>
        Task.FromResult(_falseResult);
}
