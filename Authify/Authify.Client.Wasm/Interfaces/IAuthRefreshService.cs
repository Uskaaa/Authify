using Authify.Core.Common;
using Authify.Core.Models;

namespace Authify.Core.Interfaces;

public interface IAuthRefreshService
{
    Task<OperationResult<(string AccessToken, RefreshTokenRequest RefreshToken)>> RefreshTokenAsync(RefreshTokenRequest request);
}