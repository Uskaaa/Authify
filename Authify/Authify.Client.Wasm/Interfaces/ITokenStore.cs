using Authify.Client.Wasm.Models;
using Authify.UI.Common;

namespace Authify.Client.Wasm.Interfaces;

public interface ITokenStore
{
    Task<string?> GetAccessTokenAsync();
    Task<OperationResult<RefreshTokenRequest>> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task RemoveTokensAsync();
}