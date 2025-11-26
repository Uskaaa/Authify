using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<OperationResult<RefreshTokenRequest>> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, RefreshTokenRequest refreshTokenRequest);
}