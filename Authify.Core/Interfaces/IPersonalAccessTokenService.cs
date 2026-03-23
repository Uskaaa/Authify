using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IPersonalAccessTokenService
{
    Task<OperationResult<CreatePersonalAccessTokenResponse>> CreateAsync(string userId, CreatePersonalAccessTokenRequest request);
    Task<OperationResult<List<PersonalAccessTokenDto>>> GetMineAsync(string userId);
    Task<OperationResult> RevokeAsync(string userId, Guid tokenId);
    Task<OperationResult<ResolvePersonalAccessTokenResponse>> ResolveAsync(string token);
}
