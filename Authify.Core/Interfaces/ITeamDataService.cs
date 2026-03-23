using Authify.Core.Common;
using Authify.Core.Models.Teams;

namespace Authify.Core.Interfaces;

/// <summary>
/// UI-seitiger Service für Team-Operationen. Wird von WasmTeamDataService und
/// ServerTeamDataService implementiert.
/// </summary>
public interface ITeamDataService
{
    // Team-Verwaltung
    Task<OperationResult<TeamDto>> CreateTeamAsync(CreateTeamRequest request);
    Task<OperationResult<TeamDto>> GetMyTeamAsync();
    Task<OperationResult<TeamDto>> UpdateTeamAsync(UpdateTeamRequest request);
    Task<OperationResult> DeleteTeamAsync();

    // Mitglieder-Verwaltung
    Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync();
    Task<OperationResult<TeamMemberDto>> CreateMemberAsync(CreateTeamMemberRequest request);
    Task<OperationResult> RemoveMemberAsync(string memberId);

    // Einladungs-Verwaltung
    Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(CreateInvitationRequest request);
    Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync();
    Task<OperationResult> RevokeInvitationAsync(string invitationId);

    // Öffentliche Einladungs-Abfrage (kein Auth erforderlich)
    Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token);
    Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request);

    // Rollen-Abfragen
    Task<OperationResult<bool>> IsTeamAdminAsync();
    Task<OperationResult<bool>> IsTeamMemberAsync();
}
