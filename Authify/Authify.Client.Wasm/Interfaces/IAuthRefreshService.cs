using Authify.Client.Wasm.Models;
using Authify.Core.Common;

namespace Authify.Client.Wasm.Interfaces;

public interface IAuthRefreshService
{
    Task<OperationResult<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(RefreshTokenRequest request);
}