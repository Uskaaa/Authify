using Authify.Core.Common;
using Authify.Core.Models.Teams;

namespace Authify.Core.Interfaces;

public interface ITeamInvitationService
{
    Task<OperationResult<TeamInvitationDto>> CreateInvitationAsync(string adminUserId, CreateInvitationRequest request);
    Task<OperationResult<List<TeamInvitationDto>>> GetInvitationsAsync(string adminUserId);
    Task<OperationResult> RevokeInvitationAsync(string adminUserId, string invitationId);
    Task<OperationResult<TeamInvitationDto>> GetInvitationByTokenAsync(string token);
    Task<OperationResult<string>> AcceptInvitationAsync(AcceptInvitationRequest request);
}
