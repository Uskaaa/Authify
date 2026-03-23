using Authify.Core.Common;
using Authify.Core.Models.Teams;

namespace Authify.Core.Interfaces;

public interface ITeamService
{
    Task<OperationResult<TeamDto>> CreateTeamAsync(string adminUserId, CreateTeamRequest request);
    Task<OperationResult<TeamDto>> GetTeamByAdminAsync(string adminUserId);
    Task<OperationResult<TeamDto>> GetTeamByMemberAsync(string userId);
    Task<OperationResult<TeamDto>> UpdateTeamAsync(string adminUserId, UpdateTeamRequest request);
    Task<OperationResult> DeleteTeamAsync(string adminUserId);
    Task<OperationResult<List<TeamMemberDto>>> GetMembersAsync(string adminUserId);
    Task<OperationResult<TeamMemberDto>> CreateMemberAsync(string adminUserId, CreateTeamMemberRequest request);
    Task<OperationResult> RemoveMemberAsync(string adminUserId, string memberId);
    Task<OperationResult<bool>> IsTeamAdminAsync(string userId);
    Task<OperationResult<bool>> IsTeamMemberAsync(string userId);
}
