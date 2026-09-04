using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IPersonalAccessTokenService
{
    Task<OperationResult<CreatePersonalAccessTokenResponse>> CreateAsync(string userId, CreatePersonalAccessTokenRequest request);
    Task<OperationResult<List<PersonalAccessTokenDto>>> GetMineAsync(string userId);
    Task<OperationResult> RevokeAsync(string userId, Guid tokenId);
    Task<OperationResult> DeleteAsync(string userId, Guid tokenId); // mycelis_change - hard delete, unlike RevokeAsync
    Task<OperationResult<ResolvePersonalAccessTokenResponse>> ResolveAsync(string token);
}
